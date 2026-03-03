using System.Data;
using AutoMapper;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Data.DbContexts;
using KalaGenset.ERP.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KalaGenset.ERP.Core.Services
{
    public class MarketingService : IMarketing
    {
        private readonly KalaDbContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public MarketingService(KalaDbContext context)
        {
            _context = context;
        }

        public async Task<List<Dictionary<string, object>>> GetPendingMOFNFAsync()
        {
            var data = new List<Dictionary<string, object>>();

            using (var conn = _context.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "getAuthMOFApps_sp";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@PCCode", "0"));
                    cmd.Parameters.Add(new SqlParameter("@Auth", "1"));
                    cmd.Parameters.Add(new SqlParameter("@Auth1", "1"));
                    cmd.Parameters.Add(new SqlParameter("@Auth2", "1"));
                    cmd.Parameters.Add(new SqlParameter("@AuthNFA", "0"));

                    if (conn.State == ConnectionState.Closed)
                        await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                            data.Add(row);
                        }
                    }
                }
            }

            return data;
        }

        public async Task<string> SubmitMOFNFALevelAsync(MOFNFALevelAuthSubmitRequest req)
        {
            if (req == null) return "Invalid request";

            // normalize SaveType
            var saveType = (req.SaveType ?? string.Empty).Trim();
            string result = "Invalid SaveType"; // default value

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                // Use a transaction to match original behavior
                await using var transaction = await _context.Database.BeginTransactionAsync();
            
                try
                {
                    if (string.Equals(saveType, "Auth", StringComparison.OrdinalIgnoreCase))
                    {
                        // Find MOF matching original WHERE:
                        // MOFCode = req.MOFNo AND MOFType='N' AND Auth2='1' AND AuthNFA='0'
                        var mof = await _context.Mofs
                            .FirstOrDefaultAsync(m =>
                                m.Mofcode == req.MOFNo.Trim()
                                && m.Moftype == "N"
                                && m.Auth2 == true
                                && m.AuthNfa == false);

                        if (mof == null)
                        {
                            // nothing to update
                            result = "MOF not found or already processed";
                        }

                        // Update fields
                        mof.AuthNfa = true;
                        mof.AuthNfaremark = (req.AuthRemark ?? string.Empty).Trim();
                        mof.AuthRemark = "record auth from apps";

                        _context.Mofs.Update(mof);

                        // Insert AuthorizationDetails record
                        var authDetail = new AuthorizationDetail
                        {
                            AuthDateTime = DateTime.Now,
                            EmpId = (req.UserID ?? string.Empty).Trim(),
                            AuthForm = "MOF NFA Level Auth",
                            TransactionNo = req.MOFNo.Trim(),
                            CompanyCode = "07"
                        };

                        await _context.AuthorizationDetails.AddAsync(authDetail);

                        // persist
                        await _context.SaveChangesAsync();

                        // commit
                        await transaction.CommitAsync();

                        result = "Success";
                    }
                    else if (string.Equals(saveType, "Hold", StringComparison.OrdinalIgnoreCase))
                    {
                        // Find MOF matching original WHERE:
                        // MOFCode = req.MOFNo AND MOFType='N' AND Auth2='1'
                        var mof = await _context.Mofs
                            .FirstOrDefaultAsync(m =>
                                m.Mofcode == req.MOFNo.Trim()
                                && m.Moftype == "N"
                                && m.Auth2 == true);

                        if (mof == null)
                        {
                            result = "MOF not found or not in Auth2='1' state";
                        }

                        // Update fields
                        mof.Auth2 = false;
                        mof.AuthNfaremark = (req.AuthRemark ?? string.Empty).Trim();

                        _context.Mofs.Update(mof);

                        // Insert AuthorizationDetails
                        var authDetail = new AuthorizationDetail
                        {
                            AuthDateTime = DateTime.Now,
                            EmpId = (req.UserID ?? string.Empty).Trim(),
                            AuthForm = "MOF Hold Level Auth",
                            TransactionNo = req.MOFNo.Trim(),
                            CompanyCode = "07"
                        };

                        await _context.AuthorizationDetails.AddAsync(authDetail);

                        // persist
                        await _context.SaveChangesAsync();

                        // commit
                        await transaction.CommitAsync();

                        result = "Success";
                    }
                    else
                    {
                        result = "Invalid SaveType";
                    }
                }
                catch (Exception ex)
                {
                    // rollback on error
                    try { await transaction.RollbackAsync(); } catch { /* ignore rollback errors */ }

                    throw ex;
                }
            });
            return result;
        }

    }
}
