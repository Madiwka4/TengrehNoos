using System.ComponentModel.DataAnnotations;

namespace TengrehNoos.Entities;

public class NewsArticle
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Author { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Subtitle { get; set; }
    public Uri ImageUrl { get; set; } = new Uri("https://madi-wka.xyz/img/nuclearpong.png");
    public string Category { get; set; } = string.Empty;
    public virtual List<Tag> Tags { get; set; } = new List<Tag>();
}