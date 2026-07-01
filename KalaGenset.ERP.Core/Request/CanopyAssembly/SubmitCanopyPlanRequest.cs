using System;
using System.Collections.Generic;
using KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly;

namespace KalaGenset.ERP.Core.Request.CanopyAssembly
{
    // Input for the Save button on the Canopy Plan page.
    // Mirrors the legacy setSave("S") flow: creates one CanopyPlan master,
    // N CanopyPlanDetails rows, and 2 auto-REQs per detail row (Logistics Kit
    // + Wiring Harness).
    public class SubmitCanopyPlanRequest
    {
        public string PCCode      { get; set; } = string.Empty;  // LineWisePC (selected line — pcCode_Act)
        public string ParentDgPC  { get; set; } = string.Empty;  // ParentDgPC of the selected line — pcCode_Old
        public string CompanyCode { get; set; } = string.Empty;  // "01", "03", "28"
        public string EmpCode     { get; set; } = string.Empty;  // for LoginTransactionDetails
        public DateTime FromDt    { get; set; }
        public DateTime ToDt      { get; set; }
        public List<CanopyPlanRowDto> Rows { get; set; } = new();
    }
}
