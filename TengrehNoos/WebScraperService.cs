using System.Globalization;
using System.Reflection.Metadata;
using TengrehNoos.Data;
using TengrehNoos.Entities;

namespace TengrehNoos;
using HtmlAgilityPack;
using System;


public class ScrapingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScrapingBackgroundService> _logger;
    
    public ScrapingBackgroundService(IServiceProvider serviceProvider, ILogger<ScrapingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            
            _logger.LogInformation($"WebScraping starting: {DateTime.UtcNow}");
            
            using (var scope = _serviceProvider.CreateScope())
            {
                var webScraperService = scope.ServiceProvider.GetRequiredService<WebScraperService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var lastMetaData = context.MetaData.OrderByDescending(m => m.LastScraped).FirstOrDefault();
                if (lastMetaData != null && lastMetaData.LastScraped.AddMinutes(15) > DateTime.Now.ToUniversalTime())
                {
                    _logger.LogInformation("Scraping already done in the last 15 minutes. Waiting for 5 minutes.");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }
                webScraperService.ScrapeWebsite(new Uri("https://tengrinews.kz"), "read", context);
                webScraperService.ScrapeWebsite(new Uri("https://tengrinews.kz"), "medicine", context);
                webScraperService.ScrapeWebsite(new Uri("https://tengrinews.kz"), "tech", context);
                webScraperService.ScrapeWebsite(new Uri("https://tengrinews.kz"), "economic", context);
                webScraperService.ScrapeWebsite(new Uri("https://tengrinews.kz"), "culture", context);
                webScraperService.ScrapeWebsite(new Uri("https://tengrinews.kz"), "science", context);
                webScraperService.ScrapeWebsite(new Uri("https://tengrinews.kz"), "kazakhstan_news", context);
                
            }
            //write results to the database
            var CurrentTime = DateTime.Now.ToUniversalTime();
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var metaData = new MetaData();
                var previousMetaData = context.MetaData.OrderByDescending(m => m.LastScraped).FirstOrDefault();
                int articlesScrapedLastTime = 0;
                if (previousMetaData != null)
                {
                    articlesScrapedLastTime = previousMetaData.ArticlesScraped;
                }
                metaData.LastScraped = CurrentTime;
                metaData.ArticlesScraped = context.NewsArticles.Count() - articlesScrapedLastTime;
                metaData.TimeTaken = DateTime.Now.ToUniversalTime() - CurrentTime;
                metaData.TotalArticles = context.NewsArticles.Count();
                context.MetaData.Add(metaData);
                context.SaveChanges();
            }
            _logger.LogInformation($"WebScraping finished: {DateTime.UtcNow}");
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}

public class TengriArticle
{
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Time { get; set; }
    public string? Content { get; set; }
    public string? Author { get; set; }
    public string[]? Tags { get; set; }
}

