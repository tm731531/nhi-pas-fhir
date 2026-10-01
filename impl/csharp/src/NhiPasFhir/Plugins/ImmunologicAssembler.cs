using System.Runtime.CompilerServices;
using Hl7.Fhir.Model;
using NhiPasFhir.Core;
using static NhiPasFhir.Core.FhirBuild;   // Cc — the single shared CodeableConcept builder

namespace NhiPasFhir.Plugins;

// IG 對照 (spec/docs/IG-TRACEABILITY.md): 視覺化邏輯模型「免疫製劑事前審查」→ ApplyImmModel → 範例 Bundle-bun-imm / Claim-immunologic-agent-twpas

/// <summary>免疫製劑事前審查 case type. Builds the full Bundle-immunologic-agent-twpas (36 resources:
/// SOAP note via Composition-opd + blood group + allergy + imaging/exam/lab/procedure/phototherapy
/// evidence + two applied drugs). All values transcribed from the official IG example (fabricated, no PHI);
/// patient/provider/vitals/created come from the PACase, the clinical evidence is example-fixed for v1
/// (TODO: parameterize richer evidence from PACase.Data later). Proven by the official validator, not asserted.</summary>
public sealed class ImmunologicAssembler : AbstractCaseAssembler
{
    public const string IgId = "tw.gov.mohw.nhi.pas#1.2.6";
    public const string Case = "immunologic-agent";
    public override string Ig => IgId;
    public override string CaseType => Case;

    [ModuleInitializer]
    internal static void Register() => AssemblerFactory.Register(IgId, Case, () => new ImmunologicAssembler());

    // BuildCase is unused: this case overrides Assemble wholesale (its Claim/Bundle profiles differ from the base).
    protected override CaseParts BuildCase(PACase c, Patient patient, Practitioner doctor, Organization hospital)
        => throw new NotSupportedException("immunologic-agent overrides Assemble; BuildCase is not used.");

    private static CodeableConcept Text(string text) => new() { Text = text };
    // Case-payload readers: a caller may override via PACase.Data; absent → the example default (keeps the golden stable).
    private static string S(PACase c, string k, string dflt) => c.Data.TryGetValue(k, out var v) ? (string)v : dflt;
    private static int I(PACase c, string k, int dflt) => c.Data.TryGetValue(k, out var v) ? Convert.ToInt32(v) : dflt;
    private static Attachment Pdf(string url, string title) =>
        new() { ContentType = "application/pdf", Url = url, Title = title };

