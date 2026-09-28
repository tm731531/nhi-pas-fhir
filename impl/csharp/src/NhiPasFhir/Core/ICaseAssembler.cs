using Hl7.Fhir.Model;

namespace NhiPasFhir.Core;

/// <summary>The contract every case-type implementation fulfils. Callers depend only on this.</summary>
public interface ICaseAssembler
{
    string Ig { get; }
    string CaseType { get; }
    Bundle Assemble(PACase pacase);
}
