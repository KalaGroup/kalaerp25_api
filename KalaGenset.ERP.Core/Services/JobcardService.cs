using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Jobcard;
using KalaGenset.ERP.Data.DbContexts;
using KalaGenset.ERP.Data.Models;
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

        public async Task<List<Dictionary<string, object>>> GetDGAsync(string strJobCardType, string strCompID, string strAssemblyLine)
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _context.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetJobCardDGDts_Checker_maker";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;

                    cmd.Parameters.Add(new SqlParameter("@JobCardType", SqlDbType.Char) { Value = strJobCardType });
                    cmd.Parameters.Add(new SqlParameter("@CompCode", SqlDbType.Char) { Value = strCompID });
                    cmd.Parameters.Add(new SqlParameter("@AssemblyLine", SqlDbType.VarChar, 10) { Value = strAssemblyLine });

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
            var activeRows = request.Plans?.Where(r => r.Qty > 0).ToList();
            if (activeRows == null || !activeRows.Any())
                return "No Job Card rows provided.";

            string compCode = request.pcCode_Act.Trim().Substring(0, 2);
            string result = "";

            // Financial year computed in C# — Apr to Mar cycle e.g. "25-26"
            string yr = DateTime.Now.Month >= 4
                ? $"{DateTime.Now:yy}-{DateTime.Now.AddYears(1):yy}"
                : $"{DateTime.Now.AddYears(-1):yy}-{DateTime.Now:yy}";

            try
            {
                #region STEP 1 — PRE-VALIDATION (before transaction)
                foreach (var row in activeRows)
                {
                    var preSerials = new List<(string PartCode, string SerialNo, string Gcode)>();

                    var preConn = (SqlConnection)_context.Database.GetDbConnection();
                    if (preConn.State == ConnectionState.Closed)
                        await preConn.OpenAsync();

                    //using (var srCmd = new SqlCommand("GetJobCardSrNo", preConn))
                    using (var srCmd = new SqlCommand("GetJobCardSrNo_Cheker_Maker", preConn))
                    {
                        srCmd.CommandType = CommandType.StoredProcedure;
                        srCmd.CommandTimeout = 0;
                        srCmd.Parameters.AddWithValue("@JobCodeType", "DGWOP");
                        srCmd.Parameters.AddWithValue("@PartCode", row.PartCode);
                        srCmd.Parameters.AddWithValue("@Qty", row.Qty);
                        srCmd.Parameters.AddWithValue("@CompCode", compCode);
                        srCmd.Parameters.AddWithValue("@AssemblyLine", request.pcCode_Act); //no need for GetJobCardSrNo sp
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

            var allJobCards = new List<string>();

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var sqlConn = (SqlConnection)_context.Database.GetDbConnection();
                    var sqlTran = (SqlTransaction)_context.Database.CurrentTransaction.GetDbTransaction();

                    var maxJcRecord = await _context.GetMaxCodes.Where(g => g.TblName == "JobCard" && g.CompCode == compCode
                                      && g.Prefix == "JCD" && g.Yr == yr).FirstOrDefaultAsync();

                    int intMaxJC = maxJcRecord != null ? Convert.ToInt32(maxJcRecord.MaxValue) : 0;

                    foreach (var row in activeRows)
                    {
                        #region STEP 2 — GETMAXNO INLINE — Generate unique JobCard number per row
                        intMaxJC++;  // Increment in memory each iteration

                        string strMaxJC = intMaxJC.ToString("D6");  // Pads to 6 digits: 000001, 001118, etc.
                        string jobCardNo = $"JCD/{yr}/{compCode}{strMaxJC}";
                       
                        if (maxJcRecord != null)
                        {
                            maxJcRecord.MaxValue = int.Parse(strMaxJC);
                            await _context.SaveChangesAsync();
                        }
                        #endregion

                        #region STEP 3 — INSERT JobCard master header
                        await _context.Database.ExecuteSqlRawAsync(
                            "INSERT INTO JobCard(JobCode,Dt,Yr,MaxSrNo,PCCode,Remark,CompanyCode,Active,Auth,PCCode_Act) " +
                            "VALUES(@JobCode,@Dt,@Yr,@MaxSrNo,@PCCode,@Remark,@CompCode,'1','0',@PCCode_Act)",
                            new SqlParameter("@JobCode", jobCardNo),
                            new SqlParameter("@Dt", DateTime.Now),
                            new SqlParameter("@Yr", yr),
                            new SqlParameter("@MaxSrNo", jobCardNo.Substring(10, 8)),
                            new SqlParameter("@PCCode", request.pcCode_Old.Trim()),
                            new SqlParameter("@Remark", request.Remark?.Trim() ?? ""),
                            new SqlParameter("@CompCode", compCode),
                            new SqlParameter("@PCCode_Act", request.pcCode_Act.Trim() ?? ""));
                        #endregion

                        int globalSrNo = 0;

                        #region STEP 4 — INSERT JobCardDetails (SrNo=1, one detail per job card)
                        await _context.Database.ExecuteSqlRawAsync(
                            "INSERT INTO JobCardDetails" +
                            "(JobCode,SrNo,BOMCode,PartCode,Qty,PlanCode,PlanDate,DayPlanQty," +
                            " Stage1Status,Stage2Status,Stage3Status) " +
                            "VALUES(@JobCode,@SrNo,@BOMCode,@PartCode,@Qty,@PlanCode,@PlanDate,@DayPlanQty,'P','P','P')",
                            new SqlParameter("@JobCode", jobCardNo),
                            new SqlParameter("@SrNo", 1),
                            new SqlParameter("@BOMCode", row.BOMCode?.Trim() ?? (object)DBNull.Value),
                            new SqlParameter("@PartCode", row.PartCode?.Trim() ?? (object)DBNull.Value),
                            new SqlParameter("@Qty", row.Qty),
                            new SqlParameter("@PlanCode", row.PlanCode?.Trim() ?? (object)DBNull.Value),
                            new SqlParameter("@PlanDate", row.PlanDate?.Trim() ?? (object)DBNull.Value),
                            new SqlParameter("@DayPlanQty", row.DayPlanQty ?? 0));
                        #endregion

                        double kva = row.KVA ?? 0;

                        #region STEP 5 — FETCH SERIAL NUMBERS within transaction
                        var serials = new List<(string PartCode, string SerialNo, string Gcode)>();
                        //using (var srCmd = new SqlCommand("GetJobCardSrNo", sqlConn, sqlTran))
                        using (var srCmd = new SqlCommand("GetJobCardSrNo_Cheker_Maker", sqlConn, sqlTran))
                        {
                            srCmd.CommandType = CommandType.StoredProcedure;
                            srCmd.CommandTimeout = 0;
                            srCmd.Parameters.AddWithValue("@JobCodeType", "DGWOP");
                            srCmd.Parameters.AddWithValue("@PartCode", row.PartCode);
                            srCmd.Parameters.AddWithValue("@Qty", row.Qty);
                            srCmd.Parameters.AddWithValue("@CompCode", compCode);
                            srCmd.Parameters.AddWithValue("@AssemblyLine", request.pcCode_Act); // no need for GetJobCardSrNo sp
                            using var srReader = await srCmd.ExecuteReaderAsync();
                            while (await srReader.ReadAsync())
                                serials.Add((
                                    srReader["PartCode"]?.ToString()?.Trim() ?? "",
                                    srReader["SerialNo"]?.ToString()?.Trim() ?? "",
                                    srReader["Gcode"]?.ToString()?.Trim() ?? ""));
                        }

                        // === LOG 1: GetJobCardSrNo output ===
                        Console.WriteLine($"[JC-LOG] === GetJobCardSrNo output for JobCode={jobCardNo}, PartCode={row.PartCode}, Qty={row.Qty}, KVA={kva} ===");
                        Console.WriteLine($"[JC-LOG] Total rows returned: {serials.Count}");
                        for (int i = 0; i < serials.Count; i++)
                        {
                            var s = serials[i];
                            Console.WriteLine($"[JC-LOG] Row {i + 1}: PartCode={s.PartCode}, SerialNo={s.SerialNo}, Gcode={s.Gcode}");
                        }
                        // === END LOG 1 ===

                        if (!serials.Any()) continue;
                        #endregion

                        //int jpEng = 0, jpAlt = 0, jpBat = 0, jpCpy = 0;
                        //int batCnt = 0;
                        //int cntEng = 0, cntAlt = 0, cntBat = 0, cntCpy = 0;

                        int jpEng = 0, jpAlt = 0, jpBat = 0, jpCpy = 0;
                        int jpBatRaw = 0;          // raw battery group counter — increments every 2 batteries, never wraps
                        int batCnt = 0;
                        int cntEng = 0, cntAlt = 0, cntBat = 0, cntCpy = 0;

                        // Fetch batteriesPerDG from BOM (same source GetJobCardSrNo uses)
                        int batteriesPerDG = 1;
                        using (var bomCmd = new SqlCommand(@"SELECT TOP 1 CAST(Bd.Qty AS INT)
                               FROM BOM B 
                               INNER JOIN BOMDetails Bd ON B.BOMCode = Bd.BomCode
                               INNER JOIN Part P ON B.Partcode = P.Partcode
                               WHERE B.Active = '1' AND B.Discard = '1' 
                               AND P.Active = '1' AND P.Discard = '1' 
                               AND Bd.KITCode = @KitCode 
                               AND SUBSTRING(Bd.partcode, 1, 3) = '010'", sqlConn, sqlTran))
                        {
                            bomCmd.Parameters.AddWithValue("@KitCode", row.PartCode);
                            var bomResult = await bomCmd.ExecuteScalarAsync();
                            if (bomResult != null && bomResult != DBNull.Value)
                                batteriesPerDG = Convert.ToInt32(bomResult);
                        }
                        Console.WriteLine($"[JC-LOG] BOM batteriesPerDG for {row.PartCode}: {batteriesPerDG}");

                        foreach (var serial in serials)
                        {
                            string pc3 = serial.PartCode.Length >= 3 ? serial.PartCode.Substring(0, 3) : "";
                            string pc2 = serial.PartCode.Length >= 2 ? serial.PartCode.Substring(0, 2) : "";
                            string gc3 = serial.Gcode.Length >= 3 ? serial.Gcode.Substring(0, 3) : "";

                            //#region STEP 6 — CALCULATE JPRIORITY
                            //int jPriority = 0;
                            //int batCntBefore = batCnt;     // capture state before
                            //int jpBatBefore = jpBat;
                            //if (pc3 == "001") { jpEng++; jPriority = jpEng; }
                            //else if (pc3 == "002") { jpAlt++; jPriority = jpAlt; }
                            //else if (pc3 == "401") { jpCpy++; jPriority = jpCpy; }
                            //else if (pc3 == "010" && kva <= 200) { jpBat++; jPriority = jpBat; }
                            //else if (pc3 == "010" && kva > 200)
                            //{
                            //    if (batCnt == 0) { jpBat++; batCnt = 1; }
                            //    else { batCnt = 0; }
                            //    jPriority = jpBat;
                            //}

                            //// === LOG 2: JPriority assignment per serial ===
                            //Console.WriteLine(
                            //    $"[JC-LOG] JPRI: SerialNo={serial.SerialNo}, SrNoPartCode={serial.PartCode}, pc3={pc3}, kva={kva}, " +
                            //    $"batCntBefore={batCntBefore}, batCntAfter={batCnt}, jpBatBefore={jpBatBefore}, jpBatAfter={jpBat}, " +
                            //    $"=> JPriority={jPriority}");
                            //// === END LOG 2 ===
                            //#endregion

                            #region STEP 6 — CALCULATE JPRIORITY
                            int jPriority = 0;
                            int batCntBefore = batCnt;
                            int jpBatRawBefore = jpBatRaw;

                            if (pc3 == "001") { jpEng++; jPriority = jpEng; }
                            else if (pc3 == "002") { jpAlt++; jPriority = jpAlt; }
                            else if (pc3 == "401") { jpCpy++; jPriority = jpCpy; }
                            else if (pc3 == "010")
                            {
                                // Increment jpBatRaw every batteriesPerDG batteries (1 → every battery, 2 → every 2nd, 4 → every 2nd pair)
                                // The "every 2 batteries" toggle stays for >1 batteries-per-DG; for 1 battery-per-DG, increment every time.
                                if (batteriesPerDG <= 1)
                                {
                                    jpBatRaw++;
                                }
                                else
                                {
                                    if (batCnt == 0) { jpBatRaw++; batCnt = 1; }
                                    else { batCnt = 0; }
                                }

                                // Wrap JPriority back to plan range using row.Qty
                                // Examples (Qty=2, batteriesPerDG=4):
                                //   Batteries 1-2 → jpBatRaw=1 → JP=1
                                //   Batteries 3-4 → jpBatRaw=2 → JP=2
                                //   Batteries 5-6 → jpBatRaw=3 → JP=1 (wraps back)
                                //   Batteries 7-8 → jpBatRaw=4 → JP=2 (wraps back)
                                int qty = row.Qty;
                                jPriority = qty > 0 ? ((jpBatRaw - 1) % qty) + 1 : jpBatRaw;
                                jpBat = jPriority;   // keep jpBat in sync for any downstream code/validation
                            }

                            // === LOG 2: JPriority assignment per serial ===
                            Console.WriteLine(
                                $"[JC-LOG] JPRI: SerialNo={serial.SerialNo}, SrNoPartCode={serial.PartCode}, pc3={pc3}, kva={kva}, " +
                                $"batCntBefore={batCntBefore}, batCntAfter={batCnt}, jpBatRawBefore={jpBatRawBefore}, jpBatRawAfter={jpBatRaw}, " +
                                $"batteriesPerDG={batteriesPerDG}, qty={row.Qty}, => JPriority={jPriority}");
                            // === END LOG 2 ===
                            #endregion

                            #region STEP 7 — DETERMINE TRANSFERSTATUS (D=Direct, P=Pending)
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
                                transferStatus = poPcCode == request.pcCode_Old.Trim() ? "D" : "P";
                            }
                            else { transferStatus = "P"; }
                            #endregion

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
                            if (pc3 == "001" || pc3 == "002" || pc3 == "010")
                            {
                                if (gc3 == "GIR")
                                {
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
                                    var cnsRows = await _context.ConvertSerialNoDetails
                                        .Where(c => c.Cnvcode == serial.Gcode
                                                 && c.SerialNo == serial.SerialNo)
                                        .ToListAsync();
                                    foreach (var c in cnsRows) c.JobCardStatus = "J";
                                    await _context.SaveChangesAsync();

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
                                    var mtfRows = await _context.MtfdetailsSubs
                                        .Where(m => m.Mtfcode == serial.Gcode
                                                 && m.SerialNo == serial.SerialNo
                                                 && m.PartCode == serial.PartCode)
                                        .ToListAsync();
                                    foreach (var m in mtfRows) m.JobCardStatus = "J";
                                    await _context.SaveChangesAsync();

                                    var giirMtfRows = await _context.GiirdetailsSubs
                                        .Where(g => g.SerialNo == serial.SerialNo
                                                 && g.PartCode == serial.PartCode)
                                        .ToListAsync();
                                    foreach (var g in giirMtfRows) g.JobCardStatus = "J";
                                    await _context.SaveChangesAsync();
                                }
                            }
                            else if (pc3 == "401")
                            {
                                if (gc3 == "PSH")
                                {
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
                                    var pfbRows = await _context.ProcessFeedbackDetailsSubs
                                        .Where(p => p.Trfcode == serial.Gcode
                                                 && p.SerialNo == serial.SerialNo
                                                 && p.PartCode == serial.PartCode)
                                        .ToListAsync();
                                    foreach (var p in pfbRows) p.JobCardStatus = "J";
                                    await _context.SaveChangesAsync();

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

                            // === LOG 3: Final counters per JobCard ===
                            Console.WriteLine(
                                $"[JC-LOG] === SUMMARY for JobCode={jobCardNo}, PartCode={row.PartCode}, Qty={row.Qty}, KVA={kva} ===");
                            Console.WriteLine(
                                $"[JC-LOG] Counts: Eng={cntEng}, Alt={cntAlt}, Bat={cntBat}, Cpy={cntCpy}");
                            Console.WriteLine(
                                $"[JC-LOG] JPriority maxes: jpEng={jpEng}, jpAlt={jpAlt}, jpBat={jpBat}, jpCpy={jpCpy}");
                            // === END LOG 3 ===
                        }

                        #region STEP 10 — POST-INSERT SERIAL COUNT VALIDATION
                        string descVal = await _context.Parts
                            .Where(p => p.PartCode == row.PartCode)
                            .Select(p => p.PartDesc)
                            .FirstOrDefaultAsync() ?? row.PartCode;

                        if (row.Qty > cntEng)
                        {
                            result = $"Engine SrNo Not available For DG {descVal}";
                            return;
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

                        // ── NO MATERIAL REQUISITION HERE — moved to checker approval ──

                        allJobCards.Add(jobCardNo);

                    } // END foreach row

                    await transaction.CommitAsync();

                    result = string.Join("#", allJobCards);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            return result;
        }

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

        public async Task<List<Dictionary<string, object>>> GetDGJobcard1CheckerDetails(string jobCode)
        {
            var data = new List<Dictionary<string, object>>();
            using (var conn = _context.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "getUnauthorizedJobCardDetails";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;
                    cmd.Parameters.Add(new SqlParameter("@JobCode", SqlDbType.VarChar) { Value = jobCode.Trim() });
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

        public async Task<List<string>> GetPendingAuthJobCodes()
        {
            return await _context.JobCards
                .Where(jc => jc.Auth == false)
                .Select(jc => jc.JobCode)
                .ToListAsync();
        }

        public async Task<List<Dictionary<string, object>>> GetPlanDetails(string jobCode)
        {
            var data = new List<Dictionary<string, object>>();
            using (var conn = _context.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetPlanDetails_Checker_Maker";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;
                    cmd.Parameters.Add(new SqlParameter("@JobCode", SqlDbType.VarChar) { Value = jobCode.Trim() });
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

        public async Task<string> SubmitJobcard1Checker(Jobcard1CheckerSubmitRequest jobcard1CheckerSubmitReq)
        {
            try
            {
                var jobCard = await _context.JobCards.FirstOrDefaultAsync(x => x.JobCode == jobcard1CheckerSubmitReq.JobCode);

                if (jobCard == null)
                    return "Job Card not found.";

                string compCode = jobCard.CompanyCode?.Trim() ?? "";
                string yr = jobCard.Yr?.Trim() ?? "";
                string result = "";

                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var sqlConn = (SqlConnection)_context.Database.GetDbConnection();
                        var sqlTran = (SqlTransaction)_context.Database.CurrentTransaction.GetDbTransaction();

                        if (jobcard1CheckerSubmitReq.Status == "Auth")
                        {
                            jobCard.Auth = true;
                            await _context.SaveChangesAsync();

                            foreach (var detail in jobcard1CheckerSubmitReq.Details)
                            {
                                await _context.Database.ExecuteSqlRawAsync(
                                    "INSERT INTO JobCardCheckerDetails (PlanCode, SixMName, Description, AssignTo, CorReqNo, Status) " +
                                    "VALUES (@PlanCode, @SixMName, @Description, @AssignTo, @CorReqNo, @Status)",
                                    new SqlParameter("@PlanCode", jobcard1CheckerSubmitReq.JobCode),
                                    new SqlParameter("@SixMName", detail.SixM ?? ""),
                                    new SqlParameter("@Description", detail.Description ?? ""),
                                    new SqlParameter("@AssignTo", detail.AssignTo ?? "0"),
                                    new SqlParameter("@CorReqNo", "0"),
                                    new SqlParameter("@Status", "AUTH"));
                            }

                            //if (compCode == "28")
                            //{
                            //    await transaction.CommitAsync();
                            //    result = $"{jobcard1CheckerSubmitReq.JobCode} authorized. No requisition required for this company.";
                            //    return;
                            //}

                            string pcCode = jobCard.PccodeAct?.Trim() ?? "";

                            var jobCard1Details = await _context.JobCardDetails
                                .Where(d => d.JobCode == jobcard1CheckerSubmitReq.JobCode)
                                .Select(d => new { d.PartCode, d.Qty })
                                .ToListAsync();

                            if (!jobCard1Details.Any())
                            {
                                await transaction.CommitAsync();
                                result = $"{jobcard1CheckerSubmitReq.JobCode} authorized. But No Job Card Details found for Requisition.";
                                return;
                            }

                            int intMaxReq = await _context.GetMaxCodes
                                .Where(g => g.TblName == "MaterialRequisitionWithOutPlan"
                                         && g.CompCode == compCode
                                         && g.Prefix == "REQ"
                                         && g.Yr == yr)
                                .Select(g => (int?)g.MaxValue)
                                .FirstOrDefaultAsync() ?? 0;                          

                            var reqCodes = new List<string>();

                            string profitCenterCodeAct = "";
                            string toprofitCenterCode = "";

                            if (jobcard1CheckerSubmitReq.PCCode_Act == "01.106"
                                || jobcard1CheckerSubmitReq.PCCode_Act == "03.092"
                                || jobcard1CheckerSubmitReq.PCCode_Act == "03.123")
                            {
                                profitCenterCodeAct = "23.001";
                                toprofitCenterCode = "23.001";
                            }
                            else if (jobcard1CheckerSubmitReq.PCCode_Act == "28.037"
                                || jobcard1CheckerSubmitReq.PCCode_Act == "28.040"
                                || jobcard1CheckerSubmitReq.PCCode_Act == "28.117")
                            {
                                profitCenterCodeAct = "28.020";
                                toprofitCenterCode = "28.020";
                            }

                            foreach (var detail in jobCard1Details)
                            {
                                string partCode = detail.PartCode?.Trim() ?? "";
                                double qty = detail.Qty;

                                intMaxReq++;
                                string strMaxReq = intMaxReq.ToString("D6");
                                string reqCode = $"REQ/{yr}/{compCode}{strMaxReq}";
                                

                                await _context.Database.ExecuteSqlRawAsync(
                                    "EXEC insertMaterialRequisitionWithOutPlanProcessVsPlan " +
                                    "@REQCode, @MaxSrNo, @Dt, @Yr, @ProfitCenterCode, @ToProfitCenterCode, @ProfitCenterCode_Act, @ToProfitCenterCode_Act, " +
                                    "@ClassCode, @ActNo, @SourceCode, @CompanyCode, @REQStatus, @REQType, " +
                                    "@Remark, @Discard, @Active, @Auth",
                                    new SqlParameter("@REQCode", reqCode),
                                    new SqlParameter("@MaxSrNo", reqCode.Substring(10, 8)),
                                    new SqlParameter("@Dt", DateTime.Now),
                                    new SqlParameter("@Yr", yr),
                                    new SqlParameter("@ProfitCenterCode", jobcard1CheckerSubmitReq.PCCode_Old),
                                    new SqlParameter("@ToProfitCenterCode", toprofitCenterCode),
                                    new SqlParameter("@ProfitCenterCode_Act", jobcard1CheckerSubmitReq.PCCode_Act),
                                    new SqlParameter("@ToProfitCenterCode_Act", profitCenterCodeAct),
                                    new SqlParameter("@ClassCode", partCode),
                                    new SqlParameter("@ActNo", qty.ToString()),
                                    new SqlParameter("@SourceCode", jobcard1CheckerSubmitReq.JobCode),
                                    new SqlParameter("@CompanyCode", compCode),
                                    new SqlParameter("@REQStatus", "P"),
                                    new SqlParameter("@REQType", "WIP"),
                                    new SqlParameter("@Remark", $"After Done Authorization Of Jobcard,Auto Req Raised For Plan No {jobcard1CheckerSubmitReq.JobCode}"),
                                    new SqlParameter("@Discard", 1),
                                    new SqlParameter("@Active", 1),
                                    new SqlParameter("@Auth", 1));                               

                                var bomRows = new List<(string PartCode, double Qty)>();
                                using (var bomCmd = new SqlCommand(
                                    $"EXEC InternalReqLogisticsdetailsDG '{partCode}'",
                                    sqlConn, sqlTran))
                                {
                                    using var bomReader = await bomCmd.ExecuteReaderAsync();
                                    while (await bomReader.ReadAsync())
                                        bomRows.Add((
                                            bomReader["Partcode"]?.ToString()?.Trim() ?? "",
                                            double.Parse(bomReader["RaiseReqQty"]?.ToString() ?? "0")));
                                }                              

                                int reqSrNo = 0;
                                foreach (var (bomPartCode, bomQty) in bomRows)
                                {
                                    reqSrNo++;                                   
                                    await _context.Database.ExecuteSqlRawAsync(
                                        "EXEC insertMaterialRequisitionWithOutPlanDetails @REQCode, @SrNo, @PartCode, @Qty, @REQStatus",
                                        new SqlParameter("@REQCode", reqCode),
                                        new SqlParameter("@SrNo", reqSrNo),
                                        new SqlParameter("@PartCode", bomPartCode),
                                        new SqlParameter("@Qty", bomQty * qty),
                                        new SqlParameter("@REQStatus", "P"));
                                }

                                await _context.Database.ExecuteSqlRawAsync(
                                    "EXEC insertLoginTransactionDetails @TransactionDtTime, @EmpID, @TransactionType, @TransactionFrom, @TransactionNo, @CompanyCode",
                                    new SqlParameter("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd")),
                                    new SqlParameter("@EmpID", jobcard1CheckerSubmitReq.EmpCode),
                                    new SqlParameter("@TransactionType", "S"),
                                    new SqlParameter("@TransactionFrom", "Jobcard Checker"),
                                    new SqlParameter("@TransactionNo", reqCode),
                                    new SqlParameter("@CompanyCode", compCode));

                                Console.WriteLine($"[AUTH-AUDIT] ReqCode: {reqCode} audit log inserted");

                                reqCodes.Add(reqCode);
                            }
                         
                            await _context.Database.ExecuteSqlRawAsync(
                                "UPDATE GetMaxCode SET MaxValue = @MaxValue " +
                                "WHERE TblName = @TblName AND CompCode = @CompCode AND Prefix = @Prefix AND Yr = @Yr",
                                new SqlParameter("@MaxValue", intMaxReq),
                                new SqlParameter("@TblName", "MaterialRequisitionWithOutPlan"),
                                new SqlParameter("@CompCode", compCode),
                                new SqlParameter("@Prefix", "REQ"),
                                new SqlParameter("@Yr", yr));                          

                            result = reqCodes.Any()
                                ? $"{jobcard1CheckerSubmitReq.JobCode} Authorized Successfully. Requisition No(s): {string.Join(" | ", reqCodes)}"
                                : $"{jobcard1CheckerSubmitReq.JobCode} Authorized Successfully. No Requisitions Generated.";
                        }
                        else
                        {
                            jobCard.Auth = false;
                            int intMaxReq = await _context.GetMaxCodes
                                .Where(g => g.TblName == "CorporateRequisition"
                                         && g.CompCode == compCode
                                         && g.Prefix == "COR"
                                         && g.Yr == yr)
                                .Select(g => (int?)g.MaxValue)
                                .FirstOrDefaultAsync() ?? 0;

                            int originalMaxValue = intMaxReq;
                            var rejReqCodes = new List<string>();                           

                            foreach (var detail in jobcard1CheckerSubmitReq.Details)
                            {
                                string reqCode = "0";

                                if (!string.IsNullOrWhiteSpace(detail.AssignTo))
                                {
                                    intMaxReq++;
                                    string strMaxReq = intMaxReq.ToString("D6");
                                    reqCode = $"COR/{yr}/{compCode}{strMaxReq}";                                   
                                }
                                else
                                {
                                    Console.WriteLine($"[REJ-LOOP] AssignTo is blank, skipping COR generation, SixM: {detail.SixM}");
                                }

                                await _context.Database.ExecuteSqlRawAsync(
                                    "INSERT INTO JobCardCheckerDetails (PlanCode, SixMName, Description, AssignTo, CorReqNo, Status) " +
                                    "VALUES (@PlanCode, @SixMName, @Description, @AssignTo, @CorReqNo, @Status)",
                                    new SqlParameter("@PlanCode", jobcard1CheckerSubmitReq.JobCode),
                                    new SqlParameter("@SixMName", detail.SixM ?? ""),
                                    new SqlParameter("@Description", detail.Description ?? ""),
                                    new SqlParameter("@AssignTo", detail.AssignTo ?? "0"),
                                    new SqlParameter("@CorReqNo", reqCode),
                                    new SqlParameter("@Status", "REJ"));

                                if (!string.IsNullOrWhiteSpace(detail.AssignTo))
                                {
                                    string ReqMsg = $"Jobcard Checker  JobCode: {jobcard1CheckerSubmitReq.JobCode?.Trim()}, " +
                                                    $"6MType: {detail.SixM?.Trim()}, " +
                                                    $"Description: {detail.Description?.Trim()}";

                                    string ToPCCode = _context.Employees
                                        .Where(e => e.Ecode == detail.AssignTo)
                                        .Select(e => e.ProfitCenter)
                                        .FirstOrDefault() ?? "0";

                                    await _context.Database.ExecuteSqlRawAsync(
                                        "INSERT INTO CorporateRequisition " +
                                        "(ReqCode, Dt, Yr, MaxSrNo, EmpCode, FromPCCode, ToEmpCode, ToPCCode, Priority, ReqMsg, CompanyCode, AssignStatus, Active, Discard) " +
                                        "VALUES (@ReqCode, @Dt, @Yr, @MaxSrNo, @EmpCode, @FromPCCode, @ToEmpCode, @ToPCCode, @Priority, @ReqMsg, @CompanyCode, @AssignStatus, @Active, @Discard)",
                                        new SqlParameter("@ReqCode", reqCode.Trim()),
                                        new SqlParameter("@Dt", DateTime.Now) { SqlDbType = SqlDbType.DateTime },
                                        new SqlParameter("@Yr", reqCode.Substring(4, 5)),
                                        new SqlParameter("@MaxSrNo", reqCode.Substring(12, 6)),
                                        new SqlParameter("@EmpCode", jobcard1CheckerSubmitReq.EmpCode),
                                        new SqlParameter("@FromPCCode", jobcard1CheckerSubmitReq.PCCode_Old),
                                        new SqlParameter("@ToEmpCode", detail.AssignTo),
                                        new SqlParameter("@ToPCCode", ToPCCode),
                                        new SqlParameter("@Priority", "High Priority"),
                                        new SqlParameter("@ReqMsg", ReqMsg.Trim()),
                                        new SqlParameter("@CompanyCode", reqCode.Substring(10, 2)),
                                        new SqlParameter("@AssignStatus", "P"),
                                        new SqlParameter("@Active", "1"),
                                        new SqlParameter("@Discard", "1"));

                                    using var actionCmd = new SqlCommand(
                                        "INSERT INTO CorporateRequisitionActionTaken " +
                                        "(Dt, ReqCode, AssignByCode, AssignToCode, ActionTaken, Priority, ActionStatus, AssOrAction, Active, Discard) " +
                                        "VALUES (@Dt, @ReqCode, @AssignByCode, @AssignToCode, @ActionTaken, @Priority, @ActionStatus, @AssOrAction, @Active, @Discard); " +
                                        "SELECT SCOPE_IDENTITY();",
                                        sqlConn, sqlTran);

                                    actionCmd.Parameters.AddRange(new[]
                                    {
                                        new SqlParameter("@Dt",           DateTime.Now) { SqlDbType = SqlDbType.DateTime },
                                       new SqlParameter("@ReqCode",      reqCode.Trim()),
                                       new SqlParameter("@AssignByCode", jobcard1CheckerSubmitReq.EmpCode),
                                       new SqlParameter("@AssignToCode", detail.AssignTo ?? "0"),
                                       new SqlParameter("@ActionTaken",  ""),
                                       new SqlParameter("@Priority",     "High Priority"),
                                       new SqlParameter("@ActionStatus", "P"),
                                       new SqlParameter("@AssOrAction",  "ASS"),
                                       new SqlParameter("@Active",       "1"),
                                       new SqlParameter("@Discard",      "1")
                                    });

                                    object lblDispID = await actionCmd.ExecuteScalarAsync();

                                    await _context.CorporateRequisitions
                                        .Where(r => r.ReqCode == reqCode.Trim())
                                        .ExecuteUpdateAsync(s => s.SetProperty(r => r.AssignStatus, "C"));

                                    Console.WriteLine($"[REJ-COR] ReqCode: {reqCode} inserted, ActionID: {lblDispID}");

                                    rejReqCodes.Add(reqCode);
                                }
                            }

                            if (intMaxReq > originalMaxValue)
                            {                              
                                await _context.Database.ExecuteSqlRawAsync(
                                    "UPDATE GetMaxCode SET MaxValue = @MaxValue " +
                                    "WHERE TblName = @TblName AND CompCode = @CompCode AND Prefix = @Prefix AND Yr = @Yr",
                                    new SqlParameter("@MaxValue", intMaxReq),
                                    new SqlParameter("@TblName", "CorporateRequisition"),
                                    new SqlParameter("@CompCode", compCode),
                                    new SqlParameter("@Prefix", "COR"),
                                    new SqlParameter("@Yr", yr));
                            }

                            await _context.Database.ExecuteSqlRawAsync(
                                   "EXEC insertLoginTransactionDetails @TransactionDtTime, @EmpID, @TransactionType, @TransactionFrom, @TransactionNo, @CompanyCode",
                                   new SqlParameter("@TransactionDtTime", DateTime.Now.ToString("yyyy-MM-dd")),
                                   new SqlParameter("@EmpID", jobcard1CheckerSubmitReq.EmpCode),
                                   new SqlParameter("@TransactionType", "Rej"),
                                   new SqlParameter("@TransactionFrom", "Jobcard Checker"),
                                   new SqlParameter("@TransactionNo", rejReqCodes),
                                   new SqlParameter("@CompanyCode", compCode));


                            result = rejReqCodes.Any()
                                ? $"{jobcard1CheckerSubmitReq.JobCode} Rejected Successfully. Corporate Requisition No(s): {string.Join(" | ", rejReqCodes)}"
                                : $"{jobcard1CheckerSubmitReq.JobCode} Rejected. No Requisitions Generated.";
                        }

                        await transaction.CommitAsync();                        
                    }
                    catch (Exception ex)
                    {                       
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] JobCode: {jobcard1CheckerSubmitReq.JobCode}, Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return $"Error processing JobCode {jobcard1CheckerSubmitReq.JobCode}: {ex.Message}";
            }
        }

        public async Task<List<Dictionary<string, object?>>> GetJobCardDG2DtsAsync(string strJobCardType, string strcompID, string strAssemblyLine)
        {
            var result = new List<Dictionary<string, object?>>();
            try
            {               
                using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                using (var command = new SqlCommand("GetJobCardDGDts_Checker_Maker", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@JobCardType", strJobCardType ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CompCode", strcompID ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@AssemblyLine", strAssemblyLine ?? (object)DBNull.Value);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var columnName = reader.GetName(i);
                                var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                                row[columnName] = value;
                            }

                            result.Add(row);
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

            return result;
        }

        public async Task<List<Dictionary<string, object?>>> GetJobCard2CPAsync()
        {
            var result = new List<Dictionary<string, object?>>();

            using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
            using (var command = new SqlCommand("GetJObCard2CP", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = reader.GetName(i);
                            var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                            row[columnName] = value;
                        }

                        result.Add(row);
                    }
                }
            }

            return result;
        }


        public async Task<string> GetCPStkAsync(string strKVA, string ph, string panelType, string compId, string assemblyLine)
        {
            string spName = compId == "28" ? "GetCPStk_Bangalore_Checker_Maker" : "GetCPStk_Checker_Maker";
            string cpStk = "";

            using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
            using (var command = new SqlCommand(spName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@KVA", strKVA?.Trim() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ph", ph?.Trim() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PanelType", panelType?.Trim() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AssemblyLine", assemblyLine ?? (object)DBNull.Value);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    int rowIndex = 0;
                    while (await reader.ReadAsync())
                    {
                        string stk = reader["Stk"]?.ToString()?.Trim() ?? "";
                        cpStk = rowIndex == 0 ? stk : cpStk + "-->" + stk;
                        rowIndex++;
                    }
                }
            }

            if (string.IsNullOrEmpty(cpStk))
                cpStk = "0-->0";

            if (cpStk.Length < 5)
                cpStk = cpStk + "-->0";

            return cpStk;
        }

        // ══════════════════════════════════════════════════════════════════
        // METHOD — GetJobCard2ReportAsync
        // Calls stored proc: JobCard2Report
        // Example: EXEC JobCard2Report '03', '03.123', '2026-02-01', '2026-02-28';
        // Params: @CompanyCode, @AssemblyLine, @FromDate, @ToDate
        // ══════════════════════════════════════════════════════════════════
        public async Task<List<Dictionary<string, object>>> GetJobCard2ReportAsync(
            string companyCode, string assemblyLine, DateTime fromDate, DateTime toDate)
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _context.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "JobCard2Report";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;

                    cmd.Parameters.Add(new SqlParameter("@CompanyCode",  SqlDbType.VarChar, 10) { Value = companyCode  ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@AssemblyLine", SqlDbType.VarChar, 10) { Value = assemblyLine ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@FromDate",     SqlDbType.DateTime)    { Value = fromDate });
                    cmd.Parameters.Add(new SqlParameter("@ToDate",       SqlDbType.DateTime)    { Value = toDate });

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

        // ══════════════════════════════════════════════════════════════════
        // METHOD — GetJobCard1ReportAsync
        // Calls stored proc: JobCard1Report
        // Params: @CompanyCode, @AssemblyLine, @FromDate, @ToDate
        // Returns one row per JobCard plan with stage progress.
        // ══════════════════════════════════════════════════════════════════
        public async Task<List<Dictionary<string, object>>> GetJobCard1ReportAsync(
            string companyCode, string assemblyLine, DateTime fromDate, DateTime toDate)
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _context.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "JobCard1Report";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;

                    cmd.Parameters.Add(new SqlParameter("@CompanyCode",  SqlDbType.VarChar, 10) { Value = companyCode  ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@AssemblyLine", SqlDbType.VarChar, 10) { Value = assemblyLine ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@FromDate",     SqlDbType.DateTime)    { Value = fromDate });
                    cmd.Parameters.Add(new SqlParameter("@ToDate",       SqlDbType.DateTime)    { Value = toDate });

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
    }
}
