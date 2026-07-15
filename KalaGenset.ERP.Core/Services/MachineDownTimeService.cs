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
    public class MachineDownTimeService : IMachineDownTime
    {
        private readonly KalaDbContext _context;

        // All database access goes through stored procedures (see SP_6MMachineDownTime.sql):
        //   usp_6MMachineDownTime_GetDepartments / _GetMachines / _GetRecords / _GetTrend
        //   usp_6MMachineDownTime_SaveBatch   (header + all machine lines + audit, one txn,
        //                                      details passed as a MachineDownTimeDetailType TVP)
        //   usp_6MMachineDownTime_DeleteDetail (soft-delete one line + audit, one txn)
        // The save/delete procs own their own transaction, so the service is just a thin caller.

        private const string TvpTypeName = "dbo.MachineDownTimeDetailType";

        public MachineDownTimeService(KalaDbContext context)
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
        private static string ToStr(object v) => v == null || v == DBNull.Value ? string.Empty : v.ToString()!;

        /// <summary>Companies the login may view charts for (usp_GetChildCompanies): 33 -> 01/03/28, else self.</summary>
        public async Task<List<CompanyOptionDTO>> GetViewCompaniesAsync(string companyCode)
        {
            var list = new List<CompanyOptionDTO>();
            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_GetChildCompanies";
            AddParam(cmd, "@CompanyCode", companyCode ?? string.Empty);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new CompanyOptionDTO
                {
                    CompanyCode = ToStr(rd["CompanyCode"]),
                    CompanyName = ToStr(rd["CompanyName"]),
                    ShortName = ToStr(rd["ShortName"]),
                });
            }
            return list;
        }

        /// <summary>Departments = profit centers that have machines assigned, for the session company.</summary>
        public async Task<List<MachineDeptDTO>> GetDepartmentsAsync(string companyCode)
        {
            var list = new List<MachineDeptDTO>();
            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MMachineDownTime_GetDepartments";
            AddParam(cmd, "@CompanyCode", companyCode ?? string.Empty);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new MachineDeptDTO
                {
                    DeptCode = ToStr(rd["DeptCode"]),
                    DeptName = ToStr(rd["DeptName"]),
                });
            }
            return list;
        }

        /// <summary>Machines for a profit center. MachineCode = PartCode, MachineName = AliseSerialNo.</summary>
        public async Task<List<MachineDTO>> GetMachinesByDepartmentAsync(string departmentCode)
        {
            var list = new List<MachineDTO>();
            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MMachineDownTime_GetMachines";
            AddParam(cmd, "@DeptCode", departmentCode ?? string.Empty);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new MachineDTO
                {
                    MachineCode = ToStr(rd["MachineCode"]),
                    MachineName = ToStr(rd["MachineName"]),
                });
            }
            return list;
        }

        /// <summary>View grid — 6MMachineDownTime + 6MMachineDownTimeDetails, totals computed, names joined.</summary>
        public async Task<List<MachineDownTimeRecordDTO>> GetDownTimeRecordsAsync(
            string companyCode, DateTime? date, string? departmentCode)
        {
            var list = new List<MachineDownTimeRecordDTO>();
            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MMachineDownTime_GetRecords";
            AddParam(cmd, "@CompanyCode", companyCode ?? string.Empty);
            AddParam(cmd, "@Dt", date.HasValue ? date.Value.Date : (object?)null, DbType.Date);
            AddParam(cmd, "@DeptCode", string.IsNullOrWhiteSpace(departmentCode) ? (object?)null : departmentCode);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new MachineDownTimeRecordDTO
                {
                    MCode = ToStr(rd["MCode"]),
                    SrNo = ToInt(rd["SrNo"]),
                    Date = ToStr(rd["Date"]),
                    DeptCode = ToStr(rd["DeptCode"]),
                    DeptName = ToStr(rd["DeptName"]),
                    MachineCode = ToStr(rd["MachineCode"]),
                    MachineName = ToStr(rd["MachineName"]),
                    Shift1Min = ToInt(rd["Shift1Min"]),
                    Shift2Min = ToInt(rd["Shift2Min"]),
                    TotalMin = ToInt(rd["TotalMin"]),
                    LineShift1Min = ToInt(rd["LineShift1Min"]),
                    LineShift2Min = ToInt(rd["LineShift2Min"]),
                    LineTotalMin = ToInt(rd["LineTotalMin"]),
                    Status = ToStr(rd["Status"]),
                    Remark = ToStr(rd["Remark"]),
                });
            }
            return list;
        }

        /// <summary>Records across a date range for the Daily / Weekly / Monthly trend charts.</summary>
        public async Task<List<MachineDownTimeTrendDTO>> GetDownTimeTrendAsync(
            string companyCode, DateTime fromDate, DateTime toDate)
        {
            var list = new List<MachineDownTimeTrendDTO>();
            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MMachineDownTime_GetTrend";
            AddParam(cmd, "@CompanyCode", companyCode ?? string.Empty);
            AddParam(cmd, "@FromDate", fromDate.Date, DbType.Date);
            AddParam(cmd, "@ToDate", toDate.Date, DbType.Date);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new MachineDownTimeTrendDTO
                {
                    Date = ToStr(rd["Date"]),
                    CompanyCode = ToStr(rd["CompanyCode"]),
                    DeptName = ToStr(rd["DeptName"]),
                    MachineName = ToStr(rd["MachineName"]),
                    TotalMin = ToInt(rd["TotalMin"]),
                    LineTotalMin = ToInt(rd["LineTotalMin"]),
                    Status = ToStr(rd["Status"]),
                    Remark = ToStr(rd["Remark"]),
                });
            }
            return list;
        }

        /// <summary>
        /// Save the whole form via usp_6MMachineDownTime_SaveBatch: the detail rows go in as one
        /// table-valued parameter and the proc does header find/create + MCode + upsert + audit in
        /// its own transaction.
        /// </summary>
        public async Task<bool> SaveDownTimeBatchAsync(MachineDownTimeBatchRequest request)
        {
            var dt = DateTime.ParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;

            var table = BuildDetailTable(request.Entries);

            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MMachineDownTime_SaveBatch";
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

        /// <summary>Soft-delete one machine line via usp_6MMachineDownTime_DeleteDetail (+ audit, one txn).</summary>
        public async Task<bool> DeleteDownTimeRecordAsync(string mcode, int srNo, string? modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(mcode)) return false;

            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MMachineDownTime_DeleteDetail";
            AddParam(cmd, "@MCode", mcode.Trim());
            AddParam(cmd, "@SrNo", srNo, DbType.Int32);
            AddParam(cmd, "@ModifiedBy", (object?)modifiedBy ?? string.Empty);

            var result = await cmd.ExecuteScalarAsync();   // proc returns RowsAffected
            return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
        }

        /// <summary>Build the TVP (MachineDownTimeDetailType) from the request entries.</summary>
        private static DataTable BuildDetailTable(IEnumerable<MachineDownTimeEntry> entries)
        {
            var t = new DataTable();
            t.Columns.Add("MachineCode", typeof(string));
            t.Columns.Add("Shift1Min", typeof(int));
            t.Columns.Add("Shift2Min", typeof(int));
            t.Columns.Add("LineShift1Min", typeof(int));
            t.Columns.Add("LineShift2Min", typeof(int));
            t.Columns.Add("Status", typeof(string));
            t.Columns.Add("Remark", typeof(string));

            foreach (var e in entries)
            {
                var mc = (e.MachineCode ?? string.Empty).Trim();
                if (mc.Length == 0) continue;   // skip rows with no machine selected
                t.Rows.Add(
                    mc,
                    e.Shift1Min, e.Shift2Min, e.LineShift1Min, e.LineShift2Min,
                    (object?)e.Status ?? DBNull.Value,
                    (object?)e.Remark ?? DBNull.Value);
            }
            return t;
        }
    }
}