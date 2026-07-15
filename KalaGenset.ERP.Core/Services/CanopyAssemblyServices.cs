using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.CanopyAssembly;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KalaGenset.ERP.Core.Services
{
    public class CanopyAssemblyServices : ICanopyAssembly
    {
        private readonly KalaDbContext _context;

        // Production-line PCs that the grid + save flow accept. The first
        // three are the legacy ones the .aspx page silently locked to; the
        // 01.124-126 entries are the LineWisePC values now surfaced by the
        // Line dropdown (line-rights migration). Anything else short-circuits
        // the grid load / save with no rows / an "invalid PC" error.
        private static readonly HashSet<string> AllowedProductionPCs = new(StringComparer.OrdinalIgnoreCase)
        {
            "01.005", "03.038", "01.093",
            "01.124", "01.125", "01.126",
        };

        // PC = 01.093 (KanBan / Flat-Pack-Bhosari) triggers the auto-REQ branch.
        private const string KanbanPC = "01.093";

        // The fixed "standard rate" Profit Center used by the grid SP and
        // every Rate/Wt/SqFt lookup. Hard-coded in legacy.
        private const string StandardRatePC = "01.007";

        public CanopyAssemblyServices(KalaDbContext context)
        {
            _context = context;
        }

        // ════════════════════════════════════════════════════════════════
        //  Flat Pack Canopy Plan Report (already shipped)
        // ════════════════════════════════════════════════════════════════
        public async Task<List<Dictionary<string, object?>>> GetFlatPackCanopyPlanReportAsync(
            string pcCode,
            DateTime fromDate,
            DateTime toDate)
        {
            var result = new List<Dictionary<string, object?>>();
            var pc = (pcCode ?? string.Empty).Trim();

            var fromDateTime = fromDate.Date;
            var toDateTime   = toDate.Date.AddDays(1).AddTicks(-1);

            const string sql = @"
SELECT  PF.PFBCode,
        CONVERT(NVARCHAR(10), PF.Dt, 103)                          AS PrcDt,
        P.KVA,
        P.Phase,
        P.Model,
        P.PartDesc + '-->' + PF.ProductCode                        AS CanopyPartCode,
        P.PartDesc + '-->' + PF.PartCode                           AS NestingPartCode,
        PC.PCName  + '-->' + PF.ProfitCenterCode                   AS ProfitCenter,
        PF.TurretKitCode                                           AS BOMCode,
        PF.ProcessQty,
        PF.pfbrate                                                 AS Rate,
        ROUND(ISNULL(PF.PFBRate, 0) * ISNULL(PF.ProcessQty, 0), 2) AS Amount
FROM ProcessFeedBack PF WITH (NOLOCK)
INNER JOIN Part         P  WITH (NOLOCK) ON PF.ProductCode      = P.PartCode
INNER JOIN ProfitCenter PC WITH (NOLOCK) ON PF.ProfitCenterCode = PC.PCCode
WHERE PF.ProfitCenterCode = @PCCode
  AND PF.PartCode LIKE '004%'
  AND PF.Dt >= @FromDate
  AND PF.Dt <= @ToDate
ORDER BY PF.Dt DESC, PF.PFBCode;";

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@PCCode",   SqlDbType.VarChar, 50).Value = pc;
            command.Parameters.Add("@FromDate", SqlDbType.DateTime).Value    = fromDateTime;
            command.Parameters.Add("@ToDate",   SqlDbType.DateTime).Value    = toDateTime;

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var colName = reader.GetName(i);
                    var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                    if (value is string s) value = s.Trim();
                    row[colName] = value;
                }
                result.Add(row);
            }
            return result;
        }

        // ════════════════════════════════════════════════════════════════
        //  Flat Pack Canopy Assembly Process — dropdown
        // ════════════════════════════════════════════════════════════════
        // Replaces legacy BindDDLCanopyPartDesc(). UNION ALL of two sources,
        // both filtered to canopy parts (PartCode LIKE '4%'). The user-selected
        // LineWisePC controls the KVA band shown — each line processes only
        // its own tier (see interface comment for the mapping).
        public async Task<List<FlatPackCanopyOptionDto>> GetFlatPackCanopyOptionsAsync(string pcCode)
        {
            // KVA band lookup driven by LineWisePC. Lower bound is split so
            // callers can pick inclusive (>=) or exclusive (>) — 01.125 needs
            // "above 58.5" and must not overlap with 01.124's ceiling of 58.5.
            //   01.124 → 0     <= KVA <= 58.5   (both inclusive)
            //   01.125 → 58.5  <  KVA <= 250    (lower EXCLUSIVE)
            //   01.126 → 250   <= KVA           (no upper cap)
            // For any other pcCode all three bounds stay null → no KVA filter.
            decimal? kvaMinInc = null, kvaMinExc = null, kvaMax = null;
            switch ((pcCode ?? string.Empty).Trim())
            {
                case "01.124": kvaMinInc = 0m;    kvaMax = 58.5m; break;
                case "01.125": kvaMinExc = 58.5m; kvaMax = 250m;  break;
                case "01.126": kvaMinInc = 250m;                  break;
            }

            const string sql = @"
SELECT DISTINCT Pt.Partcode,
       Partdesc + '-->' + Pt.Partcode AS Partdesc,
       KVA, Model, Phase, Type
FROM PartToCDetailsSupplier Pt WITH (NOLOCK)
INNER JOIN Part P WITH (NOLOCK) ON Pt.partcode = P.Partcode
WHERE Pt.CompanyCode IN ('01','03')
  AND Pt.TMatType = 'REQ'
  AND Pt.ForPCCode_Act IN ('01.115','01.175')
  AND Pt.SuppCode_Act IN ('23.001','01.115')
  AND Pt.active = '1'
  AND P.Active = '1'
  AND P.Discard = '1'
  AND Pt.POper > 0
  AND Pt.partcode LIKE '4%'
  AND (@KvaMinInc IS NULL OR P.KVA >= @KvaMinInc)
  AND (@KvaMinExc IS NULL OR P.KVA >  @KvaMinExc)
  AND (@KvaMax    IS NULL OR P.KVA <= @KvaMax)
UNION ALL
SELECT Pt.Partcode,
       Partdesc + '-->' + Pt.Partcode AS Partdesc,
       Pt.KVA, Pt.Model, Pt.Phase, Type
FROM MTODts Pt WITH (NOLOCK)
INNER JOIN Part P WITH (NOLOCK) ON Pt.partcode = P.Partcode
WHERE Pt.active = '1'
  AND Pt.Qty > 0
  AND P.Active = '1'
  AND P.Discard = '1'
  AND Pt.partcode LIKE '4%'
  AND Pt.DtValidity >= GETDATE()
  AND (@KvaMinInc IS NULL OR Pt.KVA >= @KvaMinInc)
  AND (@KvaMinExc IS NULL OR Pt.KVA >  @KvaMinExc)
  AND (@KvaMax    IS NULL OR Pt.KVA <= @KvaMax);";

            var results = new List<FlatPackCanopyOptionDto>();
            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var command = new SqlCommand(sql, connection);
            // Precision/Scale must match the SQL side (DECIMAL(10,2)) — otherwise
            // SqlDbType.Decimal defaults to (18,0) and silently truncates 58.5 → 58.
            command.Parameters.Add(new SqlParameter("@KvaMinInc", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = (object?)kvaMinInc ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@KvaMinExc", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = (object?)kvaMinExc ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@KvaMax",    SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = (object?)kvaMax    ?? DBNull.Value });
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new FlatPackCanopyOptionDto
                {
                    PartCode = SafeStr(reader, "Partcode"),
                    PartDesc = SafeStr(reader, "Partdesc"),
                    Kva      = SafeStr(reader, "KVA"),
                    Model    = SafeStr(reader, "Model"),
                    Phase    = SafeStr(reader, "Phase"),
                    Type     = SafeStr(reader, "Type"),
                });
            }
            return results;
        }

        // ════════════════════════════════════════════════════════════════
        //  Flat Pack Canopy Assembly Process — BindPrimary
        // ════════════════════════════════════════════════════════════════
        // Given a canopy + process type, derive the "Part Desc" textbox value.
        // CPY      → grabs the kit-of-kit "without BF & FT" variant
        // CPY(BF_FT) → grabs the BF+FT variant directly
        public async Task<FlatPackBindPrimaryResponse> GetFlatPackBindPrimaryAsync(
            string canopyPartCode,
            string processType,
            string? heading)
        {
            var resp = new FlatPackBindPrimaryResponse
            {
                Heading = (heading ?? string.Empty).Trim(),
            };
            var canopy = (canopyPartCode ?? string.Empty).Trim();
            var type   = (processType   ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(canopy) || string.IsNullOrEmpty(type)) return resp;

            string sql;
            if (type.Equals("CPY", StringComparison.OrdinalIgnoreCase))
            {
                // Kit-of-kit variant: cp1's BOM is the latest BOM whose
                // KitCode = canopy and its Partcode position 11 = '4'.
                sql = @"
SELECT TOP 1 PartDesc + '-->' + cp1.PartCode AS PartDesc, cp1.PartCode
FROM BOMdetails cp WITH (NOLOCK)
INNER JOIN BOMdetails cp1 WITH (NOLOCK) ON cp.partcode = cp1.KITCode
INNER JOIN Part p WITH (NOLOCK) ON cp1.PartCode = p.PartCode
WHERE cp1.BOMCode IN (
        SELECT MAX(b.BOMCode)
        FROM BOM b WITH (NOLOCK)
        INNER JOIN BOMdetails bd WITH (NOLOCK) ON b.BOMCode = bd.BOMCode
        WHERE bd.KitCode = @Canopy AND b.Active='1' AND b.Auth='1' AND b.CompanyCode='01')
  AND cp.KitCode = @Canopy
  AND cp.Partcode  LIKE '004%'
  AND cp1.Partcode LIKE '004%'
  AND SUBSTRING(cp1.Partcode, 11, 1) = '4'
GROUP BY cp1.PartCode, PartDesc;";
            }
            else
            {
                // BF+FT variant: direct child of the canopy KitCode.
                sql = @"
SELECT TOP 1 PartDesc + '-->' + cp.PartCode AS PartDesc, cp.PartCode
FROM BOMdetails cp WITH (NOLOCK)
INNER JOIN Part p WITH (NOLOCK) ON cp.PartCode = p.PartCode
WHERE cp.BOMCode IN (
        SELECT MAX(b.BOMCode)
        FROM BOM b WITH (NOLOCK)
        INNER JOIN BOMdetails bd WITH (NOLOCK) ON b.BOMCode = bd.BOMCode
        WHERE bd.KitCode = @Canopy AND b.Active='1' AND b.Auth='1' AND b.CompanyCode='01')
  AND cp.KitCode = @Canopy
  AND cp.Partcode LIKE '004%';";
            }

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Canopy", SqlDbType.VarChar, 50).Value = canopy;
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                resp.PartDesc = SafeStr(reader, "PartDesc");
                resp.PartCode = SafeStr(reader, "PartCode");
            }
            return resp;
        }

        // ════════════════════════════════════════════════════════════════
        //  Flat Pack Canopy Assembly Process — Search (BindDetails)
        // ════════════════════════════════════════════════════════════════
        public async Task<FlatPackProcessDetailsResponse> GetFlatPackProcessDetailsAsync(
            FlatPackProcessDetailsRequest req)
        {
            var resp = new FlatPackProcessDetailsResponse();
            var pc       = (req.PCCode ?? string.Empty).Trim();
            var partCode = (req.PartCode ?? string.Empty).Trim();
            var type     = (req.ProcessType ?? string.Empty).Trim();
            var qty      = req.ProcessQty;

            if (string.IsNullOrEmpty(pc) || string.IsNullOrEmpty(partCode) || string.IsNullOrEmpty(type))
                return resp;

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            await connection.OpenAsync();

            // 1) BomCode = MAX BOMCode where the chosen PartCode is the kit head
            resp.BomCode = await ExecuteScalarStringAsync(connection, @"
SELECT MAX(b.BOMCode)
FROM BOM b WITH (NOLOCK)
INNER JOIN BOMdetails bd WITH (NOLOCK) ON b.BOMCode = bd.BOMCode
WHERE bd.KitCode = @PartCode AND b.Active='1' AND b.Auth='1' AND b.CompanyCode='01';",
                ("@PartCode", partCode));

            // 2) Rate (from ProfitCenterPLDetails @ standard PC)
            resp.OverallRate = await ExecuteScalarDoubleAsync(connection, @"
SELECT Rate FROM ProfitCenterPLDetails WITH (NOLOCK)
WHERE PartCode = @PartCode AND ProfitCenterCode = @StdPC;",
                ("@PartCode", partCode), ("@StdPC", StandardRatePC));

            resp.OverallAmount = Math.Round(resp.OverallRate * qty, 2);

            // 3) Wt / SqFt
            (resp.Wt, resp.SqFt) = await GetWtSqftAsync(connection, partCode);

            // 4) CR/HR weights from ProfitCenterPLDetails
            resp.CRWt = await ExecuteScalarDoubleAsync(connection, @"
SELECT CRWt FROM ProfitCenterPLDetails WITH (NOLOCK)
WHERE PartCode = @PartCode AND ProfitCenterCode = @StdPC;",
                ("@PartCode", partCode), ("@StdPC", StandardRatePC));

            resp.HRWt = await ExecuteScalarDoubleAsync(connection, @"
SELECT HRWt FROM ProfitCenterPLDetails WITH (NOLOCK)
WHERE PartCode = @PartCode AND ProfitCenterCode = @StdPC;",
                ("@PartCode", partCode), ("@StdPC", StandardRatePC));

            // 5) CR/HR rates from BOMDetails (Thickness threshold 1.5)
            resp.CRRate = await ExecuteScalarDoubleAsync(connection, @"
SELECT TOP 1 SteelRate FROM BomDetails WITH (NOLOCK)
WHERE BOMCode = @BomCode AND Thickness <= 1.5 AND SteelRate > 0;",
                ("@BomCode", resp.BomCode));

            resp.HRRate = await ExecuteScalarDoubleAsync(connection, @"
SELECT TOP 1 SteelRate FROM BomDetails WITH (NOLOCK)
WHERE BOMCode = @BomCode AND Thickness > 1.5 AND SteelRate > 0;",
                ("@BomCode", resp.BomCode));

            // 6) Grid — only loads for the three production PCs (legacy gate)
            if (!AllowedProductionPCs.Contains(pc))
                return resp;

            var spName = type.Equals("CPY(BF_FT)", StringComparison.OrdinalIgnoreCase)
                ? "sp_GetFlatPackProcessDetailsCPY"
                : "sp_GetFlatPackProcessDetails";

            using (var cmd = new SqlCommand(spName, connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ProcessQty", SqlDbType.Float).Value      = qty;
                cmd.Parameters.Add("@BOMCode",    SqlDbType.NVarChar, 50).Value = resp.BomCode ?? string.Empty;
                cmd.Parameters.Add("@PartCode",   SqlDbType.NVarChar, 50).Value = partCode;
                cmd.Parameters.Add("@PCCode",     SqlDbType.NVarChar, 50).Value = pc;

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    resp.PartDetails.Add(new FlatPackPartDetailRow
                    {
                        PartCode        = SafeStr(reader, "Partcode"),
                        Qty             = SafeDouble(reader, "Qty"),
                        UName           = SafeStr(reader, "UName"),
                        Rate            = SafeDouble(reader, "Rate"),
                        TotalQty        = SafeDouble(reader, "TotalQty"),
                        Stk             = SafeDouble(reader, "Stk"),
                        QtyAfterProcess = SafeDouble(reader, "QtyAfterProcess"),
                        Amount          = SafeDouble(reader, "Amount"),
                    });
                }
            }
            return resp;
        }

        // ════════════════════════════════════════════════════════════════
        //  Flat Pack Canopy Assembly Process — Save (setSave "S")
        // ════════════════════════════════════════════════════════════════
        public async Task<FlatPackSubmitResponse> SubmitFlatPackProcessAsync(FlatPackSubmitRequest req)
        {
            ValidateSubmit(req);

            var pc          = req.PCCode.Trim();                 // LineWisePC → PCCode_Act
            var parentDgPc  = (req.ParentDgPC ?? string.Empty).Trim();  // ParentDgPC → ProfitCenterCode
            // Fallback: if the client didn't send ParentDgPC (legacy caller), keep behaviour identical
            // to before by using pc for the ProfitCenterCode. New flat-pack lines always send it.
            if (string.IsNullOrEmpty(parentDgPc)) parentDgPc = pc;
            // CompanyCode is derived from the first two chars of LineWisePC — that's the
            // authoritative source (01.124 → '01', 03.xxx → '03'). Prevents client-side
            // drift between PCCode and CompanyCode. Same convention as reqCompCode below.
            var company  = pc.Length >= 2 ? pc.Substring(0, 2) : req.CompanyCode.Trim();
            var emp      = req.EmpCode.Trim();
            var type     = req.ProcessType.Trim();
            var canopy   = req.CanopyPartCode.Trim();
            var part     = req.PartCode.Trim();
            var bom      = req.BomCode.Trim();
            var heading  = req.Heading.Trim();
            var qty      = req.ProcessQty;

            if (!AllowedProductionPCs.Contains(pc))
                throw new InvalidOperationException($"Profit Center {pc} is not enabled for Flat Pack save.");

            // CPY serials are needed BEFORE we open the transaction so we can
            // fail-fast if there aren't enough.
            List<CpySerialRow> serials = new();
            if (type.Equals("CPY", StringComparison.OrdinalIgnoreCase))
            {
                serials = await GetCPYSerialsAsync(canopy, (int)Math.Ceiling(qty));
                if (serials.Count < qty)
                    throw new InvalidOperationException("Serial No Qty is less than Process Qty");
            }

            // BOM authorization gate (legacy: Auth='False' alert).
            var bomAuth = await GetScalarAsync<string>(
                "SELECT TOP 1 CAST(Auth AS NVARCHAR(10)) FROM BOM WITH (NOLOCK) WHERE BOMCode = @BomCode AND Active = '1';",
                ("@BomCode", bom));
            if (string.Equals(bomAuth, "False", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(bomAuth, "0",     StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("BOM Authorization Pending !");

            // Per-row stock validation parity (legacy alerts).
            foreach (var row in req.PartDetails)
            {
                if (row.Stk <= 0)
                    throw new InvalidOperationException($"Insufficient Stock for Part : {row.PartCode} !");
                if (row.Stk < row.TotalQty)
                    throw new InvalidOperationException($"Insufficient Stock for Part : {row.PartCode}");
                if (row.QtyAfterProcess < 0)
                    throw new InvalidOperationException($"Insufficient Stock for Part : {row.PartCode}");
            }

            var conn = _context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var sqlTx = (SqlTransaction)tx.GetDbTransaction();

                // 1) Generate new PFBCode via the standard GetMaxCode flow.
                var pfbCode = await GetMaxNoAsync(
                    prefix: "PSH",
                    compCode: company,
                    tblName: "ProcessFeedBack",
                    tx: sqlTx);

                // MaxSrNo = "<CompCode><6-digit sequence>" — everything AFTER the
                // last '/' in the generated PFBCode (e.g. "PSH/26-27/27000001" → "27000001").
                // Using LastIndexOf is robust against prefix / yearEnd length changes.
                var maxSrNo = ExtractSequencePart(pfbCode);

                // 2) InsertProcessFeedBack (master)
                using (var cmd = new SqlCommand("InsertProcessFeedBack", (SqlConnection)conn, sqlTx))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@GroupPFBCode",     pfbCode);
                    cmd.Parameters.AddWithValue("@PFBCode",          pfbCode);
                    cmd.Parameters.AddWithValue("@MaxSrNo",          maxSrNo);
                    cmd.Parameters.AddWithValue("@Dt",               DateTime.Now);
                    cmd.Parameters.AddWithValue("@Yr",               await GetYearEndAsync());
                    cmd.Parameters.AddWithValue("@ProfitCenterCode", parentDgPc);   // ParentDgPC ('01.093' for flat-pack lines)
                    cmd.Parameters.AddWithValue("@MachineCode",      "0");
                    cmd.Parameters.AddWithValue("@SerialNo",         "0");
                    cmd.Parameters.AddWithValue("@CpyStageType",     heading);
                    cmd.Parameters.AddWithValue("@CanopyPlanCode",   "0");
                    cmd.Parameters.AddWithValue("@ProductCode",      canopy);
                    cmd.Parameters.AddWithValue("@CanopyCode",       canopy);
                    cmd.Parameters.AddWithValue("@NestingForCode",   part);
                    cmd.Parameters.AddWithValue("@ProcessQty",       qty);
                    cmd.Parameters.AddWithValue("@PartCode",         part);
                    cmd.Parameters.AddWithValue("@NestingForQty",    qty);
                    cmd.Parameters.AddWithValue("@SuppCode",         "0");
                    cmd.Parameters.AddWithValue("@TurretKitCode",    bom);
                    cmd.Parameters.AddWithValue("@VersionCode",      "0");
                    cmd.Parameters.AddWithValue("@PKitQty",          0.0);
                    cmd.Parameters.AddWithValue("@PLength",          0.0);
                    cmd.Parameters.AddWithValue("@PWidth",           0.0);
                    cmd.Parameters.AddWithValue("@PThickness",       0.0);
                    cmd.Parameters.AddWithValue("@PFBType",          "N");
                    cmd.Parameters.AddWithValue("@PFBRate",          req.OverallRate);
                    cmd.Parameters.AddWithValue("@NstWtPerUt",       req.Wt);
                    cmd.Parameters.AddWithValue("@NstSqftPerUt",     req.SqFt);
                    cmd.Parameters.AddWithValue("@WtPerUt",          req.Wt);
                    cmd.Parameters.AddWithValue("@SqftPerUt",        req.SqFt);
                    cmd.Parameters.AddWithValue("@CompanyCode",      company);
                    cmd.Parameters.AddWithValue("@Remark",           "OK");
                    cmd.Parameters.AddWithValue("@DivertStatus",     false);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3) Patch CR/HR fields + PCCode_Act onto the master row.
                //    PCCode_Act stores the LineWisePC (01.124/125/126) so downstream
                //    line-wise stock filters can distinguish which line produced this
                //    feedback, while ProfitCenterCode stays at ParentDgPC (01.093).
                using (var cmd = new SqlCommand(
                    @"UPDATE ProcessFeedBack
                         SET CRWt = @CRWt, HRWt = @HRWt, CRRate = @CRRate, HRRate = @HRRate,
                             PCCode_Act = @PCCodeAct
                       WHERE PFBCode = @PFBCode;", (SqlConnection)conn, sqlTx))
                {
                    cmd.Parameters.AddWithValue("@CRWt",      req.CRWt);
                    cmd.Parameters.AddWithValue("@HRWt",      req.HRWt);
                    cmd.Parameters.AddWithValue("@CRRate",    req.CRRate);
                    cmd.Parameters.AddWithValue("@HRRate",    req.HRRate);
                    cmd.Parameters.AddWithValue("@PCCodeAct", pc);           // LineWisePC
                    cmd.Parameters.AddWithValue("@PFBCode",   pfbCode);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4) Stock<CompID> receipt for the produced PartCode (+10 min).
                var stockTable = $"Stock{company}";
                using (var cmd = new SqlCommand(
                    $@"INSERT INTO {stockTable} (PartCode, ReceivedCode, ReceivedDate, ReceivedQty)
                       VALUES (@PartCode, @ReceivedCode, @ReceivedDate, @ReceivedQty);",
                    (SqlConnection)conn, sqlTx))
                {
                    cmd.Parameters.AddWithValue("@PartCode",     part);
                    cmd.Parameters.AddWithValue("@ReceivedCode", pfbCode);
                    cmd.Parameters.AddWithValue("@ReceivedDate", DateTime.Now.AddMinutes(10));
                    cmd.Parameters.AddWithValue("@ReceivedQty",  qty);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 5) For each grid row: InsertProcessFeedBackDetails + StockWIP issue.
                int srNo = 0;
                foreach (var row in req.PartDetails)
                {
                    srNo++;
                    var rowPartCode = ExtractPartCode(row.PartCode);

                    using (var cmd = new SqlCommand("InsertProcessFeedBackDetails", (SqlConnection)conn, sqlTx))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PFBCode",       pfbCode);
                        cmd.Parameters.AddWithValue("@SrNo",          srNo);
                        cmd.Parameters.AddWithValue("@PartCode",      rowPartCode);
                        cmd.Parameters.AddWithValue("@KITQty",        row.Qty);
                        cmd.Parameters.AddWithValue("@TotQty",        row.TotalQty);
                        cmd.Parameters.AddWithValue("@StockQty",      row.Stk);
                        cmd.Parameters.AddWithValue("@PFBRate",       row.Rate);
                        cmd.Parameters.AddWithValue("@SaleRate",      0.0);
                        cmd.Parameters.AddWithValue("@PLength",       0.0);
                        cmd.Parameters.AddWithValue("@PWidth",        0.0);
                        cmd.Parameters.AddWithValue("@PThickness",    0.0);
                        cmd.Parameters.AddWithValue("@PLossWt",       0.0);
                        cmd.Parameters.AddWithValue("@PHeight",       0.0);
                        cmd.Parameters.AddWithValue("@PLength1",      0.0);
                        cmd.Parameters.AddWithValue("@PLength2",      0.0);
                        cmd.Parameters.AddWithValue("@PWidth1",       0.0);
                        cmd.Parameters.AddWithValue("@PWidth2",       0.0);
                        cmd.Parameters.AddWithValue("@PLossSqft",     0.0);
                        cmd.Parameters.AddWithValue("@WtPerUt",       0.0);
                        cmd.Parameters.AddWithValue("@SqftPerUt",     0.0);
                        cmd.Parameters.AddWithValue("@PCatagoryCode", "0");
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // StockWIP — Option A split (matches ProcessFeedBack):
                    //   From/To ProfitCenterCode      = ParentDgPC (01.093) — legacy semantic
                    //   From/To ProfitCenterCode_Act  = LineWisePC (01.124/125/126) — line-wise stock ownership
                    using (var cmd = new SqlCommand(
                        @"INSERT INTO StockWIP
                            (FromProfitCenterCode, FromProfitCenterCode_Act,
                             ToProfitCenterCode,   ToProfitCenterCode_Act,
                             PartCode, IssueCode, IssueDate, IssueQty, StockType)
                          VALUES
                            (@FromPC, @FromPCAct,
                             @ToPC,   @ToPCAct,
                             @PartCode, @IssueCode, @IssueDate, @IssueQty, @StockType);",
                        (SqlConnection)conn, sqlTx))
                    {
                        cmd.Parameters.AddWithValue("@FromPC",    parentDgPc);
                        cmd.Parameters.AddWithValue("@FromPCAct", pc);
                        cmd.Parameters.AddWithValue("@ToPC",      parentDgPc);
                        cmd.Parameters.AddWithValue("@ToPCAct",   pc);
                        cmd.Parameters.AddWithValue("@PartCode",  rowPartCode);
                        cmd.Parameters.AddWithValue("@IssueCode", pfbCode);
                        cmd.Parameters.AddWithValue("@IssueDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@IssueQty",  row.TotalQty);
                        cmd.Parameters.AddWithValue("@StockType", 0);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // 6) Per-unit serial inserts + CanopyPlanSerialNo flip (CPY only).
                if (type.Equals("CPY", StringComparison.OrdinalIgnoreCase))
                {
                    var nUnits = (int)Math.Ceiling(qty);
                    for (int m = 0; m < nUnits; m++)
                    {
                        var sn = serials[m];
                        using (var cmd = new SqlCommand(
                            @"INSERT INTO ProcessFeedbackDetailsSub (PFBCode, SrNo, PartCode, SerialNo, PFBBOTSerialNo, BFMSrNo, FLKSrNo, Status, QPCStatus, RWStatus)
                              VALUES (@PFBCode, @SrNo, @PartCode, @SerialNo, @PFBBOTSerialNo, @BFMSrNo, @FLKSrNo, 'P', 'OK', 'OK');",
                            (SqlConnection)conn, sqlTx))
                        {
                            cmd.Parameters.AddWithValue("@PFBCode",        pfbCode);
                            cmd.Parameters.AddWithValue("@SrNo",           m + 1);
                            cmd.Parameters.AddWithValue("@PartCode",       canopy);
                            cmd.Parameters.AddWithValue("@SerialNo",       sn.SerialNo);
                            cmd.Parameters.AddWithValue("@PFBBOTSerialNo", sn.SerialNo);
                            cmd.Parameters.AddWithValue("@BFMSrNo",        sn.BFMSrNo);
                            cmd.Parameters.AddWithValue("@FLKSrNo",        sn.FLKSrNo);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        using (var cmd = new SqlCommand(
                            @"UPDATE CanopyPlanSerialNo SET CPFPSerialStatus = 'D'
                              WHERE SerialNo = @SerialNo AND Partcode = @PartCode;",
                            (SqlConnection)conn, sqlTx))
                        {
                            cmd.Parameters.AddWithValue("@SerialNo", sn.SerialNo);
                            cmd.Parameters.AddWithValue("@PartCode", canopy);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 7) Kanban auto-REQ block — fires for every flat-pack line whose
                //    ParentDgPC == KanbanPC (01.093). Covers legacy 01.093 as well
                //    as the new lines 01.124 / 01.125 / 01.126.
                if (parentDgPc.Equals(KanbanPC, StringComparison.OrdinalIgnoreCase))
                {
                    // Shortage query is driven by LineWisePC — each line calculates
                    // its own shortage list from its own line-wise stock (StockWIP
                    // FromProfitCenterCode_Act = @PC filter).
                    var kanbanRows = await GetInternalTOCRowsAsync((SqlConnection)conn, sqlTx, pc);
                    if (kanbanRows.Count > 0)
                    {
                        var reqCompCode = pc.Substring(0, 2);
                        var reqCode = await GetMaxNoAsync(
                            prefix: "REQ",
                            compCode: reqCompCode,
                            tblName: "MaterialRequisitionWithOutPlan",
                            tx: sqlTx);
                        var maxSrNoReq = ExtractSequencePart(reqCode);

                        // Header split — same idiom as ProcessFeedBack / StockWIP:
                        //   ProfitCenterCode      = ParentDgPC (01.093) — legacy semantic
                        //   ProfitCenterCode_Act  = LineWisePC (01.124/125/126) — line classification
                        //   ToProfitCenterCode(_Act) = '23.001' literal (parts store)
                        using (var cmd = new SqlCommand(
                            @"INSERT INTO MaterialRequisitionWithOutPlan
                              (REQCode, MaxSrNo, Dt, Yr,
                               ProfitCenterCode,   ProfitCenterCode_Act,
                               ToProfitCenterCode, ToProfitCenterCode_Act,
                               ClassCode, CompanyCode, ActNo, REQStatus, ReqType, Remark,
                               Discard, Active, Auth, SourceCode)
                              VALUES
                              (@REQCode, @MaxSrNo, @Dt, @Yr,
                               @PC,     @PCAct,
                               '23.001','23.001',
                               @ClassCode, @CompCode, @ActNo, 'P', 'WIP', @Remark,
                               '1', '1', '1', 'KanBan');",
                            (SqlConnection)conn, sqlTx))
                        {
                            cmd.Parameters.AddWithValue("@REQCode",   reqCode);
                            cmd.Parameters.AddWithValue("@MaxSrNo",   maxSrNoReq);
                            cmd.Parameters.AddWithValue("@Dt",        DateTime.Now);
                            cmd.Parameters.AddWithValue("@Yr",        await GetYearEndAsync());
                            cmd.Parameters.AddWithValue("@PC",        parentDgPc);   // ParentDgPC (01.093)
                            cmd.Parameters.AddWithValue("@PCAct",     pc);           // LineWisePC (01.124/125/126)
                            cmd.Parameters.AddWithValue("@ClassCode", canopy);
                            cmd.Parameters.AddWithValue("@CompCode",  reqCompCode);
                            cmd.Parameters.AddWithValue("@ActNo",     qty);
                            cmd.Parameters.AddWithValue("@Remark",    $"Auto Req For Plan No: {canopy} and Prc No: {pfbCode}");
                            await cmd.ExecuteNonQueryAsync();
                        }

                        int reqRowNo = 0;
                        foreach (var k in kanbanRows)
                        {
                            reqRowNo++;
                            using var cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails",
                                                            (SqlConnection)conn, sqlTx);
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@REQCode",   reqCode);
                            cmd.Parameters.AddWithValue("@SrNo",      reqRowNo);
                            cmd.Parameters.AddWithValue("@PartCode",  k.Partcode ?? string.Empty);
                            cmd.Parameters.AddWithValue("@Qty",       (double)k.RaiseReqQty);
                            cmd.Parameters.AddWithValue("@REQStatus", "P");
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Activity log for the Kanban REQ.
                        await InsertLoginTxnAsync((SqlConnection)conn, sqlTx,
                            emp, "S", "MaterialRequisitionWithoutPlan", reqCode, reqCompCode);
                    }
                }

                // 8) Activity log for the main FlatPack save.
                await InsertLoginTxnAsync((SqlConnection)conn, sqlTx,
                    emp, "S", "FlatPackCanopyProcess", pfbCode, company);

                await tx.CommitAsync();
                return new FlatPackSubmitResponse
                {
                    Message = $"Process Saved Successfully & Your Process Code : {pfbCode}",
                    PFBCode = pfbCode,
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Helpers
        // ════════════════════════════════════════════════════════════════

        // Mirrors EngineDGAssemblyService.GetMaxNo — same query shape, same
        // 6-digit zero-pad, same GetMaxCode update. Kept inline per the
        // per-service convention used elsewhere in this codebase.
        private async Task<string> GetMaxNoAsync(string prefix, string compCode, string tblName, SqlTransaction tx)
        {
            var yearEnd = await GetYearEndAsync();
            int intmax = 0;

            const string sql = @"SELECT ISNULL(MaxValue, 0) AS MXNO FROM GetMaxCode
                                 WHERE TblName = @TableName AND CompCode = @CompCode
                                   AND Prefix  = @Prefix    AND Yr       = @YearEnd;";

            using (var cmd = new SqlCommand(sql, (SqlConnection)_context.Database.GetDbConnection(), tx))
            {
                cmd.Parameters.AddWithValue("@TableName", tblName);
                cmd.Parameters.AddWithValue("@CompCode",  compCode);
                cmd.Parameters.AddWithValue("@Prefix",    prefix);
                cmd.Parameters.AddWithValue("@YearEnd",   yearEnd ?? string.Empty);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    intmax = Convert.ToInt32(result);
            }

            var strmax = (intmax + 1).ToString("D6");
            var newCode = $"{prefix}/{yearEnd}/{compCode}{strmax}";

            // Persist the bump within the same transaction.
            const string upd = @"UPDATE GetMaxCode SET MaxValue = @MaxValue
                                 WHERE TblName = @TableName AND CompCode = @CompCode
                                   AND Prefix  = @Prefix    AND Yr       = @YearEnd;";
            using (var cmd = new SqlCommand(upd, (SqlConnection)_context.Database.GetDbConnection(), tx))
            {
                cmd.Parameters.AddWithValue("@MaxValue",  Convert.ToInt32(strmax));
                cmd.Parameters.AddWithValue("@TableName", tblName);
                cmd.Parameters.AddWithValue("@CompCode",  compCode);
                cmd.Parameters.AddWithValue("@Prefix",    prefix);
                cmd.Parameters.AddWithValue("@YearEnd",   yearEnd ?? string.Empty);
                await cmd.ExecuteNonQueryAsync();
            }

            return newCode;
        }

        private async Task<string?> GetYearEndAsync()
        {
            return await _context.YearEnds
                .Select(y => (y.StartDate.Year % 100).ToString("00") + "-" + (y.EndDate.Year % 100).ToString("00"))
                .FirstOrDefaultAsync();
        }

        private async Task<List<CpySerialRow>> GetCPYSerialsAsync(string productCode, int qty)
        {
            var rows = new List<CpySerialRow>();
            if (qty <= 0 || string.IsNullOrWhiteSpace(productCode)) return rows;

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand("GetCPYSerialNo_4FlatPack", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@ProductCode", SqlDbType.NVarChar, 50).Value = productCode.Trim();
            cmd.Parameters.Add("@PrcQty",      SqlDbType.Int).Value          = qty;

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new CpySerialRow
                {
                    SerialNo = SafeStr(reader, "SerialNo"),
                    BFMSrNo  = SafeStr(reader, "BFMSrNo"),
                    FLKSrNo  = SafeStr(reader, "FLKSrNo"),
                });
            }
            return rows;
        }

        private static async Task<List<InternalTOCResult>> GetInternalTOCRowsAsync(
            SqlConnection conn, SqlTransaction tx, string pcCode)
        {
            var results = new List<InternalTOCResult>();
            using var cmd = new SqlCommand("InternalTOCReq_CPY_Checker_Maker", conn, tx);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@PCCode", SqlDbType.NVarChar, 10).Value = pcCode.Trim();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new InternalTOCResult
                {
                    Partcode    = SafeStr(reader, "Partcode"),
                    MOQ         = SafeDecimal(reader, "MOQ"),
                    Poper       = SafeDecimal(reader, "Poper"),
                    Stk         = SafeDecimal(reader, "stk"),
                    PndReq      = SafeDecimal(reader, "PndReq"),
                    Req         = SafeDecimal(reader, "Req"),
                    Flag        = (int)SafeDecimal(reader, "Flag"),
                    RaiseReqQty = SafeDecimal(reader, "RaiseReqQty"),
                    FromPC      = SafeStr(reader, "FromPC"),
                    ToPCCode    = SafeStr(reader, "ToPCCode"),
                });
            }
            return results;
        }

        private static async Task InsertLoginTxnAsync(SqlConnection conn, SqlTransaction tx,
            string empCode, string transactionType, string transactionFrom, string transactionNo, string companyCode)
        {
            using var cmd = new SqlCommand("insertLoginTransactionDetails", conn, tx);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@TransactionDtTime", DateTime.Now);
            cmd.Parameters.AddWithValue("@EmpID",             empCode);
            cmd.Parameters.AddWithValue("@TransactionType",   transactionType);
            cmd.Parameters.AddWithValue("@TransactionFrom",   transactionFrom);
            cmd.Parameters.AddWithValue("@TransactionNo",     transactionNo);
            cmd.Parameters.AddWithValue("@CompanyCode",       companyCode);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<(double wt, double sqft)> GetWtSqftAsync(SqlConnection connection, string partCode)
        {
            using var cmd = new SqlCommand(
                @"SELECT TOP 1 ISNULL(PWt, 0) AS PWt, ISNULL(PSqFt, 0) AS PSqFt
                  FROM ProfitCenterPLDetails WITH (NOLOCK)
                  WHERE PartCode = @PartCode AND ProfitCenterCode = @StdPC;", connection);
            cmd.Parameters.Add("@PartCode", SqlDbType.NVarChar, 50).Value = partCode;
            cmd.Parameters.Add("@StdPC",    SqlDbType.NVarChar, 50).Value = StandardRatePC;
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return (SafeDouble(reader, "PWt"), SafeDouble(reader, "PSqFt"));
            return (0, 0);
        }

        private async Task<string> ExecuteScalarStringAsync(SqlConnection connection, string sql,
            params (string Name, object Value)[] parameters)
        {
            using var cmd = new SqlCommand(sql, connection);
            foreach (var (n, v) in parameters) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? string.Empty : Convert.ToString(result)?.Trim() ?? string.Empty;
        }

        private async Task<double> ExecuteScalarDoubleAsync(SqlConnection connection, string sql,
            params (string Name, object Value)[] parameters)
        {
            using var cmd = new SqlCommand(sql, connection);
            foreach (var (n, v) in parameters) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value) return 0;
            return double.TryParse(Convert.ToString(result), out var d) ? d : 0;
        }

        // Stand-alone scalar that opens its own connection (used pre-transaction).
        private async Task<T?> GetScalarAsync<T>(string sql, params (string Name, object Value)[] parameters) where T : class
        {
            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand(sql, connection);
            foreach (var (n, v) in parameters) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
            await connection.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value) return null;
            return (T)Convert.ChangeType(result, typeof(T));
        }

        private static void ValidateSubmit(FlatPackSubmitRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.PCCode))         throw new ArgumentException("PCCode is required.");
            if (string.IsNullOrWhiteSpace(req.CompanyCode))    throw new ArgumentException("CompanyCode is required.");
            if (string.IsNullOrWhiteSpace(req.EmpCode))        throw new ArgumentException("EmpCode is required.");
            if (string.IsNullOrWhiteSpace(req.ProcessType))    throw new ArgumentException("ProcessType is required.");
            if (string.IsNullOrWhiteSpace(req.CanopyPartCode)) throw new ArgumentException("CanopyPartCode is required.");
            if (string.IsNullOrWhiteSpace(req.PartCode))       throw new ArgumentException("PartCode is required.");
            if (string.IsNullOrWhiteSpace(req.BomCode))        throw new ArgumentException("BomCode is required.");
            if (req.ProcessQty <= 0)                           throw new ArgumentException("ProcessQty must be > 0.");
            if (req.PartDetails == null || req.PartDetails.Count == 0)
                throw new ArgumentException("PartDetails must contain at least one row.");
        }

        // "PartDesc-->PartCode" → "PartCode". Identity if no separator.
        private static string ExtractPartCode(string concatenated)
        {
            if (string.IsNullOrWhiteSpace(concatenated)) return string.Empty;
            var idx = concatenated.IndexOf("-->", StringComparison.Ordinal);
            return idx < 0 ? concatenated.Trim() : concatenated.Substring(idx + 3).Trim();
        }

        // "PSH/26-27/27000001" → "27000001". Returns the portion after the
        // last '/' — this matches the legacy MaxSrNo shape (CompCode + sequence).
        private static string ExtractSequencePart(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;
            var slash = code.LastIndexOf('/');
            return slash < 0 ? code : code.Substring(slash + 1);
        }

        private static string SafeStr(IDataRecord r, string col)
        {
            try
            {
                var i = r.GetOrdinal(col);
                return r.IsDBNull(i) ? string.Empty : Convert.ToString(r.GetValue(i))?.Trim() ?? string.Empty;
            }
            catch (IndexOutOfRangeException) { return string.Empty; }
        }

        private static double SafeDouble(IDataRecord r, string col)
        {
            try
            {
                var i = r.GetOrdinal(col);
                if (r.IsDBNull(i)) return 0;
                return Convert.ToDouble(r.GetValue(i));
            }
            catch (IndexOutOfRangeException) { return 0; }
        }

        private static decimal SafeDecimal(IDataRecord r, string col)
        {
            try
            {
                var i = r.GetOrdinal(col);
                if (r.IsDBNull(i)) return 0;
                return Convert.ToDecimal(r.GetValue(i));
            }
            catch (IndexOutOfRangeException) { return 0; }
        }

        // ════════════════════════════════════════════════════════════════
        //  Canopy Assembly Plan (manual planning)
        // ════════════════════════════════════════════════════════════════

        // Same set of "production-line" PCs the legacy CanopyAssemblyPlan
        // page hard-coded for the dropdown (01.005, 03.038, 28.017).
        // We accept any PC at the controller level but only the dropdown
        // narrows to these by default.
        private static readonly HashSet<string> CanopyDropdownPCs = new(StringComparer.OrdinalIgnoreCase)
        {
            "01.005", "03.038", "28.017",
        };

        // Legacy GetPartCode WebMethod hard-coded the stock query to
        // ToPCCode='01.005'. We keep this as the default fallback but allow
        // the caller (UI) to pass any PC.
        private const string DefaultPlanStockPC = "01.005";

        // ── Dropdown lazy-load ──────────────────────────────────────────
        public async Task<List<CanopyPlanPartOptionDto>> GetCanopyPlanPartOptionsAsync(
            string? searchText, string pcCode)
        {
            var results = new List<CanopyPlanPartOptionDto>();
            var search = (searchText ?? string.Empty).Trim();
            var pc = (pcCode ?? string.Empty).Trim();

            // The legacy threshold was >10 chars typed before AJAX fired —
            // server side we relax to >=2 to enable a proper Angular debounce.
            if (search.Length < 2) return results;

            // Mirrors the legacy ddlPartDesc_LoadingItems query; parameterised.
            const string sql = @"
SELECT TOP 25
       MAX(b.BOMCode)                  AS BOMCode,
       p.PartDesc + '-->' + BD.KitCode AS PartDesc,
       BD.KitCode                      AS PartCode,
       u.UName                         AS UName
FROM BOM           b   WITH (NOLOCK)
INNER JOIN BOMDetails BD WITH (NOLOCK) ON b.BOMCode = BD.BOMCode
INNER JOIN Part       p  WITH (NOLOCK) ON BD.KitCode = p.PartCode
INNER JOIN UOM        u  WITH (NOLOCK) ON p.UOMCode = u.Uid
WHERE b.Active = '1'
  AND b.Discard = '1'
  AND BD.MOB = 'M'
  AND p.Active = '1'
  AND p.Discard = '1'
  AND p.MOB = 'M'
  AND b.Auth = '1'
  AND BD.KitCode LIKE '40%'
  AND (p.PartDesc LIKE @Search OR BD.KitCode LIKE @Search)
  AND b.CompanyCode IN ('01', '03')
GROUP BY BD.KitCode, p.PartDesc, u.UName
ORDER BY p.PartDesc;";

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 200).Value = "%" + search + "%";

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new CanopyPlanPartOptionDto
                {
                    BomCode  = SafeStr(reader, "BOMCode"),
                    PartDesc = SafeStr(reader, "PartDesc"),
                    PartCode = SafeStr(reader, "PartCode"),
                    UName    = SafeStr(reader, "UName"),
                });
            }
            return results;
        }

        // ── Per-part context lookup (BomCode + stock + pending) ─────────
        public async Task<CanopyPlanPartContextDto> GetCanopyPlanPartContextAsync(
            string partCode, string pcCode)
        {
            var ctx = new CanopyPlanPartContextDto();
            var pc  = string.IsNullOrWhiteSpace(pcCode) ? DefaultPlanStockPC : pcCode.Trim();
            var pcd = (partCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(pcd)) return ctx;

            ctx.PartCode = pcd;

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            await connection.OpenAsync();

            // 1) BomCode — latest BOM where this Part is a KitCode
            ctx.BomCode = await ExecuteScalarStringAsync(connection, @"
SELECT TOP 1 b.BomCode
FROM   BOM        b  WITH (NOLOCK)
INNER JOIN BOMDetails bd WITH (NOLOCK) ON b.BomCode = bd.BomCode
WHERE  bd.KitCode = @PartCode
   AND b.Active = '1'
   AND b.Discard = '1'
   AND b.CompanyCode = '01'
ORDER BY b.BomCode DESC;",
                ("@PartCode", pcd));

            // 2) Stock qty — ProductWip net (Received - Issued) at the given PC.
            //    Legacy hard-coded '01.005' — we accept any PC.
            ctx.StkQty = await ExecuteScalarDoubleAsync(connection, @"
SELECT (ISNULL(SUM(ReceivedQty), 0) - ISNULL(SUM(IssueQty), 0)) AS StkQty
FROM (
    SELECT 0 AS IssueQty, SUM(ReceivedQty) AS ReceivedQty
    FROM ProductWip WITH (NOLOCK)
    WHERE ToPCCode = @PCCode AND ReceivedQty > 0 AND ProductCode = @PartCode
    UNION ALL
    SELECT SUM(IssueQty) AS IssueQty, 0 AS ReceivedQty
    FROM ProductWip WITH (NOLOCK)
    WHERE FromPCCode = @PCCode AND IssueQty > 0 AND ProductCode = @PartCode
) AS T;",
                ("@PartCode", pcd), ("@PCCode", pc));

            // 3) Pending qty — CanopyPlanDetails net (Qty - CpyWopQty) for this part
            ctx.PendQty = await ExecuteScalarDoubleAsync(connection, @"
SELECT (ISNULL(SUM(Qty), 0) - ISNULL(SUM(CpyWopQty), 0)) AS PendQty
FROM (
    SELECT 0 AS CpyWopQty, SUM(Qty) AS Qty
    FROM CanopyPlanDetails WITH (NOLOCK)
    WHERE Partcode = @PartCode
    UNION ALL
    SELECT SUM(CpyWopQty) AS CpyWopQty, 0 AS Qty
    FROM CanopyPlanDetails WITH (NOLOCK)
    WHERE Partcode = @PartCode
) AS T;",
                ("@PartCode", pcd));

            return ctx;
        }

        // ── Save plan ───────────────────────────────────────────────────
        public async Task<SubmitCanopyPlanResponse> SubmitCanopyPlanAsync(
            SubmitCanopyPlanRequest req)
        {
            ValidatePlanSubmit(req);

            var pc       = req.PCCode.Trim();          // LineWisePC of the selected line (pcCode_Act)
            var pcOld    = req.ParentDgPC.Trim();      // ParentDgPC of the selected line (pcCode_Old)
            var company  = req.CompanyCode.Trim();
            var emp      = req.EmpCode.Trim();

            // pcCode_Act → (ProfitCenterCode_Act, ToProfitCenterCode) for the
            // Logistics-Kit REQ. Same driver as the JobcardService.
            string profitCenterCodeAct;
            string toprofitCenterCode;
            if (pc == "01.190" || pc == "03.069" || pc == "03.181")
            {
                profitCenterCodeAct = "23.001";
                toprofitCenterCode  = "23.001";
            }
            else if (pc == "28.025" || pc == "28.039" || pc == "28.116")
            {
                profitCenterCodeAct = "28.020";
                toprofitCenterCode  = "28.020";
            }
            else
            {
                // Fallback for any PC not yet mapped — keep parity with legacy
                // default of 23.001 so a mis-configured PC doesn't crash.
                profitCenterCodeAct = "23.001";
                toprofitCenterCode  = "23.001";
            }

            // pcCode_Act → (ProfitCenterCode_Act, ToProfitCenterCode) for the
            // Wiring-Harness REQ. Wiring team is different from Logistics.
            string whProfitCenterCodeAct;
            string whToProfitCenterCode;
            if (pc == "01.190" || pc == "03.069" || pc == "03.181")
            {
                whProfitCenterCodeAct = "01.091";
                whToProfitCenterCode  = "01.091";
            }
            else if (pc == "28.025" || pc == "28.039" || pc == "28.116")
            {
                whProfitCenterCodeAct = "28.020";
                whToProfitCenterCode  = "28.020";
            }
            else
            {
                whProfitCenterCodeAct = "01.091";
                whToProfitCenterCode  = "01.091";
            }

            var conn = _context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var sqlTx = (SqlTransaction)tx.GetDbTransaction();

                // 1) New CPCode "CPY/<yy-yy>/<CompID><N>"
                var cpCode = await GetMaxNoAsync(
                    prefix: "CPY",
                    compCode: company,
                    tblName: "CanopyPlan",
                    tx: sqlTx);
                var maxSrNo = ExtractSequencePart(cpCode);

                // 2) SP InsertCanopyPlan_Checker_Maker (master) — @AutoFlg='No' → PlanStatus='P'
                // @PCCode_Act receives the user-selected line-wise PC (LineWisePC
                // from the line-rights dropdown), which req.PCCode already carries.
                using (var cmd = new SqlCommand("InsertCanopyPlan_Checker_Maker", (SqlConnection)conn, sqlTx))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CPCode",      cpCode);
                    cmd.Parameters.AddWithValue("@Dt",          DateTime.Now);
                    cmd.Parameters.AddWithValue("@MaxSrNo",     maxSrNo);
                    cmd.Parameters.AddWithValue("@Yr",          await GetYearEndAsync());
                    cmd.Parameters.AddWithValue("@FromDt",      req.FromDt.Date);
                    cmd.Parameters.AddWithValue("@ToDt",        req.ToDt.Date);
                    cmd.Parameters.AddWithValue("@PlanPCCode",  pc);
                    cmd.Parameters.AddWithValue("@CompanyCode", company);
                    cmd.Parameters.AddWithValue("@PlanType",    "M");
                    cmd.Parameters.AddWithValue("@AutoFlg",     "No");
                    cmd.Parameters.AddWithValue("@PCCode_Act",  pc);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3) Per row: lookup PartCodeWOP, insert detail, then 2 auto-REQs.
                int srNo = 0;
                foreach (var row in req.Rows)
                {
                    srNo++;
                    var rowPartCode = (row.PartCode ?? string.Empty).Trim();
                    var rowBomCode  = (row.BomCode  ?? string.Empty).Trim();
                    var rowQty      = row.Qty;

                    // Look up PartCodeWOP — the BOM child with partcode LIKE '004%'
                    // for this canopy's KitCode.
                    var partCodeWOP = await GetScalarInTxAsync<string>(
                        (SqlConnection)conn, sqlTx,
                        @"SELECT TOP 1 Partcode
                          FROM   BOMDetails WITH (NOLOCK)
                          WHERE  BOMCode = @BomCode
                             AND KitCode = @PartCode
                             AND Partcode LIKE '004%';",
                        ("@BomCode", rowBomCode), ("@PartCode", rowPartCode)) ?? string.Empty;

                    // 3a) InsertCanopyPlanDetails
                    using (var cmd = new SqlCommand("InsertCanopyPlanDetails", (SqlConnection)conn, sqlTx))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CPCode",       cpCode);
                        cmd.Parameters.AddWithValue("@Dt",           row.Dt.Date);
                        cmd.Parameters.AddWithValue("@SrNo",         srNo);
                        cmd.Parameters.AddWithValue("@Partcode",     rowPartCode);
                        cmd.Parameters.AddWithValue("@BomCode",      rowBomCode);
                        cmd.Parameters.AddWithValue("@PartCodeWOP",  partCodeWOP);
                        cmd.Parameters.AddWithValue("@Qty",          rowQty);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // ─────────────────────────────────────────────────────────
                    // 3b + 3c) [MOVED TO CHECKER] Steps 6 & 11 auto-REQs.
                    //
                    // Previously fired here per plan detail row — but raising
                    // material REQs at plan-save time meant logistics started
                    // pulling material before QC had approved the plan. If the
                    // checker later rejects/reworks a plan, the REQs were
                    // speculative.
                    //
                    // Both blocks (Logistics-Kit REQ + Wiring-Harness REQ) now
                    // fire from SaveCanopyPlanCheckAsync's FirePlanCheckerAutoReqsAsync
                    // helper, only on the fresh Checker1 0->1 transition.
                    //
                    // Kept commented here as the reference implementation and
                    // an easy rollback point.
                    /*
                    var reqCompCode = company;
                    var logReqCode = await GetMaxNoAsync(
                        prefix: "REQ", compCode: reqCompCode,
                        tblName: "MaterialRequisitionWithOutPlan", tx: sqlTx);
                    var logMaxSrNo = ExtractSequencePart(logReqCode);
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC insertMaterialRequisitionWithOutPlanProcessVsPlan " +
                        "@REQCode, @MaxSrNo, @Dt, @Yr, @ProfitCenterCode, @ToProfitCenterCode, " +
                        "@ProfitCenterCode_Act, @ToProfitCenterCode_Act, " +
                        "@ClassCode, @ActNo, @SourceCode, @CompanyCode, " +
                        "@REQStatus, @REQType, @Remark, @Discard, @Active, @Auth",
                        new SqlParameter("@REQCode",                logReqCode),
                        new SqlParameter("@MaxSrNo",                logMaxSrNo),
                        new SqlParameter("@Dt",                     DateTime.Now),
                        new SqlParameter("@Yr",                     await GetYearEndAsync()),
                        new SqlParameter("@ProfitCenterCode",       pcOld),
                        new SqlParameter("@ToProfitCenterCode",     toprofitCenterCode),
                        new SqlParameter("@ProfitCenterCode_Act",   pc),
                        new SqlParameter("@ToProfitCenterCode_Act", profitCenterCodeAct),
                        new SqlParameter("@ClassCode",              rowPartCode),
                        new SqlParameter("@ActNo",                  rowQty.ToString()),
                        new SqlParameter("@SourceCode",             cpCode),
                        new SqlParameter("@CompanyCode",            company),
                        new SqlParameter("@REQStatus",              "P"),
                        new SqlParameter("@REQType",                "WIP"),
                        new SqlParameter("@Remark",                 $"Auto Req For : {rowPartCode} and Plan No: {cpCode}"),
                        new SqlParameter("@Discard",                1),
                        new SqlParameter("@Active",                 1),
                        new SqlParameter("@Auth",                   1));
                    var logKitRows = await GetInternalReqLogisticsKitAsync(
                        (SqlConnection)conn, sqlTx, rowPartCode, pcCodeStage: 3, requisitionFor: "029");
                    int logSr = 0;
                    foreach (var k in logKitRows)
                    {
                        logSr++;
                        using var cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails",
                            (SqlConnection)conn, sqlTx);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@REQCode",   logReqCode);
                        cmd.Parameters.AddWithValue("@SrNo",      logSr);
                        cmd.Parameters.AddWithValue("@PartCode",  k.PartCode);
                        cmd.Parameters.AddWithValue("@Qty",       k.RaiseReqQty * rowQty);
                        cmd.Parameters.AddWithValue("@REQStatus", "P");
                        await cmd.ExecuteNonQueryAsync();
                    }

                    var whReqCode = await GetMaxNoAsync(
                        prefix: "REQ", compCode: reqCompCode,
                        tblName: "MaterialRequisitionWithOutPlan", tx: sqlTx);
                    var whMaxSrNo = ExtractSequencePart(whReqCode);
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC insertMaterialRequisitionWithOutPlanProcessVsPlan " +
                        "@REQCode, @MaxSrNo, @Dt, @Yr, @ProfitCenterCode, @ToProfitCenterCode, " +
                        "@ProfitCenterCode_Act, @ToProfitCenterCode_Act, " +
                        "@ClassCode, @ActNo, @SourceCode, @CompanyCode, " +
                        "@REQStatus, @REQType, @Remark, @Discard, @Active, @Auth",
                        new SqlParameter("@REQCode",                whReqCode),
                        new SqlParameter("@MaxSrNo",                whMaxSrNo),
                        new SqlParameter("@Dt",                     DateTime.Now),
                        new SqlParameter("@Yr",                     await GetYearEndAsync()),
                        new SqlParameter("@ProfitCenterCode",       pcOld),
                        new SqlParameter("@ToProfitCenterCode",     whToProfitCenterCode),
                        new SqlParameter("@ProfitCenterCode_Act",   pc),
                        new SqlParameter("@ToProfitCenterCode_Act", whProfitCenterCodeAct),
                        new SqlParameter("@ClassCode",              rowPartCode),
                        new SqlParameter("@ActNo",                  rowQty.ToString()),
                        new SqlParameter("@SourceCode",             cpCode),
                        new SqlParameter("@CompanyCode",            company),
                        new SqlParameter("@REQStatus",              "P"),
                        new SqlParameter("@REQType",                "WIP"),
                        new SqlParameter("@Remark",                 $"Auto Req For : {rowPartCode} and Plan No: {cpCode}"),
                        new SqlParameter("@Discard",                1),
                        new SqlParameter("@Active",                 1),
                        new SqlParameter("@Auth",                   1));
                    var whRows = await GetInternalReqWHKitAsync(
                        (SqlConnection)conn, sqlTx, rowPartCode);
                    int whSr = 0;
                    foreach (var w in whRows)
                    {
                        whSr++;
                        using var cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails",
                            (SqlConnection)conn, sqlTx);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@REQCode",   whReqCode);
                        cmd.Parameters.AddWithValue("@SrNo",      whSr);
                        cmd.Parameters.AddWithValue("@PartCode",  w.PartCode);
                        cmd.Parameters.AddWithValue("@Qty",       w.RaiseReqQty * rowQty);
                        cmd.Parameters.AddWithValue("@REQStatus", "P");
                        await cmd.ExecuteNonQueryAsync();
                    }
                    */
                    // ─────────────────────────────────────────────────────────
                }

                // 4) Activity log
                await InsertLoginTxnAsync((SqlConnection)conn, sqlTx,
                    emp, "S", "CanopyPlan", cpCode, company);

                await tx.CommitAsync();
                return new SubmitCanopyPlanResponse
                {
                    Message = $"Canopy Plan Saved Successfully — Plan Code : {cpCode}",
                    CPCode  = cpCode,
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                var inner = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Error saving Canopy Plan: {inner}", ex);
            }
        }

        // ── Canopy Plan helpers ─────────────────────────────────────────

        private static async Task<List<ReqExplodeRow>> GetInternalReqLogisticsKitAsync(
            SqlConnection conn, SqlTransaction tx,
            string cpyPartCode, int pcCodeStage, string requisitionFor)
        {
            var rows = new List<ReqExplodeRow>();
            using var cmd = new SqlCommand("InternalReqLogisticsKit", conn, tx);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CpyPartCode",    cpyPartCode);
            cmd.Parameters.AddWithValue("@PCCode",         pcCodeStage);
            cmd.Parameters.AddWithValue("@RequisitionFor", requisitionFor);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new ReqExplodeRow
                {
                    PartCode    = SafeStr(reader, "Partcode"),
                    RaiseReqQty = SafeDouble(reader, "RaiseReqQty"),
                });
            }
            return rows;
        }

        private static async Task<List<ReqExplodeRow>> GetInternalReqWHKitAsync(
            SqlConnection conn, SqlTransaction tx, string canopyPartCode)
        {
            var rows = new List<ReqExplodeRow>();
            using var cmd = new SqlCommand("InternalReqLogisticsdetailsWHKIT_Canopy", conn, tx);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DGPartCode", canopyPartCode);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new ReqExplodeRow
                {
                    PartCode    = SafeStr(reader, "partcode"),
                    RaiseReqQty = SafeDouble(reader, "RaiseReqQty"),
                });
            }
            return rows;
        }

        // Scalar fetch within an open transaction.
        private static async Task<T?> GetScalarInTxAsync<T>(SqlConnection conn, SqlTransaction tx,
            string sql, params (string Name, object Value)[] parameters) where T : class
        {
            using var cmd = new SqlCommand(sql, conn, tx);
            foreach (var (n, v) in parameters) cmd.Parameters.AddWithValue(n, v ?? (object)DBNull.Value);
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value) return null;
            return (T)Convert.ChangeType(result, typeof(T));
        }

        private static void ValidatePlanSubmit(SubmitCanopyPlanRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.PCCode))      throw new ArgumentException("PCCode is required.");
            if (string.IsNullOrWhiteSpace(req.CompanyCode)) throw new ArgumentException("CompanyCode is required.");
            if (string.IsNullOrWhiteSpace(req.EmpCode))     throw new ArgumentException("EmpCode is required.");
            if (req.FromDt == default || req.ToDt == default)
                throw new ArgumentException("FromDt and ToDt are required.");
            if (req.FromDt > req.ToDt)
                throw new ArgumentException("FromDt cannot be after ToDt.");
            if (req.Rows == null || req.Rows.Count == 0)
                throw new ArgumentException("At least one plan row is required.");
            foreach (var (row, i) in req.Rows.Select((r, i) => (r, i + 1)))
            {
                if (string.IsNullOrWhiteSpace(row.PartCode)) throw new ArgumentException($"Row {i}: PartCode is required.");
                if (string.IsNullOrWhiteSpace(row.BomCode))  throw new ArgumentException($"Row {i}: BomCode is required.");
                if (row.Qty <= 0)                            throw new ArgumentException($"Row {i}: Qty must be greater than 0.");
            }
        }

        // ── Stored-procedure wrapper: getcpyplandts_checker_maker ───────
        // Returns one row per candidate canopy part for the selected line
        // (the SP applies the per-PC KVA tier + stock + pending internally).
        public async Task<List<CanopyPlanCheckerMakerRowDto>> GetCanopyPlanCheckerMakerRowsAsync(
            string lineWisePC)
        {
            var rows = new List<CanopyPlanCheckerMakerRowDto>();
            var pc = (lineWisePC ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(pc)) return rows;

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand("getcpyplandts_checker_maker", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@PCCode", SqlDbType.NVarChar, 50).Value = pc;

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new CanopyPlanCheckerMakerRowDto
                {
                    BOMCode  = SafeStr(reader, "BOMCode"),
                    PartDesc = SafeStr(reader, "PartDesc"),
                    PartCode = SafeStr(reader, "PartCode"),
                    UName    = SafeStr(reader, "UName"),
                    KVA      = SafeDouble(reader, "KVA"),
                    StkQty   = SafeDouble(reader, "StkQty"),
                    PendQty  = SafeDouble(reader, "PendQty"),
                });
            }
            return rows;
        }

        private sealed class ReqExplodeRow
        {
            public string PartCode    { get; set; } = string.Empty;
            public double RaiseReqQty { get; set; }
        }

        private sealed class CpySerialRow
        {
            public string SerialNo { get; set; } = string.Empty;
            public string BFMSrNo  { get; set; } = string.Empty;
            public string FLKSrNo  { get; set; } = string.Empty;
        }

        // ════════════════════════════════════════════════════════════════
        //  Canopy Assembly Process (operator-side) — line-rights aware
        // ════════════════════════════════════════════════════════════════

        // Kit-Below-Standard-Rate spare warehouse (unchanged from legacy).
        private const string BelowRateStockPC = "01.007";

        // Temp folder base (per-employee) — matches legacy `TempPrcCpy` layout.
        // Kept as constants to avoid a config dependency; move to appsettings
        // if/when the deployment target changes.
        private const string CanopyProcessTempBase      = @"C:\TempERPFile\TempPrcCpy";
        private const string CanopyProcessPermanentBase = @"C:\ERPFiles\TempPrcCpy";

        // ── 1) Canopy Type dropdown ─────────────────────────────────────
        // Legacy LoadMachine SP for canopy PCs returns 2 hardcoded rows
        // (Foam / RockWool). We return the same shape here to avoid coupling
        // to a per-PC SP branch that doesn't exist for LineWisePC values.
        public Task<List<CanopyProcessMachineDto>> GetCanopyProcessMachineListAsync(string pcCode)
        {
            var rows = new List<CanopyProcessMachineDto>
            {
                new() { AMCode = "1", Part = "Foam",     PartCode = "Foam-->Foam1"     },
                new() { AMCode = "2", Part = "RockWool", PartCode = "RockWool-->RockWool1" },
            };
            return Task.FromResult(rows);
        }

        // ── 2) KVA list ─────────────────────────────────────────────────
        // Sourced from CanopyPlanDetails + CanopyPlan + Part (the plan-driven
        // master), NOT ProcessFeedback — the Plan submit populates the former
        // and never sets MachineCode='Foam'/SerialNo='Foam1' on the latter, so
        // filtering PF by MachineCode/SerialNo returned zero rows every time.
        // The `machineCode` parameter is accepted for wire compatibility with
        // the Angular caller but intentionally not used in the query (the
        // Canopy Type is Foam-vs-RockWool: v1 treats it as display metadata
        // since CanopyPlanDetails doesn't record insulation type yet).
        public async Task<List<CanopyProcessKvaDto>> GetCanopyProcessKvaListAsync(
            string machineCode, string pcCode)
        {
            _ = machineCode;   // reserved for future canopy-type filter
            var rows = new List<CanopyProcessKvaDto>();
            var pc = (pcCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(pc)) return rows;

            const string sql = @"
SELECT   P.KVA, P.KVA AS KVA1
FROM     CanopyPlanDetails d  WITH (NOLOCK)
INNER    JOIN CanopyPlan   cp WITH (NOLOCK) ON cp.CPCode  = d.CPCode
INNER    JOIN Part         P  WITH (NOLOCK) ON P.PartCode = d.Partcode
WHERE    cp.PlanPCCode = @PCCode
  AND    ISNULL(cp.Active, '1')       = '1'
  AND    ISNULL(cp.Checker1, 0)       = 1
  AND    CAST(GETDATE() AS date) BETWEEN CAST(cp.FromDt AS date) AND CAST(cp.ToDt AS date)
  AND    (ISNULL(d.Qty, 0) - ISNULL(d.CpyWIPQty, 0)) > 0
  AND    P.KVA IS NOT NULL
  AND    LTRIM(RTRIM(P.KVA)) <> ''
GROUP BY P.KVA
ORDER BY TRY_CAST(P.KVA AS decimal(10,2));";

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@PCCode", SqlDbType.NVarChar, 20).Value = pc;

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new CanopyProcessKvaDto
                {
                    KVA  = SafeStr(reader, "KVA"),
                    KVA1 = SafeStr(reader, "KVA1"),
                });
            }
            return rows;
        }

        // ── 3) Model list ───────────────────────────────────────────────
        // Same source table as KVA — CanopyPlanDetails + CanopyPlan + Part.
        // Cascades from the KVA the operator just picked.
        public async Task<List<CanopyProcessModelDto>> GetCanopyProcessModelListAsync(
            string machineCode, string kva, string pcCode)
        {
            _ = machineCode;   // reserved
            var rows = new List<CanopyProcessModelDto>();
            var pc = (pcCode ?? string.Empty).Trim();
            var kv = (kva ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(pc) || string.IsNullOrEmpty(kv)) return rows;

            const string sql = @"
SELECT   P.Model, P.Model AS Model1
FROM     CanopyPlanDetails d  WITH (NOLOCK)
INNER    JOIN CanopyPlan   cp WITH (NOLOCK) ON cp.CPCode  = d.CPCode
INNER    JOIN Part         P  WITH (NOLOCK) ON P.PartCode = d.Partcode
WHERE    cp.PlanPCCode = @PCCode
  AND    ISNULL(cp.Active, '1')     = '1'
  AND    ISNULL(cp.Checker1, 0)     = 1
  AND    CAST(GETDATE() AS date) BETWEEN CAST(cp.FromDt AS date) AND CAST(cp.ToDt AS date)
  AND    (ISNULL(d.Qty, 0) - ISNULL(d.CpyWIPQty, 0)) > 0
  AND    P.KVA   = @KVA
  AND    P.Model IS NOT NULL
  AND    LTRIM(RTRIM(P.Model)) <> ''
GROUP BY P.Model
ORDER BY P.Model;";

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@PCCode", SqlDbType.NVarChar, 20).Value = pc;
            cmd.Parameters.Add("@KVA",    SqlDbType.NVarChar, 20).Value = kv;

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new CanopyProcessModelDto
                {
                    Model  = SafeStr(reader, "Model"),
                    Model1 = SafeStr(reader, "Model1"),
                });
            }
            return rows;
        }

        // ── 4) Plan context (top-1 plan row for the picked KVA + Model) ─
        // Sourced from CanopyPlanDetails + CanopyPlan + Part. LEFT JOIN to
        // ProcessFeedback surfaces an already-open PSH record if one exists
        // (→ End mode); otherwise we synthesise a "NEW/{yr}/{seq}" placeholder
        // from the CPCode so the Save-path's prefix check routes to the NEW
        // (Start) branch and creates a fresh PSH record on submit.
        public async Task<CanopyProcessPlanContextDto?> GetCanopyProcessPlanContextAsync(
            string machineCode, string kva, string model, string pcCode)
        {
            _ = machineCode;   // reserved
            var pc = (pcCode ?? string.Empty).Trim();
            var kv = (kva ?? string.Empty).Trim();
            var md = (model ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(pc) || string.IsNullOrEmpty(kv) || string.IsNullOrEmpty(md))
                return null;

            const string sql = @"
SELECT TOP 1
       CONVERT(VARCHAR(10), P.KVA) + '-->' + P.Model                   AS KVAMod,
       P.KVA                                                            AS KVA,
       P.Model                                                          AS Model,
       cp.CPCode                                                        AS CPCode,
       CONVERT(VARCHAR(19), cp.Dt, 120)                                 AS Dt,
       d.Partcode                                                       AS Partcode,
       P.PartDesc + '-->' + d.Partcode                                  AS Part,
       ISNULL(d.Qty, 0)                                                 AS CPQty,
       (ISNULL(d.Qty, 0) - ISNULL(d.CpyWIPQty, 0))                      AS PlanQtyBal,
       (ISNULL(d.Qty, 0) - ISNULL(d.CpyWIPQty, 0))                      AS PrcQty,
       COALESCE(
           pf.PFBCode,
           'NEW/' + SUBSTRING(cp.CPCode, 5, LEN(cp.CPCode))
       )                                                                AS PFBCode,
       CONVERT(VARCHAR(19), pf.EDt, 120)                                AS EDt,
       d.BomCode                                                        AS BOMCode,
       ''                                                               AS SCode
FROM   CanopyPlanDetails d  WITH (NOLOCK)
INNER  JOIN CanopyPlan   cp WITH (NOLOCK) ON cp.CPCode  = d.CPCode
INNER  JOIN Part         P  WITH (NOLOCK) ON P.PartCode = d.Partcode
LEFT   JOIN ProcessFeedback pf WITH (NOLOCK)
       ON  pf.CanopyPlanCode = cp.CPCode
       AND pf.ProductCode    = d.Partcode
       AND pf.EDt IS NULL
       AND pf.PFBCode LIKE 'PSH/%'
WHERE  cp.PlanPCCode = @PCCode
   AND ISNULL(cp.Active, '1')     = '1'
   AND ISNULL(cp.Checker1, 0)     = 1
   AND CAST(GETDATE() AS date) BETWEEN CAST(cp.FromDt AS date) AND CAST(cp.ToDt AS date)
   AND (ISNULL(d.Qty, 0) - ISNULL(d.CpyWIPQty, 0)) > 0
   AND P.KVA   = @KVA
   AND P.Model = @Model
ORDER BY cp.Dt ASC, cp.CPCode ASC;";

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@PCCode", SqlDbType.NVarChar, 20).Value = pc;
            cmd.Parameters.Add("@KVA",    SqlDbType.NVarChar, 20).Value = kv;
            cmd.Parameters.Add("@Model",  SqlDbType.NVarChar, 50).Value = md;

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new CanopyProcessPlanContextDto
            {
                KVAMod     = SafeStr(reader, "KVAMod"),
                KVA        = SafeStr(reader, "KVA"),
                Model      = SafeStr(reader, "Model"),
                CPCode     = SafeStr(reader, "CPCode"),
                Dt         = SafeStr(reader, "Dt"),
                Partcode   = SafeStr(reader, "Partcode"),
                Part       = SafeStr(reader, "Part"),
                CPQty      = SafeDouble(reader, "CPQty"),
                PlanQtyBal = SafeDouble(reader, "PlanQtyBal"),
                PrcQty     = SafeDouble(reader, "PrcQty"),
                PFBCode    = SafeStr(reader, "PFBCode"),
                EDt        = SafeStr(reader, "EDt"),
                BOMCode    = SafeStr(reader, "BOMCode"),
                SCode      = SafeStr(reader, "SCode"),
            };
        }

        // ── 5) Kit list (PSH mode) ──────────────────────────────────────
        public async Task<List<CanopyProcessKitDto>> GetCanopyProcessKitListAsync(
            string machineCode, string pcCode, string planCode, string partCode)
        {
            var rows = new List<CanopyProcessKitDto>();
            var pc = (pcCode ?? string.Empty).Trim();
            var plan = (planCode ?? string.Empty).Trim();
            var part = (partCode ?? string.Empty).Trim();
            var (machine, serial) = SplitMachineSerial(machineCode);
            if (string.IsNullOrEmpty(pc)) return rows;

            const string sql = @"
SELECT AliseName                                    AS KitDesc,
       pf.PartCode + '-->' + P.PartDesc             AS KitCode,
       pf.PFBCode                                    AS PfbCode,
       CONVERT(VARCHAR(19), pf.EDt, 120)            AS EDt
FROM   ProcessFeedback pf  WITH (NOLOCK)
INNER JOIN Part         P WITH (NOLOCK) ON pf.PartCode = P.PartCode
WHERE  pf.ProfitCenterCode = @PCCode
   AND pf.MachineCode      = @Machine
   AND pf.SerialNo         = @Serial
   AND pf.EDt IS NULL
   AND pf.CanopyPlanCode   = @PlanCode
   AND pf.ProductCode      = @PartCode
   AND pf.Active = '1'
   AND pf.Dt >= '2020-07-10 00:00:00'
ORDER BY pf.Dt DESC;";

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@PCCode",   SqlDbType.NVarChar, 20).Value = pc;
            cmd.Parameters.Add("@Machine",  SqlDbType.NVarChar, 50).Value = machine;
            cmd.Parameters.Add("@Serial",   SqlDbType.NVarChar, 50).Value = serial;
            cmd.Parameters.Add("@PlanCode", SqlDbType.NVarChar, 50).Value = plan;
            cmd.Parameters.Add("@PartCode", SqlDbType.NVarChar, 50).Value = part;

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new CanopyProcessKitDto
                {
                    KitDesc = SafeStr(reader, "KitDesc"),
                    KitCode = SafeStr(reader, "KitCode"),
                    PfbCode = SafeStr(reader, "PfbCode"),
                    EDt     = SafeStr(reader, "EDt"),
                });
            }
            return rows;
        }

        // ── 6) Kit context (Bal + rate) — PSH mode after kit pick ───────
        public async Task<CanopyProcessKitContextDto?> GetCanopyProcessKitContextAsync(
            string machineCode, string kitCode, string pcCode,
            string planCode, string partCode)
        {
            var pc = (pcCode ?? string.Empty).Trim();
            var plan = (planCode ?? string.Empty).Trim();
            var part = (partCode ?? string.Empty).Trim();
            var kit = (kitCode ?? string.Empty).Trim();
            var (machine, serial) = SplitMachineSerial(machineCode);
            if (string.IsNullOrEmpty(pc) || string.IsNullOrEmpty(kit)) return null;

            const string sql = @"
SELECT ISNULL(pf.ProcessQty, 0) AS Bal,
       ISNULL(pf.PFBRate,    0) AS SRate
FROM   ProcessFeedback pf  WITH (NOLOCK)
INNER JOIN Part         P WITH (NOLOCK) ON pf.PartCode = P.PartCode
WHERE  pf.ProfitCenterCode = @PCCode
   AND pf.MachineCode      = @Machine
   AND pf.SerialNo         = @Serial
   AND pf.EDt IS NULL
   AND pf.CanopyPlanCode   = @PlanCode
   AND pf.ProductCode      = @PartCode
   AND pf.PartCode         = @KitCode
   AND pf.Active = '1'
   AND pf.Dt >= '2020-07-10 00:00:00'
ORDER BY pf.Dt DESC;";

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@PCCode",   SqlDbType.NVarChar, 20).Value = pc;
            cmd.Parameters.Add("@Machine",  SqlDbType.NVarChar, 50).Value = machine;
            cmd.Parameters.Add("@Serial",   SqlDbType.NVarChar, 50).Value = serial;
            cmd.Parameters.Add("@PlanCode", SqlDbType.NVarChar, 50).Value = plan;
            cmd.Parameters.Add("@PartCode", SqlDbType.NVarChar, 50).Value = part;
            cmd.Parameters.Add("@KitCode",  SqlDbType.NVarChar, 50).Value = kit;

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new CanopyProcessKitContextDto
            {
                Bal   = SafeDouble(reader, "Bal"),
                SRate = SafeDouble(reader, "SRate"),
            };
        }

        // ── 7) Part Details (top table) ─────────────────────────────────
        // PSH mode: reads processfeedbackdetails filtered by PFBCode (already-open record).
        // NEW mode: explodes the BOM at the LineWisePC — rate/wt/sqft from
        // ProfitcenterPLDetails at LineWisePC (was hardcoded to 01.005 in legacy).
        public async Task<List<CanopyProcessPartRowDto>> GetCanopyProcessPartRowsAsync(
            string pcCode, int prcQty, string cpyPartCode,
            string planCode, string bomCode, string pfbCode)
        {
            var rows = new List<CanopyProcessPartRowDto>();
            var pc = (pcCode ?? string.Empty).Trim();
            var pfb = (pfbCode ?? string.Empty).Trim();
            var bom = (bomCode ?? string.Empty).Trim();
            var isPsh = pfb.StartsWith("PSH", StringComparison.OrdinalIgnoreCase);

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            await connection.OpenAsync();

            if (isPsh)
            {
                const string sqlPsh = @"
SELECT P.AliseName                                   AS Part,
       0.0                                            AS KitQty,
       pf.TotQty                                      AS PrcQty,
       0.0                                            AS StkQty,
       0.0                                            AS Wt,
       0.0                                            AS TotWt,
       0.0                                            AS Sqft,
       0.0                                            AS TotSqft,
       ISNULL(pf.PFBRate, 0)                          AS Rate,
       pf.PartCode                                    AS PartCode
FROM   ProcessFeedbackDetails pf WITH (NOLOCK)
INNER JOIN Part               P  WITH (NOLOCK) ON pf.PartCode = P.PartCode
WHERE  pf.PFBCode = @PFBCode
   AND pf.PartCode LIKE '004%';";
                using var cmd = new SqlCommand(sqlPsh, connection);
                cmd.Parameters.Add("@PFBCode", SqlDbType.NVarChar, 50).Value = pfb;
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    rows.Add(ReadPartRow(reader));
                return rows;
            }

            // NEW mode — explode the BOM's kit lines (KitCode pattern '0121/0122'
            // + substring(12,2)='12' = assembly stage). Rate/Wt/Sqft from
            // ProfitcenterPLDetails at LineWisePC. Stock filters use the
            // line-rights *_Act columns (matches the seed convention).
            const string sqlNew = @"
SELECT P.PartDesc + '-->' + Bd.PartCode                          AS Part,
       Bd.Qty                                                    AS KitQty,
       ROUND(@PrcQty * Bd.Qty, 2)                                AS PrcQty,
       ISNULL((
           SELECT ROUND(ISNULL(SUM(Recqty) - SUM(IssueQty), 0), 2)
           FROM (
               SELECT SUM(ReceivedQty) AS Recqty, 0.0 AS IssueQty
               FROM stockwip
               WHERE ToProfitCenterCode_Act = @PCCode
                 AND StockType = '0'
                 AND Partcode = Bd.PartCode
                 AND ReceivedQty > 0
               UNION ALL
               SELECT 0.0 AS Recqty, SUM(IssueQty) AS IssueQty
               FROM stockwip
               WHERE FromProfitCenterCode_Act = @PCCode
                 AND StockType = '0'
                 AND Partcode = Bd.PartCode
                 AND IssueQty > 0
           ) AS stk), 0)                                          AS StkQty,
       ISNULL(pl.PWt,   0)                                       AS Wt,
       ROUND(@PrcQty * ISNULL(pl.PWt, 0), 2)                     AS TotWt,
       ISNULL(pl.PSqft, 0)                                       AS Sqft,
       ROUND(@PrcQty * ISNULL(pl.PSqft, 0), 2)                   AS TotSqft,
       ISNULL(pl.Rate,  0)                                       AS Rate,
       Bd.PartCode                                                AS PartCode
FROM   BOMDetails Bd
INNER JOIN Part   P  ON Bd.PartCode = P.PartCode
LEFT  JOIN ProfitcenterPLDetails pl WITH (NOLOCK)
       ON pl.PartCode = Bd.PartCode AND pl.ProfitcenterCode = @PCCode
WHERE  Bd.BOMCode = @BOMCode
   AND SUBSTRING(Bd.KitCode, 1, 4) IN ('0121','0122')
   AND SUBSTRING(Bd.KitCode, 12, 2) = '12';";

            using (var cmd = new SqlCommand(sqlNew, connection))
            {
                cmd.Parameters.Add("@BOMCode", SqlDbType.NVarChar, 50).Value = bom;
                cmd.Parameters.Add("@PCCode",  SqlDbType.NVarChar, 20).Value = pc;
                cmd.Parameters.Add("@PrcQty",  SqlDbType.Int).Value          = prcQty;
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    rows.Add(ReadPartRow(reader));
            }
            return rows;
        }

        // ── 8) Assembly Kit Details (bottom mat-table) ──────────────────
        // PSH mode: reads processfeedbackdetails MOB='B' rows.
        // NEW mode: explodes the BOM's assembly kits ('0121/0122' + substr(12,2)='12').
        public async Task<List<CanopyProcessAssemblyKitRowDto>> GetCanopyProcessAssemblyKitRowsAsync(
            string pcCode, int prcQty, string cpyPartCode,
            string planCode, string bomCode, string pfbCode)
        {
            var rows = new List<CanopyProcessAssemblyKitRowDto>();
            var pc = (pcCode ?? string.Empty).Trim();
            var pfb = (pfbCode ?? string.Empty).Trim();
            var bom = (bomCode ?? string.Empty).Trim();
            var isPsh = pfb.StartsWith("PSH", StringComparison.OrdinalIgnoreCase);

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            await connection.OpenAsync();

            if (isPsh)
            {
                const string sqlPsh = @"
SELECT P.PartDesc + '-->' + pf.PartCode  AS Part,
       pf.KitQty                          AS Qty,
       pf.TotQty                          AS PrcQty,
       pf.StockQty                        AS StkQty,
       pf.PartCode                        AS PartCode
FROM   ProcessFeedbackDetails pf WITH (NOLOCK)
INNER JOIN Part               P  WITH (NOLOCK) ON pf.PartCode = P.PartCode
WHERE  pf.PFBCode = @PFBCode
   AND pf.MOB    = 'B';";
                using var cmd = new SqlCommand(sqlPsh, connection);
                cmd.Parameters.Add("@PFBCode", SqlDbType.NVarChar, 50).Value = pfb;
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    rows.Add(ReadAssemblyKitRow(reader));
                return rows;
            }

            // Stock filters use the line-rights *_Act columns (matches the
            // seed convention that populates ToProfitCenterCode_Act with
            // LineWisePC on receipts and FromProfitCenterCode_Act on issues).
            const string sqlNew = @"
SELECT P.PartDesc + '-->' + Bd.PartCode                          AS Part,
       Bd.Qty                                                    AS Qty,
       ROUND(@PrcQty * Bd.Qty, 2)                                AS PrcQty,
       ISNULL((
           SELECT ROUND(ISNULL(SUM(Recqty) - SUM(IssueQty), 0), 2)
           FROM (
               SELECT SUM(ReceivedQty) AS Recqty, 0.0 AS IssueQty
               FROM stockwip
               WHERE ToProfitCenterCode_Act = @PCCode
                 AND StockType = '0'
                 AND Partcode = Bd.PartCode
                 AND ReceivedQty > 0
               UNION ALL
               SELECT 0.0 AS Recqty, SUM(IssueQty) AS IssueQty
               FROM stockwip
               WHERE FromProfitCenterCode_Act = @PCCode
                 AND StockType = '0'
                 AND Partcode = Bd.PartCode
                 AND IssueQty > 0
           ) AS stk), 0)                                          AS StkQty,
       Bd.PartCode                                                AS PartCode
FROM   BOMDetails Bd
INNER JOIN Part   P ON Bd.PartCode = P.PartCode
WHERE  Bd.BOMCode = @BOMCode
   AND SUBSTRING(Bd.KitCode, 1, 4) IN ('0121','0122')
   AND SUBSTRING(Bd.KitCode, 12, 2) = '12';";

            using (var cmd = new SqlCommand(sqlNew, connection))
            {
                cmd.Parameters.Add("@BOMCode", SqlDbType.NVarChar, 50).Value = bom;
                cmd.Parameters.Add("@PCCode",  SqlDbType.NVarChar, 20).Value = pc;
                cmd.Parameters.Add("@PrcQty",  SqlDbType.Int).Value          = prcQty;
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    rows.Add(ReadAssemblyKitRow(reader));
            }
            return rows;
        }

        // ── 9) Submit — NEW (Start) + PSH (End) ─────────────────────────
        public async Task<SubmitCanopyProcessResponse> SubmitCanopyProcessAsync(
            SubmitCanopyProcessRequest req)
        {
            ValidateCanopyProcessSubmit(req);

            var pfb = req.PFBCode.Trim();
            if (pfb.StartsWith("NEW", StringComparison.OrdinalIgnoreCase))
                return await SubmitCanopyProcessNewAsync(req);
            if (pfb.StartsWith("PSH", StringComparison.OrdinalIgnoreCase))
                return await SubmitCanopyProcessPshAsync(req);
            throw new ArgumentException($"Unexpected PFBCode prefix: '{pfb.Substring(0, Math.Min(3, pfb.Length))}'. Expected NEW or PSH.");
        }

        // ── 9a) NEW path — creates a fresh PSH record ────────────────────
        private async Task<SubmitCanopyProcessResponse> SubmitCanopyProcessNewAsync(
            SubmitCanopyProcessRequest req)
        {
            var pc      = req.PCCode.Trim();            // LineWisePC     (selected line's active PC)
            var pcOld   = req.ParentDgPC.Trim();        // ParentDgPC     (selected line's old/parent PC)
            var company = req.CompanyCode.Trim();
            var emp     = req.EmpCode.Trim();
            var (machine, serial) = SplitMachineSerial(req.MachineCodeSrNo);

            var conn = _context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            await using var tx = await _context.Database.BeginTransactionAsync();
            var sqlTx = (SqlTransaction)tx.GetDbTransaction();

            try
            {
                // 1) Rate/Wt/Sqft lookup for the canopy product at the current
                //    LineWisePC (was hardcoded 01.005 in legacy).
                var rateWtSqft = await GetProductRateWtSqftAsync(
                    (SqlConnection)conn, sqlTx, pc, req.ProductCode.Trim());

                // 2) Generate a fresh PSH code — legacy GetmaxPrc uses
                //    ProcessFeedback + CompanyCode + Yr as the max-scan key.
                var prcNo = await GetProcessFeedbackMaxNoAsync(sqlTx, "PSH", company);
                var maxSrNo = ExtractSequencePart(prcNo);
                var yearEnd = await GetYearEndAsync();

                // 3) Master insert into ProcessFeedback.
                //    ProfitCenterCode receives ParentDgPC (old/parent PC of the
                //    selected line); the new PCCode_Act column receives
                //    LineWisePC (the line's active PC). Same driver as the Plan
                //    submit's Step-6 / Step-11 MaterialRequisitionWithOutPlan
                //    inserts.
                using (var cmd = new SqlCommand(@"
INSERT INTO ProcessFeedback
    (GroupPFBCode, PFBCode, MaxSrNo, Dt, EDt, Yr, MachineCode, SerialNo,
     ProfitCenterCode, ProductCode, CanopyPlanCode, TurretKitCode, PartCode,
     ProcessQty, CompanyCode, PFBRate, PPWCode, Remark,
     WtPerUt, SqftPerUt, NstWtPerUt, NstSqftPerUt, PCCode_Act)
VALUES (@PFBCode, @PFBCode, @MaxSrNo, @Dt, NULL, @Yr, @Machine, @Serial,
        @PCCode, @ProductCode, @PlanCode, @BOMCode, @ProductCode,
        @PrcQty, @CompanyCode, @Rate, @EmpCode, 'Nil',
        @Wt, @Sqft, @TotWt, @TotSqft, @PCCodeAct);",
                    (SqlConnection)conn, sqlTx))
                {
                    cmd.Parameters.AddWithValue("@PFBCode",     prcNo);
                    cmd.Parameters.AddWithValue("@MaxSrNo",     maxSrNo);
                    cmd.Parameters.AddWithValue("@Dt",          DateTime.Now);
                    cmd.Parameters.AddWithValue("@Yr",          yearEnd ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Machine",     machine);
                    cmd.Parameters.AddWithValue("@Serial",      serial);
                    cmd.Parameters.AddWithValue("@PCCode",      pcOld);   // ParentDgPC -> ProfitCenterCode
                    cmd.Parameters.AddWithValue("@ProductCode", req.ProductCode.Trim());
                    cmd.Parameters.AddWithValue("@PlanCode",    req.PlanCode.Trim());
                    cmd.Parameters.AddWithValue("@BOMCode",     req.BOMCode.Trim());
                    cmd.Parameters.AddWithValue("@PrcQty",      req.PrcQty);
                    cmd.Parameters.AddWithValue("@CompanyCode", company);
                    cmd.Parameters.AddWithValue("@Rate",        rateWtSqft.rate);
                    cmd.Parameters.AddWithValue("@EmpCode",     emp);
                    cmd.Parameters.AddWithValue("@Wt",          rateWtSqft.wt);
                    cmd.Parameters.AddWithValue("@Sqft",        rateWtSqft.sqft);
                    cmd.Parameters.AddWithValue("@TotWt",       Math.Round(req.PrcQty * rateWtSqft.wt,   2));
                    cmd.Parameters.AddWithValue("@TotSqft",     Math.Round(req.PrcQty * rateWtSqft.sqft, 2));
                    cmd.Parameters.AddWithValue("@PCCodeAct",   pc);      // LineWisePC -> PCCode_Act
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4) Loop kit lines — insert processfeedbackdetails + StockWIP
                //    issue, then run BR-alternate substitution if this line is
                //    the top-rate part in the plan sub.
                int srNo = 0;
                foreach (var line in req.PrcDts)
                {
                    srNo++;
                    await InsertProcessFeedbackDetailsAsync((SqlConnection)conn, sqlTx,
                        prcNo, srNo, line);
                    await InsertStockWipIssueAsync((SqlConnection)conn, sqlTx,
                        pc, line.PartCode, prcNo, line.PrcQty);

                    var brError = await ProcessKitBelowStdRateAsync(
                        (SqlConnection)conn, sqlTx, pc, prcNo, srNo,
                        req.PlanCode.Trim(), req.ProductCode.Trim(), line, req.PrcQty);
                    if (!string.IsNullOrEmpty(brError))
                    {
                        await tx.RollbackAsync();
                        throw new InvalidOperationException(brError);
                    }
                }

                // 5) Serial-number pull — Bangalore variant if company == "28".
                var isBangalore = company.Equals("28", StringComparison.OrdinalIgnoreCase);
                var serials = await GetCanopyProcessSerialsAsync(
                    (SqlConnection)conn, sqlTx, req.ProductCode.Trim(), (int)req.PrcQty, isBangalore);
                if (serials.Count < (int)req.PrcQty)
                {
                    await tx.RollbackAsync();
                    throw new InvalidOperationException(
                        $"Serial No Qty: ({serials.Count}) is less than Process Qty: {req.PrcQty}");
                }

                for (int m = 0; m < (int)req.PrcQty; m++)
                {
                    var srl = serials[m];
                    using (var cmd = new SqlCommand(@"
INSERT INTO ProcessFeedbackDetailsSub
    (PFBCode, SrNo, PartCode, SerialNo, PFBBOTSerialNo, BFMSrNo, FLKSrNo,
     Status, QPCStatus, RWStatus)
VALUES (@PFBCode, @SrNo, @ProductCode, @SerialNo, @SerialNo, @BFMSrNo, @FLKSrNo,
        'P', 'OK', 'OK');",
                        (SqlConnection)conn, sqlTx))
                    {
                        cmd.Parameters.AddWithValue("@PFBCode",     prcNo);
                        cmd.Parameters.AddWithValue("@SrNo",        m + 1);
                        cmd.Parameters.AddWithValue("@ProductCode", req.ProductCode.Trim());
                        cmd.Parameters.AddWithValue("@SerialNo",    srl.SerialNo);
                        cmd.Parameters.AddWithValue("@BFMSrNo",     srl.BFMSrNo);
                        cmd.Parameters.AddWithValue("@FLKSrNo",     srl.FLKSrNo);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Bangalore: mark upstream engine-assembly serial as booked.
                    // Others: mark plan-generated serial as delivered.
                    if (isBangalore && !string.IsNullOrEmpty(srl.SourcePfbCode))
                    {
                        using var cmd = new SqlCommand(@"
UPDATE ProcessFeedbackDetailsSub
   SET JobCardStatus = 'B'
 WHERE PfbCode  = @SourcePfb
   AND SerialNo = @SerialNo
   AND Partcode = @ProductCode;",
                            (SqlConnection)conn, sqlTx);
                        cmd.Parameters.AddWithValue("@SourcePfb",   srl.SourcePfbCode);
                        cmd.Parameters.AddWithValue("@SerialNo",    srl.SerialNo);
                        cmd.Parameters.AddWithValue("@ProductCode", req.ProductCode.Trim());
                        await cmd.ExecuteNonQueryAsync();
                    }
                    else if (!isBangalore)
                    {
                        using var cmd = new SqlCommand(@"
UPDATE CanopyPlanSerialNo
   SET CPYSerialStatus = 'D'
 WHERE SerialNo = @SerialNo
   AND Partcode = @ProductCode;",
                            (SqlConnection)conn, sqlTx);
                        cmd.Parameters.AddWithValue("@SerialNo",    srl.SerialNo);
                        cmd.Parameters.AddWithValue("@ProductCode", req.ProductCode.Trim());
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // 6) Assembly-kit stock check + insertion.
                var asslyRows = await GetAssemblyKitStockAsync(
                    (SqlConnection)conn, sqlTx, req.BOMCode.Trim(), pc);
                var shortList = new List<string>();
                int asslySr = srNo;
                foreach (var k in asslyRows)
                {
                    if (Math.Round(k.Qty * req.PrcQty, 2) > Math.Round(k.Stock, 2))
                    {
                        shortList.Add(k.PartDesc);
                        continue;
                    }
                    asslySr++;
                    using (var cmd = new SqlCommand(@"
INSERT INTO ProcessFeedbackDetails
    (PFBCode, SrNo, PartCode, KITQty, TotQty, SaleRate)
VALUES (@PFBCode, @SrNo, @PartCode, @Qty, @TotQty, @Rate);",
                        (SqlConnection)conn, sqlTx))
                    {
                        cmd.Parameters.AddWithValue("@PFBCode",  prcNo);
                        cmd.Parameters.AddWithValue("@SrNo",     asslySr);
                        cmd.Parameters.AddWithValue("@PartCode", k.PartCode);
                        cmd.Parameters.AddWithValue("@Qty",      k.Qty);
                        cmd.Parameters.AddWithValue("@TotQty",   k.Qty * req.PrcQty);
                        cmd.Parameters.AddWithValue("@Rate",     k.SuppRate);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    await InsertStockWipIssueAsync((SqlConnection)conn, sqlTx,
                        pc, k.PartCode, prcNo, k.Qty * req.PrcQty);
                }
                if (shortList.Count > 0)
                {
                    await tx.RollbackAsync();
                    throw new InvalidOperationException(
                        "Insufficient Stock (Assly Kit) For Part: " + string.Join(", ", shortList));
                }

                // 7) Update CanopyPlanDetails.WIP counts.
                using (var cmd = new SqlCommand(@"
UPDATE CanopyPlanDetails
   SET CPYWIPQty = CPYWIPQty + @PrcQty,
       CPYWOPQty = CPYWOPQty + @PrcQty
 WHERE CPCode   = @PlanCode
   AND Partcode = @ProductCode;",
                    (SqlConnection)conn, sqlTx))
                {
                    cmd.Parameters.AddWithValue("@PrcQty",      req.PrcQty);
                    cmd.Parameters.AddWithValue("@PlanCode",    req.PlanCode.Trim());
                    cmd.Parameters.AddWithValue("@ProductCode", req.ProductCode.Trim());
                    await cmd.ExecuteNonQueryAsync();
                }

                // ─────────────────────────────────────────────────────────
                // 8) [MOVED TO CHECKER] Plan-completion + Kanban REQ trigger.
                //
                // Previously fired here — but Kanban raising a material REQ at
                // Maker Start time meant we asked logistics to replenish stores
                // for units that hadn't been QC-authorized yet. If the checker
                // then rejects or reworks some units, the REQ was speculative.
                //
                // The identical logic now lives in SaveCanopyProcessCheckAsync
                // (see "Kanban trigger" block there) with an idempotency guard
                // (`CPYWIPStatus != 'D'`) so multi-PFB / multi-checker sequences
                // fire it exactly once per plan.
                //
                // Kept here — commented — as the reference implementation and
                // an easy rollback point if the business decides to revert.
                /*
                var balAfter = await GetScalarInTxAsync<object>(
                    (SqlConnection)conn, sqlTx,
                    @"SELECT (Qty - CPYWIPQty) AS BalQty
                      FROM   CanopyPlanDetails
                      WHERE  CPCode = @PlanCode AND Partcode = @ProductCode;",
                    ("@PlanCode", req.PlanCode.Trim()),
                    ("@ProductCode", req.ProductCode.Trim()));
                if (balAfter != null && Convert.ToDouble(balAfter) == 0)
                {
                    using (var cmd = new SqlCommand(@"
UPDATE CanopyPlanDetails
   SET CPYWIPStatus = 'D', CPYWOPStatus = 'D'
 WHERE CPCode   = @PlanCode
   AND Partcode = @ProductCode;",
                        (SqlConnection)conn, sqlTx))
                    {
                        cmd.Parameters.AddWithValue("@PlanCode",    req.PlanCode.Trim());
                        cmd.Parameters.AddWithValue("@ProductCode", req.ProductCode.Trim());
                        await cmd.ExecuteNonQueryAsync();
                    }

                    var kbRows = await GetInternalTOCRowsAsync((SqlConnection)conn, sqlTx, pc);
                    if (kbRows.Count > 0)
                    {
                        string kbToPCCodeAct;
                        string kbToPCCode;
                        if (pc == "01.190" || pc == "03.069" || pc == "03.181")
                        {
                            kbToPCCodeAct = "23.001";
                            kbToPCCode    = "23.001";
                        }
                        else if (pc == "28.025" || pc == "28.039" || pc == "28.116")
                        {
                            kbToPCCodeAct = "28.020";
                            kbToPCCode    = "28.020";
                        }
                        else
                        {
                            kbToPCCodeAct = "23.001";
                            kbToPCCode    = "23.001";
                        }

                        var kbReqCode = await GetMaxNoAsync(
                            prefix: "REQ",
                            compCode: company,
                            tblName: "MaterialRequisitionWithOutPlan",
                            tx: sqlTx);
                        var kbMaxSrNo = ExtractSequencePart(kbReqCode);

                        using (var cmd = new SqlCommand(@"
INSERT INTO MaterialRequisitionWithOutPlan
    (REQCode, MaxSrNo, Dt, Yr,
     ProfitCenterCode, ToProfitCenterCode,
     ProfitCenterCode_Act, ToProfitCenterCode_Act,
     ClassCode,
     CompanyCode, ActNo, REQStatus, ReqType, Remark, Discard, Active, Auth,
     SourceCode, RequisitionFor)
VALUES (@REQCode, @MaxSrNo, @Dt, @Yr,
        @PCCode, @ToPCCode,
        @PCCodeAct, @ToPCCodeAct,
        @ProductCode,
        @CompanyCode, @ActNo, 'P', 'WIP', @Remark, 1, 1, 1,
        'KanBan', '0');",
                            (SqlConnection)conn, sqlTx))
                        {
                            cmd.Parameters.AddWithValue("@REQCode",     kbReqCode);
                            cmd.Parameters.AddWithValue("@MaxSrNo",     kbMaxSrNo);
                            cmd.Parameters.AddWithValue("@Dt",          DateTime.Now);
                            cmd.Parameters.AddWithValue("@Yr",          yearEnd ?? string.Empty);
                            cmd.Parameters.AddWithValue("@PCCode",      pcOld);
                            cmd.Parameters.AddWithValue("@ToPCCode",    kbToPCCode);
                            cmd.Parameters.AddWithValue("@PCCodeAct",   pc);
                            cmd.Parameters.AddWithValue("@ToPCCodeAct", kbToPCCodeAct);
                            cmd.Parameters.AddWithValue("@ProductCode", req.ProductCode.Trim());
                            cmd.Parameters.AddWithValue("@CompanyCode", company);
                            cmd.Parameters.AddWithValue("@ActNo",       req.BatchQty.ToString());
                            cmd.Parameters.AddWithValue("@Remark",
                                $"Auto Req For Plan No: {req.ProductCode.Trim()} and Prc No: {prcNo}");
                            await cmd.ExecuteNonQueryAsync();
                        }

                        int kbSr = 0;
                        foreach (var k in kbRows)
                        {
                            kbSr++;
                            using var cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails",
                                (SqlConnection)conn, sqlTx);
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@REQCode",   kbReqCode);
                            cmd.Parameters.AddWithValue("@SrNo",      kbSr);
                            cmd.Parameters.AddWithValue("@PartCode",  k.Partcode);
                            cmd.Parameters.AddWithValue("@Qty",       (double)k.RaiseReqQty);
                            cmd.Parameters.AddWithValue("@REQStatus", "P");
                            await cmd.ExecuteNonQueryAsync();
                        }

                        await InsertLoginTxnAsync((SqlConnection)conn, sqlTx,
                            emp, "S", "MaterialRequisitionWithoutPlan", kbReqCode, company);
                    }
                }
                */
                // ─────────────────────────────────────────────────────────

                // 9) Activity log for the whole Process submission.
                await InsertLoginTxnAsync((SqlConnection)conn, sqlTx,
                    emp, "S", "Canopy Assembly Process", prcNo, company);

                await tx.CommitAsync();
                return new SubmitCanopyProcessResponse
                {
                    Message = $"ProcessCode={prcNo} — Canopy Assembly Started Successfully",
                    PFBCode = prcNo,
                };
            }
            catch (InvalidOperationException)
            {
                try { await tx.RollbackAsync(); } catch { /* already rolled back */ }
                throw;
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch { /* already rolled back */ }
                var inner = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Error saving Canopy Assembly Process (NEW): {inner}", ex);
            }
        }

        // ── 9b) PSH path — closes units for an already-open record ──────
        private async Task<SubmitCanopyProcessResponse> SubmitCanopyProcessPshAsync(
            SubmitCanopyProcessRequest req)
        {
            var pc      = req.PCCode.Trim();
            var company = req.CompanyCode.Trim();
            var emp     = req.EmpCode.Trim();
            var pfb     = req.PFBCode.Trim();

            var conn = _context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            await using var tx = await _context.Database.BeginTransactionAsync();
            var sqlTx = (SqlTransaction)tx.GetDbTransaction();

            try
            {
                // 1) Mark top-N serial numbers complete (EdtD = now).
                using (var cmd = new SqlCommand(@"
UPDATE ProcessFeedbackDetailsSub
   SET EdtD = @Now
 WHERE PfbCode = @PFBCode
   AND EdtD IS NULL
   AND SerialNo IN (
       SELECT TOP (@PrcQty) SerialNo
       FROM   ProcessFeedbackDetailsSub
       WHERE  PfbCode = @PFBCode AND EdtD IS NULL
       ORDER BY SerialNo);",
                    (SqlConnection)conn, sqlTx))
                {
                    cmd.Parameters.AddWithValue("@Now",     DateTime.Now);
                    cmd.Parameters.AddWithValue("@PFBCode", pfb);
                    cmd.Parameters.AddWithValue("@PrcQty",  (int)req.PrcQty);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2) If all units now closed → set ProcessFeedback.EDt = now.
                var totalQty = await GetScalarInTxAsync<object>((SqlConnection)conn, sqlTx,
                    "SELECT ProcessQty FROM ProcessFeedback WHERE PFBCode = @PFBCode;",
                    ("@PFBCode", pfb));
                var closedQty = await GetScalarInTxAsync<object>((SqlConnection)conn, sqlTx,
                    @"SELECT COUNT(*) FROM ProcessFeedbackDetailsSub
                      WHERE PFBCode = @PFBCode AND EdtD IS NOT NULL;",
                    ("@PFBCode", pfb));
                if (totalQty != null && closedQty != null
                    && Convert.ToInt32(totalQty) == Convert.ToInt32(closedQty))
                {
                    using var cmd = new SqlCommand(@"
UPDATE ProcessFeedback SET EDt = @Now WHERE PFBCode = @PFBCode;",
                        (SqlConnection)conn, sqlTx);
                    cmd.Parameters.AddWithValue("@Now",     DateTime.Now);
                    cmd.Parameters.AddWithValue("@PFBCode", pfb);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3) ProductWip receive row (product-level WIP).
                using (var cmd = new SqlCommand(@"
INSERT INTO ProductWip
    (ProductCode, FromPCCode, ToPCCode, IssueCode, IssueDate, IssueQty, StockType)
VALUES (@ProductCode, @PCCode, @PCCode, @PFBCode, @Now, @PrcQty, 0);",
                    (SqlConnection)conn, sqlTx))
                {
                    cmd.Parameters.AddWithValue("@ProductCode", req.ProductCode.Trim());
                    cmd.Parameters.AddWithValue("@PCCode",      pc);
                    cmd.Parameters.AddWithValue("@PFBCode",     pfb);
                    cmd.Parameters.AddWithValue("@Now",         DateTime.Now);
                    cmd.Parameters.AddWithValue("@PrcQty",      req.PrcQty);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4) Attachments — copy each from temp → permanent + link row.
                if (req.Attachments != null && req.Attachments.Count > 0)
                {
                    var tempEmpPath = System.IO.Path.Combine(CanopyProcessTempBase, emp);
                    var permBasePath = CanopyProcessPermanentBase;
                    System.IO.Directory.CreateDirectory(permBasePath);

                    int srA = 0;
                    foreach (var att in req.Attachments)
                    {
                        srA++;
                        if (string.IsNullOrWhiteSpace(att.FileName)) continue;

                        var ext = System.IO.Path.GetExtension(att.FileName);
                        var pfbKey = pfb.Length >= 18 ? pfb.Substring(4, 5) + pfb.Substring(10, 8) : pfb;
                        var savedName = $"{pfbKey}-{srA}{ext}";

                        var srcPath = System.IO.Path.Combine(tempEmpPath, att.FileName);
                        var dstPath = System.IO.Path.Combine(permBasePath, savedName);
                        if (System.IO.File.Exists(srcPath))
                        {
                            try
                            {
                                System.IO.File.Copy(srcPath, dstPath, overwrite: true);
                            }
                            catch { /* file-copy failure shouldn't block the DB write */ }
                        }

                        using var cmd = new SqlCommand(@"
INSERT INTO ProcessFeedbackFiles (GroupPFBCode, SrNo, FileName)
VALUES (@PFBCode, @SrNo, @FileName);",
                            (SqlConnection)conn, sqlTx);
                        cmd.Parameters.AddWithValue("@PFBCode",  pfb);
                        cmd.Parameters.AddWithValue("@SrNo",     srA);
                        cmd.Parameters.AddWithValue("@FileName", savedName);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // 5) Activity log.
                await InsertLoginTxnAsync((SqlConnection)conn, sqlTx,
                    emp, "S", "Canopy Assembly Process", pfb, company);

                await tx.CommitAsync();
                return new SubmitCanopyProcessResponse
                {
                    Message = $"ProcessCode={pfb} — Canopy Assembly End Successfully",
                    PFBCode = pfb,
                };
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch { /* already rolled back */ }
                var inner = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Error saving Canopy Assembly Process (PSH): {inner}", ex);
            }
        }

        // ── Canopy-Process helper methods ────────────────────────────────

        private static (string machine, string serial) SplitMachineSerial(string? machineCodeSrNo)
        {
            var raw = (machineCodeSrNo ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(raw)) return (string.Empty, string.Empty);
            var idx = raw.IndexOf("-->", StringComparison.Ordinal);
            return idx < 0
                ? (raw, string.Empty)
                : (raw.Substring(0, idx).Trim(), raw.Substring(idx + 3).Trim());
        }

        private static CanopyProcessPartRowDto ReadPartRow(SqlDataReader r) => new()
        {
            Part     = SafeStr(r, "Part"),
            KitQty   = SafeDouble(r, "KitQty"),
            PrcQty   = SafeDouble(r, "PrcQty"),
            StkQty   = SafeDouble(r, "StkQty"),
            Wt       = SafeDouble(r, "Wt"),
            TotWt    = SafeDouble(r, "TotWt"),
            Sqft     = SafeDouble(r, "Sqft"),
            TotSqft  = SafeDouble(r, "TotSqft"),
            Rate     = SafeDouble(r, "Rate"),
            PartCode = SafeStr(r, "PartCode"),
        };

        private static CanopyProcessAssemblyKitRowDto ReadAssemblyKitRow(SqlDataReader r) => new()
        {
            Part     = SafeStr(r, "Part"),
            Qty      = SafeDouble(r, "Qty"),
            PrcQty   = SafeDouble(r, "PrcQty"),
            StkQty   = SafeDouble(r, "StkQty"),
            PartCode = SafeStr(r, "PartCode"),
        };

        private static void ValidateCanopyProcessSubmit(SubmitCanopyProcessRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.PCCode))       throw new ArgumentException("PCCode is required.");
            if (string.IsNullOrWhiteSpace(req.CompanyCode))  throw new ArgumentException("CompanyCode is required.");
            if (string.IsNullOrWhiteSpace(req.EmpCode))      throw new ArgumentException("EmpCode is required.");
            if (string.IsNullOrWhiteSpace(req.PFBCode))      throw new ArgumentException("PFBCode is required.");
            if (string.IsNullOrWhiteSpace(req.MachineCodeSrNo)) throw new ArgumentException("MachineCodeSrNo is required.");
            if (string.IsNullOrWhiteSpace(req.PlanCode))     throw new ArgumentException("PlanCode is required.");
            if (string.IsNullOrWhiteSpace(req.ProductCode))  throw new ArgumentException("ProductCode is required.");
            if (string.IsNullOrWhiteSpace(req.BOMCode))      throw new ArgumentException("BOMCode is required.");
            if (req.PrcQty <= 0) throw new ArgumentException("PrcQty must be greater than 0.");
        }

        // Rate/PWt/PSqft lookup at LineWisePC (was hardcoded 01.005 in legacy —
        // switched to the line-wise PC per user request).
        private static async Task<(double rate, double wt, double sqft)> GetProductRateWtSqftAsync(
            SqlConnection conn, SqlTransaction tx, string pc, string partCode)
        {
            using var cmd = new SqlCommand(@"
SELECT TOP 1 ISNULL(Rate, 0) AS Rate, ISNULL(PSqFt, 0) AS PSqFt, ISNULL(PWt, 0) AS PWt
FROM   ProfitcenterPlDetails WITH (NOLOCK)
WHERE  ProfitcenterCode = @PC AND Partcode = @Part;",
                conn, tx);
            cmd.Parameters.AddWithValue("@PC",   pc);
            cmd.Parameters.AddWithValue("@Part", partCode);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return (SafeDouble(reader, "Rate"), SafeDouble(reader, "PWt"), SafeDouble(reader, "PSqFt"));
            return (0, 0, 0);
        }

        // Legacy GetmaxPrc — ProcessFeedback max scan by CompanyCode + Yr.
        // Prefix is passed but always "PSH" for this page.
        private async Task<string> GetProcessFeedbackMaxNoAsync(
            SqlTransaction tx, string prefix, string compCode)
        {
            var yearEnd = await GetYearEndAsync() ?? string.Empty;
            int max = 0;
            using (var cmd = new SqlCommand(@"
SELECT MAX(SUBSTRING(PFBCode, 13, 7)) AS MX
FROM   ProcessFeedback WITH (NOLOCK)
WHERE  Yr = @Yr AND CompanyCode = @CompCode;",
                (SqlConnection)_context.Database.GetDbConnection(), tx))
            {
                cmd.Parameters.AddWithValue("@Yr",       yearEnd);
                cmd.Parameters.AddWithValue("@CompCode", compCode);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value && int.TryParse(result.ToString(), out var n))
                    max = n;
            }
            var next = (max + 1).ToString("D6");
            return $"{prefix}/{yearEnd}/{compCode}{next}";
        }

        private static async Task InsertProcessFeedbackDetailsAsync(
            SqlConnection conn, SqlTransaction tx,
            string pfbCode, int srNo, CanopyProcessPartLine line)
        {
            using var cmd = new SqlCommand(@"
INSERT INTO ProcessFeedbackDetails
    (PFBCode, SrNo, PartCode, KITQty, TotQty, PFBRate, WtPerUt, SqftPerUt)
VALUES (@PFBCode, @SrNo, @PartCode, @KitQty, @TotQty, @Rate, @Wt, @Sqft);",
                conn, tx);
            cmd.Parameters.AddWithValue("@PFBCode",  pfbCode);
            cmd.Parameters.AddWithValue("@SrNo",     srNo);
            cmd.Parameters.AddWithValue("@PartCode", line.PartCode);
            cmd.Parameters.AddWithValue("@KitQty",   line.KitQty);
            cmd.Parameters.AddWithValue("@TotQty",   line.PrcQty);
            cmd.Parameters.AddWithValue("@Rate",     line.Rate);
            cmd.Parameters.AddWithValue("@Wt",       line.Wt);
            cmd.Parameters.AddWithValue("@Sqft",     line.Sqft);
            await cmd.ExecuteNonQueryAsync();
        }

        // Writes both the base (FromProfitCenterCode / ToProfitCenterCode) AND
        // the line-rights (*_Act) column pair so a later filter on either
        // convention sees the issue. The reads on this page filter by _Act,
        // so keeping the base cols in sync is a compat safety net for any
        // downstream page/report that still reads the base columns.
        private static async Task InsertStockWipIssueAsync(
            SqlConnection conn, SqlTransaction tx,
            string pc, string partCode, string issueCode, double issueQty)
        {
            using var cmd = new SqlCommand(@"
INSERT INTO StockWIP
    (FromProfitCenterCode, FromProfitCenterCode_Act,
     ToProfitCenterCode,   ToProfitCenterCode_Act,
     PartCode, IssueCode, IssueDate, IssueQty, StockType)
VALUES (@PC, @PC, @PC, @PC, @PartCode, @IssueCode, @Now, @IssueQty, 0);",
                conn, tx);
            cmd.Parameters.AddWithValue("@PC",        pc);
            cmd.Parameters.AddWithValue("@PartCode",  partCode);
            cmd.Parameters.AddWithValue("@IssueCode", issueCode);
            cmd.Parameters.AddWithValue("@Now",       DateTime.Now);
            cmd.Parameters.AddWithValue("@IssueQty",  issueQty);
            await cmd.ExecuteNonQueryAsync();
        }

        // Kit-Below-Standard-Rate substitution — only fires when the current
        // kit line matches the top-rate part in CanopyplandtsSub. Uses spare-kit
        // warehouse 01.007 for stock lookup (unchanged from legacy).
        private static async Task<string?> ProcessKitBelowStdRateAsync(
            SqlConnection conn, SqlTransaction tx,
            string pc, string prcNo, int srNo,
            string planCode, string productCode,
            CanopyProcessPartLine line, double prcQty)
        {
            // Is this line the top-rate part for the plan?
            string topRatePart;
            using (var cmd = new SqlCommand(@"
SELECT TOP 1 PartCode
FROM   CanopyplandtsSub WITH (NOLOCK)
WHERE  CPCode = @PlanCode AND CpyPartcode = @ProductCode
ORDER BY Rate DESC;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@PlanCode",    planCode);
                cmd.Parameters.AddWithValue("@ProductCode", productCode);
                var scalar = await cmd.ExecuteScalarAsync();
                topRatePart = scalar == null || scalar == DBNull.Value ? string.Empty : scalar.ToString()!.Trim();
            }
            if (!string.Equals(topRatePart, line.PartCode?.Trim(), StringComparison.OrdinalIgnoreCase))
                return null;

            var brRows = new List<(string PartCode, string AliseName, double PurRate, double Rate, double PWt, double PSqft, double Stock)>();
            using (var cmd = new SqlCommand(@"
SELECT Cd.Partcode,
       AliseName,
       ISNULL(pl.PurRate, 0) AS PurRate,
       ISNULL(pl.Rate,    0) AS Rate,
       ISNULL(pl.PWt,     0) AS PWt,
       ISNULL(pl.PSqft,   0) AS PSqft,
       ISNULL((
           SELECT ROUND(ISNULL(SUM(Recqty) - SUM(IssueQty), 0), 0)
           FROM (
               SELECT SUM(ReceivedQty) AS Recqty, 0.0 AS IssueQty
               FROM stockwip
               WHERE ToProfitCenterCode_Act = @PC AND StockType = '0'
                 AND Partcode = Cd.Partcode AND ReceivedQty > 0
               UNION ALL
               SELECT 0.0 AS Recqty, SUM(IssueQty) AS IssueQty
               FROM stockwip
               WHERE FromProfitCenterCode_Act = @PC AND StockType = '0'
                 AND Partcode = Cd.Partcode AND IssueQty > 0
           ) AS stk), 0) AS Stock
FROM   CanopyPlanDtsSubBelowStdRate Cd WITH (NOLOCK)
INNER JOIN Part P WITH (NOLOCK) ON Cd.Partcode = P.PartCode
LEFT JOIN ProfitcenterPLDetails pl WITH (NOLOCK)
       ON pl.PartCode = Cd.Partcode AND pl.ProfitcenterCode = @BrPC
WHERE  CpyPartcode = @ProductCode AND CPCode = @PlanCode;",
                conn, tx))
            {
                cmd.Parameters.AddWithValue("@PC",          pc);
                cmd.Parameters.AddWithValue("@BrPC",        BelowRateStockPC);
                cmd.Parameters.AddWithValue("@ProductCode", productCode);
                cmd.Parameters.AddWithValue("@PlanCode",    planCode);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    brRows.Add((
                        SafeStr(reader, "Partcode"),
                        SafeStr(reader, "AliseName"),
                        SafeDouble(reader, "PurRate"),
                        SafeDouble(reader, "Rate"),
                        SafeDouble(reader, "PWt"),
                        SafeDouble(reader, "PSqft"),
                        SafeDouble(reader, "Stock")));
                }
            }
            if (brRows.Count == 0) return null;

            var shortNames = brRows.Where(r => prcQty > r.Stock).Select(r => r.AliseName).ToList();
            if (shortNames.Count > 0)
                return "Insufficient Stock (BR) For Part: " + string.Join(", ", shortNames);

            // Every alternate has enough → insert them all.
            foreach (var b in brRows)
            {
                await InsertStockWipIssueAsync(conn, tx, pc, b.PartCode, prcNo, prcQty);
                using var cmd = new SqlCommand(@"
INSERT INTO ProcessFeedbackDetails
    (PFBCode, SrNo, PartCode, KITQty, TotQty, PFBRate, SaleRate, WtPerUt, SqftPerUt)
VALUES (@PFBCode, @SrNo, @PartCode, 1, @PrcQty, @PurRate, @Rate, @PWt, @PSqft);",
                    conn, tx);
                cmd.Parameters.AddWithValue("@PFBCode",  prcNo);
                cmd.Parameters.AddWithValue("@SrNo",     srNo);
                cmd.Parameters.AddWithValue("@PartCode", b.PartCode);
                cmd.Parameters.AddWithValue("@PrcQty",   prcQty);
                cmd.Parameters.AddWithValue("@PurRate",  b.PurRate);
                cmd.Parameters.AddWithValue("@Rate",     b.Rate);
                cmd.Parameters.AddWithValue("@PWt",      b.PWt);
                cmd.Parameters.AddWithValue("@PSqft",    b.PSqft);
                await cmd.ExecuteNonQueryAsync();
            }
            return null;
        }

        // Serial pull — U1/U4 use GetCPYSerialNo (from CanopyPlanSerialNo).
        // Bangalore (Company='28') uses GetCPYSerialNo_Bangalore (from upstream
        // ProcessFeedbackDetailsSub) — that variant also returns the source
        // PfbCode so we can flip JobCardStatus='B' on the upstream row.
        private static async Task<List<CanopyProcessSerialRow>> GetCanopyProcessSerialsAsync(
            SqlConnection conn, SqlTransaction tx,
            string productCode, int prcQty, bool bangalore)
        {
            var rows = new List<CanopyProcessSerialRow>();
            var spName = bangalore ? "GetCPYSerialNo_Bangalore" : "GetCPYSerialNo";

            using var cmd = new SqlCommand(spName, conn, tx);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@ProductCode", SqlDbType.NVarChar, 50).Value = productCode;
            cmd.Parameters.Add("@PrcQty",      SqlDbType.Int).Value          = prcQty;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new CanopyProcessSerialRow
                {
                    SerialNo      = SafeStr(reader, "SerialNo"),
                    BFMSrNo       = SafeStr(reader, "BFMSrNo"),
                    FLKSrNo       = SafeStr(reader, "FLKSrNo"),
                    SourcePfbCode = bangalore ? SafeStr(reader, "PFBCode") : string.Empty,
                });
            }
            return rows;
        }

        // Assembly-kit stock read — inline replacement for the polymorphic
        // GetPCKit SP so it works with any LineWisePC (SP hardcodes per-PC
        // branches). Same kit filter as the SP for canopy stages.
        private static async Task<List<AssemblyKitStockRow>> GetAssemblyKitStockAsync(
            SqlConnection conn, SqlTransaction tx, string bomCode, string pc)
        {
            var rows = new List<AssemblyKitStockRow>();
            // SuppRate lives on BOMDetails (matches the legacy GetPCKit SP body
            // — it selects `SuppRate` unqualified from BOMDetails).
            using var cmd = new SqlCommand(@"
SELECT P.PartDesc                                                AS Partdesc,
       Bd.PartCode                                               AS Partcode,
       Bd.Qty                                                    AS Qty,
       ISNULL(Bd.SuppRate, 0)                                    AS SuppRate,
       ISNULL((
           SELECT ROUND(ISNULL(SUM(Recqty) - SUM(IssueQty), 0), 2)
           FROM (
               SELECT SUM(ReceivedQty) AS Recqty, 0.0 AS IssueQty
               FROM stockwip
               WHERE ToProfitCenterCode_Act = @PC AND StockType = '0'
                 AND Partcode = Bd.PartCode AND ReceivedQty > 0
               UNION ALL
               SELECT 0.0 AS Recqty, SUM(IssueQty) AS IssueQty
               FROM stockwip
               WHERE FromProfitCenterCode_Act = @PC AND StockType = '0'
                 AND Partcode = Bd.PartCode AND IssueQty > 0
           ) AS stk), 0)                                          AS Stock
FROM   BOMDetails Bd
INNER JOIN Part P ON Bd.PartCode = P.PartCode
WHERE  Bd.BOMCode = @BOMCode
   AND SUBSTRING(Bd.KitCode, 1, 4) IN ('0121','0122')
   AND SUBSTRING(Bd.KitCode, 12, 2) = '12';",
                conn, tx);
            cmd.Parameters.AddWithValue("@BOMCode", bomCode);
            cmd.Parameters.AddWithValue("@PC",      pc);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new AssemblyKitStockRow
                {
                    PartDesc = SafeStr(reader, "Partdesc"),
                    PartCode = SafeStr(reader, "Partcode"),
                    Qty      = SafeDouble(reader, "Qty"),
                    SuppRate = SafeDouble(reader, "SuppRate"),
                    Stock    = SafeDouble(reader, "Stock"),
                });
            }
            return rows;
        }

        private sealed class CanopyProcessSerialRow
        {
            public string SerialNo      { get; set; } = string.Empty;
            public string BFMSrNo       { get; set; } = string.Empty;
            public string FLKSrNo       { get; set; } = string.Empty;
            public string SourcePfbCode { get; set; } = string.Empty;
        }

        private sealed class AssemblyKitStockRow
        {
            public string PartDesc { get; set; } = string.Empty;
            public string PartCode { get; set; } = string.Empty;
            public double Qty      { get; set; }
            public double SuppRate { get; set; }
            public double Stock    { get; set; }
        }

        // ════════════════════════════════════════════════════════════════
        //  Canopy Assembly Process Checker (quality review side)
        // ════════════════════════════════════════════════════════════════

        // Decision codes we write to ProcessFeedbackDetailsSub.QPCStatus.
        //   Accept -> 'D'   (approved, moves out of pending pool)
        //   Rework -> 'RW'  (needs rework)
        //   Reject -> 'R'   (soft reject in v1)
        private const string CheckerDecisionAccept = "Accept";
        private const string CheckerDecisionRework = "Rework";
        private const string CheckerDecisionReject = "Reject";

        // ── 1) Pending list ─────────────────────────────────────────────
        public async Task<List<CanopyProcessCheckPendingRowDto>> GetCanopyProcessCheckPendingListAsync(
            string pcCode)
        {
            var rows = new List<CanopyProcessCheckPendingRowDto>();
            var pc = (pcCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(pc)) return rows;

            // "Pending" = QPCStatus has NOT been explicitly decided yet
            // (Maker inserts 'OK' by default; checker sets 'D'/'RW'/'R').
            // Returns EVERY PSH record for the line — decided rows stay
            // visible with Status='Authorized' so operators can see
            // per-line throughput at a glance. Pending rows sort first,
            // then most recent Date first.
            const string sql = @"
SELECT pf.PFBCode,
       CONVERT(varchar(19), pf.Dt, 120)                             AS Dt,
       pf.ProductCode                                                AS ProductCode,
       P.PartDesc + '-->' + pf.ProductCode                           AS ProductDesc,
       ISNULL(P.KVA, 0)                                              AS KVA,
       ISNULL(P.Model, '')                                           AS Model,
       ISNULL(pf.ProcessQty, 0)                                      AS BatchQty,
       ISNULL(pf.ProcessQty, 0)                                      AS PrcQty,
       ISNULL(pf.MachineCode, '')                                    AS MachineCode,
       ISNULL(pf.SerialNo, '')                                       AS SerialNo,
       ISNULL(pf.PPWCode, '')                                        AS MakerCode,
       (SELECT COUNT(*) FROM ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
        WHERE pfd.PFBCode = pf.PFBCode)                              AS TotalUnitCount,
       (SELECT COUNT(*) FROM ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
        WHERE pfd.PFBCode = pf.PFBCode
          AND pfd.QPCStatus IN ('D','RW','R'))                        AS DecidedUnitCount,
       (SELECT COUNT(*) FROM ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
        WHERE pfd.PFBCode = pf.PFBCode
          AND (pfd.QPCStatus IS NULL OR pfd.QPCStatus NOT IN ('D','RW','R')))
                                                                     AS PendingUnitCount,
       CASE WHEN (SELECT COUNT(*) FROM ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
                  WHERE pfd.PFBCode = pf.PFBCode
                    AND (pfd.QPCStatus IS NULL OR pfd.QPCStatus NOT IN ('D','RW','R'))) > 0
            THEN 'Pending'
            ELSE 'Authorized'
       END                                                            AS Status
FROM   ProcessFeedback pf  WITH (NOLOCK)
INNER JOIN Part        P   WITH (NOLOCK) ON pf.ProductCode = P.PartCode
WHERE  pf.PCCode_Act = @PC
   AND pf.PFBCode LIKE 'PSH/%'
ORDER BY
    CASE WHEN EXISTS (SELECT 1 FROM ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
                      WHERE pfd.PFBCode = pf.PFBCode
                        AND (pfd.QPCStatus IS NULL OR pfd.QPCStatus NOT IN ('D','RW','R')))
         THEN 0 ELSE 1 END,
    pf.Dt DESC, pf.PFBCode DESC;";

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@PC", SqlDbType.NVarChar, 20).Value = pc;
            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new CanopyProcessCheckPendingRowDto
                {
                    PFBCode          = SafeStr(reader, "PFBCode"),
                    Dt               = SafeStr(reader, "Dt"),
                    ProductCode      = SafeStr(reader, "ProductCode"),
                    ProductDesc      = SafeStr(reader, "ProductDesc"),
                    KVA              = SafeDouble(reader, "KVA"),
                    Model            = SafeStr(reader, "Model"),
                    BatchQty         = SafeDouble(reader, "BatchQty"),
                    PrcQty           = SafeDouble(reader, "PrcQty"),
                    MachineCode      = SafeStr(reader, "MachineCode"),
                    SerialNo         = SafeStr(reader, "SerialNo"),
                    MakerCode        = SafeStr(reader, "MakerCode"),
                    TotalUnitCount   = (int)SafeDecimal(reader, "TotalUnitCount"),
                    DecidedUnitCount = (int)SafeDecimal(reader, "DecidedUnitCount"),
                    PendingUnitCount = (int)SafeDecimal(reader, "PendingUnitCount"),
                    Status           = SafeStr(reader, "Status"),
                });
            }
            return rows;
        }

        // ── 2) Full context for the modal ───────────────────────────────
        public async Task<CanopyProcessCheckContextDto?> GetCanopyProcessCheckContextAsync(string pfbCode)
        {
            var pfb = (pfbCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(pfb)) return null;

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            await connection.OpenAsync();

            // 2a) Header
            CanopyProcessCheckHeaderDto? header = null;
            const string sqlHeader = @"
SELECT pf.PFBCode,
       ISNULL(pf.GroupPFBCode, pf.PFBCode)                           AS GroupPFBCode,
       ISNULL(pf.CanopyPlanCode, '')                                 AS PlanCode,
       CONVERT(varchar(19), pf.Dt, 120)                              AS Dt,
       ISNULL(pf.MachineCode, '')                                    AS MachineCode,
       ISNULL(pf.SerialNo, '')                                       AS SerialNo,
       pf.ProductCode                                                AS ProductCode,
       P.PartDesc + '-->' + pf.ProductCode                           AS ProductDesc,
       ISNULL(pf.TurretKitCode, '')                                  AS BOMCode,
       ISNULL(P.KVA, 0)                                              AS KVA,
       ISNULL(P.Model, '')                                           AS Model,
       ISNULL(pf.ProcessQty, 0)                                      AS BatchQty,
       ISNULL(pf.ProcessQty, 0)                                      AS PrcQty,
       ISNULL(pf.PFBRate, 0)                                         AS Rate,
       ISNULL(pf.WtPerUt, 0)                                         AS WtPerUt,
       ISNULL(pf.SqftPerUt, 0)                                       AS SqftPerUt,
       ISNULL(pf.ProfitCenterCode, '')                               AS PCCode,
       ISNULL(pf.PCCode_Act, '')                                     AS PCCode_Act,
       ISNULL(pf.PPWCode, '')                                        AS MakerCode,
       ISNULL(pf.Remark, '')                                         AS Remark
FROM   ProcessFeedback pf WITH (NOLOCK)
INNER JOIN Part         P WITH (NOLOCK) ON pf.ProductCode = P.PartCode
WHERE  pf.PFBCode = @PFBCode;";
            using (var cmd = new SqlCommand(sqlHeader, connection))
            {
                cmd.Parameters.Add("@PFBCode", SqlDbType.NVarChar, 50).Value = pfb;
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    header = new CanopyProcessCheckHeaderDto
                    {
                        PFBCode      = SafeStr(reader, "PFBCode"),
                        GroupPFBCode = SafeStr(reader, "GroupPFBCode"),
                        PlanCode     = SafeStr(reader, "PlanCode"),
                        Dt           = SafeStr(reader, "Dt"),
                        MachineCode  = SafeStr(reader, "MachineCode"),
                        SerialNo     = SafeStr(reader, "SerialNo"),
                        ProductCode  = SafeStr(reader, "ProductCode"),
                        ProductDesc  = SafeStr(reader, "ProductDesc"),
                        BOMCode      = SafeStr(reader, "BOMCode"),
                        KVA          = SafeDouble(reader, "KVA"),
                        Model        = SafeStr(reader, "Model"),
                        BatchQty     = SafeDouble(reader, "BatchQty"),
                        PrcQty       = SafeDouble(reader, "PrcQty"),
                        Rate         = SafeDouble(reader, "Rate"),
                        WtPerUt      = SafeDouble(reader, "WtPerUt"),
                        SqftPerUt    = SafeDouble(reader, "SqftPerUt"),
                        PCCode       = SafeStr(reader, "PCCode"),
                        PCCode_Act   = SafeStr(reader, "PCCode_Act"),
                        MakerCode    = SafeStr(reader, "MakerCode"),
                        Remark       = SafeStr(reader, "Remark"),
                    };
                }
            }
            if (header == null) return null;

            // 2b) Consumed parts — single unified read of ProcessFeedbackDetails
            // for the PFB. Legacy split kit vs. assembly-kit via a `MOB` column
            // (`MOB='B'` == assembly body), but the migrated Maker path never
            // sets that column, and it may not even exist on newer schemas —
            // so we skip the split. Both DTO buckets get the same rows to
            // preserve the response contract; the UI hides the duplicate
            // "Assembly Kit" panel when both lists are equal.
            var kitLines = new List<CanopyProcessCheckKitLineDto>();
            const string sqlKit = @"
