namespace TengrehNoos.Entities;

public class MetaData
{
    public int Id { get; set; }
    public DateTime LastScraped { get; set; }
    public int ArticlesScraped { get; set; }
    public TimeSpan TimeTaken { get; set; }
    
    public int TotalArticles { get; set; }
}