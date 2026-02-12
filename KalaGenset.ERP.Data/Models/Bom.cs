using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Data.Models;

public partial class Bom
{
    public string Bomcode { get; set; } = null!;

    public DateTime Dt { get; set; }

    public string MaxSrNo { get; set; } = null!;

    public string Yr { get; set; } = null!;

    public string KitType { get; set; } = null!;

    public string PartCode { get; set; } = null!;

    public double Scrpper { get; set; }

    public double ScrapperCpy { get; set; }

    public double ScrapperCp { get; set; }

    public double ScrapperCps { get; set; }

    public double ScrapperTwr { get; set; }

    public double ScrapAmtHr { get; set; }

    public double ScrapAmtCr { get; set; }

    public double ScrapAmtGicpy { get; set; }

    public double ScrapAmtHrcpy { get; set; }

    public double ScrapAmtCrcpy { get; set; }

    public double ScrapAmtHrcp { get; set; }

    public double ScrapAmtCrcp { get; set; }

    public double ScrapAmtHrcps { get; set; }

    public double ScrapAmtCrcps { get; set; }

    public double ScrapAmtHrtwr { get; set; }

    public double ScrapAmtCrtwr { get; set; }

    public double ScrapAmt { get; set; }

    public double ScrapAmtCpy { get; set; }

    public double ScrapAmtCp { get; set; }

    public double ScrapAmtCps { get; set; }

    public double ScrapAmtTwr { get; set; }

    public double ScrapWgtGi { get; set; }

    public double ScrapWgtHr { get; set; }

    public double ScrapWgtCr { get; set; }

    public double ScrapWgtGicpy { get; set; }

    public double ScrapWgtHrcpy { get; set; }

    public double ScrapWgtCrcpy { get; set; }

    public double ScrapWgtHrcp { get; set; }

    public double ScrapWgtCrcp { get; set; }

    public double ScrapWgtHrcps { get; set; }

    public double ScrapWgtCrcps { get; set; }

    public double ScrapWgtHrtwr { get; set; }

    public double ScrapWgtCrtwr { get; set; }

    public double ScrapWgt { get; set; }

    public double ScrapWgtCpy { get; set; }

    public double ScrapWgtCp { get; set; }

    public double ScrapWgtCps { get; set; }

    public double ScrapWgtTwr { get; set; }

    public double MfgPer { get; set; }

    public double AdminPer { get; set; }

    public double FinancePer { get; set; }

    public double EmpPer { get; set; }

    public double MktPer { get; set; }

    /// <summary>
    /// Cost of service
    /// </summary>
    public double OverHeadPer { get; set; }

    public double Labour { get; set; }

    public double Wastage { get; set; }

    public double ProfitPer { get; set; }

    public double Cstper { get; set; }

    public double Cstamt { get; set; }

    public double Transport { get; set; }

    public double WgtGi { get; set; }

    public double WgtHr { get; set; }

    public double WgtCr { get; set; }

    public double WgtGicpy { get; set; }

    public double WgtHrcpy { get; set; }

    public double WgtCrcpy { get; set; }

    public double WgtHrcp { get; set; }

    public double WgtCrcp { get; set; }

    public double WgtHrcps { get; set; }

    public double WgtCrcps { get; set; }

    public double WgtHrtwr { get; set; }

    public double WgtCrtwr { get; set; }

    public double Wgt { get; set; }

    public double WgtCpy { get; set; }

    public double WgtCp { get; set; }

    public double WgtCps { get; set; }

    public double WgtTwr { get; set; }

    public double TotalWgt { get; set; }

    public double TotalWgtCpy { get; set; }

    public double TotalWgtCp { get; set; }

    public double TotalWgtCps { get; set; }

    public double TotalWgtTwr { get; set; }

    public double Sqft { get; set; }

    public double SqftCpy { get; set; }

    public double SqftCp { get; set; }

    public double SqftCps { get; set; }

    public double SqftTwr { get; set; }

    public double Kgamt { get; set; }

    public double KgamtCpy { get; set; }

    public double KgamtCp { get; set; }

    public double KgamtCps { get; set; }

