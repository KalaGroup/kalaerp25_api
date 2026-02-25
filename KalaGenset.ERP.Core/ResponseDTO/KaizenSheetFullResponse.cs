using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class KaizenSheetFullResponse
    {
        public int Id { get; set; }
        public string KaizenSheetNo { get; set; } = null!;

        // Header
        public int? DivisionId { get; set; }
        public string? DivisionName { get; set; }
        public string? DepartmentCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? WorkstationCode { get; set; }
        public string? WorkstationName { get; set; }
        public string? KaizenTheme { get; set; }
        public string? KaizenInitiationDate { get; set; }
        public string? CompletionDate { get; set; }

        // 5W2H
        public string? ProblemWhat { get; set; }
        public string? ProblemWhen { get; set; }
        public string? ProblemWhere { get; set; }
        public string? ProblemWho { get; set; }
        public string? ProblemWhy { get; set; }
        public string? ProblemHow { get; set; }
        public string? ProblemHowMuch { get; set; }

        // Photos
        public string? BeforePhotoPath { get; set; }
        public string? BeforePhotoName { get; set; }
        public string? AfterPhotoPath { get; set; }
        public string? AfterPhotoName { get; set; }

        // RCA 5 Why
        public string? RcaWhy1 { get; set; }
        public string? RcaWhy2 { get; set; }
        public string? RcaWhy3 { get; set; }
        public string? RcaWhy4 { get; set; }
        public string? RcaWhy5 { get; set; }

        // Idea
        public string? Idea { get; set; }
        public string? IdeaRemark { get; set; }

        // Countermeasure
        public string? CountermeasureRemark { get; set; }

        // Result & Deployment
        public string? Result { get; set; }
        public string? Improvement { get; set; }
        public string? Benefit { get; set; }
        public string? InvestmentArea { get; set; }
        public string? SavingArea { get; set; }
        public string? HorizontalDeployment { get; set; }

        // Impact Graph
        public string? ImpactGraphPath { get; set; }
        public string? ImpactGraphName { get; set; }

        // Sustenance
        public string? SustenanceWhatToDo { get; set; }
        public string? SustenanceHowToDo { get; set; }
        public string? SustenanceFrequency { get; set; }

        // Submitted
        public string? DataSubmittedBy { get; set; }
        public string? DataSubmittedOn { get; set; }

        // Audit
        public bool IsActive { get; set; }
        public bool IsDiscard { get; set; }
        public bool IsAuth { get; set; }
    }
}
