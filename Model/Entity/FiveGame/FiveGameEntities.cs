using Tool.Model.Entity.FiveGame;
using Microsoft.EntityFrameworkCore;

namespace Tool.Model.Entity.MySQL;

public class FiveGameEntities : DbContext
{
    public FiveGameEntities()
    {
    }

    public FiveGameEntities(DbContextOptions<FiveGameEntities> options) : base(options)
    {
    }

    public virtual DbSet<Member> Member { get; set; }
    public virtual DbSet<MemberCleaningBackup> MemberCleaningBackup { get; set; }
    public virtual DbSet<MemberTransferLog> MemberTransferLog { get; set; }
    public virtual DbSet<MemberSession> MemberSession { get; set; }
    public virtual DbSet<Operator> Operator { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

    }
}