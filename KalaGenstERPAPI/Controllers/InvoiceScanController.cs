using KalaGenset.ERP.Core.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceScanController : ControllerBase
    {
        private readonly I_invoiceScan _invoiceScanService;

        public InvoiceScanController(I_invoiceScan invoiceScanService)
        {
            _invoiceScanService = invoiceScanService;
        }

        [HttpGet("GetInvoiceScanDts/{strInvSrNo}")]
        public async Task<IActionResult> GetInvoiceScanDts(string strInvSrNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(strInvSrNo))
                    return BadRequest("InvoiceId is required.");

                string decodedInvoiceId = Uri.UnescapeDataString(strInvSrNo).Trim();

                var result = await _invoiceScanService.GetScanDtsInvAsync(decodedInvoiceId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpPost("Submit/{invoiceId}")]
        public async Task<IActionResult> Submit(string invoiceId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(invoiceId))
                    return BadRequest("InvoiceId is required.");

                string decodedInvoiceId = Uri.UnescapeDataString(invoiceId).Trim();

                var result = await _invoiceScanService.SubmitAsync(decodedInvoiceId);
                string mailStatus = await _invoiceScanService.SendEmailAsync(invoiceId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpGet("SendMail/{invoiceId}")]
        public async Task<IActionResult> SendMail(string invoiceId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(invoiceId))
                    return BadRequest("InvoiceId is required.");

                string decodedInvoiceId = Uri.UnescapeDataString(invoiceId).Trim();

                var result = await _invoiceScanService.SendEmailAsync(decodedInvoiceId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }
    }
}
