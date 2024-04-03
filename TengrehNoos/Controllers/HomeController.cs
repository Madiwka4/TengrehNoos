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
        var sort = HttpContext.Request.Query["sort"].ToString();
        var newsArticles = new System.Collections.Generic.List<NewsArticle>();
        if (string.IsNullOrEmpty(sort))
        {
            sort = "latest";
        }
        if (sort == "latest")
        {
            newsArticles = await _context.NewsArticles
                .OrderByDescending(n => n.Date)
                .Take(15)
                .ToListAsync();
        }
        if (sort == "oldest")
        {
            newsArticles = await _context.NewsArticles
                .OrderBy(n => n.Date)
                .Take(15)
                .ToListAsync();
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
                Tags = newsarticle.Tags,
                Subtitle = newsarticle.Subtitle,
            });
        }

        ViewData["CurrentCategory"] = sort;
        return View(newsarticlelist);
    }

    [Route ("Article/{id}")]
    public IActionResult Article(int id)
    {
        var newsArticle = _context.NewsArticles.Find(id);
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
            Tags = newsArticle.Tags,
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
                Tags = n.Tags,
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