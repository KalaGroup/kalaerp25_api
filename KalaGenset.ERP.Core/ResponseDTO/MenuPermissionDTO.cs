using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.DTO
{
    public class PagePermissionDTO
    {
        public int PageId { get; set; }
        public string PageName { get; set; }
       // public int Pcid { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
        public bool Auth { get; set; }
        public bool Export { get; set; }
    }

}
