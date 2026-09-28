// Minimal usage example: build the PA bundles and write them as FHIR JSON.
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
