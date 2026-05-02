using System.Data;
using System.Data.Common;
using System.Globalization;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.Request.Logistic;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using static Azure.Core.HttpHeader;

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

                //var result = await _context.Database
                //    .SqlQueryRaw<PCNameForMTFScanDTO>("EXEC GetPCCodeALL @PCCode, @ReqType", pcCodeParam, reqTypeParam)
                //    .ToListAsync();

                var result = await _context.Database
                    .SqlQueryRaw<PCNameForMTFScanDTO>("EXEC GetPCCodeALL_Checker_Maker @PCCode, @ReqType", pcCodeParam, reqTypeParam)
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

                //var result = await _context.Database
                //    .SqlQueryRaw<MTFCodeAndNoDTO>("EXEC GetMTFCode @FPCCode, @TPCCode", fpcCodeParam, tpcCodeParam)
                //    .ToListAsync();

                var result = await _context.Database
                    .SqlQueryRaw<MTFCodeAndNoDTO>("EXEC GetMTFCode_Maker_Checker @FPCCode, @TPCCode", fpcCodeParam, tpcCodeParam)
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error in GetMTFCodeAndMTFNoAsync");

                throw new Exception("Error fetching MTF Code & MTF No details", ex);
            }
        }

        public async Task<List<ReqCodeForMTFScanDTO>> GetReqCodeForMTFAsync(string FPCCode, string TPCCode)
        {
            try
            {
                var fpcCodeParam = new SqlParameter("@FPCCode", FPCCode);
                var tpcCodeParam = new SqlParameter("@TPCCode", TPCCode);

                var result = await _context.Database
                    .SqlQueryRaw<ReqCodeForMTFScanDTO>("EXEC GetMTFWIPInternalReqCode @FPCCode, @TPCCode", fpcCodeParam, tpcCodeParam)
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching Requisition Code details", ex);
            }
        }

        //public async Task<List<PartDescOfMTFScanDTO>> GetReqProductDtlAsync(string MTFCode)
        //{
        //    try
        //    {
        //        var decodedMTF = Uri.UnescapeDataString(MTFCode);

        //        var mtfCodeParam = new SqlParameter("@ReqMTFCode", decodedMTF);

        //        var result = await _context.Database
        //            .SqlQueryRaw<PartDescOfMTFScanDTO>("EXEC GetReqProdDetails @ReqMTFCode", mtfCodeParam)
        //            .ToListAsync();

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Error fetching Part Description", ex);
        //    }
        //}

        public async Task<List<Dictionary<string, object>>> GetReqProductDtlAsync(string MTFCode)
        {
            var data = new List<Dictionary<string, object>>();

            try
            {
                var decodedMTF = Uri.UnescapeDataString(MTFCode);

                using (var conn = _context.Database.GetDbConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "GetReqProdDetails";
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 0;

                        cmd.Parameters.Add(new SqlParameter("@ReqMTFCode", SqlDbType.Char) { Value = decodedMTF });

                        if (conn.State == ConnectionState.Closed)
                            await conn.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                data.Add(row);
                            }
                        }
                    }
                }

                return data;
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

        public async Task<List<ReqDetailsForMTFDTO>> GetReqDetailsAsync(string PCCode, string strBomCode, string StrReqCode, double StrReqQty, double StrMTFQty)
        {
            if (StrReqQty == 0)
                throw new ArgumentException("StrReqQty cannot be zero (divide-by-zero).", nameof(StrReqQty));

            var results = new List<ReqDetailsForMTFDTO>();

            var conn = _context.Database.GetDbConnection();
            if (conn.State == ConnectionState.Closed)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "dbo.GetReqDetails_MTFWIPInternal";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 0;
            cmd.Parameters.Add(new SqlParameter("@PCCode", SqlDbType.NVarChar, 50) { Value = PCCode?.Trim() ?? string.Empty });
            cmd.Parameters.Add(new SqlParameter("@BomCode", SqlDbType.NVarChar, 50) { Value = strBomCode?.Trim() ?? string.Empty });
            cmd.Parameters.Add(new SqlParameter("@ReqCode", SqlDbType.NVarChar, 100) { Value = StrReqCode?.Trim() ?? string.Empty });
            cmd.Parameters.Add(new SqlParameter("@ReqQtySet", SqlDbType.Float) { Value = StrReqQty });
            cmd.Parameters.Add(new SqlParameter("@MTFQtySet", SqlDbType.Float) { Value = StrMTFQty });

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new ReqDetailsForMTFDTO
                {
                    PartDesc = SafeString(reader, "PartDesc"),
                    PartCode = SafeString(reader, "PartCode"),
                    UName = SafeString(reader, "UName"),
                    Uid = SafeString(reader, "Uid"),
                    KitQty = SafeDouble(reader, "KitQty"),
                    ReqQty = SafeDouble(reader, "ReqQty"),
                    PQty = SafeDouble(reader, "PQty"),
                    Stk = SafeDouble(reader, "Stk"),
                    MTFQty = SafeDouble(reader, "MTFQty"),
                    QtyAfterMTF = SafeDouble(reader, "QtyAfterMTF"),
                    Rate = SafeDouble(reader, "Rate"),
                    Amt = SafeDouble(reader, "Amt"),
                    SheetQty = SafeDouble(reader, "SheetQty"),
                    ConvUOMCode = SafeString(reader, "ConvUOMCode"),
                    Length = SafeDouble(reader, "Length"),
                    Width = SafeDouble(reader, "Width"),
                    Thickness = SafeDouble(reader, "Thickness"),
                    MOB = SafeString(reader, "MOB"),
                });
            }

            return results;
        }  

        // ─────────── Small utilities ───────────

        private static string SafeString(DbDataReader reader, string col)
        {
            var o = reader.GetOrdinal(col);
            return reader.IsDBNull(o) ? string.Empty : (reader.GetValue(o)?.ToString()?.Trim() ?? string.Empty);
        }

        private static double SafeDouble(DbDataReader reader, string col)
        {
            var o = reader.GetOrdinal(col);
            return reader.IsDBNull(o) ? 0 : Convert.ToDouble(reader.GetValue(o), CultureInfo.InvariantCulture);
        }

        public async Task<string> SubmitMTFWIPInternalAsync(MTFWIPInternalRequest req)
        {
            // ═══════════════════════════════════════════════════════════════════
            //  1. EARLY VALIDATION (no DB state change)
            // ═══════════════════════════════════════════════════════════════════
            if (req.ReqBalQty < req.MTFQty)
                return "MTF Qty Greater Then MTF Balance Qty!";

            var parts = (req.MTFDetails ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d =>
                {
                    var bits = d.Trim().Split(new[] { "-->" }, StringSplitOptions.None);
                    return new
                    {
                        PartCode = bits[0].Trim(),
                        MtfQty = double.Parse(bits[1].Trim(), CultureInfo.InvariantCulture),
                        Rate = double.Parse(bits[2].Trim(), CultureInfo.InvariantCulture),
                        StockQty = double.Parse(bits[3].Trim(), CultureInfo.InvariantCulture),
                    };
                })
                .ToList();

            var fromCompCode = req.FromPCCode.Trim().Substring(0, 2);
            var toCompCode = req.ToPCCode.Trim().Substring(0, 2);
            var isCrossCompany = fromCompCode == "23" && fromCompCode != toCompCode;
            var isInternalSameCompany = fromCompCode == "23" && fromCompCode == toCompCode;

            // Share EF Core's connection + transaction with raw ADO.NET
            var conn = (SqlConnection)_context.Database.GetDbConnection();
            if (conn.State == ConnectionState.Closed)
                await conn.OpenAsync();

            // ═══════════════════════════════════════════════════════════════════
            //  2. PRE-TRANSACTION VALIDATION
            //     - Stock sufficiency
            //     - SrNo availability for Eng(001)/Alt(002)/Bat(010)
            //     - GIR qty for non-SrNo parts on cross-company transfers
            // ═══════════════════════════════════════════════════════════════════
            foreach (var p in parts)
            {
                // Stock check (matches: DtsV[3] - DtsV[1] < 0)
                if (p.StockQty - p.MtfQty < 0)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT PartDesc+'-->'+PartCode AS Partdesc FROM Part WHERE Partcode = @pc AND Active = '1'";
                    cmd.Parameters.Add(new SqlParameter("@pc", p.PartCode));
                    var name = (await cmd.ExecuteScalarAsync())?.ToString() ?? "0";
                    return $"Insufficient Stock for Part : {name}!";
                }

                var prefix3 = p.PartCode.Length >= 3 ? p.PartCode.Substring(0, 3) : "";

                if (prefix3 == "001" || prefix3 == "002" || prefix3 == "010")
                {
                    // Call MTFSRNo_AgainstJobcard — count available SrNos by type
                    int engCount = 0, altCount = 0, batCount = 0;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "EXEC MTFSRNo_AgainstJobcard @pc, @qty, @req";
                        cmd.Parameters.Add(new SqlParameter("@pc", p.PartCode));
                        cmd.Parameters.Add(new SqlParameter("@qty", p.MtfQty));
                        cmd.Parameters.Add(new SqlParameter("@req", req.ReqCode.Trim()));
                        using var r = await cmd.ExecuteReaderAsync();
                        while (await r.ReadAsync())
                        {
                            var pc = r["PartCode"]?.ToString()?.Trim() ?? "";
                            if (pc.Length >= 3)
                            {
                                var pre = pc.Substring(0, 3);
                                if (pre == "001") engCount++;
                                else if (pre == "002") altCount++;
                                else if (pre == "010") batCount++;
                            }
                        }
                    }

                    string ShortageType() =>
                        prefix3 == "001" ? "Engine"
                        : prefix3 == "002" ? "Alternator"
                        : "Battery";

                    int available =
                        prefix3 == "001" ? engCount
                        : prefix3 == "002" ? altCount
                        : batCount;

                    double neededRaw = prefix3 == "010" ? Math.Round(p.MtfQty, 0) : p.MtfQty;
                    if (neededRaw > available)
                    {
                        using var cmd2 = conn.CreateCommand();
                        cmd2.CommandText = "SELECT partdesc FROM Part WHERE Partcode = @pc";
                        cmd2.Parameters.Add(new SqlParameter("@pc", p.PartCode));
                        var name = (await cmd2.ExecuteScalarAsync())?.ToString() ?? "0";
                        return $"{ShortageType()} SrNo Not available For DG {name}";
                    }
                }
                else if (isCrossCompany)
                {
                    // Non-Eng/Alt/Bat parts on cross-company: check GIR summary
                    bool anyRow = false;
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "EXEC MTFSRNo_GIRQty 'Summary', @pc";
                    cmd.Parameters.Add(new SqlParameter("@pc", p.PartCode));
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        anyRow = true;
                        var balQty = Convert.ToDouble(r["BalQty"], CultureInfo.InvariantCulture);
                        if (p.MtfQty > balQty)
                        {
                            var diff = p.MtfQty - balQty;
                            // read part desc on separate cmd — reader still open
                            await r.CloseAsync();
                            using var c2 = conn.CreateCommand();
                            c2.CommandText = "SELECT partdesc FROM Part WHERE Partcode = @pc";
                            c2.Parameters.Add(new SqlParameter("@pc", p.PartCode));
                            var name = (await c2.ExecuteScalarAsync())?.ToString() ?? "0";
                            return $"{diff} - Giir Qty not aviable For Product {name}";
                        }
                    }
                    if (!anyRow)
                    {
                        await r.CloseAsync();
                        using var c2 = conn.CreateCommand();
                        c2.CommandText = "SELECT partdesc FROM Part WHERE Partcode = @pc";
                        c2.Parameters.Add(new SqlParameter("@pc", p.PartCode));
                        var name = (await c2.ExecuteScalarAsync())?.ToString() ?? "0";
                        return $"{p.PartCode} - Giir Qty not aviable For Product {name}";
                    }
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            //  3. MAIN TRANSACTION
            // ═══════════════════════════════════════════════════════════════════
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {              
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var sqlTran = (SqlTransaction)transaction.GetDbTransaction();

                try
                {
                    // ── 3a. Fiscal year (inlined from yearEnd) ──
                    string fiscalYear;
                    using (var c = conn.CreateCommand())
                    {
                        c.Transaction = sqlTran;
                        c.CommandText = "SELECT SUBSTRING(CONVERT(VARCHAR(10), startdate, 103), 9, 2) + '-' + SUBSTRING(CONVERT(VARCHAR(10), enddate, 103), 9, 2) FROM yearend";
                        fiscalYear = (await c.ExecuteScalarAsync())?.ToString()?.Trim() ?? "";
                    }

                    // ── 3b. Generate MTF Code (inlined from GetMaxNo, with UPDLOCK+HOLDLOCK) ──
                    string mtfCode;
                    string mtfSerial;
                    {
                        int maxVal;
                        using (var c = conn.CreateCommand())
                        {
                            c.Transaction = sqlTran;
                            // UPDLOCK+HOLDLOCK prevents two concurrent Submits from reading the same MaxValue
                            c.CommandText = @"
                    SELECT ISNULL(MaxValue, 0)
                      FROM GetMaxCode WITH (UPDLOCK, HOLDLOCK)
                     WHERE TblName = @t AND CompCode = @c AND Prefix = @p AND Yr = @y";
                            c.Parameters.Add(new SqlParameter("@t", "MTF"));
                            c.Parameters.Add(new SqlParameter("@c", fromCompCode));
                            c.Parameters.Add(new SqlParameter("@p", "MTF"));
                            c.Parameters.Add(new SqlParameter("@y", fiscalYear));
                            var res = await c.ExecuteScalarAsync();
                            maxVal = res == null || res == DBNull.Value ? 0 : Convert.ToInt32(res);
                        }
                        mtfSerial = (maxVal + 1).ToString("D6");
                        mtfCode = $"MTF/{fiscalYear}/{fromCompCode}{mtfSerial}";

                        using (var c = conn.CreateCommand())
                        {
                            c.Transaction = sqlTran;
                            c.CommandText = @"
                    UPDATE GetMaxCode
                       SET MaxValue = @new
                     WHERE Prefix = @p AND TblName = @t AND CompCode = @c AND Yr = @y";
                            c.Parameters.Add(new SqlParameter("@new", mtfSerial));
                            c.Parameters.Add(new SqlParameter("@p", "MTF"));
                            c.Parameters.Add(new SqlParameter("@t", "MTF"));
                            c.Parameters.Add(new SqlParameter("@c", fromCompCode));
                            c.Parameters.Add(new SqlParameter("@y", fiscalYear));
                            await c.ExecuteNonQueryAsync();
                        }
                    }

                    // ── 3c. InsertPlanMTF ──
                    using (var c = conn.CreateCommand())
                    {
                        c.Transaction = sqlTran;
                        c.CommandText = "InsertPlanMTF";
                        c.CommandType = CommandType.StoredProcedure;
                        c.Parameters.AddWithValue("@MTFCode", mtfCode);
                        c.Parameters.AddWithValue("@MaxSrNo", mtfCode.Substring(10, 8));
                        c.Parameters.AddWithValue("@Dt", DateTime.Now);
                        c.Parameters.AddWithValue("@Yr", fiscalYear);
                        c.Parameters.AddWithValue("@FromProfitCenterCode", req.FromPCCode.Trim());
                        c.Parameters.AddWithValue("@ToProfitCenterCode", req.ToPCCode.Trim());
                        c.Parameters.AddWithValue("@ForProfitCenterCode", 0);
                        c.Parameters.AddWithValue("@RequisitionCode", req.ReqCode.Trim());
                        c.Parameters.AddWithValue("@CpyPartCode", req.ProdPartCode.Trim());
                        c.Parameters.AddWithValue("@PlanPartCode", "0");
                        c.Parameters.AddWithValue("@SCode", "0");
                        c.Parameters.AddWithValue("@PlanIssueQty", req.MTFQty);
                        c.Parameters.AddWithValue("@ReceivedDt", DateTime.Now);
                        c.Parameters.AddWithValue("@MTFStatus", isCrossCompany ? "P" : "D");
                        c.Parameters.AddWithValue("@CompanyCode", fromCompCode);
                        c.Parameters.AddWithValue("@Remark", req.Remark?.Trim() ?? "");
                        c.Parameters.AddWithValue("@WtPerUt", 0);
                        c.Parameters.AddWithValue("@SqftPerUt", 0);
                        await c.ExecuteNonQueryAsync();
                    }

                    // ── 3d. Per-part details loop ──
                    int srNo = 0;
                    foreach (var p in parts)
                    {
                        srNo++;
                        var prefix3 = p.PartCode.Length >= 3 ? p.PartCode.Substring(0, 3) : "";

                        // InsertPlanMTFDetails
                        string detailRemark =
                            toCompCode == "01" ? "WIP Internal Unit-1"
                            : toCompCode == "03" ? "WIP Internal Unit-4"
                            : "WIP Internal";

                        using (var c = conn.CreateCommand())
                        {
                            c.Transaction = sqlTran;
                            c.CommandText = "InsertPlanMTFDetails";
                            c.CommandType = CommandType.StoredProcedure;
                            c.Parameters.AddWithValue("@MTFCode", mtfCode);
                            c.Parameters.AddWithValue("@SrNo", srNo);
                            c.Parameters.AddWithValue("@PartCode", p.PartCode);
                            c.Parameters.AddWithValue("@PendingQty", 0);
                            c.Parameters.AddWithValue("@IssueQty", p.MtfQty);
                            c.Parameters.AddWithValue("@Remark", detailRemark);
                            c.Parameters.AddWithValue("@Rate", p.Rate);
                            c.Parameters.AddWithValue("@MemoStatus", "D");
                            c.Parameters.AddWithValue("@WtPerUt", 0);
                            c.Parameters.AddWithValue("@SqftPerUt", 0);
                            await c.ExecuteNonQueryAsync();
                        }

                        // StockWIP issue row
                        using (var c = conn.CreateCommand())
                        {
                            c.Transaction = sqlTran;
                            c.CommandText = @"
                    INSERT INTO StockWIP (FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType, FromProfitCenterCode_Act, ToProfitCenterCode_Act)
                    VALUES (@from, @pc, @mtf, @dt, @qty, @to, 0, @FromProfitCenterCode_Act, @ToProfitCenterCode_Act)";
                            c.Parameters.AddWithValue("@from", req.FromPCCode.Trim());
                            c.Parameters.AddWithValue("@pc", p.PartCode);
                            c.Parameters.AddWithValue("@mtf", mtfCode);
                            c.Parameters.AddWithValue("@dt", DateTime.Now);
                            c.Parameters.AddWithValue("@qty", p.MtfQty);
                            c.Parameters.AddWithValue("@to", req.ToPCCode.Trim());
                            c.Parameters.AddWithValue("@FromProfitCenterCode_Act", req.FromPCCode.Trim());
                            c.Parameters.AddWithValue("@ToProfitCenterCode_Act", req.ToPCCode.Trim());
                            await c.ExecuteNonQueryAsync();
                        }

                        if (prefix3 == "001" || prefix3 == "002" || prefix3 == "010")
                        {
                            // Fetch serial numbers again (original calls MTFSRNo_AgainstJobcard here)
                            var srRows = new List<(string SerialNo, string JobcardStatus, string GCode)>();
                            using (var c = conn.CreateCommand())
                            {
                                c.Transaction = sqlTran;
                                c.CommandText = "EXEC MTFSRNo_AgainstJobcard @pc, @qty, @req";
                                c.Parameters.Add(new SqlParameter("@pc", p.PartCode));
                                c.Parameters.Add(new SqlParameter("@qty", p.MtfQty));
                                c.Parameters.Add(new SqlParameter("@req", req.ReqCode.Trim()));
                                using var r = await c.ExecuteReaderAsync();
                                while (await r.ReadAsync())
                                {
                                    srRows.Add((
                                        r["SerialNo"]?.ToString()?.Trim() ?? "",
                                        r["JobcardStatus"]?.ToString()?.Trim() ?? "",
                                        r["GCode"]?.ToString()?.Trim() ?? ""
                                    ));
                                }
                            }

                            int srNoK = 0;
                            foreach (var sr in srRows)
                            {
                                srNoK++;

                                // MTFDetailssub insert
                                using (var c = conn.CreateCommand())
                                {
                                    c.Transaction = sqlTran;
                                    c.CommandText = @"
                            INSERT INTO MTFDetailssub (MTFCode, SrNo, PartCode, SerialNo, JobcardStatus)
                            VALUES (@mtf, @sr, @pc, @sn, @js)";
                                    c.Parameters.AddWithValue("@mtf", mtfCode);
                                    c.Parameters.AddWithValue("@sr", srNoK);
                                    c.Parameters.AddWithValue("@pc", p.PartCode);
                                    c.Parameters.AddWithValue("@sn", sr.SerialNo);
                                    c.Parameters.AddWithValue("@js", sr.JobcardStatus == "J" ? "J" : "P");
                                    await c.ExecuteNonQueryAsync();
                                }

                                var gPrefix3 = sr.GCode.Length >= 3 ? sr.GCode.Substring(0, 3) : "";

                                // ── UPDATE via LINQ (EF Core) ──
                                if (gPrefix3 == "GIR")
                                {
                                    var ent = await _context.GiirdetailsSubs
                                        .FirstOrDefaultAsync(e => e.SerialNo == sr.SerialNo && e.Giircode == sr.GCode);
                                    if (ent != null)
                                    {
                                        ent.Trfstatus = "D";
                                        ent.Trfcode = mtfCode;
                                        await _context.SaveChangesAsync();
                                    }
                                }
                                else if (gPrefix3 == "GRI")
                                {
                                    var ent = await _context.GatereceiptInternalDetailsSubs
                                        .FirstOrDefaultAsync(e => e.SerialNo == sr.SerialNo && e.Gricode == sr.GCode);
                                    if (ent != null)
                                    {
                                        ent.Trfstatus = "D";
                                        ent.Trfcode = mtfCode;
                                        await _context.SaveChangesAsync();
                                    }
                                }
                                else if (gPrefix3 == "CNS")
                                {
                                    // Resolve GIIR code via ConvertSerialNoDetails
                                    string strGiirCode = "0";
                                    using (var c = conn.CreateCommand())
                                    {
                                        c.Transaction = sqlTran;
                                        c.CommandText = "SELECT GiirCode FROM ConvertSerialNoDetails WHERE CNVCode = @cnv AND SerialNo = @sn";
                                        c.Parameters.Add(new SqlParameter("@cnv", sr.GCode));
                                        c.Parameters.Add(new SqlParameter("@sn", sr.SerialNo));
                                        strGiirCode = (await c.ExecuteScalarAsync())?.ToString()?.Trim() ?? "0";
                                    }

                                    var giirEnt = await _context.GiirdetailsSubs
                                        .FirstOrDefaultAsync(e => e.SerialNo == sr.SerialNo && e.Giircode == strGiirCode);
                                    if (giirEnt != null)
                                    {
                                        giirEnt.Trfstatus = "D";
                                        giirEnt.Trfcode = mtfCode;
                                        await _context.SaveChangesAsync();
                                    }

                                    var cnvEnt = await _context.ConvertSerialNoDetails
                                        .FirstOrDefaultAsync(e => e.SerialNo == sr.SerialNo && e.Cnvcode == sr.GCode);
                                    if (cnvEnt != null)
                                    {
                                        cnvEnt.MtfserialStatus = "D";
                                        cnvEnt.Cmtfcode = mtfCode;
                                        await _context.SaveChangesAsync();
                                    }
                                }
                            }

                            // Internal same-company: additional StockWIP received row
                            if (isInternalSameCompany)
                            {
                                string stageName =
                                    (prefix3 == "001" || prefix3 == "002") ? "StageI"
                                    : "StageIII"; // prefix3 == "010"

                                using var c = conn.CreateCommand();
                                c.Transaction = sqlTran;
                                c.CommandText = @"
                        INSERT INTO StockWIP (FromProfitCenterCode, PartCode, ReceivedCode, ReceivedDate, ReceivedQty, ToProfitCenterCode, StockType, StageName, FromProfitCenterCode_Act, ToProfitCenterCode_Act)
                        VALUES (@from, @pc, @mtf, @dt, @qty, @to, '0', @stage, @FromProfitCenterCode_Act, @ToProfitCenterCode_Act)";
                                c.Parameters.AddWithValue("@from", req.FromPCCode.Trim());
                                c.Parameters.AddWithValue("@pc", p.PartCode);
                                c.Parameters.AddWithValue("@mtf", mtfCode);
                                c.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                c.Parameters.AddWithValue("@qty", p.MtfQty);
                                c.Parameters.AddWithValue("@to", req.ToPCCode.Trim());
                                c.Parameters.AddWithValue("@stage", stageName);
                                c.Parameters.AddWithValue("@FromProfitCenterCode_Act", req.FromPCCode.Trim());
                                c.Parameters.AddWithValue("@ToProfitCenterCode_Act", req.ToPCCode.Trim());
                                await c.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // Non-001/002/010 parts, internal same-company
                            if (isInternalSameCompany)
                            {
                                using var c = conn.CreateCommand();
                                c.Transaction = sqlTran;
                                c.CommandText = @"
                        INSERT INTO StockWIP (FromProfitCenterCode, PartCode, ReceivedCode, ReceivedDate, ReceivedQty, ToProfitCenterCode, StockType, StageName, FromProfitCenterCode_Act, ToProfitCenterCode_Act)
                        VALUES (@from, @pc, @mtf, @dt, @qty, @to, '0', '0' @FromProfitCenterCode_Act, @ToProfitCenterCode_Act)";
                                c.Parameters.AddWithValue("@from", req.FromPCCode.Trim());
                                c.Parameters.AddWithValue("@pc", p.PartCode);
                                c.Parameters.AddWithValue("@mtf", mtfCode);
                                c.Parameters.AddWithValue("@dt", DateTime.Now);
                                c.Parameters.AddWithValue("@qty", p.MtfQty);
                                c.Parameters.AddWithValue("@to", req.ToPCCode.Trim());
                                c.Parameters.AddWithValue("@FromProfitCenterCode_Act", req.FromPCCode.Trim());
                                c.Parameters.AddWithValue("@ToProfitCenterCode_Act", req.ToPCCode.Trim());
                                await c.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    // ── 3e. Close requisition if fully MTF'd (LINQ) ──
                    if (Math.Abs(req.ReqBalQty - req.MTFQty) < 0.000001)
                    {
                        var mr = await _context.MaterialRequisitionWithOutPlans
                            .FirstOrDefaultAsync(e => e.Reqcode == req.ReqCode.Trim());
                        if (mr != null)
                        {

                            mr.Reqstatus = "D";
                            await _context.SaveChangesAsync();
                        }
                    }

                    // ── 3f. LoginTransactionDetails for MTF ──
                    using (var c = conn.CreateCommand())
                    {
                        c.Transaction = sqlTran;
                        c.CommandText = "InsertLoginTransactionDetails";
                        c.CommandType = CommandType.StoredProcedure;
                        c.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now);
                        c.Parameters.AddWithValue("@EmpID", req.UserID.ToString().Trim());
                        c.Parameters.AddWithValue("@TransactionType", "S");
                        c.Parameters.AddWithValue("@TransactionFrom", "MTF");
                        c.Parameters.AddWithValue("@TransactionNo", mtfCode);
                        c.Parameters.AddWithValue("@CompanyCode", req.CompID.Trim());
                        await c.ExecuteNonQueryAsync();

                    }

                    // ═══════════════════════════════════════════════════════════════
                    //  4. MEMO SECTION — cross-company transfers only
                    // ═══════════════════════════════════════════════════════════════
                    if (isCrossCompany)
                    {
                        // ── 4a. Pre-collect GIIR numbers for Eng/Alt/Bat (preserves original's last-value-wins behavior) ──
                        string engGiir = "", altGiir = "", batGiir = "";
                        foreach (var p in parts)
                        {
                            var pre = p.PartCode.Length >= 3 ? p.PartCode.Substring(0, 3) : "";
                            if (pre != "001" && pre != "002" && pre != "010") continue;

                            using var c = conn.CreateCommand();
                            c.Transaction = sqlTran;
                            c.CommandText = "EXEC MTFSRNoGIIRNo_Qty @pc, @qty";
                            c.Parameters.Add(new SqlParameter("@pc", p.PartCode));
                            c.Parameters.Add(new SqlParameter("@qty", p.MtfQty));
                            using var r = await c.ExecuteReaderAsync();
                            while (await r.ReadAsync())
                            {
                                var giir = r["GIIRNO"]?.ToString()?.Trim() ?? "";

                                // NOTE: Preserving original behavior exactly —
                                //       if buffer empty: set; else REPLACE with "," + value (not append).
                                //       This is a legacy quirk; downstream consumers may rely on it.
                                if (pre == "001") engGiir = string.IsNullOrEmpty(engGiir) ? giir : "," + giir;
                                if (pre == "002") altGiir = string.IsNullOrEmpty(altGiir) ? giir : "," + giir;
                                if (pre == "010") batGiir = string.IsNullOrEmpty(batGiir) ? giir : "," + giir;
                            }
                        }

                        // ── 4b. Generate Memo Code (inlined GetMaxNo) ──
                        string memoCode;
                        string memoSerial;
                        {
                            int maxVal;
                            using (var c = conn.CreateCommand())
                            {
                                c.Transaction = sqlTran;
                                c.CommandText = @"
                        SELECT ISNULL(MaxValue, 0)
                          FROM GetMaxCode WITH (UPDLOCK, HOLDLOCK)
                         WHERE TblName = @t AND CompCode = @c AND Prefix = @p AND Yr = @y";
                                c.Parameters.Add(new SqlParameter("@t", "MemoExciseMfg"));
                                c.Parameters.Add(new SqlParameter("@c", fromCompCode));
                                c.Parameters.Add(new SqlParameter("@p", "MOE"));
                                c.Parameters.Add(new SqlParameter("@y", fiscalYear));
                                var res = await c.ExecuteScalarAsync();
                                maxVal = res == null || res == DBNull.Value ? 0 : Convert.ToInt32(res);
                            }
                            memoSerial = (maxVal + 1).ToString("D6");
                            memoCode = $"MOE/{fiscalYear}/{fromCompCode}{memoSerial}";

                            using var c2 = conn.CreateCommand();
                            c2.Transaction = sqlTran;
                            c2.CommandText = @"
                    UPDATE GetMaxCode
                       SET MaxValue = @new
                     WHERE Prefix = @p AND TblName = @t AND CompCode = @c AND Yr = @y";
                            c2.Parameters.Add(new SqlParameter("@new", memoSerial));
                            c2.Parameters.Add(new SqlParameter("@p", "MOE"));
                            c2.Parameters.Add(new SqlParameter("@t", "MemoExciseMfg"));
                            c2.Parameters.Add(new SqlParameter("@c", fromCompCode));
                            c2.Parameters.Add(new SqlParameter("@y", fiscalYear));
                            await c2.ExecuteNonQueryAsync();
                        }

                        // ── 4c. InsertMemoExciseMfg_DG ──
                        bool anyEmpty = string.IsNullOrEmpty(engGiir.Trim())
                                     || string.IsNullOrEmpty(altGiir.Trim())
                                     || string.IsNullOrEmpty(batGiir.Trim());
                        using (var c = conn.CreateCommand())
                        {
                            c.Transaction = sqlTran;
                            c.CommandText = "InsertMemoExciseMfg_DG";
                            c.CommandType = CommandType.StoredProcedure;
                            c.Parameters.AddWithValue("@MECode", memoCode);
                            c.Parameters.AddWithValue("@MaxSrNo", memoSerial);
                            c.Parameters.AddWithValue("@Dt", DateTime.Now);
                            c.Parameters.AddWithValue("@Yr", fiscalYear);
                            c.Parameters.AddWithValue("@FromProfitCenter", req.FromPCCode.Trim());
                            c.Parameters.AddWithValue("@ToProfitCenter", req.ToPCCode.Trim());
                            c.Parameters.AddWithValue("@TallyHeadCode", "2");
                            c.Parameters.AddWithValue("@ConsigneeCode", getConsigneeCode(req.ToPCCode.Trim()).Trim());
                            c.Parameters.AddWithValue("@TMcode", "01");
                            c.Parameters.AddWithValue("@PONo", "0");
                            c.Parameters.AddWithValue("@PODate", "");
                            c.Parameters.AddWithValue("@CompanyCode", fromCompCode);
                            c.Parameters.AddWithValue("@StockType", "0");
                            c.Parameters.AddWithValue("@WtPerUt", 0);
                            c.Parameters.AddWithValue("@SqftPerUt", 0);
                            c.Parameters.AddWithValue("@Auth", 0);
                            c.Parameters.AddWithValue("@MMTFCode", mtfCode);
                            c.Parameters.AddWithValue("@MTFScanStatus", anyEmpty ? "D" : "P");
                            await c.ExecuteNonQueryAsync();
                        }

                        // ── 4d. Login log for memo ──
                        using (var c = conn.CreateCommand())
                        {
                            c.Transaction = sqlTran;
                            c.CommandText = "InsertLoginTransactionDetails";
                            c.CommandType = CommandType.StoredProcedure;
                            c.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now);
                            c.Parameters.AddWithValue("@EmpID", req.UserID.ToString().Trim());
                            c.Parameters.AddWithValue("@TransactionType", "S");
                            c.Parameters.AddWithValue("@TransactionFrom", "MemoExciseMfg");
                            c.Parameters.AddWithValue("@TransactionNo", memoCode);
                            c.Parameters.AddWithValue("@CompanyCode", fromCompCode);
                            await c.ExecuteNonQueryAsync();
                        }

                        // ── 4e. Memo detail rows ──
                        int memoSrNo = 0;
                        string stockTbl = getStockTbl(fromCompCode);

                        foreach (var p in parts)
                        {
                            if (p.MtfQty <= 0) continue;

                            var pre = p.PartCode.Length >= 3 ? p.PartCode.Substring(0, 3) : "";
                            string giirCodeToSend = "";

                            // Lookup MOB
                            string mob = "";
                            using (var c = conn.CreateCommand())
                            {
                                c.Transaction = sqlTran;
                                c.CommandText = "SELECT MOB FROM Part WHERE partcode = @pc";
                                c.Parameters.Add(new SqlParameter("@pc", p.PartCode));
                                mob = (await c.ExecuteScalarAsync())?.ToString()?.Trim() ?? "";
                            }

                            if (pre == "001" || pre == "002" || pre == "010")
                            {
                                giirCodeToSend =
                                    pre == "001" ? engGiir
                                    : pre == "002" ? altGiir
                                    : batGiir;
                            }
                            else if (mob == "B" || mob == "M")
                            {
                                // Allocate from GIIR details
                                var girRows = new List<(string ReceivedCode, double BalQty)>();
                                using (var c = conn.CreateCommand())
                                {
                                    c.Transaction = sqlTran;
                                    c.CommandText = "EXEC MTFSRNo_GIRQty 'Details', @pc";
                                    c.Parameters.Add(new SqlParameter("@pc", p.PartCode));
                                    using var r = await c.ExecuteReaderAsync();
                                    while (await r.ReadAsync())
                                    {
                                        girRows.Add((
                                            r["ReceivedCode"]?.ToString()?.Trim() ?? "",
                                            Convert.ToDouble(r["BalQty"], CultureInfo.InvariantCulture)
                                        ));
                                    }
                                }

                                double remaining = p.MtfQty;
                                var giirBuilder = "";
                                foreach (var g in girRows)
                                {
                                    if (remaining <= 0) break;
                                    double allocate = g.BalQty >= remaining ? remaining : g.BalQty;
                                    remaining -= allocate;

                                    giirBuilder += $"#{g.ReceivedCode}@{allocate}";

                                    // memoExciseMfgGIIR insert
                                    using (var c = conn.CreateCommand())
                                    {
                                        c.Transaction = sqlTran;
                                        c.CommandText = @"
                                INSERT INTO memoExciseMfgGIIR (MECODE, GiirCode, IssueQty)
                                VALUES (@me, @gc, @qty)";
                                        c.Parameters.AddWithValue("@me", memoCode);
                                        c.Parameters.AddWithValue("@gc", g.ReceivedCode);
                                        c.Parameters.AddWithValue("@qty", allocate);
                                        await c.ExecuteNonQueryAsync();
                                    }

                                    // For MOB='M': also MTFProcessDetails
                                    if (mob == "M")
                                    {
                                        using var c = conn.CreateCommand();
                                        c.Transaction = sqlTran;
                                        c.CommandText = @"
                                INSERT INTO MTFProcessDetails (MTFCODE, SrNo, PartCode, ProcessNo, MTFPQty, SerialNo, TRFPStatus)
                                VALUES (@mtf, '1', @pc, @pn, @qty, '', 'P')";
                                        c.Parameters.AddWithValue("@mtf", mtfCode);
                                        c.Parameters.AddWithValue("@pc", p.PartCode);
                                        c.Parameters.AddWithValue("@pn", g.ReceivedCode);
                                        c.Parameters.AddWithValue("@qty", allocate);
                                        await c.ExecuteNonQueryAsync();
                                    }

                                    // Stock table insert (dynamic name via ComCon.getStockTbl — reused)
                                    using (var c = conn.CreateCommand())
                                    {
                                        c.Transaction = sqlTran;
                                        c.CommandText = $@"
                                INSERT INTO {stockTbl} (PartCode, ReceivedCode, IssueCode, IssueDate, IssueQty)
                                VALUES (@pc, @rc, @ic, @dt, @qty)";
                                        c.Parameters.AddWithValue("@pc", p.PartCode);
                                        c.Parameters.AddWithValue("@rc", g.ReceivedCode);
                                        c.Parameters.AddWithValue("@ic", memoCode);
                                        c.Parameters.AddWithValue("@dt", DateTime.Now);
                                        c.Parameters.AddWithValue("@qty", allocate);
                                        await c.ExecuteNonQueryAsync();
                                    }

                                    // Check GIR bal after insert
                                    double girBal;
                                    using (var c = conn.CreateCommand())
                                    {
                                        c.Transaction = sqlTran;
                                        c.CommandText = $@"
                                SELECT ISNULL((SUM(ReceivedQty) - SUM(IssueQty)), 0) AS Bal
                                  FROM {stockTbl}
                                 WHERE ReceivedCode = @rc AND Partcode = @pc";
                                        c.Parameters.AddWithValue("@rc", g.ReceivedCode);
                                        c.Parameters.AddWithValue("@pc", p.PartCode);
                                        girBal = Convert.ToDouble((await c.ExecuteScalarAsync()) ?? 0, CultureInfo.InvariantCulture);
                                    }

                                    if (girBal == 0)
                                    {
                                        if (mob == "B")
                                        {
                                            //LINQ: UPDATE GIIRDetails SET GIIRStatus = 'D'
                                            var gDetails = await _context.Giirdetails
                                                .Where(e => e.Giircode == g.ReceivedCode && e.PartCode == p.PartCode)
                                                .ToListAsync();
                                            foreach (var e in gDetails) e.Giirstatus = "D";
                                            if (gDetails.Count > 0) await _context.SaveChangesAsync();
                                        }
                                        else // mob == "M"
                                        {
                                            // LINQ: UPDATE ProcessWithKit SET MTFStatus='D'
                                            var pwk = await _context.ProcessWithKits
                                                .FirstOrDefaultAsync(e => e.Pwkcode == g.ReceivedCode && e.ProcessKitCode == p.PartCode);
                                            if (pwk != null)
                                            {
                                                pwk.Mtfstatus = "D";
                                                await _context.SaveChangesAsync();
                                            }
                                        }
                                    }

                                    // For MOB='B' only: check GIIR remaining + mark header done
                                    if (mob == "B")
                                    {
                                        int remainingCnt;
                                        using (var c = conn.CreateCommand())
                                        {
                                            c.Transaction = sqlTran;
                                            c.CommandText = @"
                                    SELECT COUNT(GIIRStatus) FROM GIIRDetails
                                     WHERE GIIRStatus <> 'D' AND GIIRCode = @gc";
                                            c.Parameters.AddWithValue("@gc", g.ReceivedCode);
                                            remainingCnt = Convert.ToInt32((await c.ExecuteScalarAsync()) ?? 0);
                                        }

                                        if (remainingCnt == 0)
                                        {
                                            var giirHdr = await _context.Giirs
                                                .FirstOrDefaultAsync(e => e.Giircode == g.ReceivedCode);
                                            if (giirHdr != null)
                                            {
                                                giirHdr.Giirstatus = "D";
                                                await _context.SaveChangesAsync();
                                            }
                                        }
                                    }
                                }

                                giirCodeToSend = giirBuilder;
                            }

                            // InsertMemoExciseMfgDetails
                            memoSrNo++;
                            using (var c = conn.CreateCommand())
                            {
                                c.Transaction = sqlTran;
                                c.CommandText = "InsertMemoExciseMfgDetails";
                                c.CommandType = CommandType.StoredProcedure;
                                c.Parameters.AddWithValue("@MECode", memoCode);
                                c.Parameters.AddWithValue("@SrNo", memoSrNo);
                                c.Parameters.AddWithValue("@GIIRCode", string.IsNullOrEmpty(giirCodeToSend) ? (object)0 : giirCodeToSend);
                                c.Parameters.AddWithValue("@PartCode", p.PartCode);
                                c.Parameters.AddWithValue("@StockQty", p.StockQty);
                                c.Parameters.AddWithValue("@Qty", p.StockQty - p.MtfQty);
                                c.Parameters.AddWithValue("@IssueQty", p.MtfQty);
                                c.Parameters.AddWithValue("@InvUOM", 0);
                                c.Parameters.AddWithValue("@GIIRBalQty", 0);
                                c.Parameters.AddWithValue("@Rate", p.Rate);
                                c.Parameters.AddWithValue("@SaleRate", p.Rate);
                                c.Parameters.AddWithValue("@ManulDescription", 0);
                                c.Parameters.AddWithValue("@PerUnitWt", 0);
                                c.Parameters.AddWithValue("@PerUnitSqft", 0);
                                c.Parameters.AddWithValue("@MCRWt", 0);
                                c.Parameters.AddWithValue("@MHRWt", 0);
                                c.Parameters.AddWithValue("@MCRAmt", 0);
                                c.Parameters.AddWithValue("@MHRAmt", 0);
                                await c.ExecuteNonQueryAsync();
                            }

                            // InsertMemoExciseMfgDetailsSub
                            using (var c = conn.CreateCommand())
                            {
                                c.Transaction = sqlTran;
                                c.CommandText = "InsertMemoExciseMfgDetailsSub";
                                c.CommandType = CommandType.StoredProcedure;
                                c.Parameters.AddWithValue("@MECode", memoCode);
                                c.Parameters.AddWithValue("@SrNo", memoSrNo);
                                c.Parameters.AddWithValue("@PartCode", p.PartCode);
                                c.Parameters.AddWithValue("@MTFCode", mtfCode);
                                c.Parameters.AddWithValue("@IssueQty", p.MtfQty);
                                c.Parameters.AddWithValue("@BalQty", p.StockQty - p.MtfQty);
                                c.Parameters.AddWithValue("@MTFIssueQty", p.MtfQty);
                                await c.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    await transaction.CommitAsync();
                    return mtfCode;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private static string getStockTbl(string compID)
        {
            return compID switch
            {
                "01" => "Stock01",
                "02" => "Stock04",   // legacy crossover — preserved intentionally
                "03" => "Stock03",
                "04" => "Stock02",   // legacy crossover — preserved intentionally
                "05" => "Stock05",
                "07" => "Stock07",
                "08" => "Stock08",
                "09" => "Stock09",
                "10" => "Stock10",
                "13" => "Stock13",
                "14" => "Stock14",
                "15" => "Stock15",
                "16" => "Stock16",
                "17" => "Stock17",
                "18" => "Stock18",
                "19" => "Stock19",
                "20" => "Stock20",
                "21" => "Stock21",
                "22" => "Stock22",
                "23" => "Stock23",
                _ => "0"          // matches legacy default
            };
        }

        private static string getConsigneeCode(string toPCCode)
        {
            if (string.IsNullOrWhiteSpace(toPCCode) || toPCCode.Length < 2)
                return "0";

            return toPCCode.Trim().Substring(0, 2) switch
            {
                "01" => "03.01.01.01.23.0001",
                "02" => "03.01.01.01.01.0001",
                "03" => "03.01.01.01.01.0002",
                "04" => "03.01.01.02.03.0001",
                "05" => "03.01.01.03.02.0001",
                "10" => "03.01.01.02.08.0009",
                "14" => "03.01.01.37.01.0119",
                _ => "0",
            };
        }
    }
}
