using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using TengrehNoos.Data;
using TengrehNoos.Entities;
using TengrehNoos.Models;

namespace TengrehNoos.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    
    
    public async Task<IActionResult> Index()
    {
        var page = HttpContext.Request.Query["p"].ToString();
        int pageNumber = 1;
        if (!int.TryParse(page, out pageNumber) || pageNumber < 1)
        {
            pageNumber = 1;
        }

        int skip = (pageNumber - 1) * 5;
        
        var query = HttpContext.Request.Query["query"].ToString();
        var selectedTags = HttpContext.Request.Query.Keys.Where(k => Request.Query[k] == "true").ToArray();
        var sort = HttpContext.Request.Query["sort"].ToString();
        var category = HttpContext.Request.Query["category"].ToString();
        var newsArticles = new System.Collections.Generic.List<NewsArticle>();
        var allCategories = _context.NewsArticles.Select(n => n.Category).Distinct().ToList();
        var tagsByCount = await _context.Tags
            .Include(t => t.NewsArticles)
            .Select(t => new TagsWithCountModel
            {
                Tag = t,
                Count = t.NewsArticles.Count,
                IsSelected = selectedTags.Contains(t.Name),
            })
            .OrderByDescending(t => t.Count)
            .ToListAsync();
        if (string.IsNullOrEmpty(sort))
        {
            sort = "latest";
        }

        if (selectedTags.Length > 0)
        {
            newsArticles = await _context.Tags
                .Where(t => selectedTags.Contains(t.Name))
                .SelectMany(t => t.NewsArticles).Where(n => n.Title.Contains(query) || n.Content.Contains(query) || query == "")
                .Where(n => n.Category == category || category == "")
                .Include(newsArticle => newsArticle.Tags)
                .Distinct().ToListAsync();
        }
        else
        {
            newsArticles = await _context.NewsArticles.Where(n => n.Title.Contains(query) || n.Content.Contains(query) || query == "")
                .Where(n => n.Category == category || category == "")
                .Include(newsArticle => newsArticle.Tags).ToListAsync();
        }
        int numberOfArticles = newsArticles.Count;
        if (sort == "latest")
        {
            newsArticles = newsArticles.OrderByDescending(n => n.Date).Skip(skip).Take(5).ToList();
        }
        else if (sort == "oldest")
        {
            newsArticles = newsArticles.OrderBy(n => n.Date).Skip(skip).Take(5).ToList();
        }
        var newsarticlelist = new List<NewsArticlePreviewModel>();
        foreach (var newsarticle in newsArticles)
        {
            newsarticlelist.Add(new NewsArticlePreviewModel
            {
                Id = newsarticle.Id,
                Title = newsarticle.Title,
                Author = newsarticle.Author,
                Date = newsarticle.Date,
                ImageUrl = newsarticle.ImageUrl,
                Category = newsarticle.Category,
                Tags = newsarticle.Tags.Select(t => t.Name).ToList(),
                Subtitle = newsarticle.Subtitle,
            });
        }
        MainPageModel output = new MainPageModel
        {
            NewsArticles = newsarticlelist,
            Tags = tagsByCount,
            Categories = allCategories,
            Pages = numberOfArticles / 5,
        };
        ViewData["CurrentSort"] = sort;
        if (query != "")
        {
            ViewData["CurrentQuery"] = query;
        }
        if (category != "")
        {
            ViewData["CurrentCategory"] = category;
        }
        if (pageNumber > 1)
        {
            ViewData["CurrentPage"] = pageNumber;
        }
        var lastUpdated = _context.MetaData.FirstOrDefault();
        if (lastUpdated != null)
        {
            ViewData["LastUpdated"] = lastUpdated.LastScraped.ToString("dd/MM/yyyy HH:mm:ss");
        }
        return View(output);
    }

    [Route ("Article/{id}")]
    public IActionResult Article(int id)
    {
        var newsArticle = _context.NewsArticles
            .Include(n => n.Tags)
            .FirstOrDefault(n => n.Id == id);
        if (newsArticle == null)
        {
            return NotFound();
        }
        var newsarticle = new NewsArticleModel
        {
            Id = newsArticle.Id,
            Title = newsArticle.Title,
            Author = newsArticle.Author,
            Date = newsArticle.Date,
            Content = newsArticle.Content,
            ImageUrl = newsArticle.ImageUrl,
            Category = newsArticle.Category,
            Tags = newsArticle.Tags.Select(t => t.Name).ToList(),
            Subtitle = newsArticle.Subtitle,
        };
        var currentArticleTags = newsArticle.Tags.Select(t => t.Name).ToList();
        var relatedArticles = _context.NewsArticles
            .Where(n => n.Tags.Any(t => currentArticleTags.Contains(t.Name)) && n.Id != newsArticle.Id)
            .OrderByDescending(n => n.Date)
            .Take(5)
            .ToList();
        if (relatedArticles.Count < 5)
        {
            var relatedArticleIds = relatedArticles.Select(a => a.Id).ToList();
            var moreArticles = _context.NewsArticles
                .Where(n => n.Category == newsArticle.Category && !relatedArticleIds.Contains(n.Id) && n.Id != newsArticle.Id)
                .OrderBy(n => Guid.NewGuid()) 
                .Take(5 - relatedArticles.Count)
                .ToList();

            relatedArticles.AddRange(moreArticles);
        }
        NewsArticlesAndRelatedModel output = new NewsArticlesAndRelatedModel
        {
            NewsArticle = newsarticle,
            RelatedArticles = relatedArticles.Select(n => new NewsArticlePreviewModel
            {
                Id = n.Id,
                Title = n.Title,
                Author = n.Author,
                Date = n.Date,
                ImageUrl = n.ImageUrl,
                Category = n.Category,
                Tags = n.Tags.Select(t => t.Name).ToList(),
                Subtitle = n.Subtitle,
            }).ToList(),
        };
        return View(output);
    }
    
    [Route("Scraper/{key}")]
    public void Scraper(string key)
    {
        if (key == "" || key == null)
        {
            key = "read";
        }
        var scraper = new WebScraperService();
        scraper.ScrapeWebsite(new Uri("https://tengrinews.kz/"), key, _context);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}