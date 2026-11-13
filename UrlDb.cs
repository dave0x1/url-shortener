using Microsoft.EntityFrameworkCore;

class UrlDb : DbContext
{
    public UrlDb(DbContextOptions<UrlDb> options)
        : base (options) { }

    public DbSet<Url> Urls => Set<Url>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Url>().HasIndex(u => u.Code).IsUnique();
        b.Entity<Url>().HasIndex(u => u.Address).IsUnique();
    }
}