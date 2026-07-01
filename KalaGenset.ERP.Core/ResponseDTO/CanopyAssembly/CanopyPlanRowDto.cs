using System;

namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // One row of the Canopy Plan grid — used both for client → server submit
    // and for any future read-back endpoints.
    public class CanopyPlanRowDto
    {
        public DateTime Dt        { get; set; }                  // per-row production date
        public string PartCode    { get; set; } = string.Empty;  // KitCode (e.g. 4010301001530140001)
        public string PartDesc    { get; set; } = string.Empty;
        public string BomCode     { get; set; } = string.Empty;
        public double Qty         { get; set; }
        public double StkQty      { get; set; }
        public double PendQty     { get; set; }
    }
}