SELECT pfd.SrNo,
       pfd.PartCode,
       ISNULL(P.PartDesc, '')                                        AS PartDesc,
       ISNULL(pfd.KITQty, 0)                                         AS KitQty,
       ISNULL(pfd.TotQty, 0)                                         AS TotQty,
       ISNULL(pfd.PFBRate, 0)                                        AS Rate
FROM   ProcessFeedbackDetails pfd WITH (NOLOCK)
LEFT JOIN Part                P  WITH (NOLOCK) ON pfd.PartCode = P.PartCode
WHERE  pfd.PFBCode = @PFBCode
ORDER BY pfd.SrNo;";
            using (var cmd = new SqlCommand(sqlKit, connection))
            {
                cmd.Parameters.Add("@PFBCode", SqlDbType.NVarChar, 50).Value = pfb;
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    kitLines.Add(new CanopyProcessCheckKitLineDto
                    {
                        SrNo     = (int)SafeDecimal(reader, "SrNo"),
                        PartCode = SafeStr(reader, "PartCode"),
                        PartDesc = SafeStr(reader, "PartDesc"),
                        KitQty   = SafeDouble(reader, "KitQty"),
                        TotQty   = SafeDouble(reader, "TotQty"),
                        Rate     = SafeDouble(reader, "Rate"),
                    });
                }
            }

            // AssemblyKitLines stays empty — the UI's Panel C is hidden when
            // this list is empty, so only the unified Kit panel shows.
            var asslyLines = new List<CanopyProcessCheckKitLineDto>();

            // 2d) Per-unit serial rows.
            var units = new List<CanopyProcessCheckSerialUnitDto>();
            const string sqlUnits = @"
