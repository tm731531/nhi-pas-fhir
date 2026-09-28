// Minimal usage example: build a cancer-drug PA bundle and write it as FHIR JSON.
using NhiPasFhir;

var bundle = NhiPas.Build(Samples.CancerDrugCase());   // PACase -> Factory -> Assembler -> Bundle
var json = NhiPas.ToJson(bundle);
var outPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "build", "pa-bundle.cs.json");
outPath = Path.GetFullPath(outPath);
Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
File.WriteAllText(outPath, json);
Console.WriteLine($"wrote {outPath} — {bundle.Entry.Count} entries");
