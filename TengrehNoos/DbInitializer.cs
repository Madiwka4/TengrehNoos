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

        var testArticle = new NewsArticle
        {
            Title = "Test Article",
            Author = "Test Author",
            Date = DateTime.Now,
            Content = "This is a test article.",
            ImageUrl = new Uri("https://example.com/test.jpg"),
            Category = "News",
            Tags = new List<string> { "Test", "Article" }
        };

        context.NewsArticles.Add(testArticle);
        context.SaveChanges();
    }
}