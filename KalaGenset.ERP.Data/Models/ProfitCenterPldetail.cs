using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class ProfitCenterPldetail
{
    public string ProfitCenterCode { get; set; } = null!;

    public DateTime SysDt { get; set; }

    public int SrNo { get; set; }

    public string PartCode { get; set; } = null!;

    public double PurRate { get; set; }

    public double Rate { get; set; }

    public double SaleRate { get; set; }

    public string Pltype { get; set; } = null!;

    public string Bomcode { get; set; } = null!;

    public bool Auth { get; set; }

    public bool Sauth { get; set; }

    public double Pwt { get; set; }

    public double PsqFt { get; set; }

    public bool Status { get; set; }

    public bool WsStatusKalaToPms { get; set; }

    public double MatRate { get; set; }

    public double PrcRate { get; set; }

    public double Crwt { get; set; }

    public double Hrwt { get; set; }

    public double ScrapRate { get; set; }

    public double Cramt { get; set; }

    public double Ohper { get; set; }

    public double Ohamt { get; set; }

    public double Hramt { get; set; }

    public double ProfitPer { get; set; }

    public double PrfAmt { get; set; }

    public double AsslyAmt { get; set; }

    public double MarPer { get; set; }

    public double MarAmt { get; set; }

    public double WastEmpPer { get; set; }

    public double WastEmpAmt { get; set; }
}
