using KalaGenset.ERP.Core.DTO;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.DbContexts;
using KalaGenset.ERP.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace KalaGenset.ERP.Core.Services
{
    public class QualityService : IQuality
    {
        private readonly KalaDbContext _context;
        /// <summary>
        /// Initializes a new instance of the <see cref="QualityService"/> class.
        /// </summary>
        /// <param name="context">The database context for accessing ERP data.</param>

        public QualityService(KalaDbContext context)
        {
            _context = context;
        }

        public IEnumerable<CompanyDTO> FetchCompanyDetails()
        {
            var result = from company in _context.Companies
                         where company.Active == true
                         select new CompanyDTO
                         {
                             CID = company.Cid,
                             // CCode = company.Ccode,
                             CName = company.Cname
                         };

            return result.ToList();
        }

        public IEnumerable<PCNameForMTFScanDTO> FetchPCNames()
        {
            var result = from pc in _context.ProfitCenters
                         where pc.Active == true
                         select new PCNameForMTFScanDTO
                         {
                             PCName = pc.Pcname,
                             PCCode = pc.Pccode,
                         };
            return result.ToList();
        }

        public IEnumerable<PartcodeForCalibrationDTO> FetchPartcodesForCalibration()
        {
            var result = from part in _context.Parts
                         where part.CategoryId == "092" && part.Active == true
                         select new PartcodeForCalibrationDTO
                         {
                             partcode = part.PartCode,
                             instrument = part.PartDesc
                         };
            return result.ToList();
        }

        public async Task<List<CalibrationMst>> GetUnauthorizedCalibrationDataAsync(int companyId)
        {
            return await _context.CalibrationMsts
                .Where(x => x.CompanyId == companyId && x.Auth == false)
                .ToListAsync();
        }

        public async Task<bool> SaveCalibrationMasterAsync(CalibrationMasterRequest request)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (request.Designation == "checker")
                    {
                        // Update existing records
                        foreach (var entry in request.Entries)
                        {
                            var existing = await _context.CalibrationMsts
                                .FirstOrDefaultAsync(x => x.InstrumentId == entry.InstrumentId);

                            if (existing != null)
                            {
                                existing.PartCode = entry.partCode;
                                existing.Type = entry.Type;
                                existing.IdNo = entry.IdNo;
                                existing.SrNo = entry.SrNo;
                                existing.Make = entry.Make;
                                existing.Range = entry.Range;
                                existing.Unit = entry.Unit;
                                existing.Lc = entry.LC;
                                existing.Location = entry.Location;
                                existing.CalDate = entry.CalDate;
                                existing.DueDate = entry.DueDate;
                                existing.CheckerRemark = request.CheckerRemark;
                                existing.Auth = true;
                            }
                        }
                    }
                    else
                    {
                        // Insert new records (Maker)
                        var calibrationEntries = request.Entries.Select(entry => new CalibrationMst
                        {
                            CompanyId = request.CompanyId,
                            PartCode = entry.partCode,
                            Type = entry.Type,
                            IdNo = entry.IdNo,
                            SrNo = entry.SrNo,
                            Make = entry.Make,
                            Range = entry.Range,
                            Unit = entry.Unit,
                            Lc = entry.LC,
                            Location = entry.Location,
                            CalDate = entry.CalDate,
                            DueDate = entry.DueDate,
                            MakerRemark = request.MakerRemark,
                            IsActive = true,
                        }).ToList();

                        await _context.CalibrationMsts.AddRangeAsync(calibrationEntries);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();                   
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
            return true;
        }

        public async Task<List<DivisionCodeAnd_NameDTO>> GetDivisonCOdeAndNameFromDB()
        {
            return await _context.Divisions
                 .Where(x => x.Active == true)
                 .Select(x => new DivisionCodeAnd_NameDTO
                 {
                     DivisionId = x.DivisionId,
                     DivisionName = x.Division1
                 })
                 .ToListAsync();
        }

        public async Task<List<DepartmentCodeAndNameDTO>> GetDepartmentsByDivisionCodeAsync(int divisionId)
        {
            return await _context.DepartmentMsts
                .Where(x => x.DivisionId == divisionId && x.Active == true)
                .Select(x => new DepartmentCodeAndNameDTO
                {
                    DepartmentCode = x.Dcode,
                    DepartmentName = x.Dname
                })
                .ToListAsync();
        }

        public async Task<List<WorkstationCodeAndNameDTO>> GetWorkstationsByDepartmentCodeAsync()
        {
            return await _context.WorkStations
                .Where(x => x.Active == true)
                .Select(x => new WorkstationCodeAndNameDTO
                {
                    WorkstationCode = x.Wkcode,
                    WorkstationName = x.WorkStationName
                })
                .ToListAsync();
        }

        /// <summary>
        /// Create a new Kaizen Sheet record
        /// </summary>
        public async Task<string> CreateKaizenSheet(CreateKaizenSheetRequest request)
        {
            try
            {
                // Generate KaizenSheetNo
                var kaizenSheetNo = await GenerateKaizenSheetNo(request.CompanyId);

                var entity = new KaizenSheetMaster
                {
                    KaizenSheetNo = kaizenSheetNo,

                    // Header
                    DivisionId = request.DivisionId,
                    DivisionName = request.DivisionName,
                    DepartmentCode = request.DepartmentCode,
                    DepartmentName = request.DepartmentName,
                    WorkstationCode = request.WorkstationCode,
                    WorkstationName = request.WorkstationName,
                    KaizenTheme = request.KaizenTheme,
                    KaizenInitiationDate = DateOnly.Parse(request.KaizenInitiationDate),
                    CompletionDate = string.IsNullOrEmpty(request.CompletionDate)
                        ? null
                        : DateOnly.Parse(request.CompletionDate),

                    // Problem 5W2H
                    ProblemWhat = request.ProblemWhat,
                    ProblemWhen = request.ProblemWhen,
                    ProblemWhere = request.ProblemWhere,
                    ProblemWho = request.ProblemWho,
                    ProblemWhy = request.ProblemWhy,
                    ProblemHow = request.ProblemHow,
                    ProblemHowMuch = request.ProblemHowMuch,

                    // RCA 5 Why
                    RcaWhy1 = request.RcaWhy1,
                    RcaWhy2 = request.RcaWhy2,
                    RcaWhy3 = request.RcaWhy3,
                    RcaWhy4 = request.RcaWhy4,
                    RcaWhy5 = request.RcaWhy5,

                    // Idea
                    Idea = request.Idea,
                    IdeaRemark = request.IdeaRemark,

                    // Countermeasure
                    CountermeasureRemark = request.CountermeasureRemark,

                    // Result & Deployment
                    Result = request.Result,
                    Improvement = request.Improvement,
                    Benefit = request.Benefit,
                    InvestmentArea = request.InvestmentArea,
                    SavingArea = request.SavingArea,
                    HorizontalDeployment = request.HorizontalDeployment,

                    // Sustenance
                    SustenanceWhatToDo = request.SustenanceWhatToDo,
                    SustenanceHowToDo = request.SustenanceHowToDo,
                    SustenanceFrequency = request.SustenanceFrequency,

                    // File paths
                    BeforePhotoPath = request.BeforePhotoPath,
                    BeforePhotoName = request.BeforePhotoName,
                    AfterPhotoPath = request.AfterPhotoPath,
                    AfterPhotoName = request.AfterPhotoName,
                    ImpactGraphPath = request.ImpactGraphPath,
                    ImpactGraphName = request.ImpactGraphName,

                    // Data Submitted
                    DataSubmittedBy = request.DataSubmittedBy,
                    DataSubmittedOn = string.IsNullOrEmpty(request.DataSubmittedOn)
                        ? null
                        : DateOnly.Parse(request.DataSubmittedOn),

                    // Audit defaults
                    IsActive = true,
                    IsDiscard = false,
                    IsAuth = false
                };

                _context.KaizenSheetMasters.Add(entity);
                await _context.SaveChangesAsync();

                // ✅ return generated sheet number
                return kaizenSheetNo;
            }
            catch (Exception ex)
            {
                // optional: log error here
                throw new Exception("Error while creating Kaizen Sheet.", ex);
            }
        }

        /// <summary>
        /// Generate next KaizenSheetNo: KZ-YYYY-NNNN
        /// </summary>
        public async Task<string> GenerateKaizenSheetNo(string companyCode)
        {
            // Fetch financial year from DB (e.g., "25-26")
            var yearEnd = await _context.YearEnds
                .Select(y => $"{y.StartDate:yy}-{y.EndDate:yy}")
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(yearEnd))
                throw new Exception("Financial year not configured in YearEnd table.");

            // Prefix: KSN/25-26/01
            var prefix = $"KSN/{yearEnd}/{companyCode}";

            // Get max serial number for this company in this financial year
            var lastSheet = await _context.KaizenSheetMasters
                .Where(k => k.KaizenSheetNo.StartsWith(prefix))
                .OrderByDescending(k => k.KaizenSheetNo)
                .Select(k => k.KaizenSheetNo)
                .FirstOrDefaultAsync();

            int nextNum = 1;

            if (lastSheet != null)
            {
                // Extract serial part after prefix → e.g., "000001" from "KSN/25-26/01000001"
                var serialStr = lastSheet.Substring(prefix.Length);
                if (int.TryParse(serialStr, out int lastNum))
                {
                    nextNum = lastNum + 1;
                }
            }

            // Result: KSN/25-26/01000001
            return $"{prefix}{nextNum:D6}";
        }

        public async Task<List<KaizenSheetListResponse>> GetAllKaizenSheets()
        {
            return await _context.KaizenSheetMasters
                .Where(k => k.IsActive && !k.IsDiscard)
                .OrderByDescending(k => k.Id)
                .Select(k => new KaizenSheetListResponse
                {
                    Id = k.Id,
                    KaizenSheetNo = k.KaizenSheetNo,
                    DivisionName = k.DivisionName,
                    DepartmentName = k.DepartmentName,
                    WorkstationName = k.WorkstationName,
                    KaizenTheme = k.KaizenTheme,
                    KaizenInitiationDate = k.KaizenInitiationDate.ToString("yyyy-MM-dd"),
                    CompletionDate = k.CompletionDate.HasValue ? k.CompletionDate.Value.ToString("yyyy-MM-dd") : null,
                    Result = k.Result,
                    Improvement = k.Improvement,
                    DataSubmittedBy = k.DataSubmittedBy,
                    IsActive = k.IsActive,
                    IsAuth = k.IsAuth
                })
                .ToListAsync();
        }

        public async Task<List<KaizenSheetFullResponse>> GetAllKaizenSheetsFull()
        {
            return await _context.KaizenSheetMasters
                .Where(k => k.IsActive && !k.IsDiscard)
                .OrderByDescending(k => k.Id)
                .Select(k => new KaizenSheetFullResponse
                {
                    Id = k.Id,
                    KaizenSheetNo = k.KaizenSheetNo,
                    DivisionId = k.DivisionId,
                    DivisionName = k.DivisionName,
                    DepartmentCode = k.DepartmentCode,
                    DepartmentName = k.DepartmentName,
                    WorkstationCode = k.WorkstationCode,
                    WorkstationName = k.WorkstationName,
                    KaizenTheme = k.KaizenTheme,
                    KaizenInitiationDate = k.KaizenInitiationDate.ToString("yyyy-MM-dd"),
                    CompletionDate = k.CompletionDate.HasValue ? k.CompletionDate.Value.ToString("yyyy-MM-dd") : null,
                    ProblemWhat = k.ProblemWhat,
                    ProblemWhen = k.ProblemWhen,
                    ProblemWhere = k.ProblemWhere,
                    ProblemWho = k.ProblemWho,
                    ProblemWhy = k.ProblemWhy,
                    ProblemHow = k.ProblemHow,
                    ProblemHowMuch = k.ProblemHowMuch,
                    BeforePhotoPath = k.BeforePhotoPath,
                    BeforePhotoName = k.BeforePhotoName,
                    AfterPhotoPath = k.AfterPhotoPath,
                    AfterPhotoName = k.AfterPhotoName,
                    RcaWhy1 = k.RcaWhy1,
                    RcaWhy2 = k.RcaWhy2,
                    RcaWhy3 = k.RcaWhy3,
                    RcaWhy4 = k.RcaWhy4,
                    RcaWhy5 = k.RcaWhy5,
                    Idea = k.Idea,
                    IdeaRemark = k.IdeaRemark,
                    CountermeasureRemark = k.CountermeasureRemark,
                    Result = k.Result,
                    Improvement = k.Improvement,
                    Benefit = k.Benefit,
                    InvestmentArea = k.InvestmentArea,
                    SavingArea = k.SavingArea,
                    HorizontalDeployment = k.HorizontalDeployment,
                    ImpactGraphPath = k.ImpactGraphPath,
                    ImpactGraphName = k.ImpactGraphName,
                    SustenanceWhatToDo = k.SustenanceWhatToDo,
                    SustenanceHowToDo = k.SustenanceHowToDo,
                    SustenanceFrequency = k.SustenanceFrequency,
                    DataSubmittedBy = k.DataSubmittedBy,
                    DataSubmittedOn = k.DataSubmittedOn.HasValue ? k.DataSubmittedOn.Value.ToString("yyyy-MM-dd") : null,
                    IsActive = k.IsActive,
                    IsDiscard = k.IsDiscard,
                    IsAuth = k.IsAuth
                })
                .ToListAsync();
        }

        public async Task<bool> DeleteKaizenSheet(int id)
        {
            var entity = await _context.KaizenSheetMasters.FindAsync(id);
            if (entity == null) return false;

            entity.IsActive = false;
            entity.IsDiscard = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> UpdateKaizenSheet(int id, CreateKaizenSheetRequest request)
        {
            var entity = await _context.KaizenSheetMasters.FindAsync(id);
            if (entity == null)
                throw new Exception("Kaizen sheet not found");

            // Header
            entity.DivisionId = request.DivisionId;
            entity.DivisionName = request.DivisionName;
            entity.DepartmentCode = request.DepartmentCode;
            entity.DepartmentName = request.DepartmentName;
            entity.WorkstationCode = request.WorkstationCode;
            entity.WorkstationName = request.WorkstationName;
            entity.KaizenTheme = request.KaizenTheme;
            entity.KaizenInitiationDate = DateOnly.Parse(request.KaizenInitiationDate);
            entity.CompletionDate = string.IsNullOrEmpty(request.CompletionDate)
                ? null : DateOnly.Parse(request.CompletionDate);

            // 5W2H
            entity.ProblemWhat = request.ProblemWhat;
            entity.ProblemWhen = request.ProblemWhen;
            entity.ProblemWhere = request.ProblemWhere;
            entity.ProblemWho = request.ProblemWho;
            entity.ProblemWhy = request.ProblemWhy;
            entity.ProblemHow = request.ProblemHow;
            entity.ProblemHowMuch = request.ProblemHowMuch;

            // RCA
            entity.RcaWhy1 = request.RcaWhy1;
            entity.RcaWhy2 = request.RcaWhy2;
            entity.RcaWhy3 = request.RcaWhy3;
            entity.RcaWhy4 = request.RcaWhy4;
            entity.RcaWhy5 = request.RcaWhy5;

            // Idea & Countermeasure
            entity.Idea = request.Idea;
            entity.IdeaRemark = request.IdeaRemark;
            entity.CountermeasureRemark = request.CountermeasureRemark;

            // Result & Deployment
            entity.Result = request.Result;
            entity.Improvement = request.Improvement;
            entity.Benefit = request.Benefit;
            entity.InvestmentArea = request.InvestmentArea;
            entity.SavingArea = request.SavingArea;
            entity.HorizontalDeployment = request.HorizontalDeployment;

            // Files — only update if new path provided
            if (!string.IsNullOrEmpty(request.BeforePhotoPath))
            {
                entity.BeforePhotoPath = request.BeforePhotoPath;
                entity.BeforePhotoName = request.BeforePhotoName;
            }
            if (!string.IsNullOrEmpty(request.AfterPhotoPath))
            {
                entity.AfterPhotoPath = request.AfterPhotoPath;
                entity.AfterPhotoName = request.AfterPhotoName;
            }
            if (!string.IsNullOrEmpty(request.ImpactGraphPath))
            {
                entity.ImpactGraphPath = request.ImpactGraphPath;
                entity.ImpactGraphName = request.ImpactGraphName;
            }

            // Sustenance
            entity.SustenanceWhatToDo = request.SustenanceWhatToDo;
            entity.SustenanceHowToDo = request.SustenanceHowToDo;
            entity.SustenanceFrequency = request.SustenanceFrequency;

            // Submitted
            entity.DataSubmittedBy = request.DataSubmittedBy;
            entity.DataSubmittedOn = string.IsNullOrEmpty(request.DataSubmittedOn)
                ? null : DateOnly.Parse(request.DataSubmittedOn);

            await _context.SaveChangesAsync();

            return "Kaizen sheet updated successfully";
        }

        public async Task<bool> AuthorizeKaizenSheet(int id)
        {
            var entity = await _context.KaizenSheetMasters
                .FirstOrDefaultAsync(k => k.Id == id && k.IsActive && !k.IsDiscard && !k.IsAuth);

            if (entity == null) return false;

            entity.IsAuth = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<QualityCheckListReportRowDTO>> GetAllQualityCheckListsAsync()
        {
            // 1) Master rows + PCName via LEFT JOIN on ProfitCenters. Newest first.
            var masters = await (
                from q in _context.StageWiseQualityCheckLists
                join pc in _context.ProfitCenters
                    on q.Pccode equals pc.Pccode into pcj
                from pc in pcj.DefaultIfEmpty()
                orderby q.StageWiseQcid descending
                select new
                {
                    q.StageWiseQcid,
                    q.Pccode,
                    PCName = pc != null ? pc.Pcname : null,
                    q.StageName,
                    q.FromKva,
                    q.ToKva,
                    q.MakerRemark,
                    q.CheckerAuthRemark,
                    q.IsActive,
                    q.IsAuth,
                    q.IsDiscard
                }
            ).ToListAsync();

            if (masters.Count == 0)
                return new List<QualityCheckListReportRowDTO>();

            var ids = masters.Select(m => m.StageWiseQcid).ToList();

            // 2) All detail rows in a single round-trip via raw SQL.
            //    The EF entity doesn't expose `StageWiseQCDetailId` (only the
            //    composite PK on (StageWiseQcid, SrNo)), so we hit the column
            //    directly. Ids are server-side ints — safe to inline.
            //    [OK-NOK] is bracketed because the column name has a hyphen.
            var detailRaw = new List<(int Parent, QualityCheckListItemDTO Item)>();

            using (var conn = _context.Database.GetDbConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT StageWiseQCDetailId, StageWiseQCId, SrNo, " +
                    "       SubAssemblyPart, QualityProcessCheckpoint, " +
                    "       Specification, Observation, [OK-NOK] AS OkNok " +
                    "FROM   StageWiseQualityCheckListDetails " +
                    "WHERE  StageWiseQCId IN (" + string.Join(",", ids) + ") " +
                    "ORDER BY StageWiseQCId, SrNo";
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;

                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int parent = Convert.ToInt32(reader["StageWiseQCId"]);
                    detailRaw.Add((parent, new QualityCheckListItemDTO
                    {
                        StageWiseQcdetailId = Convert.ToInt32(reader["StageWiseQCDetailId"]),
                        SrNo = Convert.ToInt32(reader["SrNo"]),
                        SubAssemblyPart = reader["SubAssemblyPart"] as string,
                        QualityProcessCheckpoint = reader["QualityProcessCheckpoint"] as string,
                        Specification = reader["Specification"] as string,
                        Observation = reader["Observation"] as string,
                        OkNok = reader["OkNok"] as string,
                    }));
                }
            }

            var itemsByMaster = detailRaw
                .GroupBy(x => x.Parent)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Item).ToList());

            // 3) Build final DTOs with derived AuthStatus.
            //    Precedence: Discarded > Inactive > Authorized > Pending.
            return masters.Select(m =>
            {
                if (!itemsByMaster.TryGetValue(m.StageWiseQcid, out var items) || items == null)
                {
                    items = new List<QualityCheckListItemDTO>();
                }

                return new QualityCheckListReportRowDTO
                {
                    StageWiseQcid = m.StageWiseQcid,
                    Pccode = m.Pccode,
                    PCName = m.PCName,
                    StageName = m.StageName,
                    FromKva = m.FromKva,
                    ToKva = m.ToKva,
                    MakerRemark = m.MakerRemark,
                    CheckerAuthRemark = m.CheckerAuthRemark,
                    IsActive = m.IsActive,
                    IsAuth = m.IsAuth,
                    IsDiscard = m.IsDiscard,
                    ItemCount = items.Count,
                    AuthStatus = m.IsDiscard ? "Discarded"
                               : !m.IsActive ? "Inactive"
                               : m.IsAuth ? "Authorized"
                                          : "Pending",
                    Items = items
                };
            }).ToList();
        }

        public async Task<bool> UpdateStageWiseQualityCheckListAsync(UpdateStageWiseQualityCheckListRequest request)
        {
            bool ok = false;

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1) Load master via EF and update MakerRemark.
                    var master = await _context.StageWiseQualityCheckLists
                        .FirstOrDefaultAsync(x => x.StageWiseQcid == request.stageWiseQcid);
                    if (master == null)
                    {
                        ok = false;
                        return;
                    }
                    master.MakerRemark = request.makerRemark;

                    var sqlConn = (SqlConnection)_context.Database.GetDbConnection();
                    var sqlTran = (SqlTransaction)_context.Database.CurrentTransaction.GetDbTransaction();

                    // 2) Hard-delete details flagged by the user. Ids are server-trusted ints.
                    if (request.deletedItemIds != null && request.deletedItemIds.Count > 0)
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM StageWiseQualityCheckListDetails " +
                            "WHERE StageWiseQCDetailId IN (" + string.Join(",", request.deletedItemIds) + ")");
                    }

                    // 3) Push all remaining details' SrNo into a safe range so we can re-assign
                    //    SrNos in step 4 without violating the (StageWiseQCId, SrNo) PK.
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE StageWiseQualityCheckListDetails SET SrNo = SrNo + 10000 " +
                        "WHERE StageWiseQCId = @Id",
                        new SqlParameter("@Id", request.stageWiseQcid));

                    // 4) UPDATE existing details (matched by StageWiseQCDetailId) /
                    //    INSERT new ones (no id supplied by UI).
                    foreach (var item in request.checkpointItems ?? new List<UpdateStageWiseQualityCheckListDetailRequest>())
                    {
                        if (item.stageWiseQcdetailId.HasValue && item.stageWiseQcdetailId.Value > 0)
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "UPDATE StageWiseQualityCheckListDetails " +
                                "SET SrNo = @Sr, SubAssemblyPart = @Sap, " +
                                "    QualityProcessCheckpoint = @Qpc, Specification = @Sp, " +
                                "    Observation = @Ob, [OK-NOK] = @OkNok " +
                                "WHERE StageWiseQCDetailId = @Id",
                                new SqlParameter("@Sr", item.srNo),
                                new SqlParameter("@Sap", (object?)item.subAssemblyPart ?? DBNull.Value),
                                new SqlParameter("@Qpc", (object?)item.qualityProcessCheckpoint ?? DBNull.Value),
                                new SqlParameter("@Sp", (object?)item.specification ?? DBNull.Value),
                                new SqlParameter("@Ob", (object?)item.observation ?? DBNull.Value),
                                new SqlParameter("@OkNok", (object?)item.ok_nok ?? DBNull.Value),
                                new SqlParameter("@Id", item.stageWiseQcdetailId.Value));
                        }
                        else
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "INSERT INTO StageWiseQualityCheckListDetails" +
                                "(StageWiseQCId, SrNo, SubAssemblyPart, QualityProcessCheckpoint, " +
                                " Specification, Observation, [OK-NOK]) " +
                                "VALUES(@Mid, @Sr, @Sap, @Qpc, @Sp, @Ob, @OkNok)",
                                new SqlParameter("@Mid", request.stageWiseQcid),
                                new SqlParameter("@Sr", item.srNo),
                                new SqlParameter("@Sap", (object?)item.subAssemblyPart ?? DBNull.Value),
                                new SqlParameter("@Qpc", (object?)item.qualityProcessCheckpoint ?? DBNull.Value),
                                new SqlParameter("@Sp", (object?)item.specification ?? DBNull.Value),
                                new SqlParameter("@Ob", (object?)item.observation ?? DBNull.Value),
                                new SqlParameter("@OkNok", (object?)item.ok_nok ?? DBNull.Value));
                        }
                    }

                    // 5) If no details remain (user removed all), deactivate the master.
                    int remaining;
                    using (var cnt = new SqlCommand(
                        "SELECT COUNT(*) FROM StageWiseQualityCheckListDetails WHERE StageWiseQCId = @Id",
                        sqlConn, sqlTran))
                    {
                        cnt.Parameters.AddWithValue("@Id", request.stageWiseQcid);
                        remaining = Convert.ToInt32(await cnt.ExecuteScalarAsync());
                    }
                    if (remaining == 0) master.IsActive = false;

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                    ok = true;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            return ok;
        }

        public async Task<bool> CheckDuplicateQualityCheckListAsync(string pcCode, string stageName, string fromKva, string toKva)
        {
            if (!decimal.TryParse(fromKva, out var fromKvaDecimal))
                throw new Exception("Invalid FromKVA value");
            if (!decimal.TryParse(toKva, out var toKvaDecimal))
                throw new Exception("Invalid ToKVA value");

            // Discarded checklists with the same combo are not considered duplicates —
            // they're already in the "tombstone" state and shouldn't block a fresh one.
            return await _context.StageWiseQualityCheckLists
                .AnyAsync(x => x.Pccode == pcCode
                            && x.StageName == stageName
                            && x.FromKva == fromKvaDecimal
                            && x.ToKva == toKvaDecimal
                            && x.IsDiscard == false);
        }

        public async Task<bool> SoftDeleteStageWiseQualityCheckListAsync(int id)
        {
            var master = await _context.StageWiseQualityCheckLists
                .FirstOrDefaultAsync(x => x.StageWiseQcid == id);
            if (master == null) return false;

            // UI service contract: "POST — soft-delete a Quality Check List master (IsActive = false)".
            // Report row will show with AuthStatus = "Inactive".
            master.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AuthorizeStageWiseQualityCheckListAsync(int id, string? checkerRemark)
        {
            var master = await _context.StageWiseQualityCheckLists
                .FirstOrDefaultAsync(x => x.StageWiseQcid == id);
            if (master == null) return false;

            master.IsAuth = true;
            master.CheckerAuthRemark = checkerRemark?.Trim() ?? string.Empty;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevertAuthorizationStageWiseQualityCheckListAsync(int id)
        {
            var master = await _context.StageWiseQualityCheckLists
                .FirstOrDefaultAsync(x => x.StageWiseQcid == id);
            if (master == null) return false;

            master.IsAuth = false;
            master.CheckerAuthRemark = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SaveStageWiseQualityCheckListAsync(StageWiseQualityCheckListRequest request)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1) MASTER — PK auto-generated on SaveChangesAsync().
                var master = new StageWiseQualityCheckList
                {
                    Pccode = request.pcCode,
                    StageName = request.stageName,
                    FromKva = request.fromKVA,
                    ToKva = request.toKVA,
                    IsActive = true,
                    MakerRemark = request.makerRemark,
                };
                _context.StageWiseQualityCheckLists.Add(master);
                await _context.SaveChangesAsync();

                // 2) DETAILS — share the freshly minted master PK.
                var details = request.checkpointItems
                    .Select(it => new StageWiseQualityCheckListDetail
                    {
                        StageWiseQcid = master.StageWiseQcid,
                        SrNo = it.srNo,
                        SubAssemblyPart = it.subAssemblyPart,
                        QualityProcessCheckpoint = it.qualityProcessCheckpoint,
                        Specification = it.specification,
                        Observation = it.observation,
                        OkNok = it.ok_nok
                    })
                    .ToList();
                _context.StageWiseQualityCheckListDetails.AddRange(details);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                throw new Exception("Error saving Stage Wise Quality Check List", ex);
            }
        }
    }
}
 