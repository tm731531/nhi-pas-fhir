using System.Runtime.CompilerServices;
using Hl7.Fhir.Model;
using NhiPasFhir.Core;
using static NhiPasFhir.Core.FhirBuild;

namespace NhiPasFhir.Plugins;

// IG 對照: 次世代基因定序 NGS (tw.gov.mohw.nhi.ngs 1.0.0) — 官方範例 Bundle-bun-nos-min (document, 11 resources).
// Every code/system/value transcribed VERBATIM from the official example (fabricated data, no PHI).

/// <summary>次世代基因定序 (NGS) case — reproduces the official example Bundle-bun-nos-min: a FHIR
/// *document* Bundle spined by Composition-twngs, referencing DiagnosticReport (genetic analysis) +
/// Condition + Patient + Organization (hospital + genetic-testing lab) + Specimen + Device (sequencer)
/// + DocumentReference + Observation (the genomic panel: 20 gene-studied + 6 HGVS variants) + ServiceRequest.</summary>
public sealed class NgsAssembler : IgAssemblerBase
{
    public const string IgId = "tw.gov.mohw.nhi.ngs#1.0.0";
    public const string Case = "ngs";
    public override string Ig => IgId;
    public override string CaseType => Case;
    protected override string CanonicalBase => Sys.NgsBase;

    [ModuleInitializer]
    internal static void Register() => AssemblerFactory.Register(IgId, Case, () => new NgsAssembler());

    // P / R / Cc come from IgAssemblerBase.

