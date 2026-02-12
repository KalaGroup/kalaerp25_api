using KalaGenset.ERP.Core.DTO;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Emit;
using System.Security.Claims;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAuthenticationController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        public UserAuthenticationController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpPost("LoginUser")]
        public IActionResult LoginUser([FromBody] User Model)
        {
            var result = _employeeService.AuthenticateUser(Model.UserId, Model.Password);

            if (!result.success)
            {
                return BadRequest(result);
            }

            return Ok(result);     
        }
        [HttpGet("Validate-Token")]
        public IActionResult ValidateToken()
        {
              var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (_employeeService.ValidateToken(token, out var message))
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                // Extracting the username from the token
                var EmpCode = jwtToken.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sub)?.Value;


                return Ok(new { EmpCode });
            }

            return Unauthorized(new { message });
        }

        [HttpGet]
        [Route("GetMenu")]
        [Authorize]
        public async Task<IActionResult> GetMenu()
        {
            string empCode = "";
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (_employeeService.ValidateToken(token, out var message))
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                // Extracting the empcode from the token
                empCode = jwtToken.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sub)?.Value;
            }

            var menus = await _employeeService.GetMenuData(empCode);
            return Ok(menus);
        }

    }
}
