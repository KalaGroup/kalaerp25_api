using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.ControlPanelBox;
using KalaGenset.ERP.Core.ResponseDTO.ControlPanelBox;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KalaGenset.ERP.Core.Services
{
    public class ControlPanelBoxService : IControlPanelBox
    {
        private readonly KalaDbContext _context;

        public ControlPanelBoxService(KalaDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<ControlPanelBoxPlanRowDto>> GetPlanRowsByKvaAsync(string kva)
        {
            var rows = new List<ControlPanelBoxPlanRowDto>();
            var kvaValue = (kva ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(kvaValue)) return rows;

            // Inline SQL — verbatim from the query supplied by the operator team,
            // with WITH (NOLOCK) hints added (codebase convention for read paths)
            // and the KVA literal converted into a parameter (@Kva) to avoid
            // injection. Everything else — column set, JOINs, filters, GROUP BY,
            // ORDER BY, TOP 25 — is preserved exactly.
            const string sql = @"
SELECT TOP 25
    MAX(B.BOMCode)                       AS BOMCode,
    P.PartDesc + '-->' + BD.KitCode      AS PartDesc,
    BD.KitCode,
    U.UName
FROM        BOM        B  WITH (NOLOCK)
INNER JOIN  BOMDetails BD WITH (NOLOCK) ON B.BOMCode  = BD.BOMCode
INNER JOIN  Part       P  WITH (NOLOCK) ON BD.KitCode = P.PartCode
INNER JOIN  Uom        U  WITH (NOLOCK) ON P.UomCode  = U.Uid
WHERE B.Active   = '1'
  AND B.Discard  = '1'
  AND B.Auth     = '1'
  AND BD.MOB     = 'M'
  AND P.Active   = '1'
  AND P.Discard  = '1'
  AND P.MOB      = 'M'
  AND BD.KitCode LIKE '003%'
  AND B.CompanyCode = '01'
  AND KVA = @Kva
GROUP BY BD.KitCode, P.PartDesc, U.UName
ORDER BY P.PartDesc;";

            using var conn = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Kva", SqlDbType.NVarChar, 20).Value = kvaValue;

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new ControlPanelBoxPlanRowDto
                {
                    BOMCode  = SafeStr(reader, "BOMCode"),
                    PartDesc = SafeStr(reader, "PartDesc"),
                    KitCode  = SafeStr(reader, "KitCode"),
                    UName    = SafeStr(reader, "UName"),
                });
            }
            return rows;
        }

        // ─────────────────────────────────────────────────────────────
        //  SubmitPlan — Step 1: header-only insert (CanopyPlan)
        //  Mirrors the legacy setSave block:
        //    getMaxNo → UPDATE GetMaxCode → InsertCanopyPlan_Maker_Checker
        //  All in one transaction — rollback on any failure.
        //  Details insert (CanopyPlanDetails per Row) is Step 2 — the rows
        //  field on the request is accepted but not yet consumed here.
        // ─────────────────────────────────────────────────────────────

        // Hardcoded per the current spec — Control Panel Plan is fixed to
        // this line + company; the client is not allowed to override.
        private const string ControlPanelPlanPCCode    = "01.041";
        private const string ControlPanelPlanPCCodeAct = "01.041";
        private const string ControlPanelPlanCompCode  = "01";
        private const string ControlPanelPlanType      = "M";     // Manual
        private const string ControlPanelPlanAutoFlg   = "No";    // Manual
        private const int    ControlPanelPlanChecker1  = 0;       // 0 = pending

        /// <inheritdoc />
        public async Task<SubmitControlPanelBoxPlanResponse> SubmitPlanAsync(
            SubmitControlPanelBoxPlanRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var conn = _context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var sqlTx = (SqlTransaction)tx.GetDbTransaction();

                // 1) Reserve next CPCode (read current GetMaxCode → +1 → persist
                //    → return the formatted code, all inside our transaction).
                var cpCode  = await GetMaxNoAsync("CPY", ControlPanelPlanCompCode,
                                                  "CanopyPlan", sqlTx);
                // MaxSrNo = the part after the last slash, e.g. "01000042".
                var maxSrNo = ExtractSequencePart(cpCode);
                var yearEnd = await GetYearEndAsync();

                // 2) InsertCanopyPlan_Maker_Checker — header row.
                using (var cmd = new SqlCommand("InsertCanopyPlan_Maker_Checker",
                                                 (SqlConnection)conn, sqlTx))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CPCode",      cpCode);
                    cmd.Parameters.AddWithValue("@Dt",          DateTime.Now);
                    cmd.Parameters.AddWithValue("@MaxSrNo",     maxSrNo);
                    cmd.Parameters.AddWithValue("@Yr",          yearEnd ?? string.Empty);
                    cmd.Parameters.AddWithValue("@FromDt",      request.FromDt.Date);
                    cmd.Parameters.AddWithValue("@ToDt",        request.ToDt.Date);
                    cmd.Parameters.AddWithValue("@PlanPCCode",  ControlPanelPlanPCCode);
                    cmd.Parameters.AddWithValue("@CompanyCode", ControlPanelPlanCompCode);
                    cmd.Parameters.AddWithValue("@PlanType",    ControlPanelPlanType);
                    cmd.Parameters.AddWithValue("@AutoFlg",     ControlPanelPlanAutoFlg);
                    cmd.Parameters.AddWithValue("@PCCode_Act",  ControlPanelPlanPCCodeAct);
                    cmd.Parameters.AddWithValue("@Checker1",    ControlPanelPlanChecker1);
                    await cmd.ExecuteNonQueryAsync();
                }

                // TODO Step 2: for each row in request.Rows, INSERT INTO
                // CanopyPlanDetails (CPCode = cpCode, PartCode = row.PartCode,
                // BOMCode, Qty, Dt, PartCodeWOP, CpyWIPQty, etc.).

                await tx.CommitAsync();
                return new SubmitControlPanelBoxPlanResponse
                {
                    Message = $"Plan Saved Successfully — Plan Code : {cpCode}",
                    CPCode  = cpCode,
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  Helpers — copies of the versions in CanopyAssemblyServices.
        //  Kept private so this service is self-contained; no cross-service
        //  dep. If a shared utility service is introduced later, these three
        //  can move there.
        // ─────────────────────────────────────────────────────────────

        // Reads current MaxValue from GetMaxCode, increments by 1, persists
        // the bump inside the caller's transaction, and returns the full
        // formatted code (e.g. "CPY/26-27/01000042").
        private async Task<string> GetMaxNoAsync(string prefix, string compCode,
                                                  string tblName, SqlTransaction tx)
        {
            var yearEnd = await GetYearEndAsync();
            int intmax  = 0;

            const string readSql = @"SELECT ISNULL(MaxValue, 0) AS MXNO FROM GetMaxCode
                                     WHERE TblName = @TableName AND CompCode = @CompCode
                                       AND Prefix  = @Prefix    AND Yr       = @YearEnd;";
            using (var cmd = new SqlCommand(readSql,
                                             (SqlConnection)_context.Database.GetDbConnection(), tx))
            {
                cmd.Parameters.AddWithValue("@TableName", tblName);
                cmd.Parameters.AddWithValue("@CompCode",  compCode);
                cmd.Parameters.AddWithValue("@Prefix",    prefix);
                cmd.Parameters.AddWithValue("@YearEnd",   yearEnd ?? string.Empty);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    intmax = Convert.ToInt32(result);
            }

            var strmax  = (intmax + 1).ToString("D6");
            var newCode = $"{prefix}/{yearEnd}/{compCode}{strmax}";

            const string updSql = @"UPDATE GetMaxCode SET MaxValue = @MaxValue
                                     WHERE TblName = @TableName AND CompCode = @CompCode
                                       AND Prefix  = @Prefix    AND Yr       = @YearEnd;";
            using (var cmd = new SqlCommand(updSql,
                                             (SqlConnection)_context.Database.GetDbConnection(), tx))
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

        // "26-27" style year end from YearEnds DbSet.
        private async Task<string?> GetYearEndAsync()
        {
            return await _context.YearEnds
                .Select(y => (y.StartDate.Year % 100).ToString("00") + "-" +
                             (y.EndDate.Year   % 100).ToString("00"))
                .FirstOrDefaultAsync();
        }

        // "CPY/26-27/01000042" → "01000042".
        private static string ExtractSequencePart(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;
            var slash = code.LastIndexOf('/');
            return slash < 0 ? code : code.Substring(slash + 1);
        }

        // Small local helper — matches the SafeStr pattern used across the
        // sibling services. Kept private so it doesn't collide.
        private static string SafeStr(SqlDataReader r, string col)
        {
            var i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? string.Empty : (r.GetValue(i)?.ToString()?.Trim() ?? string.Empty);
        }
    }
}
