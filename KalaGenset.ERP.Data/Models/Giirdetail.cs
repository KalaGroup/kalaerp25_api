using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Giirdetail
{
    public string Giircode { get; set; } = null!;

    public int SrNo { get; set; }

    public string Girdpocode { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public double ChallanQty { get; set; }

    public double RecQty { get; set; }

    public double AccQty { get; set; }

    public double DivQty { get; set; }

    /// <summary>
    /// For Rework qty of giir
    /// </summary>
    public double Rwqty { get; set; }

    public double RejQty { get; set; }

    public string Giirstatus { get; set; } = null!;

    public string StoreRemark { get; set; } = null!;

    public string QualityRemark { get; set; } = null!;

    public string RwdefectRemark { get; set; } = null!;

    public string DefectRemark { get; set; } = null!;

    public int MaterialType { get; set; }

    public string DgversionCode { get; set; } = null!;

    /// <summary>
    /// N=Normal Process,A= Assly
    /// </summary>
    public string Dgtype { get; set; } = null!;

    public double SamplingQty { get; set; }
}
