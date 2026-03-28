using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class JobCard
{
    public string JobCode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public string Pccode { get; set; } = null!;

    public string Stage1Status { get; set; } = null!;

    public string Stage2Status { get; set; } = null!;

    public string Stage3Status { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public bool Active { get; set; }

    public bool Auth { get; set; }

    public string? PccodeAct { get; set; }
}
