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

        // Three "production-line" PCs that the legacy page silently locks the
        // grid + save flow to. Other PCs render nothing.
        private static readonly HashSet<string> AllowedProductionPCs = new(StringComparer.OrdinalIgnoreCase)
        {
            "01.005", "03.038", "01.093",
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
        // Replaces legacy BindDDLCanopyPartDesc() — UNION ALL of two sources,
        // both filtered to canopy parts (PartCode LIKE '4%').
        public async Task<List<FlatPackCanopyOptionDto>> GetFlatPackCanopyOptionsAsync()
        {
            const string sql = @"
SELECT DISTINCT Pt.Partcode,
       Partdesc + '-->' + Pt.Partcode AS Partdesc,
       KVA, Model, Phase, Type
FROM PartToCDetailsSupplier Pt WITH (NOLOCK)
INNER JOIN Part P WITH (NOLOCK) ON Pt.partcode = P.Partcode
WHERE Pt.CompanyCode IN ('01','03')
  AND Pt.TMatType = 'REQ'
  AND Pt.ForPCCode IN ('01.005','01.004','03.051')
  AND Pt.SuppCode IN ('23.001','01.005')
  AND Pt.active = '1'
  AND P.Active = '1'
  AND P.Discard = '1'
  AND Pt.POper > 0
  AND Pt.partcode LIKE '4%'
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
  AND Pt.DtValidity >= GETDATE();";

            var results = new List<FlatPackCanopyOptionDto>();
            using var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var command = new SqlCommand(sql, connection);
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

            var pc       = req.PCCode.Trim();
            var company  = req.CompanyCode.Trim();
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
                    cmd.Parameters.AddWithValue("@ProfitCenterCode", pc);
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

                // 3) Patch CR/HR fields onto the master row.
                using (var cmd = new SqlCommand(
                    @"UPDATE ProcessFeedBack SET CRWt = @CRWt, HRWt = @HRWt, CRRate = @CRRate, HRRate = @HRRate
                      WHERE PFBCode = @PFBCode;", (SqlConnection)conn, sqlTx))
                {
                    cmd.Parameters.AddWithValue("@CRWt",    req.CRWt);
                    cmd.Parameters.AddWithValue("@HRWt",    req.HRWt);
                    cmd.Parameters.AddWithValue("@CRRate",  req.CRRate);
                    cmd.Parameters.AddWithValue("@HRRate",  req.HRRate);
                    cmd.Parameters.AddWithValue("@PFBCode", pfbCode);
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

                    using (var cmd = new SqlCommand(
                        @"INSERT INTO StockWIP (FromProfitCenterCode, PartCode, IssueCode, IssueDate, IssueQty, ToProfitCenterCode, StockType)
                          VALUES (@FromPC, @PartCode, @IssueCode, @IssueDate, @IssueQty, @ToPC, @StockType);",
                        (SqlConnection)conn, sqlTx))
                    {
                        cmd.Parameters.AddWithValue("@FromPC",    pc);
                        cmd.Parameters.AddWithValue("@PartCode",  rowPartCode);
                        cmd.Parameters.AddWithValue("@IssueCode", pfbCode);
                        cmd.Parameters.AddWithValue("@IssueDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@IssueQty",  row.TotalQty);
                        cmd.Parameters.AddWithValue("@ToPC",      pc);   // To == From in legacy
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

                // 7) Kanban auto-REQ block (PC = 01.093 only).
                if (pc.Equals(KanbanPC, StringComparison.OrdinalIgnoreCase))
                {
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

                        using (var cmd = new SqlCommand(
                            @"INSERT INTO MaterialRequisitionWithOutPlan
                              (REQCode, MaxSrNo, Dt, Yr, ProfitCenterCode, ToProfitCenterCode, ClassCode,
                               CompanyCode, ActNo, REQStatus, ReqType, Remark, Discard, Active, Auth, SourceCode)
                              VALUES
                              (@REQCode, @MaxSrNo, @Dt, @Yr, @PC, '23.001', @ClassCode, @CompCode, @ActNo,
                               'P', 'WIP', @Remark, '1', '1', '1', 'KanBan');",
                            (SqlConnection)conn, sqlTx))
                        {
                            cmd.Parameters.AddWithValue("@REQCode",   reqCode);
                            cmd.Parameters.AddWithValue("@MaxSrNo",   maxSrNoReq);
                            cmd.Parameters.AddWithValue("@Dt",        DateTime.Now);
                            cmd.Parameters.AddWithValue("@Yr",        await GetYearEndAsync());
                            cmd.Parameters.AddWithValue("@PC",        pc);
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
            using var cmd = new SqlCommand("InternalTOCReq", conn, tx);
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

                    // 3b) Logistics-Kit REQ (uses InternalReqLogisticsKit @PartCode, 3, '029')
                    var reqCompCode = company;
                    var logReqCode = await GetMaxNoAsync(
                        prefix: "REQ",
                        compCode: reqCompCode,
                        tblName: "MaterialRequisitionWithOutPlan",
                        tx: sqlTx);
                    var logMaxSrNo = ExtractSequencePart(logReqCode);

                    // Step 6: master REQ via SP (matches SubmitJobCardAsync pattern).
                    // Writes the new PCCode_Act / ToPCCode_Act columns:
                    //   @ProfitCenterCode      = pcOld          (ParentDgPC — old PC)
                    //   @ProfitCenterCode_Act  = pc             (LineWisePC — active PC)
                    //   @ToProfitCenterCode    = toprofitCenterCode
                    //   @ToProfitCenterCode_Act = profitCenterCodeAct
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

                    // 3c) Wiring-Harness REQ (uses InternalReqLogisticsdetailsWHKIT_Canopy)
                    var whReqCode = await GetMaxNoAsync(
                        prefix: "REQ",
                        compCode: reqCompCode,
                        tblName: "MaterialRequisitionWithOutPlan",
                        tx: sqlTx);
                    var whMaxSrNo = ExtractSequencePart(whReqCode);

                    // Step 11: master REQ via SP (matches Step 6 pattern for WH team).
                    // Writes the new PCCode_Act / ToPCCode_Act columns:
                    //   @ProfitCenterCode       = pcOld                    (ParentDgPC — old PC)
                    //   @ProfitCenterCode_Act   = pc                       (LineWisePC — active PC)
                    //   @ToProfitCenterCode     = whToProfitCenterCode     (Wiring-Harness team)
                    //   @ToProfitCenterCode_Act = whProfitCenterCodeAct    (same routing)
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
    }
}
