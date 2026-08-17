using System.Collections.Generic;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;

namespace KalaGenset.ERP.Core.Interface
{
    /// <summary>
    /// Gantt Task Feedback — the employee gives ESP feedback on his own Gantt
    /// project tasks, several at a time. Gantt is only the navigator; the
    /// feedback lands in the CorporateRequisition* tables via GanttTasks.ReqCode.
    /// </summary>
    public interface IGanttFeedback
    {
        /// <summary>Projects created by this employee that still have tasks pending feedback.</summary>
        Task<List<GanttFeedbackProjectDTO>> GetProjectsAsync(string empCode);

        /// <summary>Tasks in one project that are ESP-closed but pending at feedback.</summary>
        Task<List<GanttFeedbackTaskDTO>> GetPendingTasksAsync(string empCode, int projectId);

        /// <summary>Write one ESP feedback row per ticked task, in a single transaction.</summary>
        Task<GanttFeedbackSaveResultDTO> SaveBatchAsync(GanttFeedbackBatchRequest request);
    }
}
