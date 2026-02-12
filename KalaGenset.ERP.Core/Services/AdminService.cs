using KalaGenset.ERP.Core.DTO;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Data.DbContexts;
using KalaGenset.ERP.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Services
{
    public class AdminService : IAdminService
    {
        private readonly KalaDbContext _context;
        public AdminService(KalaDbContext context)
        {
            _context = context;
        }

        public IEnumerable<PagePermissionDTO> GetProfitCenterPermissions(int PCID)
        {
            //var result = (from page in _context.PageMsts
            //              join rolePermission in _context.RolePermissions
            //              on page.PageId equals rolePermission.PageId into rolePermissionsGroup
            //              from rp in rolePermissionsGroup.Where(rp => rp.Pcid == PCID).DefaultIfEmpty()
            //              select new PagePermissionDTO
            //              {
            //                  PageId = page.PageId,
            //                  PageName = page.PageName,
            //                  Add = rp != null && rp.Add.HasValue ? rp.Add.Value : false,
            //                  Edit = rp != null && rp.Edit.HasValue ? rp.Edit.Value : false,
            //                  Delete = rp != null && rp.Delete.HasValue ? rp.Delete.Value : false,
            //                  Auth = rp != null && rp.Auth.HasValue ? rp.Auth.Value : false,
            //                  Export = rp != null && rp.Export.HasValue ? rp.Export.Value : false
            //              }).ToList();
            return null;
            //return result;
        }


        public async Task<string> SaveUserRightsAsync(List<UserRightsInputDTO> pageData)
        {
            if (pageData == null || !pageData.Any())
            {
                return "No data received.";
            }

            foreach (var item in pageData)
            {
                // Find the existing RolePermission record based on RoleId and MenuId
                var existingRolePermission = new RolePermission();
                //await _context.RolePermissions
                //.FirstOrDefaultAsync(rp => rp.Pcid == item.Pcid && rp.PageId == item.PageId);

                if (existingRolePermission != null)
                {
                    // Update the existing RolePermission record
                    existingRolePermission.Add = item.Add;
                    existingRolePermission.Edit = item.Edit;
                    existingRolePermission.Delete = item.Delete;
                    existingRolePermission.Auth = item.Auth;
                    existingRolePermission.Export = item.Export;

                   // _context.RolePermissions.Update(existingRolePermission); // Update the record
                }
                else
                {
                    var newRolePermission = new RolePermission
                    {
                        Pcid = item.Pcid,
                        PageId = item.PageId,
                        Add = item.Add,
                        Edit = item.Edit,
                        Delete = item.Delete,
                        Auth = item.Auth,
                        Export = item.Export
                    };

                   // _context.RolePermissions.Add(newRolePermission); // Add new record
                                                                    
                }
            }

            // Save the changes to the database
            await _context.SaveChangesAsync();
            return "Role permissions updated successfully.";
        }

        public IEnumerable<EmployeeDTO> FetchEmployees()
        {
            var result = from emp in _context.Employees
                         select new EmployeeDTO
                         {
                             
                             EmpCode = emp.Ecode,
                             Ename = emp.Fname
                         };
            return result.ToList();
        }

        public IEnumerable<RoleDetailsDTO> FetchRoles(int profitCenterId)
        {
            //return (from mapping in _context.RoleProfitCenterMappings
            //        join role in _context.RoleMsts on mapping.RoleId equals role.RoleId
            //        where mapping.Pcid == profitCenterId
            //        select new RoleDetailsDTO
            //        {
            //            RoleId = role.RoleId,
            //            RoleName = role.RoleName
            //        })
            //        .Distinct()
            //        .ToList();
            return null;
        }


        public IEnumerable<CompanyDTO> FetchCompanyDetails()
        {
            var result = from company in _context.Companies
                         select new CompanyDTO
                         {
                             //CID = company.Cid,
                             CCode = company.Ccode,
                             CName = company.Cname
                         };
            return result.ToList();
        }

        public IEnumerable<ProfitCenterDTO> GetProfitCentersDetails()
        {
            return _context.ProfitCenters
                           .Select(pc => new ProfitCenterDTO
                           {
                               PCID = pc.PcId,
                               Pcname = pc.Pcname
                           }).ToList();
        }

        public IEnumerable<ProfitCenterDTO> FetchProfitCenterDetails(int CompanyId)
        {

            //var result = from mapping in _context.RoleProfitCenterMappings
            //             where mapping.Cid == CompanyId  // Filter RoleProfitCenterMappings by CID (CompanyId)
            //             join profitCenter in _context.ProfitCenters on mapping.Pcid equals profitCenter.PcId  // Join with ProfitCenters on PCID
            //             select new ProfitCenterDTO
            //             {
            //                 PCID = profitCenter.PcId,
            //                 Pcname = profitCenter.Pcname,
            //             };
            //return result.ToList();
            return null;
        }

        public IEnumerable<ProfitCenterDTO> FetchProfitCentersByCompanyCode(string[] companyCodes)
        {
            var result = from profitcenter in _context.ProfitCenters
                         where companyCodes.Contains(profitcenter.CompanyCode)
                         select new ProfitCenterDTO
                         {
                             PCID = profitcenter.PcId,
                             Pcname = profitcenter.Pcname,
                             PCCode= profitcenter.Pccode,
                             CompanyCode = profitcenter.CompanyCode 
                         };

            return result.ToList();
        }

        public async Task<int> InsertUserRoleInfo(UserRoleDTO userDto)
        {
            // Generate all combinations of RoleId, Cid, and Pcid for the EmpId
            var userRoles = (
                from roleId in userDto.RoleIds
                from cid in userDto.CompanyIds
                from pcid in userDto.ProfitCenterIds
                select new UserRole
                {
                    EmpId = userDto.EmpId,
                    RoleId = roleId,
                    Cid = cid,
                    Pcid = pcid
                }).ToList();

            // Add all records to the database
          // await _context.UserRoles.AddRangeAsync(userRoles);

            // Save changes to the database
            return await _context.SaveChangesAsync();
        }

        //public async Task<int> AddEmployeeAsync(EmpMstDTO employeeDto, byte[]? photoCopy)
        //{
        //    if (employeeDto == null) throw new ArgumentNullException(nameof(employeeDto));

        //    var employee = new EmpMst
        //    {
        //        Ename = $"{employeeDto.FirstName} {employeeDto.MiddleName} {employeeDto.LastName}".Trim(),
        //        Ename = $"{employeeDto.FirstName} {(string.IsNullOrWhiteSpace(employeeDto.MiddleName) ? "" : employeeDto.MiddleName + " ")}{employeeDto.LastName}".Trim(),
        //        Active = employeeDto.Active,
        //        Password = "Pass@123",
        //        GradeId = employeeDto.GradeID,
        //        DesignationId = employeeDto.DesignationID,
        //        Ccode = employeeDto.CCode,
        //        Pccode = employeeDto.PCCode,
        //        JoinDate = employeeDto.JoinDate.HasValue ? DateOnly.FromDateTime(employeeDto.JoinDate.Value) : null,
        //        EmployeeType = employeeDto.EmployeeType,
        //        Contractor = employeeDto.Contractor,
        //        WorkDesignation = employeeDto.WorkDesignation,
        //        IsOvertime = employeeDto.IsOvertime,
        //        ByOfferLetter = employeeDto.ByOfferLetter,
        //        TempEmp = employeeDto.TempEmp,
        //        PhotoCopy = photoCopy
        //    };

        //    //_context.Employees.Add(employee);
        //    //await _context.SaveChangesAsync();

        //    return employee.EmpId;
        //}

        public IEnumerable<RoleProfitcenterMapDTO> GetRolesByProfitCenterIdAndCompanyId(int CID, int PCID)
        {
            //var result = from mapping in _context.RoleProfitCenterMappings
            //             where mapping.Pcid == PCID && mapping.Cid == CID  // Filter based on PCID and CID
            //             join role in _context.RoleMsts on mapping.RoleId equals role.RoleId  // Join with RoleMst to get role details
            //             select new RoleProfitcenterMapDTO
            //             {
            //                 RoleId = role.RoleId,
            //                 RoleName = role.RoleName
            //             };
            //return result.ToList();
            return null;
        }

        public async Task<List<PagePermissionDto>> GetPermittedPages(int pcId, int roleId)
        {
            //return await (from mapping in _context.ProfitCenterRolePageMappings
            //              join page in _context.PageMsts on mapping.PageId equals page.PageId
            //              where mapping.Pcid == pcId && mapping.RoleId == roleId
            //              select new PagePermissionDto
            //              {
            //                  PageId = page.PageId,
            //                  PageName = page.PageName,
            //                  PermissionStatus = mapping.PermissionStatus
            //              }).ToListAsync();
            return null;
        }

        public async Task<bool> UpdatePagePermissionsAsync(List<PagePermissionUpdateDto> updates)
        {
            //if (updates == null || updates.Count == 0)
            //{
            //    return false;
            //}

            //foreach (var update in updates)
            //{
            //    var mapping = await _context.ProfitCenterRolePageMappings
            //        .FirstOrDefaultAsync(m => m.Pcid == update.PCID &&
            //                                   m.RoleId == update.RoleId &&
            //                                   m.PageId == update.PageId);

            //    if (mapping != null)
            //    {
            //        mapping.PermissionStatus = update.PermissionStatus;
            //    }
            //}

            //await _context.SaveChangesAsync();
            return true;
        }




    }
}
