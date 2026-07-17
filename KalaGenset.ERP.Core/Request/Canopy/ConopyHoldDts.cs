using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.Canopy
{

    /// <summary>
    /// Grid row returned by GetconopyHold proc
    /// (legacy Obout grid columns: CPCode, Dt, KVA, Model, PartDesc, partcode, Batch).
    /// </summary>
    public class Canopy_JobCardHoldRequest
    {

        public string EmpCode { get; set; }
        public string CompCode { get; set; }
        public string PCCode { get; set; }
        /// Replaces the legacy "HoldDetails" @@#@@ / @#@ delimited string.
        public List<Canopy_JobCardHoldDetail> Details { get; set; }
    }

    public class Canopy_JobCardHoldDetail
    {
        public string CPCode { get; set; }
        public string Partcode { get; set; }
        public string InActiveRemark { get; set; }
    }
}

