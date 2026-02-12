using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Employee
{
    public string Ecode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string Fname { get; set; } = null!;

    public string Mname { get; set; } = null!;

    public string Lname { get; set; } = null!;

    public string Vanva { get; set; } = null!;

    public string TempEcode { get; set; } = null!;

    public DateTime JoinDate { get; set; }

    public DateTime? Confirmdate { get; set; }

    public DateTime? Leavedate { get; set; }

    public DateTime? Resigndate { get; set; }

    public string Cadd { get; set; } = null!;

    public string Padd { get; set; } = null!;

    public string MailId { get; set; } = null!;

    public string CompMailId { get; set; } = null!;

    public string Ph1 { get; set; } = null!;

    public string Ph2 { get; set; } = null!;

    public string Mobile { get; set; } = null!;

    public DateTime Dob { get; set; }

    public string MartialStatus { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public string Qualification { get; set; } = null!;

    public string Specilization { get; set; } = null!;

    public string? BloodGroup { get; set; }

    public string PassPortNo { get; set; } = null!;

    public string Panno { get; set; } = null!;

    public string Pfno { get; set; } = null!;

    public int PfnoMax { get; set; }

    public string BankCode { get; set; } = null!;

    public string BankAccNo { get; set; } = null!;

    public string BankIfsccode { get; set; } = null!;

    public string BankAddress { get; set; } = null!;

    public string EpayMode { get; set; } = null!;

    public string LicenceNo { get; set; } = null!;

    public string BatchNo { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public string EmployeeType { get; set; } = null!;

    public string CountryId { get; set; } = null!;

    public int StateId { get; set; }

    public int CityId { get; set; }

    public int PaySheetHeadType { get; set; }

    public string DeptOneId { get; set; } = null!;

    public int DeptTwoId { get; set; }

    public int SubDeptOneId { get; set; }

    public int SubDeptTwoId { get; set; }

    public int SubDeptThreeId { get; set; }

    public int SubDeptFourId { get; set; }

    public int SubDeptFiveId { get; set; }

    public int SubDeptSixId { get; set; }

    public int SubDeptSevenId { get; set; }

    public string SubDeptId { get; set; } = null!;

    public string DeptKitCode { get; set; } = null!;

    public string PositionRoleId { get; set; } = null!;

    public string SubDeptIdN { get; set; } = null!;

    public string GradeId { get; set; } = null!;

    public string GradeIdStd { get; set; } = null!;

    public string GradeIdAct { get; set; } = null!;

    public string DesignationId { get; set; } = null!;

    public string DesignationIdStd { get; set; } = null!;

    public string DesignationIdAct { get; set; } = null!;

    public string WorkDesignationId { get; set; } = null!;

    public string WorkStationId { get; set; } = null!;

    public string DirectReporting { get; set; } = null!;

    public string IndirectReporting { get; set; } = null!;

    public string DepartmentHod { get; set; } = null!;

    public double Remuneration { get; set; }

    public double Consolidated { get; set; }

    public double HouseRent { get; set; }

    public double ConveyanceAll { get; set; }

    public double Education { get; set; }

    public double Cca { get; set; }

    public double MiscAll { get; set; }

    public double GrossSalary { get; set; }

    public double Pfemployee { get; set; }

    public double EsicEmployee { get; set; }

    public double Pt { get; set; }

    public double TakeHomeSalary { get; set; }

    public double Pfemployer { get; set; }

    public double EsicEmployer { get; set; }

    public double Exgratiapm { get; set; }

    public double Medicalpm { get; set; }

    public double Ltapm { get; set; }

    public double Bonuspm { get; set; }

    public double Gratuity { get; set; }

    public double Ctcpm { get; set; }

    public double BasicSalaryOman { get; set; }

    public double HousingAllowOman { get; set; }

    public double TransportationAllowOman { get; set; }

    public double OtherAllowOman { get; set; }

    public double IncentiveOman { get; set; }

    public double TakeHomeSalaryOman { get; set; }

    public double PerDayRateOman { get; set; }

    public double MobileAmountOman { get; set; }

    public double MobileLimitChargesOman { get; set; }

    public double CtcpmOman { get; set; }

    public string CompanyCode { get; set; } = null!;

    public string CompanyCodeStd { get; set; } = null!;

    public string CompanyCodeAct { get; set; } = null!;

    public string LegalEntity { get; set; } = null!;

    public string ContractorCode { get; set; } = null!;

    public string DeptName { get; set; } = null!;

    public string DeptNameN { get; set; } = null!;

    public double PprivilegeLeave { get; set; }

    public double PrivilegeLeave { get; set; }

    public double PsickLeave { get; set; }

    public double CasualLeave { get; set; }

    public double SickLeave { get; set; }

    public double CompensatoryLeave { get; set; }

    public string WeeklyOff { get; set; } = null!;

    public double CanteenAmount { get; set; }

    public double MobileAmount { get; set; }

    public double MobileLimitCharges { get; set; }

    public double UniformAmount { get; set; }

    public double MediClamAmount { get; set; }

    public double PerDayRate { get; set; }

    public double Welfare { get; set; }

    public double PerformanceIncentive { get; set; }

    public double TravellingAllowance { get; set; }

    public bool Erplogin { get; set; }

    public bool MobileSim { get; set; }

    public string CmobileNo { get; set; } = null!;

    public bool AddOnMobileNo { get; set; }

    public string CmobilePlan { get; set; } = null!;

    public bool Uniform { get; set; }

    public bool Icard { get; set; }

    public string IcardNo { get; set; } = null!;

    public bool BusFacility { get; set; }

    public bool Mediclaim { get; set; }

    public string IpolicyCompany { get; set; } = null!;

    public string GroupPolicyNo { get; set; } = null!;

    public string PolicyNo { get; set; } = null!;

    public double SumInsured { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool Computer { get; set; }

    public bool CanteenC { get; set; }

    public string? FilePath { get; set; }

    public bool BankAcc { get; set; }

    public string? EmpStatus { get; set; }

    public string? EmpConfirmStatus { get; set; }

    public string CtcchangesStatus { get; set; } = null!;

    public string College { get; set; } = null!;

    public string University { get; set; } = null!;

    public string Experience { get; set; } = null!;

    public bool Active { get; set; }

    public bool Ot { get; set; }

    public string ProfitCenter { get; set; } = null!;

    public string ProfitCenterOld { get; set; } = null!;

    public string ProfitCenterStd { get; set; } = null!;

    public string ProfitCenterAct { get; set; } = null!;

    public string NameAsPerAdhar { get; set; } = null!;

    public string AdharCardNo { get; set; } = null!;

    public string Uanno { get; set; } = null!;

    public string Esino { get; set; } = null!;

    public string Auth1Remark { get; set; } = null!;

    public string Auth2Remark { get; set; } = null!;

    public bool Auth1 { get; set; }

    public bool Auth2 { get; set; }

    public int StaffStdQty { get; set; }

    public int LabourStdQty { get; set; }

    public int OsvenderStdQty { get; set; }

    public bool Status { get; set; }

    public bool WsStatusKalaToPms { get; set; }

    public bool KalaToBio { get; set; }
}
