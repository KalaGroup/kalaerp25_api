using System;
using System.Collections.Generic;
using KalaGenset.ERP.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KalaGenset.ERP.Data.DbContexts;

public partial class KalaDbContext : DbContext
{
    public KalaDbContext()
    {
    }

    public KalaDbContext(DbContextOptions<KalaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActionTaken> ActionTakens { get; set; }

    public virtual DbSet<AuthorizationDetail> AuthorizationDetails { get; set; }

    public virtual DbSet<Bom> Boms { get; set; }

    public virtual DbSet<Bomdetail> Bomdetails { get; set; }

    public virtual DbSet<CalibrationMst> CalibrationMsts { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<DispatchInstruction> DispatchInstructions { get; set; }

    public virtual DbSet<DispatchInstructionDetail> DispatchInstructionDetails { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeType> EmployeeTypes { get; set; }

    public virtual DbSet<GatereceiptInternalDetailsSub> GatereceiptInternalDetailsSubs { get; set; }

    public virtual DbSet<GetMaxCode> GetMaxCodes { get; set; }

    public virtual DbSet<GiirdetailsSub> GiirdetailsSubs { get; set; }

    public virtual DbSet<JobCard> JobCards { get; set; }

    public virtual DbSet<JobCardDetail> JobCardDetails { get; set; }

    public virtual DbSet<JobCardDetailsSub> JobCardDetailsSubs { get; set; }

    public virtual DbSet<Jobcard2DetailsSub> Jobcard2DetailsSubs { get; set; }

    public virtual DbSet<LoginMst> LoginMsts { get; set; }

    public virtual DbSet<MemoExciseMfg> MemoExciseMfgs { get; set; }

    public virtual DbSet<Mof> Mofs { get; set; }

    public virtual DbSet<MtfdetailsSub> MtfdetailsSubs { get; set; }

    public virtual DbSet<PanelTypeKit> PanelTypeKits { get; set; }

    public virtual DbSet<Part> Parts { get; set; }

    public virtual DbSet<PartTocdetailsSupplier> PartTocdetailsSuppliers { get; set; }

    public virtual DbSet<PcstageWiseRate> PcstageWiseRates { get; set; }

    public virtual DbSet<PcstageWiseRateChange> PcstageWiseRateChanges { get; set; }

    public virtual DbSet<PrcChkDetail> PrcChkDetails { get; set; }

    public virtual DbSet<PrimaryCompAssign> PrimaryCompAssigns { get; set; }

    public virtual DbSet<PrimaryCompAssignDetail> PrimaryCompAssignDetails { get; set; }

    public virtual DbSet<ProcessFeedBack> ProcessFeedBacks { get; set; }

    public virtual DbSet<ProcessFeedbackDetailsSub> ProcessFeedbackDetailsSubs { get; set; }

    public virtual DbSet<ProfitCenter> ProfitCenters { get; set; }

    public virtual DbSet<ProfitCenterPldetail> ProfitCenterPldetails { get; set; }

    public virtual DbSet<ProfitCenterplDetailsChanged> ProfitCenterplDetailsChangeds { get; set; }

    public virtual DbSet<PurchaseCosting> PurchaseCostings { get; set; }

    public virtual DbSet<QualityProcessCheckDefectDg> QualityProcessCheckDefectDgs { get; set; }

    public virtual DbSet<QualityProcessCheckerDetailsDg> QualityProcessCheckerDetailsDgs { get; set; }

    public virtual DbSet<QualityProcessCheckerDg> QualityProcessCheckerDgs { get; set; }

    public virtual DbSet<SilCladdingRate> SilCladdingRates { get; set; }

    public virtual DbSet<StageWiseQualityCheckList> StageWiseQualityCheckLists { get; set; }

    public virtual DbSet<StageWiseQualityCheckListDetail> StageWiseQualityCheckListDetails { get; set; }

    public virtual DbSet<Stockwip> Stockwips { get; set; }

    public virtual DbSet<SupplierPriceListChanged> SupplierPriceListChangeds { get; set; }

    public virtual DbSet<TestReport> TestReports { get; set; }

    public virtual DbSet<Uom> Uoms { get; set; }

    public virtual DbSet<Video> Videos { get; set; }

    public virtual DbSet<YearEnd> YearEnds { get; set; }

    public virtual DbSet<_6m> _6ms { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=13.71.50.58;Database=ERP_NEW_System;Uid=sa;Pwd=3HMungiIMR;Encrypt=false;MultipleActiveResultSets=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActionTaken>(entity =>
        {
            entity.HasKey(e => e.Actno);

            entity.ToTable("ActionTaken");

            entity.Property(e => e.Actno)
                .HasMaxLength(50)
                .HasColumnName("ACTNo");
            entity.Property(e => e.ActionStatus)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.CommissioningDt).HasColumnType("datetime");
            entity.Property(e => e.CustAckDt).HasColumnType("datetime");
            entity.Property(e => e.CustAckName)
                .HasMaxLength(20)
                .HasDefaultValue("NIL");
            entity.Property(e => e.CustAckSign)
                .HasMaxLength(15)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Dghours).HasColumnName("DGHours");
            entity.Property(e => e.Discard).HasDefaultValue(true);
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.EngSrNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.FeedBackStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.GenSrNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Invstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("INVStatus");
            entity.Property(e => e.MailCc)
                .HasDefaultValue("NIL")
                .HasColumnName("MailCC");
            entity.Property(e => e.MailStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.MaxSrNo).HasMaxLength(50);
            entity.Property(e => e.NextDt).HasColumnType("datetime");
            entity.Property(e => e.Pcacode)
                .HasMaxLength(50)
                .HasColumnName("PCACode");
            entity.Property(e => e.QualityStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Remark).HasDefaultValue("NIL");
            entity.Property(e => e.SolvedDt).HasColumnType("datetime");
            entity.Property(e => e.Yr).HasMaxLength(50);
        });

        modelBuilder.Entity<AuthorizationDetail>(entity =>
        {
            entity.HasKey(e => e.Aid);

            entity.Property(e => e.Aid).HasColumnName("AID");
            entity.Property(e => e.AuthDateTime).HasColumnType("datetime");
            entity.Property(e => e.AuthForm).HasMaxLength(500);
            entity.Property(e => e.CompanyCode).HasMaxLength(50);
            entity.Property(e => e.EmpId)
                .HasMaxLength(50)
                .HasColumnName("EmpID");
        });

        modelBuilder.Entity<Bom>(entity =>
        {
            entity.HasKey(e => new { e.Bomcode, e.PartCode });

            entity.ToTable("BOM");

            entity.HasIndex(e => new { e.Bomcode, e.Yr, e.Discard, e.Active }, "_dta_index_BOM_12_980927312__K1_K4_K125_K126").HasFillFactor(80);

            entity.Property(e => e.Bomcode)
                .HasMaxLength(50)
                .HasColumnName("BOMCode");
            entity.Property(e => e.PartCode).HasMaxLength(50);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.AuthDt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CompanyCode).HasMaxLength(50);
            entity.Property(e => e.CpaccAmt).HasColumnName("CPAccAmt");
            entity.Property(e => e.Cpamt).HasColumnName("CPAmt");
            entity.Property(e => e.CpasblyAmt).HasColumnName("CPAsblyAmt");
            entity.Property(e => e.CpprocCost).HasColumnName("CPProcCost");
            entity.Property(e => e.Cpqty).HasColumnName("CPQty");
            entity.Property(e => e.CpsprocCost).HasColumnName("CPSProcCost");
            entity.Property(e => e.CpyaccAmt).HasColumnName("CPYAccAmt");
            entity.Property(e => e.CpyprocCost).HasColumnName("CPYProcCost");
            entity.Property(e => e.Cpyqty).HasColumnName("CPYQty");
            entity.Property(e => e.Cstamt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("CSTAmt");
            entity.Property(e => e.Cstper).HasColumnName("CSTper");
            entity.Property(e => e.DgasblyAmt).HasColumnName("DGAsblyAmt");
            entity.Property(e => e.Dgsamt).HasColumnName("DGSAmt");
            entity.Property(e => e.Discard).HasDefaultValue(true);
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.FoamCpySamt).HasColumnName("FoamCpySAmt");
            entity.Property(e => e.GeomSamt).HasColumnName("GeomSAmt");
            entity.Property(e => e.Ismcishbamt).HasColumnName("ISMCISHBAmt");
            entity.Property(e => e.Ismcishbwt).HasColumnName("ISMCISHBWt");
            entity.Property(e => e.Kgamt).HasColumnName("KGAmt");
            entity.Property(e => e.KgamtCp).HasColumnName("KGAmtCP");
            entity.Property(e => e.KgamtCps).HasColumnName("KGAmtCPS");
            entity.Property(e => e.KgamtCpy).HasColumnName("KGAmtCPY");
            entity.Property(e => e.KgamtTwr).HasColumnName("KGAmtTwr");
            entity.Property(e => e.KitType)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.MaxSrNo).HasMaxLength(50);
            entity.Property(e => e.NestingBfstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("NestingBFStatus");
            entity.Property(e => e.NestingBftstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("NestingBFTStatus");
            entity.Property(e => e.NestingCpsstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("NestingCPSStatus");
            entity.Property(e => e.NestingCpstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("NestingCPStatus");
            entity.Property(e => e.NestingCpystatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("NestingCPYStatus");
            entity.Property(e => e.NestingFtstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("NestingFTStatus");
            entity.Property(e => e.NestingLstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("NestingLStatus");
            entity.Property(e => e.NestingStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.NestingTwrStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.OverHeadPer).HasComment("Cost of service");
            entity.Property(e => e.Ppglamt).HasColumnName("PPGLAmt");
            entity.Property(e => e.Ppglwt).HasColumnName("PPGLWt");
            entity.Property(e => e.ProfitAmtCp).HasColumnName("ProfitAmtCP");
            entity.Property(e => e.Remark)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
            entity.Property(e => e.ScrapAmtCp).HasColumnName("ScrapAmtCP");
            entity.Property(e => e.ScrapAmtCps).HasColumnName("ScrapAmtCPS");
            entity.Property(e => e.ScrapAmtCpy).HasColumnName("ScrapAmtCPY");
            entity.Property(e => e.ScrapAmtCr).HasColumnName("ScrapAmtCR");
            entity.Property(e => e.ScrapAmtCrcp).HasColumnName("ScrapAmtCRCP");
            entity.Property(e => e.ScrapAmtCrcps).HasColumnName("ScrapAmtCRCPS");
            entity.Property(e => e.ScrapAmtCrcpy).HasColumnName("ScrapAmtCRCPY");
            entity.Property(e => e.ScrapAmtCrtwr).HasColumnName("ScrapAmtCRTwr");
            entity.Property(e => e.ScrapAmtGicpy).HasColumnName("ScrapAmtGICPY");
            entity.Property(e => e.ScrapAmtHr).HasColumnName("ScrapAmtHR");
            entity.Property(e => e.ScrapAmtHrcp).HasColumnName("ScrapAmtHRCP");
            entity.Property(e => e.ScrapAmtHrcps).HasColumnName("ScrapAmtHRCPS");
            entity.Property(e => e.ScrapAmtHrcpy).HasColumnName("ScrapAmtHRCPY");
            entity.Property(e => e.ScrapAmtHrtwr).HasColumnName("ScrapAmtHRTwr");
            entity.Property(e => e.ScrapWgtCp).HasColumnName("ScrapWgtCP");
            entity.Property(e => e.ScrapWgtCps).HasColumnName("ScrapWgtCPS");
            entity.Property(e => e.ScrapWgtCpy).HasColumnName("ScrapWgtCPY");
            entity.Property(e => e.ScrapWgtCr).HasColumnName("ScrapWgtCR");
            entity.Property(e => e.ScrapWgtCrcp).HasColumnName("ScrapWgtCRCP");
            entity.Property(e => e.ScrapWgtCrcps).HasColumnName("ScrapWgtCRCPS");
            entity.Property(e => e.ScrapWgtCrcpy).HasColumnName("ScrapWgtCRCPY");
            entity.Property(e => e.ScrapWgtCrtwr).HasColumnName("ScrapWgtCRTwr");
            entity.Property(e => e.ScrapWgtGi).HasColumnName("ScrapWgtGI");
            entity.Property(e => e.ScrapWgtGicpy).HasColumnName("ScrapWgtGICPY");
            entity.Property(e => e.ScrapWgtHr).HasColumnName("ScrapWgtHR");
            entity.Property(e => e.ScrapWgtHrcp).HasColumnName("ScrapWgtHRCP");
            entity.Property(e => e.ScrapWgtHrcps).HasColumnName("ScrapWgtHRCPS");
            entity.Property(e => e.ScrapWgtHrcpy).HasColumnName("ScrapWgtHRCPY");
            entity.Property(e => e.ScrapWgtHrtwr).HasColumnName("ScrapWgtHRTwr");
            entity.Property(e => e.ScrapperCp).HasColumnName("ScrapperCP");
            entity.Property(e => e.ScrapperCps).HasColumnName("ScrapperCPS");
            entity.Property(e => e.ScrapperCpy).HasColumnName("ScrapperCPY");
            entity.Property(e => e.SqFtAmtCp).HasColumnName("SqFtAmtCP");
            entity.Property(e => e.SqFtAmtCps).HasColumnName("SqFtAmtCPS");
            entity.Property(e => e.SqFtAmtCpy).HasColumnName("SqFtAmtCPY");
            entity.Property(e => e.SqftCp).HasColumnName("SqftCP");
            entity.Property(e => e.SqftCps).HasColumnName("SqftCPS");
            entity.Property(e => e.SqftCpy).HasColumnName("SqftCPY");
            entity.Property(e => e.Tcramt).HasColumnName("TCRAmt");
            entity.Property(e => e.TcramtCp).HasColumnName("TCRAmtCP");
            entity.Property(e => e.TcramtCps).HasColumnName("TCRAmtCPS");
            entity.Property(e => e.TcramtCpy).HasColumnName("TCRAmtCPY");
            entity.Property(e => e.TcramtTwr).HasColumnName("TCRAmtTwr");
            entity.Property(e => e.Tgiamt).HasColumnName("TGIAmt");
            entity.Property(e => e.TgiamtCpy).HasColumnName("TGIAmtCPY");
            entity.Property(e => e.Thramt).HasColumnName("THRAmt");
            entity.Property(e => e.ThramtCp).HasColumnName("THRAmtCP");
            entity.Property(e => e.ThramtCps).HasColumnName("THRAmtCPS");
            entity.Property(e => e.ThramtCpy).HasColumnName("THRAmtCPY");
            entity.Property(e => e.ThramtTwr).HasColumnName("THRAmtTwr");
            entity.Property(e => e.TmatCost)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TMatCost");
            entity.Property(e => e.TmatCostCp)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TMatCostCP");
            entity.Property(e => e.TmatCostCps)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TMatCostCPS");
            entity.Property(e => e.TmatCostCpy)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TMatCostCPY");
            entity.Property(e => e.TmatCostTwr)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TMatCostTwr");
            entity.Property(e => e.ToTprcCost).HasColumnName("ToTPrcCost");
            entity.Property(e => e.TotMatAmtCp).HasColumnName("TotMatAmtCP");
            entity.Property(e => e.TotMatAmtCps).HasColumnName("TotMatAmtCPS");
            entity.Property(e => e.TotMatAmtCpy).HasColumnName("TotMatAmtCPY");
            entity.Property(e => e.TotalWgtCp).HasColumnName("TotalWgtCP");
            entity.Property(e => e.TotalWgtCps).HasColumnName("TotalWgtCPS");
            entity.Property(e => e.TotalWgtCpy).HasColumnName("TotalWgtCPY");
            entity.Property(e => e.TprcCostKg)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TPrcCostKG");
            entity.Property(e => e.TprcCostKgcp)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TPrcCostKGCP");
            entity.Property(e => e.TprcCostKgcps)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TPrcCostKGCPS");
            entity.Property(e => e.TprcCostKgcpy)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TPrcCostKGCPY");
            entity.Property(e => e.TprcCostKgtwr)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TPrcCostKGTwr");
            entity.Property(e => e.TprcCostSqft)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TPrcCostSqft");
            entity.Property(e => e.TprcCostSqftCp)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TPrcCostSqftCP");
            entity.Property(e => e.TprcCostSqftCps)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TPrcCostSqftCPS");
            entity.Property(e => e.TprcCostSqftCpy)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TPrcCostSqftCPY");
            entity.Property(e => e.TprcCostSqftTwr)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TPrcCostSqftTwr");
            entity.Property(e => e.WgtCp).HasColumnName("WgtCP");
            entity.Property(e => e.WgtCps).HasColumnName("WgtCPS");
            entity.Property(e => e.WgtCpy).HasColumnName("WgtCPY");
            entity.Property(e => e.WgtCr).HasColumnName("WgtCR");
            entity.Property(e => e.WgtCrcp).HasColumnName("WgtCRCP");
            entity.Property(e => e.WgtCrcps).HasColumnName("WgtCRCPS");
            entity.Property(e => e.WgtCrcpy).HasColumnName("WgtCRCPY");
            entity.Property(e => e.WgtCrtwr).HasColumnName("WgtCRTwr");
            entity.Property(e => e.WgtGi).HasColumnName("WgtGI");
            entity.Property(e => e.WgtGicpy).HasColumnName("WgtGICPY");
            entity.Property(e => e.WgtHr).HasColumnName("WgtHR");
            entity.Property(e => e.WgtHrcp).HasColumnName("WgtHRCP");
            entity.Property(e => e.WgtHrcps).HasColumnName("WgtHRCPS");
            entity.Property(e => e.WgtHrcpy).HasColumnName("WgtHRCPY");
            entity.Property(e => e.WgtHrtwr).HasColumnName("WgtHRTwr");
            entity.Property(e => e.Yr)
                .HasMaxLength(50)
                .HasDefaultValue("10-11");
        });

        modelBuilder.Entity<Bomdetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("BOMDetails");

            entity.HasIndex(e => new { e.Kitcode, e.PartCode, e.Bomcode }, "_dta_index_BOMDetails_12_849450846__K26_K3_K1").HasFillFactor(80);

            entity.HasIndex(e => new { e.Kitcode, e.PartCode }, "_dta_index_BOMDetails_14_849450846__K26_K3").HasFillFactor(80);

            entity.HasIndex(e => new { e.PartCode, e.Kitcode }, "_dta_index_BOMDetails_14_849450846__K3_K26").HasFillFactor(80);

            entity.Property(e => e.Bomcode)
                .HasMaxLength(50)
                .HasColumnName("BOMCode");
            entity.Property(e => e.CategoryId)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("categoryID");
            entity.Property(e => e.Kgrate).HasColumnName("KGRate");
            entity.Property(e => e.Kitcode)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("KITCode");
            entity.Property(e => e.MatCost).HasDefaultValueSql("('0')");
            entity.Property(e => e.MatType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("NA")
                .IsFixedLength();
            entity.Property(e => e.Mob)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .IsFixedLength()
                .HasColumnName("MOB");
            entity.Property(e => e.NestingBfstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("NestingBFStatus");
            entity.Property(e => e.NestingBftstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("NestingBFTStatus");
            entity.Property(e => e.NestingCpystatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("NestingCPYStatus");
            entity.Property(e => e.NestingFtstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("NestingFTStatus");
            entity.Property(e => e.NestingLstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("NestingLStatus");
            entity.Property(e => e.NestingStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.PartCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.PkitCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PKitCode");
            entity.Property(e => e.PrcCostKg)
                .HasDefaultValueSql("('0')")
                .HasColumnName("PrcCostKG");
            entity.Property(e => e.PrcCostSqft).HasDefaultValueSql("('0')");
            entity.Property(e => e.PrcCostUnitKg).HasColumnName("PrcCostUnitKG");
            entity.Property(e => e.PrcWithRate).HasDefaultValueSql("((0))");
            entity.Property(e => e.RoomType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.SteelRate).HasDefaultValueSql("('0')");
            entity.Property(e => e.TotalPrcCostUnitKg).HasColumnName("TotalPrcCostUnitKG");
        });

        modelBuilder.Entity<CalibrationMst>(entity =>
        {
            entity.HasKey(e => e.InstrumentId).HasName("PK__Calibrat__430A5386E51EC584");

            entity.ToTable("Calibration_mst");

            entity.Property(e => e.CalDate).HasColumnType("smalldatetime");
            entity.Property(e => e.CheckerRemark)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.DueDate).HasColumnType("smalldatetime");
            entity.Property(e => e.IdNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Lc)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("LC");
            entity.Property(e => e.Location)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Make)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MakerRemark)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PartCode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Range)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SrNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Unit)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Cid);

            entity.ToTable("Company");

            entity.Property(e => e.Cid)
                .HasMaxLength(50)
                .HasColumnName("CID");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasComment("1 - Active 0 - INActive");
            entity.Property(e => e.Address1)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasDefaultValue("NA");
            entity.Property(e => e.Address2)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasDefaultValue("NA");
            entity.Property(e => e.Auth)
                .HasDefaultValue(true)
                .HasComment("1 - Authorize 0 - UnAuthorize");
            entity.Property(e => e.CaliseName)
                .HasMaxLength(200)
                .HasDefaultValue("NIL")
                .HasColumnName("CAliseName");
            entity.Property(e => e.Ccode)
                .HasMaxLength(50)
                .HasColumnName("CCode");
            entity.Property(e => e.Cdt)
                .HasColumnType("datetime")
                .HasColumnName("CDt");
            entity.Property(e => e.Cinno)
                .HasMaxLength(50)
                .HasDefaultValue("-")
                .HasColumnName("CINNo");
            entity.Property(e => e.CityId)
                .HasComment("CityName")
                .HasColumnName("CityID");
            entity.Property(e => e.Cname)
                .HasMaxLength(200)
                .HasColumnName("CName");
            entity.Property(e => e.CommRate)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.CountryId)
                .HasMaxLength(50)
                .HasComment("CountryName")
                .HasColumnName("CountryID");
            entity.Property(e => e.CpphNo)
                .HasMaxLength(50)
                .HasColumnName("CPPhNo");
            entity.Property(e => e.CsttinNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("CSTTinNo");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(50)
                .HasDefaultValue("01");
            entity.Property(e => e.DbtcertNo)
                .HasMaxLength(100)
                .HasDefaultValueSql("((0))")
                .HasColumnName("DBTCertNo");
            entity.Property(e => e.DesignationId)
                .HasMaxLength(50)
                .HasComment("DesignationName")
                .HasColumnName("DesignationID");
            entity.Property(e => e.Division)
                .HasMaxLength(255)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Eccno)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("ECCNo");
            entity.Property(e => e.ElectricityBoardName).HasDefaultValueSql("((0))");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("EMail");
            entity.Property(e => e.EntityType)
                .HasMaxLength(200)
                .HasDefaultValue("Mfg Plant");
            entity.Property(e => e.EstablishmentCode)
                .HasMaxLength(50)
                .HasDefaultValue("-");
            entity.Property(e => e.Fax)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.GradeId)
                .HasMaxLength(50)
                .HasColumnName("GradeID");
            entity.Property(e => e.GsttinNo)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("GSTTinNo");
            entity.Property(e => e.Hoadd)
                .HasMaxLength(300)
                .HasDefaultValue("NIL")
                .HasColumnName("HOAdd");
            entity.Property(e => e.Iecno)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("IECNo");
            entity.Property(e => e.LedgerId)
                .HasMaxLength(50)
                .HasColumnName("LedgerID");
            entity.Property(e => e.MeterNo)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Mobile)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.MsebconsumerNo)
                .HasMaxLength(50)
                .HasDefaultValue("NA")
                .HasColumnName("MSEBConsumerNo");
            entity.Property(e => e.OrganisationId)
                .HasMaxLength(50)
                .HasComment("Company Type")
                .HasColumnName("OrganisationID");
            entity.Property(e => e.Panno)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("PANNo");
            entity.Property(e => e.ParentCompany)
                .HasMaxLength(10)
                .HasDefaultValue("01");
            entity.Property(e => e.Ph1)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("PH1");
            entity.Property(e => e.Ph2)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("PH2");
            entity.Property(e => e.Range)
                .HasMaxLength(255)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Remark)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
            entity.Property(e => e.SancElecUom)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("SancElecUOM");
            entity.Property(e => e.SeedLicenseNo)
                .HasMaxLength(100)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.SrvTaxNo)
                .HasMaxLength(50)
                .HasDefaultValue("-");
            entity.Property(e => e.StateId)
                .HasComment("StateName")
                .HasColumnName("StateID");
            entity.Property(e => e.Tanno)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("TANNo");
            entity.Property(e => e.Url)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("URL");
            entity.Property(e => e.VattinNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("VATTinNo");
            entity.Property(e => e.Wadd)
                .HasMaxLength(300)
                .HasDefaultValue("NIL")
                .HasColumnName("WAdd");
        });

        modelBuilder.Entity<DispatchInstruction>(entity =>
        {
            entity.HasKey(e => e.Dino);

            entity.ToTable("DispatchInstruction");

            entity.HasIndex(e => new { e.ConsigneeCode, e.Dino, e.Mofcode, e.IndentorCode, e.Active }, "_dta_index_DispatchInstruction_15_1457661232__K10_K1_K8_K11_K57");

            entity.Property(e => e.Dino)
                .HasMaxLength(50)
                .HasColumnName("DINo");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Auth1).HasDefaultValue(true);
            entity.Property(e => e.AuthChkWh).HasColumnName("AuthChkWH");
            entity.Property(e => e.AuthRemark1)
                .HasMaxLength(600)
                .HasDefaultValue("Auto Auth");
            entity.Property(e => e.AuthRemark2)
                .HasMaxLength(2000)
                .HasDefaultValue("NIL");
            entity.Property(e => e.AuthRemarkChkWh)
                .HasMaxLength(600)
                .HasDefaultValue("NIL")
                .HasColumnName("AuthRemarkChkWH");
            entity.Property(e => e.BinvStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("BInvStatus");
            entity.Property(e => e.BranchCode).HasMaxLength(50);
            entity.Property(e => e.BranchCodeOld)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("BranchCode_OLD");
            entity.Property(e => e.CompanyCode).HasMaxLength(50);
            entity.Property(e => e.ConsigneeCode)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.CrLimitAuthRemark)
                .HasMaxLength(600)
                .HasDefaultValue("NIL");
            entity.Property(e => e.DeliveryTerms)
                .HasMaxLength(500)
                .HasDefaultValue("NIL");
            entity.Property(e => e.DescriptionManual)
                .HasMaxLength(1000)
                .HasDefaultValue("NIL");
            entity.Property(e => e.DidoneInvPenAmt).HasColumnName("DIDoneInvPenAmt");
            entity.Property(e => e.Dt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExportRemark).HasDefaultValue("NIL");
            entity.Property(e => e.IndentorCode)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Invstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("INVStatus");
            entity.Property(e => e.Lcdesc)
                .HasMaxLength(500)
                .HasDefaultValue("NIL")
                .HasColumnName("LCDesc");
            entity.Property(e => e.Lcdt)
                .HasColumnType("datetime")
                .HasColumnName("LCDt");
            entity.Property(e => e.LcexpiryDt)
                .HasColumnType("datetime")
                .HasColumnName("LCExpiryDt");
            entity.Property(e => e.Lcno)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("LCNo");
            entity.Property(e => e.Lctype)
                .HasMaxLength(50)
                .HasColumnName("LCType");
            entity.Property(e => e.LctypeDesc)
                .HasMaxLength(500)
                .HasDefaultValue("NIL")
                .HasColumnName("LCTypeDesc");
            entity.Property(e => e.MaxSrNo).HasMaxLength(50);
            entity.Property(e => e.Mofcode)
                .HasMaxLength(50)
                .HasColumnName("MOFCode");
            entity.Property(e => e.MofschId).HasColumnName("MOFSchID");
            entity.Property(e => e.NotificationAvailed).HasDefaultValue("NIL");
            entity.Property(e => e.NotificationDate).HasColumnType("datetime");
            entity.Property(e => e.NotificationNo)
                .HasMaxLength(50)
                .HasDefaultValue("0");
            entity.Property(e => e.OctriBy)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("K")
                .IsFixedLength();
            entity.Property(e => e.OnAcParty)
                .HasMaxLength(500)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Pdirstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PDIRStatus");
            entity.Property(e => e.PortOfDischarge)
                .HasMaxLength(500)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.PortOfLoading)
                .HasMaxLength(500)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Ppwcode)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("PPWCode");
            entity.Property(e => e.Pslstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PSLStatus");
            entity.Property(e => e.Rdiscard)
                .HasDefaultValue(true)
                .HasColumnName("RDiscard");
            entity.Property(e => e.Remark)
                .HasMaxLength(4000)
                .HasDefaultValue("NIL");
            entity.Property(e => e.ReverseRemark)
                .HasMaxLength(1000)
                .HasDefaultValue("NIL");
            entity.Property(e => e.TaxationBy)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("C")
                .IsFixedLength();
            entity.Property(e => e.TrallocationRemark)
                .HasMaxLength(200)
                .HasDefaultValue("NIL")
                .HasColumnName("TRAllocationRemark");
            entity.Property(e => e.TrallocationStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TRAllocationStatus");
            entity.Property(e => e.TransportBy)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("K")
                .IsFixedLength();
            entity.Property(e => e.TransportRoute)
                .HasMaxLength(500)
                .HasDefaultValue("NIL");
            entity.Property(e => e.VesselFlightNo)
                .HasMaxLength(100)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Wfdid)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("WFDID");
            entity.Property(e => e.Yr)
                .HasMaxLength(50)
                .HasDefaultValue("10-11");
        });

        modelBuilder.Entity<DispatchInstructionDetail>(entity =>
        {
            entity.HasKey(e => new { e.Dino, e.Rdgcode });

            entity.Property(e => e.Dino)
                .HasMaxLength(50)
                .HasColumnName("DINo");
            entity.Property(e => e.Rdgcode)
                .HasMaxLength(50)
                .HasColumnName("RDGCode");
            entity.Property(e => e.AllocDate).HasColumnType("datetime");
            entity.Property(e => e.InvStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Pdirstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PDIRStatus");
            entity.Property(e => e.Ppwcode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PPWCode");
            entity.Property(e => e.Psldstatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PSLDStatus");
            entity.Property(e => e.Rdgqty).HasColumnName("RDGQty");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Ecode);

            entity.ToTable("Employee");

            entity.Property(e => e.Ecode)
                .HasMaxLength(50)
                .HasColumnName("ECode");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.AdharCardNo)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Auth1Remark)
                .HasMaxLength(500)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Auth2Remark)
                .HasMaxLength(500)
                .HasDefaultValue("NIL");
            entity.Property(e => e.BankAccNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.BankAddress)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("NA");
            entity.Property(e => e.BankCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.BankIfsccode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("BankIFSCCode");
            entity.Property(e => e.BatchNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.BloodGroup).HasMaxLength(50);
            entity.Property(e => e.Cadd)
                .HasMaxLength(300)
                .HasDefaultValue("NIL")
                .HasColumnName("CAdd");
            entity.Property(e => e.Cca).HasColumnName("CCA");
            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.CmobileNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("CMobileNo");
            entity.Property(e => e.CmobilePlan)
                .HasMaxLength(50)
                .HasDefaultValue("NA")
                .HasColumnName("CMobilePlan");
            entity.Property(e => e.College)
                .HasMaxLength(150)
                .HasDefaultValue("NA");
            entity.Property(e => e.CompMailId)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("CompMailID");
            entity.Property(e => e.CompanyCode).HasMaxLength(50);
            entity.Property(e => e.CompanyCodeAct)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("CompanyCode_Act");
            entity.Property(e => e.CompanyCodeStd)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("CompanyCode_Std");
            entity.Property(e => e.Confirmdate).HasColumnType("datetime");
            entity.Property(e => e.ContractorCode).HasMaxLength(100);
            entity.Property(e => e.CountryId)
                .HasMaxLength(50)
                .HasColumnName("CountryID");
            entity.Property(e => e.CtcchangesStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("N")
                .IsFixedLength()
                .HasColumnName("CTCChangesStatus");
            entity.Property(e => e.Ctcpm).HasColumnName("CTCpm");
            entity.Property(e => e.CtcpmOman).HasColumnName("CTCpmOman");
            entity.Property(e => e.DepartmentHod)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("DepartmentHOD");
            entity.Property(e => e.DeptKitCode)
                .HasMaxLength(500)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.DeptName).HasMaxLength(1500);
            entity.Property(e => e.DeptNameN)
                .HasDefaultValueSql("((0))")
                .HasColumnName("DeptName_N");
            entity.Property(e => e.DeptOneId)
                .HasMaxLength(50)
                .HasColumnName("DeptOneID");
            entity.Property(e => e.DeptTwoId).HasColumnName("DeptTwoID");
            entity.Property(e => e.DesignationId)
                .HasMaxLength(50)
                .HasColumnName("DesignationID");
            entity.Property(e => e.DesignationIdAct)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("DesignationID_Act");
            entity.Property(e => e.DesignationIdStd)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("DesignationID_Std");
            entity.Property(e => e.DirectReporting)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Dob)
                .HasColumnType("datetime")
                .HasColumnName("DOB");
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.EmpConfirmStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("p")
                .IsFixedLength();
            entity.Property(e => e.EmpStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("p")
                .IsFixedLength();
            entity.Property(e => e.EmployeeType).HasMaxLength(50);
            entity.Property(e => e.EpayMode)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("EPayMode");
            entity.Property(e => e.Erplogin).HasColumnName("ERPLogin");
            entity.Property(e => e.EsicEmployee).HasColumnName("ESIcEmployee");
            entity.Property(e => e.EsicEmployer).HasColumnName("ESIcEmployer");
            entity.Property(e => e.Esino)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("ESINo");
            entity.Property(e => e.Experience)
                .HasMaxLength(150)
                .HasDefaultValue("NA");
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.Fname)
                .HasMaxLength(50)
                .HasColumnName("FName");
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("M")
                .IsFixedLength();
            entity.Property(e => e.GradeId)
                .HasMaxLength(50)
                .HasColumnName("GradeID");
            entity.Property(e => e.GradeIdAct)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("GradeID_Act");
            entity.Property(e => e.GradeIdStd)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("GradeID_Std");
            entity.Property(e => e.GroupPolicyNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Icard).HasColumnName("ICard");
            entity.Property(e => e.IcardNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("ICardNo");
            entity.Property(e => e.IndirectReporting)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("INDirectReporting");
            entity.Property(e => e.IpolicyCompany)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("IPolicyCompany");
            entity.Property(e => e.JoinDate).HasColumnType("datetime");
            entity.Property(e => e.LabourStdQty).HasColumnName("Labour_Std_Qty");
            entity.Property(e => e.Leavedate).HasColumnType("datetime");
            entity.Property(e => e.LegalEntity)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.LicenceNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Lname)
                .HasMaxLength(50)
                .HasColumnName("LName");
            entity.Property(e => e.Ltapm).HasColumnName("LTApm");
            entity.Property(e => e.MailId)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("MailID");
            entity.Property(e => e.MartialStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("M")
                .IsFixedLength();
            entity.Property(e => e.Mname)
                .HasMaxLength(50)
                .HasColumnName("MName");
            entity.Property(e => e.Mobile)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.NameAsPerAdhar)
                .HasMaxLength(100)
                .HasDefaultValue("Nil");
            entity.Property(e => e.OsvenderStdQty).HasColumnName("OSVender_Std_Qty");
            entity.Property(e => e.Ot).HasColumnName("OT");
            entity.Property(e => e.Padd)
                .HasMaxLength(300)
                .HasDefaultValue("NIL")
                .HasColumnName("PAdd");
            entity.Property(e => e.Panno)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("PANNo");
            entity.Property(e => e.PassPortNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.PaySheetHeadType).HasDefaultValue(10);
            entity.Property(e => e.Pfemployee).HasColumnName("PFEmployee");
            entity.Property(e => e.Pfemployer).HasColumnName("PFEmployer");
            entity.Property(e => e.Pfno)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("PFNo");
            entity.Property(e => e.PfnoMax).HasColumnName("PFNoMax");
            entity.Property(e => e.Ph1)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("PH1");
            entity.Property(e => e.Ph2)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("PH2");
            entity.Property(e => e.PolicyNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.PositionRoleId)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PositionRoleID");
            entity.Property(e => e.PprivilegeLeave).HasColumnName("PPrivilegeLeave");
            entity.Property(e => e.ProfitCenter)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.ProfitCenterAct)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("ProfitCenter_Act");
            entity.Property(e => e.ProfitCenterOld)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("ProfitCenter_OLD");
            entity.Property(e => e.ProfitCenterStd)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("ProfitCenter_Std");
            entity.Property(e => e.PsickLeave).HasColumnName("PSickLeave");
            entity.Property(e => e.Pt).HasColumnName("PT");
            entity.Property(e => e.Qualification)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Remark)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Resigndate).HasColumnType("datetime");
            entity.Property(e => e.Specilization)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.StaffStdQty).HasColumnName("Staff_Std_Qty");
            entity.Property(e => e.StateId).HasColumnName("StateID");
            entity.Property(e => e.SubDeptFiveId).HasColumnName("SubDeptFiveID");
            entity.Property(e => e.SubDeptFourId).HasColumnName("SubDeptFourID");
            entity.Property(e => e.SubDeptId)
                .HasMaxLength(50)
                .HasColumnName("SubDeptID");
            entity.Property(e => e.SubDeptIdN)
                .HasMaxLength(500)
                .HasDefaultValueSql("((0))")
                .HasColumnName("SubDeptID_N");
            entity.Property(e => e.SubDeptOneId).HasColumnName("SubDeptOneID");
            entity.Property(e => e.SubDeptSevenId).HasColumnName("SubDeptSevenID");
            entity.Property(e => e.SubDeptSixId).HasColumnName("SubDeptSixID");
            entity.Property(e => e.SubDeptThreeId).HasColumnName("SubDeptThreeID");
            entity.Property(e => e.SubDeptTwoId).HasColumnName("SubDeptTwoID");
            entity.Property(e => e.TempEcode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("TempECode");
            entity.Property(e => e.Uanno)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("UANNo");
            entity.Property(e => e.University)
                .HasMaxLength(150)
                .HasDefaultValue("NA");
            entity.Property(e => e.ValidFrom)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ValidTo)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Vanva)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("VA")
                .IsFixedLength()
                .HasColumnName("VANVA");
            entity.Property(e => e.WeeklyOff)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.WorkDesignationId)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("WorkDesignationID");
            entity.Property(e => e.WorkStationId)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("WorkStationID");
        });

        modelBuilder.Entity<EmployeeType>(entity =>
        {
            entity.HasKey(e => e.Etid);

            entity.ToTable("EmployeeType");

            entity.Property(e => e.Etid)
                .HasMaxLength(50)
                .HasColumnName("ETID");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Auth).HasDefaultValue(true);
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.Etname)
                .HasMaxLength(50)
                .HasColumnName("ETName");
            entity.Property(e => e.Remark)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
        });

        modelBuilder.Entity<GatereceiptInternalDetailsSub>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("GatereceiptInternalDetailsSub");

            entity.Property(e => e.ConvertStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Gdiscard)
                .HasDefaultValue(true)
                .HasColumnName("GDiscard");
            entity.Property(e => e.Gricode)
                .HasMaxLength(50)
                .HasColumnName("GRICode");
            entity.Property(e => e.JobcardStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.M45status)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("M45Status");
            entity.Property(e => e.PartCode).HasMaxLength(50);
            entity.Property(e => e.SerialNo).HasMaxLength(50);
            entity.Property(e => e.SerialStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .IsFixedLength();
            entity.Property(e => e.Trfcode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("TRFCode");
            entity.Property(e => e.Trfstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TRFStatus");
            entity.Property(e => e.TrserialStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TRSerialStatus");
        });

        modelBuilder.Entity<GetMaxCode>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("GetMaxCode");

            entity.Property(e => e.CompCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.FormName).HasMaxLength(50);
            entity.Property(e => e.Prefix)
                .HasMaxLength(25)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Remark)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.TblName).HasMaxLength(50);
            entity.Property(e => e.Yr).HasMaxLength(50);
        });

        modelBuilder.Entity<GiirdetailsSub>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("GIIRDetailsSub");

            entity.Property(e => e.ControllerNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.ConvertStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Gdiscard)
                .HasDefaultValue(true)
                .HasColumnName("GDiscard");
            entity.Property(e => e.Giircode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("GIIRCode");
            entity.Property(e => e.Girdspocode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("GIRDSPOCode");
            entity.Property(e => e.JobCardStatus)
                .HasMaxLength(3)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Krmno)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("KRMNo");
            entity.Property(e => e.M45status)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("M45Status");
            entity.Property(e => e.PartCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SerialNo)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.SerialStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .IsFixedLength();
            entity.Property(e => e.Trfcode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("TRFCode");
            entity.Property(e => e.Trfstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TRFStatus");
            entity.Property(e => e.TrserialStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TRSerialStatus");
        });

        modelBuilder.Entity<JobCard>(entity =>
        {
            entity.HasKey(e => e.JobCode);

            entity.ToTable("JobCard");

            entity.Property(e => e.JobCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.CompanyCode).HasMaxLength(10);
            entity.Property(e => e.Dt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MaxSrNo)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Pccode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PCCode");
            entity.Property(e => e.Remark)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Stage1Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Stage2Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Stage3Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Yr)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
        });

        modelBuilder.Entity<JobCardDetail>(entity =>
        {
            entity.HasKey(e => new { e.JobCode, e.PartCode });

            entity.Property(e => e.JobCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PartCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Bomcode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("BOMCode");
            entity.Property(e => e.PlanCode)
                .HasMaxLength(50)
                .HasDefaultValue("OLD");
            entity.Property(e => e.PlanDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Stage1Dt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Stage1Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Stage2Dt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Stage2Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Stage3Dt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Stage3Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<JobCardDetailsSub>(entity =>
        {
            entity.HasKey(e => new { e.JobCode, e.PartCode, e.SerialNo });

            entity.ToTable("JobCardDetailsSub");

            entity.Property(e => e.JobCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PartCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SerialNo)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.JobCard2Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Jpriority).HasColumnName("JPriority");
            entity.Property(e => e.SrNoPartCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Stage1Date).HasColumnType("datetime");
            entity.Property(e => e.Stage1EndPlay)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Stage1Qastatus)
                .HasMaxLength(10)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("Stage1QAStatus");
            entity.Property(e => e.Stage1StartDate).HasColumnType("datetime");
            entity.Property(e => e.Stage1StartPlay)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Stage1StartStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Stage1Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Stage2Date).HasColumnType("datetime");
            entity.Property(e => e.Stage2Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Stage3Date).HasColumnType("datetime");
            entity.Property(e => e.Stage3Qastatus)
                .HasMaxLength(10)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("Stage3QAStatus");
            entity.Property(e => e.Stage3Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.TransferCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.TransferStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
        });

        modelBuilder.Entity<Jobcard2DetailsSub>(entity =>
        {
            entity.HasKey(e => new { e.JobCode, e.SrNoPartCode, e.SerialNo });

            entity.ToTable("Jobcard2DetailsSub");

            entity.Property(e => e.JobCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SrNoPartCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SerialNo)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.J2priority)
                .HasDefaultValue(1)
                .HasColumnName("J2Priority");
            entity.Property(e => e.JobCard1)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PartCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Pdirstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PDIRStatus");
            entity.Property(e => e.PrcStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Stage3Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("D")
                .IsFixedLength();
            entity.Property(e => e.TransCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Trstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TRStatus");
        });

        modelBuilder.Entity<LoginMst>(entity =>
        {
            entity.ToTable("LoginMst");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.AllowMapp).HasColumnName("AllowMApp");
            entity.Property(e => e.AngRights)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.ChangeDt).HasColumnType("datetime");
            entity.Property(e => e.CustomerCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.DregId)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("DRegID");
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.Imeino)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("IMEINo");
            entity.Property(e => e.LastLoginDt).HasColumnType("datetime");
            entity.Property(e => e.LoginType)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("U")
                .IsFixedLength();
            entity.Property(e => e.MPin)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("mPIN");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OnAccountOf)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.OnMachine)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PassWord)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RightsType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("U")
                .IsFixedLength();
            entity.Property(e => e.SupplierCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.UserType)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("E")
                .IsFixedLength();
        });

        modelBuilder.Entity<MemoExciseMfg>(entity =>
        {
            entity.HasKey(e => e.Mecode);

            entity.ToTable("MemoExciseMfg");

            entity.HasIndex(e => e.Mecode, "_dta_index_MemoExciseMfg_12_1549925289__K1").HasFillFactor(80);

            entity.HasIndex(e => new { e.Active, e.Mecode, e.Mestatus }, "_dta_index_MemoExciseMfg_12_1549925289__K33_K1_K17").HasFillFactor(80);

            entity.HasIndex(e => new { e.Active, e.Dt, e.Mecode, e.ConsigneeCode, e.FromProfitCenter, e.ToProfitCenter }, "_dta_index_MemoExciseMfg_12_1549925289__K33_K3_K1_K8_K5_K6").HasFillFactor(80);

            entity.Property(e => e.Mecode)
                .HasMaxLength(50)
                .HasColumnName("MECode");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Auth2).HasDefaultValue(true);
            entity.Property(e => e.AuthRemark1)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
            entity.Property(e => e.AuthRemark2)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
            entity.Property(e => e.AuthRemark3)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
            entity.Property(e => e.CompanyCode).HasMaxLength(50);
            entity.Property(e => e.ConsigneeCode).HasMaxLength(50);
            entity.Property(e => e.CurrencyCode).HasMaxLength(50);
            entity.Property(e => e.CurrencyRate).HasDefaultValue(0.0);
            entity.Property(e => e.DeliveryTerms)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Discard).HasDefaultValue(true);
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.ExpStatus)
                .HasMaxLength(2)
                .HasDefaultValue("P");
            entity.Property(e => e.ExportStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.FromProfitCenter).HasMaxLength(50);
            entity.Property(e => e.MaxSrNo).HasMaxLength(50);
            entity.Property(e => e.Mestatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("MEStatus");
            entity.Property(e => e.Metype)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("N")
                .IsFixedLength()
                .HasComment("For Rejection memo of mfg")
                .HasColumnName("METype");
            entity.Property(e => e.Mfgdino)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("MFGDINo");
            entity.Property(e => e.Mmtfcode)
                .HasMaxLength(25)
                .HasDefaultValueSql("((0))")
                .HasColumnName("MMTFCode");
            entity.Property(e => e.MtfscanStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("D")
                .IsFixedLength()
                .HasColumnName("MTFScanStatus");
            entity.Property(e => e.OnAcParty)
                .HasMaxLength(600)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Opstatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("C")
                .IsFixedLength()
                .HasColumnName("OPStatus");
            entity.Property(e => e.Podate)
                .HasColumnType("datetime")
                .HasColumnName("PODate");
            entity.Property(e => e.Pono)
                .HasMaxLength(100)
                .HasDefaultValueSql("((0))")
                .IsFixedLength()
                .HasColumnName("PONo");
            entity.Property(e => e.PortOfDischarge)
                .HasMaxLength(100)
                .HasDefaultValue("NIL");
            entity.Property(e => e.PortOfLoading)
                .HasMaxLength(100)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Remark)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
            entity.Property(e => e.StockType).HasComment("0-Normal,1-Raw,2-Process");
            entity.Property(e => e.Tmcode)
                .HasMaxLength(50)
                .HasColumnName("TMcode");
            entity.Property(e => e.ToProfitCenter).HasMaxLength(50);
            entity.Property(e => e.TransportRoute)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
            entity.Property(e => e.VehicleNo)
                .HasMaxLength(50)
                .HasDefaultValue("-");
            entity.Property(e => e.VehicleType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("RR")
                .IsFixedLength();
            entity.Property(e => e.VesselFlightNo)
                .HasMaxLength(100)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Viostatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("I")
                .IsFixedLength()
                .HasColumnName("VIOStatus");
            entity.Property(e => e.Yr)
                .HasMaxLength(50)
                .HasDefaultValue("10-11");
        });

        modelBuilder.Entity<Mof>(entity =>
        {
            entity.HasKey(e => e.Mofcode);

            entity.ToTable("MOF");

            entity.HasIndex(e => new { e.Mofcode, e.DispatchFromCode, e.PartCode, e.BranchCode, e.OrderBy, e.Active }, "_dta_index_MOF_15_883703142__K1_K64_K8_K87_K14_K114");

            entity.HasIndex(e => new { e.Dspinst, e.Active, e.Discard, e.Dt, e.Moftype, e.OnAccountOf, e.PayModeCode, e.PayDaysCode, e.DispatchFromCode, e.CategoryId, e.BranchCode, e.CommissioningCode, e.ServiceCode }, "_dta_index_MOF_5_2071547755__K87_K138_K109_K3_K90_K11_K69_K70_K71_K7_K94_K80_K83_1_8_9_12_13_17_56_57_59_60_61_62_76_77_78_79_");

            entity.Property(e => e.Mofcode)
                .HasMaxLength(50)
                .HasColumnName("MOFCode");
            entity.Property(e => e.AccPayRemark).HasDefaultValue("NIL");
            entity.Property(e => e.Accessories)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
            entity.Property(e => e.ActCompletionDt).HasColumnType("datetime");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.AddPartRemark)
                .HasMaxLength(1500)
                .HasDefaultValue("NIL");
            entity.Property(e => e.AdvPayGuaRefNo).HasMaxLength(100);
            entity.Property(e => e.AmountRo).HasColumnName("AmountRO");
            entity.Property(e => e.Auth).HasDefaultValue(true);
            entity.Property(e => e.Auth2HoldRemark)
                .HasMaxLength(500)
                .HasDefaultValue("NA");
            entity.Property(e => e.AuthAc)
                .HasDefaultValue(true)
                .HasColumnName("AuthAC");
            entity.Property(e => e.AuthNfa).HasColumnName("AuthNFA");
            entity.Property(e => e.AuthNfaremark)
                .HasMaxLength(500)
                .HasDefaultValue("NA")
                .HasColumnName("AuthNFARemark");
            entity.Property(e => e.AuthRemark)
                .HasMaxLength(500)
                .HasDefaultValue("Auto Auth");
            entity.Property(e => e.AuthRemark1)
                .HasMaxLength(500)
                .HasDefaultValue("NIL");
            entity.Property(e => e.AuthRemark2)
                .HasMaxLength(500)
                .HasDefaultValue("NIL");
            entity.Property(e => e.AuthRemarkAc)
                .HasMaxLength(500)
                .HasDefaultValue("NIL")
                .HasColumnName("AuthRemarkAC");
            entity.Property(e => e.AuthRemarkService)
                .HasMaxLength(1000)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Bomcode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("BOMCode");
            entity.Property(e => e.Bomprice).HasColumnName("BOMPrice");
            entity.Property(e => e.BranchCode).HasMaxLength(50);
            entity.Property(e => e.BranchCodeOld)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("BranchCode_OLD");
            entity.Property(e => e.CategoryId)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("CategoryID");
            entity.Property(e => e.Cess).HasColumnName("CESS");
            entity.Property(e => e.Cgst).HasColumnName("CGST");
            entity.Property(e => e.CommencementDt).HasColumnType("datetime");
            entity.Property(e => e.CommissioningCode).HasMaxLength(50);
            entity.Property(e => e.CompanyCode).HasMaxLength(50);
            entity.Property(e => e.CompletionDt).HasColumnType("datetime");
            entity.Property(e => e.CompletionTime).HasMaxLength(50);
            entity.Property(e => e.CpanelAmt).HasColumnName("CPanelAmt");
            entity.Property(e => e.Cpbomamt).HasColumnName("CPBOMAmt");
            entity.Property(e => e.Cpbomcode)
                .HasMaxLength(100)
                .HasDefaultValueSql("((0))")
                .HasColumnName("CPBOMCode");
            entity.Property(e => e.Cst).HasColumnName("CST");
            entity.Property(e => e.CurrencyCode).HasMaxLength(50);
            entity.Property(e => e.CustomerCode).HasMaxLength(50);
            entity.Property(e => e.Destination)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Dginspection).HasColumnName("DGInspection");
            entity.Property(e => e.DginspectionBy)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("N")
                .IsFixedLength()
                .HasColumnName("DGInspectionBy");
            entity.Property(e => e.Discard).HasDefaultValue(true);
            entity.Property(e => e.DispatchFromCode)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.DivertMoffrom)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("DivertMOFFrom");
            entity.Property(e => e.DivertStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Doadvance).HasColumnName("DOAdvance");
            entity.Property(e => e.DobankAddress)
                .HasDefaultValue("nil")
                .HasColumnName("DOBankAddress");
            entity.Property(e => e.DobankName)
                .HasMaxLength(500)
                .HasDefaultValue("nil")
                .HasColumnName("DOBankName");
            entity.Property(e => e.Dodate)
                .HasColumnType("datetime")
                .HasColumnName("DODate");
            entity.Property(e => e.DonetDisbursed).HasColumnName("DONetDisbursed");
            entity.Property(e => e.Dono)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("DONo");
            entity.Property(e => e.Dspinst)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasComment("For Dispatch Instructions If Qty Is complete Status Will Change To C")
                .HasColumnName("DSPInst");
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.ExpiryDtofGua).HasColumnType("datetime");
            entity.Property(e => e.ExportStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.FormType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("C")
                .IsFixedLength();
            entity.Property(e => e.HedCess).HasColumnName("HEdCESS");
            entity.Property(e => e.Igst).HasColumnName("IGST");
            entity.Property(e => e.IndentId)
                .HasMaxLength(100)
                .HasColumnName("IndentID");
            entity.Property(e => e.IndentorCode).HasMaxLength(50);
            entity.Property(e => e.InsuranceBy)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("K")
                .IsFixedLength();
            entity.Property(e => e.InvoicePartDesc)
                .HasMaxLength(500)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Krmamt).HasColumnName("KRMAmt");
            entity.Property(e => e.MailStatusNfa).HasColumnName("MailStatusNFA");
            entity.Property(e => e.MaxSrNo).HasMaxLength(10);
            entity.Property(e => e.Mcramt).HasColumnName("MCRAmt");
            entity.Property(e => e.Mcrwt).HasColumnName("MCRWt");
            entity.Property(e => e.MemoStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Mhramt).HasColumnName("MHRAmt");
            entity.Property(e => e.Mhrwt).HasColumnName("MHRWt");
            entity.Property(e => e.MinvStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("MInvStatus");
            entity.Property(e => e.MktPricelist).HasComment("0");
            entity.Property(e => e.MktgreSchStatus)
                .HasMaxLength(50)
                .HasColumnName("MKTGReSchStatus");
            entity.Property(e => e.MktschRemark)
                .HasDefaultValue("NIL")
                .HasColumnName("MKTSchRemark");
            entity.Property(e => e.MofFirstlevel)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Mofstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("MOFStatus");
            entity.Property(e => e.Moftype)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("N")
                .IsFixedLength()
                .HasColumnName("MOFType");
            entity.Property(e => e.Msqft).HasColumnName("MSqft");
            entity.Property(e => e.MsreplTranVersion)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("msrepl_tran_version");
            entity.Property(e => e.Mwt).HasColumnName("MWt");
            entity.Property(e => e.NfaholdStatus)
                .HasMaxLength(500)
                .HasDefaultValue("NA")
                .HasColumnName("NFAHoldStatus");
            entity.Property(e => e.Nfakala).HasColumnName("NFAKala");
            entity.Property(e => e.Nfakoel).HasColumnName("NFAKoel");
            entity.Property(e => e.Nfano)
                .HasMaxLength(50)
                .HasDefaultValue("NA")
                .HasColumnName("NFANo");
            entity.Property(e => e.Nfaother).HasColumnName("NFAOther");
            entity.Property(e => e.OctriBy)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("K")
                .IsFixedLength();
            entity.Property(e => e.OmanBankCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .IsFixedLength();
            entity.Property(e => e.OnAccountOf).HasMaxLength(50);
            entity.Property(e => e.Orc).HasColumnName("ORC");
            entity.Property(e => e.Orcagent).HasColumnName("ORCAgent");
            entity.Property(e => e.Orcby)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("K")
                .IsFixedLength()
                .HasColumnName("ORCBy");
            entity.Property(e => e.Orcstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("ORCStatus");
            entity.Property(e => e.OrderBy)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.PackingBy)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("K")
                .IsFixedLength();
            entity.Property(e => e.PartCode).HasMaxLength(50);
            entity.Property(e => e.PayDaysCode).HasMaxLength(50);
            entity.Property(e => e.PayModeCode).HasMaxLength(50);
            entity.Property(e => e.Pfistatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PFIStatus");
            entity.Property(e => e.Poamount).HasColumnName("POAmount");
            entity.Property(e => e.Podate)
                .HasColumnType("datetime")
                .HasColumnName("PODate");
            entity.Property(e => e.Pono)
                .HasMaxLength(100)
                .HasColumnName("PONo");
            entity.Property(e => e.Ppmstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PPMStatus");
            entity.Property(e => e.Ppwstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PPWStatus");
            entity.Property(e => e.PrdschRemark)
                .HasDefaultValue("NIL")
                .HasColumnName("PRDSchRemark");
            entity.Property(e => e.ProductMob)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("M")
                .IsFixedLength()
                .HasColumnName("ProductMOB");
            entity.Property(e => e.ProjectName).HasMaxLength(100);
            entity.Property(e => e.PulseId)
                .HasMaxLength(100)
                .HasColumnName("PulseID");
            entity.Property(e => e.PulseSodate)
                .HasColumnType("datetime")
                .HasColumnName("PulseSODate");
            entity.Property(e => e.PulseSono)
                .HasMaxLength(100)
                .HasColumnName("PulseSONo");
            entity.Property(e => e.Qtnno)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("QTNNo");
            entity.Property(e => e.Qtype)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("N")
                .IsFixedLength();
            entity.Property(e => e.RecStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Remark)
                .HasMaxLength(1500)
                .HasDefaultValue("NIL");
            entity.Property(e => e.RevComplDt).HasColumnType("datetime");
            entity.Property(e => e.Rtcno)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("RTCNo");
            entity.Property(e => e.SectorCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.ServiceCode).HasMaxLength(50);
            entity.Property(e => e.ServiceDealerCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Sgst).HasColumnName("SGST");
            entity.Property(e => e.SiteReadyDate).HasColumnType("datetime");
            entity.Property(e => e.SiteStatusRemark).HasDefaultValue("NIL");
            entity.Property(e => e.SiteVisitDate).HasColumnType("datetime");
            entity.Property(e => e.StartMaintPeriod).HasMaxLength(50);
            entity.Property(e => e.TaxationBy)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("C")
                .IsFixedLength();
            entity.Property(e => e.Tcs).HasColumnName("TCS");
            entity.Property(e => e.TempStatus).HasDefaultValue(true);
            entity.Property(e => e.TentativeDt).HasColumnType("datetime");
            entity.Property(e => e.TocallocateStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TOCAllocateStatus");
            entity.Property(e => e.ToolKit).HasDefaultValue(true);
            entity.Property(e => e.TransportBy)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("K")
                .IsFixedLength();
            entity.Property(e => e.UnloadingBy)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("C")
                .IsFixedLength();
            entity.Property(e => e.Vat).HasColumnName("VAT");
            entity.Property(e => e.WorkOrderDt).HasColumnType("datetime");
            entity.Property(e => e.WorkOrderNo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Yr)
                .HasMaxLength(50)
                .HasDefaultValue("10-11");
        });

        modelBuilder.Entity<MtfdetailsSub>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("MTFDetailsSub");

            entity.Property(e => e.AutoclaveStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.JobCardStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Mtfcode)
                .HasMaxLength(50)
                .HasColumnName("MTFCode");
            entity.Property(e => e.PartCode).HasMaxLength(50);
            entity.Property(e => e.Qastatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("QAStatus");
            entity.Property(e => e.SerialNo).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Trfstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TRFStatus");
            entity.Property(e => e.TrserialStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TRSerialStatus");
        });

        modelBuilder.Entity<PanelTypeKit>(entity =>
        {
            entity.HasKey(e => e.PanelTypeId);

            entity.ToTable("PanelTypeKit");

            entity.Property(e => e.PanelTypeId).HasColumnName("PanelTypeID");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Auth).HasDefaultValue(true);
            entity.Property(e => e.DgkVa).HasColumnName("DGkVA");
            entity.Property(e => e.Dgmodel)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DGModel");
            entity.Property(e => e.Dgphase)
                .HasDefaultValue(3)
                .HasColumnName("DGPhase");
            entity.Property(e => e.Dgtype)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("iGreen")
                .IsFixedLength()
                .HasColumnName("DGType");
            entity.Property(e => e.Dt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PanelTypeName)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Remark)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasDefaultValue("OK")
                .IsFixedLength();
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasKey(e => e.PartCode).HasName("PK_PartMaster");

            entity.ToTable("Part", tb => tb.HasTrigger("sp_MSsync_upd_trig_Part_1"));

            entity.HasIndex(e => new { e.PartCode, e.Uomcode }, "_dta_index_Part_12_1631305567__K1_K8_3").HasFillFactor(80);

            entity.HasIndex(e => new { e.ClassCode, e.Mob }, "_dta_index_Part_12_1631305567__K7_K12_1").HasFillFactor(80);

            entity.HasIndex(e => new { e.ClassCode, e.Mob, e.PartCode }, "_dta_index_Part_12_1631305567__K7_K12_K1").HasFillFactor(80);

            entity.HasIndex(e => new { e.ClassCode, e.Mob, e.PartCode, e.CategoryId, e.Uomcode }, "_dta_index_Part_12_1631305567__K7_K12_K1_K11_K8").HasFillFactor(80);

            entity.HasIndex(e => new { e.ClassCode, e.PartCode, e.CategoryId, e.Uomcode }, "_dta_index_Part_12_1631305567__K7_K1_K11_K8").HasFillFactor(80);

            entity.HasIndex(e => new { e.ChapterCode, e.PartCode }, "_dta_index_Part_12_1631305567__K84_K1").HasFillFactor(80);

            entity.HasIndex(e => new { e.Uomcode, e.ClassCode, e.Mob, e.PartCode, e.CategoryId }, "_dta_index_Part_12_1631305567__K8_K7_K12_K1_K11").HasFillFactor(80);

            entity.HasIndex(e => new { e.CategoryId, e.Twelve, e.PartCode, e.Kit }, "_dta_index_Part_5_337253548__K11_K71_K1_K85");

            entity.HasIndex(e => new { e.CategoryId, e.Fourteen, e.Thirteen, e.PartCode, e.Kit }, "_dta_index_Part_5_337253548__K11_K73_K72_K1_K85_71");

            entity.HasIndex(e => e.PartCode, "_dta_index_Part_5_337253548__K1_30_63_71_72_73_85_95");

            entity.HasIndex(e => e.PartCode, "_dta_index_Part_5_337253548__K1_3_30_32_43_54");

            entity.HasIndex(e => e.PartCode, "_dta_index_Part_5_337253548__K1_9987");

            entity.HasIndex(e => new { e.PartCode, e.Kva, e.Model, e.Phase }, "_dta_index_Part_5_337253548__K1_K30_K32_K43_31");

            entity.HasIndex(e => new { e.PartCode, e.Four }, "_dta_index_Part_5_337253548__K1_K63");

            entity.HasIndex(e => new { e.PartCode, e.Twelve }, "_dta_index_Part_5_337253548__K1_K71");

            entity.HasIndex(e => new { e.PartCode, e.Uomcode }, "_dta_index_Part_5_337253548__K1_K8_3_86");

            entity.HasIndex(e => new { e.Twelve, e.PartCode }, "_dta_index_Part_5_337253548__K71_K1");

            entity.HasIndex(e => new { e.Twelve, e.PartCode, e.Kit }, "_dta_index_Part_5_337253548__K71_K1_K85");

            entity.HasIndex(e => new { e.Twelve, e.PartCode, e.Kit, e.Thirteen }, "_dta_index_Part_5_337253548__K71_K1_K85_K72");

            entity.HasIndex(e => new { e.Twelve, e.PartCode, e.Kit, e.Thirteen, e.Fourteen }, "_dta_index_Part_5_337253548__K71_K1_K85_K72_K73");

            entity.HasIndex(e => new { e.Twelve, e.PartCode, e.Kit, e.Fourteen }, "_dta_index_Part_5_337253548__K71_K1_K85_K73");

            entity.HasIndex(e => new { e.Twelve, e.Fourteen, e.Thirteen, e.PartCode, e.Kit }, "_dta_index_Part_5_337253548__K71_K73_K72_K1_K85");

            entity.Property(e => e.PartCode).HasMaxLength(50);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.AliseName)
                .HasMaxLength(1000)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Amps)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("AMPS");
            entity.Property(e => e.ApplicationCode)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Araino)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("ARAINo");
            entity.Property(e => e.Asp)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("ASP");
            entity.Property(e => e.AuthRemark)
                .HasMaxLength(500)
                .HasDefaultValue("NIL");
            entity.Property(e => e.CalibrationDueOn)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Capacity)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.CategoryId)
                .HasMaxLength(50)
                .HasColumnName("CategoryID");
            entity.Property(e => e.Cfm)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("CFM");
            entity.Property(e => e.ChapterCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.ClassCode).HasMaxLength(50);
            entity.Property(e => e.ConvUomcode)
                .HasMaxLength(50)
                .HasColumnName("ConvUOMCode");
            entity.Property(e => e.ConversionValue).HasDefaultValue(1.0);
            entity.Property(e => e.Coupling)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Cylinder)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.DgconvUnit).HasColumnName("DGConvUnit");
            entity.Property(e => e.Diameter)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Discard).HasDefaultValue(true);
            entity.Property(e => e.DrwgNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.FrameSize)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Gauge)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Gstperc).HasColumnName("GSTPerc");
            entity.Property(e => e.Housing)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Hp)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("HP");
            entity.Property(e => e.InsulationClass)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Kit).HasColumnName("KIT");
            entity.Property(e => e.Kva).HasColumnName("KVA");
            entity.Property(e => e.LeastCount)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.LocationCode)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Make)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.MatDt).HasColumnType("datetime");
            entity.Property(e => e.MatType)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("MOV")
                .IsFixedLength();
            entity.Property(e => e.Mob)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("M")
                .IsFixedLength()
                .HasColumnName("MOB");
            entity.Property(e => e.Mobint)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("B")
                .IsFixedLength()
                .HasColumnName("MOBInt");
            entity.Property(e => e.Model)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.ModelGenset)
                .HasMaxLength(50)
                .HasDefaultValue("NA");
            entity.Property(e => e.ModelSeries)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .IsFixedLength();
            entity.Property(e => e.MsreplTranVersion)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("msrepl_tran_version");
            entity.Property(e => e.One)
                .HasMaxLength(5)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.PartDesc)
                .HasMaxLength(1000)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Phase)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Pitch)
                .HasMaxLength(50)
                .HasDefaultValue("0")
                .HasComment("Bending Strokes");
            entity.Property(e => e.Poles)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.PurchaseHead)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.PurchaseStockTransferHead)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Rating)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Remark).HasDefaultValue("NIL");
            entity.Property(e => e.RevNo)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Rpm)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("RPM");
            entity.Property(e => e.SalesHead)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.SalesStockTransferHead)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Sizes)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("SIZES");
            entity.Property(e => e.Start)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Uomcode)
                .HasMaxLength(50)
                .HasColumnName("UOMCode");
            entity.Property(e => e.Voltage)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Wattage)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
        });

        modelBuilder.Entity<PartTocdetailsSupplier>(entity =>
        {
            entity.HasKey(e => new { e.CompanyCode, e.PartCode, e.SuppCode, e.ForPccode, e.Product });

            entity.ToTable("PartTOCDetailsSupplier");

            entity.Property(e => e.CompanyCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PartCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SuppCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ForPccode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("ForPCCode");
            entity.Property(e => e.Product)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValue("CP")
                .IsFixedLength();
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Auth).HasDefaultValue(true);
            entity.Property(e => e.Dt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Fos).HasColumnName("FOS");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.Moq).HasColumnName("MOQ");
            entity.Property(e => e.PcostCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PCostCode");
            entity.Property(e => e.PcostUomcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PCostUOMCode");
            entity.Property(e => e.Poper).HasColumnName("POPer");
            entity.Property(e => e.Postatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("POStatus");
            entity.Property(e => e.Potype)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("M")
                .IsFixedLength()
                .HasColumnName("POType");
            entity.Property(e => e.Remark)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasDefaultValue("Nil");
            entity.Property(e => e.Rlt).HasColumnName("RLT");
            entity.Property(e => e.TmatType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("TOC")
                .HasColumnName("TMatType");
        });

        modelBuilder.Entity<PcstageWiseRate>(entity =>
        {
            entity.HasKey(e => new { e.Pccode, e.PartCode, e.StageName }).HasName("PK_PCStageWiseRate_1");

            entity.ToTable("PCStageWiseRate");

            entity.Property(e => e.Pccode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PCCode");
            entity.Property(e => e.PartCode).HasMaxLength(20);
            entity.Property(e => e.StageName)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Dt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.JobCode).HasMaxLength(20);
        });

        modelBuilder.Entity<PcstageWiseRateChange>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PCStageWiseRateChange");

            entity.Property(e => e.Dt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.JobCode).HasMaxLength(20);
            entity.Property(e => e.PartCode).HasMaxLength(20);
            entity.Property(e => e.Pccode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PCCode");
            entity.Property(e => e.StageName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PrcChkDetail>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.DgstartTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("DGStartTime");
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.MainSerialNo).HasMaxLength(50);
            entity.Property(e => e.PrcChkPoints).HasMaxLength(50);
            entity.Property(e => e.PrcName).HasMaxLength(50);
            entity.Property(e => e.PrcStatus)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.Qa6m).HasColumnName("QA6M");
            entity.Property(e => e.Qastatus)
                .HasMaxLength(10)
                .HasDefaultValue("D")
                .IsFixedLength()
                .HasColumnName("QAStatus");
            entity.Property(e => e.QastatusDt)
                .HasColumnType("datetime")
                .HasColumnName("QAStatusDt");
            entity.Property(e => e.TransCode).HasMaxLength(50);
        });

        modelBuilder.Entity<PrimaryCompAssign>(entity =>
        {
            entity.HasKey(e => e.Pcacode);

            entity.ToTable("PrimaryCompAssign");

            entity.Property(e => e.Pcacode)
                .HasMaxLength(50)
                .HasColumnName("PCACode");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.AssignByEmpCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Astatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("AStatus");
            entity.Property(e => e.Auth).HasDefaultValue(true);
            entity.Property(e => e.CompNo).HasMaxLength(50);
            entity.Property(e => e.CompType)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.CompanyCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.EmpCode).HasMaxLength(50);
            entity.Property(e => e.KoelempCode)
                .HasMaxLength(50)
                .HasColumnName("KOELEmpCode");
            entity.Property(e => e.MaxSrNo).HasMaxLength(50);
            entity.Property(e => e.PartCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Priority).HasMaxLength(50);
            entity.Property(e => e.Remark)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
            entity.Property(e => e.ServiceNo).HasMaxLength(50);
            entity.Property(e => e.TechnicianCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.WarrantyStatus).HasMaxLength(50);
            entity.Property(e => e.Yr)
                .HasMaxLength(50)
                .HasDefaultValue("10-11");
        });

        modelBuilder.Entity<PrimaryCompAssignDetail>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.Pcacode)
                .HasMaxLength(50)
                .HasColumnName("PCACode");
            entity.Property(e => e.SrNo).HasDefaultValue(1);
            entity.Property(e => e.SupEmpCode).HasMaxLength(50);
            entity.Property(e => e.Type)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue("D")
                .IsFixedLength();
        });

        modelBuilder.Entity<ProcessFeedBack>(entity =>
        {
            entity.HasKey(e => e.Pfbcode);

            entity.ToTable("ProcessFeedBack");

            entity.HasIndex(e => new { e.Dt, e.Pfbcode, e.ProfitCenterCode }, "_dta_index_ProcessFeedBack_12_1306448424__K4_K2_K6").HasFillFactor(80);

            entity.HasIndex(e => new { e.ProfitCenterCode, e.Dt, e.Pfbcode, e.NestingForCode, e.CanopyCode, e.GroupPfbcode }, "_dta_index_ProcessFeedBack_12_1306448424__K6_K4_K2_K10_K9_K1_17").HasFillFactor(80);

            entity.HasIndex(e => new { e.CompanyCode, e.Dt, e.Qpcstatus, e.Discard, e.Active, e.CanopyPlanCode, e.NestingForCode, e.CanopyCode, e.PartCode, e.ProfitCenterCode }, "_dta_index_ProcessFeedBack_6_1861490306__K22_K4_K25_K31_K30_K8_K10_K9_K13_K6_2").HasFillFactor(80);

            entity.HasIndex(e => new { e.Active, e.ProfitCenterCode, e.NestingForCode, e.Pfbcode, e.GroupPfbcode }, "_dta_index_ProcessFeedBack_6_1861490306__K30_K6_K10_K2_K1").HasFillFactor(80);

            entity.Property(e => e.Pfbcode)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PFBCode");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Auth).HasDefaultValue(true);
            entity.Property(e => e.CanopyCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.CanopyPlanCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.CatId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("00")
                .HasColumnName("CatID");
            entity.Property(e => e.CompanyCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CpyStageType)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Crrate).HasColumnName("CRRate");
            entity.Property(e => e.Crwt).HasColumnName("CRWt");
            entity.Property(e => e.Discard).HasDefaultValue(true);
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.Edt)
                .HasColumnType("datetime")
                .HasColumnName("EDt");
            entity.Property(e => e.GroupPfbcode)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("GroupPFBCode");
            entity.Property(e => e.Hrrate).HasColumnName("HRRate");
            entity.Property(e => e.Hrwt).HasColumnName("HRWt");
            entity.Property(e => e.MachineCode)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.MaxSrNo)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Mofcode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("MOFCode");
            entity.Property(e => e.NestingForCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.PartCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Pfbrate).HasColumnName("PFBRate");
            entity.Property(e => e.Pfbtype)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("N")
                .IsFixedLength()
                .HasComment("N=Normal Process,A= Assly")
                .HasColumnName("PFBType");
            entity.Property(e => e.PkitQty).HasColumnName("PKitQty");
            entity.Property(e => e.Plength).HasColumnName("PLength");
            entity.Property(e => e.Ppdirstatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PPDIRStatus");
            entity.Property(e => e.Ppwcode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PPWCode");
            entity.Property(e => e.PrcBomcode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("PrcBOMCode");
            entity.Property(e => e.PrcinvAmount).HasColumnName("PRCInvAmount");
            entity.Property(e => e.PrcinvType)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue("O")
                .IsFixedLength()
                .HasColumnName("PRCInvType");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.ProfitCenterCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Pthickness).HasColumnName("PThickness");
            entity.Property(e => e.Pwidth).HasColumnName("PWidth");
            entity.Property(e => e.Qpcstatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("QPCStatus");
            entity.Property(e => e.Rdiscard)
                .HasDefaultValue(true)
                .HasColumnName("RDiscard");
            entity.Property(e => e.Remark)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.SerialNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.SilCladdingStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.SupplierCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Trstatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TRStatus");
            entity.Property(e => e.TurretKitCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.VersionCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Yr)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("10-11");
        });

        modelBuilder.Entity<ProcessFeedbackDetailsSub>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ProcessFeedbackDetailsSub");

            entity.Property(e => e.BfmsrNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("BFMSrNo");
            entity.Property(e => e.Castatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("CAStatus");
            entity.Property(e => e.ConvertStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Ecode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("ECode");
            entity.Property(e => e.EdtD).HasColumnType("datetime");
            entity.Property(e => e.FlksrNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("FLKSrNo");
            entity.Property(e => e.JobCardStatus)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.PartCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Pcstatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PCStatus");
            entity.Property(e => e.Pdirdt)
                .HasColumnType("datetime")
                .HasColumnName("PDIRDt");
            entity.Property(e => e.Pdirrwstatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PDIRRWStatus");
            entity.Property(e => e.Pdirstatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PDIRStatus");
            entity.Property(e => e.PfbbotserialNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PFBBOTSerialNo");
            entity.Property(e => e.Pfbcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PFBCode");
            entity.Property(e => e.ProcStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Qpcstatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("QPCStatus");
            entity.Property(e => e.Rwstatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("RWStatus");
            entity.Property(e => e.SerialNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.Trfcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("TRFCode");
            entity.Property(e => e.Trfstatus)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TRFStatus");
            entity.Property(e => e.TrserialStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TRSerialStatus");
        });

        modelBuilder.Entity<ProfitCenter>(entity =>
        {
            entity.HasKey(e => e.PcId).HasName("PK_ProfitCenter_NEW_1");

            entity.ToTable("ProfitCenter");

            entity.Property(e => e.PcId).HasColumnName("PC_ID");
            entity.Property(e => e.Auth).HasDefaultValue(true);
            entity.Property(e => e.CompanyCode).HasMaxLength(50);
            entity.Property(e => e.Discard).HasDefaultValue(true);
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.Hodecode)
                .HasMaxLength(50)
                .HasColumnName("HODECode");
            entity.Property(e => e.HodmailId)
                .HasMaxLength(300)
                .HasColumnName("HODMailID");
            entity.Property(e => e.PcaliseName)
                .HasMaxLength(300)
                .HasColumnName("PCAliseName");
            entity.Property(e => e.Pccode)
                .HasMaxLength(50)
                .HasColumnName("PCCode");
            entity.Property(e => e.Pcname)
                .HasMaxLength(250)
                .HasColumnName("PCName");
            entity.Property(e => e.Pcrate).HasColumnName("PCRate");
            entity.Property(e => e.Remark).HasMaxLength(300);
            entity.Property(e => e.StateId).HasColumnName("StateID");
        });

        modelBuilder.Entity<ProfitCenterPldetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ProfitCenterPLDetails");

            entity.HasIndex(e => new { e.ProfitCenterCode, e.PartCode, e.MatRate, e.PrcRate }, "_dta_index_ProfitCenterPLDetails_12_1972878791__K1_K3_K15_K16").HasFillFactor(80);

            entity.Property(e => e.Bomcode)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("BOMCode");
            entity.Property(e => e.Cramt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("CRAmt");
            entity.Property(e => e.Crwt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("CRWt");
            entity.Property(e => e.Hramt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("HRAmt");
            entity.Property(e => e.Hrwt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("HRWt");
            entity.Property(e => e.MatRate).HasDefaultValueSql("('0')");
            entity.Property(e => e.Ohamt).HasColumnName("OHAmt");
            entity.Property(e => e.Ohper).HasColumnName("OHPer");
            entity.Property(e => e.PartCode).HasMaxLength(50);
            entity.Property(e => e.Pltype)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("M")
                .IsFixedLength()
                .HasColumnName("PLType");
            entity.Property(e => e.PrcRate).HasDefaultValueSql("('0')");
            entity.Property(e => e.ProfitCenterCode).HasMaxLength(50);
            entity.Property(e => e.ProfitPer).HasDefaultValueSql("('10')");
            entity.Property(e => e.PsqFt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("PSqFt");
            entity.Property(e => e.Pwt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("PWt");
            entity.Property(e => e.Rate).HasDefaultValueSql("('0')");
            entity.Property(e => e.Sauth).HasColumnName("SAuth");
            entity.Property(e => e.ScrapRate).HasDefaultValueSql("('0')");
            entity.Property(e => e.SysDt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<ProfitCenterplDetailsChanged>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ProfitCenterplDetailsChanged");

            entity.HasIndex(e => new { e.ProfitCenterCode, e.ChangeDateTime }, "_dta_index_ProfitCenterplDetailsChanged_12_1927990245__K1_K8_3_4_5_7_11").HasFillFactor(80);

            entity.Property(e => e.Bomcode)
                .HasMaxLength(50)
                .HasDefaultValue("NIL")
                .HasColumnName("BOMCode");
            entity.Property(e => e.ChangeDateTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Cramt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("CRAmt");
            entity.Property(e => e.Crwt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("CRWt");
            entity.Property(e => e.Ecode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("ECode");
            entity.Property(e => e.Hramt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("HRAmt");
            entity.Property(e => e.Hrwt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("HRWt");
            entity.Property(e => e.MatRate).HasDefaultValueSql("('0')");
            entity.Property(e => e.Ohamt).HasColumnName("OHAmt");
            entity.Property(e => e.Ohper).HasColumnName("OHper");
            entity.Property(e => e.PartCode).HasMaxLength(50);
            entity.Property(e => e.Pltype)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("M")
                .IsFixedLength()
                .HasColumnName("PLType");
            entity.Property(e => e.PrcRate).HasDefaultValueSql("('0')");
            entity.Property(e => e.ProfitCenterCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.ProfitPer).HasDefaultValueSql("('10')");
            entity.Property(e => e.PsqFt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("PSqFt");
            entity.Property(e => e.Pwt)
                .HasDefaultValueSql("('0')")
                .HasColumnName("PWt");
            entity.Property(e => e.Rtype)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("N")
                .IsFixedLength()
                .HasColumnName("RType");
            entity.Property(e => e.SaleRate).HasDefaultValueSql("('0')");
            entity.Property(e => e.ScrapRate).HasDefaultValueSql("('0')");
        });

        modelBuilder.Entity<PurchaseCosting>(entity =>
        {
            entity.HasKey(e => e.Pccode);

            entity.ToTable("PurchaseCosting");

            entity.HasIndex(e => new { e.Active, e.Discard, e.Auth, e.SupplierCode, e.PartCode, e.Pccode }, "_dta_index_PurchaseCosting_12_1135551329__K31_K30_K32_K4_K5_K1_2_9_10_11_20_21_22_23_28_29").HasFillFactor(80);

            entity.Property(e => e.Pccode)
                .ValueGeneratedNever()
                .HasComment("Purchase Costing")
                .HasColumnName("PCCode");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.AuthRemark)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Cessperc).HasColumnName("CESSPerc");
            entity.Property(e => e.CompanyCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyCode).HasMaxLength(50);
            entity.Property(e => e.Discard).HasDefaultValue(true);
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.Edperc).HasColumnName("EDPerc");
            entity.Property(e => e.HcessPerc).HasColumnName("HCessPerc");
            entity.Property(e => e.OrderValidSelected)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("Q")
                .IsFixedLength();
            entity.Property(e => e.PartCode).HasMaxLength(50);
            entity.Property(e => e.Pol).HasColumnName("POL");
            entity.Property(e => e.Pom).HasColumnName("POM");
            entity.Property(e => e.RateSelected)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("M")
                .IsFixedLength();
            entity.Property(e => e.Remark)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasDefaultValue("NIL");
            entity.Property(e => e.SupplierCode).HasMaxLength(50);
            entity.Property(e => e.Uomcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("01")
                .HasColumnName("UOMCode");
            entity.Property(e => e.ValidDate).HasColumnType("datetime");
            entity.Property(e => e.Vatperc).HasColumnName("VATPerc");
            entity.Property(e => e.Yr)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("10-11");
        });

        modelBuilder.Entity<QualityProcessCheckDefectDg>(entity =>
        {
            entity.HasKey(e => e.QpcdefectDgid);

            entity.ToTable("QualityProcessCheckDefectDG");

            entity.Property(e => e.QpcdefectDgid).HasColumnName("QPCDefectDGId");
            entity.Property(e => e.Instrument)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Pccode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PCCode");
            entity.Property(e => e.Qdccode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("QDCCode");
            entity.Property(e => e.QualityProcessCheckerDgid).HasColumnName("QualityProcessCheckerDGId");

            entity.HasOne(d => d.QualityProcessCheckerDg).WithMany(p => p.QualityProcessCheckDefectDgs)
                .HasForeignKey(d => d.QualityProcessCheckerDgid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QualityProcessCheckDefectDG_QualityProcessCheckerDG");
        });

        modelBuilder.Entity<QualityProcessCheckerDetailsDg>(entity =>
        {
            entity.HasKey(e => e.QpcdetailsDgid);

            entity.ToTable("QualityProcessCheckerDetailsDG");

            entity.Property(e => e.QpcdetailsDgid).HasColumnName("QPCDetailsDGId");
            entity.Property(e => e.CheckerRemark)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.OkNok)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("OK_NOK");
            entity.Property(e => e.QualityProcessCheckerDgid).HasColumnName("QualityProcessCheckerDGId");
            entity.Property(e => e.StageWiseQcid).HasColumnName("StageWiseQCId");

            entity.HasOne(d => d.QualityProcessCheckerDg).WithMany(p => p.QualityProcessCheckerDetailsDgs)
                .HasForeignKey(d => d.QualityProcessCheckerDgid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QualityProcessCheckerDetailsDG_QualityProcessCheckerDG");
        });

        modelBuilder.Entity<QualityProcessCheckerDg>(entity =>
        {
            entity.HasKey(e => e.QualityProcessCheckerDgid).HasName("PK__QualityP__93C1707142C357FC");

            entity.ToTable("QualityProcessCheckerDG");

            entity.Property(e => e.QualityProcessCheckerDgid).HasColumnName("QualityProcessCheckerDGId");
            entity.Property(e => e.JobCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Jpriority).HasColumnName("JPriority");
            entity.Property(e => e.PartCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.QualityStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StageName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SilCladdingRate>(entity =>
        {
            entity.HasKey(e => new { e.Kva, e.Model, e.CompanyCode }).HasName("PK_SilCladdingrate_1");

            entity.ToTable("SilCladdingRate");

            entity.Property(e => e.Kva)
                .HasMaxLength(50)
                .HasColumnName("KVA");
            entity.Property(e => e.Model)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.CompanyCode).HasMaxLength(50);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Auth).HasDefaultValue(true);
            entity.Property(e => e.Discard).HasDefaultValue(true);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<StageWiseQualityCheckList>(entity =>
        {
            entity.HasKey(e => e.StageWiseQcid).HasName("PK__StageWis__FFE308E77544A9CB");

            entity.ToTable("StageWiseQualityCheckList");

            entity.Property(e => e.StageWiseQcid).HasColumnName("StageWiseQCId");
            entity.Property(e => e.CheckerAuthRemark)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FromKva)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("FromKVA");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MakerRemark)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Pccode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PCCode");
            entity.Property(e => e.StageName)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ToKva)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("ToKVA");
        });

        modelBuilder.Entity<StageWiseQualityCheckListDetail>(entity =>
        {
            entity.HasKey(e => new { e.StageWiseQcid, e.SrNo }).HasName("PK_StageWiseQCDetails");

            entity.Property(e => e.StageWiseQcid).HasColumnName("StageWiseQCId");
            entity.Property(e => e.Observation)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.OkNok)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("OK-NOK");
            entity.Property(e => e.QualityProcessCheckpoint)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Specification)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.SubAssemblyPart)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.StageWiseQc).WithMany(p => p.StageWiseQualityCheckListDetails)
                .HasForeignKey(d => d.StageWiseQcid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StageWiseQCDetails_Master");
        });

        modelBuilder.Entity<Stockwip>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("STOCKWIP");

            entity.HasIndex(e => new { e.FromProfitCenterCode, e.IssueDate, e.ReceivedDate }, "_dta_index_STOCKWIP_12_904142762__K1_K4_K8_2_5_6_9_10").HasFillFactor(80);

            entity.HasIndex(e => new { e.PartCode, e.FromProfitCenterCode, e.ToProfitCenterCode, e.ReceivedDate }, "_dta_index_STOCKWIP_12_904142762__K2_K1_K6_K8_3_4_5_7_9").HasFillFactor(80);

            entity.HasIndex(e => new { e.PartCode, e.IssueDate, e.ReceivedDate }, "_dta_index_STOCKWIP_12_904142762__K2_K4_K8_1_5_6_9").HasFillFactor(80);

            entity.HasIndex(e => new { e.PartCode, e.ReceivedDate }, "_dta_index_STOCKWIP_12_904142762__K2_K8_1_3_4_5_6_7_9").HasFillFactor(80);

            entity.HasIndex(e => new { e.ToProfitCenterCode, e.IssueDate, e.ReceivedDate }, "_dta_index_STOCKWIP_12_904142762__K6_K4_K8_1_2_5_9_10").HasFillFactor(80);

            entity.HasIndex(e => new { e.ToProfitCenterCode, e.ReceivedDate, e.IssueDate }, "_dta_index_STOCKWIP_14_904142762__K6_K8_K4").HasFillFactor(80);

            entity.HasIndex(e => new { e.ReceivedQty, e.ReceivedDate, e.PartCode, e.ToProfitCenterCode }, "_dta_index_STOCKWIP_14_904142762__K9_K8_K2_K6").HasFillFactor(80);

            entity.Property(e => e.FromProfitCenterCode).HasMaxLength(50);
            entity.Property(e => e.Grade)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.IssueCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.IssueDate).HasColumnType("datetime");
            entity.Property(e => e.LotNo)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PanelTypeId).HasColumnName("PanelTypeID");
            entity.Property(e => e.PartCode).HasMaxLength(50);
            entity.Property(e => e.ReceivedCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.ReceivedDate).HasColumnType("datetime");
            entity.Property(e => e.StageName)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("0");
            entity.Property(e => e.StockType).HasDefaultValueSql("('0')");
            entity.Property(e => e.ToProfitCenterCode).HasMaxLength(50);
        });

        modelBuilder.Entity<SupplierPriceListChanged>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("SupplierPriceListChanged");

            entity.Property(e => e.ChangeDateTime).HasColumnType("datetime");
            entity.Property(e => e.EmployeeCode).HasMaxLength(50);
            entity.Property(e => e.PartCode).HasMaxLength(50);
            entity.Property(e => e.SupplierCode).HasMaxLength(50);
        });

        modelBuilder.Entity<TestReport>(entity =>
        {
            entity.HasKey(e => e.Trcode);

            entity.ToTable("TestReport");

            entity.Property(e => e.Trcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TRCode");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Alternator)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Ambtemp)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL")
                .HasColumnName("AMBTemp");
            entity.Property(e => e.Auth).HasDefaultValue(true);
            entity.Property(e => e.Br)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL")
                .HasColumnName("BR");
            entity.Property(e => e.CompanyCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DgendTime)
                .HasColumnType("datetime")
                .HasColumnName("DGEndTime");
            entity.Property(e => e.DgstartTime)
                .HasColumnType("datetime")
                .HasColumnName("DGStartTime");
            entity.Property(e => e.Distatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("DIStatus");
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.EngineModel)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Frequency)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Hct).HasColumnName("HCT");
            entity.Property(e => e.Hp)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL")
                .HasColumnName("HP");
            entity.Property(e => e.Hwt).HasColumnName("HWT");
            entity.Property(e => e.Kw)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL")
                .HasColumnName("KW");
            entity.Property(e => e.Llop).HasColumnName("LLOP");
            entity.Property(e => e.MachineNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.MaxSrNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Osd).HasColumnName("OSD");
            entity.Property(e => e.Pdirstatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("PDIRStatus");
            entity.Property(e => e.Pf)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL")
                .HasColumnName("PF");
            entity.Property(e => e.Ph)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL")
                .HasColumnName("PH");
            entity.Property(e => e.ProcessCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Qastatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("QAStatus");
            entity.Property(e => e.RatedAmps)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL")
                .HasColumnName("RatedAMPS");
            entity.Property(e => e.RatedKva)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL")
                .HasColumnName("RatedKVA");
            entity.Property(e => e.RatedVolt)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("RatedVOLT");
            entity.Property(e => e.Rdiscard)
                .HasDefaultValue(true)
                .HasColumnName("RDiscard");
            entity.Property(e => e.Remark)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasDefaultValue("NIL");
            entity.Property(e => e.RevTrcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("RevTRCode");
            entity.Property(e => e.RoomTempreture)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Rpmregulation)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL")
                .HasColumnName("RPMRegulation");
            entity.Property(e => e.Ry)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL")
                .HasColumnName("RY");
            entity.Property(e => e.Speed)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Tpsstatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength()
                .HasColumnName("TPSStatus");
            entity.Property(e => e.TrendTime)
                .HasColumnType("datetime")
                .HasColumnName("TREndTime");
            entity.Property(e => e.TrstartTime)
                .HasColumnType("datetime")
                .HasColumnName("TRStartTime");
            entity.Property(e => e.VoltageRegulation)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Yb)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NIL")
                .HasColumnName("YB");
            entity.Property(e => e.Yr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("10-11");
        });

        modelBuilder.Entity<Uom>(entity =>
        {
            entity.HasKey(e => e.Uid);

            entity.ToTable("UOM");

            entity.Property(e => e.Uid)
                .HasMaxLength(50)
                .HasColumnName("UID");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasComment("1 - Active");
            entity.Property(e => e.AliseName)
                .HasMaxLength(50)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Auth)
                .HasDefaultValue(true)
                .HasComment("1 - A 0- UA");
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.Remark)
                .HasMaxLength(300)
                .HasDefaultValue("NIL");
            entity.Property(e => e.Uname)
                .HasMaxLength(50)
                .HasColumnName("UName");
            entity.Property(e => e.UnitTypeId)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("UnitTypeID");
        });

        modelBuilder.Entity<Video>(entity =>
        {
            entity.ToTable("videos");

            entity.Property(e => e.VideoId)
                .HasComment("1")
                .HasColumnName("video_id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.EmpCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValueSql("((0))")
                .HasColumnName("emp_code");
            entity.Property(e => e.EngSrNo)
                .HasMaxLength(50)
                .HasColumnName("eng_sr_no");
            entity.Property(e => e.PdirVideoName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("pdir_video_name");
            entity.Property(e => e.PdirVideoType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("N")
                .IsFixedLength()
                .HasColumnName("pdir_video_type");
            entity.Property(e => e.Sysdt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("sysdt");
            entity.Property(e => e.TrVideoName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("tr_video_name");
            entity.Property(e => e.TrVideoType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("N")
                .IsFixedLength()
                .HasColumnName("tr_video_type");
            entity.Property(e => e.VideoPath)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("video_path");
        });

        modelBuilder.Entity<YearEnd>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("YearEnd");

            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.MarqueeText)
                .HasMaxLength(500)
                .HasDefaultValue("-");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.SysVersion)
                .HasMaxLength(50)
                .HasDefaultValue("-");
        });

        modelBuilder.Entity<_6m>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("6M");

            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Dt).HasColumnType("datetime");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(20);
            entity.Property(e => e.Remark).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
