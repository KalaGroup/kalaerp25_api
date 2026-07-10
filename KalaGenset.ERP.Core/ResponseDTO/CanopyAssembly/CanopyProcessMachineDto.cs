namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Row returned by SP LoadMachine — for Canopy PCs this is a small hardcoded
    // list (Foam / RockWool) with PartCode carrying a "<name>--><serial>" concat.
    public class CanopyProcessMachineDto
    {
        public string AMCode   { get; set; } = string.Empty;
        public string Part     { get; set; } = string.Empty;
        public string PartCode { get; set; } = string.Empty;   // "Foam-->Foam1"
    }
}
