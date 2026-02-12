using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class MtfdetailsSub
{
    public string Mtfcode { get; set; } = null!;

    public int SrNo { get; set; }

    public string PartCode { get; set; } = null!;

    public string SerialNo { get; set; } = null!;

    public string Trfstatus { get; set; } = null!;

    public string TrserialStatus { get; set; } = null!;

    public string Qastatus { get; set; } = null!;

    public string JobCardStatus { get; set; } = null!;

    public string AutoclaveStatus { get; set; } = null!;

    public string Status { get; set; } = null!;
}
