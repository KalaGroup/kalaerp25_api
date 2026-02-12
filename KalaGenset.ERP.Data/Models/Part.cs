using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Part
{
    public string PartCode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string PartDesc { get; set; } = null!;

    public string AliseName { get; set; } = null!;

    public string MatType { get; set; } = null!;

    public DateTime MatDt { get; set; }

    public string ClassCode { get; set; } = null!;

    public string Uomcode { get; set; } = null!;

    public string ConvUomcode { get; set; } = null!;

    public double ConversionValue { get; set; }

    public string CategoryId { get; set; } = null!;

    public string Mob { get; set; } = null!;

    public string Mobint { get; set; } = null!;

    public string LocationCode { get; set; } = null!;

    public string DrwgNo { get; set; } = null!;

    public string RevNo { get; set; } = null!;

    public string Araino { get; set; } = null!;

    public double Length { get; set; }

    public double Length1 { get; set; }

    public double Length2 { get; set; }

    public double Width { get; set; }

    public double Width1 { get; set; }

    public double Width2 { get; set; }

    public double Height { get; set; }

    public double Thickness { get; set; }

    public double Weight { get; set; }

    public double SqFt { get; set; }

    public double LossWgt { get; set; }

    public double LossSqFt { get; set; }

    public double Kva { get; set; }

    public string Type { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string ModelSeries { get; set; } = null!;

    public string ModelGenset { get; set; } = null!;

    public string Make { get; set; } = null!;

    public string Hp { get; set; } = null!;

    public string Cylinder { get; set; } = null!;

    public string Start { get; set; } = null!;

    public string Capacity { get; set; } = null!;

    public string Coupling { get; set; } = null!;

    public string Asp { get; set; } = null!;

    public string ApplicationCode { get; set; } = null!;

    public string Phase { get; set; } = null!;

    public string FrameSize { get; set; } = null!;

    public string InsulationClass { get; set; } = null!;

    public string Housing { get; set; } = null!;

    public string Amps { get; set; } = null!;

    public string Poles { get; set; } = null!;

    /// <summary>
    /// Bending Strokes
    /// </summary>
    public string Pitch { get; set; } = null!;

    public string Wattage { get; set; } = null!;

    public string Rpm { get; set; } = null!;

    public string Sizes { get; set; } = null!;

    public string Gauge { get; set; } = null!;

    public string Cfm { get; set; } = null!;

    public string Voltage { get; set; } = null!;

    public string CalibrationDueOn { get; set; } = null!;

    public string Diameter { get; set; } = null!;

    public string Rating { get; set; } = null!;

    public string LeastCount { get; set; } = null!;

    public string One { get; set; } = null!;

    public int Two { get; set; }

    public int Three { get; set; }

    public int Four { get; set; }

    public int Five { get; set; }

    public int Six { get; set; }

    public int Seven { get; set; }

    public int Eight { get; set; }

    public int Nine { get; set; }

    public int Ten { get; set; }

    public int Eleven { get; set; }

    public int Twelve { get; set; }

    public int Thirteen { get; set; }

    public int Fourteen { get; set; }

    public int Fifteen { get; set; }

    public int Sixteen { get; set; }

    public int Seventeen { get; set; }

    public int Eighteen { get; set; }

    public int Nineteen { get; set; }

    public int DgconvUnit { get; set; }

    public bool Kit { get; set; }

    public string ChapterCode { get; set; } = null!;

    public string PurchaseHead { get; set; } = null!;

    public string SalesHead { get; set; } = null!;

    public string SalesStockTransferHead { get; set; } = null!;

    public string PurchaseStockTransferHead { get; set; } = null!;

    public int PrdParaId { get; set; }

    public double Gstperc { get; set; }

    public string Remark { get; set; } = null!;

    public string AuthRemark { get; set; } = null!;

    public bool Active { get; set; }

    public bool Discard { get; set; }

    public bool Auth { get; set; }

    public bool Status { get; set; }

    public bool WsStatusKalaToPms { get; set; }

    public bool KalaToBio { get; set; }

    public Guid MsreplTranVersion { get; set; }
}
