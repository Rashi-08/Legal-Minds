using Microsoft.EntityFrameworkCore;
using LegalMinds.Backend.Models;

namespace LegalMinds.Backend.Database
{
    public class LegalMindsDbContext : DbContext
    {
        public LegalMindsDbContext(DbContextOptions<LegalMindsDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Case> Cases { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
