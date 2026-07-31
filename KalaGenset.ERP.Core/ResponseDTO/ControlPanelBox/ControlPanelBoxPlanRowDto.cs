namespace KalaGenset.ERP.Core.ResponseDTO.ControlPanelBox
{
    // One row returned by ControlPanelBox/GetPlanRowsByKva.
    // Represents a candidate Control Panel Box BOM for the operator-picked KVA.
    // The PartDesc value is already formatted "<Part.PartDesc>-->--<KitCode>"
    // by the SQL, so the UI can render it directly.
    public class ControlPanelBoxPlanRowDto
    {
        public string BOMCode  { get; set; } = string.Empty;
        public string PartDesc { get; set; } = string.Empty;
        public string KitCode  { get; set; } = string.Empty;
        public string UName    { get; set; } = string.Empty;
    }
}
