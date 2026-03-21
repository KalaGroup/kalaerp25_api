using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using KalaGenset.ERP.Core.ResponseDTO;

namespace KalaGenset.ERP.Core.Services
{
    public class InvoiceScanService : I_invoiceScan
    {
        private readonly KalaDbContext _context;

        public InvoiceScanService(KalaDbContext context)
        {
            _context = context;
        }

        public async Task<List<Dictionary<string, object>>> GetScanDtsInvAsync(string strSrNo)
        {
            var data = new List<Dictionary<string, object>>();
            using (var conn = _context.Database.GetDbConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "GetInvoiceScanDts";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;
                    cmd.Parameters.Add(new SqlParameter("@InvBarcode", SqlDbType.Char) { Value = strSrNo });
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

        public async Task<string> SubmitAsync(string invoiceId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    string invId = invoiceId.Trim();
                    string typeCode = invId.Substring(10, 2);

                    bool isSalesInvoice = typeCode is "01" or "03" or "08" or "28";

                    if (isSalesInvoice)
                    {
                        var invoice = await _context.InvoiceSales
                                                    .Where(x => x.Invid == invId)
                                                    .FirstOrDefaultAsync()
                                      ?? throw new Exception($"Sales invoice not found: {invId}");

                        invoice.GateOut = "D";
                        invoice.GateOutTime = DateTime.Now;
                    }
                    else
                    {
                        var invoice = await _context.InvoiceDealers
                                                    .Where(x => x.Invid == invId)
                                                    .FirstOrDefaultAsync()
                                      ?? throw new Exception($"Dealer invoice not found: {invId}");

                        invoice.GateOut = "D";
                        invoice.GateOutTime = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return invId;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Submit failed: {ex.Message}", ex);
                }
            });
        }

        public async Task<string> SendEmailAsync(string invId)
        {
            invId = invId.Trim();

            try
            {
                // ── 1. Fetch gate-out details via SP ──────────────────────────────
                GateOutDto details = null;

                var sqlConn = (SqlConnection)_context.Database.GetDbConnection();
                if (sqlConn.State == ConnectionState.Closed)
                    await sqlConn.OpenAsync();

                using (var cmd = new SqlCommand("getGateOutDetails", sqlConn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;
                    cmd.Parameters.AddWithValue("@Invcode", invId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        details = new GateOutDto
                        {
                            MOFCode = reader["MOFCode"]?.ToString()?.Trim() ?? "",
                            InvId = reader["INVID"]?.ToString()?.Trim() ?? "",
                            Dt = reader["Dt"]?.ToString()?.Trim() ?? "",
                            CustName = reader["CustName"]?.ToString()?.Trim() ?? "",
                            CustAddress = reader["CustAddress"]?.ToString()?.Trim() ?? "",
                            PartDesc = reader["PartDesc"]?.ToString()?.Trim() ?? "",
                            PCName = reader["PCName"]?.ToString()?.Trim() ?? "",
                            VehicleNo = reader["VehicleNo"]?.ToString()?.Trim() ?? "",
                            DriverName = reader["DriverName"]?.ToString()?.Trim() ?? "",
                            DriverMobileNo = reader["DriverMobileNo"]?.ToString()?.Trim() ?? "",
                            HODMailID = reader["HODMailID"]?.ToString()?.Trim() ?? "",
                            CCMailID = reader["CCMailID"]?.ToString()?.Trim() ?? "",
                            ReplyToMailID = reader["ReplyToMailID"]?.ToString()?.Trim() ?? "",
                            OrderBy = reader["OrderBy"]?.ToString()?.Trim() ?? "",
                        };
                    }
                }

                if (details == null)
                    return $"False: No gate-out record found for InvId={invId}";

                // ── 2. Fetch assigned employee email via inline SQL ───────────────
                string assignToMailID = "";

                using (var empCmd = new SqlCommand(
                    "SELECT CompmailID AS AssignToMailID FROM employee WHERE ecode = @Ecode", sqlConn))
                {
                    empCmd.CommandType = CommandType.Text;
                    empCmd.CommandTimeout = 0;
                    empCmd.Parameters.AddWithValue("@Ecode", details.OrderBy);

                    using var empReader = await empCmd.ExecuteReaderAsync();
                    if (await empReader.ReadAsync())
                        assignToMailID = empReader["AssignToMailID"]?.ToString()?.Trim() ?? "";
                }

                // ── 3. Build recipient list ───────────────────────────────────────
                string toMailID = ResolveToMailID(details.HODMailID, assignToMailID);
                toMailID = AppendBranchMailID(toMailID, details.PCName);

                if (string.IsNullOrWhiteSpace(toMailID))
                    toMailID = "fs@kalabiz.com";

                // ── 4. Build and send email ───────────────────────────────────────
                using var mail = new MailMessage();
                mail.From = new MailAddress("erp@kalabiz.com", "Kala Genset Pvt. Ltd. - ERP System");
                mail.Subject = $"{details.MOFCode} has been Dispatched on {DateTime.Now:dd/MM/yyyy hh:mm tt}";
                mail.IsBodyHtml = true;
                mail.Body = BuildEmailBody(details);
                mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                AddUniqueMailAddresses(mail.To, toMailID);
                AddUniqueMailAddresses(mail.CC, details.CCMailID);
                AddUniqueMailAddresses(mail.Bcc, "fs@kalabiz.com");
                AddUniqueMailAddresses(mail.ReplyToList, details.ReplyToMailID);

                using var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential("erp@kalabiz.com", "wanv ftwc dobq blrr"),
                    Timeout = 60000
                };

                ServicePointManager.ServerCertificateValidationCallback =
                    (s, cert, chain, err) => true;

                await smtp.SendMailAsync(mail);

                return "True";
            }
            catch (Exception ex)
            {
                return $"False | Message: {ex.Message} | StackTrace: {ex.StackTrace}";
            }
        }

