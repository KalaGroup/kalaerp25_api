using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class CorporateRequisition
{
    public string ReqCode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public string EmpCode { get; set; } = null!;

    public string FromPccode { get; set; } = null!;

    public string? ToEmpCode { get; set; }

    public string ToPccode { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string ReqMsg { get; set; } = null!;

    public string AssignStatus { get; set; } = null!;

    public string FeedbackStatus { get; set; } = null!;

    public string FeedbackRating { get; set; } = null!;

    public DateTime? FeedbackDt { get; set; }

    public string FeedbackRemark { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public DateTime? TargetDate { get; set; }

    public bool Active { get; set; }

    public bool Discard { get; set; }
}
