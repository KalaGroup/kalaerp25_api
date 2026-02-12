using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace KalaGenset.ERP.Core.Request
{
    public class SubmitSiteVisitDetailsRequest
    {
        public string comp_code { get; set; }
        public string user_code { get; set; }
        public string pca_code { get; set; }
        public string eng_sr_no { get; set; }
        public string dg_hour { get; set; }
        public string no_of_start { get; set; }
        public string work_date { get; set; }
        public string action_status { get; set; }
        public string problem_code { get; set; }
        public string problem_sub_code { get; set; }
        public string corrective_action { get; set; }
        public string preventive_action { get; set; }
        public string sign { get; set; }
        public string name { get; set; }
        public int photo_count { get; set; }
        public List<IFormFile> photos { get; set; } = new List<IFormFile>();
        public List<string> file_types { get; set; } = new List<string>();
    }
}
