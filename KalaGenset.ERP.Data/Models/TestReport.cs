using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class TestReport
{
    public string Trcode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Yr { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public string ProcessCode { get; set; } = null!;

    public DateTime? TrstartTime { get; set; }

    public DateTime? DgstartTime { get; set; }

    public DateTime? DgendTime { get; set; }

    public DateTime? TrendTime { get; set; }

    public string RevTrcode { get; set; } = null!;

    public double BalQty { get; set; }

    public string EngineModel { get; set; } = null!;

    public string Hp { get; set; } = null!;

    public string Kw { get; set; } = null!;

    public string Speed { get; set; } = null!;

    public string Alternator { get; set; } = null!;

    public string RatedKva { get; set; } = null!;

    public string RatedVolt { get; set; } = null!;

    public string RatedAmps { get; set; } = null!;

    public string Ph { get; set; } = null!;

    public string Pf { get; set; } = null!;

    public string Frequency { get; set; } = null!;

    public string Ambtemp { get; set; } = null!;

    public string Ry { get; set; } = null!;

    public string Yb { get; set; } = null!;

    public string Br { get; set; } = null!;

    public string VoltageRegulation { get; set; } = null!;

    public string RoomTempreture { get; set; } = null!;

    public string Rpmregulation { get; set; } = null!;

    public bool Llop { get; set; }

    public bool Hwt { get; set; }

    public bool? Hct { get; set; }

    public bool Osd { get; set; }

    public string Distatus { get; set; } = null!;

    public string Pdirstatus { get; set; } = null!;

    public string? Qastatus { get; set; }

    public string MachineNo { get; set; } = null!;

    public string? Tpsstatus { get; set; }

    public string Remark { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public double DieselRate { get; set; }

    public double PerUnitQty { get; set; }

    public bool Active { get; set; }

    public bool Rdiscard { get; set; }

    public bool Auth { get; set; }
}
