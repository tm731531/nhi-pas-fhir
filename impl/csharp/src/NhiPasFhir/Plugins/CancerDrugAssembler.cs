using System.Runtime.CompilerServices;
using Hl7.Fhir.Model;
using NhiPasFhir.Core;

namespace NhiPasFhir.Plugins;

/// <summary>癌藥/一般送核 case type — reproduces the official full example Bundle-bun-1 (28 resources):
/// imaging chain + cancer-stage + gene/diagnostic (+ Specimen + genetic-testing Org) + exam/lab evidence +
/// procedure/substance + assessments + two applied drugs. Values transcribed from the official example
/// (fabricated, no PHI); case payload + 申報別/案件別 come from PACase. Validated to 0 errors.
/// (Shares much of its evidence chain with ImmunologicAssembler — a shared builder extraction is a TODO.)</summary>
public sealed class CancerDrugAssembler : AbstractCaseAssembler
{
    public const string IgId = "tw.gov.mohw.nhi.pas#1.2.6";
    public const string Case = "cancer-drug";
    public override string Ig => IgId;
    public override string CaseType => Case;

    [ModuleInitializer]
    internal static void Register() => AssemblerFactory.Register(IgId, Case, () => new CancerDrugAssembler());

    protected override CaseParts BuildCase(PACase c, Patient patient, Practitioner doctor, Organization hospital)
        => throw new NotSupportedException("cancer-drug overrides Assemble; BuildCase is not used.");

    private static CodeableConcept Cc(string sys, string code, string? display = null) => new(sys, code, display);
    private static CodeableConcept Text(string text) => new() { Text = text };
    private static Attachment Pdf(string url, string title) => new() { ContentType = "application/pdf", Url = url, Title = title };
    private static string S(PACase c, string k, string dflt) => c.Data.TryGetValue(k, out var v) ? (string)v : dflt;
    private static int I(PACase c, string k, int dflt) => c.Data.TryGetValue(k, out var v) ? Convert.ToInt32(v) : dflt;

    public override Bundle Assemble(PACase c)
    {
        var patient = BuildPatient(c);
        var doctor = BuildDoctor(c);
        var hospital = BuildHospital(c);
        var nhi = BuildNhi();
        var cov = BuildCoverage(patient, nhi);
        var enc = new Encounter
        {
            Id = "enc-min", Meta = Profile("Encounter-twpas"),
            Status = Encounter.EncounterStatus.Planned,
            Class = new Coding(Sys.V3ActCode, "AMB"),
            ServiceType = Cc(Sys.ServiceDept, "AJ"),
        };

        // --- imaging chain ---
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
            Status = EventStatus.Completed, Subject = Ref(patient),
            BodySite = new CodeableConcept(Sys.Snomed, "774007") { Coding = { new Coding(Sys.Snomed, "774007") { Display = "Head and neck structure" } } },
            Content = new Attachment { ContentType = "image/jpeg", Url = "file://US01.jpg" },
        };
        var diaRepIma = new DiagnosticReport
        {
            Id = "diaRep-ima-min", Meta = Profile("DiagnosticReport-image-twpas"),
            Status = DiagnosticReport.DiagnosticReportStatus.Final,
            Category = { Cc(Sys.CsSupportingInfo, "imagingReport") },
            Code = Cc(Sys.Icd10pcsTw, "B34JZZ3"),
            Subject = Ref(patient), Effective = new FhirDateTime("2024-05-07"), Performer = { Ref(doctor) },
            ImagingStudy = { Ref(imaStu) }, Conclusion = "影像報告結果",
            PresentedForm = { Pdf("file://ImagingDiagnosticReport01.pdf", "影像報告"), Pdf("file://ImagingDiagnosticReport02.pdf", "影像報告") },
        };

