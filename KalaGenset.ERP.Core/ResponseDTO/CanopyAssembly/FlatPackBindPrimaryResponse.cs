namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Output of the cascade: given (canopyPartCode, processType) the server
    // returns the "Part Desc" textbox value (PartDesc-->PartCode), which the
    // UI then parses to feed into the Search step.
    public class FlatPackBindPrimaryResponse
    {
        public string PartDesc { get; set; } = string.Empty;   // "<desc>-->" + PartCode
        public string PartCode { get; set; } = string.Empty;   // raw for caller convenience
        public string Heading  { get; set; } = string.Empty;   // mirrors legacy lblU1U4 (URL.ULHeading)
    }
}
