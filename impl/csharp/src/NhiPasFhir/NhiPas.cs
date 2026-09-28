using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NhiPasFhir.Core;

namespace NhiPasFhir;

/// <summary>Facade — the simplest way to use the library.</summary>
public static class NhiPas
{
    public static Bundle Build(PACase pacase) => AssemblerFactory.ForCase(pacase).Assemble(pacase);

    public static string ToJson(Resource r) => r.ToJson();
}
