using NhiPasFhir.Core;

namespace NhiPasFhir;

/// <summary>Fabricated example inputs (no real patient data). Mirrors the Python reference case.</summary>
public static class Samples
{
    public static PACase CancerDrugCase() => new(
        Ig: "tw.gov.mohw.nhi.pas#1.2.6", CaseType: "cancer-drug",
        Patient: new Dictionary<string, string>
        { ["id_card"] = "A123456789", ["name"] = "王大明", ["gender"] = "male", ["birth_date"] = "1965-03-02" },
        Provider: new Dictionary<string, string>
        { ["doctor_id_card"] = "B234567890", ["doctor_name"] = "李醫師",
          ["hospital_code"] = "0101090517", ["hospital_name"] = "臺北市立聯合醫院" },
        Vitals: new Dictionary<string, double> { ["weight_kg"] = 68.0, ["height_cm"] = 172.0 },
        Created: "2026-09-28T09:00:00+08:00",
        Data: new Dictionary<string, object>
        {
            ["drug_code"] = "BC27730100", ["authored_on"] = "2024-01-01",
            ["diagnosis_icd"] = "C90.00", ["diagnosis_date"] = "2024-01-01",
            ["diagnosis_text"] = "Multiple myeloma, not having achieved remission",
            ["drug_qty_tbl"] = 52, ["program_text"] = "多發性骨髓瘤第一線治療",
            ["lab_loinc"] = "777-3", ["lab_value"] = 5.1, ["lab_unit"] = "mmol/l", ["lab_date"] = "2024-01-01",
        });

    /// <summary>免疫製劑 case. v1 reads patient/provider/vitals/created; clinical evidence is example-fixed
    /// inside the assembler (see ImmunologicAssembler). Fabricated data only.</summary>
    public static PACase ImmunologicCase() => new(
        Ig: "tw.gov.mohw.nhi.pas#1.2.6", CaseType: "immunologic-agent",
        Patient: new Dictionary<string, string>
        { ["id_card"] = "A123456789", ["name"] = "王大明", ["gender"] = "male", ["birth_date"] = "2001-01-01" },
        Provider: new Dictionary<string, string>
        { ["doctor_id_card"] = "F123456789", ["doctor_name"] = "李醫師",
          ["hospital_code"] = "0101090517", ["hospital_name"] = "臺北市立聯合醫院" },
        Vitals: new Dictionary<string, double> { ["weight_kg"] = 59.65, ["height_cm"] = 170.0 },
        Created: "2024-05-30",
        // Case payload — a caller sets these to describe their own case; omit any to fall back to the example.
        Data: new Dictionary<string, object>
        {
            ["diagnosis_icd"] = "I50.812", ["diagnosis_date"] = "2024-01-01",
            ["diagnosis_text"] = "Adenocarcinoma, descending colon, cT3N2M1a, cStage IVA, KRAS G12V, with multiple liver metastases, status post FOLFIRI",
            ["procedure_icd"] = "3E0Y704", ["procedure_date"] = "2024-01-01",
            ["drug_code"] = "BC27730100", ["drug_qty"] = 52, ["program_text"] = "ALK陽性的晚期非小細胞肺癌第一線治療",
            ["drug_code_2"] = "KC011162B5", ["drug_qty_2"] = 70, ["apply_reason"] = "C50P1",
        });

    /// <summary>申復 (appeal) — same as a 送核 case but subType=3. Any case type accepts subtype_code/priority_code.</summary>
    public static PACase AppealCase()
    {
        var c = CancerDrugCase();
        return c with { Data = new Dictionary<string, object>(c.Data)
        {
            ["subtype_code"] = "3",                                          // 3 = 申復
            ["filing_ref"] = "FHR3501200000_2016101000000001.JSON",          // 送核檔名
            ["old_acpt_no"] = "202405301000002",                            // 原送核受理編號 (invariant applType)
        } };
    }

    /// <summary>自主審查 (self-assessment) — 送核 but priority=3.</summary>
    public static PACase SelfAssessmentCase()
    {
        var c = CancerDrugCase();
        return c with { Data = new Dictionary<string, object>(c.Data) { ["priority_code"] = "3" } };  // 3 = 自主審查
    }

    /// <summary>重大傷病 (nhi.ci) — reproduces the official example Bundle-bun-min. Fabricated data.
    /// A different IG (Task-based); patient/doctor drive a few fields, the rest is example-fixed.</summary>
    public static PACase CatastrophicIllnessCase() => new(
        Ig: "tw.gov.mohw.nhi.ci#1.0.2", CaseType: "catastrophic-illness",
        Patient: new Dictionary<string, string>
        { ["id_card"] = "A123456789", ["name"] = "王大明", ["gender"] = "male", ["birth_date"] = "2001-01-01" },
        Provider: new Dictionary<string, string>
        { ["doctor_id_card"] = "A234649456", ["doctor_name"] = "王小明" },
        Vitals: new Dictionary<string, double>(),
        Created: "2024-01-01",
        Data: new Dictionary<string, object>());

    /// <summary>電子處方箋 (nhi.empd) — reproduces the official example Bundle-bun-ep. Fabricated data.</summary>
    public static PACase EPrescriptionCase() => new(
        Ig: "tw.gov.mohw.nhi.empd#0.1.0", CaseType: "e-prescription",
        Patient: new Dictionary<string, string>
        { ["id_card"] = "Z199999829", ["name"] = "甄○康", ["gender"] = "female", ["birth_date"] = "1985-01-02" },
        Provider: new Dictionary<string, string>(),
        Vitals: new Dictionary<string, double>(),
        Created: "2024-02-19",
        Data: new Dictionary<string, object>());

    /// <summary>NHI decision (核定回應) — fabricated. Claim-level 同意 (approve-comment "1"), with two
    /// medical orders as item.detail[] (exercises the item(1)+detail(N) cardinality of ClaimResponse-twpas).</summary>
    public static ResponseCase ResponseCase() => new(
        ResponseId: "202505301000002", PatientRef: "Patient/pat-1", HospitalRef: "Organization/org-hosp",
        ClaimRef: "Claim/cla-1", Created: "2026-09-28", Disposition: "審畢結果", OverallApproveCode: "1",
        Items: new[]
        {
            new ResponseItem(ItemSequence: 1, ApproveCode: "1", ApprovedValue: 2),
            new ResponseItem(ItemSequence: 2, ApproveCode: "1", ApprovedValue: 2),
        });

    /// <summary>錯誤回報範例(fabricated).</summary>
    public static OutcomeIssue[] ErrorOutcome() => new[] { new OutcomeIssue("error", "processing", "MSG_PARAM_INVALID") };
}
