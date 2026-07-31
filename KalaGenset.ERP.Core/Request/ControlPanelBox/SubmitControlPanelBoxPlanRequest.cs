using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Core.Request.ControlPanelBox
{
    /// <summary>
    /// Input for POST /api/ControlPanelBox/SubmitPlan — the operator's manual
    /// Control Panel Plan. Step 1 of the migration only inserts the CanopyPlan
    /// header row via SP <c>InsertCanopyPlan_Maker_Checker</c>; the details
    /// insert (CanopyPlanDetails per Row) lands in Step 2. The client is
    /// expected to send Rows already so the contract stays stable across steps.
    ///
    /// PCCode / CompanyCode / PCCode_Act / Checker1 are all hardcoded server-side
    /// per the current spec — they are NOT accepted from the client.
    /// </summary>
    public class SubmitControlPanelBoxPlanRequest
    {
        public string   EmpCode { get; set; } = string.Empty;
        public DateTime FromDt  { get; set; }
        public DateTime ToDt    { get; set; }
        public List<SubmitControlPanelBoxPlanRow> Rows { get; set; } = new();
    }

    public class SubmitControlPanelBoxPlanRow
    {
        public string Dt       { get; set; } = string.Empty;   // 'YYYY-MM-DD'
        public string PartCode { get; set; } = string.Empty;   // BOMDetails.KitCode
        public string PartDesc { get; set; } = string.Empty;
        public string BomCode  { get; set; } = string.Empty;
        public double Qty      { get; set; }
    }
}
