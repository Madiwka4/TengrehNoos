using TengrehNoos.Entities;

namespace TengrehNoos.Models;

public class NewsArticleModel
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string Author { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    
    public string Content { get; set; } = string.Empty;
    public Uri ImageUrl { get; set; } = new Uri("https://madi-wka.xyz/img/nuclearpong.png");
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new List<string>();
    public string? Subtitle { get; set; }
}

public class NewsArticlePreviewModel
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string Author { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    
    public string? Subtitle { get; set; }
    
    public Uri ImageUrl { get; set; } = new Uri("https://madi-wka.xyz/img/nuclearpong.png");
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new List<string>();
}

public class NewsArticlesAndRelatedModel
{
    public NewsArticleModel NewsArticle { get; set; }
    public List<NewsArticlePreviewModel> RelatedArticles { get; set; } = new List<NewsArticlePreviewModel>();
}

public class TagsWithCountModel
{
    public Tag Tag { get; set; } = new Tag();
    public int Count { get; set; }
    public bool IsSelected { get; set; }
}

public class MainPageModel
{
    public List<NewsArticlePreviewModel> NewsArticles { get; set; } = new List<NewsArticlePreviewModel>();
    public List<TagsWithCountModel> Tags { get; set; } = new List<TagsWithCountModel>();
    
    public List<string> Categories { get; set; } = new List<string>();
}