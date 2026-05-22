using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class QualityCheckListItemDto
    {
        public int     StageWiseQcdetailId      { get; set; }
        public int     SrNo                     { get; set; }
        public string  SubAssemblyPart          { get; set; }
        public string  QualityProcessCheckpoint { get; set; }
        public string  Specification            { get; set; }
        public string  Observation              { get; set; }
        public string  OkNok                    { get; set; }
    }
}