    public override Bundle Assemble(PACase c)
    {
        var patient = new Patient
        {
            Id = "pat-min", Meta = P("Patient-twngs"),
            Identifier =
            {
                new Identifier { Use = Identifier.IdentifierUse.Official, Type = Cc(Sys.V2_0203, "NNxxx"), System = Sys.IdCard, Value = c.Patient.GetValueOrDefault("id_card", "A123456789") },
                new Identifier { Use = Identifier.IdentifierUse.Official, Type = Cc(Sys.V2_0203, "MR"), System = Sys.Tpech, Value = "123456" },
            },
            Name = { new HumanName { Use = HumanName.NameUse.Usual, Text = c.Patient.GetValueOrDefault("name", "王大明") } },
            Gender = AdministrativeGender.Male,
            BirthDate = c.Patient.GetValueOrDefault("birth_date", "2001-01-01"),
        };

        var orgHosp = new Organization
        {
            Id = "org-hosp-min", Meta = P("Organization-twngs"),
            Identifier = { new Identifier { Use = Identifier.IdentifierUse.Official, Type = Cc(Sys.V2_0203, "PRN"), System = Sys.NgsOrgId, Value = "1145010010" } },
            Type = { Cc(Sys.OrgType, "prov") },
            Name = "佛教慈濟醫療財團法人花蓮慈濟醫院",
        };

        var orgGene = new Organization
        {
            Id = "org-gene-min", Meta = P("Organization-gene-twngs"),
            Identifier = { new Identifier { System = Sys.DepMohw, Value = "2023LDTB0002" } },
        };

        var cond = new Condition
        {
            Id = "con-nos-min", Meta = P("Condition-twngs"),
            ClinicalStatus = Cc(Sys.ConditionClinical, "active"),
            Category = { Cc(Sys.CondCategory, "encounter-diagnosis") },
            Code = Cc(Sys.Icd10cmTw, "C34.90", "未明示側性支氣管或肺惡性腫瘤"),
            Subject = R("Patient/pat-min"),
            RecordedDate = "2024-07-10",
            Note = { new Annotation { Text = "病患診斷為非小細胞肺癌（NOS 類型），現於腦部發現轉移病灶，擬進行癌症相關基因檢測以評估是否具有標靶治療或免疫治療標的。申請進行多基因 panel 定序檢測，包含 TP53 等常見肺癌相關突變，以利後續治療規劃與藥物選擇。" } },
        };

        var specimen = new Specimen
        {
            Id = "spe-min", Meta = P("Specimen-twngs"),
            Identifier = { new Identifier { Value = "123-4567" } },
            Type = Cc(Sys.Loinc, "LP7057-5", "Bld"),
            Subject = R("Patient/pat-min"),
            ReceivedTime = "2024-05-06T09:00:00.000Z",
        };

        var device = new Device
        {
            Id = "dev-min", Meta = P("Device-twngs"),
            DeviceName = { new Device.DeviceNameComponent { Name = "illumina", Type = DeviceNameType.UserFriendlyName } },
            ModelNumber = "HiSeq 4000 System",
        };

        var doc = new DocumentReference
        {
            Id = "doc-nos-min", Meta = P("DocumentReference-twngs"),
            Status = DocumentReferenceStatus.Current,
            Subject = R("Patient/pat-min"),
            Content = { new DocumentReference.ContentComponent
                { Attachment = new Attachment { ContentType = "application/pdf", Url = "file://GenReport01.pdf", Title = "Lung non-small cell lung carcinoma (NOS) 基因檢測" } } },
        };

        var serReq = new ServiceRequest
        {
            Id = "serReq-min", Meta = P("ServiceRequest-twngs"),
            Status = RequestStatus.Active, Intent = RequestIntent.Plan,
            Code = Cc(Sys.MedicalServicePaymentTw, "30301B", "實體腫瘤次世代基因定序－BRCA1/2基因檢測"),
            Subject = R("Patient/pat-min"),
        };

        var obs = new Observation
        {
            Id = "obs-nos-min", Meta = P("Observation-twngs"),
            Identifier = { new Identifier { Value = "121992445968" } },
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.ObsCategory, "laboratory") },
            Code = Cc(Sys.Loinc, "69548-6"),
            Subject = R("Patient/pat-min"),
            Effective = new FhirDateTime("2024-07-17"),
            Performer = { R("Organization/org-gene-min") },
            Value = Cc(Sys.Loinc, "LA9633-4", "Present"),
            Interpretation = { Cc(Sys.V3InterpObs, "POS", "Positive") },
            Method = Cc(Sys.Loinc, "LA26398-0", "Sequencing"),
            Specimen = R("Specimen/spe-min"),
            Device = R("Device/dev-min"),
            DerivedFrom = { R("DocumentReference/doc-nos-min") },
        };
        obs.Component.Add(Comp(Cc(Sys.Loinc, "81247-9", "Master HL7 genetic variant reporting panel"),
            Cc(Sys.Loinc, "21739-8", "TP53 gene mutations found [Identifier] in Blood or Tissue by Molecular genetics method Nominal"), "LA6692-3"));
        foreach (var (hgnc, sym) in new[]
        {
            ("HGNC:11998", "TP53"), ("HGNC:23252", "KEAP1"), ("HGNC:6407", "KRAS"), ("HGNC:7392", "MTAP"),
            ("HGNC:1787", "CDKN2A"), ("HGNC:1788", "CDKN2B"), ("HGNC:882", "ATR"), ("HGNC:3763", "FLT1"),
            ("HGNC:7882", "NOTCH2"), ("HGNC:8803", "PDGFRA"), ("HGNC:9122", "PMS2"), ("HGNC:9346", "PRDM1"),
            ("HGNC:9801", "RAC1"), ("HGNC:9967", "RET"), ("HGNC:427", "ALK"), ("HGNC:1097", "BRAF"),
            ("HGNC:3236", "EGFR"), ("HGNC:3430", "ERBB2"), ("HGNC:7029", "MET"), ("HGNC:10261", "ROS1"),
        })
            obs.Component.Add(Comp(Cc(Sys.Loinc, "48018-6", "Gene studied [ID]"), Cc(Sys.GeneNames, hgnc, sym)));
        foreach (var hgvs in new[]
        {
            "NM_001184.3:c.4408G>A", "NM_001184.3:c.2503T>G", "NM_024408.3:c.5403G>T",
            "NM_006206.4:c.1319C>T", "NM_001198.3:c.1214C>G", "NM_020975.4:c.874G>A",
        })
            obs.Component.Add(Comp(Cc(Sys.Loinc, "48004-6", "DNA change (c.HGVS)"), Cc(Sys.Hgvs, hgvs), "LA6690-7"));

        var diaRep = new DiagnosticReport
        {
            Id = "dia-nos-min", Meta = P("DiagnosticReport-twngs"),
            Extension = { new Extension(Sys.NgsExtDiagReportCondition, R("Condition/con-nos-min")) },
            BasedOn = { R("ServiceRequest/serReq-min") },
            Status = DiagnosticReport.DiagnosticReportStatus.Final,
            Code = Cc(Sys.Loinc, "51969-4", "Genetic analysis report"),
            Subject = R("Patient/pat-min"),
            Effective = new FhirDateTime("2024-07-20"),
            Performer = { R("Organization/org-gene-min") },
            Result = { R("Observation/obs-nos-min") },
        };

        var comp = new Composition
        {
            Id = "com-nos-min", Meta = P("Composition-twngs"),
            Status = CompositionStatus.Final,
            Type = Cc(Sys.NgsApplyType, "1", "送核"),
            Category = { Cc(Sys.NgsCaseClassification, "04", "西醫慢性病") },
            Subject = R("Patient/pat-min"),
            Date = "2024-07-25",
            Author = { R("Organization/org-hosp-min") },
            Title = "Lung non-small cell lung carcinoma (NOS) 基因檢測報告之證明文件",
            Section =
            {
                new Composition.SectionComponent { Entry =
                {
                    R("DiagnosticReport/dia-nos-min"), R("Condition/con-nos-min"), R("Organization/org-gene-min"),
                    R("Specimen/spe-min"), R("Device/dev-min"), R("DocumentReference/doc-nos-min"),
                    R("Observation/obs-nos-min"), R("ServiceRequest/serReq-min"),
                } },
            },
        };

        var ordered = new List<Resource> { comp, diaRep, cond, patient, orgHosp, orgGene, specimen, device, doc, obs, serReq };
        return WrapBundle("bun-nos-min", "Bundle-twngs", Bundle.BundleType.Document, ordered,
            identifier: new Identifier { System = "https://www.nhi.gov.tw", Value = "789123" },
            timestamp: new DateTimeOffset(2024, 7, 25, 13, 50, 58, TimeSpan.FromHours(8)));
    }

    private static Observation.ComponentComponent Comp(CodeableConcept code, CodeableConcept value, string? interpLoinc = null)
    {
        var comp = new Observation.ComponentComponent { Code = code, Value = value };
        if (interpLoinc != null) comp.Interpretation.Add(new CodeableConcept { Coding = { new Coding(Sys.Loinc, interpLoinc) } });
        return comp;
    }
}
