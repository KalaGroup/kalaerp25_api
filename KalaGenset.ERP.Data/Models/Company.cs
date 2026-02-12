using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Company
{
    public string Cid { get; set; } = null!;

    public string Ccode { get; set; } = null!;

    public DateTime Cdt { get; set; }

    public string Cname { get; set; } = null!;

    public string CaliseName { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public string ParentCompany { get; set; } = null!;

    public string CurrencyCode { get; set; } = null!;

    public string Wadd { get; set; } = null!;

    public string Address1 { get; set; } = null!;

    public string Address2 { get; set; } = null!;

    public string Hoadd { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string Ph1 { get; set; } = null!;

    public string Ph2 { get; set; } = null!;

    public string Mobile { get; set; } = null!;

    public string Fax { get; set; } = null!;

    public string ContactPerson { get; set; } = null!;

    public string CpphNo { get; set; } = null!;

    public string VattinNo { get; set; } = null!;

    public string CsttinNo { get; set; } = null!;

    public string Tanno { get; set; } = null!;

    public string GsttinNo { get; set; } = null!;

    public string DbtcertNo { get; set; } = null!;

    public string SeedLicenseNo { get; set; } = null!;

    public string Panno { get; set; } = null!;

    public string Eccno { get; set; } = null!;

    public string Iecno { get; set; } = null!;

    public string Range { get; set; } = null!;

    public string Division { get; set; } = null!;

    public string CommRate { get; set; } = null!;

    public string SrvTaxNo { get; set; } = null!;

    public string EstablishmentCode { get; set; } = null!;

    public string Cinno { get; set; } = null!;

    /// <summary>
    /// Company Type
    /// </summary>
    public string OrganisationId { get; set; } = null!;

    public string GradeId { get; set; } = null!;

    /// <summary>
    /// DesignationName
    /// </summary>
    public string DesignationId { get; set; } = null!;

    /// <summary>
    /// CountryName
    /// </summary>
    public string CountryId { get; set; } = null!;

    /// <summary>
    /// StateName
    /// </summary>
    public int StateId { get; set; }

    /// <summary>
    /// CityName
    /// </summary>
    public int CityId { get; set; }

    public string LedgerId { get; set; } = null!;

    public string ElectricityBoardName { get; set; } = null!;

    public string MsebconsumerNo { get; set; } = null!;

    public string MeterNo { get; set; } = null!;

    public double SancElecLoad { get; set; }

    public string SancElecUom { get; set; } = null!;

    public string Remark { get; set; } = null!;

    /// <summary>
    /// 1 - Active 0 - INActive
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// 1 - Authorize 0 - UnAuthorize
    /// </summary>
    public bool Auth { get; set; }

    public bool Status { get; set; }

    public bool WsStatusKalaToPms { get; set; }

    public bool KalaToBio { get; set; }
}
