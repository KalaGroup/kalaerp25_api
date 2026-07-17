using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class CanopyPlanDtsSub
{
    public string Cpcode { get; set; } = null!;

    public string CpyPartCode { get; set; } = null!;

    public int SrNo { get; set; }

    public string Partcode { get; set; } = null!;

    public string? CatId { get; set; }

    public string? CompCode { get; set; }

    public int Cpqty { get; set; }

    public int Cptqty { get; set; }

    public string Cptstatus { get; set; } = null!;

    public int CppartCutQty { get; set; }

    public string CppartCutStatus { get; set; } = null!;

    public int Cpbqty { get; set; }

    public string Cpbstatus { get; set; } = null!;

    public int Cpfqty { get; set; }

    public string Cpfstatus { get; set; } = null!;

    public int Cppcqty { get; set; }

    public string Cppcstatus { get; set; } = null!;

    public bool Turret { get; set; }

    public bool PartCutting { get; set; }

    public bool Bending { get; set; }

    public bool Fabrication { get; set; }

    public bool PowderCoating { get; set; }

    public string ProductionType { get; set; } = null!;

    public double Rate { get; set; }

    public int Strokes { get; set; }
}
