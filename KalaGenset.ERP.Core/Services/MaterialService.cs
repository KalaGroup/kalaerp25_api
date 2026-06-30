using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KalaGenset.ERP.Core.Services
{
    public class MaterialService : IMaterial
    {
        private readonly KalaDbContext _context;

        // All database access goes through stored procedures (see SP_6MMaterial.sql):
        //   usp_6MMaterial_GetDepartments / _GetRecords
        //   usp_6MMaterial_SaveBatch    (header + all material lines + audit, one txn, REPLACE
        //                                semantics; details passed as a MaterialDetailType TVP)
        //   usp_6MMaterial_DeleteDetail  (soft-delete one line + audit, one txn)
        // Quantity is float -> read/written as double. The Plan (KVA) dropdown uses the existing
        // GetActivePartKVAList proc elsewhere. The save/delete procs own their own transaction,
        // so the service is just a thin caller.

        private const string TvpTypeName = "dbo.MaterialDetailType";

        public MaterialService(KalaDbContext context)
        {
            _context = context;
        }

        // Open the EF connection for raw ADO.NET. EF owns/disposes it.
        private async Task<SqlConnection> GetOpenConnectionAsync()
        {
            var conn = (SqlConnection)_context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();
            return conn;
        }

        private static void AddParam(DbCommand cmd, string name, object? value, DbType? type = null)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            if (type.HasValue) p.DbType = type.Value;
            cmd.Parameters.Add(p);
        }

        private static int ToInt(object v) => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
        private static double ToDbl(object v) => v == null || v == DBNull.Value ? 0d : Convert.ToDouble(v);
        private static string ToStr(object v) => v == null || v == DBNull.Value ? string.Empty : v.ToString()!;

        /// <summary>Departments = all active profit centers (with lines) for the company.</summary>
        public async Task<List<MaterialDeptDTO>> GetDepartmentsAsync(string companyCode)
        {
            var list = new List<MaterialDeptDTO>();
            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MMaterial_GetDepartments";
            AddParam(cmd, "@CompanyCode", companyCode ?? string.Empty);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new MaterialDeptDTO
                {
                    DeptCode = ToStr(rd["DeptCode"]),
                    DeptName = ToStr(rd["DeptName"]),
                });
            }
            return list;
        }

        /// <summary>View grid — 6MMaterial + 6MMaterialDetails, DeptName joined from ProfitCenter.</summary>
        public async Task<List<MaterialRecordDTO>> GetMaterialRecordsAsync(
            string companyCode, DateTime? date, string? deptCode)
        {
            var list = new List<MaterialRecordDTO>();
            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MMaterial_GetRecords";
            AddParam(cmd, "@CompanyCode", companyCode ?? string.Empty);
            AddParam(cmd, "@Dt", date.HasValue ? date.Value.Date : (object?)null, DbType.Date);
            AddParam(cmd, "@DeptCode", string.IsNullOrWhiteSpace(deptCode) ? (object?)null : deptCode);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new MaterialRecordDTO
                {
                    MCode = ToStr(rd["MCode"]),
                    SrNo = ToInt(rd["SrNo"]),
                    Date = ToStr(rd["Date"]),
                    DeptCode = ToStr(rd["DeptCode"]),
                    DeptName = ToStr(rd["DeptName"]),
                    Plan = ToStr(rd["Plan"]),
                    MaterialType = ToStr(rd["MaterialType"]),
                    Quantity = ToDbl(rd["Quantity"]),
                    Status = ToStr(rd["Status"]),
                    Person = ToStr(rd["Person"]),
                });
            }
            return list;
        }

        /// <summary>
        /// Save the whole form via usp_6MMaterial_SaveBatch: the material rows go in as one
        /// table-valued parameter; the proc does header find/create + MCode + REPLACE (deactivate
        /// current lines, insert submitted fresh) + audit in its own transaction.
        /// </summary>
        public async Task<bool> SaveMaterialBatchAsync(MaterialBatchRequest request)
        {
            var dt = DateTime.ParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;

            var table = BuildDetailTable(request.Entries);

            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MMaterial_SaveBatch";
            AddParam(cmd, "@CompanyCode", (request.CompanyCode ?? string.Empty).Trim());
            AddParam(cmd, "@Dt", dt, DbType.Date);
            AddParam(cmd, "@ProfitCenterCode", (request.DeptCode ?? string.Empty).Trim());
            AddParam(cmd, "@CreatedBy", (object?)request.CreatedBy ?? string.Empty);
            cmd.Parameters.Add(new SqlParameter("@Details", SqlDbType.Structured)
            {
                TypeName = TvpTypeName,
                Value = table,
            });

            await cmd.ExecuteNonQueryAsync();   // proc manages its own transaction
            return true;
        }

        /// <summary>Soft-delete one material line via usp_6MMaterial_DeleteDetail (+ audit, one txn).</summary>
        public async Task<bool> DeleteMaterialRecordAsync(string mcode, int srNo, string? modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(mcode)) return false;

            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MMaterial_DeleteDetail";
            AddParam(cmd, "@MCode", mcode.Trim());
            AddParam(cmd, "@SrNo", srNo, DbType.Int32);
            AddParam(cmd, "@ModifiedBy", (object?)modifiedBy ?? string.Empty);

            var result = await cmd.ExecuteScalarAsync();   // proc returns RowsAffected
            return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
        }

        /// <summary>Build the TVP (MaterialDetailType) from the request entries.</summary>
        private static DataTable BuildDetailTable(IEnumerable<MaterialEntry> entries)
        {
            var t = new DataTable();
            t.Columns.Add("Plan", typeof(string));
            t.Columns.Add("MaterialType", typeof(string));
            t.Columns.Add("Quantity", typeof(double));
            t.Columns.Add("Status", typeof(string));
            t.Columns.Add("Person", typeof(string));

            foreach (var e in entries)
            {
                var plan = (e.Plan ?? string.Empty).Trim();
                var type = (e.MaterialType ?? string.Empty).Trim();
                if (plan.Length == 0 || type.Length == 0) continue;   // skip blank lines
                t.Rows.Add(
                    plan, type, e.Quantity,
                    (object?)e.Status ?? DBNull.Value,
                    (object?)e.Person ?? DBNull.Value);
            }
            return t;
        }
    }
}