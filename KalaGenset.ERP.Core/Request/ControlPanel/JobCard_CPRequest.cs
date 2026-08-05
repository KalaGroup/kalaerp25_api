using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.ControlPanel
{
    public class JobCard_CPRequest
    {
        public string EmpCode { get; set; }
        public string PCCode_Act { get; set; }
        public string PCCode { get; set; }
        public string CompCode { get; set; }
        public string Remark { get; set; }
        public string JobCard_CPDts { get; set; }
    }
    public class CPLineItem
    {
        public double Kva { get; set; }
        public string Model { get; set; }
        //public string Phase { get; set; }
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
