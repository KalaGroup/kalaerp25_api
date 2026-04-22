using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using KalaGenset.ERP.Core.DTO;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Response;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace KalaGenset.ERP.Core.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly KalaDbContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;

        public EmployeeService(KalaDbContext context, IMapper mapper, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
            _secretKey = configuration["JwtSettings:SecretKey"];
        }

        public AuthResponse AuthenticateUser(string userid, string password)
        {
            var user = (from emp in _context.Employees
                        join login in _context.LoginMsts
                        on emp.Ecode equals login.Name
                        join comp in _context.Companies
                        on emp.CompanyCodeAct equals comp.Cid into compGroup
                        from comp in compGroup.DefaultIfEmpty()
                        join pcenter in _context.ProfitCenters
                        on emp.ProfitCenterAct equals pcenter.Pccode into pcGroup
                        from pcenter in pcGroup.DefaultIfEmpty()
                        where emp.Ecode == userid && login.PassWord == password
                        select new UserDto
                        {
                            EmpCode = emp.Ecode,
                            UserId = login.Id,
                            LoginType = login.LoginType,
                            Ename = emp.Fname + " " + emp.Lname,
                            PCCode_Old = emp.ProfitCenter ?? "",
                            PCCode = emp.ProfitCenterAct ?? "",
                            CompanyId = emp.CompanyCodeAct ?? "",
                            CompanyName = comp != null ? comp.Cname : "",
                            ProfitCenterName = pcenter != null ? pcenter.Pcname : "",
                            IsActive = emp.Active
                        }).FirstOrDefault();

            if (user == null)
            {
                return new AuthResponse
                {
                    success = false,
                    message = "Invalid User Or Password..!"
                };
            }
            else if (!user.IsActive)
            {
                return new AuthResponse
                {
                    success = false,
                    message = "The User Is Either Inactive or Invalid..!"
                };
            }
            {
                var _token = GenerateJwtToken(user.EmpCode);
                return new AuthResponse
                {
                    token = _token,
                    username = user.Ename,
                    pccode = user.PCCode ?? "",
                    pccode_old = user.PCCode_Old ?? "",
                    empCode = user.EmpCode,
                    loginType = user.LoginType,
                    userId = user.UserId,
                    companyId = user.CompanyId ?? "",
                    companyName = user.CompanyName ?? "",
                    profitCenterName = user.ProfitCenterName ?? "",
                    success = true,
                    message = "Login Successful..!"
                };
            }
        }

        private string GenerateJwtToken(string empCode)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, empCode),
            //new Claim("RoleId", roleId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);

        }


        public bool ValidateToken(string token, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrEmpty(token))
            {
                message = "Token is missing";
                return false;
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false, // Set to true if you want to validate the issuer
                    ValidateAudience = false, // Set to true if you want to validate the audience
                    //ClockSkew = TimeSpan.Zero // Optional: reduce the default clock skew
                }, out SecurityToken validatedToken);

                return true; // Token is valid
            }
            catch (SecurityTokenExpiredException)
            {
                message = "Token has expired";
                return false;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                message = "Invalid token signature";
                return false;
            }
            catch (Exception)
            {
                message = "Token is invalid";
                return false;
            }
        }

        public async Task<IEnumerable<MenuDto>> GetMenuData(string empcode)
        {
            // Define your parameter
            // var employeeCodeParam = new SqlParameter("@EmployeeCode", empcode);

            // Execute the stored procedure and map results to a model (e.g., ProfitCenterMenu)
            // var results = await _context.Database
            //  .SqlQueryRaw<MenuDto>("EXEC GetAllProfitCenterMenusERP25 @EmployeeCode", employeeCodeParam)
            //  .ToListAsync();

            // return results;

            // ✅ REPLACE: Define all 7 parameters for getTreeMenuNewERP
            var parameters = new[]
            {
                 new SqlParameter("@Param1", "ERP25"),       
                 new SqlParameter("@Param2", ""),            
                 new SqlParameter("@Param3", empcode),       
                 new SqlParameter("@Param4", ""),            
                 new SqlParameter("@Param5", ""),            
                 new SqlParameter("@Param6", ""),             
                 new SqlParameter("@Param7", "")             
            };

            // ✅ REPLACE: Execute the new stored procedure
            var results = await _context.Database
                .SqlQueryRaw<MenuDto>("EXEC getTreeMenuNewERP @Param1, @Param2, @Param3, @Param4, @Param5, @Param6, @Param7", parameters)
                .ToListAsync();

            return results;
        }
    }
}
