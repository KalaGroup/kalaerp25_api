using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class TurretKitForPrc
{
    public string Cpcode { get; set; } = null!;

    public string Tkitid { get; set; } = null!;

    public string Bomcode { get; set; } = null!;

    public string CanopyPartCode { get; set; } = null!;

    public string TurretKitPartcode { get; set; } = null!;

    public string SheetPartCode { get; set; } = null!;

    public string Kittype { get; set; } = null!;

    public double Tlength { get; set; }

    public double Twidth { get; set; }

    public double Tthickness { get; set; }

    public int SerialNo { get; set; }

    public double SerialQty { get; set; }

    public string PrcStatus { get; set; } = null!;

    public string Partcutstatus { get; set; } = null!;

    public string? CompCode { get; set; }

    public string? CatId { get; set; }
}
