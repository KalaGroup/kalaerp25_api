using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class TurretKitForPrcDt
{
    public string Cpcode { get; set; } = null!;

    public string Tkitid { get; set; } = null!;

    public int SrNo { get; set; }

    public string PartCode { get; set; } = null!;

    public double Qty { get; set; }

    public double Tlength { get; set; }

    public double Twidth { get; set; }

    public double Theight { get; set; }

    public double Tthickness { get; set; }

    public double TlossWt { get; set; }

    public double Tlength1 { get; set; }

    public double Tlength2 { get; set; }

    public double Twidth1 { get; set; }

    public double Twidth2 { get; set; }

    public double Tlosssqft { get; set; }

    public string Tcatagorycode { get; set; } = null!;
}
