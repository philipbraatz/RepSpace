using Microsoft.EntityFrameworkCore;

namespace Data;

public class RepSpaceDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<ReputationLog> ReputationLogs { get; set; }
    public DbSet<Community> Communities { get; set; }
    public DbSet<Vote> Votes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("YourConnectionStringHere");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Define relationships and entity configurations
        modelBuilder.Entity<User>().HasKey(u => u.UniqueId);
        modelBuilder.Entity<UserProfile>().HasOne(u => u.User).WithOne().HasForeignKey<UserProfile>(u => u.UserId);
        // Add more configurations as needed
    }
}
