using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Bomdetail
{
    public string Bomcode { get; set; } = null!;

    public int SrNo { get; set; }

    public string PartCode { get; set; } = null!;

    public double Qty { get; set; }

    public double Weight { get; set; }

    public double SqFt { get; set; }

    public double TotalWt { get; set; }

    public double TotalSqFt { get; set; }

    public string PrcWithRate { get; set; } = null!;

    public double PrcCostUnitKg { get; set; }

    public double TotalPrcCostUnitKg { get; set; }

    public double PrcCostUnitSqFt { get; set; }

    public double TotalPrcCostUnitSqFt { get; set; }

    public double AsblyCostPerUnit { get; set; }

    public double TotalAsblyCost { get; set; }

    public double Kgrate { get; set; }

    public double SqFtRate { get; set; }

    public double AssblyRate { get; set; }

    public double SuppRate { get; set; }

    public double Length { get; set; }

    public double Width { get; set; }

    public double Thickness { get; set; }

    public double LossWgt { get; set; }

    public double LossSqft { get; set; }

    public string Mob { get; set; } = null!;

    public string Kitcode { get; set; } = null!;

    public string CategoryId { get; set; } = null!;

    public double ScrapQty { get; set; }

    public double ScrapAmt { get; set; }

    public bool Status { get; set; }

    public bool WsStatusKalaToPms { get; set; }

    public bool KalaToBio { get; set; }

    public string? NestingStatus { get; set; }

    public string NestingLstatus { get; set; } = null!;

    public string? NestingCpystatus { get; set; }

    public string? NestingBfstatus { get; set; }

    public string? NestingBftstatus { get; set; }

    public string? NestingFtstatus { get; set; }

    public double MatCost { get; set; }

    public double PrcCostKg { get; set; }

    public double PrcCostSqft { get; set; }

    public double SteelRate { get; set; }

    public string PkitCode { get; set; } = null!;

    public string RoomType { get; set; } = null!;

    public string MatType { get; set; } = null!;
}
