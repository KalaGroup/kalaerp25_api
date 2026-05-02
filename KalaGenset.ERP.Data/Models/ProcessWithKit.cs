using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class ProcessWithKit
{
    public string Pwkcode { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public string ProfitCenterCode { get; set; } = null!;

    public string ProcessType { get; set; } = null!;

    public string ProcessKitCode { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public double ProcessQty { get; set; }

    public double PkitQty { get; set; }

    public double Pfbrate { get; set; }

    public string CompanyCode { get; set; } = null!;

    public string Mtfstatus { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public string PrcBomcode { get; set; } = null!;

    public DateTime? PrcBomdt { get; set; }

    public bool Active { get; set; }

    public bool Discard { get; set; }

    public bool Rdiscard { get; set; }

    public bool Auth { get; set; }
}
