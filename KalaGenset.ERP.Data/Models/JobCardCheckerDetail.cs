using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class JobCardCheckerDetail
{
    public int Id { get; set; }

    public string? PlanCode { get; set; }

    public string? SixMname { get; set; }

    public string? Description { get; set; }

    public string? AssignTo { get; set; }

    public string? CorReqNo { get; set; }

    public string? Status { get; set; }
}
