using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Jobcard;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KalaGenset.ERP.Core.Services
{
    public class JobcardService : IJobcard
    {
        private readonly KalaDbContext _context;

        public JobcardService(KalaDbContext context)
        {
            _context = context;
        }

        public async Task<List<Dictionary<string, object>>> GetDGAsync(string strJobCardType, string strCompID)
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _context.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetJobCardDGDts";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;

                    cmd.Parameters.Add(new SqlParameter("@JobCardType", SqlDbType.Char) { Value = strJobCardType });
                    cmd.Parameters.Add(new SqlParameter("@CompCode", SqlDbType.Char) { Value = strCompID });

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

        public async Task<string> SubmitJobCardAsync(JobCardSubmitRequest request)
        {
            var activeRows = request.Rows?.Where(r => r.Qty > 0).ToList();
            if (activeRows == null || !activeRows.Any())
                return "No Job Card rows provided.";

            string compCode = request.PCCode.Trim().Substring(0, 2);
            string jobCardNo = "";
            string result = "";

            // Financial year computed in C# — Apr to Mar cycle e.g. "25-26"
            string yr = DateTime.Now.Month >= 4
                ? $"{DateTime.Now:yy}-{DateTime.Now.AddYears(1):yy}"
                : $"{DateTime.Now.AddYears(-1):yy}-{DateTime.Now:yy}";

            try
            {
                #region STEP 1 — PRE-VALIDATION (before transaction)
                // Validates serial availability for all rows before opening transaction.
                // Mirrors original outer loop — fail fast with clear message.
                foreach (var row in activeRows)
                {
                    var preSerials = new List<(string PartCode, string SerialNo, string Gcode)>();

                    var preConn = (SqlConnection)_context.Database.GetDbConnection();
                    if (preConn.State == ConnectionState.Closed)
                        await preConn.OpenAsync();

                    using (var srCmd = new SqlCommand("GetJobCardSrNo", preConn))
                    {
                        srCmd.CommandType = CommandType.StoredProcedure;
                        srCmd.CommandTimeout = 0;
                        srCmd.Parameters.AddWithValue("@JobCodeType", "DGWOP");
                        srCmd.Parameters.AddWithValue("@PartCode", row.PartCode);
                        srCmd.Parameters.AddWithValue("@Qty", row.Qty);
                        srCmd.Parameters.AddWithValue("@CompCode", compCode);
                        using var r = await srCmd.ExecuteReaderAsync();
                        while (await r.ReadAsync())
                            preSerials.Add((
                                r["PartCode"]?.ToString()?.Trim() ?? "",
                                r["SerialNo"]?.ToString()?.Trim() ?? "",
                                r["Gcode"]?.ToString()?.Trim() ?? ""));
                    }

                    if (!preSerials.Any()) return "Job Card Details not available";

                    int preEng = 0, preAlt = 0, preBat = 0, preCpy = 0;
                    foreach (var s in preSerials)
                    {
                        if (s.PartCode.Length >= 3 && s.PartCode.Substring(0, 3) == "001") preEng++;
                        else if (s.PartCode.Length >= 3 && s.PartCode.Substring(0, 3) == "002") preAlt++;
                        else if (s.PartCode.Length >= 3 && s.PartCode.Substring(0, 3) == "010") preBat++;
                        else if (s.PartCode.Length >= 2 && s.PartCode.Substring(0, 2) == "40") preCpy++;
                    }

                    // Fetch part description via LINQ for error message
                    string preDesc = await _context.Parts
                        .Where(p => p.PartCode == row.PartCode)
                        .Select(p => p.PartDesc)
                        .FirstOrDefaultAsync() ?? row.PartCode;

                    if (row.Qty > preEng) return $"Engine SrNo Not available For DG {preDesc}";
                    else if (row.Qty > preAlt) return $"Alternator SrNo Not available For DG {preDesc}";
                    else if (row.Qty > preBat && await checkTranBOMForBat(row.PartCode))
                        return $"Battery SrNo Not available For DG {preDesc}";
                    else if (row.Qty > preCpy) return $"Canopy SrNo Not available For DG {preDesc}";
                }
                #endregion
            }
            catch (Exception ex)
            {

                throw;
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var sqlConn = (SqlConnection)_context.Database.GetDbConnection();
                    var sqlTran = (SqlTransaction)_context.Database.CurrentTransaction.GetDbTransaction();

                    #region STEP 2 — GETMAXNO INLINE — Generate JobCard number e.g. JCD/25-26/03000124
                    var maxJcRecord = await _context.GetMaxCodes
                        .Where(g => g.TblName == "JobCard" && g.CompCode == compCode
                                 && g.Prefix == "JCD" && g.Yr == yr)
                        .FirstOrDefaultAsync();

                    int intMaxJC = maxJcRecord != null ? Convert.ToInt32(maxJcRecord.MaxValue) : 0;
                    string strMaxJC = intMaxJC switch
                    {
                        0 => "000001",
                        < 9 => "00000" + (intMaxJC + 1),
                        < 99 => "0000" + (intMaxJC + 1),
                        < 999 => "000" + (intMaxJC + 1),
                        < 9999 => "00" + (intMaxJC + 1),
                        < 99999 => "0" + (intMaxJC + 1),
                        _ => Convert.ToString(intMaxJC + 1)
                    };
                    jobCardNo = $"JCD/{yr}/{compCode}{strMaxJC}";

                    // Update GetMaxCode via LINQ (update operation)
                    if (maxJcRecord != null)
                    {
                        maxJcRecord.MaxValue = int.Parse(strMaxJC);
                        await _context.SaveChangesAsync();
                    }
                    #endregion

                    #region STEP 3 — INSERT JobCard master header
                    await _context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO JobCard(JobCode,Dt,Yr,MaxSrNo,PCCode,Remark,CompanyCode,Active,Auth) " +
                        "VALUES(@JobCode,@Dt,@Yr,@MaxSrNo,@PCCode,@Remark,@CompCode,'1','1')",
                        new SqlParameter("@JobCode", jobCardNo),
                        new SqlParameter("@Dt", DateTime.Now),
                        new SqlParameter("@Yr", yr),
                        new SqlParameter("@MaxSrNo", jobCardNo.Substring(10, 8)),
                        new SqlParameter("@PCCode", request.PCCode.Trim()),
                        new SqlParameter("@Remark", request.Remark?.Trim() ?? ""),
                        new SqlParameter("@CompCode", compCode));
                    #endregion

                    int globalSrNo = 0;
                    int detailSrNo = 0;

                    foreach (var row in activeRows)
                    {
                        detailSrNo++;

                        #region STEP 4 — INSERT JobCardDetails (one per DG model row)
                        // Links back to monthly plan via PlanCode + PlanDate.
                        // SP recalculates PenPQty = DayPlanQty - SUM(Qty here) on next search.
                        await _context.Database.ExecuteSqlRawAsync(
                            "INSERT INTO JobCardDetails" +
                            "(JobCode,SrNo,BOMCode,PartCode,Qty,PlanCode,PlanDate,DayPlanQty," +
                            " Stage1Status,Stage2Status,Stage3Status) " +
                            "VALUES(@JobCode,@SrNo,@BOMCode,@PartCode,@Qty,@PlanCode,@PlanDate,@DayPlanQty,'P','P','P')",
                            new SqlParameter("@JobCode", jobCardNo),
                            new SqlParameter("@SrNo", detailSrNo),
                            new SqlParameter("@BOMCode", row.BOMCode?.Trim() ?? (object)DBNull.Value),
                            new SqlParameter("@PartCode", row.PartCode?.Trim() ?? (object)DBNull.Value),
                            new SqlParameter("@Qty", row.Qty),
                            new SqlParameter("@PlanCode", row.PlanCode?.Trim() ?? (object)DBNull.Value),
                            new SqlParameter("@PlanDate", row.PlanDate?.Trim() ?? (object)DBNull.Value),
                            new SqlParameter("@DayPlanQty", row.DayPlanQty ?? 0));
                        #endregion

                        // KVA from row object — already fetched by SP, avoids extra DB call.
                        // Drives dual-battery logic: >200 KVA needs 2 batteries per DG unit.
                        double kva = row.KVA ?? 0;

                        #region STEP 5 — FETCH SERIAL NUMBERS within transaction
                        // Calls GetJobCardSrNo SP inside transaction so serials are locked to this scope.
                        var serials = new List<(string PartCode, string SerialNo, string Gcode)>();
                        using (var srCmd = new SqlCommand("GetJobCardSrNo", sqlConn, sqlTran))
                        {
                            srCmd.CommandType = CommandType.StoredProcedure;
                            srCmd.CommandTimeout = 0;
                            srCmd.Parameters.AddWithValue("@JobCodeType", "DGWOP");
                            srCmd.Parameters.AddWithValue("@PartCode", row.PartCode);
                            srCmd.Parameters.AddWithValue("@Qty", row.Qty);
                            srCmd.Parameters.AddWithValue("@CompCode", compCode);
                            using var srReader = await srCmd.ExecuteReaderAsync();
                            while (await srReader.ReadAsync())
                                serials.Add((
                                    srReader["PartCode"]?.ToString()?.Trim() ?? "",
                                    srReader["SerialNo"]?.ToString()?.Trim() ?? "",
                                    srReader["Gcode"]?.ToString()?.Trim() ?? ""));
                        }
                        if (!serials.Any()) continue;
                        #endregion

                        // JPriority counters — reset per DG row
                        // Ensures Engine 1 always pairs with Alternator 1, Battery 1, Canopy 1
                        int jpEng = 0, jpAlt = 0, jpBat = 0, jpCpy = 0;
                        int batCnt = 0; // 0/1 toggle for >200KVA — both batteries share same JPriority

                        // Post-insert counters for qty verification
                        int cntEng = 0, cntAlt = 0, cntBat = 0, cntCpy = 0;

                        foreach (var serial in serials)
                        {
                            string pc3 = serial.PartCode.Length >= 3 ? serial.PartCode.Substring(0, 3) : "";
                            string pc2 = serial.PartCode.Length >= 2 ? serial.PartCode.Substring(0, 2) : "";
                            string gc3 = serial.Gcode.Length >= 3 ? serial.Gcode.Substring(0, 3) : "";

                            #region STEP 6 — CALCULATE JPRIORITY
                            // Each component type has its own sequential counter.
                            // >200KVA battery: batCnt toggle keeps both batteries on same JPriority.
                            int jPriority = 0;
                            if (pc3 == "001") { jpEng++; jPriority = jpEng; }
                            else if (pc3 == "002") { jpAlt++; jPriority = jpAlt; }
                            else if (pc3 == "401") { jpCpy++; jPriority = jpCpy; }
                            else if (pc3 == "010" && kva <= 200) { jpBat++; jPriority = jpBat; }
                            else if (pc3 == "010" && kva > 200)
                            {
                                if (batCnt == 0) { jpBat++; batCnt = 1; }
                                else { batCnt = 0; }
                                jPriority = jpBat;
                            }
                            #endregion

                            #region STEP 7 — DETERMINE TRANSFERSTATUS (D=Direct, P=Pending)
                            // D = component already at this assembly profit center
                            // P = component needs to be transferred to this PC before assembly
                            string transferStatus;
                            if (gc3 == "MTF" || gc3 == "CNS")
                            {
                                transferStatus = "D";
                            }
                            else if (gc3 == "GIR" && compCode == "01"
                                     && serial.Gcode.Length >= 12
                                     && serial.Gcode.Substring(10, 2) == "01")
                            {
                                transferStatus = "D";
                            }
                            else if (gc3 == "GIR")
                            {
                                string poPcCode = await GetPOPCcode(serial.Gcode, sqlConn, sqlTran);
                                transferStatus = poPcCode == request.PCCode.Trim() ? "D" : "P";
                            }
                            else { transferStatus = "P"; }
                            #endregion

                            // Bat(010) and Cpy(401) = D — directly usable, no assembly work needed
                            // Eng(001) and Alt(002) = P — requires Stage1 assembly process
                            string stage = (pc3 == "401" || pc3 == "010") ? "D" : "P";

                            #region STEP 8 — INSERT JobCardDetailsSub (one row per serial number)
                            globalSrNo++;
                            await _context.Database.ExecuteSqlRawAsync(
                                "INSERT INTO JobCardDetailsSub" +
                                "(JobCode,SrNo,PartCode,SrNoPartCode,SerialNo,JPriority," +
                                " TransferCode,Transferstatus,stage1Status,stage2Status) " +
                                "VALUES(@JobCode,@SrNo,@PartCode,@SrNoPartCode,@SerialNo," +
                                "@JPriority,@TransferCode,@Transferstatus,@Stage1,@Stage2)",
                                new SqlParameter("@JobCode", jobCardNo),
                                new SqlParameter("@SrNo", globalSrNo),
                                new SqlParameter("@PartCode", row.PartCode?.Trim()),
                                new SqlParameter("@SrNoPartCode", serial.PartCode),
                                new SqlParameter("@SerialNo", serial.SerialNo),
                                new SqlParameter("@JPriority", jPriority),
                                new SqlParameter("@TransferCode", serial.Gcode),
                                new SqlParameter("@Transferstatus", transferStatus),
                                new SqlParameter("@Stage1", stage),
                                new SqlParameter("@Stage2", stage));
                            #endregion

                            #region STEP 9 — LOCK SOURCE DOCUMENTS via LINQ (JobCardStatus = J)
                            // Prevents this serial from being picked again by GetJobCardSrNo.
                            // Different source tables updated based on Gcode prefix.
                            if (pc3 == "001" || pc3 == "002" || pc3 == "010")
                            {
                                if (gc3 == "GIR")
                                {
                                    // Lock Gate Inward Inspection Receipt
                                    var giirRows = await _context.GiirdetailsSubs
                                        .Where(g => g.Giircode == serial.Gcode
                                                 && g.SerialNo == serial.SerialNo
                                                 && g.PartCode == serial.PartCode)
                                        .ToListAsync();
                                    foreach (var g in giirRows) g.JobCardStatus = "J";
                                    await _context.SaveChangesAsync();
                                }
                                else if (gc3 == "GRI")
                                {
                                    // Lock Gate Receipt Internal
                                    var griRows = await _context.GatereceiptInternalDetailsSubs
                                        .Where(g => g.Gricode == serial.Gcode
                                                 && g.SerialNo == serial.SerialNo
                                                 && g.PartCode == serial.PartCode)
                                        .ToListAsync();
                                    foreach (var g in griRows) g.JobcardStatus = "J";
                                    await _context.SaveChangesAsync();
                                }
                                else if (gc3 == "CNS")
                                {
                                   // Lock Convert Serial document
                                    var cnsRows = await _context.ConvertSerialNoDetails
                                        .Where(c => c.Cnvcode == serial.Gcode
                                                 && c.SerialNo == serial.SerialNo)
                                        .ToListAsync();
                                    foreach (var c in cnsRows) c.JobCardStatus = "J";
                                    await _context.SaveChangesAsync();

                                    // Also lock the original GIIR this CNS was converted from
                                    string origGiir = await _context.ConvertSerialNoDetails
                                        .Where(c => c.Cnvcode == serial.Gcode
                                                 && c.SerialNo == serial.SerialNo)
                                        .Select(c => c.Giircode)
                                        .FirstOrDefaultAsync() ?? "";

                                    if (!string.IsNullOrEmpty(origGiir))
                                    {
                                        var origGiirRows = await _context.GiirdetailsSubs
                                            .Where(g => g.Giircode == origGiir
                                                     && g.SerialNo == serial.SerialNo
                                                     && g.PartCode == serial.PartCode)
                                            .ToListAsync();
                                        foreach (var g in origGiirRows) g.JobCardStatus = "J";
                                        await _context.SaveChangesAsync();
                                    }
                                }
                                else if (gc3 == "MTF")
                                {
                                    // Lock Material Transfer Form
                                    var mtfRows = await _context.MtfdetailsSubs
                                        .Where(m => m.Mtfcode == serial.Gcode
                                                 && m.SerialNo == serial.SerialNo
                                                 && m.PartCode == serial.PartCode)
                                        .ToListAsync();
                                    foreach (var m in mtfRows) m.JobCardStatus = "J";
                                    await _context.SaveChangesAsync();

                                    // Also lock original GIIR linked to this MTF
                                    var giirMtfRows = await _context.GiirdetailsSubs
                                        .Where(g => g.SerialNo == serial.SerialNo
                                                 && g.PartCode == serial.PartCode)
                                        .ToListAsync();
                                    foreach (var g in giirMtfRows) g.JobCardStatus = "J";
                                    await _context.SaveChangesAsync();
                                }
                            }
                            else if (pc3 == "401") // Canopy — different source documents
                            {
                                if (gc3 == "PSH")
                                {
                                    // Lock Process Feedback (canopy assembly output document)
                                    var pshRows = await _context.ProcessFeedbackDetailsSubs
                                        .Where(p => p.Pfbcode == serial.Gcode
                                                 && p.SerialNo == serial.SerialNo
                                                 && p.PartCode == serial.PartCode)
                                        .ToListAsync();
                                    foreach (var p in pshRows) p.JobCardStatus = "J";
                                    await _context.SaveChangesAsync();
                                }
                                else if (gc3 == "MTF")
                                {
                                    // Lock ProcessFeedback via TRFCode
                                    var pfbRows = await _context.ProcessFeedbackDetailsSubs
                                        .Where(p => p.Trfcode == serial.Gcode
                                                 && p.SerialNo == serial.SerialNo
                                                 && p.PartCode == serial.PartCode)
                                        .ToListAsync();
                                    foreach (var p in pfbRows) p.JobCardStatus = "J";
                                    await _context.SaveChangesAsync();

                                    // Also lock MTF record
                                    var mtfCpyRows = await _context.MtfdetailsSubs
                                        .Where(m => m.Mtfcode == serial.Gcode
                                                 && m.SerialNo == serial.SerialNo
                                                 && m.PartCode == serial.PartCode)
                                        .ToListAsync();
                                    foreach (var m in mtfCpyRows) m.JobCardStatus = "J";
                                    await _context.SaveChangesAsync();
                                }
                            }
                            #endregion

                            if (pc3 == "001") cntEng++;
                            else if (pc3 == "002") cntAlt++;
                            else if (pc3 == "010") cntBat++;
                            else if (pc2 == "40") cntCpy++;
                        }

                        #region STEP 10 — POST-INSERT SERIAL COUNT VALIDATION (within transaction)
                        // Mirrors original "checked SrNo To JobCardqty" region.
                        // Verifies that inserted serial count matches the requested Qty.
                        string descVal = await _context.Parts
                            .Where(p => p.PartCode == row.PartCode)
                            .Select(p => p.PartDesc)
                            .FirstOrDefaultAsync() ?? row.PartCode;

                        if (row.Qty > cntEng)
                        {
                            result = $"Engine SrNo Not available For DG {descVal}";
                            return; // await using disposes + rolls back transaction
                        }
                        else if (row.Qty > cntAlt)
                        {
                            result = $"Alternator SrNo Not available For DG {descVal}";
                            return;
                        }
                        else if (row.Qty > cntBat && await checkTranBOMForBat(row.PartCode))
                        {
                            result = $"Battery SrNo Not available For DG {descVal}";
                            return;
                        }
                        else if (row.Qty > cntCpy)
                        {
                            result = $"Canopy SrNo Not available For DG {descVal}";
                            return;
                        }
                        #endregion
                    }

                    #region STEP 11 — AUTO MATERIAL REQUISITION
                    // Raises REQ to logistics (23.001) for each DG row.
                    // Skipped for CompCode 28 (Bangalore) — business rule added 21/11/2025.
                    var reqCodes = new List<string>();

                    if (compCode != "28")
                    {
                        foreach (var row in activeRows)
                        {
                            // GETMAXNO INLINE — MaterialRequisitionWithOutPlan
                            var maxReqRecord = await _context.GetMaxCodes
                                .Where(g => g.TblName == "MaterialRequisitionWithOutPlan"
                                         && g.CompCode == compCode
                                         && g.Prefix == "REQ"
                                         && g.Yr == yr)
                                .FirstOrDefaultAsync();

                            int intMaxReq = maxReqRecord != null ? Convert.ToInt32(maxReqRecord.MaxValue) : 0;
                            string strMaxReq = intMaxReq switch
                            {
                                0 => "000001",
                                < 9 => "00000" + (intMaxReq + 1),
                                < 99 => "0000" + (intMaxReq + 1),
                                < 999 => "000" + (intMaxReq + 1),
                                < 9999 => "00" + (intMaxReq + 1),
                                < 99999 => "0" + (intMaxReq + 1),
                                _ => Convert.ToString(intMaxReq + 1)
                            };
                            string reqCode = $"REQ/{yr}/{compCode}{strMaxReq}";

                            // Update GetMaxCode via LINQ (update operation)
                            if (maxReqRecord != null)
                            {
                                maxReqRecord.MaxValue = int.Parse(strMaxReq);
                                await _context.SaveChangesAsync();
                            }

                            reqCodes.Add(reqCode);

                            // INSERT Requisition master header via SP
                            await _context.Database.ExecuteSqlRawAsync(
                                "EXEC insertMaterialRequisitionWithOutPlanProcessVsPlan " +
                                "@REQCode, @MaxSrNo, @Dt, @Yr, @ProfitCenterCode, @ToProfitCenterCode, " +
                                "@ClassCode, @ActNo, @SourceCode, @CompanyCode, @REQStatus, @REQType, " +
                                "@Remark, @Discard, @Active, @Auth",
                                new SqlParameter("@REQCode", reqCode),
                                new SqlParameter("@MaxSrNo", reqCode.Substring(10, 8)),
                                new SqlParameter("@Dt", DateTime.Now),
                                new SqlParameter("@Yr", yr),
                                new SqlParameter("@ProfitCenterCode", request.PCCode.Trim()),
                                new SqlParameter("@ToProfitCenterCode", "23.001"),
                                new SqlParameter("@ClassCode", row.PartCode?.Trim()),
                                new SqlParameter("@ActNo", row.Qty.ToString()),
                                new SqlParameter("@SourceCode", jobCardNo),
                                new SqlParameter("@CompanyCode", compCode),
                                new SqlParameter("@REQStatus", "P"),
                                new SqlParameter("@REQType", "WIP"),
                                new SqlParameter("@Remark", $"Auto Req For Plan No {jobCardNo}"),
                                new SqlParameter("@Discard", 1),
                                new SqlParameter("@Active", 1),
                                new SqlParameter("@Auth", 1));

                            // Fetch BOM component list via SP — data returning, use connection
                            // Qty = BOM qty per unit × number of DGs in this row
                            var bomRows = new List<(string PartCode, double Qty)>();
                            using (var bomCmd = new SqlCommand(
                                $"EXEC InternalReqLogisticsdetailsDG '{row.PartCode?.Trim()}'",
                                sqlConn, sqlTran))
                            {
                                using var bomReader = await bomCmd.ExecuteReaderAsync();
                                while (await bomReader.ReadAsync())
                                    bomRows.Add((
                                        bomReader["Partcode"]?.ToString()?.Trim() ?? "",
                                        double.Parse(bomReader["RaiseReqQty"]?.ToString() ?? "0")));
                            }

                            // INSERT one detail line per BOM component
                            int reqSrNo = 0;
                            foreach (var (bomPartCode, bomQty) in bomRows)
                            {
                                reqSrNo++;
                                await _context.Database.ExecuteSqlRawAsync(
                                    "EXEC insertMaterialRequisitionWithOutPlanDetails @REQCode, @SrNo, @PartCode, @Qty, @REQStatus",
                                    new SqlParameter("@REQCode", reqCode),
                                    new SqlParameter("@SrNo", reqSrNo),
                                    new SqlParameter("@PartCode", bomPartCode),
                                    new SqlParameter("@Qty", bomQty * row.Qty),
                                    new SqlParameter("@REQStatus", "P"));
                            }

                            // INSERT audit log entry per requisition
                            await _context.Database.ExecuteSqlRawAsync(
                                "EXEC insertLoginTransactionDetails @TransactionDtTime, @EmpID, @TransactionType, @TransactionFrom, @TransactionNo, @CompanyCode",
                                new SqlParameter("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd")),
                                new SqlParameter("@EmpID", "Auto Against Plan"),
                                new SqlParameter("@TransactionType", "S"),
                                new SqlParameter("@TransactionFrom", "MaterialRequisitionWithoutPlan"),
                                new SqlParameter("@TransactionNo", reqCode),
                                new SqlParameter("@CompanyCode", compCode));
                        }
                    }
                    #endregion

                    await transaction.CommitAsync();

                    string reqAll = string.Join("#", reqCodes);
                    result = string.IsNullOrEmpty(reqAll)
                        ? jobCardNo
                        : $"{jobCardNo} With Requisition No: {reqAll}";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    result = $"StackTrace {ex.StackTrace} Message {ex.Message}";
                }
            });

            return result;
        }


        // ══════════════════════════════════════════════════════════════════
        // METHOD 2 — GetPOPCcode
        // Fetches the ProfitCenterCode from the Purchase Order linked to a
        // GIIR document. Used to determine TransferStatus D or P.
        // Returns "0" if not exactly one matching PC found.
        // ══════════════════════════════════════════════════════════════════
        private async Task<string> GetPOPCcode(
            string giirCode, SqlConnection conn, SqlTransaction tran)
        {
            using var cmd = new SqlCommand(
                "SELECT ProfitcenterCode FROM Giir G " +
                "INNER JOIN GiirDetails Gd     ON G.GiirCode  = Gd.GiirCode " +
                "INNER JOIN PurchaseOrder PO   ON Gd.GIRDPOCode = PO.POCode " +
                $"WHERE G.GiirCode = '{giirCode}' " +
                "GROUP BY ProfitcenterCode",
                conn, tran);

            var results = new List<string>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(reader["ProfitcenterCode"]?.ToString()?.Trim() ?? "");

            return results.Count == 1 ? results[0] : "0";
        }


        // ══════════════════════════════════════════════════════════════════
        // METHOD 3 — checkTranBOMForBat
        // Checks if the BOM for a DG product code includes a battery (010...).
        // Returns false = no battery in BOM, skip battery serial check.
        // Returns true  = battery required, raise error if serials unavailable.
        // ══════════════════════════════════════════════════════════════════
        private async Task<bool> checkTranBOMForBat(string productCode)
        {
            return await _context.Bomdetails
                .Where(b => b.Kitcode == productCode
                         && b.PartCode.StartsWith("010"))
                .AnyAsync();
        }
    }
}
