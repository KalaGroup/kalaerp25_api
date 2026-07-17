using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class TurretKit
{
    public string Tkitid { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string BomCode { get; set; } = null!;

    public string SaveAsBomCode { get; set; } = null!;

    public string CanopyPartCode { get; set; } = null!;

    public string NestingForPartcode { get; set; } = null!;

    public int VersionNo { get; set; }

    public string SheetPartCode { get; set; } = null!;

    public string Kittype { get; set; } = null!;

    public double Tlength { get; set; }

    public double Twidth { get; set; }

    public double Tthickness { get; set; }

    public int SerialNo { get; set; }

    public double SerialQty { get; set; }

    public double PunchCutTime { get; set; }

    public double CuttingLengthArea { get; set; }

    public double MachineSpeed { get; set; }

    public string CpyCompleteStatus { get; set; } = null!;

    public string? CatId { get; set; }

    public string NestingType { get; set; } = null!;

    public string Bomtype { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public bool Discard { get; set; }

    public bool Active { get; set; }

    public bool Auth { get; set; }
}
