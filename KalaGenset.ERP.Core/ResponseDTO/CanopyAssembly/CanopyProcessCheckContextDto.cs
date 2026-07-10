using System.Collections.Generic;

namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Full detail for one PSH record shown in the checker's modal.
    // Header + kit consumption + assembly-kit consumption + per-unit serials.
    public class CanopyProcessCheckContextDto
    {
        public CanopyProcessCheckHeaderDto Header { get; set; } = new();
        public List<CanopyProcessCheckKitLineDto>    KitLines         { get; set; } = new();
        public List<CanopyProcessCheckKitLineDto>    AssemblyKitLines { get; set; } = new();
        public List<CanopyProcessCheckSerialUnitDto> Units            { get; set; } = new();
    }

    public class CanopyProcessCheckHeaderDto
    {
        public string PFBCode      { get; set; } = string.Empty;
        public string GroupPFBCode { get; set; } = string.Empty;
        public string PlanCode     { get; set; } = string.Empty;
        public string Dt           { get; set; } = string.Empty;
        public string MachineCode  { get; set; } = string.Empty;   // Foam / RockWool
        public string SerialNo     { get; set; } = string.Empty;   // machine's serial
        public string ProductCode  { get; set; } = string.Empty;
        public string ProductDesc  { get; set; } = string.Empty;
        public string BOMCode      { get; set; } = string.Empty;
        public double KVA          { get; set; }
        public string Model        { get; set; } = string.Empty;
        public double BatchQty     { get; set; }
        public double PrcQty       { get; set; }
        public double Rate         { get; set; }
        public double WtPerUt      { get; set; }
        public double SqftPerUt    { get; set; }
        public string PCCode       { get; set; } = string.Empty;   // ProfitCenterCode (ParentDgPC)
        public string PCCode_Act   { get; set; } = string.Empty;   // LineWisePC
        public string MakerCode    { get; set; } = string.Empty;
        public string Remark       { get; set; } = string.Empty;
    }

    // Reused for both Panel B (kit lines) and Panel C (assembly kit lines).
    public class CanopyProcessCheckKitLineDto
    {
        public int    SrNo     { get; set; }
        public string PartCode { get; set; } = string.Empty;
        public string PartDesc { get; set; } = string.Empty;
        public double KitQty   { get; set; }
        public double TotQty   { get; set; }
        public double Rate     { get; set; }
    }

    // One per-unit row in Panel D (the check surface).
    public class CanopyProcessCheckSerialUnitDto
    {
        public int    SrNo      { get; set; }
        public string SerialNo  { get; set; } = string.Empty;
        public string BFMSrNo   { get; set; } = string.Empty;
        public string FLKSrNo   { get; set; } = string.Empty;
        public string Status    { get; set; } = string.Empty;   // 'P' / 'D'
        public string QPCStatus { get; set; } = string.Empty;   // 'P' / 'D' / 'RW' / 'R'
        public string RWStatus  { get; set; } = string.Empty;
    }
}
