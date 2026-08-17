using System;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KalaGenset.ERP.API.Controllers
{
    /// <summary>
    /// Gantt Task Feedback — an employee gives ESP feedback on the tasks of his own
    /// Gantt projects, several at a time. Gantt supplies the project/task listing;
    /// the feedback is written to the CorporateRequisition* tables via GanttTasks.ReqCode.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GanttFeedbackController : ControllerBase
    {
        private readonly IGanttFeedback _service;

        public GanttFeedbackController(IGanttFeedback service)
        {
            _service = service;
        }

        /// <summary>
        /// A missing stored procedure (SQL error 2812) means SP_GanttTaskFeedback.sql has not been
        /// run on this connection's database. Without this the endpoint answers a bare 500 with no
        /// body, which is impossible to diagnose from the browser.
        /// </summary>
        private IActionResult SqlProblem(SqlException ex)
        {
            var message = ex.Number == 2812
                ? "Database objects are missing. Run SP_GanttTaskFeedback.sql on the database this " +
                  "API is connected to, then retry. Detail: " + ex.Message
                : "The database call failed. Detail: " + ex.Message;

            return StatusCode(500, new { success = false, sqlErrorNumber = ex.Number, message });
        }

        // GET api/GanttFeedback/GetProjects?empCode=01250904
        [HttpGet("GetProjects")]
        public async Task<IActionResult> GetProjects(string empCode)
        {
            try
            {
                var result = await _service.GetProjectsAsync(empCode);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return SqlProblem(ex);
            }
        }

        // GET api/GanttFeedback/GetPendingTasks?empCode=01250904&projectId=704
        [HttpGet("GetPendingTasks")]
        public async Task<IActionResult> GetPendingTasks(string empCode, int projectId)
        {
            try
            {
                var result = await _service.GetPendingTasksAsync(empCode, projectId);
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return SqlProblem(ex);
            }
        }

        // POST api/GanttFeedback/SaveFeedbackBatch
        [HttpPost("SaveFeedbackBatch")]
        public async Task<IActionResult> SaveFeedbackBatch([FromBody] GanttFeedbackBatchRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Request body is required." });

            var result = await _service.SaveBatchAsync(request);
            return Ok(result);
        }
    }
}
