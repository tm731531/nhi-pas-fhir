namespace NhiPasFhir.Core;

/// <summary>Neutral, non-FHIR description of one prior-authorization case (callers never touch FHIR).</summary>
public sealed record PACase(
    string Ig,
    string CaseType,
    IReadOnlyDictionary<string, string> Patient,   // id_card, name, gender, birth_date
    IReadOnlyDictionary<string, string> Provider,  // doctor_id_card, doctor_name, hospital_code, hospital_name
    IReadOnlyDictionary<string, double> Vitals,    // weight_kg, height_cm
    string Created,
    IReadOnlyDictionary<string, object> Data);     // case-specific (diagnosis, drug, labs…)
