using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.DTO
{
    public class MenuDto
    {
        //public string FormType { get; set; }

        //public string FormName { get; set; }

        //public string ProfitCenter { get; set; }

        //public string RoutePath { get; set; }

        public string? Ecode { get; set; }

        public string? Role_Group { get; set; }

        public int FromTypeID { get; set; }

        public string? FromTypeName { get; set; }

        public string? ERPType { get; set; }

        public int EPDID { get; set; }

        public string? PageTittle { get; set; }

        public string? PageURL { get; set; }

        public int PageType { get; set; }

        public int PageTypeNewERP { get; set; }

        public string? PageTypeNewERPMenuName { get; set; }

        public int DivisionId { get; set; }

        public string? Division { get; set; }

    }
}
