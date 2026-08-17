using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KalaGenset.ERP.Core.Services
{
    public class GanttFeedbackService : IGanttFeedback
    {
        private readonly KalaDbContext _context;

        // All database access goes through stored procedures (see SP_GanttTaskFeedback.sql):
        //   usp_GanttFeedback_GetProjects      projects created by the employee with pending tasks
        //   usp_GanttFeedback_GetPendingTasks  tasks that are ESP-closed but pending at feedback
        //   usp_GanttFeedback_SaveBatch        one feedback row per ticked task, one txn,
        //                                      task ids passed as a GanttFeedbackTaskType TVP
        // The save proc owns its transaction AND re-validates every task, so this
        // service is just a thin caller.

        private const string TvpTypeName = "dbo.GanttFeedbackTaskType";

        public GanttFeedbackService(KalaDbContext context)
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

        /// <summary>Projects the employee created that still have tasks awaiting his feedback.</summary>
        public async Task<List<GanttFeedbackProjectDTO>> GetProjectsAsync(string empCode)
        {
            var list = new List<GanttFeedbackProjectDTO>();
            if (string.IsNullOrWhiteSpace(empCode)) return list;

            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_GanttFeedback_GetProjects";
            AddParam(cmd, "@EmpCode", empCode.Trim());

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new GanttFeedbackProjectDTO
                {
                    ProjectID = ToInt(rd["ProjectID"]),
                    ProjectName = ToStr(rd["ProjectName"]),
                    ProjectStartDate = ToStr(rd["ProjectStartDate"]),
                    ProjectEndDate = ToStr(rd["ProjectEndDate"]),
                    PendingCount = ToInt(rd["PendingCount"]),
                });
            }
            return list;
        }

        /// <summary>Tasks in one project that are ESP-closed but pending at feedback.</summary>
        public async Task<List<GanttFeedbackTaskDTO>> GetPendingTasksAsync(string empCode, int projectId)
        {
            var list = new List<GanttFeedbackTaskDTO>();
            if (string.IsNullOrWhiteSpace(empCode) || projectId <= 0) return list;

            var conn = await GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "usp_GanttFeedback_GetPendingTasks";
            AddParam(cmd, "@EmpCode", empCode.Trim());
            AddParam(cmd, "@ProjectID", projectId, DbType.Int32);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new GanttFeedbackTaskDTO
                {
                    TaskID = ToInt(rd["TaskID"]),
                    TaskName = ToStr(rd["TaskName"]),
                    ReqCode = ToStr(rd["ReqCode"]),
                    ActNo = ToInt(rd["ActNo"]),
                    TaskStart = ToStr(rd["TaskStart"]),
                    TaskFinish = ToStr(rd["TaskFinish"]),
                    ReqDtTime = ToStr(rd["ReqDtTime"]),
                    ActionDtTime = ToStr(rd["ActionDtTime"]),
                    ReqMsg = ToStr(rd["ReqMsg"]),
                    ActionTaken = ToStr(rd["ActionTaken"]),
                    AssignedToName = ToStr(rd["AssignedToName"]),
                    ActionByName = ToStr(rd["ActionByName"]),
                });
            }
            return list;
        }

        /// <summary>
        /// Save feedback for every ticked task via usp_GanttFeedback_SaveBatch. The task ids
        /// go in as one table-valued parameter and the proc re-checks eligibility, writes the
        /// feedback rows, updates each requisition header and audits — all in its own
        /// transaction. SavedCount can be lower than the number sent if a task was already
        /// fed back from another screen in the meantime.
        /// </summary>
        public async Task<GanttFeedbackSaveResultDTO> SaveBatchAsync(GanttFeedbackBatchRequest request)
        {
            var requested = request.TaskIds?.Count ?? 0;
            var result = new GanttFeedbackSaveResultDTO { RequestedCount = requested };

            if (requested == 0)
            {
                result.Message = "No tasks were selected.";
                return result;
            }

            var table = BuildTaskTable(request.TaskIds!);

            try
            {
                var conn = await GetOpenConnectionAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "usp_GanttFeedback_SaveBatch";
                AddParam(cmd, "@EmpCode", (request.EmpCode ?? string.Empty).Trim());
                AddParam(cmd, "@CompanyCode", (request.CompanyCode ?? string.Empty).Trim());
                AddParam(cmd, "@ProjectID", request.ProjectID, DbType.Int32);
                AddParam(cmd, "@Rating", (request.Rating ?? string.Empty).Trim());
                AddParam(cmd, "@FeedbackStatus", (request.FeedbackStatus ?? string.Empty).Trim());
                AddParam(cmd, "@Feedback", (request.Feedback ?? string.Empty).Trim());
                cmd.Parameters.Add(new SqlParameter("@Tasks", SqlDbType.Structured)
                {
                    TypeName = TvpTypeName,
                    Value = table,
                });

                var scalar = await cmd.ExecuteScalarAsync();   // proc returns SavedCount
                result.SavedCount = ToInt(scalar!);
                result.Success = result.SavedCount > 0;

                if (result.SavedCount == 0)
                {
                    result.Message = "Nothing was saved — the selected tasks are no longer pending feedback.";
                }
                else if (result.SavedCount < requested)
                {
                    result.Message =
                        $"Feedback saved for {result.SavedCount} of {requested} task(s). " +
                        "The rest were already fed back.";
                }
                else
                {
                    result.Message = $"Feedback saved successfully for {result.SavedCount} task(s).";
                }
            }
            catch (SqlException ex)
            {
                // 51001-51005 are the proc's own validation THROWs — safe to show the user.
                result.Success = false;
                result.SavedCount = 0;
                result.Message = ex.Number >= 51001 && ex.Number <= 51005
                    ? ex.Message
                    : "Could not save the feedback. Please try again.";
            }

            return result;
        }

        /// <summary>Build the TVP (GanttFeedbackTaskType) from the ticked task ids.</summary>
        private static DataTable BuildTaskTable(IEnumerable<int> taskIds)
        {
            var t = new DataTable();
            t.Columns.Add("TaskID", typeof(int));

            var seen = new HashSet<int>();
            foreach (var id in taskIds)
            {
                if (id <= 0) continue;      // skip junk ids
                if (!seen.Add(id)) continue; // TVP has a PK on TaskID — no duplicates
                t.Rows.Add(id);
            }
            return t;
        }
    }
}
