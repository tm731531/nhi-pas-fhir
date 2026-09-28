using System.Runtime.CompilerServices;
using Hl7.Fhir.Model;
using NhiPasFhir.Core;

namespace NhiPasFhir.Plugins;

/// <summary>癌藥事前審查 case type. Values from the official IG examples; validated to 0 errors.</summary>
public sealed class CancerDrugAssembler : AbstractCaseAssembler
{
    public const string IgId = "tw.gov.mohw.nhi.pas#1.2.6";
    public const string Case = "cancer-drug";
    public override string Ig => IgId;
    public override string CaseType => Case;

    [ModuleInitializer]
    internal static void Register() => AssemblerFactory.Register(IgId, Case, () => new CancerDrugAssembler());

    private static string S(PACase c, string k) => (string)c.Data[k];
    private static double D(PACase c, string k) => Convert.ToDouble(c.Data[k]);
    private static int I(PACase c, string k) => Convert.ToInt32(c.Data[k]);

    protected override CaseParts BuildCase(PACase c, Patient patient, Practitioner doctor, Organization hospital)
    {
        var med = new MedicationRequest
        {
            Id = "medReq-apply", Meta = Profile("MedicationRequest-apply-twpas"),
            Status = MedicationRequest.MedicationrequestStatus.OnHold,
            Intent = MedicationRequest.MedicationRequestIntent.Plan,
            Medication = new CodeableConcept(Sys.CsMedication, S(c, "drug_code")),
            Subject = Ref(patient),
            AuthoredOn = c.Data.TryGetValue("authored_on", out var a) ? (string)a : null,
            DosageInstruction = { Dosage() },
        };

        var item = new Claim.ItemComponent
        {
            Sequence = 1,
            ProductOrService = new CodeableConcept(Sys.CsOrderType, "1", "藥品"),
            Modifier =
            {
                new CodeableConcept(Sys.CsContinuation, "1", "初次使用"),
                new CodeableConcept(Sys.CsLineOfTherapy, "1", "第一線治療"),
            },
            Quantity = new Quantity { Value = I(c, "drug_qty_tbl"), System = Sys.Ucum, Code = "{tbl}" },
        };
        if (c.Data.TryGetValue("program_text", out var pt))
            item.ProgramCode.Add(new CodeableConcept { Text = (string)pt });
        item.Extension.Add(new Extension(Sys.ExtRequestedService, Ref(med)));

        var dx = new Claim.DiagnosisComponent
        {
            Sequence = 1,
            Diagnosis = new CodeableConcept(Sys.Icd10cmTw, S(c, "diagnosis_icd")),
        };
        dx.Type.Add(new CodeableConcept { Text = S(c, "diagnosis_text") });
        dx.Extension.Add(new Extension(Sys.ExtDxRecordedDate, new Date(S(c, "diagnosis_date"))));

        // C90/priority invariant → one 'tests' lab report.
        var lab = new Observation
        {
            Id = "obs-lab", Meta = Profile("Observation-laboratory-result-twpas"),
            Status = ObservationStatus.Final,
            Category = { new CodeableConcept(Sys.CsSupportingInfo, "tests") },
            Code = new CodeableConcept(Sys.Loinc, S(c, "lab_loinc")),
            Subject = Ref(patient),
            Effective = new FhirDateTime(S(c, "lab_date")),
            Performer = { Ref(doctor) },
            Value = new Quantity { Value = (decimal)D(c, "lab_value"), Unit = S(c, "lab_unit") },
        };

        return new CaseParts(
            new List<Resource> { med },
            new List<Claim.ItemComponent> { item },
            new List<Claim.DiagnosisComponent> { dx },
            new List<(string, Resource)> { ("tests", lab) });
    }

    private static Dosage Dosage() => new()
    {
        Timing = new Timing
        {
            Repeat = new Timing.RepeatComponent
            {
                Bounds = new Period { Start = "2024-01-01", End = "2024-02-11" },
                Count = 42,
            },
            Code = new CodeableConcept { Coding = { new Coding(Sys.GtsAbbrev, "QD"), new Coding(Sys.MedFreqNhi, "AC1H") } },
        },
        Route = new CodeableConcept(Sys.Snomed, "26643006"),
        DoseAndRate = { new Dosage.DoseAndRateComponent { Dose = new Quantity { Value = 75, System = Sys.Ucum, Code = "mg/m2" } } },
    };
}
