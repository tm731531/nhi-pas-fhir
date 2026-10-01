using Hl7.Fhir.Model;

namespace NhiPasFhir.Core;

/// <summary>The one home for building the FHIR datatypes every assembler needs, regardless of IG —
/// because every assembler is producing FHIR. Pure + stateless; consumed by composition (typically
/// <c>using static NhiPasFhir.Core.Fhir;</c>) so it couples to nothing and is tested once, in isolation
/// (see FhirTests). This replaces the per-file / per-base duplicate CodeableConcept + reference helpers.</summary>
public static class FhirBuild
{
    /// <summary>CodeableConcept with a single coding (+ optional display) and optional text.</summary>
    public static CodeableConcept Cc(string system, string code, string? display = null, string? text = null)
        => new() { Coding = { new Coding(system, code) { Display = display } }, Text = text };

    /// <summary>A single Coding (+ optional display) — e.g. a QuestionnaireResponse answer value.</summary>
    public static Coding Cd(string system, string code, string? display = null)
        => new(system, code) { Display = display };

    /// <summary>A relative reference ("Patient/pat-1").</summary>
    public static ResourceReference R(string typeSlashId) => new(typeSlashId);

    /// <summary>An absolute reference ("{canonicalBase}/Patient/pat-1") — some IGs (e.g. EMR) use these.</summary>
    public static ResourceReference AbsRef(string canonicalBase, string typeSlashId)
        => new($"{canonicalBase}/{typeSlashId}");
}
