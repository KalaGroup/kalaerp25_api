using System.Collections.Generic;

namespace KalaGenset.ERP.Core.Request.CanopyAssembly
{
    // Input for the Save/End button on the Canopy Assembly Process page.
    //  * NEW mode (PFBCode starts with "NEW") → creates a fresh PSH record,
    //    inserts kit lines + StockWIP + serial-number rows + assembly-kit +
    //    optionally raises a Kanban REQ if the plan is fully consumed.
    //  * PSH mode (PFBCode starts with "PSH") → closes N units (top-N by
    //    SerialNo where EdtD IS NULL), inserts ProductWip, links attachments.
    public class SubmitCanopyProcessRequest
    {
        public string EmpCode          { get; set; } = string.Empty;
        public string PCCode           { get; set; } = string.Empty;   // LineWisePC (selected line)
        public string ParentDgPC       { get; set; } = string.Empty;   // ParentDgPC — reserved for future use
        public string CompanyCode      { get; set; } = string.Empty;   // "01" / "03" / "28"
        public string MachineCodeSrNo  { get; set; } = string.Empty;   // "Foam-->Foam1"
        public string PlanCode         { get; set; } = string.Empty;   // CPCode from plan header
        public string ProductCode      { get; set; } = string.Empty;   // canopy PartCode
        public string BOMCode          { get; set; } = string.Empty;
        public string PFBCode          { get; set; } = string.Empty;   // "NEW/..." or "PSH/..."
        public double BatchQty         { get; set; }                   // plan CPQty
        public double PrcQty           { get; set; }                   // this-submit qty
        public string Remark           { get; set; } = "Nil";
        public List<CanopyProcessPartLine> PrcDts { get; set; } = new();
        public List<CanopyProcessAttachment> Attachments { get; set; } = new();
    }

    // One kit-consumption line — mirrors the legacy PrcDts CSV column order:
    // PartCode --> KitQty --> PrcQty --> Rate --> Wt --> Sqft.
    public class CanopyProcessPartLine
    {
        public string PartCode { get; set; } = string.Empty;
        public double KitQty   { get; set; }
        public double PrcQty   { get; set; }
        public double Rate     { get; set; }
        public double Wt       { get; set; }
        public double Sqft     { get; set; }
    }

    // Attachments already uploaded via /CanopyAssembly/UploadCanopyProcessFile.
    // The Save flow copies these from the per-employee temp folder to the
    // permanent archive path and inserts one ProcessFeedbackFiles row per entry.
    public class CanopyProcessAttachment
    {
        public int    SrNo     { get; set; }
        public string FileName { get; set; } = string.Empty;
    }
}
