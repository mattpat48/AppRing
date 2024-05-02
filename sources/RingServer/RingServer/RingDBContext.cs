using Microsoft.EntityFrameworkCore;

public class RingDBContext : DbContext
{
    public RingDBContext(DbContextOptions<RingDBContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(u => new { u.phoneNumber, u.deviceId });
        modelBuilder.Entity<Gate>().HasKey(g => g.gateId);
        modelBuilder.Entity<UserGate>().HasKey(ug => ug.usergateId);
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Gate> Gates { get; set; }
    public DbSet<UserGate> UsersGates { get; set; }
}

public class User
{
    public string deviceId { get; set; }
    public string phoneNumber { get; set; }
    public string publicKey { get; set; }
    public string verificationCode { get; set; }
    public DateTime verificationExpire { get; set; }
    public DateTime lastLogin { get; set; }
    public string rememberLogin { get; set; }
}

public class Gate
{
    public string gateId { get; set; }
    public string name { get; set; }
}

public class UserGate
{
    public string usergateId { get; set; }
    public string deviceId { get; set; }
    public string phoneNumber { get; set; }
    public string gateId { get; set; }
}