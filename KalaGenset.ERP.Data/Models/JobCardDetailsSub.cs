using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class JobCardDetailsSub
{
    public string JobCode { get; set; } = null!;

    public int SrNo { get; set; }

    public string PartCode { get; set; } = null!;

    public string SrNoPartCode { get; set; } = null!;

    public string SerialNo { get; set; } = null!;

    public string TransferCode { get; set; } = null!;

    public string TransferStatus { get; set; } = null!;

    public int Jpriority { get; set; }

    public string Stage1StartStatus { get; set; } = null!;

    public string Stage1StartPlay { get; set; } = null!;

    public DateTime? Stage1StartDate { get; set; }

    public string Stage1Status { get; set; } = null!;

    public string Stage1EndPlay { get; set; } = null!;

    public DateTime? Stage1Date { get; set; }

    public string Stage1Qastatus { get; set; } = null!;

    public string Stage2Status { get; set; } = null!;

    public DateTime? Stage2Date { get; set; }

    public string Stage3Status { get; set; } = null!;

    public DateTime? Stage3Date { get; set; }

    public string Stage3Qastatus { get; set; } = null!;

    public string JobCard2Status { get; set; } = null!;
}
