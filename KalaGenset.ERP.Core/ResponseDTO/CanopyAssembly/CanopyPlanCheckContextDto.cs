using System.Collections.Generic;

namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Full detail for one CanopyPlan shown in the checker's modal.
    // Header + all detail rows the maker planned.
    public class CanopyPlanCheckContextDto
    {
        public CanopyPlanCheckHeaderDto     Header  { get; set; } = new();
        public List<CanopyPlanCheckDetailRowDto> Details { get; set; } = new();
    }

    public class CanopyPlanCheckHeaderDto
    {
        public string CPCode      { get; set; } = string.Empty;
        public string Dt          { get; set; } = string.Empty;
        public string FromDt      { get; set; } = string.Empty;
        public string ToDt        { get; set; } = string.Empty;
        public string PlanPCCode  { get; set; } = string.Empty;
        public string PCCode_Act  { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string PlanType    { get; set; } = string.Empty;   // 'M' / 'G'
        public string PlanStatus  { get; set; } = string.Empty;   // 'P' / 'D'
        public string MakerCode   { get; set; } = string.Empty;
        public string Yr          { get; set; } = string.Empty;
    }

    // One row per planned canopy.
    public class CanopyPlanCheckDetailRowDto
    {
        public int    SrNo         { get; set; }
        public string Dt           { get; set; } = string.Empty;
        public string Partcode     { get; set; } = string.Empty;
        public string PartDesc     { get; set; } = string.Empty;
        public string BomCode      { get; set; } = string.Empty;
        public string PartCodeWOP  { get; set; } = string.Empty;
        public double Qty          { get; set; }
        public double CpyWIPQty    { get; set; }
        public double CpyWOPQty    { get; set; }
        public string CpyWIPStatus { get; set; } = string.Empty;   // 'D' when process closed the plan
        public string CpyWOPStatus { get; set; } = string.Empty;
    }
}
