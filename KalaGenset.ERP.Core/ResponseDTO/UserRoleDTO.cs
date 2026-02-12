using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.DTO
{
    public class UserRoleDTO
    {
        public int EmpId { get; set; }
        public List<int> RoleIds { get; set; }
        public List<int> CompanyIds { get; set; }
        public List<int> ProfitCenterIds { get; set; }
    }
}
