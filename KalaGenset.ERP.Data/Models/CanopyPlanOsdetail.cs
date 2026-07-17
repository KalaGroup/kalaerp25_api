using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class CanopyPlanOsdetail
{
    public string Cpcode { get; set; } = null!;

    public string CpyPartCode { get; set; } = null!;

    public string Partcode { get; set; } = null!;

    public int SrNo { get; set; }

    public string Scode { get; set; } = null!;

    public double Qty { get; set; }

    public double Osmtfqty { get; set; }

    public string Osmtfstatus { get; set; } = null!;

    public double Osfqty { get; set; }

    public string Osfstatus { get; set; } = null!;

    public double Ospcqty { get; set; }

    public string Ospcstatus { get; set; } = null!;

    public bool Turret { get; set; }

    public bool Bending { get; set; }

    public bool Fabrication { get; set; }

    public bool PowderCoating { get; set; }

    public bool PowderCoatingAssembly { get; set; }

    public string ParentPart { get; set; } = null!;
}
