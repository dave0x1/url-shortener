using Microsoft.EntityFrameworkCore;

class UrlDb : DbContext
{
    public UrlDb(DbContextOptions<UrlDb> options)
        : base (options) { }

    public DbSet<Url> Urls => Set<Url>();
}