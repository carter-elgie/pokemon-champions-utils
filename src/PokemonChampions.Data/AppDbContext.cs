using Microsoft.EntityFrameworkCore;
using PokemonChampions.Data.Entities;

namespace PokemonChampions.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PokemonEntity> Pokemon => Set<PokemonEntity>();
    public DbSet<MoveEntity> Moves => Set<MoveEntity>();
    public DbSet<ItemEntity> Items => Set<ItemEntity>();
    public DbSet<AbilityEntity> Abilities => Set<AbilityEntity>();
    public DbSet<LearnsetEntity> Learnsets => Set<LearnsetEntity>();
    public DbSet<UsageStatsEntity> UsageStats => Set<UsageStatsEntity>();
    public DbSet<UsageMoveEntity> UsageMoves => Set<UsageMoveEntity>();
    public DbSet<UsageItemEntity> UsageItems => Set<UsageItemEntity>();
    public DbSet<UsageAbilityEntity> UsageAbilities => Set<UsageAbilityEntity>();
    public DbSet<UsageSpreadEntity> UsageSpreads => Set<UsageSpreadEntity>();
    public DbSet<TeamEntity> Teams => Set<TeamEntity>();
    public DbSet<TeamMemberEntity> TeamMembers => Set<TeamMemberEntity>();
    public DbSet<AliasEntity> Aliases => Set<AliasEntity>();
    public DbSet<AppSettingEntity> AppSettings => Set<AppSettingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Pokemon: unique index on ShowdownId (case-insensitive via SQLite collation)
        modelBuilder.Entity<PokemonEntity>()
            .HasIndex(p => p.ShowdownId)
            .IsUnique();
        modelBuilder.Entity<PokemonEntity>()
            .HasIndex(p => p.NormalizedId);
        modelBuilder.Entity<PokemonEntity>()
            .Property(p => p.ShowdownId)
            .UseCollation("NOCASE");

        // Move
        modelBuilder.Entity<MoveEntity>()
            .HasIndex(m => m.ShowdownId)
            .IsUnique();
        modelBuilder.Entity<MoveEntity>()
            .HasIndex(m => m.NormalizedId);

        // Item
        modelBuilder.Entity<ItemEntity>()
            .HasIndex(i => i.ShowdownId)
            .IsUnique();
        modelBuilder.Entity<ItemEntity>()
            .HasIndex(i => i.NormalizedId);

        // Ability
        modelBuilder.Entity<AbilityEntity>()
            .HasIndex(a => a.ShowdownId)
            .IsUnique();
        modelBuilder.Entity<AbilityEntity>()
            .HasIndex(a => a.NormalizedId);

        // Learnset: unique per (pokemon, move, generation, method)
        modelBuilder.Entity<LearnsetEntity>()
            .HasIndex(l => new { l.PokemonId, l.MoveId, l.Generation, l.Method })
            .IsUnique();

        // UsageStats: unique per (pokemon, format, month, source)
        modelBuilder.Entity<UsageStatsEntity>()
            .HasIndex(u => new { u.PokemonId, u.FormatShowdownId, u.StatsMonth, u.Source })
            .IsUnique();

        // Team: unique name (case-insensitive)
        modelBuilder.Entity<TeamEntity>()
            .HasIndex(t => t.Name)
            .IsUnique();
        modelBuilder.Entity<TeamEntity>()
            .Property(t => t.Name)
            .UseCollation("NOCASE");

        // TeamMember: cascade delete with team
        modelBuilder.Entity<TeamMemberEntity>()
            .HasOne(m => m.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // Alias: unique alias text (case-insensitive)
        modelBuilder.Entity<AliasEntity>()
            .HasIndex(a => a.AliasText)
            .IsUnique();
        modelBuilder.Entity<AliasEntity>()
            .Property(a => a.AliasText)
            .UseCollation("NOCASE");
    }
}
