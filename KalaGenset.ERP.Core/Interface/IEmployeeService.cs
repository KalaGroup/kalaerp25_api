using KalaGenset.ERP.Core.DTO;
using KalaGenset.ERP.Core.Response;
using KalaGenset.ERP.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IEmployeeService
    {
        public AuthResponse AuthenticateUser(string username, string password);

        bool ValidateToken(string token, out string message);

        public Task<IEnumerable<MenuDto>> GetMenuData(string empcode);

    }
}
