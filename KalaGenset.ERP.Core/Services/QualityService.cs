using KalaGenset.ERP.Core.DTO;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.DbContexts;
using KalaGenset.ERP.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
                var kaizenSheetNo = await GenerateKaizenSheetNo();

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
        public async Task<string> GenerateKaizenSheetNo()
        {
            try
            {
                var year = DateTime.Now.Year.ToString();
                var prefix = $"KZ-{year}-";

                // Get the max number for current year
                var lastSheet = await _context.KaizenSheetMasters
                    .Where(k => k.KaizenSheetNo.StartsWith(prefix))
                    .OrderByDescending(k => k.KaizenSheetNo)
                    .Select(k => k.KaizenSheetNo)
                    .FirstOrDefaultAsync();

                int nextNum = 1;
                if (lastSheet != null)
                {
                    var lastNumStr = lastSheet.Substring(prefix.Length);
                    if (int.TryParse(lastNumStr, out int lastNum))
                    {
                        nextNum = lastNum + 1;
                    }
                }

                return $"{prefix}{nextNum:D4}";
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
    }
}
 