using Hl7.Fhir.Model;
using NhiPasFhir.MediaDeclaration;

namespace NhiPasFhir.Ingest.NhiUpload;

/// <summary>The ingest thread for 健保署 每日上傳: Big5 XML -> NhiUploadRecord -> MediaRecord ->
/// MediaDeclarationConverter -> FHIR Bundle. One Bundle per REC (this returns the first REC's Bundle,
/// matching the converter's one-case shape). No 媒體申報/上傳 FHIR IG exists, so the Bundle is base-R4
/// FHIR (+ TW Core where the converter applies it), not a profile-conformant resource.</summary>
public static class NhiUploadPipeline
{
    public static Bundle ToFhir(byte[] big5Bytes)
    {
        var recs = NhiUploadXmlParser.Parse(big5Bytes);
        if (recs.Count == 0) throw new InvalidOperationException("每日上傳 file contained no REC records");
        return ToFhir(recs[0]);
    }

    public static Bundle ToFhir(NhiUploadRecord rec)
        => MediaDeclarationConverter.ToFhir(NhiUploadToMediaRecord.Map(rec));
}
