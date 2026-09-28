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

    public static string ToJson(Resource r) => r.ToJson();
}