public class WebScraperService
{
    public DateTime ParseCustomDate(string dateStr)
    {
        //clean up the trailing and leading whitespace
        dateStr = dateStr.Trim();
        var russianMonths = new Dictionary<string, string>
        {
            {"января", "January"},
            {"февраля", "February"},
            {"марта", "March"},
            {"апреля", "April"},
            {"мая", "May"},
            {"июня", "June"},
            {"июля", "July"},
            {"августа", "August"},
            {"сентября", "September"},
            {"октября", "October"},
            {"ноября", "November"},
            {"декабря", "December"}
        };

        if (dateStr.StartsWith("Вчера"))
        {
            var timeStr = dateStr.Substring(6);
            var time = TimeSpan.Parse(timeStr);
            return DateTime.Today.Subtract(TimeSpan.FromDays(1)).Add(time).AddHours(-5);
        }
        else if (dateStr.StartsWith("Сегодня"))
        {
            var timeStr = dateStr.Substring(8); 
            var time = TimeSpan.Parse(timeStr);
            return DateTime.Today.Add(time).AddHours(-5);
        }
        else
        {
            var dateParts = dateStr.Split(' ');
            var day = int.Parse(dateParts[0]);
            var month = russianMonths.ContainsKey(dateParts[1]) ? russianMonths[dateParts[1]] : dateParts[1];
            var year = dateParts.Length > 2 ? int.Parse(dateParts[2].Substring(0, 4)) : DateTime.Today.Year; // Remove the comma
            
            var timeStr = dateParts.Length > 3 ? dateParts[3] : "00:00:00";
            var time = TimeSpan.Parse(timeStr);
            return new DateTime(year, DateTime.ParseExact(month, "MMMM", CultureInfo.InvariantCulture).Month, day).Add(time).AddHours(-5);
        }
    }
    public void ScrapeWebsite(Uri uri, string cat, ApplicationDbContext context)
    {
        var web = new HtmlWeb();
        var doc = web.Load(uri + cat);
        var articles = new List<TengriArticle>();
        
        var nodes = doc.DocumentNode.SelectNodes("//div[@class='content_main_item']");
        foreach (var node in nodes)
        {
            var article = new TengriArticle();
            
            //select node from div with class content_main_item_title
            var titleNode = node.SelectSingleNode(".//span[@class='content_main_item_title']");
            var announceNode = node.SelectSingleNode(".//span[@class='content_main_item_announce']");
            var metaNode = node.SelectSingleNode(".//div[@class='content_main_item_meta']");
            
            //article title is the text of the first a tag in the titleNode
            article.Title = titleNode.SelectSingleNode(".//a").InnerText;
            article.Url = titleNode.SelectSingleNode(".//a").GetAttributeValue("href", "");
            
            article.Subtitle = announceNode.InnerText;
            
            article.Author = "Tengri News";
            
            //check if such an article exists in the database
            if (context.NewsArticles.Any(a => a.Title == article.Title))
            {
                continue;
            }
            
            //load the article from the URL
            var articleDoc = web.Load(uri + article.Url);
            
            //get the article time from the first div with class date-time
            var timeNode = articleDoc.DocumentNode.SelectSingleNode("//div[@class='date-time']");
            article.Time = timeNode.InnerText;
            
            
            //get the author from the first div with class content_main_meta_author_item_name
            var authorNode = articleDoc.DocumentNode.SelectSingleNode("//span[@class='content_main_meta_author_item_name']");
            //get the author name from the text of the first a tag in the authorNode
            if (authorNode != null)
            {
                article.Author = authorNode.SelectSingleNode(".//a").InnerText;
            }
            else
            {
                article.Author = "Tengri News";
            }
            
            //get the image URL from the first img in the picture element with class content_main_thumb_img
            var imageNode = articleDoc.DocumentNode.SelectSingleNode("//picture[@class='content_main_thumb_img']");
            if (imageNode != null)
            {
                article.ImageUrl = imageNode.SelectSingleNode(".//img").GetAttributeValue("src", "");
            }
            else
            {
                article.ImageUrl = "https://madi-wka.xyz/img/nuclearpong.png";
            }
            
            //get the node of the div with class content_main_text_tags
            var tagsNode = articleDoc.DocumentNode.SelectSingleNode("//div[@class='content_main_text_tags']");
            //loop over each span in the tagsNode and add the text to the tags list
            if (tagsNode != null && tagsNode.SelectNodes(".//span") != null)
            {
                var tags = new List<string>();
                foreach (var tagNode in tagsNode.SelectNodes(".//span"))
                {
                    tags.Add(tagNode.InnerText);
                }
                article.Tags = tags.ToArray();
            }
            
            //get the contents of the content_main_text div
            var contentNode = articleDoc.DocumentNode.SelectSingleNode("//div[@class='content_main_text']");
            //convert the contentNode to an HTML string
            article.Content = contentNode.InnerHtml;
            
            
            
            
            articles.Add(article);
        }
        
        
        //convert the articles into NewsArticle entities
        var newsArticles = new List<NewsArticle>();
        foreach (var article in articles)
        {
            if (article.Title == null) continue;
            if (article is not { Author: not null, Time: not null }) continue;
            if (article is not { Content: not null, ImageUrl: not null }) continue;
            var tags = new List<Tag>();
            if (article.Tags != null)
            {
                foreach (var name in article.Tags)
                {
                    var tag = context.Tags.FirstOrDefault(t => t.Name == name);
                    if (tag == null)
                    {
                        if (context.Tags.Local.Any(t => t.Name == name))
                        {
                            tag = context.Tags.Local.First(t => t.Name == name);
                        }
                        else
                        {
                            tag = new Tag { Name = name };
                            context.Tags.Add(tag);
                        }
                    }
                    tags.Add(tag);
                }
            }

            var newsArticle = new NewsArticle
            {
                Title = article.Title,
                Author = article.Author,
                Date = ParseCustomDate(article.Time).ToUniversalTime(),
                Content = article.Content,
                ImageUrl = new Uri(uri + article.ImageUrl),
                Category = string.Concat(cat.Substring(0, 1).ToUpper(), cat.AsSpan(1)),
                Subtitle = article.Subtitle,
                Tags = tags,
            };
            newsArticles.Add(newsArticle);
        }
//add the newsArticles to the database
        context.NewsArticles.AddRange(newsArticles);
        context.SaveChanges();
    }

}