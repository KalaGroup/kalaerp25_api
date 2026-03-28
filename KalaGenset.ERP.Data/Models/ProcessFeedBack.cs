using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class ProcessFeedBack
{
    public string GroupPfbcode { get; set; } = null!;

    public string Pfbcode { get; set; } = null!;

    public string MaxSrNo { get; set; } = null!;

    public DateTime Dt { get; set; }

    public DateTime? Edt { get; set; }

    public string Yr { get; set; } = null!;

    public string ProfitCenterCode { get; set; } = null!;

    public string MachineCode { get; set; } = null!;

    public string SerialNo { get; set; } = null!;

    public string CpyStageType { get; set; } = null!;

    public string CanopyPlanCode { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public string CanopyCode { get; set; } = null!;

    public string NestingForCode { get; set; } = null!;

    public double NestingForQty { get; set; }

    public string SupplierCode { get; set; } = null!;

    public string TurretKitCode { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public string Ppwcode { get; set; } = null!;

    public string Mofcode { get; set; } = null!;

    public string VersionCode { get; set; } = null!;

    public double ProcessQty { get; set; }

    public double PkitQty { get; set; }

    public double Plength { get; set; }

    public double Pwidth { get; set; }

    public double Pthickness { get; set; }

    public double NstWtPerUt { get; set; }

    public double NstSqftPerUt { get; set; }

    public double WtPerUt { get; set; }

    public double SqftPerUt { get; set; }

    public double Crwt { get; set; }

    public double Hrwt { get; set; }

    public double Crrate { get; set; }

    public double Hrrate { get; set; }

    public string CompanyCode { get; set; } = null!;

    public string Ppdirstatus { get; set; } = null!;

    public string Trstatus { get; set; } = null!;

    public string Qpcstatus { get; set; } = null!;

    public bool DivertStatus { get; set; }

    /// <summary>
    /// N=Normal Process,A= Assly
    /// </summary>
    public string Pfbtype { get; set; } = null!;

    public double Pfbrate { get; set; }

    public double SilCladdingRate { get; set; }

    public string SilCladdingStatus { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public string PrcBomcode { get; set; } = null!;

    public double PrcinvAmount { get; set; }

    public string PrcinvType { get; set; } = null!;

    public bool Checker1 { get; set; }

    public bool Active { get; set; }

    public bool Discard { get; set; }

    public bool Rdiscard { get; set; }

    public bool Auth { get; set; }

    public string? CatId { get; set; }
}
