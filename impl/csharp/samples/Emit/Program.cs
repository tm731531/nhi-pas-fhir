// Minimal usage example: build the PA bundles and write them as FHIR JSON.
using Hl7.Fhir.Model;
using NhiPasFhir;
using NhiPasFhir.Core;

void Emit(string file, PACase c)
{
    var bundle = NhiPas.Build(c);                       // PACase -> Factory -> Assembler -> Bundle
    var outPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "build", file));
    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
    File.WriteAllText(outPath, NhiPas.ToJson(bundle));
    Console.WriteLine($"wrote {outPath} — {bundle.Entry.Count} entries");
}

Emit("pa-bundle.cs.json", Samples.CancerDrugCase());
Emit("pa-bundle-immunologic.cs.json", Samples.ImmunologicCase());
Emit("pa-bundle-appeal.cs.json", Samples.AppealCase());                 // 申復
Emit("pa-bundle-self.cs.json", Samples.SelfAssessmentCase());           // 自主審查

// 維度2 — NHI decision (核定回應) + error outcome
void EmitResource(string file, Resource r)
{
    var outPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "build", file));
    File.WriteAllText(outPath, NhiPas.ToJson(r));
    Console.WriteLine($"wrote {outPath}");
}
EmitResource("pa-bundle-response.cs.json", NhiPas.BuildResponse(Samples.ResponseCase()));
EmitResource("pa-outcome.cs.json", NhiPas.BuildOutcome(Samples.ErrorOutcome()));

// value-variants — prove per-profile sync with the official examples
foreach (var (file, resource) in Variants.All())
    EmitResource($"variant-{file}.cs.json", resource);
