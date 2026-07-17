using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class CanopyPlanSerialNo
{
    public string Cpcode { get; set; } = null!;

    public int SrNo { get; set; }

    public string PartCode { get; set; } = null!;

    public string SerialNo { get; set; } = null!;

    public string BfmsrNo { get; set; } = null!;

    public string FlksrNo { get; set; } = null!;

    public string CptserialStatus { get; set; } = null!;

    public string CpbserialStatus { get; set; } = null!;

    public string CpfserialStatus { get; set; } = null!;

    public string CppcserialStatus { get; set; } = null!;

    public string CpfpserialStatus { get; set; } = null!;

    public string CpyserialStatus { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string Trfstatus { get; set; } = null!;

    public string Trfcode { get; set; } = null!;

    public string Qpcstatus { get; set; } = null!;

    public string Rwstatus { get; set; } = null!;

    public string JobCardStatus { get; set; } = null!;
}
