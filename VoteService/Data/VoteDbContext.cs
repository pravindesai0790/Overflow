using Microsoft.EntityFrameworkCore;
using VoteService.Models;

namespace VoteService.Data;

public class VoteDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Vote> Votes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vote>(x =>
        {
            // this will guarantee that one user one vote policy
            x.HasIndex(v => new { v.UserId, v.TargetType, v.TargetId }).IsUnique();
        });
    }
}