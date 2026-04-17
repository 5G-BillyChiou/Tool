using Microsoft.EntityFrameworkCore;

namespace Tool.Model.Entity.FiveGameTrans;

public class FiveGameTransEntities : DbContext
{
    public FiveGameTransEntities()
    {
    }

    public FiveGameTransEntities(DbContextOptions<FiveGameTransEntities> options) : base(options)
    {
    }

    public virtual DbSet<Accounting> Accounting { get; set; }
    public virtual DbSet<AccountingBonus> AccountingBonus { get; set; }
    public virtual DbSet<Ledger> Ledger { get; set; }
    public virtual DbSet<PreAccountingResult> PreAccountingResult { get; set; }
    public virtual DbSet<MemberWallet> MemberWallet { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}