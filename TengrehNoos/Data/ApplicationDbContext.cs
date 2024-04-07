using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TengrehNoos.Entities;

namespace TengrehNoos.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<NewsArticle> NewsArticles { get; set; }
    public DbSet<Tag> Tags { get; set; }
    
    public DbSet<MetaData> MetaData { get; set; }
}