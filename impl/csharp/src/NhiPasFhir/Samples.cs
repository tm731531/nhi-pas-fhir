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
}
