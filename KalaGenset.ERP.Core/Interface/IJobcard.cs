using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request.Jobcard;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IJobcard
    {
        public Task<List<Dictionary<string, object>>> GetDGAsync(string strJobCardType, string strCompID);

        public Task<string> SubmitJobCardAsync(JobCardSubmitRequest request);
    }
}
