using System.Collections.Generic;

namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Full payload returned by Search:
    //   - BomCode (legacy txtBomCode)
    //   - Master rate/amount/weight/sqft (legacy txtOverAllRate etc.)
    //   - CR/HR weight + rate sidebar (legacy txtCRWt/txtHRWt/txtCRRate/txtHRRate)
    //   - Grid rows from sp_GetFlatPackProcessDetails*
    public class FlatPackProcessDetailsResponse
    {
        public string BomCode { get; set; } = string.Empty;
        public double OverallRate   { get; set; }
        public double OverallAmount { get; set; }
        public double Wt { get; set; }
        public double SqFt { get; set; }
        public double CRWt { get; set; }
        public double HRWt { get; set; }
        public double CRRate { get; set; }
        public double HRRate { get; set; }
        public List<FlatPackPartDetailRow> PartDetails { get; set; } = new();
    }
}
