using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace KalaGenset.ERP.Core.Request
{
    public class UploadVideopdfDGAssemblyRequest
    {
        public string UploadFor { get; set; } 
        public string EngSrNo { get; set; }
        public IFormFile File { get; set; }
        public string EmpCode { get; set; }
        public int Id { get; set; } = 0;
    }
}
