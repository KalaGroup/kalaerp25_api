using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.DTO
{
    public class UserDto
    {
        public string LoginType { get; set; }
        public int EmpId { get; set; }
        public string EmpCode { get; set; }
        public string Ename { get; set; }
        public string PCCode { get; set; }
        public string PCCode_Old { get; set; }
        public int UserId { get; set; }
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string ProfitCenterName { get; set; }
        public bool IsActive { get; set; }
       
    }
}
