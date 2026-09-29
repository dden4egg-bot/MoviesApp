using Microsoft.EntityFrameworkCore;

namespace MoviesApp;

public class MoviesContext : DbContext
{
    public DbSet<Director> Directors { get; set; }
    public DbSet<Movie> Movies { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=movies.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Relationship: Director 1 --- * Movie
        modelBuilder.Entity<Movie>()
            .HasOne(m => m.Director)
            .WithMany(d => d.Movies)
            .HasForeignKey(m => m.DirectorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}