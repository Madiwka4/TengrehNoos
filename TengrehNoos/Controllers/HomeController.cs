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
        var query = HttpContext.Request.Query["query"].ToString();
        var selectedTags = HttpContext.Request.Query.Keys.Where(k => Request.Query[k] == "true").ToArray();
        var sort = HttpContext.Request.Query["sort"].ToString();
        var newsArticles = new System.Collections.Generic.List<NewsArticle>();
        var tagsByCount = _context.Tags
            .Select(t => new TagsWithCountModel
            {
                Tag = t,
                Count = t.NewsArticles.Count,
                IsSelected = selectedTags.Contains(t.Name),
            })
            .OrderByDescending(t => t.Count)
            .ToList();
        if (string.IsNullOrEmpty(sort))
        {
            sort = "latest";
        }

        if (selectedTags.Length > 0)
        {
            newsArticles = await _context.Tags
                .Where(t => selectedTags.Contains(t.Name))
                .SelectMany(t => t.NewsArticles).Where(n => n.Title.Contains(query) || n.Content.Contains(query) || query == "")
                .Include(newsArticle => newsArticle.Tags)
                .Distinct().ToListAsync();
        }
        else
        {
            newsArticles = await _context.NewsArticles.Where(n => n.Title.Contains(query) || n.Content.Contains(query) || query == "").Include(newsArticle => newsArticle.Tags).ToListAsync();
        }
        if (sort == "latest")
        {
            newsArticles = newsArticles.OrderByDescending(n => n.Date).Take(15).ToList();
        }
        else if (sort == "oldest")
        {
            newsArticles = newsArticles.OrderBy(n => n.Date).Take(15).ToList();
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
        };
        ViewData["CurrentSort"] = sort;
        if (query != "")
        {
            ViewData["CurrentQuery"] = query;
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
        var relatedArticles = _context.NewsArticles
            .Where(n => n.Category == newsArticle.Category && n.Id != newsArticle.Id)
            .OrderByDescending(n => n.Date)
            .Take(3)
            .ToList();
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
    
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}