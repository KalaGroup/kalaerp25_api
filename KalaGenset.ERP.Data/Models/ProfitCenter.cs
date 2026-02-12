using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class ProfitCenter
{
    public int PcId { get; set; }

    public string Pccode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Pcname { get; set; } = null!;

    public string PcaliseName { get; set; } = null!;

    public double Pcrate { get; set; }

    public string CompanyCode { get; set; } = null!;

    public int SrNo { get; set; }

    public string Remark { get; set; } = null!;

    public string HodmailId { get; set; } = null!;

    public string Hodecode { get; set; } = null!;

    public double Area { get; set; }

    public bool Active { get; set; }

    public bool Kit { get; set; }

    public bool Auth { get; set; }

    public bool Discard { get; set; }

    public bool Status { get; set; }

    public bool WsStatusKalaToPms { get; set; }

    public bool KalaToBio { get; set; }

    public int StateId { get; set; }
}
