using EchoPBX.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace EchoPBX.Data;

public class EchoDbContext : DbContext
{
    public DbSet<AccessToken> AccessTokens { get; set; }

    public DbSet<Admin> Admins { get; set; }

    public DbSet<Cdr> Cdr { get; set; }

    public DbSet<Contact> Contacts { get; set; }

    public DbSet<Extension> Extensions { get; set; }

    public DbSet<Queue> Queues { get; set; }

    public DbSet<SystemSetting> SystemSettings { get; set; }

    public DbSet<Trunk> Trunks { get; set; }

    public DbSet<DtmfMenuEntry> DtmfMenuEntries { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = Path.Combine(Constants.DataDirectory, "echopbx.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Extension>().Property(x => x.ExtensionNumber).ValueGeneratedNever();
        modelBuilder.Entity<Extension>().HasOne(x => x.OutgoingTrunk).WithMany().HasForeignKey(x => x.OutgoingTrunkId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Extension>().HasMany(x => x.Queues).WithOne(x => x.Extension).HasForeignKey(x => x.ExtensionNumber);

        modelBuilder.Entity<Queue>().HasMany(x => x.Extensions).WithOne(x => x.Queue).HasForeignKey(x => x.QueueId);
        modelBuilder.Entity<QueueExtension>().HasKey(x => new { x.QueueId, x.ExtensionNumber });

        modelBuilder.Entity<Trunk>().Property(x => x.IncomingCallBehaviour).HasDefaultValueSql($"{(int)IncomingCallBehaviour.RingAllExtensions}").HasSentinel(0);
        modelBuilder.Entity<Trunk>().HasOne(x => x.Queue).WithMany().HasForeignKey(x => x.QueueId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Trunk>().HasMany(x => x.Extensions).WithMany().UsingEntity<TrunkExtension>(
            j => j.HasOne(te => te.Extension).WithMany().HasForeignKey(te => te.ExtensionNumber),
            j => j.HasOne(te => te.Trunk).WithMany().HasForeignKey(te => te.TrunkId),
            j => j.HasKey(t => new { t.TrunkId, t.ExtensionNumber })
        );

        modelBuilder.Entity<DtmfMenuEntry>().HasKey(x => new { x.TrunkId, x.Digit });
        modelBuilder.Entity<DtmfMenuEntry>()
            .HasOne(x => x.Trunk)
            .WithMany(x => x.DtmfMenuEntries)
            .HasForeignKey(x => x.TrunkId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DtmfMenuEntry>()
            .HasOne(x => x.Queue)
            .WithMany()
            .HasForeignKey(x => x.QueueId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SystemSetting>().HasKey(x => x.Name);
        modelBuilder.Entity<SystemSetting>().HasData([
            new SystemSetting { Name = "DashboardLanguage", Value = "en" },
            new SystemSetting { Name = "AsteriskLanguage", Value = "en" },
        ]);
        modelBuilder.Entity<Admin>().HasAlternateKey(x => x.Username);
        modelBuilder.Entity<AccessToken>().HasOne(x => x.Admin).WithMany().HasForeignKey(x => x.AdminId).OnDelete(DeleteBehavior.Cascade);
    }
}