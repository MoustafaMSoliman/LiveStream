using LiveStream.DOMAIN;
using Microsoft.EntityFrameworkCore;

namespace LiveStream.INFRASTRUCTURE.Configuration;

public class LiveStreamContext : DbContext
{
    public DbSet<Device> Devices { get; set; }  
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlServer();
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