        // ── Recipient resolution ─────────────────────────────────────────────────────

        private static string ResolveToMailID(string hodMailID, string assignToMailID)
        {
            const string dinesh = "dinesh.vyawahare@kalabiz.com";
            const string harisha = "harisha.sn@kalabiz.com";

            // BLR override — if either is Dinesh, redirect entirely to Harisha
            if (hodMailID == dinesh || assignToMailID == dinesh)
                return harisha;

            // No employee found — fall back to HOD only
            if (string.IsNullOrWhiteSpace(assignToMailID))
                return hodMailID;

            // HODMailID may contain multiple emails — avoid duplicating AssignTo if already present
            var hodList = hodMailID
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim());

            return hodList.Contains(assignToMailID, StringComparer.OrdinalIgnoreCase)
                ? hodMailID
                : $"{hodMailID},{assignToMailID}";
        }

        private static string AppendBranchMailID(string toMailID, string pcName)
        {
            string branchMail = pcName switch
            {
                "Akurdi - HO" or "Goa" or "Kolhapur" or "Solapur" => "br@kalabiz.com",
                "Mkt Corporate" => "aw@kalabiz.com",
                "Bhopal" => "sales.bhopal@kalabiz.com",
                "Indore" => "sales.indore@kalabiz.com",
                "Pune" => "pulse@kalabiz.com",
                _ => ""
            };

            return string.IsNullOrEmpty(branchMail)
                ? toMailID
                : $"{toMailID},{branchMail}";
        }

        private static void AddUniqueMailAddresses(MailAddressCollection collection, string commaSeparated)
        {
            if (string.IsNullOrWhiteSpace(commaSeparated)) return;

            foreach (var address in commaSeparated.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = address.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed) &&
                    !collection.Any(x => x.Address.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
                {
                    collection.Add(new MailAddress(trimmed));
                }
            }
        }

        private static string BuildEmailBody(GateOutDto d)
        {
            const string td = "<td style=\"border-color:#5c87b2;border-style:solid;" +
                              "border-width:thin;white-space:nowrap;padding-left:5px;\">";
            const string tr = "<tr style=\"color:#555555;\">";

            var rows = new (string Label, string Value)[]
            {
                  ("MOF No",              d.MOFCode),
                  ("Invoice No",          d.InvId),
                  ("Invoice Date",        d.Dt),
                  ("Customer Name",       d.CustName),
                  ("Delivery Address",    d.CustAddress),
                  ("Product Description", d.PartDesc),
                  ("Branch Name",         d.PCName),
                  ("Vehicle No",          d.VehicleNo),
                  ("Driver Name",         d.DriverName),
                  ("Driver Mobile No",    d.DriverMobileNo),
            };

            var sb = new StringBuilder();
            sb.Append("<p style='color:#1F497D;'>Dear Sir/Madam,</p>");
            sb.Append("<span style='color:#1F497D;'>Dispatch Details as Follows:</span><BR><BR>");
            sb.Append("<table style=\"border-collapse:collapse;text-align:left;width:700px;\">");

            foreach (var (label, value) in rows)
                sb.Append($"{tr}{td}<b>{label}: </b>{value}</td></tr>");

            sb.Append("</table>");
            sb.Append("<BR><span style='color:#1F497D'>Thank You/Regards.</span><BR>");
            sb.Append("<BR><span style='color:#1F497D'><b>Kala Genset Pvt. Ltd.</b></span>");

            return sb.ToString();
        }
    }
}
