namespace KalaGenset.ERP.Core.ResponseDTO
{
    /// <summary>
    /// One selectable company for the chart company-picker (from usp_GetChildCompanies).
    /// Shared by the 6M forms (machine / manpower).
    /// </summary>
    public class CompanyOptionDTO
    {
        public string CompanyCode { get; set; } = string.Empty;   // '01' / '03' / '28'
        public string CompanyName { get; set; } = string.Empty;   // Company.CName
        public string ShortName { get; set; } = string.Empty;     // Company.CAliseName
    }
}