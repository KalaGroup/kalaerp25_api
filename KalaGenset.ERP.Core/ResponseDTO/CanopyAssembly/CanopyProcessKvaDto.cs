namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // KVA list for the selected machine + line. KVA1 is the value bound to the
    // dropdown; KVA is the display text. Legacy filters by KVA tier per PC
    // (< 82.5 for 01.005, >= 82.5 for 03.038, no filter for 28.017).
    public class CanopyProcessKvaDto
    {
        public string KVA  { get; set; } = string.Empty;   // display
        public string KVA1 { get; set; } = string.Empty;   // value
    }
}