        // --- cancer stage ---
        var obsCancerStage = new Observation
        {
            Id = "obs-cancer-figo", Meta = Profile("Observation-cancer-stage-twpas"),
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.CsSupportingInfo, "cancerStage") },
            Code = Cc(Sys.Snomed, "385361009"),
            Subject = Ref(patient), Effective = new FhirDateTime("2024-05-07"), Performer = { Ref(doctor) },
            Value = new CodeableConcept(Sys.NciThesaurus, "C96244"),
        };

        // --- examination report ---
        var diaRep = new DiagnosticReport
        {
            Id = "diaRep-min", Meta = Profile("DiagnosticReport-twpas"),
            Status = DiagnosticReport.DiagnosticReportStatus.Final,
            Category = { Cc(Sys.CsSupportingInfo, "examinationReport") },
            Code = new CodeableConcept(Sys.Loinc, "66117-3") { Text = "Prostate" },
            Subject = Ref(patient), Effective = new FhirDateTime("2024-05-07"), Performer = { Ref(doctor) },
            Conclusion = "細胞檢查報告結果",
            PresentedForm = { Pdf("file://PathologyReport01.pdf", "PathologyReport01"), Pdf("file://PathologyReport02.pdf", "PathologyReport02") },
        };

        // --- gene diagnostic (+ Specimen + gene doc + genetic-testing Org) ---
        var orgGene = new Organization
        {
            Id = "org-gene-example", Meta = Profile("Organization-genetic-testing-twpas"),
            Identifier = { new Identifier { System = "https://dep.mohw.gov.tw", Value = "2023LDTB0002" } },
        };
        var specimen = new Specimen
        {
            Id = "spe-min", Meta = Profile("Specimen-twpas"),
            Type = Cc(Sys.Loinc, "LP7057-5"), Subject = Ref(patient),
            ReceivedTime = "2024-05-06T09:00:00.000Z",
        };
        var docGene = Doc("doc-gene-min", "gene", patient, Pdf("file://GenReport01.pdf", "GenReport01"));
        var obsDiagnostic = new Observation
        {
            Id = "obs-diagnostic-min", Meta = Profile("Observation-diagnostic-twpas"),
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.CsSupportingInfo, "geneInfo") },
            Code = Cc(Sys.Loinc, "69548-6"),
            Subject = Ref(patient), Effective = new FhirDateTime("2024-05-07"), Performer = { Ref(orgGene) },
            Value = new FhirString("基因檢測報告結果"),
            Interpretation = { new CodeableConcept(Sys.V3InterpObs, "POS") },
            Method = Cc(Sys.Loinc, "LA26418-6"),
            Specimen = Ref(specimen),
            DerivedFrom = { Ref(docGene) },
            Component =
            {
                new Observation.ComponentComponent
                {
                    Code = Cc(Sys.Loinc, "21702-6"),
                    Value = new FhirString("KRAS 12 mutation: Not Detected, KRAS 13 mutation: Not Detected, KRAS 61 mutation: Not Detected"),
                    Interpretation = { new CodeableConcept(Sys.Loinc, "LA11883-8", "Not detected") { Text = "Not detected" } },
                },
            },
        };

        // --- lab evidence ---
        var docTest = new DocumentReference
        {
            Id = "doc-test-min", Meta = Profile("DocumentReference-twpas"),
            Status = DocumentReferenceStatus.Current,
            Category = { Cc(Sys.CsPdfType, "test") }, Subject = Ref(patient),
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
            Category = { Cc(Sys.CsSupportingInfo, "tests") }, Code = Cc(Sys.Loinc, "777-3"),
            Subject = Ref(patient), Effective = new FhirDateTime("2024-01-01"), Performer = { Ref(doctor) },
            Value = new Quantity { Value = 5.1m, Unit = "mmol/l" },
            Interpretation = { new CodeableConcept(Sys.V3InterpObs, "H") { Text = "高" } },
            ReferenceRange = { new Observation.ReferenceRangeComponent
                { Low = new Quantity { Value = 2.9m, Unit = "mmol/l", System = Sys.Ucum, Code = "mmol/L" },
                  High = new Quantity { Value = 4.9m, Unit = "mmol/l", System = Sys.Ucum, Code = "mmol/L" } } },
            DerivedFrom = { Ref(docTest) },
        };
        var obsCbc = new Observation
        {
            Id = "obs-lab-cbc", Meta = Profile("Observation-laboratory-result-twpas"),
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.CsSupportingInfo, "tests") }, Code = Cc(Sys.Loinc, "58410-2"),
            Subject = Ref(patient), Effective = new FhirDateTime("2024-01-01"), Performer = { Ref(doctor) },
            DerivedFrom = { Ref(docTest) },
            Component =
            {
                Cbc("6690-2", 3.6m, "10*3/uL", 3.9m, 10.6m), Cbc("789-8", 4.7m, "10*6/uL", 4.5m, 5.9m),
                Cbc("718-7", 14m, "g/dL", 13.5m, 17.5m), Cbc("4544-3", 45m, "%", 41m, 53m),
                Cbc("777-3", 101m, "10*3/uL", 150m, 400m),
            },
        };

        // --- assessments + treatment + procedure/substance + docs ---
        var obsNyha = new Observation
        {
            Id = "obs-pat-nyha", Meta = Profile("Observation-pat-assessment-twpas"),
            Status = ObservationStatus.Final, Category = { Cc(Sys.CsSupportingInfo, "patientAssessment") },
            Code = Cc(Sys.Loinc, "88020-3"), Subject = Ref(patient), Effective = new FhirDateTime("2024-01-01"),
            Performer = { Ref(doctor) }, Value = new FhirString("class1"),
        };
        var medTreat = new MedicationRequest
        {
            Id = "medReq-treat", Meta = Profile("MedicationRequest-treat-twpas"),
            Status = MedicationRequest.MedicationrequestStatus.Completed,
            StatusReason = Cc(Sys.MedReqStatusReason, "altchoice"),
            Intent = MedicationRequest.MedicationRequestIntent.Order,
            Category = { Cc(Sys.CsDrugCategory, "nhi") },
            Medication = new CodeableConcept(Sys.CsMedication, "A000755151"), Subject = Ref(patient),
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
            Id = "pro-min", Meta = Profile("Procedure-twpas"), Status = EventStatus.Completed,
            Code = Cc(Sys.Icd10pcsTw, "D0000ZZ"), Subject = Ref(patient), Performed = new FhirDateTime("2024-05-06T09:00:00.000Z"),
        };
        var subMin = new Substance
        {
            Id = "sub-min", Meta = Profile("Substance-twpas"), Code = Cc(Sys.Icd10pcsTw, "D0000ZZ"),
            Ingredient = { new Substance.IngredientComponent
                { Quantity = new Ratio { Numerator = new Quantity { Value = 5000, System = Sys.Ucum, Code = "mg" }, Denominator = new Quantity { Value = 0 } },
                  Substance = Cc(Sys.Icd10pcsTw, "D0000ZZ") } },
        };
        proMin.UsedReference.Add(Ref(subMin));
        var docCareplan = Doc("doc-careplan-min", "careplan", patient, Pdf("file://CarePlanReport01.pdf", "免疫檢查點抑制劑治療計畫"));
        var obsTx = new Observation
        {
            Id = "obs-tx-min", Meta = Profile("Observation-tx-assessment-twpas"), Status = ObservationStatus.Final,
            Code = Cc(Sys.CsTxAst, "IWGC", "International Working Group(IWG) Consensus Criteria"),
            Subject = Ref(patient), Effective = new FhirDateTime("2024-05-07"), Performer = { Ref(doctor) },
            Value = new FhirString("Partial remission (PR)"),
        };
        var docMedRec = Doc("doc-medicalRecord-min", "medrec", patient, Pdf("file://王大明病歷.pdf", "王大明病歷"));

        // --- applied drugs ---
        var medApply1 = new MedicationRequest
        {
            Id = "medReq-apply", Meta = Profile("MedicationRequest-apply-twpas"),
            Status = MedicationRequest.MedicationrequestStatus.OnHold, Intent = MedicationRequest.MedicationRequestIntent.Plan,
            Medication = new CodeableConcept(Sys.CsMedication, S(c, "drug_code", "BC27730100")), Subject = Ref(patient),
            DosageInstruction =
            {
                ApplyDose("2024-01-01", "2024-02-11", 42, 75, new[] { (Sys.GtsAbbrev, "QD"), (Sys.MedFreqNhi, "AC1H") }),
                ApplyDose("2024-02-12", "2024-03-10", 1, 150, new[] { (Sys.MedFreqNhi, "Q4WD1"), (Sys.MedFreqNhi, "Q4WD2"), (Sys.MedFreqNhi, "Q4WD3"), (Sys.MedFreqNhi, "Q4WD4"), (Sys.MedFreqNhi, "Q4WD5"), (Sys.MedFreqNhi, "AC1H") }),
            },
        };
        var medApply2 = new MedicationRequest
        {
            Id = "medReq-apply-2", Meta = Profile("MedicationRequest-apply-twpas"),
            Status = MedicationRequest.MedicationrequestStatus.OnHold, Intent = MedicationRequest.MedicationRequestIntent.Plan,
            Medication = new CodeableConcept(Sys.CsMedication, S(c, "drug_code_2", "KC011162B5")), Subject = Ref(patient),
            DosageInstruction = { ApplyDose("2024-03-11", "2024-07-28", 1, 200, new[] { (Sys.MedFreqNhi, "Q4WD1"), (Sys.MedFreqNhi, "Q4WD2"), (Sys.MedFreqNhi, "Q4WD3"), (Sys.MedFreqNhi, "Q4WD4"), (Sys.MedFreqNhi, "Q4WD5"), (Sys.MedFreqNhi, "AC1H") }) },
            DispenseRequest = new MedicationRequest.DispenseRequestComponent { Quantity = new Quantity { System = Sys.OrderableDrugForm } },
        };

        // --- Claim (Claim-twpas) ---
        var claim = new Claim
        {
            Id = "cla-1", Meta = Profile("Claim-twpas"),
            Status = FinancialResourceStatusCodes.Active, Type = Cc(Sys.ClaimType, "institutional"),
            SubType = SubTypeOf(c), Use = ClaimUseCode.Preauthorization, Priority = PriorityOf(c),
            Patient = Ref(patient), Created = c.Created, Enterer = Ref(doctor), Provider = Ref(hospital),
            Insurance = { new Claim.InsuranceComponent { Sequence = 1, Focal = true, Coverage = Ref(cov) } },
        };
        claim.Extension.Add(new Extension(Sys.ExtClaimEncounter, Ref(enc)));
        int s = 1;
        claim.SupportingInfo.Add(Vital(s++, "weight", (decimal)c.Vitals["weight_kg"], "kg"));
        claim.SupportingInfo.Add(Vital(s++, "height", (decimal)c.Vitals["height_cm"], "cm"));
        claim.SupportingInfo.Add(new Claim.SupportingInformationComponent
        { Sequence = s++, Category = Cc(Sys.CsSupportingInfo, "pregnancyBreastfeedingStatus"), Value = new FhirBoolean(false) });
        foreach (var (cat, report) in new (string, Resource)[]
        {
            ("imagingReport", diaRepIma), ("cancerStage", obsCancerStage), ("examinationReport", diaRep),
            ("geneInfo", obsDiagnostic), ("tests", obsLab), ("tests", obsCbc), ("patientAssessment", obsNyha),
            ("medicationRequest", medTreat), ("radiotherapy", proMin), ("carePlanDocument", docCareplan),
            ("medicalRecord", docMedRec), ("treatmentAssessment", obsTx),
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
            Sequence = 1, ProductOrService = Cc(Sys.CsOrderType, "1", "藥品"),
            Modifier = { Cc(Sys.CsContinuation, "1", "初次使用"), Cc(Sys.CsLineOfTherapy, "1", "第一線治療") },
            ProgramCode = { Text(S(c, "program_text", "ALK陽性的晚期非小細胞肺癌第一線治療")) },
            Quantity = new Quantity { Value = I(c, "drug_qty", 52), System = Sys.Ucum, Code = "{tbl}" },
        };
        item1.Extension.Add(new Extension(Sys.ExtRequestedService, Ref(medApply1)));
        var item2 = new Claim.ItemComponent
        {
            Sequence = 2, ProductOrService = Cc(Sys.CsOrderType, "1", "藥品"),
            Modifier = { Cc(Sys.CsContinuation, "1", "初次使用"), Cc(Sys.CsLineOfTherapy, "1", "第一線治療") },
            ProgramCode = { new CodeableConcept(Sys.CsApplyReason, S(c, "apply_reason", "C50P1")) },
            Quantity = new Quantity { Value = I(c, "drug_qty_2", 70), System = Sys.Ucum, Code = "{tbl}" },
        };
        item2.Extension.Add(new Extension(Sys.ExtRequestedService, Ref(medApply2)));
        claim.Item.Add(item1);
        claim.Item.Add(item2);
        // 補件/申復/爭議 need the original acceptance number (invariant applType).
        if (c.Data.TryGetValue("old_acpt_no", out var acpt))
        {
            if (c.Data.TryGetValue("filing_ref", out var fref))
                claim.Identifier.Add(new Identifier { Use = Identifier.IdentifierUse.Usual, Value = (string)fref });
            claim.Identifier.Add(new Identifier { Use = Identifier.IdentifierUse.Secondary, Value = (string)acpt });
        }

        var ordered = new List<Resource>
        {
            claim, enc, patient, doctor, hospital, diaRepIma, imaStu, media, obsCancerStage, diaRep,
            obsDiagnostic, specimen, docGene, obsLab, obsCbc, docTest, obsNyha, medTreat, proMin, subMin,
            docCareplan, obsTx, medApply1, medApply2, cov, nhi, orgGene, docMedRec,
        };
        if (c.Data.TryGetValue("priority_code", out var pc) && (string)pc == "3")
            ordered.Add(BuildSelfAssessment(c, patient, nhi, claim));
        return WrapBundle("Bundle-twpas", ordered);
    }

    private static Observation.ComponentComponent Cbc(string loinc, decimal value, string unit, decimal low, decimal high) => new()
    {
        Code = Cc(Sys.Loinc, loinc), Value = new Quantity { Value = value, Unit = unit },
        ReferenceRange = { new Observation.ReferenceRangeComponent { Low = new Quantity { Value = low, Unit = unit }, High = new Quantity { Value = high, Unit = unit } } },
    };

    private DocumentReference Doc(string id, string pdfType, Patient patient, Attachment att) => new()
    {
        Id = id, Meta = Profile("DocumentReference-twpas"), Status = DocumentReferenceStatus.Current,
        Category = { Cc(Sys.CsPdfType, pdfType) }, Subject = Ref(patient),
        Content = { new DocumentReference.ContentComponent { Attachment = att } },
    };

    private static Dosage ApplyDose(string start, string end, int count, decimal doseMg, (string sys, string code)[] freq)
    {
        var code = new CodeableConcept();
        foreach (var (sys, cc) in freq) code.Coding.Add(new Coding(sys, cc));
        return new Dosage
        {
            Timing = new Timing { Repeat = new Timing.RepeatComponent { Bounds = new Period { Start = start, End = end }, Count = count }, Code = code },
            Route = new CodeableConcept(Sys.Snomed, "26643006"),
            DoseAndRate = { new Dosage.DoseAndRateComponent { Dose = new Quantity { Value = doseMg, System = Sys.Ucum, Code = "mg/m2" } } },
        };
    }
}
