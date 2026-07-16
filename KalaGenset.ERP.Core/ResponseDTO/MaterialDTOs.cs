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
        public string CompanyCode { get; set; } = string.Empty;   // owning company (LEFT(PCCode,2))
        public string DeptCode { get; set; } = string.Empty;      // = header.ProfitCenterCode
        public string DeptName { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;          // KVA
        public double PlanQuantity { get; set; }                   // renamed from Quantity
        public string MaterialType { get; set; } = string.Empty;  // Raw / Consumable / Spares / Tools
        public string PartCode { get; set; } = string.Empty;      // Raw -> Part.PartCode; else blank
        public string PartName { get; set; } = string.Empty;      // Raw -> Part.PartDesc; else free text
        public int ShortageQty { get; set; }                       // 0 = no shortage
        public string IssueType { get; set; } = string.Empty;     // Wrong / Damaged / Shortage
        public string Status { get; set; } = string.Empty;        // Open / Closed (auto via ESP feedback)
        public string Remark { get; set; } = string.Empty;
        public string Person { get; set; } = string.Empty;        // person to communicate (employee)
        public string EspReqCode { get; set; } = string.Empty;     // COR number if an ESP was raised; '' = not yet
    }

    /// <summary>One dated trend row for the shortage charts (usp_6MMaterial_GetTrend).</summary>
    public class MaterialTrendDTO
    {
        public string Date { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string DeptName { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public string MaterialType { get; set; } = string.Empty;
        public string PartCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public int ShortageQty { get; set; }
        public string IssueType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Person { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;        // Reason for the charts grid
    }

    /// <summary>Part option for the Raw part dropdown (usp_6MMaterial_GetPartsByKVA).</summary>
    public class PartOptionDTO
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;      // Part.PartDesc
    }

    /// <summary>ESP target employee (proxied from ERP20 GetToEmpNamePCCode) — carries the PC code the Submit needs.</summary>
    public class EspEmployeeDTO
    {
        public string ECode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ProfitCenter { get; set; } = string.Empty;
        public string Pccode { get; set; } = string.Empty;
    }

    /// <summary>Employee option for the person dropdown (CopReqActionAssignToEmp_Sp).</summary>
    public class EmployeeOptionDTO
    {
        public string ECode { get; set; } = string.Empty;
        public string EmpName { get; set; } = string.Empty;       // "fname lname [ ecode ]"
    }
}