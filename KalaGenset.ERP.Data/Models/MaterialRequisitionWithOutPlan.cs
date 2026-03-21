using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class MaterialRequisitionWithOutPlan
{
    /// <summary>
    /// WithOut Plan
    /// </summary>
    public string Reqcode { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public string ProfitCenterCode { get; set; } = null!;

    public string ToProfitCenterCode { get; set; } = null!;

    public string ClassCode { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public string? RequisitionFor { get; set; }

    public string ActNo { get; set; } = null!;

    public string Reqstatus { get; set; } = null!;

    public string ReqType { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public bool Discard { get; set; }

    public DateTime? AuthDt { get; set; }

    public bool Active { get; set; }

    public bool Auth { get; set; }

    public string SourceCode { get; set; } = null!;

    public string? InActiveRemark { get; set; }

    public string Pofcode { get; set; } = null!;
}
