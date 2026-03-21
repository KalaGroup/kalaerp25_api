using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class ConvertSerialNoDetail
{
    public string Cnvcode { get; set; } = null!;

    public int SrNo { get; set; }

    public string PartCode { get; set; } = null!;

    public string SerialNo { get; set; } = null!;

    public string Giircode { get; set; } = null!;

    public string SerialStatus { get; set; } = null!;

    /// <summary>
    /// P- Pending Q - Quality C- PV I - INvoice D - Done
    /// </summary>
    public string TrserialStatus { get; set; } = null!;

    public string MtfserialStatus { get; set; } = null!;

    public string Cmtfcode { get; set; } = null!;

    public string JobCardStatus { get; set; } = null!;
}