SELECT pfd.SrNo,
       ISNULL(pfd.SerialNo, '')  AS SerialNo,
       ISNULL(pfd.BFMSrNo, '')   AS BFMSrNo,
       ISNULL(pfd.FLKSrNo, '')   AS FLKSrNo,
       ISNULL(pfd.Status, '')    AS Status,
       ISNULL(pfd.QPCStatus, '') AS QPCStatus,
       ISNULL(pfd.RWStatus, '')  AS RWStatus
FROM   ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
WHERE  pfd.PFBCode = @PFBCode
ORDER BY pfd.SrNo;";
            using (var cmd = new SqlCommand(sqlUnits, connection))
            {
                cmd.Parameters.Add("@PFBCode", SqlDbType.NVarChar, 50).Value = pfb;
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    units.Add(new CanopyProcessCheckSerialUnitDto
                    {
                        SrNo      = (int)SafeDecimal(reader, "SrNo"),
                        SerialNo  = SafeStr(reader, "SerialNo"),
                        BFMSrNo   = SafeStr(reader, "BFMSrNo"),
                        FLKSrNo   = SafeStr(reader, "FLKSrNo"),
                        Status    = SafeStr(reader, "Status"),
                        QPCStatus = SafeStr(reader, "QPCStatus"),
                        RWStatus  = SafeStr(reader, "RWStatus"),
                    });
                }
            }

            return new CanopyProcessCheckContextDto
            {
                Header           = header,
                KitLines         = kitLines,
                AssemblyKitLines = asslyLines,
                Units            = units,
            };
        }

        // ── 3) Save per-unit decisions ──────────────────────────────────
        // Soft reject (v1): flip QPCStatus and log the activity. Serials are
        // NOT returned to the pool, WIP is NOT reversed. If Reject semantics
        // need to escalate (hard reject) later, extend this method behind a
        // config flag.
        public async Task<SaveCanopyProcessCheckResponse> SaveCanopyProcessCheckAsync(
            SaveCanopyProcessCheckRequest request)
        {
            ValidateCanopyProcessCheckRequest(request);

            var conn = _context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            await using var tx = await _context.Database.BeginTransactionAsync();
            var sqlTx = (SqlTransaction)tx.GetDbTransaction();

            var pfb = request.PFBCode.Trim();
            var emp = request.EmpCode.Trim();
            var company = request.CompanyCode.Trim();

            int accepted = 0, rework = 0, rejected = 0;

            try
            {
                foreach (var d in request.Decisions)
                {
                    if (string.IsNullOrWhiteSpace(d.SerialNo)) continue;
                    var qpc = MapDecisionToStatus(d.Decision);
                    if (qpc == null) continue;

                    // For Rework we also mark RWStatus='P' so the rework tracker
                    // can pick these up; Accept leaves RWStatus as 'OK'; Reject
                    // sets RWStatus='NA' (v1 — soft reject, not scheduled for RW).
                    string rwStatus = d.Decision switch
                    {
                        CheckerDecisionAccept => "OK",
                        CheckerDecisionRework => "P",
                        CheckerDecisionReject => "NA",
                        _ => "OK",
                    };

                    using (var cmd = new SqlCommand(@"
UPDATE ProcessFeedbackDetailsSub
   SET QPCStatus = @QPC,
       RWStatus  = @RW
 WHERE PFBCode  = @PFBCode
   AND SerialNo = @SerialNo;",
                        (SqlConnection)conn, sqlTx))
                    {
                        cmd.Parameters.AddWithValue("@QPC",      qpc);
                        cmd.Parameters.AddWithValue("@RW",       rwStatus);
                        cmd.Parameters.AddWithValue("@PFBCode",  pfb);
                        cmd.Parameters.AddWithValue("@SerialNo", d.SerialNo.Trim());
                        var affected = await cmd.ExecuteNonQueryAsync();
                        if (affected == 0) continue;
                    }

                    switch (d.Decision)
                    {
                        case CheckerDecisionAccept: accepted++; break;
                        case CheckerDecisionRework: rework++;   break;
                        case CheckerDecisionReject: rejected++; break;
                    }
                }

                // ─── Kanban trigger — moved from Maker (see commented Step 8
                //     block in SubmitCanopyProcessNewAsync). Fires only when
                //     the plan is fully consumed AND hasn't already been
                //     marked done. The CPYWIPStatus='D' flip is the
                //     idempotency guard so multi-PFB / multi-checker
                //     sequences fire the Kanban REQ exactly once per plan.
                //     Only meaningful when at least one Accept happened —
                //     Rework/Reject decisions don't move the plan forward.
                if (accepted > 0)
                {
                    var pc          = request.PCCode.Trim();          // LineWisePC
                    var pcOld       = request.ParentDgPC.Trim();      // ParentDgPC
                    var planCode    = request.PlanCode.Trim();
                    var productCode = request.ProductCode.Trim();

                    // Single read: (remaining balance, current CPYWIPStatus).
                    double planBal = 0;
                    string planCurStatus = string.Empty;
                    using (var cmd = new SqlCommand(@"
SELECT ISNULL(Qty - CPYWIPQty, 0)      AS BalQty,
       ISNULL(CPYWIPStatus,   '')      AS CPYWIPStatus
FROM   CanopyPlanDetails WITH (UPDLOCK, ROWLOCK)
WHERE  CPCode   = @PlanCode
   AND Partcode = @ProductCode;",
                        (SqlConnection)conn, sqlTx))
                    {
                        cmd.Parameters.AddWithValue("@PlanCode",    planCode);
                        cmd.Parameters.AddWithValue("@ProductCode", productCode);
                        using var reader = await cmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            planBal       = SafeDouble(reader, "BalQty");
                            planCurStatus = SafeStr(reader, "CPYWIPStatus");
                        }
                    }

                    if (planBal <= 0 && planCurStatus != "D")
                    {
                        // Flip plan status first — the idempotency guard for
                        // the next checker that touches units on this plan.
                        using (var cmd = new SqlCommand(@"
UPDATE CanopyPlanDetails
   SET CPYWIPStatus = 'D', CPYWOPStatus = 'D'
 WHERE CPCode   = @PlanCode
   AND Partcode = @ProductCode;",
                            (SqlConnection)conn, sqlTx))
                        {
                            cmd.Parameters.AddWithValue("@PlanCode",    planCode);
                            cmd.Parameters.AddWithValue("@ProductCode", productCode);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        var kbRows = await GetInternalTOCRowsAsync(
                            (SqlConnection)conn, sqlTx, pc);
                        if (kbRows.Count > 0)
                        {
                            // Kanban destination mirrors Plan-submit Step 6.
                            string kbToPCCodeAct;
                            string kbToPCCode;
                            if (pc == "01.190" || pc == "03.069" || pc == "03.181")
                            {
                                kbToPCCodeAct = "23.001";
                                kbToPCCode    = "23.001";
                            }
                            else if (pc == "28.025" || pc == "28.039" || pc == "28.116")
                            {
                                kbToPCCodeAct = "28.020";
                                kbToPCCode    = "28.020";
                            }
                            else
                            {
                                kbToPCCodeAct = "23.001";
                                kbToPCCode    = "23.001";
                            }

                            var yearEnd = await GetYearEndAsync() ?? string.Empty;
                            var kbReqCode = await GetMaxNoAsync(
                                prefix: "REQ",
                                compCode: company,
                                tblName: "MaterialRequisitionWithOutPlan",
                                tx: sqlTx);
                            var kbMaxSrNo = ExtractSequencePart(kbReqCode);

                            using (var cmd = new SqlCommand(@"
INSERT INTO MaterialRequisitionWithOutPlan
    (REQCode, MaxSrNo, Dt, Yr,
     ProfitCenterCode, ToProfitCenterCode,
     ProfitCenterCode_Act, ToProfitCenterCode_Act,
     ClassCode,
     CompanyCode, ActNo, REQStatus, ReqType, Remark, Discard, Active, Auth,
     SourceCode, RequisitionFor)
VALUES (@REQCode, @MaxSrNo, @Dt, @Yr,
        @PCCode, @ToPCCode,
        @PCCodeAct, @ToPCCodeAct,
        @ProductCode,
        @CompanyCode, @ActNo, 'P', 'WIP', @Remark, 1, 1, 1,
        'KanBan', '0');",
                                (SqlConnection)conn, sqlTx))
                            {
                                cmd.Parameters.AddWithValue("@REQCode",     kbReqCode);
                                cmd.Parameters.AddWithValue("@MaxSrNo",     kbMaxSrNo);
                                cmd.Parameters.AddWithValue("@Dt",          DateTime.Now);
                                cmd.Parameters.AddWithValue("@Yr",          yearEnd);
                                cmd.Parameters.AddWithValue("@PCCode",      pcOld);         // ParentDgPC -> ProfitCenterCode
                                cmd.Parameters.AddWithValue("@ToPCCode",    kbToPCCode);
                                cmd.Parameters.AddWithValue("@PCCodeAct",   pc);            // LineWisePC -> ProfitCenterCode_Act
                                cmd.Parameters.AddWithValue("@ToPCCodeAct", kbToPCCodeAct);
                                cmd.Parameters.AddWithValue("@ProductCode", productCode);
                                cmd.Parameters.AddWithValue("@CompanyCode", company);
                                cmd.Parameters.AddWithValue("@ActNo",       request.BatchQty.ToString());
                                cmd.Parameters.AddWithValue("@Remark",
                                    $"Auto Req For Plan No: {productCode} and Prc No: {pfb}");
                                await cmd.ExecuteNonQueryAsync();
                            }

                            int kbSr = 0;
                            foreach (var k in kbRows)
                            {
                                kbSr++;
                                using var cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails",
                                    (SqlConnection)conn, sqlTx);
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@REQCode",   kbReqCode);
                                cmd.Parameters.AddWithValue("@SrNo",      kbSr);
                                cmd.Parameters.AddWithValue("@PartCode",  k.Partcode);
                                cmd.Parameters.AddWithValue("@Qty",       (double)k.RaiseReqQty);
                                cmd.Parameters.AddWithValue("@REQStatus", "P");
                                await cmd.ExecuteNonQueryAsync();
                            }

                            await InsertLoginTxnAsync((SqlConnection)conn, sqlTx,
                                emp, "S", "MaterialRequisitionWithoutPlan", kbReqCode, company);
                        }
                    }
                }
                // ─── End Kanban trigger ───

                // Activity log — one row per check submission.
                await InsertLoginTxnAsync((SqlConnection)conn, sqlTx,
                    emp, "S", "Canopy Process Checker", pfb, company);

                await tx.CommitAsync();

                var msg = $"Check saved for {pfb} — Accepted: {accepted}, Rework: {rework}, Rejected: {rejected}.";
                return new SaveCanopyProcessCheckResponse
                {
                    Message       = msg,
                    PFBCode       = pfb,
                    AcceptedCount = accepted,
                    ReworkCount   = rework,
                    RejectedCount = rejected,
                };
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch { /* already rolled back */ }
                var inner = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Error saving Canopy Process check: {inner}", ex);
            }
        }

        // ── 4) Date-range Report (for Excel export) ─────────────────────
        public async Task<List<CanopyProcessCheckReportRowDto>> GetCanopyProcessCheckReportAsync(
            string pcCode, DateTime fromDate, DateTime toDate)
        {
            var rows = new List<CanopyProcessCheckReportRowDto>();
            var pc = (pcCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(pc)) return rows;

            var from = fromDate.Date;
            var to   = toDate.Date.AddDays(1).AddTicks(-1);   // include full end day

            const string sql = @"
SELECT pf.PFBCode,
       CONVERT(varchar(19), pf.Dt, 120)                             AS Dt,
       pf.ProductCode                                                AS ProductCode,
       P.PartDesc + '-->' + pf.ProductCode                           AS ProductDesc,
       ISNULL(P.KVA, 0)                                              AS KVA,
       ISNULL(P.Model, '')                                           AS Model,
       ISNULL(pf.ProcessQty, 0)                                      AS BatchQty,
       ISNULL(pf.ProcessQty, 0)                                      AS PrcQty,
       ISNULL(pf.MachineCode, '')                                    AS MachineCode,
       ISNULL(pf.SerialNo, '')                                       AS SerialNo,
       ISNULL(pf.PPWCode, '')                                        AS MakerCode,
       ISNULL(pf.CanopyPlanCode, '')                                 AS PlanCode,
       ISNULL(pf.TurretKitCode, '')                                  AS BOMCode,
       (SELECT COUNT(*) FROM ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
        WHERE pfd.PFBCode = pf.PFBCode)                              AS TotalUnitCount,
       (SELECT COUNT(*) FROM ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
        WHERE pfd.PFBCode = pf.PFBCode
          AND (pfd.QPCStatus IS NULL OR pfd.QPCStatus NOT IN ('D','RW','R')))
                                                                     AS PendingUnitCount,
       (SELECT COUNT(*) FROM ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
        WHERE pfd.PFBCode = pf.PFBCode AND pfd.QPCStatus = 'D')       AS AcceptedCount,
       (SELECT COUNT(*) FROM ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
        WHERE pfd.PFBCode = pf.PFBCode AND pfd.QPCStatus = 'RW')      AS ReworkCount,
       (SELECT COUNT(*) FROM ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
        WHERE pfd.PFBCode = pf.PFBCode AND pfd.QPCStatus = 'R')       AS RejectedCount,
       (SELECT COUNT(*) FROM ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
        WHERE pfd.PFBCode = pf.PFBCode AND pfd.QPCStatus IN ('D','RW','R'))
                                                                     AS DecidedUnitCount,
       CASE WHEN (SELECT COUNT(*) FROM ProcessFeedbackDetailsSub pfd WITH (NOLOCK)
                  WHERE pfd.PFBCode = pf.PFBCode
                    AND (pfd.QPCStatus IS NULL OR pfd.QPCStatus NOT IN ('D','RW','R'))) > 0
            THEN 'Pending'
            ELSE 'Authorized'
       END                                                            AS Status
FROM   ProcessFeedback pf  WITH (NOLOCK)
INNER JOIN Part        P   WITH (NOLOCK) ON pf.ProductCode = P.PartCode
WHERE  pf.PCCode_Act = @PC
   AND pf.PFBCode LIKE 'PSH/%'
   AND pf.Dt >= @FromDate
   AND pf.Dt <= @ToDate
ORDER BY pf.Dt DESC, pf.PFBCode DESC;";

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@PC",       SqlDbType.NVarChar, 20).Value = pc;
            cmd.Parameters.Add("@FromDate", SqlDbType.DateTime).Value     = from;
            cmd.Parameters.Add("@ToDate",   SqlDbType.DateTime).Value     = to;

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new CanopyProcessCheckReportRowDto
                {
                    PFBCode          = SafeStr(reader, "PFBCode"),
                    Dt               = SafeStr(reader, "Dt"),
                    ProductCode      = SafeStr(reader, "ProductCode"),
                    ProductDesc      = SafeStr(reader, "ProductDesc"),
                    KVA              = SafeDouble(reader, "KVA"),
                    Model            = SafeStr(reader, "Model"),
                    BatchQty         = SafeDouble(reader, "BatchQty"),
                    PrcQty           = SafeDouble(reader, "PrcQty"),
                    MachineCode      = SafeStr(reader, "MachineCode"),
                    SerialNo         = SafeStr(reader, "SerialNo"),
                    MakerCode        = SafeStr(reader, "MakerCode"),
                    PlanCode         = SafeStr(reader, "PlanCode"),
                    BOMCode          = SafeStr(reader, "BOMCode"),
                    TotalUnitCount   = (int)SafeDecimal(reader, "TotalUnitCount"),
                    PendingUnitCount = (int)SafeDecimal(reader, "PendingUnitCount"),
                    AcceptedCount    = (int)SafeDecimal(reader, "AcceptedCount"),
                    ReworkCount      = (int)SafeDecimal(reader, "ReworkCount"),
                    RejectedCount    = (int)SafeDecimal(reader, "RejectedCount"),
                    DecidedUnitCount = (int)SafeDecimal(reader, "DecidedUnitCount"),
                    Status           = SafeStr(reader, "Status"),
                });
            }
            return rows;
        }

        private static string? MapDecisionToStatus(string? decision) => decision switch
        {
            CheckerDecisionAccept => "D",
            CheckerDecisionRework => "RW",
            CheckerDecisionReject => "R",
            _ => null,
        };

        private static void ValidateCanopyProcessCheckRequest(SaveCanopyProcessCheckRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.PFBCode))     throw new ArgumentException("PFBCode is required.");
            if (string.IsNullOrWhiteSpace(req.EmpCode))     throw new ArgumentException("EmpCode is required.");
            if (string.IsNullOrWhiteSpace(req.CompanyCode)) throw new ArgumentException("CompanyCode is required.");
            if (req.Decisions == null || req.Decisions.Count == 0)
                throw new ArgumentException("At least one unit decision is required.");
        }

        // ════════════════════════════════════════════════════════════════
        //  Canopy Plan Checker (plan-authorization side)
        // ════════════════════════════════════════════════════════════════

        // ── 1) Pending / Authorized plan list ──────────────────────────
        public async Task<List<CanopyPlanCheckPendingRowDto>> GetCanopyPlanCheckPendingListAsync(
            string pcCode)
        {
            var rows = new List<CanopyPlanCheckPendingRowDto>();
            var pc = (pcCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(pc)) return rows;

            // Pending rows sort first (by CASE WHEN in ORDER BY), then most
            // recent Dt. Authorized rows stay visible for throughput visibility.
            // Maker code isn't a column on CanopyPlan — sourced from the
            // Plan save's audit row in LoginTransactionDetails.
            // Authorization is tracked via the Checker1 bit column
            // (0 = pending, 1 = authorized) — PlanStatus is ignored per
            // business direction.
            const string sql = @"
SELECT cp.CPCode,
       CONVERT(varchar(19), cp.Dt,     120)                          AS Dt,
       CONVERT(varchar(10), cp.FromDt, 120)                          AS FromDt,
       CONVERT(varchar(10), cp.ToDt,   120)                          AS ToDt,
       ISNULL(cp.PlanPCCode,  '')                                    AS PlanPCCode,
       ISNULL(cp.PlanType,    '')                                    AS PlanType,
       CAST(ISNULL(cp.Checker1, 0) AS varchar(1))                    AS PlanStatus,
       ISNULL((SELECT TOP 1 EmpID FROM LoginTransactionDetails WITH (NOLOCK)
               WHERE TransactionNo = cp.CPCode
                 AND TransactionFrom = 'CanopyPlan'
               ORDER BY TransactionDtTime ASC), '')                  AS MakerCode,
       ISNULL(cp.CompanyCode, '')                                    AS CompanyCode,
       (SELECT COUNT(*) FROM CanopyPlanDetails cpd WITH (NOLOCK)
        WHERE cpd.CPCode = cp.CPCode)                                AS DetailRowCount,
       ISNULL((SELECT SUM(ISNULL(cpd.Qty, 0)) FROM CanopyPlanDetails cpd WITH (NOLOCK)
               WHERE cpd.CPCode = cp.CPCode), 0)                     AS TotalPlanQty,
       -- Aggregate distinct KVAs across the plan's parts using the classic
       -- STUFF+FOR XML pattern (works on SQL Server 2005+ — no STRING_AGG
       -- dependency). Empty when none of the parts carry a KVA.
       ISNULL(STUFF((
              SELECT DISTINCT ', ' + CONVERT(varchar(10), P.KVA)
              FROM   CanopyPlanDetails cpd WITH (NOLOCK)
              INNER JOIN Part P WITH (NOLOCK) ON P.PartCode = cpd.Partcode
              WHERE  cpd.CPCode = cp.CPCode
                AND  P.KVA IS NOT NULL
              FOR XML PATH(''), TYPE
       ).value('.', 'varchar(max)'), 1, 2, ''), '')                  AS KVAs,
       -- Same STUFF+FOR XML pattern for distinct Partcodes.
       ISNULL(STUFF((
              SELECT DISTINCT ', ' + cpd.Partcode
              FROM   CanopyPlanDetails cpd WITH (NOLOCK)
              WHERE  cpd.CPCode = cp.CPCode
                AND  cpd.Partcode IS NOT NULL
                AND  LTRIM(RTRIM(cpd.Partcode)) <> ''
              FOR XML PATH(''), TYPE
       ).value('.', 'varchar(max)'), 1, 2, ''), '')                  AS PartCodes,
       CASE WHEN ISNULL(cp.Checker1, 0) = 1 THEN 'Authorized' ELSE 'Pending' END
                                                                     AS StatusLabel
FROM   CanopyPlan cp WITH (NOLOCK)
WHERE  cp.PCCode_Act = @PC
ORDER BY
    CASE WHEN ISNULL(cp.Checker1, 0) = 1 THEN 1 ELSE 0 END,
    cp.Dt DESC, cp.CPCode DESC;";

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@PC", SqlDbType.NVarChar, 20).Value = pc;
            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new CanopyPlanCheckPendingRowDto
                {
                    CPCode          = SafeStr(reader, "CPCode"),
                    Dt              = SafeStr(reader, "Dt"),
                    FromDt          = SafeStr(reader, "FromDt"),
                    ToDt            = SafeStr(reader, "ToDt"),
                    PlanPCCode      = SafeStr(reader, "PlanPCCode"),
                    PlanType        = SafeStr(reader, "PlanType"),
                    PlanStatus      = SafeStr(reader, "PlanStatus"),
                    MakerCode       = SafeStr(reader, "MakerCode"),
                    CompanyCode     = SafeStr(reader, "CompanyCode"),
                    DetailRowCount  = (int)SafeDecimal(reader, "DetailRowCount"),
                    TotalPlanQty    = SafeDouble(reader, "TotalPlanQty"),
                    KVAs            = SafeStr(reader, "KVAs"),
                    PartCodes       = SafeStr(reader, "PartCodes"),
                    Status          = SafeStr(reader, "StatusLabel"),
                });
            }
            return rows;
        }

        // ── 2) Full plan context (header + details) ────────────────────
        public async Task<CanopyPlanCheckContextDto?> GetCanopyPlanCheckContextAsync(string cpCode)
        {
            var cp = (cpCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(cp)) return null;

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            await connection.OpenAsync();

            // Header
            CanopyPlanCheckHeaderDto? header = null;
            const string sqlHeader = @"
SELECT cp.CPCode,
       CONVERT(varchar(19), cp.Dt,     120) AS Dt,
       CONVERT(varchar(10), cp.FromDt, 120) AS FromDt,
       CONVERT(varchar(10), cp.ToDt,   120) AS ToDt,
       ISNULL(cp.PlanPCCode,  '')           AS PlanPCCode,
       ISNULL(cp.PCCode_Act,  '')           AS PCCode_Act,
       ISNULL(cp.CompanyCode, '')           AS CompanyCode,
       ISNULL(cp.PlanType,    '')           AS PlanType,
       CAST(ISNULL(cp.Checker1, 0) AS varchar(1)) AS PlanStatus,
       ISNULL((SELECT TOP 1 EmpID FROM LoginTransactionDetails WITH (NOLOCK)
               WHERE TransactionNo = cp.CPCode
                 AND TransactionFrom = 'CanopyPlan'
               ORDER BY TransactionDtTime ASC), '')
                                            AS MakerCode,
       ISNULL(cp.Yr,          '')           AS Yr
FROM   CanopyPlan cp WITH (NOLOCK)
WHERE  cp.CPCode = @CPCode;";
            using (var cmd = new SqlCommand(sqlHeader, connection))
            {
                cmd.Parameters.Add("@CPCode", SqlDbType.NVarChar, 50).Value = cp;
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    header = new CanopyPlanCheckHeaderDto
                    {
                        CPCode      = SafeStr(reader, "CPCode"),
                        Dt          = SafeStr(reader, "Dt"),
                        FromDt      = SafeStr(reader, "FromDt"),
                        ToDt        = SafeStr(reader, "ToDt"),
                        PlanPCCode  = SafeStr(reader, "PlanPCCode"),
                        PCCode_Act  = SafeStr(reader, "PCCode_Act"),
                        CompanyCode = SafeStr(reader, "CompanyCode"),
                        PlanType    = SafeStr(reader, "PlanType"),
                        PlanStatus  = SafeStr(reader, "PlanStatus"),
                        MakerCode   = SafeStr(reader, "MakerCode"),
                        Yr          = SafeStr(reader, "Yr"),
                    };
                }
            }
            if (header == null) return null;

            // Details
            var details = new List<CanopyPlanCheckDetailRowDto>();
            const string sqlDetails = @"
