using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KalaGenset.ERP.Core.Services
{
    public class LogisticService : ILogistic
    {
        private readonly KalaDbContext _context;
        public LogisticService(KalaDbContext context)
        {
            _context = context;
        }

        public async Task<List<PCNameForMTFScanDTO>> GetPCodeAllAsync(string PCCode, string ReqType)
        {
            try
            {
                var pcCodeParam = new SqlParameter("@PCCode", PCCode);
                var reqTypeParam = new SqlParameter("@ReqType", ReqType);

                var result = await _context.Database
                    .SqlQueryRaw<PCNameForMTFScanDTO>("EXEC GetPCCodeALL @PCCode, @ReqType", pcCodeParam, reqTypeParam)
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching PC Code details", ex);
            }
        }

        public async Task<List<MTFCodeAndNoDTO>> GetMTFCodeAndMTFNoAsync(string FPCCode, string TPCCode)
        {
            try
            {
                var fpcCodeParam = new SqlParameter("@FPCCode", FPCCode);
                var tpcCodeParam = new SqlParameter("@TPCCode", TPCCode);

                var result = await _context.Database
                    .SqlQueryRaw<MTFCodeAndNoDTO>("EXEC GetMTFCode @FPCCode, @TPCCode", fpcCodeParam, tpcCodeParam)
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error in GetMTFCodeAndMTFNoAsync");

                throw new Exception("Error fetching MTF Code & MTF No details", ex);
            }
        }

        public async Task<List<PartDescOfMTFScanDTO>> GetReqProductDtlAsync(string MTFCode)
        {
            try
            {
                var decodedMTF = Uri.UnescapeDataString(MTFCode);

                var mtfCodeParam = new SqlParameter("@ReqMTFCode", decodedMTF);

                var result = await _context.Database
                    .SqlQueryRaw<PartDescOfMTFScanDTO>("EXEC GetReqProdDetails @ReqMTFCode", mtfCodeParam)
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching Part Description", ex);
            }
        }

        public async Task<List<GetMTFSrNoDtsDTO>> GetMTFSrNoDtlAsync(string MTFCode)
        {
            try
            {
                var decodedMTF = Uri.UnescapeDataString(MTFCode);

                var mtfCodeParam = new SqlParameter("@MTFCode", decodedMTF);

                var result = await _context.Database
                    .SqlQueryRaw<GetMTFSrNoDtsDTO>("EXEC GetMtfSrNoDts @MTFCode", mtfCodeParam)
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching Part Description", ex);
            }
        }

        public async Task<string> SubmitMTFScanDetailsAsync(MTFScanSubmitRequest mtfScanSubmitRequest)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    foreach (var item in mtfScanSubmitRequest.MTFSerialNoDts)
                    {
                        var updateMTFDetailsSub = await _context.MtfdetailsSubs.FirstOrDefaultAsync(m => m.Mtfcode == mtfScanSubmitRequest.MtfCode && m.PartCode == item.Partcode && m.SerialNo == item.SerialNo);
                        if (updateMTFDetailsSub != null)
                        {
                            updateMTFDetailsSub.Trfstatus = "M";
                        }

                        var updateJobcardDetailsSub = await _context.JobCardDetailsSubs.FirstOrDefaultAsync(j => j.TransferCode == mtfScanSubmitRequest.MtfCode && j.SrNoPartCode == item.Partcode && j.SerialNo == item.SerialNo);
                        if (updateJobcardDetailsSub != null)
                        {
                            updateJobcardDetailsSub.TransferStatus = "D";
                        }
                        await _context.SaveChangesAsync();
                    }

                    var updateMemoExiseMfg = await _context.MemoExciseMfgs.FirstOrDefaultAsync(me => me.Mmtfcode == mtfScanSubmitRequest.MtfCode);
                    if (updateMemoExiseMfg != null)
                    {
                        updateMemoExiseMfg.MtfscanStatus = "D";
                    }

                    var decodedMTF = Uri.UnescapeDataString(mtfScanSubmitRequest.MtfCode);

                    var mtfCodeParam = new SqlParameter("@MTFCode", decodedMTF);

                    List<GetMTFDetailsResponseDTO> dsDetailsSub = new List<GetMTFDetailsResponseDTO>();

                    dsDetailsSub = await _context.Database
                    .SqlQueryRaw<GetMTFDetailsResponseDTO>("EXEC GetMTFDts @MTFCode", mtfCodeParam)
                    .ToListAsync();

                    if (dsDetailsSub != null)
                    {
                        foreach (var detail in dsDetailsSub)
                        {
                            string? alreadySave = await _context.Stockwips.Where(x =>
                                                 x.FromProfitCenterCode == detail.FPCCode &&
                                                 x.PartCode == detail.Partcode &&
                                                 x.ReceivedCode == mtfScanSubmitRequest.MtfCode.Trim() &&
                                                 x.ReceivedQty > 0 &&
                                                 x.ToProfitCenterCode == detail.TPCCode)
                                                 .Select(x => x.ReceivedCode)
                                                 .FirstOrDefaultAsync() ?? "";

                            if (alreadySave != null)
                            {
                                if (detail.Partcode.Substring(0, 3) == "001" || detail.Partcode.Substring(0, 3) == "002")
                                {

                                }
                                else if (detail.Partcode.Substring(0, 3) == "010")
                                {

                                }
                                else
                                {

                                }
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception("Error submitting MTF Scan details", ex);
                }
            });
            return mtfScanSubmitRequest.MtfCode;
        }
    }
}
