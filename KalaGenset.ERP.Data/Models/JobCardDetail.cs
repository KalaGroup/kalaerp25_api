using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class JobCardDetail
{
    public string JobCode { get; set; } = null!;

    public int SrNo { get; set; }

    public string Bomcode { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public double Qty { get; set; }

    public string PlanCode { get; set; } = null!;

    public DateTime PlanDate { get; set; }

    public double DayPlanQty { get; set; }

    public string Stage1Status { get; set; } = null!;

    public DateTime Stage1Dt { get; set; }

    public string Stage2Status { get; set; } = null!;

    public DateTime Stage2Dt { get; set; }

    public string Stage3Status { get; set; } = null!;

    public DateTime Stage3Dt { get; set; }

    public double Stage3Qty { get; set; }

    public double JobCard2Qty { get; set; }
}
