namespace KalaGenset.ERP.Core.ResponseDTO.ControlPanelBox
{
    /// <summary>
    /// Output of POST /api/ControlPanelBox/SubmitPlan.
    /// Mirrors the shape used by CanopyAssembly/SubmitCanopyPlan so the
    /// Angular save handler can reuse the same success-modal pattern.
    /// </summary>
    public class SubmitControlPanelBoxPlanResponse
    {
        public string Message { get; set; } = string.Empty;
        public string CPCode  { get; set; } = string.Empty;
    }
}
