using System;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    /// <summary>Department dropdown item = ProfitCenter (with line).</summary>
    public class MaterialDeptDTO
    {
        public string DeptCode { get; set; } = string.Empty;       // ProfitCenter.PCCode
        public string DeptName { get; set; } = string.Empty;       // ProfitCenter.PCName
    }

    /// <summary>
    /// One saved material row for the View grid.
    /// Identity is (MCode, SrNo) — the detail key — used by the grid for edit/delete.
    /// </summary>
    public class MaterialRecordDTO
    {
        public string MCode { get; set; } = string.Empty;         // 6MMaterial.MCode
        public int SrNo { get; set; }                              // 6MMaterialDetails.SrNo
        public string Date { get; set; } = string.Empty;          // yyyy-MM-dd
        public string DeptCode { get; set; } = string.Empty;      // = header.ProfitCenterCode
        public string DeptName { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;          // KVA
        public string MaterialType { get; set; } = string.Empty;  // Raw / Consumable / Spares / Tools
        public double Quantity { get; set; }
        public string Status { get; set; } = string.Empty;        // the "OK" / note
        public string Person { get; set; } = string.Empty;        // person to communicate
    }
}