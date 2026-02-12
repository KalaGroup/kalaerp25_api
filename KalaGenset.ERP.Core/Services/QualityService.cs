using KalaGenset.ERP.Core.DTO;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.DbContexts;
using KalaGenset.ERP.Data.Models;
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

        //public async Task<bool> SaveCalibrationMasterAsync(CalibrationMasterRequest request)
        //{
        //    using var transaction = await _context.Database.BeginTransactionAsync();
        //    try
        //    {
        //        var calibrationEntries = request.Entries.Select(entry => new CalibrationMst
        //        {
        //            CompanyId = request.CompanyId,
        //            PartCode = entry.partCode,
        //            Type = entry.Type,
        //            IdNo = entry.IdNo,
        //            SrNo = entry.SrNo,
        //            Make = entry.Make,
        //            Range = entry.Range,
        //            Unit = entry.Unit,
        //            Lc = entry.LC,
        //            Location = entry.Location,
        //            CalDate = entry.CalDate,
        //            DueDate = entry.DueDate,
        //            MakerRemark = request.MakerRemark,
        //            IsActive = true,
        //        }).ToList();

        //        await _context.CalibrationMsts.AddRangeAsync(calibrationEntries);
        //        await _context.SaveChangesAsync();
        //        await transaction.CommitAsync();

        //        return true;
        //    }
        //    catch
        //    {
        //        await transaction.RollbackAsync();
        //        throw;
        //    }
        //}

        public async Task<List<CalibrationMst>> GetUnauthorizedCalibrationDataAsync(int companyId)
        {
            return await _context.CalibrationMsts
                .Where(x => x.CompanyId == companyId && x.Auth == false)
                .ToListAsync();
        }

        public async Task<bool> SaveCalibrationMasterAsync(CalibrationMasterRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
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
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
