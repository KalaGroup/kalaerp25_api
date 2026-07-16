using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Core.Request
{
    /// <summary>Whole department's material for one date, saved in one transaction
    /// into [6MMaterial] (header) + [6MMaterialDetails] (lines). A save replaces the
    /// day+department's current active lines with the submitted ones.</summary>
    public class MaterialBatchRequest
    {
        public string Date { get; set; } = string.Empty;          // yyyy-MM-dd (from the form)
        public string CompanyCode { get; set; } = string.Empty;   // from session
        public string DeptCode { get; set; } = string.Empty;      // -> header.ProfitCenterCode
        public string DeptName { get; set; } = string.Empty;      // ignored on save (resolved by join)
        public string? CreatedBy { get; set; }                     // session user id
        public List<MaterialEntry> Entries { get; set; } = new();
    }

    public class MaterialEntry
    {
        public string DeptCode { get; set; } = string.Empty;      // department per ROW now
        public string Plan { get; set; } = string.Empty;          // KVA
        public double PlanQuantity { get; set; }                   // renamed from Quantity
        public string MaterialType { get; set; } = string.Empty;  // Raw / Consumable / Spares / Tools
        public string? PartCode { get; set; }                      // Raw: Part.PartCode; others: blank
        public string? PartName { get; set; }                      // Raw -> dropdown; else free text / blank
        public int ShortageQty { get; set; }                       // 0 = none; 1-100 otherwise
        public string? IssueType { get; set; }                      // Wrong / Damaged / Shortage
        public string? Status { get; set; }                        // Open (auto-Closed via ESP feedback)
        public string? Remark { get; set; }
        public string? Person { get; set; }                        // employee display name
    }

    /// <summary>Update ONE material line in place (proper per-row edit).</summary>
    public class MaterialRowUpdateRequest
    {
        public string MCode { get; set; } = string.Empty;
        public int SrNo { get; set; }
        public string Plan { get; set; } = string.Empty;
        public double PlanQuantity { get; set; }
        public string MaterialType { get; set; } = string.Empty;
        public string? PartCode { get; set; }
        public string? PartName { get; set; }
        public int ShortageQty { get; set; }
        public string? IssueType { get; set; }
        public string? Status { get; set; }
        public string? Remark { get; set; }
        public string? Person { get; set; }
        public string? ModifiedBy { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
    }

    /// <summary>Raise an ESP (Corporate Requisition) — proxied to the ERP20 API's
    /// /Corporate/CorporateReq/Submit with strType="Save", ReqCode="0".</summary>
    public class EspRaiseRequest
    {
        public string EmpCode { get; set; } = string.Empty;       // raiser's employee code (session user)
        public string FromPCCode { get; set; } = string.Empty;    // requesting department (record's PC code)
        public string ToEmpCode { get; set; } = string.Empty;     // target employee
        public string ToPCCode { get; set; } = string.Empty;      // target employee's PC code
        public string Priority { get; set; } = string.Empty;      // High Priority / Medium Priority / Low Priority
        public string ReqMsg { get; set; } = string.Empty;        // the (editable) shortage message
        public string CompanyCode { get; set; } = string.Empty;   // login company
        public string TargetDate { get; set; } = string.Empty;    // "yyyy-MM-dd" — selected by the user
        public string? MCode { get; set; }                          // material line to stamp with the ReqCode
        public int? SrNo { get; set; }
    }
}