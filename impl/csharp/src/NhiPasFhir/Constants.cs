namespace NhiPasFhir;

// IG 對照 (spec/docs/IG-TRACEABILITY.md): 規範文件「專門術語」:所有 CodeSystem / ValueSet 的 canonical system URL

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
    // This is the system the TW Core ValueSet icd-10-cm-2023-tw includes. NOTE: TW Core 0.3.2 ships the
    // CodeSystem with a wrong canonical url (points to a /ValueSet/ path), so the validator cannot resolve
    // it → Condition-twpas CLOSED slicing fails. tools/fetch_validation_assets.sh builds a corrected
    // CodeSystem patch (right url + the 96802 concepts) passed via -ig so validation can prove conformance.
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

    // --- immunologic-agent additional systems (transcribed from the official example) ---
    public const string Icd10pcsTw = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/icd-10-pcs-2023-tw";  // see Icd10cmTw note (same CodeSystem-url bug in TW Core 0.3.2)
    public const string ConditionClinical = "http://terminology.hl7.org/CodeSystem/condition-clinical";
    public const string ObsCategory = "http://terminology.hl7.org/CodeSystem/observation-category";
    public const string AllergyClinical = "http://terminology.hl7.org/CodeSystem/allergyintolerance-clinical";
    public const string CarePlanCatTw = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/careplan-category-tw";
    public const string Dcm = "http://dicom.nema.org/resources/ontology/DCM";
    public const string CsPdfType = PasBase + "/CodeSystem/nhi-pdf-type";
    public const string CsDrugCategory = PasBase + "/CodeSystem/nhi-drug-category";
    public const string MedReqStatusReason = "http://terminology.hl7.org/CodeSystem/medicationrequest-status-reason";
    public const string CsTxAst = PasBase + "/CodeSystem/nhi-tx-ast";
    public const string CsPhototherapy = PasBase + "/CodeSystem/nhi-phototherapy";
    public const string CsApplyReason = PasBase + "/CodeSystem/nhi-apply-reason";
    public const string NciThesaurus = PasBase + "/CodeSystem/nci-thesaurus";
    public const string CsPatAst = PasBase + "/CodeSystem/nhi-pat-ast";
    public const string Immigration = "http://www.immigration.gov.tw";   // 居留證 (PRC)
    public const string V3InterpObs = "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation";
    public const string OrderableDrugForm = "http://terminology.hl7.org/CodeSystem/v3-orderableDrugForm";

    // --- ClaimResponse (核定回應) ---
    public const string Adjudication = "http://terminology.hl7.org/CodeSystem/adjudication";
    public const string CsApproveComment = PasBase + "/CodeSystem/nhi-approve-comment";
    public const string ExtClaimResponseRequestor = Sd + "/extension-claimResponse-requestor";
    public const string OperationOutcomeCs = "http://terminology.hl7.org/CodeSystem/operation-outcome";

    public const string ExtClaimEncounter = Sd + "/extension-claim-encounter";
    public const string ExtRequestedService = Sd + "/extension-requestedService";
    public const string ExtDxRecordedDate = "http://hl7.org/fhir/us/davinci-pas/StructureDefinition/extension-diagnosisRecordedDate";

    // --- 重大傷病 (nhi.ci 1.0.2) — systems transcribed from the official examples (Bundle-bun-min). ---
    public const string CiBase = "https://nhicore.nhi.gov.tw/ci";
    public const string CiApproveResult = CiBase + "/CodeSystem/nhi-approve-result";
    public const string CiCategory = CiBase + "/CodeSystem/nhi-category";
    public const string CiApplyMode = CiBase + "/CodeSystem/nhi-apply-mode";
    public const string CiApplyType = CiBase + "/CodeSystem/nhi-apply-type";
    public const string CiOrgId = CiBase + "/CodeSystem/organization-identifier-tw";
    public const string CiCancerStage = CiBase + "/CodeSystem/cancer-stage";
    public const string CiCancerStaging = CiBase + "/CodeSystem/nhi-cancerstaging";
    public const string CiCancerStageStatus = CiBase + "/CodeSystem/nhi-cancerstage-status";
    public const string CiCancerTreatment = CiBase + "/CodeSystem/nhi-cancer-treatment";
    public const string CiCancerTreatmentPlan = CiBase + "/CodeSystem/nhi-cancer-treatment-plan";
    public const string CiQuestionnaire = CiBase + "/Questionnaire/apply-catastrophic-illness";
    public const string TwcorePostal3 = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/postal-code3-tw";
    public const string ExtTwPostalCode = "https://twcore.mohw.gov.tw/ig/twcore/StructureDefinition/tw-postal-code";
    public const string Tpech = "https://tpech.gov.taipei";           // 院內病歷號 assigning authority (example)
    public const string BcpImg = "urn:ietf:bcp:13";                    // image media type
    public const string RfcUri = "urn:ietf:rfc:3986";                  // DICOM SOP class as URI
    public const string CondCategory = "http://terminology.hl7.org/CodeSystem/condition-category";

    // --- 電子處方箋與調劑 (nhi.empd 0.2.1) — transcribed from the official example Bundle-bun-01-ep. ---
    public const string EmpdBase = "https://nhicore.nhi.gov.tw/empd";
    public const string EmpdPrescriptionId = EmpdBase + "/identifier/prescription";           // Bundle.identifier + MedicationRequest PrescriptionNo slice
    public const string EmpdMedicalEncounterId = EmpdBase + "/medical-encounter-identifier";   // Encounter.identifier slice medical-encounter-identifier
    public const string EmpdFuncSequenceNumber = EmpdBase + "/func-sequence-number";           // Encounter.identifier slice func-sequence-number
    public const string EmpdOutpatientCaseType = EmpdBase + "/CodeSystem/nhi-outpatient-case-type";  // Encounter.class (was CaseType-cs in 0.1.0)
    public const string EmpdPartCode = EmpdBase + "/CodeSystem/PartCode-cs";                   // Encounter.type
    public const string EmpdNhiIdentityType = EmpdBase + "/CodeSystem/nhi-identity-type";      // Coverage.type
    public const string EmpdPaymentCategory = EmpdBase + "/CodeSystem/PaymentCategory-cs";     // Coverage Extension-PaymentCategory value
    public const string EmpdExtPaymentCategory = EmpdBase + "/StructureDefinition/Extension-PaymentCategory";
    public const string EmpdExtCombinedPrescriptionNote = EmpdBase + "/StructureDefinition/Extension-CombinedPrescriptionNote";
    public const string EmpdOrgIdTw = EmpdBase + "/CodeSystem/organization-identifier-tw";     // Organization.identifier:nhi-organization system
    public const string EmpdNhiMedicationCs = EmpdBase + "/CodeSystem/NHIMedication-cs";       // Medication.code nhi-medication slice (was twcore medication-nhi-tw in 0.1.0)
    public const string EmpdTypeOfPrescription = EmpdBase + "/CodeSystem/TypeOfPrescription-cs";
    public const string EmpdOrderType = EmpdBase + "/CodeSystem/OrderType-cs";
    public const string EmpdSelfpayStatus = EmpdBase + "/CodeSystem/SelfpayStatus-cs";
    public const string EmpdExtTotalDuration = EmpdBase + "/StructureDefinition/Extension-TotalDuration";
    public const string Moi = "http://www.moi.gov.tw";                 // Patient 國民身分證統一編號 (no trailing slash in empd 0.2.1)
    public const string Tmip = "https://www.tmip.com.tw/";             // Patient 病歷號 (medical-record) assigning authority (example)
    public const string DepMohwDoma = "https://dep.mohw.gov.tw/DOMA";  // Practitioner medical-license identifier (example)
    public const string CdmisFda = "https://cdmis.fda.gov.tw";         // Practitioner qualification identifier system (was mohw.gov.tw in 0.1.0)
    public const string ServiceDeptTreatmentNhiTw = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/medical-treatment-department-nhi-tw";  // Encounter.serviceType
    public const string ExtPersonAge = TwcoreSd + "/person-age";
    public const string MedicationPathTw = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/medication-path-tw";

    // --- 次世代基因定序 NGS (nhi.ngs 1.0.0) — transcribed from the official example Bundle-bun-nos-min. ---
    public const string NgsBase = "https://nhicore.nhi.gov.tw/ngs";
    public const string NgsApplyType = NgsBase + "/CodeSystem/nhi-apply-type";
    public const string NgsCaseClassification = NgsBase + "/CodeSystem/nhi-case-classification";
    public const string NgsOrgId = NgsBase + "/CodeSystem/organization-identifier-tw";
    public const string NgsExtDiagReportCondition = NgsBase + "/StructureDefinition/extension-DiagnosticReport-condition";
    public const string DepMohw = "https://dep.mohw.gov.tw";            // genetic-testing org identifier
    public const string GeneNames = "http://www.genenames.org";        // HGNC gene ids
    public const string Hgvs = "http://varnomen.hgvs.org";             // HGVS variant nomenclature
    public const string MedicalServicePaymentTw = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/medical-service-payment-tw";

    // --- 傳染病檢驗報告 (cdc.twidir 0.1.1) — transcribed from the official example Bundle-bundle-request-ser-min. ---
    public const string TwidirBase = "https://twidir.cdc.gov.tw/twidir";
    public const string TwidirIdentifierType = TwidirBase + "/CodeSystem/twcdc-identifier-type-values";
    public const string TwidirLoincPartSystem = TwidirBase + "/CodeSystem/loinc-part-system-values";
    public const string TwidirOrgType = TwidirBase + "/CodeSystem/twcdc-organization-type-values";
    public const string TwidirDeviceType = TwidirBase + "/CodeSystem/twcdc-device-type-values";
    public const string Icd10cm2021Tw = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/icd-10-cm-2021-tw";
    public const string TwcoreOrgId = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/organization-identifier-tw";
    public const string PractitionerTwcore = TwcoreSd + "/Practitioner-twcore";
    public const string Boca = "http://www.boca.gov.tw/";             // 護照號碼
    public const string TpechSlash = "https://tpech.gov.taipei/";     // 院內病歷號 (trailing slash)
    public const string TphMohw = "https://www.tph.mohw.gov.tw";      // 醫師證號
    public const string LimsCdc = "https://lims.cdc.gov.tw/";         // lab result id

    // --- 電子病歷交換單張 EMR (tw.gov.mohw.emr 0.2.0) — transcribed from the official example Bundle-example-IC (檢驗檢查). ---
    public const string EmrBase = "https://twcore.mohw.gov.tw/ig/emr";
    public const string ExtIdentifierSuffix = TwcoreSd + "/identifier-suffix";
    public const string Vghtpe = "https://www.vghtpe.gov.tw/Index.action";
    public const string TwcoreIndex = "https://twcore.mohw.gov.tw/ig/index.html";
    public const string Iso3166_1_3 = "http://hl7.org/fhir/ValueSet/iso3166-1-3";
}
