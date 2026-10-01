using Hl7.Fhir.Model;

namespace NhiPasFhir.MediaDeclaration;

/// <summary>Converts an NHI medical-expense media/XML declaration (媒體申報) to/from FHIR R4.
/// Forward: a t/d/p record -> a FHIR *collection* Bundle (Patient + Encounter + Condition(s) + Claim
/// use=claim + MedicationRequest/Procedure per 醫令). Reverse: a Claim graph -> a media record, with
/// the t-segment totals recomputed from the d/p entries (never trusted inbound).
///
/// Faithful-or-TODO: only fields verified against the spec (版更 112.08.25) are mapped; code tables not
/// yet transcribed (案件分類/科別/給付類別…) pass through as raw codes with a TODO, never invented.
/// The FHIR target is base R4 Claim on TW Core — there is NO published 健保費用申報 FHIR IG, so this is
/// structurally valid FHIR, not a conformance profile. See spec/docs/media-declaration-to-fhir.md.</summary>
public static class MediaDeclarationConverter
{
    // TODO: confirm official identifier system URLs for NHI codes; these are placeholders.
    private const string NhiId = "https://nhicore.nhi.gov.tw";
    private const string Icd10Cm = "http://hl7.org/fhir/sid/icd-10-cm";
    private const string ClaimTypeCs = "http://terminology.hl7.org/CodeSystem/claim-type";
    private const string ProcessPriorityCs = "http://terminology.hl7.org/CodeSystem/processpriority";
    private const string DiagnosisTypeCs = "http://terminology.hl7.org/CodeSystem/ex-diagnosistype";
    private const string V3ActCode = "http://terminology.hl7.org/CodeSystem/v3-ActCode";

    private static ResourceReference Rf(Resource r) => new($"{r.TypeName}/{r.Id}");
    private static Bundle.EntryComponent E(Resource r) => new() { FullUrl = $"{r.TypeName}/{r.Id}", Resource = r };

