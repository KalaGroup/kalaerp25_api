namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class CreateKaizenSheetRequest
    {
        public string CompanyId { get; set; }
        // Header Info
        public int? DivisionId { get; set; }
        public string? DivisionName { get; set; }
        public string? DepartmentCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? WorkstationCode { get; set; }
        public string? WorkstationName { get; set; }
        public string? KaizenTheme { get; set; }
        public string KaizenInitiationDate { get; set; } = null!;   // "yyyy-MM-dd"
        public string? CompletionDate { get; set; }                  // "yyyy-MM-dd"

        // Problem Description (5W2H)
        public string? ProblemWhat { get; set; }
        public string? ProblemWhen { get; set; }
        public string? ProblemWhere { get; set; }
        public string? ProblemWho { get; set; }
        public string? ProblemWhy { get; set; }
        public string? ProblemHow { get; set; }
        public string? ProblemHowMuch { get; set; }

        // RCA — 5 Why Analysis
        public string? RcaWhy1 { get; set; }
        public string? RcaWhy2 { get; set; }
        public string? RcaWhy3 { get; set; }
        public string? RcaWhy4 { get; set; }
        public string? RcaWhy5 { get; set; }

        // Idea
        public string? Idea { get; set; }               // "Providing" or "Changing"
        public string? IdeaRemark { get; set; }

        // Countermeasure
        public string? CountermeasureRemark { get; set; }

        // Result & Deployment
        public string? Result { get; set; }              // "Reduce Time,Increase Life"
        public string? Improvement { get; set; }         // "P,Q,C,S"
        public string? Benefit { get; set; }             // "Increase Production,Reduce Cost"
        public string? InvestmentArea { get; set; }
        public string? SavingArea { get; set; }
        public string? HorizontalDeployment { get; set; }

        // Kaizen Sustenance
        public string? SustenanceWhatToDo { get; set; }
        public string? SustenanceHowToDo { get; set; }
        public string? SustenanceFrequency { get; set; }

        // Data Submitted
        public string? DataSubmittedBy { get; set; }
        public string? DataSubmittedOn { get; set; }     // "yyyy-MM-dd"

        // File paths — set by controller after saving files, not sent by UI
        public string? BeforePhotoPath { get; set; }
        public string? BeforePhotoName { get; set; }
        public string? AfterPhotoPath { get; set; }
        public string? AfterPhotoName { get; set; }
        public string? ImpactGraphPath { get; set; }
        public string? ImpactGraphName { get; set; }
    }
}

