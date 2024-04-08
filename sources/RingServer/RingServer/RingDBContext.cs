using Microsoft.EntityFrameworkCore;

public class RingDBContext : DbContext
{
    public RingDBContext(DbContextOptions<RingDBContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(u => new { u.phoneNumber, u.deviceId });
    }

    public DbSet<User> Users { get; set; }
}

public class User
{
    public string phoneNumber { get; set; }
    public string deviceId { get; set; }
    public string verificationCode { get; set; }
    public DateTime verificationExpire { get; set; }
    public DateTime lastLogin { get; set; }
    public string publicKey { get; set; }
}
