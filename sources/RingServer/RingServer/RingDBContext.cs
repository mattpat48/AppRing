using Microsoft.EntityFrameworkCore;

public class RingDBContext : DbContext
{
    private string connectionString;
    public RingDBContext(DbContextOptions <RingDBContext> options, string connectionString) : base(options)
    {
        this.connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder.UseSqlServer());
    }

    public DbSet<User> Users { get; set; }
}

public class User
{
    public string phoneNumber { get; set; }
    public string deviceId { get; set; }
    public string verificationCode { get; set; }
    public DateTime codeExpiration { get; set; }
    public DateTime lastLogin { get; set; }
    public string publicKey { get; set; }
}
