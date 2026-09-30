// media-tool — convert NHI medical-expense media declarations (媒體申報) <-> FHIR, both directions.
//   to-fhir  <record.txt>   : t/d/p segment record  -> FHIR collection Bundle (JSON on stdout)
//   to-media <bundle.json>  : FHIR Claim graph       -> t/d/p segment record (on stdout)
// See spec/docs/media-declaration-to-fhir.md. C# is the primary implementation; tools/*.py is the mirror.
using Hl7.Fhir.Model;
using NhiPasFhir.Core;
using NhiPasFhir.MediaDeclaration;

if (args.Length < 2)
{
    Console.Error.WriteLine("usage: media-tool <to-fhir|to-media> <path>");
    return 1;
}

var (cmd, path) = (args[0], args[1]);
switch (cmd)
{
    case "to-fhir":
        var rec = MediaDeclarationParser.Parse(File.ReadAllText(path));
        var bundle = MediaDeclarationConverter.ToFhir(rec);
        Console.WriteLine(NhiPasFhir.NhiPas.ToJson(bundle));
        return 0;

    case "to-media":
        var json = File.ReadAllText(path);
        var parsed = FhirJson.Parse<Bundle>(json);
        var media = MediaDeclarationConverter.ToMediaRecord(parsed);
        Console.Write(MediaDeclarationParser.Write(media));
        return 0;

    default:
        Console.Error.WriteLine($"unknown command '{cmd}' (expected to-fhir | to-media)");
        return 1;
}
