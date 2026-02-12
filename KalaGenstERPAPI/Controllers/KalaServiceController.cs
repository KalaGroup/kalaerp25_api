using Azure.Core;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KalaServiceController : ControllerBase
    {
        private readonly IKalaService _kalaService;
        public KalaServiceController(IKalaService kalaService)
        {
            _kalaService = kalaService;
        }

        [HttpGet("GetPendingSiteVisits")]
        public async Task<IActionResult> GetPendingSiteVisit(string Ecode)
        {
            var result = await _kalaService.GetPendingSiteVisitAsync(Ecode);
            if (result == null || result.Count == 0)
                return NotFound("No data found.");
            return Ok(result);
        }

        [HttpPost("SubmitCustomerFeedback")]
        public async Task<IActionResult> SubmitCustomerFeedback([FromForm] KalaServiceCustomerFeedbackSubmitRequest submitCustomerFeedbackReq)
        {
            var result =  await _kalaService.SubmitCustomerFeedbackAsyc(submitCustomerFeedbackReq);
            return Ok(result);
        }

        [HttpPost("SubmitSiteVisitDetails")]
        public async Task<IActionResult> SubmitSiteVisitDetails([FromForm] SubmitSiteVisitDetailsRequest submitSiteVisitDetailsRequest)
        {          
            if (submitSiteVisitDetailsRequest.photos == null || submitSiteVisitDetailsRequest.photos.Count == 0)
            {
                return BadRequest(new { message = "No photos provided" });
            }

            if (submitSiteVisitDetailsRequest.file_types == null || submitSiteVisitDetailsRequest.file_types.Count == 0)
            {
                return BadRequest(new { message = "No file types provided" });
            }

            if (submitSiteVisitDetailsRequest.photos.Count != submitSiteVisitDetailsRequest.file_types.Count)
            {
                return BadRequest(new { message = $"Photo count ({submitSiteVisitDetailsRequest.photos.Count}) does not match file_types count ({submitSiteVisitDetailsRequest.file_types.Count})" });
            }

            var result = await _kalaService.SubmitSiteVisitDetailsAsync(submitSiteVisitDetailsRequest);
            return Ok(result);

        }
    }
}
