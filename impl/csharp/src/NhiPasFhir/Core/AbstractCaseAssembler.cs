using Hl7.Fhir.Model;

namespace NhiPasFhir.Core;

/// <summary>Shared assembly behaviour (TW Core clinical layer + Bundle). Case types override BuildCase.</summary>
public abstract class AbstractCaseAssembler : ICaseAssembler
{
    public abstract string Ig { get; }
    public abstract string CaseType { get; }

    protected static Meta Profile(string name) => new() { Profile = new[] { $"{Sys.Sd}/{name}" } };
    protected static ResourceReference Ref(Resource r) => new($"{r.TypeName}/{r.Id}");
    protected static string FullUrl(Resource r) => $"{Sys.PasBase}/{r.TypeName}/{r.Id}";

    protected record CaseParts(
        List<Resource> Extras,
        List<Claim.ItemComponent> Items,
        List<Claim.DiagnosisComponent> Diagnoses,
        List<(string Category, Resource Report)> Reports);

    protected abstract CaseParts BuildCase(PACase c, Patient patient, Practitioner doctor, Organization hospital);

    public Bundle Assemble(PACase c)
    {
        var patient = new Patient
        {
            Id = "pat-1", Meta = Profile("Patient-twpas"),
            Identifier = { new Identifier
            {
                Use = Identifier.IdentifierUse.Official,
                Type = new CodeableConcept(Sys.V2_0203, "NNxxx"),
                System = Sys.IdCard, Value = c.Patient["id_card"],
            } },
            Name = { new HumanName { Use = HumanName.NameUse.Usual, Text = c.Patient["name"] } },
            Gender = Enum.Parse<AdministrativeGender>(c.Patient["gender"], true),
            BirthDate = c.Patient["birth_date"],
        };
        var doctor = new Practitioner
        {
            Id = "pra-1", Meta = Profile("Practitioner-twpas"),
            Identifier = { new Identifier
            {
                Use = Identifier.IdentifierUse.Official,
                Type = new CodeableConcept(Sys.V2_0203, "NNxxx"),
                System = Sys.IdCard, Value = c.Provider["doctor_id_card"],
            } },
            Name = { new HumanName { Text = c.Provider["doctor_name"] } },
        };
        var hospital = new Organization
        {
            Id = "org-hosp", Meta = Profile("Organization-twpas"),
            Identifier = { new Identifier
            {
                Use = Identifier.IdentifierUse.Official,
                Type = new CodeableConcept(Sys.V2_0203, "PRN"),
                System = Sys.OrgId, Value = c.Provider["hospital_code"],
            } },
            Type = { new CodeableConcept(Sys.OrgType, "prov") },
            Name = c.Provider.GetValueOrDefault("hospital_name"),
        };
        var nhi = new Organization
        {
            Id = "org-nhi",
            Meta = new Meta { Profile = new[] { $"{Sys.TwcoreSd}/Organization-govt-twcore" } },
            Identifier = { new Identifier
            {
                Use = Identifier.IdentifierUse.Official,
                Type = new CodeableConcept(Sys.TwcoreV2_0203, "GOI"),
                System = Sys.OidNat, Value = "A21030000I",
            } },
            Type = { new CodeableConcept(Sys.OrgType, "govt") },
            Name = "衛生福利部中央健康保險署",
        };
        var enc = new Encounter
        {
            Id = "enc-1", Meta = Profile("Encounter-twpas"),
            Status = Encounter.EncounterStatus.Planned,
            Class = new Coding(Sys.V3ActCode, "AMB"),
            ServiceType = new CodeableConcept(Sys.ServiceDept, "AJ"),
            Subject = Ref(patient),
        };
        var cov = new Coverage
        {
            Id = "cov-1", Meta = Profile("Coverage-twpas"),
            Status = FinancialResourceStatusCodes.Active,
            Beneficiary = Ref(patient), Payor = { Ref(nhi) },
        };

        var parts = BuildCase(c, patient, doctor, hospital);

        var supportingInfo = new List<Claim.SupportingInformationComponent>
        {
            new() { Sequence = 1, Category = new CodeableConcept(Sys.CsSupportingInfo, "weight"),
                    Value = new Quantity((decimal)c.Vitals["weight_kg"], "kg", Sys.Ucum) },
            new() { Sequence = 2, Category = new CodeableConcept(Sys.CsSupportingInfo, "height"),
                    Value = new Quantity((decimal)c.Vitals["height_cm"], "cm", Sys.Ucum) },
        };
        int seq = 3;
        foreach (var (cat, report) in parts.Reports)
            supportingInfo.Add(new Claim.SupportingInformationComponent
            {
                Sequence = seq++, Category = new CodeableConcept(Sys.CsSupportingInfo, cat), Value = Ref(report),
            });

        var claim = new Claim
        {
            Id = "cla-1", Meta = Profile("Claim-twpas"),
            Status = FinancialResourceStatusCodes.Active,
            Type = new CodeableConcept(Sys.ClaimType, "institutional"),
            Use = ClaimUseCode.Preauthorization,
            SubType = new CodeableConcept(Sys.CsApplyType, "1", "送核"),
            Priority = new CodeableConcept(Sys.CsTmhbType, "1", "一般事前審查申請"),
            Patient = Ref(patient), Created = c.Created, Enterer = Ref(doctor), Provider = Ref(hospital),
            Insurance = { new Claim.InsuranceComponent { Sequence = 1, Focal = true, Coverage = Ref(cov) } },
            Item = parts.Items, Diagnosis = parts.Diagnoses, SupportingInfo = supportingInfo,
        };
        claim.Extension.Add(new Extension(Sys.ExtClaimEncounter, Ref(enc)));

        var ordered = new List<Resource> { claim, enc, patient, doctor, hospital };
        ordered.AddRange(parts.Extras);
        ordered.Add(cov);
        ordered.Add(nhi);
        ordered.AddRange(parts.Reports.Select(r => r.Report));

        var bundle = new Bundle { Id = "bun-demo", Meta = Profile("Bundle-twpas"), Type = Bundle.BundleType.Collection };
        foreach (var r in ordered)
            bundle.Entry.Add(new Bundle.EntryComponent { FullUrl = FullUrl(r), Resource = r });
        return bundle;
    }
}
