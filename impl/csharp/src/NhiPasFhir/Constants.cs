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
    public const string CiSd = CiBase + "/StructureDefinition";
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

    // --- 電子處方箋與調劑 (nhi.empd 0.1.0) — transcribed from the official example Bundle-bun-ep. ---
    public const string EmpdBase = "https://nhicore.nhi.gov.tw/empd";
    public const string EmpdSd = EmpdBase + "/StructureDefinition";
    public const string EmpdCaseType = EmpdBase + "/CodeSystem/CaseType-cs";
    public const string EmpdPaymentCategory = EmpdBase + "/CodeSystem/PaymentCategory-cs";
    public const string EmpdTypeOfPrescription = EmpdBase + "/CodeSystem/TypeOfPrescription-cs";
    public const string EmpdOrderType = EmpdBase + "/CodeSystem/OrderType-cs";
    public const string EmpdSelfpayStatus = EmpdBase + "/CodeSystem/SelfpayStatus-cs";
    public const string EmpdExtTotalDuration = EmpdSd + "/Extension-TotalDuration";
    public const string MoiSlash = "http://www.moi.gov.tw/";           // note: empd uses a trailing slash
    public const string Hpio = "http://ns.electronichealth.net.au/id/hi/hpio/1.0";  // Org identifier (example)
    public const string MohwSlash = "https://www.mohw.gov.tw/";        // practitioner qualification id
    public const string ExtPersonAge = TwcoreSd + "/person-age";
    public const string MedicationNhiTw = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/medication-nhi-tw";
    public const string MedicationPathTw = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/medication-path-tw";

    // --- 次世代基因定序 NGS (nhi.ngs 1.0.0) — transcribed from the official example Bundle-bun-nos-min. ---
    public const string NgsBase = "https://nhicore.nhi.gov.tw/ngs";
    public const string NgsSd = NgsBase + "/StructureDefinition";
    public const string NgsApplyType = NgsBase + "/CodeSystem/nhi-apply-type";
    public const string NgsCaseClassification = NgsBase + "/CodeSystem/nhi-case-classification";
    public const string NgsOrgId = NgsBase + "/CodeSystem/organization-identifier-tw";
    public const string NgsExtDiagReportCondition = NgsSd + "/extension-DiagnosticReport-condition";
    public const string DepMohw = "https://dep.mohw.gov.tw";            // genetic-testing org identifier
    public const string GeneNames = "http://www.genenames.org";        // HGNC gene ids
    public const string Hgvs = "http://varnomen.hgvs.org";             // HGVS variant nomenclature
    public const string MedicalServicePaymentTw = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/medical-service-payment-tw";
}
