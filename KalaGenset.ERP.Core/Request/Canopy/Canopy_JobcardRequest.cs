namespace KalaGenset.ERP.Core.Request.Canopy
{
    public class JobCard_CpyRequest
    {
        public string EmpCode { get; set; }
        public string PcCode { get; set; }
        public string CompCode { get; set; }
        public string Remark { get; set; }
        public List<CanopyPlanLineItem> LineItems { get; set; } = new();
    }

    public class CanopyPlanLineItem
    {
        public double Kva { get; set; }
        public string Model { get; set; }
        public string PartCode { get; set; }
        public double FNorm { get; set; }
        public double TotStk { get; set; }
        public double WipStk { get; set; }
        public double PenPlanQty { get; set; }
        public double PReq { get; set; }
        public int PlanQty { get; set; }
        public int BatchQty { get; set; }
        public string BomCode { get; set; }
        public string PlanCode { get; set; }
        public DateTime? PlanDate { get; set; }
        public int DayPlanQty { get; set; }
    }
}