    // ---------------------------------------------------------------- forward: media -> FHIR
    public static Bundle ToFhir(MediaRecord rec)
    {
        var bundle = new Bundle { Type = Bundle.BundleType.Collection };

        var org = new Organization
        {
            Id = "org-filer",
            Identifier = { new Identifier($"{NhiId}/hospital-id", rec.T("t2") ?? "") },   // t2 服務機構代號
        };
        var patient = new Patient
        {
            Id = "pat-1",
            Identifier = { new Identifier($"{NhiId}/national-id", rec.D("d3") ?? "") },     // d3 身分證統一編號
            BirthDate = RocDate.ToIso(rec.D("d11")),                                        // d11 出生年月日 (ROC)
        };
        var doctor = new Practitioner { Id = "pra-1" };
        if (!string.IsNullOrEmpty(rec.D("d30")))                                             // d30 診治醫事人員代號
            doctor.Identifier.Add(new Identifier($"{NhiId}/practitioner-id", rec.D("d30"))); // omit if absent — never emit an empty-valued identifier (invalid FHIR; never fabricate)
        // 健保 is the payer — base R4 Claim requires insurance[1..*] + a Coverage. (d14 給付類別 -> Coverage.type: TODO.)
        var nhi = new Organization
        {
            Id = "org-nhi",
            Identifier = { new Identifier($"{NhiId}/payer", "NHI") },
        };
        var coverage = new Coverage
        {
            Id = "cov-1",
            Status = FinancialResourceStatusCodes.Active,
            Beneficiary = Rf(patient),
            Payor = { Rf(nhi) },
        };
        var enc = new Encounter
        {
            Id = "enc-1",
            Status = Encounter.EncounterStatus.Finished,
            Class = new Coding(V3ActCode, "AMB"),
            Subject = Rf(patient),
            Period = new Period { Start = RocDate.ToIso(rec.D("d9")), End = RocDate.ToIso(rec.D("d10")) }, // d9/d10
        };
        if (!string.IsNullOrEmpty(rec.D("d29")))                                            // d29 就醫序號 (IC 卡)
            enc.Identifier.Add(new Identifier($"{NhiId}/ic-card-seq", rec.D("d29")));

        var bundleEntries = new List<Resource> { org, patient, doctor, nhi, coverage, enc };

        // d19 主診斷 + d20-d23 次診斷 -> Condition[] + Claim.diagnosis[]
        var diagnoses = new List<Claim.DiagnosisComponent>();
        var dxFields = new[] { "d19", "d20", "d21", "d22", "d23" };
        for (var i = 0; i < dxFields.Length; i++)
        {
            var icd = rec.D(dxFields[i]);
            if (string.IsNullOrEmpty(icd)) continue;
            var cond = new Condition
            {
                Id = $"cond-{i}",
                Code = new CodeableConcept(Icd10Cm, icd),                                   // ICD-10-CM (小數點免填)
                Subject = Rf(patient),
            };
            bundleEntries.Add(cond);
            diagnoses.Add(new Claim.DiagnosisComponent
            {
                Sequence = i + 1,
                Diagnosis = Rf(cond),
                Type = { new CodeableConcept(DiagnosisTypeCs, dxFields[i] == "d19" ? "principal" : "secondary") },
            });
        }

        // 醫令清單段 p -> Claim.item[] (+ MedicationRequest for 用藥, Procedure for 診療)
        var items = new List<Claim.ItemComponent>();
        foreach (var p in rec.Orders)
        {
            var seq = int.TryParse(Get(p, "p13"), out var s) ? s : items.Count + 1;         // p13 醫令序
            var item = new Claim.ItemComponent
            {
                Sequence = seq,
                ProductOrService = new CodeableConcept($"{NhiId}/nhi-code", Get(p, "p4")),   // p4 藥品(項目)代號
            };
            if (decimal.TryParse(Get(p, "p10"), out var qty)) item.Quantity = new Quantity { Value = qty }; // p10 總量
            if (decimal.TryParse(Get(p, "p11"), out var up)) item.UnitPrice = new Money { Value = up };     // p11 單價
            if (decimal.TryParse(Get(p, "p12"), out var net)) item.Net = new Money { Value = net };         // p12 點數
            items.Add(item);

            switch (Get(p, "p3"))                                                            // p3 醫令類別
            {
                case "1":                                                                    // 用藥明細 -> MedicationRequest
                    var mr = new MedicationRequest
                    {
                        Id = $"med-{seq}",
                        Status = MedicationRequest.MedicationrequestStatus.Completed,
                        Intent = MedicationRequest.MedicationRequestIntent.Order,
                        Medication = new CodeableConcept($"{NhiId}/nhi-drug-code", Get(p, "p4")),
                        Subject = Rf(patient),
                    };
                    // p5 用量 / p7 頻率 / p9 途徑 = 健保藥品使用標準碼 (TODO: map to structured Dosage codings)
                    if (!string.IsNullOrEmpty(Get(p, "p7")))
                        mr.DosageInstruction.Add(new Dosage { Text = $"freq={Get(p, "p7")} route={Get(p, "p9")} dose={Get(p, "p5")}" });
                    bundleEntries.Add(mr);
                    break;
                case "2":                                                                    // 診療明細 -> Procedure
                    bundleEntries.Add(new Procedure
                    {
                        Id = $"proc-{seq}",
                        Status = EventStatus.Completed,
                        Code = new CodeableConcept($"{NhiId}/nhi-code", Get(p, "p4")),
                        Subject = Rf(patient),
                    });
                    break;
                    // p3 = 0/3/4/9 -> Claim.item only (TODO: 特材->Device, 診察費/藥事服務費 lines)
            }
        }

        var claim = new Claim
        {
            Id = "cla-1",
            Status = FinancialResourceStatusCodes.Active,
            Use = ClaimUseCode.Claim,
            Type = new CodeableConcept(ClaimTypeCs, "institutional"),
            Patient = Rf(patient),
            Provider = Rf(org),
            Priority = new CodeableConcept(ProcessPriorityCs, "normal"),
            Created = RocDate.ToIso(rec.T("t6")) ?? RocDate.ToIso(rec.D("d9")),              // t6 申報日期
            CareTeam = { new Claim.CareTeamComponent { Sequence = 1, Provider = Rf(doctor) } },
            Insurance = { new Claim.InsuranceComponent { Sequence = 1, Focal = true, Coverage = Rf(coverage) } },
        };
        if (!string.IsNullOrEmpty(rec.D("d1")))                                               // d1 案件分類 (TODO code table)
            claim.SubType = new CodeableConcept($"{NhiId}/case-category", rec.D("d1"));        // omit if absent — never an empty-valued coding
        if (!string.IsNullOrEmpty(rec.D("d2")))                                               // d2 流水編號
            claim.Identifier.Add(new Identifier($"{NhiId}/claim-seq", rec.D("d2")));          // omit if absent — never an empty-valued identifier
        claim.Diagnosis.AddRange(diagnoses);
        claim.Item.AddRange(items);
        bundleEntries.Add(claim);

        foreach (var r in bundleEntries) bundle.Entry.Add(E(r));
        return bundle;
    }

