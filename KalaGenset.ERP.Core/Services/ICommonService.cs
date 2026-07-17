using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data;
using System.Data;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Services
{
    public interface ICommonService
    {
        Task<string> GetMaxNoAsync(string tableName, string prefix, string compCode);
        Task<string> GetTranNameAsync(string sql, params object[] parameters);
    }

    public class CommonService : ICommonService
    {
        private readonly KalaDbContext _db;

        public CommonService(KalaDbContext db) => _db = db;

        //public async Task<string> GetMaxNoAsync(string tableName, string prefix, string compCode)
        //{
        //    var conn = _db.Database.GetDbConnection();
        //    if (conn.State == ConnectionState.Closed)
        //        await conn.OpenAsync();

        //    using var cmd = conn.CreateCommand();
        //    cmd.CommandText = "GetMaxNo";
        //    cmd.CommandType = CommandType.StoredProcedure;

        //    cmd.Parameters.Add(new SqlParameter("@TableName", tableName));
        //    cmd.Parameters.Add(new SqlParameter("@Prefix", prefix));
        //    cmd.Parameters.Add(new SqlParameter("@CompCode", compCode));

        //    var result = await cmd.ExecuteScalarAsync();
        //    return result?.ToString() ?? string.Empty;
        //}

        //public async Task<string> GetMaxNoAsync(string tableName, string prefix, string compCode)
        //{
        //    var conn = _db.Database.GetDbConnection();
        //    if (conn.State == ConnectionState.Closed)
        //        await conn.OpenAsync();

        //    using var cmd = conn.CreateCommand();
        //    cmd.CommandText = "GetMaxNo";
        //    cmd.CommandType = CommandType.StoredProcedure;

        //    // CRITICAL: enroll in EF's current transaction (this is what was missing)
        //    var currentTx = _db.Database.CurrentTransaction;
        //    if (currentTx != null)
        //        cmd.Transaction = currentTx.GetDbTransaction();

        //    cmd.Parameters.Add(new SqlParameter("@TableName", tableName));
        //    cmd.Parameters.Add(new SqlParameter("@Prefix", prefix));
        //    cmd.Parameters.Add(new SqlParameter("@CompCode", compCode));

        //    var result = await cmd.ExecuteScalarAsync();
        //    return result?.ToString() ?? string.Empty;
        //}

        // ---- Common code generator: transaction-safe + concurrency-safe ----
        public async Task<string> GetMaxNoAsync(string tableName, string prefix, string compCode)
        {
            string yearEnd = GetFinancialYear();   // TODO: confirm format matches your existing codes
            return await GetMaxAsync(tableName, prefix, yearEnd, compCode);
        }

        private async Task<string> GetMaxAsync(string tableName, string prefix, string yearEnd, string compCode)
        {
            // WITH (UPDLOCK, HOLDLOCK): locks the row for the rest of the transaction so a second
            // concurrent save waits instead of reading the same MaxValue -> no duplicate codes.
            int currentMax = await _db.Database
                .SqlQueryRaw<int>(
                    "SELECT ISNULL(MaxValue, 0) AS Value FROM GetMaxCode WITH (UPDLOCK, HOLDLOCK) " +
                    "WHERE TblName = {0} AND CompCode = {1} AND Prefix = {2} AND Yr = {3}",
                    tableName, compCode, prefix, yearEnd)
                .FirstOrDefaultAsync();

            int next = currentMax + 1;
            string paddedSerial = next.ToString().PadLeft(6, '0');
            string transCode = $"{prefix}/{yearEnd}/{compCode}{paddedSerial}";

            int rowsUpdated = await _db.Database.ExecuteSqlRawAsync(
                "UPDATE GetMaxCode SET MaxValue = {0} " +
                "WHERE Prefix = {1} AND TblName = {2} AND CompCode = {3} AND Yr = {4}",
                next, prefix, tableName, compCode, yearEnd);

            if (rowsUpdated == 0)
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO GetMaxCode (TblName, CompCode, Prefix, Yr, MaxValue) " +
                    "VALUES ({0}, {1}, {2}, {3}, {4})",
                    tableName, compCode, prefix, yearEnd, next);
            }

            return transCode;
        }

        // Indian financial year as a 5-char string, e.g. "25-26" (April–March).
        // This matches Substring(4,5) used elsewhere for the Yr. ADJUST if your format differs.
        private static string GetFinancialYear()
        {
            var now = DateTime.Now;
            int startYear = now.Month >= 4 ? now.Year : now.Year - 1;
            int endYear = startYear + 1;
            return $"{startYear % 100:D2}-{endYear % 100:D2}";
        }

        public async Task<string> GetTranNameAsync(string sql, params object[] parameters)
        {
            var result = await _db.Database
                .SqlQueryRaw<string>(sql, parameters)
                .FirstOrDefaultAsync();

            return result ?? string.Empty;
        }
        // In CommonService.cs
       
    }
}