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
    public class ManpowerStatusService : IManpowerStatus
    {
        private readonly KalaDbContext _context;

        // Skill -> Grade name mapping: W3 = Skilled, W2 = Semi, W1 = Unskilled.
        // All database access goes through stored procedures (see SP_6MManpowerStatus.sql):
        //   usp_6MManpowerStatus_GetDepartments / _GetStations / _GetRecords / _GetTrend
        //   usp_6MManpowerStatus_SaveBatch   (header + all station lines + audit, one txn,
        //                                     details passed as a ManpowerStatusDetailType TVP;
        //                                     Sanctioned snapshot is frozen on update, set on insert)
        //   usp_6MManpowerStatus_DeleteDetail (soft-delete one line + audit, one txn)
        // Skill quantities are float -> read/written as double. The save/delete procs own their
        // own transaction, so the service is just a thin caller.

        private const string TvpTypeName = "dbo.ManpowerStatusDetailType";

        public ManpowerStatusService(KalaDbContext context)
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

        /// <summary>Departments = profit centers with at least one W1/W2/W3 sanctioned station.</summary>
        public async Task<List<ManpowerDeptDTO>> GetDepartmentsAsync(string companyCode)
        {
            var list = new List<ManpowerDeptDTO>();
            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MManpowerStatus_GetDepartments";
            AddParam(cmd, "@CompanyCode", companyCode ?? string.Empty);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new ManpowerDeptDTO
                {
                    PcId = ToInt(rd["PcId"]),
                    PcCode = ToStr(rd["PcCode"]),
                    PcName = ToStr(rd["PcName"]),
                });
            }
            return list;
        }

        /// <summary>Stations of a profit center with sanctioned headcount pivoted into 3 skill columns.</summary>
        public async Task<List<ManpowerStationDTO>> GetStationsByDepartmentAsync(int pcId, string companyCode)
        {
            var list = new List<ManpowerStationDTO>();
            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MManpowerStatus_GetStations";
            AddParam(cmd, "@PcId", pcId, DbType.Int32);
            AddParam(cmd, "@CompanyCode", companyCode ?? string.Empty);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new ManpowerStationDTO
                {
                    WkCode = ToStr(rd["WkCode"]),
                    WorkStationName = ToStr(rd["WorkStationName"]),
                    SancSkilled = ToDbl(rd["SancSkilled"]),
                    SancSemi = ToDbl(rd["SancSemi"]),
                    SancUnskilled = ToDbl(rd["SancUnskilled"]),
                });
            }
            return list;
        }

        /// <summary>View grid — 6MManpowerStatus + 6MManpowerStatusDetails, Shortage = Sanctioned - Available.</summary>
        public async Task<List<ManpowerStatusRecordDTO>> GetManpowerRecordsAsync(
            string companyCode, DateTime? date, string? shift, int? pcId)
        {
            var list = new List<ManpowerStatusRecordDTO>();
            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MManpowerStatus_GetRecords";
            AddParam(cmd, "@CompanyCode", companyCode ?? string.Empty);
            AddParam(cmd, "@Dt", date.HasValue ? date.Value.Date : (object?)null, DbType.Date);
            AddParam(cmd, "@Shift", string.IsNullOrWhiteSpace(shift) ? (object?)null : shift);
            AddParam(cmd, "@PcId", pcId.HasValue && pcId.Value > 0 ? pcId.Value : (object?)null, DbType.Int32);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new ManpowerStatusRecordDTO
                {
                    MCode = ToStr(rd["MCode"]),
                    SrNo = ToInt(rd["SrNo"]),
                    Date = ToStr(rd["Date"]),
                    Shift = ToStr(rd["Shift"]),
                    PcId = ToInt(rd["PcId"]),
                    PcName = ToStr(rd["PcName"]),
                    WkCode = ToStr(rd["WKCode"]),
                    WorkStationName = ToStr(rd["WorkStationName"]),
                    SancSkilled = ToDbl(rd["SancSkilled"]),
                    SancSemi = ToDbl(rd["SancSemi"]),
                    SancUnskilled = ToDbl(rd["SancUnskilled"]),
                    AvailSkilled = ToDbl(rd["AvailSkilled"]),
                    AvailSemi = ToDbl(rd["AvailSemi"]),
                    AvailUnskilled = ToDbl(rd["AvailUnskilled"]),
                    ShortSkilled = ToDbl(rd["ShortSkilled"]),
                    ShortSemi = ToDbl(rd["ShortSemi"]),
                    ShortUnskilled = ToDbl(rd["ShortUnskilled"]),
                    Absent = ToDbl(rd["Absent"]),
                    Remark = ToStr(rd["Remark"]),
                });
            }
            return list;
        }

        /// <summary>Records across a date range for the Daily / Weekly / Monthly shortage charts.</summary>
        public async Task<List<ManpowerShortageTrendDTO>> GetShortageTrendAsync(
            string companyCode, DateTime fromDate, DateTime toDate)
        {
            var list = new List<ManpowerShortageTrendDTO>();
            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MManpowerStatus_GetTrend";
            AddParam(cmd, "@CompanyCode", companyCode ?? string.Empty);
            AddParam(cmd, "@FromDate", fromDate.Date, DbType.Date);
            AddParam(cmd, "@ToDate", toDate.Date, DbType.Date);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new ManpowerShortageTrendDTO
                {
                    Date = ToStr(rd["Date"]),
                    PcName = ToStr(rd["PcName"]),
                    WorkStationName = ToStr(rd["WorkStationName"]),
                    ShortTotal = ToDbl(rd["ShortTotal"]),
                    Absent = ToDbl(rd["Absent"]),
                });
            }
            return list;
        }

        /// <summary>
        /// Save the whole form via usp_6MManpowerStatus_SaveBatch: the station rows go in as one
        /// table-valued parameter; the proc does header find/create + MCode + upsert (Sanctioned
        /// frozen on update, captured on insert) + audit in its own transaction.
        /// </summary>
        public async Task<bool> SaveManpowerBatchAsync(ManpowerStatusBatchRequest request)
        {
            var dt = DateTime.ParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;

            var table = BuildDetailTable(request.Entries);

            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MManpowerStatus_SaveBatch";
            AddParam(cmd, "@CompanyCode", (request.CompanyCode ?? string.Empty).Trim());
            AddParam(cmd, "@Dt", dt, DbType.Date);
            AddParam(cmd, "@Shift", string.IsNullOrWhiteSpace(request.Shift) ? "F" : request.Shift.Trim());
            AddParam(cmd, "@ProfitCenterCode", (request.PcCode ?? string.Empty).Trim());
            AddParam(cmd, "@CreatedBy", (object?)request.CreatedBy ?? string.Empty);
            cmd.Parameters.Add(new SqlParameter("@Details", SqlDbType.Structured)
            {
                TypeName = TvpTypeName,
                Value = table,
            });

            await cmd.ExecuteNonQueryAsync();   // proc manages its own transaction
            return true;
        }

        /// <summary>Soft-delete one station line via usp_6MManpowerStatus_DeleteDetail (+ audit, one txn).</summary>
        public async Task<bool> DeleteManpowerRecordAsync(string mcode, int srNo, string? modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(mcode)) return false;

            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_6MManpowerStatus_DeleteDetail";
            AddParam(cmd, "@MCode", mcode.Trim());
            AddParam(cmd, "@SrNo", srNo, DbType.Int32);
            AddParam(cmd, "@ModifiedBy", (object?)modifiedBy ?? string.Empty);

            var result = await cmd.ExecuteScalarAsync();   // proc returns RowsAffected
            return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
        }

        /// <summary>Build the TVP (ManpowerStatusDetailType) from the request entries.</summary>
        private static DataTable BuildDetailTable(IEnumerable<ManpowerStatusEntry> entries)
        {
            var t = new DataTable();
            t.Columns.Add("WkCode", typeof(string));
            t.Columns.Add("SancSkilled", typeof(double));
            t.Columns.Add("SancSemi", typeof(double));
            t.Columns.Add("SancUnskilled", typeof(double));
            t.Columns.Add("AvailSkilled", typeof(double));
            t.Columns.Add("AvailSemi", typeof(double));
            t.Columns.Add("AvailUnskilled", typeof(double));
            t.Columns.Add("Absent", typeof(double));
            t.Columns.Add("Remark", typeof(string));

            foreach (var e in entries)
            {
                var wk = (e.WkCode ?? string.Empty).Trim();
                if (wk.Length == 0) continue;   // skip rows with no work station
                t.Rows.Add(
                    wk,
                    e.SancSkilled, e.SancSemi, e.SancUnskilled,
                    e.AvailSkilled, e.AvailSemi, e.AvailUnskilled, e.Absent,
                    (object?)e.Remark ?? DBNull.Value);
            }
            return t;
        }
    }
}