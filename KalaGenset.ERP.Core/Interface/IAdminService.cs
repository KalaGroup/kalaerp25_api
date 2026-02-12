using KalaGenset.ERP.Core.DTO;
using KalaGenset.ERP.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Interface
{
    public interface IAdminService
    {
        public IEnumerable<PagePermissionDTO> GetProfitCenterPermissions(int PCID);
      //  public IEnumerable<PagePermissionDTO> GetProfitCenterPermissionsByPCID(IEnumerable<int> PCIDs);

        public IEnumerable<EmployeeDTO> FetchEmployees();

        public IEnumerable<RoleDetailsDTO>FetchRoles(int profitCenterId);

        public IEnumerable<DTO.CompanyDTO>FetchCompanyDetails();
        public IEnumerable<ProfitCenterDTO> GetProfitCentersDetails();

        public IEnumerable<ProfitCenterDTO>FetchProfitCenterDetails(int companyId);
        Task<string> SaveUserRightsAsync(List<UserRightsInputDTO> pageData);

        public IEnumerable<ProfitCenterDTO> FetchProfitCentersByCompanyCode(string[] companyCodes);

        public Task<int> InsertUserRoleInfo(UserRoleDTO userDto);
       // public Task<int> AddEmployeeAsync(EmpMstDTO employeeDto, byte[]? photoCopy);

        public IEnumerable<RoleProfitcenterMapDTO>GetRolesByProfitCenterIdAndCompanyId(int CID, int PCID);

       public Task<List<PagePermissionDto>> GetPermittedPages(int pcId, int roleId);
      public  Task<bool> UpdatePagePermissionsAsync(List<PagePermissionUpdateDto> updates);

    }
}
