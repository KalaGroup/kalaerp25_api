using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class ActionTaken
{
    public string Actno { get; set; } = null!;

    public DateTime Dt { get; set; }

    public DateTime SolvedDt { get; set; }

    public string MaxSrNo { get; set; } = null!;

    public string Yr { get; set; } = null!;

    public string Pcacode { get; set; } = null!;

    public string EngSrNo { get; set; } = null!;

    public string GenSrNo { get; set; } = null!;

    public double Dghours { get; set; }

    public double NosOfStart { get; set; }

    public string ActionStatus { get; set; } = null!;

    public DateTime NextDt { get; set; }

    public DateTime? CommissioningDt { get; set; }

    public string Remark { get; set; } = null!;

    public string FeedBackStatus { get; set; } = null!;

    public string QualityStatus { get; set; } = null!;

    public string MailStatus { get; set; } = null!;

    public string Invstatus { get; set; } = null!;

    public string MailCc { get; set; } = null!;

    public DateTime? CustAckDt { get; set; }

    public string CustAckName { get; set; } = null!;

    public string CustAckSign { get; set; } = null!;

    public bool Active { get; set; }

    public bool Discard { get; set; }
}
