namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Resolves the active plan header for (machine, KVA, model, line). Reads
    // the top-1 ProcessFeedback row with EDt IS NULL (matching legacy Mode-3
    // of getCpyPrcddl). Called after the user picks Model in the cascade.
    public class CanopyProcessPlanContextDto
    {
        public string KVAMod       { get; set; } = string.Empty;   // "82.5-->KG82.5AS"
        public string KVA          { get; set; } = string.Empty;
        public string Model        { get; set; } = string.Empty;
        public string CPCode       { get; set; } = string.Empty;   // CanopyPlanCode
        public string Dt           { get; set; } = string.Empty;
        public string Partcode     { get; set; } = string.Empty;   // ProductCode (canopy)
        public string Part         { get; set; } = string.Empty;   // "PartDesc-->PartCode"
        public double CPQty        { get; set; }
        public double PlanQtyBal   { get; set; }
        public double PrcQty       { get; set; }
        public string PFBCode      { get; set; } = string.Empty;   // "NEW/..." or "PSH/..." or "0"
        public string EDt          { get; set; } = string.Empty;
        public string BOMCode      { get; set; } = string.Empty;   // TurretKitCode
        public string SCode        { get; set; } = string.Empty;
    }
}
