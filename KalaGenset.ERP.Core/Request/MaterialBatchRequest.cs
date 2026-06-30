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
        public string Plan { get; set; } = string.Empty;          // KVA
        public string MaterialType { get; set; } = string.Empty;  // Raw / Consumable / Spares / Tools
        public double Quantity { get; set; }
        public string? Status { get; set; }
        public string? Person { get; set; }
    }
}