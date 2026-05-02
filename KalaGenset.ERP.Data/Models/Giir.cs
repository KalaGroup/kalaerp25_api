using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Giir
{
    public string Giircode { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public DateTime Dt { get; set; }

    public DateTime? Qdt { get; set; }

    public string Yr { get; set; } = null!;

    public string Pocode { get; set; } = null!;

    public string ChallanCode { get; set; } = null!;

    public DateTime ChallanDate { get; set; }

    public string VehicleNo { get; set; } = null!;

    public DateTime GateTime { get; set; }

    /// <summary>
    /// P- Pending Q - Quality C- PV I - INvoice D - Done
    /// </summary>
    public string Giirstatus { get; set; } = null!;

    public string GateReceiptCode { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public string StoreRemark { get; set; } = null!;

    public string ReverseRemark { get; set; } = null!;

    public string QualityRemark { get; set; } = null!;

    public string? LotNo { get; set; }

    public string? Grade { get; set; }

    public bool Rdiscard { get; set; }

    public bool Active { get; set; }

    public bool Auth { get; set; }
}
