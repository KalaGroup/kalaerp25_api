namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    public class SaveCanopyPlanCheckResponse
    {
        public string Message   { get; set; } = string.Empty;
        public string CPCode    { get; set; } = string.Empty;
        public string PlanStatus{ get; set; } = string.Empty;   // final DB status after save
    }
}
