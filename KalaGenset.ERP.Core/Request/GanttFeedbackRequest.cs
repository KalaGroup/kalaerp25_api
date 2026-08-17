using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Core.Request
{
    /// <summary>
    /// Feedback for several Gantt tasks at once. One rating / status / remark is
    /// applied to every ticked task, producing one ESP feedback row per task.
    /// Only the task IDs travel from the client — the ReqCode and ActNo behind
    /// each task are resolved (and re-validated) inside usp_GanttFeedback_SaveBatch.
    /// </summary>
    public class GanttFeedbackBatchRequest
    {
        public string EmpCode { get; set; } = string.Empty;      // session employee (ECode)
        public string CompanyCode { get; set; } = string.Empty;  // session company, for the audit row
        public int ProjectID { get; set; }                       // project the tasks were picked from

        public string Rating { get; set; } = string.Empty;       // "1".."5"
        public string FeedbackStatus { get; set; } = string.Empty; // "A" (Yes) / "R" (No)
        public string Feedback { get; set; } = string.Empty;     // remark text

        public List<int> TaskIds { get; set; } = new();          // GanttTasks.ID of every ticked row
    }
}
