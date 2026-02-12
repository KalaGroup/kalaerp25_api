using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Jobcard2DetailsSub
{
    public string JobCode { get; set; } = null!;

    public int SrNo { get; set; }

    public string PartCode { get; set; } = null!;

    public int PanelType { get; set; }

    public string JobCard1 { get; set; } = null!;

    public string TransCode { get; set; } = null!;

    public string Stage3Status { get; set; } = null!;

    public string SrNoPartCode { get; set; } = null!;

    public string SerialNo { get; set; } = null!;

    public int J2priority { get; set; }

    public string PrcStatus { get; set; } = null!;

    public string Trstatus { get; set; } = null!;

    public string Pdirstatus { get; set; } = null!;
}
