using System;
using System.Linq;
using TengrehNoos.Data;
using TengrehNoos.Entities;

public static class DbInitializer
{
    public static void SeedDatabase(ApplicationDbContext context)
    {
        // Check if there are any articles already
        if (context.NewsArticles.Any())
        {
            return; // DB has been seeded
        }

        var tagNames = new[] { "Test", "Article" };
        var tags = tagNames.Select(name => context.Tags.Local.FirstOrDefault(t => t.Name == name) ?? new Tag { Name = name }).ToList();
        context.Tags.AddRange(tags);
        var testArticle = new NewsArticle
        {
            Title = "Test Article",
            Author = "Test Author",
            Date = DateTime.UtcNow,
            Content = "This is a test article.",
            ImageUrl = new Uri("https://example.com/test.jpg"),
            Category = "News",
            Tags = tags,
        };
        
        var tagNamesS = new[] { "Second Test", "Article" };
        var tagsS = tagNamesS.Select(name => context.Tags.Local.FirstOrDefault(t => t.Name == name) ?? new Tag { Name = name }).ToList();
        context.Tags.AddRange(tagsS);
        var testArticleS = new NewsArticle
        {
            Title = "Test Article 2",
            Author = "Test Author",
            Date = DateTime.UtcNow,
            Content = "This is a test article.",
            ImageUrl = new Uri("https://example.com/test.jpg"),
            Category = "News",
            Tags = tagsS,
        };
        
        context.NewsArticles.Add(testArticle);
        context.NewsArticles.Add(testArticleS);
        context.SaveChanges();
    }
}