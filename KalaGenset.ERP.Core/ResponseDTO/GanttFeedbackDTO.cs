using System;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    /// <summary>
    /// One Gantt project the logged-in employee created that still has tasks
    /// waiting for his ESP feedback. Projects with nothing pending are not returned.
    /// </summary>
    public class GanttFeedbackProjectDTO
    {
        public int ProjectID { get; set; }                              // GanttProject.ProjectID
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectStartDate { get; set; } = string.Empty;    // dd/MM/yyyy
        public string ProjectEndDate { get; set; } = string.Empty;      // dd/MM/yyyy
        public int PendingCount { get; set; }                           // tasks awaiting feedback
    }

    /// <summary>
    /// One task inside a project that is "ESP closed but pending at feedback".
    /// These are the rows the employee ticks on the form.
    /// </summary>
    public class GanttFeedbackTaskDTO
    {
        public int TaskID { get; set; }                                 // GanttTasks.ID (the checkbox key)
        public string TaskName { get; set; } = string.Empty;            // GanttTasks.Name
        public string ReqCode { get; set; } = string.Empty;             // GanttTasks.ReqCode -> the ESP requisition
        public int ActNo { get; set; }                                  // latest finished CorporateRequisitionActionTaken.ID
        public string TaskStart { get; set; } = string.Empty;           // dd/MM/yyyy hh:mm tt
        public string TaskFinish { get; set; } = string.Empty;
        public string ReqDtTime { get; set; } = string.Empty;
        public string ActionDtTime { get; set; } = string.Empty;
        public string ReqMsg { get; set; } = string.Empty;              // original requisition message
        public string ActionTaken { get; set; } = string.Empty;         // what the action-taker wrote on finishing
        public string AssignedToName { get; set; } = string.Empty;      // parsed out of GanttTasks.Assignments
        public string ActionByName { get; set; } = string.Empty;        // who finished the action
    }

    /// <summary>Result of a batch feedback submit.</summary>
    public class GanttFeedbackSaveResultDTO
    {
        public bool Success { get; set; }
        public int SavedCount { get; set; }                             // rows actually written
        public int RequestedCount { get; set; }                         // tasks the client sent
        public string Message { get; set; } = string.Empty;
    }
}