SELECT ISNULL(cpd.SrNo, 0)                     AS SrNo,
       CONVERT(varchar(10), cpd.Dt, 120)       AS Dt,
       ISNULL(cpd.Partcode,    '')             AS Partcode,
       ISNULL(p.PartDesc,      '')             AS PartDesc,
       ISNULL(cpd.BomCode,     '')             AS BomCode,
       ISNULL(cpd.PartCodeWOP, '')             AS PartCodeWOP,
       ISNULL(cpd.Qty,          0)             AS Qty,
       ISNULL(cpd.CPYWIPQty,    0)             AS CpyWIPQty,
       ISNULL(cpd.CPYWOPQty,    0)             AS CpyWOPQty,
       ISNULL(cpd.CPYWIPStatus,'')             AS CpyWIPStatus,
       ISNULL(cpd.CPYWOPStatus,'')             AS CpyWOPStatus
FROM   CanopyPlanDetails cpd WITH (NOLOCK)
LEFT   JOIN Part            p   WITH (NOLOCK) ON p.PartCode = cpd.Partcode
WHERE  cpd.CPCode = @CPCode
ORDER BY cpd.SrNo;";
            using (var cmd = new SqlCommand(sqlDetails, connection))
            {
                cmd.Parameters.Add("@CPCode", SqlDbType.NVarChar, 50).Value = cp;
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    details.Add(new CanopyPlanCheckDetailRowDto
                    {
                        SrNo         = (int)SafeDecimal(reader, "SrNo"),
                        Dt           = SafeStr(reader, "Dt"),
                        Partcode     = SafeStr(reader, "Partcode"),
                        PartDesc     = SafeStr(reader, "PartDesc"),
                        BomCode      = SafeStr(reader, "BomCode"),
                        PartCodeWOP  = SafeStr(reader, "PartCodeWOP"),
                        Qty          = SafeDouble(reader, "Qty"),
                        CpyWIPQty    = SafeDouble(reader, "CpyWIPQty"),
                        CpyWOPQty    = SafeDouble(reader, "CpyWOPQty"),
                        CpyWIPStatus = SafeStr(reader, "CpyWIPStatus"),
                        CpyWOPStatus = SafeStr(reader, "CpyWOPStatus"),
                    });
                }
            }

            return new CanopyPlanCheckContextDto
            {
                Header  = header,
                Details = details,
            };
        }

        // ── 3) Save checker's decision ─────────────────────────────────
        // v1 handles Accept only — flips CanopyPlan.Checker1 0 -> 1 and, on the
        // fresh 0->1 transition, fires the Logistics-Kit + Wiring-Harness REQs
        // for every plan detail row (Steps 6 & 11 — moved here from the Maker's
        // SubmitCanopyPlanAsync so REQs only exist once QC has authorized the
        // plan). Rework / Reject values are accepted at the DTO level for
        // future use but no DB state changes for them today.
        public async Task<SaveCanopyPlanCheckResponse> SaveCanopyPlanCheckAsync(
            SaveCanopyPlanCheckRequest request)
        {
            ValidateCanopyPlanCheckRequest(request);

            var conn = _context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            await using var tx = await _context.Database.BeginTransactionAsync();
            var sqlTx = (SqlTransaction)tx.GetDbTransaction();

            var cp       = request.CPCode.Trim();
            var emp      = request.EmpCode.Trim();
            var company  = request.CompanyCode.Trim();
            var pc       = request.PCCode.Trim();        // LineWisePC
            var pcOld    = request.ParentDgPC.Trim();    // ParentDgPC
            var decision = (request.Decision ?? "Accept").Trim();

            try
            {
                string finalStatus = "0";

                if (string.Equals(decision, "Accept", StringComparison.OrdinalIgnoreCase))
                {
                    // Idempotency guard — only flip if Checker1 is currently 0.
                    // @@ROWCOUNT tells us whether we actually transitioned.
                    int affectedRows;
                    using (var cmd = new SqlCommand(@"
UPDATE CanopyPlan
   SET Checker1 = 1
 WHERE CPCode = @CPCode
   AND ISNULL(Checker1, 0) = 0;",
                        (SqlConnection)conn, sqlTx))
                    {
                        cmd.Parameters.AddWithValue("@CPCode", cp);
                        affectedRows = await cmd.ExecuteNonQueryAsync();
                    }
                    finalStatus = "1";

                    // Fire Steps 6 & 11 only on the fresh 0->1 transition.
                    // If the plan was already authorized, the UPDATE returns 0
                    // and we skip — no duplicate REQs.
                    if (affectedRows > 0)
                    {
                        await FirePlanCheckerAutoReqsAsync(
                            (SqlConnection)conn, sqlTx, cp, pc, pcOld, company, emp);
                    }
                }
                else
                {
                    // Rework / Reject — reserved for future. Read current
                    // status back so the response is truthful.
                    finalStatus = await ExecuteScalarStringAsync(
                        (SqlConnection)conn,
                        "SELECT CAST(ISNULL(Checker1, 0) AS varchar(1)) FROM CanopyPlan WHERE CPCode = @CPCode;",
                        ("@CPCode", cp));
                }

                await InsertLoginTxnAsync((SqlConnection)conn, sqlTx,
                    emp, "S", "Canopy Plan Checker", cp, company);

                await tx.CommitAsync();

                return new SaveCanopyPlanCheckResponse
                {
                    Message    = $"Plan {cp} — {decision} recorded (Checker1='{finalStatus}').",
                    CPCode     = cp,
                    PlanStatus = finalStatus,
                };
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch { /* already rolled back */ }
                var inner = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Error saving Canopy Plan check: {inner}", ex);
            }
        }

        // Steps 6 & 11 from SubmitCanopyPlanAsync — moved here so the auto-REQs
        // (Logistics-Kit + Wiring-Harness) only fire once QC has authorized
        // the plan. Loops through every CanopyPlanDetails row for the plan.
        private async Task FirePlanCheckerAutoReqsAsync(
            SqlConnection conn, SqlTransaction sqlTx,
            string cpCode, string pc, string pcOld, string company, string emp)
        {
            // PC mapping — matches Plan-submit's Step 6 driver.
            string profitCenterCodeAct;
            string toprofitCenterCode;
            if (pc == "01.190" || pc == "03.069" || pc == "03.181")
            {
                profitCenterCodeAct = "23.001";
                toprofitCenterCode  = "23.001";
            }
            else if (pc == "28.025" || pc == "28.039" || pc == "28.116")
            {
                profitCenterCodeAct = "28.020";
                toprofitCenterCode  = "28.020";
            }
            else
            {
                profitCenterCodeAct = "23.001";
                toprofitCenterCode  = "23.001";
            }

            // Same for Wiring-Harness (different team) — Step 11 driver.
            string whProfitCenterCodeAct;
            string whToProfitCenterCode;
            if (pc == "01.190" || pc == "03.069" || pc == "03.181")
            {
                whProfitCenterCodeAct = "01.091";
                whToProfitCenterCode  = "01.091";
            }
            else if (pc == "28.025" || pc == "28.039" || pc == "28.116")
            {
                whProfitCenterCodeAct = "28.020";
                whToProfitCenterCode  = "28.020";
            }
            else
            {
                whProfitCenterCodeAct = "01.091";
                whToProfitCenterCode  = "01.091";
            }

            // Load every detail row (Partcode + Qty) for the plan.
            var rows = new List<(string PartCode, double Qty)>();
            using (var cmd = new SqlCommand(@"
SELECT ISNULL(cpd.Partcode, '') AS Partcode,
       ISNULL(cpd.Qty,       0) AS Qty
FROM   CanopyPlanDetails cpd WITH (NOLOCK)
WHERE  cpd.CPCode = @CPCode
ORDER BY cpd.SrNo;",
                conn, sqlTx))
            {
                cmd.Parameters.AddWithValue("@CPCode", cpCode);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rows.Add((SafeStr(reader, "Partcode"), SafeDouble(reader, "Qty")));
                }
            }

            var yearEnd = await GetYearEndAsync() ?? string.Empty;

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.PartCode)) continue;

                // Step 6 — Logistics-Kit REQ
                var logReqCode = await GetMaxNoAsync(
                    prefix: "REQ",
                    compCode: company,
                    tblName: "MaterialRequisitionWithOutPlan",
                    tx: sqlTx);
                var logMaxSrNo = ExtractSequencePart(logReqCode);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC insertMaterialRequisitionWithOutPlanProcessVsPlan " +
                    "@REQCode, @MaxSrNo, @Dt, @Yr, @ProfitCenterCode, @ToProfitCenterCode, " +
                    "@ProfitCenterCode_Act, @ToProfitCenterCode_Act, " +
                    "@ClassCode, @ActNo, @SourceCode, @CompanyCode, " +
                    "@REQStatus, @REQType, @Remark, @Discard, @Active, @Auth",
                    new SqlParameter("@REQCode",                logReqCode),
                    new SqlParameter("@MaxSrNo",                logMaxSrNo),
                    new SqlParameter("@Dt",                     DateTime.Now),
                    new SqlParameter("@Yr",                     yearEnd),
                    new SqlParameter("@ProfitCenterCode",       pcOld),
                    new SqlParameter("@ToProfitCenterCode",     toprofitCenterCode),
                    new SqlParameter("@ProfitCenterCode_Act",   pc),
                    new SqlParameter("@ToProfitCenterCode_Act", profitCenterCodeAct),
                    new SqlParameter("@ClassCode",              row.PartCode),
                    new SqlParameter("@ActNo",                  row.Qty.ToString()),
                    new SqlParameter("@SourceCode",             cpCode),
                    new SqlParameter("@CompanyCode",            company),
                    new SqlParameter("@REQStatus",              "P"),
                    new SqlParameter("@REQType",                "WIP"),
                    new SqlParameter("@Remark",                 $"Auto Req For : {row.PartCode} and Plan No: {cpCode}"),
                    new SqlParameter("@Discard",                1),
                    new SqlParameter("@Active",                 1),
                    new SqlParameter("@Auth",                   1));

                var logKitRows = await GetInternalReqLogisticsKitAsync(
                    conn, sqlTx, row.PartCode, pcCodeStage: 3, requisitionFor: "029");
                int logSr = 0;
                foreach (var k in logKitRows)
                {
                    logSr++;
                    using var cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails",
                        conn, sqlTx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@REQCode",   logReqCode);
                    cmd.Parameters.AddWithValue("@SrNo",      logSr);
                    cmd.Parameters.AddWithValue("@PartCode",  k.PartCode);
                    cmd.Parameters.AddWithValue("@Qty",       k.RaiseReqQty * row.Qty);
                    cmd.Parameters.AddWithValue("@REQStatus", "P");
                    await cmd.ExecuteNonQueryAsync();
                }

                // Step 11 — Wiring-Harness REQ
                var whReqCode = await GetMaxNoAsync(
                    prefix: "REQ",
                    compCode: company,
                    tblName: "MaterialRequisitionWithOutPlan",
                    tx: sqlTx);
                var whMaxSrNo = ExtractSequencePart(whReqCode);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC insertMaterialRequisitionWithOutPlanProcessVsPlan " +
                    "@REQCode, @MaxSrNo, @Dt, @Yr, @ProfitCenterCode, @ToProfitCenterCode, " +
                    "@ProfitCenterCode_Act, @ToProfitCenterCode_Act, " +
                    "@ClassCode, @ActNo, @SourceCode, @CompanyCode, " +
                    "@REQStatus, @REQType, @Remark, @Discard, @Active, @Auth",
                    new SqlParameter("@REQCode",                whReqCode),
                    new SqlParameter("@MaxSrNo",                whMaxSrNo),
                    new SqlParameter("@Dt",                     DateTime.Now),
                    new SqlParameter("@Yr",                     yearEnd),
                    new SqlParameter("@ProfitCenterCode",       pcOld),
                    new SqlParameter("@ToProfitCenterCode",     whToProfitCenterCode),
                    new SqlParameter("@ProfitCenterCode_Act",   pc),
                    new SqlParameter("@ToProfitCenterCode_Act", whProfitCenterCodeAct),
                    new SqlParameter("@ClassCode",              row.PartCode),
                    new SqlParameter("@ActNo",                  row.Qty.ToString()),
                    new SqlParameter("@SourceCode",             cpCode),
                    new SqlParameter("@CompanyCode",            company),
                    new SqlParameter("@REQStatus",              "P"),
                    new SqlParameter("@REQType",                "WIP"),
                    new SqlParameter("@Remark",                 $"Auto Req For : {row.PartCode} and Plan No: {cpCode}"),
                    new SqlParameter("@Discard",                1),
                    new SqlParameter("@Active",                 1),
                    new SqlParameter("@Auth",                   1));

                var whRows = await GetInternalReqWHKitAsync(conn, sqlTx, row.PartCode);
                int whSr = 0;
                foreach (var w in whRows)
                {
                    whSr++;
                    using var cmd = new SqlCommand("insertMaterialRequisitionWithOutPlanDetails",
                        conn, sqlTx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@REQCode",   whReqCode);
                    cmd.Parameters.AddWithValue("@SrNo",      whSr);
                    cmd.Parameters.AddWithValue("@PartCode",  w.PartCode);
                    cmd.Parameters.AddWithValue("@Qty",       w.RaiseReqQty * row.Qty);
                    cmd.Parameters.AddWithValue("@REQStatus", "P");
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // ── 4) Date-range report ────────────────────────────────────────
        public async Task<List<CanopyPlanCheckReportRowDto>> GetCanopyPlanCheckReportAsync(
            string pcCode, DateTime fromDate, DateTime toDate)
        {
            var rows = new List<CanopyPlanCheckReportRowDto>();
            var pc = (pcCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(pc)) return rows;

            var from = fromDate.Date;
            var to   = toDate.Date.AddDays(1).AddTicks(-1);

            const string sql = @"
SELECT cp.CPCode,
       CONVERT(varchar(19), cp.Dt,     120)                          AS Dt,
       CONVERT(varchar(10), cp.FromDt, 120)                          AS FromDt,
       CONVERT(varchar(10), cp.ToDt,   120)                          AS ToDt,
       ISNULL(cp.PlanPCCode,  '')                                    AS PlanPCCode,
       ISNULL(cp.PlanType,    '')                                    AS PlanType,
       CAST(ISNULL(cp.Checker1, 0) AS varchar(1))                    AS PlanStatus,
       ISNULL((SELECT TOP 1 EmpID FROM LoginTransactionDetails WITH (NOLOCK)
               WHERE TransactionNo = cp.CPCode
                 AND TransactionFrom = 'CanopyPlan'
               ORDER BY TransactionDtTime ASC), '')                  AS MakerCode,
       ISNULL(cp.CompanyCode, '')                                    AS CompanyCode,
       (SELECT COUNT(*) FROM CanopyPlanDetails cpd WITH (NOLOCK)
        WHERE cpd.CPCode = cp.CPCode)                                AS DetailRowCount,
       ISNULL((SELECT SUM(ISNULL(cpd.Qty, 0)) FROM CanopyPlanDetails cpd WITH (NOLOCK)
               WHERE cpd.CPCode = cp.CPCode), 0)                     AS TotalPlanQty,
       -- Aggregate distinct KVAs across the plan's parts using the classic
       -- STUFF+FOR XML pattern (works on SQL Server 2005+ — no STRING_AGG
       -- dependency). Empty when none of the parts carry a KVA.
       ISNULL(STUFF((
              SELECT DISTINCT ', ' + CONVERT(varchar(10), P.KVA)
              FROM   CanopyPlanDetails cpd WITH (NOLOCK)
              INNER JOIN Part P WITH (NOLOCK) ON P.PartCode = cpd.Partcode
              WHERE  cpd.CPCode = cp.CPCode
                AND  P.KVA IS NOT NULL
              FOR XML PATH(''), TYPE
       ).value('.', 'varchar(max)'), 1, 2, ''), '')                  AS KVAs,
       -- Same STUFF+FOR XML pattern for distinct Partcodes.
       ISNULL(STUFF((
              SELECT DISTINCT ', ' + cpd.Partcode
              FROM   CanopyPlanDetails cpd WITH (NOLOCK)
              WHERE  cpd.CPCode = cp.CPCode
                AND  cpd.Partcode IS NOT NULL
                AND  LTRIM(RTRIM(cpd.Partcode)) <> ''
              FOR XML PATH(''), TYPE
       ).value('.', 'varchar(max)'), 1, 2, ''), '')                  AS PartCodes,
       ISNULL((SELECT SUM(ISNULL(cpd.CPYWIPQty, 0)) FROM CanopyPlanDetails cpd WITH (NOLOCK)
               WHERE cpd.CPCode = cp.CPCode), 0)                     AS TotalWIPQty,
       CASE WHEN ISNULL(cp.Checker1, 0) = 1 THEN 'Authorized' ELSE 'Pending' END
                                                                     AS StatusLabel
FROM   CanopyPlan cp WITH (NOLOCK)
WHERE  cp.PCCode_Act = @PC
   AND cp.Dt >= @FromDt
   AND cp.Dt <= @ToDt
ORDER BY cp.Dt DESC, cp.CPCode DESC;";

            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@PC",     SqlDbType.NVarChar, 20).Value = pc;
            cmd.Parameters.Add("@FromDt", SqlDbType.DateTime).Value     = from;
            cmd.Parameters.Add("@ToDt",   SqlDbType.DateTime).Value     = to;
            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new CanopyPlanCheckReportRowDto
                {
                    CPCode         = SafeStr(reader, "CPCode"),
                    Dt             = SafeStr(reader, "Dt"),
                    FromDt         = SafeStr(reader, "FromDt"),
                    ToDt           = SafeStr(reader, "ToDt"),
                    PlanPCCode     = SafeStr(reader, "PlanPCCode"),
                    PlanType       = SafeStr(reader, "PlanType"),
                    PlanStatus     = SafeStr(reader, "PlanStatus"),
                    MakerCode      = SafeStr(reader, "MakerCode"),
                    CompanyCode    = SafeStr(reader, "CompanyCode"),
                    DetailRowCount = (int)SafeDecimal(reader, "DetailRowCount"),
                    TotalPlanQty   = SafeDouble(reader, "TotalPlanQty"),
                    TotalWIPQty    = SafeDouble(reader, "TotalWIPQty"),
                    KVAs           = SafeStr(reader, "KVAs"),
                    PartCodes      = SafeStr(reader, "PartCodes"),
                    Status         = SafeStr(reader, "StatusLabel"),
                });
            }
            return rows;
        }

        private static void ValidateCanopyPlanCheckRequest(SaveCanopyPlanCheckRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.CPCode))      throw new ArgumentException("CPCode is required.");
            if (string.IsNullOrWhiteSpace(req.EmpCode))     throw new ArgumentException("EmpCode is required.");
            if (string.IsNullOrWhiteSpace(req.CompanyCode)) throw new ArgumentException("CompanyCode is required.");
        }
    }
}
