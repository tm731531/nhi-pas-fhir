namespace NhiPasFhir;

/// <summary>Verified systems/CodeSystems/extension URLs (from the official IG package + examples).</summary>
public static class Sys
{
    public const string PasBase = "https://nhicore.nhi.gov.tw/pas";
    public const string Sd = PasBase + "/StructureDefinition";
    public const string TwcoreSd = "https://twcore.mohw.gov.tw/ig/twcore/StructureDefinition";

    public const string V2_0203 = "http://terminology.hl7.org/CodeSystem/v2-0203";
    public const string TwcoreV2_0203 = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/v2-0203";
    public const string IdCard = "http://www.moi.gov.tw";
    public const string OrgId = PasBase + "/CodeSystem/organization-identifier-tw";
    public const string OidNat = "https://oid.nat.gov.tw/";
    public const string Ucum = "http://unitsofmeasure.org";
    public const string Loinc = "http://loinc.org";
    public const string Snomed = "http://snomed.info/sct";
    public const string V3ActCode = "http://terminology.hl7.org/CodeSystem/v3-ActCode";
    public const string OrgType = "http://terminology.hl7.org/CodeSystem/organization-type";
    public const string ClaimType = "http://terminology.hl7.org/CodeSystem/claim-type";
    public const string ServiceDept = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/medical-consultation-department-nhi-tw";
    public const string Icd10cmTw = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/icd-10-cm-2023-tw";
    public const string GtsAbbrev = "http://terminology.hl7.org/CodeSystem/v3-GTSAbbreviation";
    public const string MedFreqNhi = PasBase + "/CodeSystem/medication-frequency-nhi-tw";

    public const string CsApplyType = PasBase + "/CodeSystem/nhi-apply-type";
    public const string CsTmhbType = PasBase + "/CodeSystem/nhi-tmhb-type";
    public const string CsSupportingInfo = PasBase + "/CodeSystem/nhi-supporting-info-type";
    public const string CsOrderType = PasBase + "/CodeSystem/nhi-order-type";
    public const string CsContinuation = PasBase + "/CodeSystem/nhi-continuation-status";
    public const string CsLineOfTherapy = PasBase + "/CodeSystem/nhi-line-of-therapy";
    public const string CsMedication = PasBase + "/CodeSystem/nhi-medication";

    public const string ExtClaimEncounter = Sd + "/extension-claim-encounter";
    public const string ExtRequestedService = Sd + "/extension-requestedService";
    public const string ExtDxRecordedDate = "http://hl7.org/fhir/us/davinci-pas/StructureDefinition/extension-diagnosisRecordedDate";
}
