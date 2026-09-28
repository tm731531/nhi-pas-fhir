using Hl7.Fhir.Model;

namespace NhiPasFhir;

/// <summary>Individual resource variants from the official examples — same profile, different clinical data.
/// Producing each and validating it to 0 errors proves the library stays in sync with the official IG for
/// every documented value shape (cancer-stage integer/string/coded, patient-assessment quantity/component,
/// imaging report by LOINC, phototherapy doc, resident patient by 居留證). References point to the request
/// bundle's patient/practitioner/study. All values transcribed from the official examples; no PHI.</summary>
public static class Variants
{
    private static Meta P(string name) => new() { Profile = new[] { $"{Sys.Sd}/{name}" } };
    private static readonly ResourceReference Pat = new("Patient/pat-1");
    private static readonly ResourceReference Pra = new("Practitioner/pra-1");
    private static CodeableConcept Cat(string code) => new(Sys.CsSupportingInfo, code);

    /// <summary>癌症分期 — 中樞神經分期(整數值).</summary>
    public static Observation CancerStageCns() => new()
    {
        Id = "obs-cancer-cns", Meta = P("Observation-cancer-stage-twpas"), Status = ObservationStatus.Final,
        Category = { Cat("cancerStage") }, Code = new CodeableConcept(Sys.Snomed, "277460003"),
        Subject = Pat, Effective = new FhirDateTime("2024-05-07"), Performer = { Pra }, Value = new Integer(2),
    };

    /// <summary>癌症分期 — TNM(字串值).</summary>
    public static Observation CancerStageTnm() => new()
    {
        Id = "obs-cancer-tnm", Meta = P("Observation-cancer-stage-twpas"), Status = ObservationStatus.Final,
        Category = { Cat("cancerStage") }, Code = new CodeableConcept(Sys.Snomed, "399390009"),
        Subject = Pat, Effective = new FhirDateTime("2024-05-07"), Performer = { Pra }, Value = new FhirString("pT4aN2bM1a"),
    };

    /// <summary>病人狀態評估 — PDAI(數量值).</summary>
    public static Observation PatAssessmentPdai() => new()
    {
        Id = "obs-pat-pdai", Meta = P("Observation-pat-assessment-twpas"), Status = ObservationStatus.Final,
        Category = { Cat("patientAssessment") }, Code = new CodeableConcept(Sys.CsPatAst, "PDAI"),
        Subject = Pat, Effective = new FhirDateTime("2024-01-01"), Performer = { Pra }, Value = new Quantity { Value = 260.02m },
    };

    /// <summary>病人狀態評估 — CTCAE(多 component 分級).</summary>
    public static Observation PatAssessmentCtcae() => new()
    {
        Id = "obs-pat-ctcae", Meta = P("Observation-pat-assessment-twpas"), Status = ObservationStatus.Final,
        Category = { Cat("patientAssessment") }, Code = new CodeableConcept(Sys.Snomed, "711434002"),
        Subject = Pat, Effective = new FhirDateTime("2025-01-01"), Performer = { Pra },
        Component =
        {
            new Observation.ComponentComponent { Code = new CodeableConcept(Sys.CsPatAst, "C143528"), Value = new FhirString("grade3") },
            new Observation.ComponentComponent { Code = new CodeableConcept(Sys.CsPatAst, "C143750"), Value = new FhirString("grade4") },
            new Observation.ComponentComponent { Code = new CodeableConcept(Sys.CsPatAst, "C143752"), Value = new FhirString("grade5") },
        },
    };

    /// <summary>影像報告 — 以 LOINC 編碼(而非 ICD-10-PCS).</summary>
    public static DiagnosticReport ImagingReportLoinc() => new()
    {
        Id = "diaRep-ima-loinc", Meta = P("DiagnosticReport-image-twpas"), Status = DiagnosticReport.DiagnosticReportStatus.Final,
        Category = { Cat("imagingReport") }, Code = new CodeableConcept(Sys.Loinc, "18748-4"),
        Subject = Pat, Effective = new FhirDateTime("2024-05-07"), Performer = { Pra },
        ImagingStudy = { new ResourceReference("ImagingStudy/imaStu-min") }, Conclusion = "影像報告結果",
        PresentedForm =
        {
            new Attachment { ContentType = "application/pdf", Url = "file://ImagingDiagnosticReport01.pdf", Title = "影像報告" },
            new Attachment { ContentType = "application/pdf", Url = "file://ImagingDiagnosticReport02.pdf", Title = "影像報告" },
        },
    };

    /// <summary>照光治療文件.</summary>
    public static DocumentReference PhototherapyDoc() => new()
    {
        Id = "doc-phototherapy-min", Meta = P("DocumentReference-twpas"), Status = DocumentReferenceStatus.Current,
        Category = { new CodeableConcept(Sys.CsPdfType, "phototherapy") }, Subject = Pat,
        Content = { new DocumentReference.ContentComponent { Attachment = new Attachment { ContentType = "application/pdf", Url = "file://ptReport01.pdf", Title = "ptReport01" } } },
    };

    /// <summary>居留證病人(PRC / 移民署),非本國身分證.</summary>
    public static Patient ResidentPatient() => new()
    {
        Id = "pat-resident", Meta = P("Patient-twpas"),
        Identifier =
        {
            new Identifier { Use = Identifier.IdentifierUse.Official, Type = new CodeableConcept(Sys.V2_0203, "PRC"), System = Sys.Immigration, Value = "AB12345678" },
            new Identifier { Use = Identifier.IdentifierUse.Official, Type = new CodeableConcept(Sys.V2_0203, "MR"), System = "https://tpech.gov.taipei", Value = "123456" },
        },
        Name = { new HumanName { Use = HumanName.NameUse.Usual, Text = "王小明" } },
        Gender = AdministrativeGender.Male, BirthDate = "2001-01-01",
    };

    /// <summary>All variants, keyed by output filename, for emit + validation.</summary>
    public static IReadOnlyList<(string File, Resource Resource)> All() => new (string, Resource)[]
    {
        ("obs-cancer-cns", CancerStageCns()), ("obs-cancer-tnm", CancerStageTnm()),
        ("obs-pat-pdai", PatAssessmentPdai()), ("obs-pat-ctcae", PatAssessmentCtcae()),
        ("diaRep-ima-loinc", ImagingReportLoinc()), ("doc-phototherapy", PhototherapyDoc()),
        ("pat-resident", ResidentPatient()),
    };
}