    public override Bundle Assemble(PACase c)
    {
        var patient = BuildPatient(c);
        var doctor = BuildDoctor(c);
        var hospital = BuildHospital(c);
        var nhi = BuildNhi();
        var cov = BuildCoverage(patient, nhi);

        // --- Encounters: enc-min (the Claim's encounter) + enc-opd (門診就醫, anchors the SOAP note) ---
        var encMin = new Encounter
        {
            Id = "enc-min", Meta = Profile("Encounter-twpas"),
            Status = Encounter.EncounterStatus.Planned,
            Class = new Coding(Sys.V3ActCode, "AMB"),
            ServiceType = ServiceDeptOf(c),
        };
        var conDx = new Condition
        {
            Id = "con-diagnosis", Meta = Profile("Condition-twpas"),
            ClinicalStatus = Cc(Sys.ConditionClinical, "active"),
            Category = { Cc(Sys.Loinc, "29548-5") },
            Code = Cc(Sys.Icd10cmTw, "M17.11"),
            Subject = Ref(patient),
        };
        var conCo = new Condition
        {
            Id = "con-comorbidity", Meta = Profile("Condition-twpas"),
            ClinicalStatus = Cc(Sys.ConditionClinical, "active"),
            Category = { Cc(Sys.Loinc, "29548-5") },
            Code = Cc(Sys.Icd10cmTw, "I10"),
            Subject = Ref(patient),
        };
        var encOpd = new Encounter
        {
            Id = "enc-opd", Meta = Profile("Encounter-opd-twpas"),
            Status = Encounter.EncounterStatus.Finished,
            Class = new Coding(Sys.V3ActCode, "AMB"),
            ServiceType = Cc(Sys.Snomed, "419772000"),
            Subject = Ref(patient),
            Participant = { new Encounter.ParticipantComponent { Individual = Ref(doctor) } },
            Period = new Period { Start = "2025-11-11" },
            ServiceProvider = Ref(hospital),
            Diagnosis =
            {
                new Encounter.DiagnosisComponent { Condition = Ref(conDx), Rank = 1 },
                new Encounter.DiagnosisComponent { Condition = Ref(conCo), Rank = 2 },
            },
        };
        conDx.Encounter = Ref(encOpd);
        conCo.Encounter = Ref(encOpd);

        // --- SOAP note (Composition-opd + its four section targets) ---
        var obsSubj = new Observation
        {
            Id = "obs-subjective", Meta = Profile("Observation-subjective-twpas"),
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.ObsCategory, "survey") },
            Code = Cc(Sys.Loinc, "61150-9"),
            Subject = Ref(patient), Encounter = Ref(encOpd),
            Effective = new FhirDateTime("2025-11-11"),
            Value = new FhirString("Pain and tenderness of Right knee since yesterday."),
        };
        var obsObj = new Observation
        {
            Id = "obs-objective", Meta = Profile("Observation-objective-twpas"),
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.ObsCategory, "survey") },
            Code = Cc(Sys.Loinc, "61149-1"),
            Subject = Ref(patient), Encounter = Ref(encOpd),
            Effective = new FhirDateTime("2025-11-11"),
            Value = new FhirString("BP 110/70 mmHg  PR 80/min  BT 38.50C"),
        };
        var cliImp = new ClinicalImpression
        {
            Id = "cliImp-min", Meta = Profile("ClinicalImpression-twpas"),
            Status = ClinicalImpression.ClinicalImpressionStatus.Completed,
            Subject = Ref(patient), Encounter = Ref(encOpd),
            Summary = "Right knee . arthritis . active",
        };
        var carePlan = new CarePlan
        {
            Id = "careplan-min", Meta = Profile("CarePlan-twpas"),
            Status = RequestStatus.Completed,
            Intent = CarePlan.CarePlanIntent.Plan,
            Category = { Cc(Sys.CarePlanCatTw, "assess-plan") },
            Description = "1.Arrenge Arthrocentesis of Right knee and synovial fluid analysis (routine, culture and crystal analysis) after patient consent. 2.Analgesics. 3.Bed rest with ice packing if necessary.",
            Subject = Ref(patient), Encounter = Ref(encOpd),
        };
        var composition = new Composition
        {
            Id = "opd", Meta = Profile("Composition-opd-twpas"),
            Status = CompositionStatus.Final,
            Type = Cc(Sys.Loinc, "34108-1"),
            Subject = Ref(patient), Encounter = Ref(encOpd),
            Date = "2024-05-30",
            Author = { Ref(hospital) },
            Title = "門診病歷",
            Section =
            {
                new Composition.SectionComponent { Code = Cc(Sys.Loinc, "10154-3"), Entry = { Ref(obsSubj) } },
                new Composition.SectionComponent { Code = Cc(Sys.Loinc, "61149-1"), Entry = { Ref(obsObj) } },
                new Composition.SectionComponent { Code = Cc(Sys.Loinc, "51848-0"), Entry = { Ref(cliImp) } },
                new Composition.SectionComponent { Code = Cc(Sys.Loinc, "18776-5"), Entry = { Ref(carePlan) } },
            },
        };

        // --- Evidence chain ---
        var bloodGroup = new Observation
        {
            Id = "obs-blood-group", Meta = Profile("Observation-blood-group-twpas"),
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.CsSupportingInfo, "bloodgroup") },
            Code = Cc(Sys.Loinc, "882-1"),
            Subject = Ref(patient),
            Effective = new FhirDateTime("2024-05-07"),
            Value = new CodeableConcept(Sys.Snomed, "112144000"),
        };
        var allergy = new AllergyIntolerance
        {
            Id = "all-min", Meta = Profile("AllergyIntolerance-twpas"),
            ClinicalStatus = Cc(Sys.AllergyClinical, "active"),
            Code = Text("對 A 藥物過敏，過敏反應為全身性皮疹、呼吸喘，約在服用後15分鐘發生。曾於2023年5月因感冒服用過 A 藥，出現上述過敏反應，故此後避免使用。"),
            Patient = Ref(patient),
        };
        var imaStu = new ImagingStudy
        {
            Id = "imaStu-min", Meta = Profile("ImagingStudy-twpas"),
            Identifier = { new Identifier { System = "urn:dicom:uid", Value = "urn:oid:2.16.886.2102.54.4546465747.465465465" } },
            Status = ImagingStudy.ImagingStudyStatus.Registered,
            Subject = Ref(patient),
            Series =
            {
                new ImagingStudy.SeriesComponent
                {
                    Uid = "2.16.886.2102.54.4546465747.465465466",
                    Modality = new Coding(Sys.Dcm, "CT"),
                    BodySite = new Coding(Sys.Snomed, "774007") { Display = "Head and neck structure" },
                    Instance =
                    {
                        new ImagingStudy.InstanceComponent { Uid = "2.25.88017001449189502323411118737039844241", SopClass = new Coding("urn:ietf:rfc:3986", "urn:oid:1.2.840.10008.5.1.4.1.1.2") },
                        new ImagingStudy.InstanceComponent { Uid = "2.25.88017001449189502323411118737039844242", SopClass = new Coding("urn:ietf:rfc:3986", "urn:oid:1.2.840.10008.5.1.4.1.1.2") },
                    },
                },
            },
        };
        var media = new Media
        {
            Id = "med-min", Meta = Profile("Media-twpas"),
            Status = EventStatus.Completed,
            Subject = Ref(patient),
            BodySite = new CodeableConcept(Sys.Snomed, "774007") { Coding = { new Coding(Sys.Snomed, "774007") { Display = "Head and neck structure" } } },
            Content = new Attachment { ContentType = "image/jpeg", Url = "file://US01.jpg" },
        };
        var diaRepIma = new DiagnosticReport
        {
            Id = "diaRep-ima-min", Meta = Profile("DiagnosticReport-image-twpas"),
            Status = DiagnosticReport.DiagnosticReportStatus.Final,
            Category = { Cc(Sys.CsSupportingInfo, "imagingReport") },
            Code = Cc(Sys.Icd10pcsTw, "B34JZZ3"),
            Subject = Ref(patient),
            Effective = new FhirDateTime("2024-05-07"),
            Performer = { Ref(doctor) },
            ImagingStudy = { Ref(imaStu) },
            Conclusion = "影像報告結果",
            PresentedForm = { Pdf("file://ImagingDiagnosticReport01.pdf", "影像報告"), Pdf("file://ImagingDiagnosticReport02.pdf", "影像報告") },
        };
        var diaRep = new DiagnosticReport
        {
            Id = "diaRep-min", Meta = Profile("DiagnosticReport-twpas"),
            Status = DiagnosticReport.DiagnosticReportStatus.Final,
            Category = { Cc(Sys.CsSupportingInfo, "examinationReport") },
            Code = new CodeableConcept(Sys.Loinc, "66117-3") { Text = "Prostate" },
            Subject = Ref(patient),
            Effective = new FhirDateTime("2024-05-07"),
            Performer = { Ref(doctor) },
            Conclusion = "細胞檢查報告結果",
            PresentedForm = { Pdf("file://PathologyReport01.pdf", "PathologyReport01"), Pdf("file://PathologyReport02.pdf", "PathologyReport02") },
        };
        var docTest = new DocumentReference
        {
            Id = "doc-test-min", Meta = Profile("DocumentReference-twpas"),
            Status = DocumentReferenceStatus.Current,
            Category = { Cc(Sys.CsPdfType, "test") },
            Subject = Ref(patient),
            Content =
            {
                new DocumentReference.ContentComponent { Attachment = Pdf("file://TestReport01.pdf", "TestReport01") },
                new DocumentReference.ContentComponent { Attachment = Pdf("file://TestReport02.pdf", "TestReport02") },
            },
        };
        var obsLab = new Observation
        {
            Id = "obs-lab-min", Meta = Profile("Observation-laboratory-result-twpas"),
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.CsSupportingInfo, "tests") },
            Code = Cc(Sys.Loinc, "777-3"),
            Subject = Ref(patient),
            Effective = new FhirDateTime("2024-01-01"),
            Performer = { Ref(doctor) },
            Value = new Quantity { Value = 5.1m, Unit = "mmol/l" },
            Interpretation = { new CodeableConcept(Sys.V3InterpObs, "H") { Text = "高" } },
            ReferenceRange =
            {
                new Observation.ReferenceRangeComponent
                {
                    Low = new Quantity { Value = 2.9m, Unit = "mmol/l", System = Sys.Ucum, Code = "mmol/L" },
                    High = new Quantity { Value = 4.9m, Unit = "mmol/l", System = Sys.Ucum, Code = "mmol/L" },
                },
            },
            DerivedFrom = { Ref(docTest) },
        };
        var obsCbc = new Observation
        {
            Id = "obs-lab-cbc", Meta = Profile("Observation-laboratory-result-twpas"),
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.CsSupportingInfo, "tests") },
            Code = Cc(Sys.Loinc, "58410-2"),
            Subject = Ref(patient),
            Effective = new FhirDateTime("2024-01-01"),
            Performer = { Ref(doctor) },
            DerivedFrom = { Ref(docTest) },
            Component =
            {
                Cbc("6690-2", 3.6m, "10*3/uL", 3.9m, 10.6m),
                Cbc("789-8", 4.7m, "10*6/uL", 4.5m, 5.9m),
                Cbc("718-7", 14m, "g/dL", 13.5m, 17.5m),
                Cbc("4544-3", 45m, "%", 41m, 53m),
                Cbc("777-3", 101m, "10*3/uL", 150m, 400m),
            },
        };
        var obsNyha = new Observation
        {
            Id = "obs-pat-nyha", Meta = Profile("Observation-pat-assessment-twpas"),
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.CsSupportingInfo, "patientAssessment") },
            Code = Cc(Sys.Loinc, "88020-3"),
            Subject = Ref(patient),
            Effective = new FhirDateTime("2024-01-01"),
            Performer = { Ref(doctor) },
            Value = new FhirString("class1"),
        };
        var medTreat = new MedicationRequest
        {
            Id = "medReq-treat", Meta = Profile("MedicationRequest-treat-twpas"),
            Status = MedicationRequest.MedicationrequestStatus.Completed,
            StatusReason = Cc(Sys.MedReqStatusReason, "altchoice"),
            Intent = MedicationRequest.MedicationRequestIntent.Order,
            Category = { Cc(Sys.CsDrugCategory, "nhi") },
            Medication = new CodeableConcept(Sys.CsMedication, "A000755151"),
            Subject = Ref(patient),
            DosageInstruction =
            {
                new Dosage
                {
                    Timing = new Timing { Repeat = new Timing.RepeatComponent { Bounds = new Period { Start = "2024-05-01", End = "2024-05-07" } }, Code = Text("1W3D") },
                    Route = new CodeableConcept(Sys.Snomed, "26643006"),
                    DoseAndRate = { new Dosage.DoseAndRateComponent { Dose = new Quantity { Value = 4, Unit = "tablets", System = Sys.Ucum, Code = "{tbl}" } } },
                },
            },
        };
        var proMin = new Procedure
        {
            Id = "pro-min", Meta = Profile("Procedure-twpas"),
            Status = EventStatus.Completed,
            Code = Cc(Sys.Icd10pcsTw, "D0000ZZ"),
            Subject = Ref(patient),
            Performed = new FhirDateTime("2024-05-06T09:00:00.000Z"),
        };
        var subMin = new Substance
        {
            Id = "sub-min", Meta = Profile("Substance-twpas"),
            Code = Cc(Sys.Icd10pcsTw, "D0000ZZ"),
            Ingredient =
            {
                new Substance.IngredientComponent
                {
                    Quantity = new Ratio { Numerator = new Quantity { Value = 5000, System = Sys.Ucum, Code = "mg" }, Denominator = new Quantity { Value = 0 } },
                    Substance = Cc(Sys.Icd10pcsTw, "D0000ZZ"),
                },
            },
        };
        proMin.UsedReference.Add(Ref(subMin));
        var subPhoto = new Substance
        {
            Id = "sub-phototherapy", Meta = Profile("Substance-phototherapy-twpas"),
            Code = Cc(Sys.CsPhototherapy, "nb-UVB"),
            Ingredient =
            {
                new Substance.IngredientComponent
                {
                    Quantity = new Ratio { Numerator = new Quantity { Value = 24 }, Denominator = new Quantity { Value = 1 } },
                    Substance = Cc(Sys.CsPhototherapy, "nb-UVB"),
                },
            },
        };
        var proPhoto = new Procedure
        {
            Id = "pro-phototherapy", Meta = Profile("Procedure-phototherapy-twpas"),
            Status = EventStatus.Completed,
            Code = Cc(Sys.CsPhototherapy, "nb-UVB", text: "窄頻UVB(nb-UVB)"),
            Subject = Ref(patient),
            Performed = new Period { Start = "2024-01-01", End = "2024-03-31" },
            UsedReference = { Ref(subPhoto) },
        };
        var docCareplan = Doc("doc-careplan-min", "careplan", patient, Pdf("file://CarePlanReport01.pdf", "免疫檢查點抑制劑治療計畫"));
        var obsTx = new Observation
        {
            Id = "obs-tx-min", Meta = Profile("Observation-tx-assessment-twpas"),
            Status = ObservationStatus.Final,
            Code = Cc(Sys.CsTxAst, "IWGC", text: "International Working Group(IWG) Consensus Criteria"),
            Subject = Ref(patient),
            Effective = new FhirDateTime("2024-05-07"),
            Performer = { Ref(doctor) },
            Value = new FhirString("Partial remission (PR)"),
        };
        var docMedRec = Doc("doc-medicalRecord-min", "medrec", patient, Pdf("file://王大明病歷.pdf", "王大明病歷"));
        var docPatAssess = Doc("doc-patientAssessment-min", "patientAssessment", patient, Pdf("file://病人狀態評估報告.pdf", "病人狀態評估報告名稱"));

        // --- Applied drugs (the request items) ---
        var medApply1 = new MedicationRequest
        {
            Id = "medReq-apply", Meta = Profile("MedicationRequest-apply-twpas"),
            Status = MedicationRequest.MedicationrequestStatus.OnHold,
            Intent = MedicationRequest.MedicationRequestIntent.Plan,
            Medication = new CodeableConcept(Sys.CsMedication, S(c, "drug_code", "BC27730100")),
            Subject = Ref(patient),
            DosageInstruction =
            {
                ApplyDose("2024-01-01", "2024-02-11", 42, 75, new[] { (Sys.GtsAbbrev, "QD"), (Sys.MedFreqNhi, "AC1H") }),
                ApplyDose("2024-02-12", "2024-03-10", 1, 150, new[] { (Sys.MedFreqNhi, "Q4WD1"), (Sys.MedFreqNhi, "Q4WD2"), (Sys.MedFreqNhi, "Q4WD3"), (Sys.MedFreqNhi, "Q4WD4"), (Sys.MedFreqNhi, "Q4WD5"), (Sys.MedFreqNhi, "AC1H") }),
            },
        };
        var medApply2 = new MedicationRequest
        {
            Id = "medReq-apply-2", Meta = Profile("MedicationRequest-apply-twpas"),
            Status = MedicationRequest.MedicationrequestStatus.OnHold,
            Intent = MedicationRequest.MedicationRequestIntent.Plan,
            Medication = new CodeableConcept(Sys.CsMedication, S(c, "drug_code_2", "KC011162B5")),
            Subject = Ref(patient),
            DosageInstruction = { ApplyDose("2024-03-11", "2024-07-28", 1, 200, new[] { (Sys.MedFreqNhi, "Q4WD1"), (Sys.MedFreqNhi, "Q4WD2"), (Sys.MedFreqNhi, "Q4WD3"), (Sys.MedFreqNhi, "Q4WD4"), (Sys.MedFreqNhi, "Q4WD5"), (Sys.MedFreqNhi, "AC1H") }) },
            DispenseRequest = new MedicationRequest.DispenseRequestComponent { Quantity = new Quantity { System = Sys.OrderableDrugForm } },
        };

        // --- Claim (Claim-immunologic-agent-twpas) ---
        var claim = new Claim
        {
            Id = "cla-imm", Meta = Profile("Claim-immunologic-agent-twpas"),
            Status = FinancialResourceStatusCodes.Active,
            Type = Cc(Sys.ClaimType, "institutional"),
            SubType = SubTypeOf(c),
            Use = ClaimUseCode.Preauthorization,
            Patient = Ref(patient), Created = c.Created, Enterer = Ref(doctor), Provider = Ref(hospital),
            Priority = PriorityOf(c),
            Insurance = { new Claim.InsuranceComponent { Sequence = 1, Focal = true, Coverage = Ref(cov) } },
        };
        claim.Extension.Add(new Extension(Sys.ExtClaimEncounter, Ref(encMin)));
        int s = 1;
        claim.SupportingInfo.Add(Vital(s++, "weight", (decimal)c.Vitals["weight_kg"], "kg"));
        claim.SupportingInfo.Add(Vital(s++, "height", (decimal)c.Vitals["height_cm"], "cm"));
        foreach (var (cat, report) in new (string, Resource)[]
        {
            ("bloodgroup", bloodGroup), ("imagingReport", diaRepIma), ("examinationReport", diaRep),
            ("tests", obsLab), ("tests", obsCbc), ("patientAssessment", obsNyha), ("medicationRequest", medTreat),
            ("radiotherapy", proMin), ("carePlanDocument", docCareplan), ("medicalRecord", docMedRec),
            ("treatmentAssessment", obsTx), ("opd", composition), ("allergy", allergy), ("phototherapy", proPhoto),
        })
            claim.SupportingInfo.Add(new Claim.SupportingInformationComponent
            { Sequence = s++, Category = Cc(Sys.CsSupportingInfo, cat), Value = Ref(report) });

        var dx = new Claim.DiagnosisComponent { Sequence = 1, Diagnosis = Cc(Sys.Icd10cmTw, S(c, "diagnosis_icd", "I50.812")) };
        dx.Type.Add(Text(S(c, "diagnosis_text", "Adenocarcinoma, descending colon, cT3N2M1a, cStage IVA, KRAS G12V, with multiple liver metastases, status post FOLFIRI")));
        dx.Extension.Add(new Extension(Sys.ExtDxRecordedDate, new Date(S(c, "diagnosis_date", "2024-01-01"))));
        claim.Diagnosis.Add(dx);
        claim.Procedure.Add(new Claim.ProcedureComponent { Sequence = 1, Date = S(c, "procedure_date", "2024-01-01"), Procedure = Cc(Sys.Icd10pcsTw, S(c, "procedure_icd", "3E0Y704")) });

        var item1 = new Claim.ItemComponent
        {
            Sequence = 1,
            ProductOrService = Cc(Sys.CsOrderType, "1", text: "藥品"),
            Modifier = { Cc(Sys.CsContinuation, "1", text: "初次使用"), Cc(Sys.CsLineOfTherapy, "1", text: "第一線治療") },
            ProgramCode = { Text(S(c, "program_text", "ALK陽性的晚期非小細胞肺癌第一線治療")) },
            Quantity = new Quantity { Value = I(c, "drug_qty", 52), System = Sys.Ucum, Code = "{tbl}" },
        };
        item1.Extension.Add(new Extension(Sys.ExtRequestedService, Ref(medApply1)));
        var item2 = new Claim.ItemComponent
        {
            Sequence = 2,
            ProductOrService = Cc(Sys.CsOrderType, "1", text: "藥品"),
            Modifier = { Cc(Sys.CsContinuation, "1", text: "初次使用"), Cc(Sys.CsLineOfTherapy, "1", text: "第一線治療") },
            ProgramCode = { new CodeableConcept(Sys.CsApplyReason, S(c, "apply_reason", "C50P1")) },
            Quantity = new Quantity { Value = I(c, "drug_qty_2", 70), System = Sys.Ucum, Code = "{tbl}" },
        };
        item2.Extension.Add(new Extension(Sys.ExtRequestedService, Ref(medApply2)));
        claim.Item.Add(item1);
        claim.Item.Add(item2);

        var ordered = new List<Resource>
        {
            claim, composition, encOpd, encMin, patient, doctor, hospital, conDx, conCo,
            bloodGroup, allergy, obsSubj, obsObj, cliImp, carePlan, diaRepIma, imaStu, media, diaRep,
            obsLab, obsCbc, docTest, obsNyha, medTreat, proMin, subMin, proPhoto, subPhoto, docCareplan,
            obsTx, medApply1, medApply2, cov, nhi, docMedRec, docPatAssess,
        };
        return WrapBundle("Bundle-immunologic-agent-twpas", ordered);
    }

    private static Observation.ComponentComponent Cbc(string loinc, decimal value, string unit, decimal low, decimal high) => new()
    {
        Code = Cc(Sys.Loinc, loinc),
        Value = new Quantity { Value = value, Unit = unit },
        ReferenceRange = { new Observation.ReferenceRangeComponent { Low = new Quantity { Value = low, Unit = unit }, High = new Quantity { Value = high, Unit = unit } } },
    };

    private DocumentReference Doc(string id, string pdfType, Patient patient, Attachment att) => new()
    {
        Id = id, Meta = Profile("DocumentReference-twpas"),
        Status = DocumentReferenceStatus.Current,
        Category = { Cc(Sys.CsPdfType, pdfType) },
        Subject = Ref(patient),
        Content = { new DocumentReference.ContentComponent { Attachment = att } },
    };

    private static Dosage ApplyDose(string start, string end, int count, decimal doseMg, (string sys, string code)[] freq)
    {
        var code = new CodeableConcept();
        foreach (var (sys, c) in freq) code.Coding.Add(new Coding(sys, c));
        return new Dosage
        {
            Timing = new Timing { Repeat = new Timing.RepeatComponent { Bounds = new Period { Start = start, End = end }, Count = count }, Code = code },
            Route = new CodeableConcept(Sys.Snomed, "26643006"),
            DoseAndRate = { new Dosage.DoseAndRateComponent { Dose = new Quantity { Value = doseMg, System = Sys.Ucum, Code = "mg/m2" } } },
        };
    }
}
