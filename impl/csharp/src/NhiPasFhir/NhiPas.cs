using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NhiPasFhir.Core;

namespace NhiPasFhir;

/// <summary>Facade — the simplest way to use the library.</summary>
public static class NhiPas
{
    public static Bundle Build(PACase pacase) => AssemblerFactory.ForCase(pacase).Assemble(pacase);

    /// <summary>Build the NHI decision Bundle (核定回應) for a submitted claim. 維度 2.</summary>
    public static Bundle BuildResponse(ResponseCase response) => ResponseBuilder.Build(response);

    /// <summary>Build an OperationOutcome-twpas (錯誤回報) for a submission that cannot be processed.</summary>
    public static OperationOutcome BuildOutcome(params OutcomeIssue[] issues) => OutcomeBuilder.Build(issues);

    /// <summary>Rewrite a bundle to urn:uuid entry-referencing style (Bundle-bun-uuid-example).</summary>
    public static Bundle ToUuidStyle(Bundle bundle) => UuidStyle.ToUuidStyle(bundle);

    public static string ToJson(Resource r) => r.ToJson();
}
