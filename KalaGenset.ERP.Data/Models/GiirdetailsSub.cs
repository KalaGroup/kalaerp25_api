using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class GiirdetailsSub
{
    public string Giircode { get; set; } = null!;

    public int SrNo { get; set; }

    public string Girdspocode { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public string SerialNo { get; set; } = null!;

    public string ControllerNo { get; set; } = null!;

    public string Krmno { get; set; } = null!;

    public string SerialStatus { get; set; } = null!;

    public string TrserialStatus { get; set; } = null!;

    public string M45status { get; set; } = null!;

    public string ConvertStatus { get; set; } = null!;

    public string Trfstatus { get; set; } = null!;

    public string Trfcode { get; set; } = null!;

    public string JobCardStatus { get; set; } = null!;

    public bool Gdiscard { get; set; }
}
