using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class ProcessFeedbackDetailsSub
{
    public string Pfbcode { get; set; } = null!;

    public DateTime? EdtD { get; set; }

    public DateTime? Pdirdt { get; set; }

    public int SrNo { get; set; }

    public string PartCode { get; set; } = null!;

    public string SerialNo { get; set; } = null!;

    public string BfmsrNo { get; set; } = null!;

    public string FlksrNo { get; set; } = null!;

    public string PfbbotserialNo { get; set; } = null!;

    public string Ecode { get; set; } = null!;

    public string Trfstatus { get; set; } = null!;

    public string Trfcode { get; set; } = null!;

    public string TrserialStatus { get; set; } = null!;

    public string Qpcstatus { get; set; } = null!;

    public string Rwstatus { get; set; } = null!;

    public string ProcStatus { get; set; } = null!;

    public string JobCardStatus { get; set; } = null!;

    public string ConvertStatus { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string Pdirstatus { get; set; } = null!;

    public string Pdirrwstatus { get; set; } = null!;

    public string Pcstatus { get; set; } = null!;

    public string Castatus { get; set; } = null!;
}