    // ---------------------------------------------------------------- reverse: FHIR -> media
    /// <summary>Minimal inverse: reconstruct the verified d/p fields from a Claim graph and RECOMPUTE
    /// the t-segment totals (件數 = 1 case, t38 點數 = Σ p12). Code-table fields are left as stored.</summary>
    public static MediaRecord ToMediaRecord(Bundle bundle)
    {
        var rec = new MediaRecord();
        var byRef = bundle.Entry.Where(e => e.Resource != null)
            .ToDictionary(e => $"{e.Resource!.TypeName}/{e.Resource!.Id}", e => e.Resource!);

        var claim = bundle.Entry.Select(e => e.Resource).OfType<Claim>().FirstOrDefault()
                    ?? throw new InvalidOperationException("bundle has no Claim to reverse");
        var patient = Resolve<Patient>(byRef, claim.Patient);
        var org = Resolve<Organization>(byRef, claim.Provider);
        var doctor = claim.CareTeam.Count > 0 ? Resolve<Practitioner>(byRef, claim.CareTeam[0].Provider) : null;

        rec.Summary["t2"] = org?.Identifier.FirstOrDefault()?.Value ?? "";
        rec.Summary["t6"] = RocDate.ToRoc(claim.Created) ?? "";
        rec.Case["d1"] = claim.SubType?.Coding.FirstOrDefault()?.Code ?? "";
        rec.Case["d2"] = claim.Identifier.FirstOrDefault()?.Value ?? "";
        rec.Case["d3"] = patient?.Identifier.FirstOrDefault()?.Value ?? "";
        if (patient?.BirthDate != null) rec.Case["d11"] = RocDate.ToRoc(patient.BirthDate) ?? "";
        if (doctor != null) rec.Case["d30"] = doctor.Identifier.FirstOrDefault()?.Value ?? "";

        var enc = bundle.Entry.Select(e => e.Resource).OfType<Encounter>().FirstOrDefault();
        if (enc?.Period?.Start != null) rec.Case["d9"] = RocDate.ToRoc(enc.Period.Start) ?? "";
        if (enc?.Period?.End != null) rec.Case["d10"] = RocDate.ToRoc(enc.Period.End) ?? "";

        // diagnoses -> d19/d20... in sequence order
        var dxFields = new[] { "d19", "d20", "d21", "d22", "d23" };
        foreach (var dx in claim.Diagnosis.OrderBy(d => d.Sequence))
        {
            var idx = (dx.Sequence ?? 1) - 1;
            if (idx < 0 || idx >= dxFields.Length) continue;
            var cond = dx.Diagnosis is ResourceReference rr ? Resolve<Condition>(byRef, rr) : null;
            rec.Case[dxFields[idx]] = cond?.Code?.Coding.FirstOrDefault()?.Code
                ?? (dx.Diagnosis as CodeableConcept)?.Coding.FirstOrDefault()?.Code ?? "";
        }

        decimal totalPoints = 0;
        foreach (var it in claim.Item.OrderBy(i => i.Sequence))
        {
            var p = new Dictionary<string, string>
            {
                ["p13"] = (it.Sequence ?? 0).ToString(),
                ["p4"] = it.ProductOrService?.Coding.FirstOrDefault()?.Code ?? "",
            };
            if (it.Quantity?.Value is { } q) p["p10"] = q.ToString();
            if (it.UnitPrice?.Value is { } u) p["p11"] = u.ToString();
            if (it.Net?.Value is { } n) { p["p12"] = n.ToString(); totalPoints += n; }
            rec.Orders.Add(p);
        }

        // t-segment totals are DERIVED — recompute, never trust inbound. One outpatient case here.
        rec.Summary["t37"] = "1";                    // 申請件數總計
        rec.Summary["t38"] = totalPoints.ToString(); // 申請點數總計 = Σ p12
        return rec;
    }

    private static string Get(Dictionary<string, string> d, string k) => d.TryGetValue(k, out var v) ? v : "";

    private static T? Resolve<T>(Dictionary<string, Resource> byRef, ResourceReference? r) where T : Resource
        => r?.Reference != null && byRef.TryGetValue(r.Reference, out var res) ? res as T : null;
}
