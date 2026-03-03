using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KalaGenset.ERP.Core.Services
{
    public class CanopyService : ICanopy
    {
        private readonly KalaDbContext _context;
        /// <summary>
        /// Initializes a new instance of the <see cref="CanopyService"/> class.
        /// </summary>
        /// <param name="context">The database context for accessing ERP data.</param>

        public CanopyService(KalaDbContext context)
        {
            _context = context;
        }

        public async Task<List<Dictionary<string, object>>> GetCanopyPlanAsync(string strJobCardType, string strcompID)
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _context.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetJobCard_Cpy_PlanDts";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@Type", strJobCardType));
                    cmd.Parameters.Add(new SqlParameter("@CompId", strcompID));

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

        public async Task<string> SubmitCanopyPlanDetails(JobCard_CpyRequest request)
        {
            string StrDisplayMsg = "";
            string StrDispCode_CPYPlan = "";
            String StrDispCode_MaterialReq_CNC_ALL_msg = "";
            String StrDispCode_MaterialReq_WH_ALL_msg = "";
            String StrDispCode_MaterialReq_FAB_ALL_msg = "";
            String StrDispCode_MaterialReq_POC_ALL_msg = "";
            string StrDispCode_MaterialReq_CNC = "";

              var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // Get Year End
                        string? YearEnd = await _context.YearEnds
                            .Select(y => $"{y.StartDate:yy}-{y.EndDate:yy}")
                            .FirstOrDefaultAsync();

                        StrDispCode_CPYPlan = await GetMaxAsync("CanopyPlan", "CPY", YearEnd, request.CompCode);
                        await _context.Database.ExecuteSqlRawAsync("EXEC InsertCanopyPlan @CPCode={0}, @Dt={1}, @MaxSrNo={2}, @Yr={3}, " +
                                                                    "@FromDt={4}, @ToDt={5}, @PlanPCCode={6}, @CompanyCode={7}, @PlanType={8}, @AutoFlg={9}",
                                                StrDispCode_CPYPlan,
                                                DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"),
                                                StrDispCode_CPYPlan.Substring(10, 8),
                                                StrDispCode_CPYPlan.Substring(4, 5),
                                                DateTime.Now.ToString("yyyy-MM-dd 00:00:00"),
                                                DateTime.Now.ToString("yyyy-MM-dd 00:00:00"),
                                                request.PcCode,
                                                request.CompCode,
                                                "G",
                                                "Yes");
                        // ── Process each selected line item ───────────────────────────────
                        int srNo = 0;

                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception("Error submitting canopy plan details", ex);
                    }


                });

            return "This method is a placeholder for submitting canopy plan details. The actual implementation will depend on the specific requirements and business logic for processing canopy plans. The method currently initializes several string variables that may be used to store messages or codes related to the submission process. The final return value is a placeholder string indicating that the submission logic needs to be implemented.";
        }

        private async Task<string> GetMaxAsync(string tableName, string prefix, string yearEnd, string compCode)
        {
            // 1. Read current max — same SELECT as old method
            int currentMax = await _context.Database
                .SqlQueryRaw<int>(
                    "SELECT ISNULL(MaxValue, 0) AS Value FROM GetMaxCode " +
                    "WHERE TblName = {0} AND CompCode = {1} AND Prefix = {2} AND Yr = {3}",
                    tableName, compCode, prefix, yearEnd)
                .FirstOrDefaultAsync();

            // 2. Pad to 6 digits — replaces the old if/else chain
            int next = currentMax + 1;
            string paddedSerial = next.ToString().PadLeft(6, '0');

            // 3. Build transaction code — same format as old: Prefix/Yr/CompCode+Serial
            string transCode = $"{prefix}/{yearEnd}/{compCode}{paddedSerial}";

            // 4. Update max — same UPDATE as old method
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE GetMaxCode SET MaxValue = {0} " +
                "WHERE Prefix = {1} AND TblName = {2} AND CompCode = {3} AND Yr = {4}",
                paddedSerial, prefix, tableName, compCode, yearEnd);

            return transCode;
        }      
    }
}
