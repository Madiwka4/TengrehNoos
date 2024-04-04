namespace TengrehNoos.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public virtual List<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();
}