    public double KgamtTwr { get; set; }

    public double SgFtAmt { get; set; }

    public double SqFtAmtCpy { get; set; }

    public double SqFtAmtCp { get; set; }

    public double SqFtAmtCps { get; set; }

    public double SqFtAmtTwr { get; set; }

    public double Ppglwt { get; set; }

    public double Ppglamt { get; set; }

    public double ChemicalWt { get; set; }

    public double FoamCpySamt { get; set; }

    public double SqTubeWt { get; set; }

    public double SqTubeAmt { get; set; }

    public double CanopyAsblyAmt { get; set; }

    public double Ismcishbwt { get; set; }

    public double Ismcishbamt { get; set; }

    public double TowerAsblyAmt { get; set; }

    public double GeomSamt { get; set; }

    public double GeomAsblyAmt { get; set; }

    public double Cpamt { get; set; }

    public double CpasblyAmt { get; set; }

    public double ProfitAmtCp { get; set; }

    public double Dgsamt { get; set; }

    public double DgasblyAmt { get; set; }

    public double SuppAmt { get; set; }

    public double Tgiamt { get; set; }

    public double Thramt { get; set; }

    public double Tcramt { get; set; }

    public double TgiamtCpy { get; set; }

    public double ThramtCpy { get; set; }

    public double TcramtCpy { get; set; }

    public double ThramtCp { get; set; }

    public double TcramtCp { get; set; }

    public double ThramtCps { get; set; }

    public double TcramtCps { get; set; }

    public double ThramtTwr { get; set; }

    public double TcramtTwr { get; set; }

    public double TmatCost { get; set; }

    public double TmatCostCpy { get; set; }

    public double TmatCostCp { get; set; }

    public double TmatCostCps { get; set; }

    public double TmatCostTwr { get; set; }

    public double TprcCostKg { get; set; }

    public double TprcCostKgcpy { get; set; }

    public double TprcCostKgcp { get; set; }

    public double TprcCostKgcps { get; set; }

    public double TprcCostKgtwr { get; set; }

    public double TprcCostSqft { get; set; }

    public double TprcCostSqftCpy { get; set; }

    public double TprcCostSqftCp { get; set; }

    public double TprcCostSqftCps { get; set; }

    public double TprcCostSqftTwr { get; set; }

    public double EngAmt { get; set; }

    public double AltAmt { get; set; }

    public double BattAmt { get; set; }

    public double InsuKitAmt { get; set; }

    public double CpyaccAmt { get; set; }

    public double CpaccAmt { get; set; }

    public double TwrAccAmt { get; set; }

    public double TotMatAmt { get; set; }

    public double TotMatAmtCpy { get; set; }

    public double TotMatAmtCp { get; set; }

    public double TotMatAmtCps { get; set; }

    public double TotMatAmtTwr { get; set; }

    public double LandedCostMat { get; set; }

    public double CpyprocCost { get; set; }

    public double CpprocCost { get; set; }

    public double CpsprocCost { get; set; }

    public double TwrProcCost { get; set; }

    public double ToTprcCost { get; set; }

    public double EngQty { get; set; }

    public double AltQty { get; set; }

    public double Cpyqty { get; set; }

    public double Cpqty { get; set; }

    public double BatQty { get; set; }

    public double GoemKitQty { get; set; }

    public string NestingStatus { get; set; } = null!;

    public string NestingLstatus { get; set; } = null!;

    public string NestingCpstatus { get; set; } = null!;

    public string NestingCpsstatus { get; set; } = null!;

    public string NestingTwrStatus { get; set; } = null!;

    public string NestingCpystatus { get; set; } = null!;

    public string NestingBfstatus { get; set; } = null!;

    public string NestingBftstatus { get; set; } = null!;

    public string NestingFtstatus { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    public string Remark { get; set; } = null!;

    public bool Auth { get; set; }

    public DateTime AuthDt { get; set; }

    public bool Discard { get; set; }

    public bool Active { get; set; }

    public bool Status { get; set; }

    public bool WsStatusKalaToPms { get; set; }

    public bool KalaToBio { get; set; }

    public double MarginPer { get; set; }
}
