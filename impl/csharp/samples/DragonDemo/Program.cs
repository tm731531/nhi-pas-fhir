using System.Text;
using Hl7.Fhir.Model;
using NhiPasFhir;
using NhiPasFhir.Core;
using NhiPasFhir.CardReader;
using NhiPasFhir.Ingest.NhiUpload;

// ───────────────────────────────────────────────────────────────────────────────────────────────
// 一條龍 DEMO — 讀卡 → 進料 → FHIR → 送件 (dry-run)
//
// Composition root: wires the three independent adapters (CardReader / Ingest / Core) end-to-end.
// Honest gates: the live card read needs a PC/SC reader (falls back to a fabricated card); the final
// 送件 is DRY-RUN only — real submission needs HCA 醫事憑證 + 健保 VPN (work-order #13), not done here.
// All data is fabricated. No PHI.
// ───────────────────────────────────────────────────────────────────────────────────────────────

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

Console.WriteLine("🐉 一條龍 DEMO — 讀卡 → 進料 → FHIR → 送件 (dry-run)");
Console.WriteLine(new string('─', 72));

// ── ① 讀卡 (CardReader) ─────────────────────────────────────────────────────────────────────────
// Try a live PC/SC read; if there is no reader/card/PC-SC subsystem, fall back to a fabricated card so
// the demo always runs. The basic segment needs NO 醫事人員卡.
NhiCardBasic card;
try
{
    var reader = new NhiCardReader();
    if (reader.ListReaders().Count == 0) throw new InvalidOperationException("no PC/SC reader attached");
    card = reader.ReadFirstCard();
    Console.WriteLine($"① 讀卡   : LIVE — read 健保卡 from reader");
}
catch (Exception e)
{
    // Fabricated fallback (no real data) so the chain is demonstrable without hardware.
    card = new NhiCardBasic("000012345678", "王小明", "A123456789", "1996-03-12", CardSex.Male, "2016-06-01");
    Console.WriteLine($"① 讀卡   : FALLBACK (no live card: {e.Message}) — using fabricated 健保卡 sample");
}
var cardPatient = CardToPatient.ToPatient(card);
Console.WriteLine($"           → Patient  身分證={cardPatient.Identifier[0].Value}  生日={cardPatient.BirthDate}  性別={cardPatient.Gender}  姓名={card.FullName}");

// ── ② 進料 (Ingest) ─────────────────────────────────────────────────────────────────────────────
// A fabricated 健保署 每日上傳 XML (Big5) — the real file format from a clinic HIS export.
const string uploadXml =
    "<?xml version=\"1.0\" encoding=\"Big5\"?>" +
    "<RECS><REC>" +
      "<MSH><h1>1234567890</h1><h2>11305</h2><h3>1</h3></MSH>" +
      "<MB1><A12>A123456789</A12><A13>0850312</A13><A17>11305201030</A17><A18>0001</A18><D19>E1140</D19><D20>王小明</D20></MB1>" +
      "<MB2><p1>1</p1><p2>BC12345100</p2><p3>測試藥品</p3><p5>TIDPC</p5><p6>PO</p6><p7>30</p7><p8>5</p8></MB2>" +
      "<MB2><p1>2</p1><p2>47029C</p2><p7>1</p7><p8>200</p8></MB2>" +
    "</REC></RECS>";
var uploadBytes = Encoding.GetEncoding(950).GetBytes(uploadXml);
var bundle = NhiUploadPipeline.ToFhir(uploadBytes);
var claim = bundle.Entry.Select(e => e.Resource).OfType<Claim>().Single();
Console.WriteLine($"② 進料   : parsed 每日上傳 XML (Big5) → FHIR Bundle");
Console.WriteLine($"           → {bundle.Entry.Count} resources, Claim with {claim.Item.Count} 醫令, "
                  + $"{bundle.Entry.Select(e => e.Resource).OfType<MedicationRequest>().Count()} MedicationRequest, "
                  + $"{bundle.Entry.Select(e => e.Resource).OfType<Procedure>().Count()} Procedure");

// ── ③ FHIR — validate (base-R4 structural gate) ─────────────────────────────────────────────────
var json = NhiPas.ToJson(bundle);
try
{
    _ = FhirJson.Parse<Bundle>(json);   // strict reparse — throws if structurally invalid
    Console.WriteLine($"③ FHIR   : strict reparse OK — base-R4 structurally valid ({Encoding.UTF8.GetByteCount(json)} bytes)");
    Console.WriteLine($"           (note: 媒體申報/上傳 has no FHIR IG → base R4, not a profile-conformant resource)");
}
catch (Exception e)
{
    Console.WriteLine($"③ FHIR   : ✗ INVALID — {e.Message}");
    return 1;
}

// ── ④ 送件 (Submit) — DRY-RUN only ──────────────────────────────────────────────────────────────
Console.WriteLine($"④ 送件   : DRY-RUN — assembled a {Encoding.UTF8.GetByteCount(json)}-byte submission payload.");
Console.WriteLine($"           🔒 NOT transmitted — real submission needs HCA 醫事憑證 + 健保 VPN (醫事機構身分), work-order #13.");

Console.WriteLine(new string('─', 72));
Console.WriteLine("🐉 chain complete: 讀卡 ✅ → 進料 ✅ → FHIR ✅ → 送件 🔒(dry-run). All data fabricated; no PHI.");

// Write the Bundle next to the exe so it can be inspected / fed to `make validate`.
var outPath = Path.Combine(AppContext.BaseDirectory, "dragon-demo-bundle.json");
File.WriteAllText(outPath, json);
Console.WriteLine($"   FHIR Bundle written to: {outPath}");
return 0;
