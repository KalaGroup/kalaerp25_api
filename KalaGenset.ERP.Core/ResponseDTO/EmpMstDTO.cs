using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.DTO
{
    public class EmpMstDTO
    {
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public bool Active { get; set; }
        
        public int? GradeID { get; set; }
        public long? DesignationID { get; set; }
        public string CCode { get; set; }
        public string PCCode { get; set; }
        public DateTime? JoinDate { get; set; }
        public int? EmployeeType { get; set; }
        public int? Contractor { get; set; }
        public int? WorkDesignation { get; set; }
        public bool IsOvertime { get; set; }
        public bool ByOfferLetter { get; set; }
        public bool TempEmp { get; set; }
       //public byte[]? PhotoCopy { get; set; }

    }
}
