# HIS Ingest Feed Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Parse the real 健保署 每日上傳 XML (Big5) into a normalized record and feed the existing FHIR converter so a fabricated sample produces a base-R4-valid FHIR Bundle, inside a new ingest project that keeps the open-source FHIR core clean.

**Architecture:** Hexagonal. New `NhiPasFhir.Ingest` project depends on the pure FHIR core; the core never depends on it. Flow: `每日上傳 XML (Big5) → NhiUploadXmlParser → NhiUploadRecord → NhiUploadToMediaRecord → MediaRecord → MediaDeclarationConverter.ToFhir (unchanged) → Bundle`. The parser/mapper translate 每日上傳 field IDs (MSH/MB1/MB2) into the 媒體申報 `MediaRecord` vocabulary (t/d/p) so the existing converter is reused untouched.

**Tech Stack:** C# / .NET 8, Firely `Hl7.Fhir.R4` 6.5.0 (core only), `System.Text.Encoding.CodePages` (Ingest only, for Big5/cp950), xUnit.

## Global Constraints

- Git on `dev`, never `main`. Commit only the files a step names. End every commit message with: `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`.
- English for all code/comments/commits; keep 健保 domain terms in原文 (每日上傳, 就醫序號, field IDs like `A12`/`p4`).
- No real patient data — every sample/test uses fabricated identifiers only.
- **Never fabricate a field meaning.** Every field the parser/mapper consumes cites its source (go-tw-his-parser `his_import.go` comment and/or the existing converter's 媒體申報 field comments). Any field without a confident target is left unmapped with an explicit `// TODO: confirm from <source>` — never coerced into a FHIR element.
- **Fail loud**: a record missing a field the FHIR output requires (身分證 `A12`) MUST throw naming the field; never emit a Bundle with a blank/fabricated required identifier.
- **Boundary**: the core project (`NhiPasFhir.csproj`) MUST NOT reference the ingest project and MUST contain no Big5/vendor/XML-dialect code. Dependency arrow is ingest → core only.
- No 媒體申報/上傳 FHIR IG exists → conformance gate for this feature is **base-R4 structural validity** via strict Firely reparse (the repo's existing no-IG gate) + a byte-exact golden. State this honestly in output/docs; do not claim a TW profile.
- Target framework `net8.0`; nullable + implicit usings enabled (match sibling csproj files).

---

### Task 0: Create `NhiPasFhir.Ingest` project + move `MediaDeclaration/` into it

Establishes the hexagonal boundary (spec US2, FR-002/003). Deliverable: new project holds the ingest code, core is clean, whole solution builds and all existing tests still pass.

**Files:**
- Create: `impl/csharp/src/NhiPasFhir.Ingest/NhiPasFhir.Ingest.csproj`
- Move (git mv): `impl/csharp/src/NhiPasFhir/MediaDeclaration/{MediaRecord,RocDate,MediaDeclarationParser,MediaDeclarationConverter}.cs` → `impl/csharp/src/NhiPasFhir.Ingest/MediaDeclaration/`
- Modify: `impl/csharp/NhiPasFhir.sln` (add project)
- Modify: `impl/csharp/tests/NhiPasFhir.Tests/NhiPasFhir.Tests.csproj` (add ProjectReference to Ingest)
- Modify: `impl/csharp/samples/MediaTool/MediaTool.csproj` (add ProjectReference to Ingest)

**Interfaces:**
- Consumes: core's `NhiPasFhir` / `NhiPasFhir.Core` types (`NhiPas.ToJson`, `FhirBuild`), `Hl7.Fhir.*`.
- Produces: assembly `NhiPasFhir.Ingest` exposing namespace `NhiPasFhir.MediaDeclaration` (unchanged namespace — the types just live in the ingest assembly now).

- [ ] **Step 1: Create the Ingest csproj**

Create `impl/csharp/src/NhiPasFhir.Ingest/NhiPasFhir.Ingest.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Big5 / cp950 decoding for 每日上傳 XML. Lives HERE, never in the pure FHIR core. -->
    <PackageReference Include="System.Text.Encoding.CodePages" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Hexagonal: ingest depends on core. Core must NEVER reference ingest. -->
    <ProjectReference Include="..\NhiPasFhir\NhiPasFhir.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Move the MediaDeclaration files into Ingest**

Run:
```bash
cd ~/Desktop/nhi-pas-fhir/impl/csharp
mkdir -p src/NhiPasFhir.Ingest/MediaDeclaration
git mv src/NhiPasFhir/MediaDeclaration/MediaRecord.cs            src/NhiPasFhir.Ingest/MediaDeclaration/
git mv src/NhiPasFhir/MediaDeclaration/RocDate.cs               src/NhiPasFhir.Ingest/MediaDeclaration/
git mv src/NhiPasFhir/MediaDeclaration/MediaDeclarationParser.cs src/NhiPasFhir.Ingest/MediaDeclaration/
git mv src/NhiPasFhir/MediaDeclaration/MediaDeclarationConverter.cs src/NhiPasFhir.Ingest/MediaDeclaration/
rmdir src/NhiPasFhir/MediaDeclaration 2>/dev/null || true
```
(The files keep `namespace NhiPasFhir.MediaDeclaration;` — no edit needed; they now compile in the Ingest assembly.)

- [ ] **Step 3: Register the new project + wire references**

Run:
```bash
cd ~/Desktop/nhi-pas-fhir/impl/csharp
export PATH=$HOME/.dotnet:$PATH
dotnet sln NhiPasFhir.sln add src/NhiPasFhir.Ingest/NhiPasFhir.Ingest.csproj
dotnet add tests/NhiPasFhir.Tests/NhiPasFhir.Tests.csproj reference src/NhiPasFhir.Ingest/NhiPasFhir.Ingest.csproj
dotnet add samples/MediaTool/MediaTool.csproj reference src/NhiPasFhir.Ingest/NhiPasFhir.Ingest.csproj
```

- [ ] **Step 4: Verify the boundary — core is clean**

Run:
```bash
cd ~/Desktop/nhi-pas-fhir/impl/csharp
test ! -d src/NhiPasFhir/MediaDeclaration && echo "CORE CLEAN: no MediaDeclaration in core"
grep -q "NhiPasFhir.Ingest" src/NhiPasFhir/NhiPasFhir.csproj && echo "BOUNDARY VIOLATION" || echo "CORE does not reference Ingest: OK"
```
Expected: `CORE CLEAN: no MediaDeclaration in core` and `CORE does not reference Ingest: OK`.

- [ ] **Step 5: Build + run the full existing suite (regression gate)**

Run:
```bash
cd ~/Desktop/nhi-pas-fhir/impl/csharp
export PATH=$HOME/.dotnet:$PATH
dotnet build NhiPasFhir.sln -v q && dotnet test NhiPasFhir.sln -v q
```
Expected: build succeeds; all existing tests PASS (the MediaDeclarationTests now resolve the types via the Ingest reference).

- [ ] **Step 6: Commit**

```bash
cd ~/Desktop/nhi-pas-fhir
git add -A impl/csharp
git commit -m "refactor(ingest): extract NhiPasFhir.Ingest; move MediaDeclaration out of the pure FHIR core

Hexagonal boundary: ingest depends on core; core keeps only Core/ + Plugins/
(pure FHIR, Firely-only). No behaviour change — same tests pass.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 1: `NhiUploadRecord` model (faithful to the 每日上傳 structure)

The strongly-typed in-memory shape mirroring go-tw-his-parser `his_import.go` (`RECS>REC>MSH/MB1/MB2`). Field meanings transcribed from its comments (spec US3, FR-006).

**Files:**
- Create: `impl/csharp/src/NhiPasFhir.Ingest/NhiUpload/NhiUploadRecord.cs`
- Test: `impl/csharp/tests/NhiPasFhir.Tests/NhiUploadRecordTests.cs`

**Interfaces:**
- Produces: `NhiPasFhir.Ingest.NhiUpload.NhiUploadRecord` with `Msh Msh`, `Mb1 Mb1`, `List<Mb2> Orders`; nested `Msh{H1,H2,H3}`, `Mb1{A01,A11,A12,A13,A14,A17,A18,A23,D19,D20,D31,D32}`, `Mb2{P1,P2,P3,P5,P6,P7,P8,D27,D36}` — all `string` (raw as parsed; empty string = absent).

- [ ] **Step 1: Write the failing test**

Create `impl/csharp/tests/NhiPasFhir.Tests/NhiUploadRecordTests.cs`:
```csharp
using NhiPasFhir.Ingest.NhiUpload;
using Xunit;

// The in-memory shape of one 健保署 每日上傳 record (MSH + MB1 + MB2*).
public class NhiUploadRecordTests
{
    [Fact]
    public void Record_holds_header_case_and_order_lines()
    {
        var rec = new NhiUploadRecord
        {
            Msh = new Msh { H1 = "1234567890", H2 = "11305", H3 = "1" },
            Mb1 = new Mb1 { A12 = "A123456789", A13 = "0850312", A17 = "1130520", A18 = "0001", D19 = "E1140", D20 = "王小明" },
            Orders = { new Mb2 { P1 = "1", P2 = "BC12345100", P5 = "TIDPC", P6 = "PO", P7 = "30", P8 = "5" } },
        };

        Assert.Equal("1234567890", rec.Msh.H1);
        Assert.Equal("A123456789", rec.Mb1.A12);
        Assert.Equal("王小明", rec.Mb1.D20);
        Assert.Single(rec.Orders);
        Assert.Equal("BC12345100", rec.Orders[0].P2);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH && dotnet test NhiPasFhir.sln --filter NhiUploadRecordTests -v q`
Expected: FAIL — `NhiUploadRecord` / `Msh` / `Mb1` / `Mb2` do not exist (compile error).

- [ ] **Step 3: Write minimal implementation**

Create `impl/csharp/src/NhiPasFhir.Ingest/NhiUpload/NhiUploadRecord.cs`:
```csharp
namespace NhiPasFhir.Ingest.NhiUpload;

/// <summary>One 健保署 每日上傳 record (`REC` = MSH 訊息表頭 + MB1 就醫基本資料 + MB2 醫令明細*).
/// Field names/meanings transcribed from go-tw-his-parser `his_import.go` (NHIMSH/NHIMB1/NHIMB2).
/// All values are raw strings as parsed (empty string = absent); ROC dates are converted later by the
/// mapper via the core's RocDate. This is a faithful shape, not a FHIR type.</summary>
public sealed class NhiUploadRecord
{
    public Msh Msh { get; set; } = new();
    public Mb1 Mb1 { get; set; } = new();
    public List<Mb2> Orders { get; } = new();
}

/// <summary>MSH 訊息表頭.</summary>
public sealed class Msh
{
    public string H1 { get; set; } = "";  // 醫事機構代號
    public string H2 { get; set; } = "";  // 費用年月 (民國 YYYMM)
    public string H3 { get; set; } = "";  // 申報類別
}

/// <summary>MB1 就醫基本資料.</summary>
public sealed class Mb1
{
    public string A01 { get; set; } = "";  // 資料格式 (1=正常,2=異常,3=補正正常,4=補正異常)
    public string A11 { get; set; } = "";  // 卡片號碼
    public string A12 { get; set; } = "";  // 身分證號 (病患主鍵)
    public string A13 { get; set; } = "";  // 出生日期 (民國 YYYMMDD)
    public string A14 { get; set; } = "";  // 原處方醫療機構代碼
    public string A17 { get; set; } = "";  // 就診日期時間 (民國 YYYMMDDHHMMSS)
    public string A18 { get; set; } = "";  // 就醫序號
    public string A23 { get; set; } = "";  // 就醫類別
    public string D19 { get; set; } = "";  // 主診斷代碼 (ICD-10)
    public string D20 { get; set; } = "";  // 病患姓名
    public string D31 { get; set; } = "";  // 調劑藥師身分證
    public string D32 { get; set; } = "";  // 藥師姓名
}

/// <summary>MB2 醫令明細.</summary>
public sealed class Mb2
{
    public string P1 { get; set; } = "";  // 醫令類別 (1=藥品,2=診療,9=藥事服務費)
    public string P2 { get; set; } = "";  // 醫令代碼 (健保碼)
    public string P3 { get; set; } = "";  // 藥品名稱
    public string P5 { get; set; } = "";  // 使用頻率 (BID/TID/QID…)
    public string P6 { get; set; } = "";  // 給藥途徑 (PO/EXT…)
    public string P7 { get; set; } = "";  // 總量
    public string P8 { get; set; } = "";  // 單價
    public string D27 { get; set; } = ""; // 給藥日份
    public string D36 { get; set; } = ""; // 連處次數
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH && dotnet test NhiPasFhir.sln --filter NhiUploadRecordTests -v q`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
cd ~/Desktop/nhi-pas-fhir
git add impl/csharp/src/NhiPasFhir.Ingest/NhiUpload/NhiUploadRecord.cs impl/csharp/tests/NhiPasFhir.Tests/NhiUploadRecordTests.cs
git commit -m "feat(ingest): NhiUploadRecord model for 每日上傳 (MSH/MB1/MB2)

Fields transcribed from go-tw-his-parser his_import.go; raw strings, no FHIR.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: `NhiUploadXmlParser` — Big5 XML → `NhiUploadRecord[]`

Decodes Big5 (cp950) and parses `RECS>REC>MSH/MB1/MB2*`. Proves the Big5 round-trip (spec SC-005) and multi-record handling (US1 scenario 3).

**Files:**
- Create: `impl/csharp/src/NhiPasFhir.Ingest/NhiUpload/NhiUploadXmlParser.cs`
- Test: `impl/csharp/tests/NhiPasFhir.Tests/NhiUploadXmlParserTests.cs`

**Interfaces:**
- Consumes: `NhiUploadRecord`, `Msh`, `Mb1`, `Mb2` (Task 1).
- Produces: `NhiPasFhir.Ingest.NhiUpload.NhiUploadXmlParser` with static `List<NhiUploadRecord> Parse(byte[] big5Bytes)` and `List<NhiUploadRecord> ParseFile(string path)`.

- [ ] **Step 1: Write the failing test**

Create `impl/csharp/tests/NhiPasFhir.Tests/NhiUploadXmlParserTests.cs`:
```csharp
using System.Text;
using NhiPasFhir.Ingest.NhiUpload;
using Xunit;

// 健保署 每日上傳 XML is Big5 (cp950). Parser must decode it and split REC records faithfully.
public class NhiUploadXmlParserTests
{
    // Fabricated two-REC upload. Built as a UTF-8 string, encoded to Big5 bytes to exercise the real path.
    private const string Xml =
        "<?xml version=\"1.0\" encoding=\"Big5\"?>" +
        "<RECS>" +
          "<REC>" +
            "<MSH><h1>1234567890</h1><h2>11305</h2><h3>1</h3></MSH>" +
            "<MB1><A12>A123456789</A12><A13>0850312</A13><A17>1130520</A17><A18>0001</A18><D19>E1140</D19><D20>王小明</D20></MB1>" +
            "<MB2><p1>1</p1><p2>BC12345100</p2><p3>測試藥品</p3><p5>TIDPC</p5><p6>PO</p6><p7>30</p7><p8>5</p8></MB2>" +
            "<MB2><p1>2</p1><p2>47029C</p2><p7>1</p7><p8>200</p8></MB2>" +
          "</REC>" +
          "<REC>" +
            "<MSH><h1>1234567890</h1><h2>11305</h2><h3>1</h3></MSH>" +
            "<MB1><A12>B234567890</A12><A13>0900101</A13><A17>1130521</A17><D19>I10</D19><D20>陳大文</D20></MB1>" +
            "<MB2><p1>1</p1><p2>AC54321000</p2><p7>14</p7><p8>3</p8></MB2>" +
          "</REC>" +
        "</RECS>";

    private static byte[] Big5() { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); return Encoding.GetEncoding(950).GetBytes(Xml); }

    [Fact]
    public void Parses_each_rec_with_its_mb2_lines()
    {
        var recs = NhiUploadXmlParser.Parse(Big5());
        Assert.Equal(2, recs.Count);
        Assert.Equal("A123456789", recs[0].Mb1.A12);
        Assert.Equal(2, recs[0].Orders.Count);
        Assert.Single(recs[1].Orders);
        Assert.Equal("AC54321000", recs[1].Orders[0].P2);
    }

    [Fact]
    public void Decodes_big5_chinese_without_mojibake()
    {
        var recs = NhiUploadXmlParser.Parse(Big5());
        Assert.Equal("王小明", recs[0].Mb1.D20);   // SC-005: exact characters, no mojibake
        Assert.Equal("陳大文", recs[1].Mb1.D20);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH && dotnet test NhiPasFhir.sln --filter NhiUploadXmlParserTests -v q`
Expected: FAIL — `NhiUploadXmlParser` does not exist.

- [ ] **Step 3: Write minimal implementation**

Create `impl/csharp/src/NhiPasFhir.Ingest/NhiUpload/NhiUploadXmlParser.cs`:
```csharp
using System.Text;
using System.Xml.Linq;

namespace NhiPasFhir.Ingest.NhiUpload;

/// <summary>Parses a 健保署 每日上傳 XML file (`RECS>REC>MSH/MB1/MB2`, Big5/cp950) into NhiUploadRecords.
/// Element names follow the upload format (go-tw-his-parser his_import.go). Decoding is Big5 — a UTF-8
/// assumption would mojibake Chinese 姓名/診斷, so the Big5 provider is registered and used explicitly.</summary>
public static class NhiUploadXmlParser
{
    static NhiUploadXmlParser() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static List<NhiUploadRecord> ParseFile(string path) => Parse(File.ReadAllBytes(path));

    public static List<NhiUploadRecord> Parse(byte[] big5Bytes)
    {
        var xml = Encoding.GetEncoding(950).GetString(big5Bytes);
        var root = XDocument.Parse(xml).Root
                   ?? throw new InvalidOperationException("每日上傳 XML has no root RECS element");

        var recs = new List<NhiUploadRecord>();
        foreach (var rec in root.Elements("REC"))
        {
            var msh = rec.Element("MSH");
            var mb1 = rec.Element("MB1");
            var r = new NhiUploadRecord
            {
                Msh = new Msh { H1 = V(msh, "h1"), H2 = V(msh, "h2"), H3 = V(msh, "h3") },
                Mb1 = new Mb1
                {
                    A01 = V(mb1, "A01"), A11 = V(mb1, "A11"), A12 = V(mb1, "A12"), A13 = V(mb1, "A13"),
                    A14 = V(mb1, "A14"), A17 = V(mb1, "A17"), A18 = V(mb1, "A18"), A23 = V(mb1, "A23"),
                    D19 = V(mb1, "D19"), D20 = V(mb1, "D20"), D31 = V(mb1, "D31"), D32 = V(mb1, "D32"),
                },
            };
            foreach (var mb2 in rec.Elements("MB2"))
                r.Orders.Add(new Mb2
                {
                    P1 = V(mb2, "p1"), P2 = V(mb2, "p2"), P3 = V(mb2, "p3"), P5 = V(mb2, "p5"),
                    P6 = V(mb2, "p6"), P7 = V(mb2, "p7"), P8 = V(mb2, "p8"),
                    D27 = V(mb2, "D27"), D36 = V(mb2, "D36"),
                });
            recs.Add(r);
        }
        return recs;
    }

    private static string V(XElement? parent, string name) => parent?.Element(name)?.Value?.Trim() ?? "";
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH && dotnet test NhiPasFhir.sln --filter NhiUploadXmlParserTests -v q`
Expected: PASS (both facts).

- [ ] **Step 5: Commit**

```bash
cd ~/Desktop/nhi-pas-fhir
git add impl/csharp/src/NhiPasFhir.Ingest/NhiUpload/NhiUploadXmlParser.cs impl/csharp/tests/NhiPasFhir.Tests/NhiUploadXmlParserTests.cs
git commit -m "feat(ingest): Big5 每日上傳 XML parser (RECS>REC>MSH/MB1/MB2)

Decodes cp950 explicitly (CodePagesEncodingProvider) — proven no-mojibake.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: `NhiUploadToMediaRecord` mapper — field-ID translation + fail-loud

Translates 每日上傳 IDs (MSH/MB1/MB2) → 媒體申報 `MediaRecord` IDs (t/d/p) so the existing converter is reused. Throws if 身分證 (A12) is absent (FR-007). Every mapping is sourced; unmappable fields (姓名, 藥品名稱) are left as TODO, not invented.

**Files:**
- Create: `impl/csharp/src/NhiPasFhir.Ingest/NhiUpload/NhiUploadToMediaRecord.cs`
- Test: `impl/csharp/tests/NhiPasFhir.Tests/NhiUploadToMediaRecordTests.cs`

**Interfaces:**
- Consumes: `NhiUploadRecord` (Task 1), `NhiPasFhir.MediaDeclaration.MediaRecord` (core-adjacent, in Ingest).
- Produces: `NhiPasFhir.Ingest.NhiUpload.NhiUploadToMediaRecord` with static `MediaRecord Map(NhiUploadRecord rec)`.

Field translation table (each line sourced: `每日上傳 (his_import.go)` → `媒體申報 (MediaDeclarationConverter comments)`):
```
MSH.h1 醫事機構代號      -> t2 服務機構代號
MSH.h2 費用年月(YYYMM)   -> t3 費用年月
MB1.A12 身分證           -> d3 身分證統一編號   (REQUIRED — throw if empty)
MB1.A13 出生日期(ROC)    -> d11 出生年月日
MB1.A17 就診日期(ROC,取前7碼 YYYMMDD) -> d9 就醫日期 AND d10 (single-visit end)
MB1.A18 就醫序號         -> d29 就醫序號(IC卡)
MB1.D19 主診斷           -> d19 主診斷
MB2.p1 醫令類別          -> p3 醫令類別   (1=藥品/用藥,2=診療,9=藥事服務費 — converter switches on this)
MB2.p2 醫令代碼          -> p4 項目代號
MB2.p5 使用頻率          -> p7 頻率
MB2.p6 給藥途徑          -> p9 途徑
MB2.p7 總量              -> p10 總量
MB2.p8 單價              -> p11 單價
(generated running index) -> p13 醫令序
-- NOT mapped (no faithful target): MB1.D20 姓名, MB2.p3 藥品名稱 (converter uses codes, not names),
   點數 p12 (每日上傳 is a submission not a billing total). Carried nowhere; TODO if ever needed.
```

- [ ] **Step 1: Write the failing test**

Create `impl/csharp/tests/NhiPasFhir.Tests/NhiUploadToMediaRecordTests.cs`:
```csharp
using System;
using NhiPasFhir.Ingest.NhiUpload;
using Xunit;

// 每日上傳 (MSH/MB1/MB2) -> 媒體申報 MediaRecord (t/d/p), so the existing converter is reused unchanged.
public class NhiUploadToMediaRecordTests
{
    private static NhiUploadRecord Sample() => new()
    {
        Msh = new Msh { H1 = "1234567890", H2 = "11305" },
        Mb1 = new Mb1 { A12 = "A123456789", A13 = "0850312", A17 = "11305201030", A18 = "0001", D19 = "E1140" },
        Orders =
        {
            new Mb2 { P1 = "1", P2 = "BC12345100", P5 = "TIDPC", P6 = "PO", P7 = "30", P8 = "5" },
            new Mb2 { P1 = "2", P2 = "47029C", P7 = "1", P8 = "200" },
        },
    };

    [Fact]
    public void Maps_header_case_and_order_fields_to_media_ids()
    {
        var m = NhiUploadToMediaRecord.Map(Sample());
        Assert.Equal("1234567890", m.T("t2"));
        Assert.Equal("A123456789", m.D("d3"));
        Assert.Equal("0850312", m.D("d11"));
        Assert.Equal("1130520", m.D("d9"));     // A17 first 7 chars (YYYMMDD), time dropped
        Assert.Equal("0001", m.D("d29"));
        Assert.Equal("E1140", m.D("d19"));
        Assert.Equal(2, m.Orders.Count);
        Assert.Equal("1", m.Orders[0]["p3"]);   // 醫令類別 from MB2.p1
        Assert.Equal("BC12345100", m.Orders[0]["p4"]); // code from MB2.p2
        Assert.Equal("TIDPC", m.Orders[0]["p7"]);      // freq from MB2.p5
        Assert.Equal("PO", m.Orders[0]["p9"]);         // route from MB2.p6
        Assert.Equal("30", m.Orders[0]["p10"]);        // qty from MB2.p7
        Assert.Equal("5", m.Orders[0]["p11"]);         // price from MB2.p8
        Assert.Equal("2", m.Orders[1]["p3"]);
    }

    [Fact]
    public void Missing_national_id_fails_loud()
    {
        var bad = Sample();
        bad.Mb1.A12 = "";
        var ex = Assert.Throws<ArgumentException>(() => NhiUploadToMediaRecord.Map(bad));
        Assert.Contains("A12", ex.Message);   // names the missing field — never a blank Patient id
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH && dotnet test NhiPasFhir.sln --filter NhiUploadToMediaRecordTests -v q`
Expected: FAIL — `NhiUploadToMediaRecord` does not exist.

- [ ] **Step 3: Write minimal implementation**

Create `impl/csharp/src/NhiPasFhir.Ingest/NhiUpload/NhiUploadToMediaRecord.cs`:
```csharp
using NhiPasFhir.MediaDeclaration;

namespace NhiPasFhir.Ingest.NhiUpload;

/// <summary>Normalizes a 每日上傳 record (MSH/MB1/MB2) into the 媒體申報 MediaRecord vocabulary (t/d/p) so
/// MediaDeclarationConverter is reused unchanged. Every target is sourced (see the plan's translation
/// table); 姓名 (D20) and 藥品名稱 (p3) have no faithful MediaRecord/converter target and are intentionally
/// NOT mapped — mapping them would require inventing a field, which the repo's #1 rule forbids.</summary>
public static class NhiUploadToMediaRecord
{
    public static MediaRecord Map(NhiUploadRecord rec)
    {
        // 身分證 is the Patient primary key — fail loud rather than emit a blank/fabricated identifier.
        if (string.IsNullOrWhiteSpace(rec.Mb1.A12))
            throw new ArgumentException("每日上傳 record missing required 身分證 (MB1.A12); cannot build Patient identifier", nameof(rec));

        var m = new MediaRecord();

        // 總表段 t
        Put(m.Summary, "t2", rec.Msh.H1);   // 醫事機構代號 -> 服務機構代號
        Put(m.Summary, "t3", rec.Msh.H2);   // 費用年月 (YYYMM)

        // 點數清單段 d
        Put(m.Case, "d3", rec.Mb1.A12);     // 身分證
        Put(m.Case, "d11", rec.Mb1.A13);    // 出生日期 (ROC)
        var visit = Ymd(rec.Mb1.A17);       // 就診日期時間 -> YYYMMDD
        Put(m.Case, "d9", visit);           // 就醫日期
        Put(m.Case, "d10", visit);          // 單次就醫: 迄日 = 起日
        Put(m.Case, "d29", rec.Mb1.A18);    // 就醫序號 (IC 卡)
        Put(m.Case, "d19", rec.Mb1.D19);    // 主診斷

        // 醫令清單段 p
        var seq = 1;
        foreach (var o in rec.Orders)
        {
            var p = new Dictionary<string, string>();
            Put(p, "p13", seq.ToString()); // 醫令序 (generated — 每日上傳 MB2 has none)
            Put(p, "p3", o.P1);            // 醫令類別
            Put(p, "p4", o.P2);            // 醫令代碼
            Put(p, "p7", o.P5);            // 頻率
            Put(p, "p9", o.P6);            // 途徑
            Put(p, "p10", o.P7);           // 總量
            Put(p, "p11", o.P8);           // 單價
            // NOT mapped: o.P3 藥品名稱 (converter keys on code p4, not name) — TODO if names ever needed.
            m.Orders.Add(p);
            seq++;
        }
        return m;
    }

    /// <summary>ROC YYYMMDD(HHMMSS) -> YYYMMDD (drop any time); pass through shorter/empty unchanged.</summary>
    private static string Ymd(string roc) => roc.Length >= 7 ? roc[..7] : roc;

    private static void Put(Dictionary<string, string> d, string key, string value)
    {
        if (!string.IsNullOrEmpty(value)) d[key] = value;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH && dotnet test NhiPasFhir.sln --filter NhiUploadToMediaRecordTests -v q`
Expected: PASS (both facts).

- [ ] **Step 5: Commit**

```bash
cd ~/Desktop/nhi-pas-fhir
git add impl/csharp/src/NhiPasFhir.Ingest/NhiUpload/NhiUploadToMediaRecord.cs impl/csharp/tests/NhiPasFhir.Tests/NhiUploadToMediaRecordTests.cs
git commit -m "feat(ingest): map 每日上傳 (MSH/MB1/MB2) -> MediaRecord (t/d/p), fail-loud on missing 身分證

Sourced field translation; 姓名/藥品名稱 left unmapped (no faithful target), not invented.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: End-to-end — Big5 file → FHIR Bundle, base-R4 gate + golden

Wires parser → mapper → existing converter and locks the result with the repo's no-IG gate (strict Firely reparse) plus a byte-exact golden (spec SC-001, SC-006).

**Files:**
- Create: `impl/csharp/src/NhiPasFhir.Ingest/NhiUpload/NhiUploadPipeline.cs`
- Test: `impl/csharp/tests/NhiPasFhir.Tests/NhiUploadEndToEndTests.cs`
- Create (golden): `impl/csharp/tests/NhiPasFhir.Tests/goldens/nhi-upload-bundle.json` (generated in Step 3)

**Interfaces:**
- Consumes: `NhiUploadXmlParser` (Task 2), `NhiUploadToMediaRecord` (Task 3), `MediaDeclarationConverter.ToFhir` + `NhiPas.ToJson` + `FhirJson.Parse` (core).
- Produces: `NhiPasFhir.Ingest.NhiUpload.NhiUploadPipeline` with static `Bundle ToFhir(byte[] big5Bytes)` returning the Bundle of the **first** REC (one-case-per-Bundle, mirroring MediaDeclarationConverter's one-case shape).

- [ ] **Step 1: Write the failing test**

Create `impl/csharp/tests/NhiPasFhir.Tests/NhiUploadEndToEndTests.cs`:
```csharp
using System.IO;
using System.Text;
using Hl7.Fhir.Model;
using NhiPasFhir;
using NhiPasFhir.Core;
using NhiPasFhir.Ingest.NhiUpload;
using Xunit;

// Full ingest thread: Big5 每日上傳 XML -> parser -> mapper -> existing converter -> FHIR Bundle.
public class NhiUploadEndToEndTests
{
    private const string Xml =
        "<?xml version=\"1.0\" encoding=\"Big5\"?>" +
        "<RECS><REC>" +
          "<MSH><h1>1234567890</h1><h2>11305</h2><h3>1</h3></MSH>" +
          "<MB1><A12>A123456789</A12><A13>0850312</A13><A17>11305201030</A17><A18>0001</A18><D19>E1140</D19><D20>王小明</D20></MB1>" +
          "<MB2><p1>1</p1><p2>BC12345100</p2><p3>測試藥品</p3><p5>TIDPC</p5><p6>PO</p6><p7>30</p7><p8>5</p8></MB2>" +
          "<MB2><p1>2</p1><p2>47029C</p2><p7>1</p7><p8>200</p8></MB2>" +
        "</REC></RECS>";

    private static byte[] Big5()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(950).GetBytes(Xml);
    }

    [Fact]
    public void Produces_patient_claim_and_orders()
    {
        var bundle = NhiUploadPipeline.ToFhir(Big5());
        var patient = bundle.Entry.Select(e => e.Resource).OfType<Patient>().Single();
        Assert.Equal("A123456789", patient.Identifier.Single().Value);
        Assert.Equal("1996-03-12", patient.BirthDate);           // A13 ROC -> 西元
        var claim = bundle.Entry.Select(e => e.Resource).OfType<Claim>().Single();
        Assert.Equal(2, claim.Item.Count);                       // two MB2 lines
        Assert.Single(bundle.Entry.Select(e => e.Resource).OfType<MedicationRequest>()); // p1=1
        Assert.Single(bundle.Entry.Select(e => e.Resource).OfType<Procedure>());          // p1=2
    }

    [Fact]
    public void Output_is_structurally_valid_base_r4()
    {
        // No 媒體申報/上傳 FHIR IG exists -> gate is base-R4 structural validity via strict Firely reparse.
        var json = NhiPas.ToJson(NhiUploadPipeline.ToFhir(Big5()));
        var reparsed = FhirJson.Parse<Bundle>(json);             // throws if structurally invalid
        Assert.NotEmpty(reparsed.Entry);
    }

    [Fact]
    public void Matches_golden()
    {
        var json = NhiPas.ToJson(NhiUploadPipeline.ToFhir(Big5()));
        var golden = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goldens", "nhi-upload-bundle.json"));
        Assert.Equal(golden.Trim(), json.Trim());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH && dotnet test NhiPasFhir.sln --filter NhiUploadEndToEndTests -v q`
Expected: FAIL — `NhiUploadPipeline` does not exist.

- [ ] **Step 3: Write minimal implementation + generate the golden**

Create `impl/csharp/src/NhiPasFhir.Ingest/NhiUpload/NhiUploadPipeline.cs`:
```csharp
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
```

Generate the golden from the real output (first run writes it, then it is committed and asserted against):
```bash
cd ~/Desktop/nhi-pas-fhir/impl/csharp
export PATH=$HOME/.dotnet:$PATH
mkdir -p tests/NhiPasFhir.Tests/goldens
# Emit the pipeline output once to seed the golden, using a tiny dotnet-script-free approach:
dotnet test NhiPasFhir.sln --filter "NhiUploadEndToEndTests.Output_is_structurally_valid_base_r4" -v q
```
Then seed the golden by temporarily making `Matches_golden` write-if-missing: if `goldens/nhi-upload-bundle.json` is absent, change Step-1's `Matches_golden` to first `File.WriteAllText(path, json)` when the file does not exist, run the test once (it writes + passes), then revert that line so the test is a pure assertion. Verify the written golden visually (Patient A123456789, Claim with 2 items) before committing. (Add the copy rule so the golden reaches the test output dir — the tests csproj already copies `goldens\**\*.json`.)

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH && dotnet test NhiPasFhir.sln --filter NhiUploadEndToEndTests -v q`
Expected: PASS (all three facts, golden now present and matching).

- [ ] **Step 5: Full-suite regression + commit**

```bash
cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH && dotnet test NhiPasFhir.sln -v q
cd ~/Desktop/nhi-pas-fhir
git add impl/csharp/src/NhiPasFhir.Ingest/NhiUpload/NhiUploadPipeline.cs impl/csharp/tests/NhiPasFhir.Tests/NhiUploadEndToEndTests.cs impl/csharp/tests/NhiPasFhir.Tests/goldens/nhi-upload-bundle.json
git commit -m "feat(ingest): end-to-end 每日上傳 Big5 XML -> FHIR Bundle (base-R4 gate + golden)

Reuses MediaDeclarationConverter unchanged. Gate = strict Firely reparse (no
upload FHIR IG exists) + byte-exact golden.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Docs + honest limitations + test-count sync

Record the new capability, the sourced field map, and the honest no-IG limitation; fix the recurring hardcoded test-count drift.

**Files:**
- Create: `spec/docs/nhi-upload-xml-to-fhir.md`
- Modify: `README.md` (+ `TESTING.md` if it carries a test count) — update the test count
- Modify: `spec/specs/002-his-ingest-feed/spec.md` (Status → Implemented)

- [ ] **Step 1: Write the ingest doc**

Create `spec/docs/nhi-upload-xml-to-fhir.md` with: (a) what 每日上傳 XML is (`RECS>REC>MSH/MB1/MB2`, Big5); (b) the full field-translation table from Task 3 with each row's source (his_import.go comment / converter comment) and the explicitly-unmapped fields (姓名/藥品名稱) with the reason; (c) the honest limitation — **no 媒體申報/上傳 FHIR IG exists, so output is base-R4 (+ TW Core where the converter applies it), not profile-conformant**; (d) the gate (strict Firely reparse + golden); (e) scope note: 費用申報 CSV + vendor exports deferred; 讀卡/送件 are separate features and 送件 is credential-gated.

- [ ] **Step 2: Sync the test count**

Run to get the real number, then update the figure wherever it appears:
```bash
cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH
dotnet test NhiPasFhir.sln -v q 2>&1 | grep -iE 'Passed!|total'
grep -rniE 'tests? (pass|green)|[0-9]+ tests' README.md TESTING.md 2>/dev/null
```
Update the count in `README.md`/`TESTING.md` to the new total. (Known footgun: this count is hardcoded in multiple docs; update every occurrence the grep finds.)

- [ ] **Step 3: Flip the spec status**

In `spec/specs/002-his-ingest-feed/spec.md`, change the `**Status**:` line to `Implemented 2026-10-01 — 每日上傳 XML ingest; 費用申報 CSV + vendor exports deferred`.

- [ ] **Step 4: Commit**

```bash
cd ~/Desktop/nhi-pas-fhir
git add spec/docs/nhi-upload-xml-to-fhir.md README.md TESTING.md spec/specs/002-his-ingest-feed/spec.md
git commit -m "docs(ingest): document 每日上傳 XML->FHIR field map + honest no-IG limitation; sync test count

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage:**
- FR-001 (parse RECS>REC>MSH/MB1/MB2, Big5) → Task 2. FR-002/US2 (new project, arrow ingest→core) → Task 0 (+Step 4 boundary assert). FR-003 (move MediaDeclaration) → Task 0. FR-004 (reuse converter) → Task 3 mapper + Task 4 pipeline. FR-005 (RocDate) → reused by converter; mapper passes ROC through (converter calls RocDate). FR-006 (sourced/TODO) → Task 1 comments + Task 3 table + Task 5 doc. FR-007 (fail-loud A12) → Task 3. FR-008 (base-R4 gate) → Task 4. FR-009 (no PHI) → all samples fabricated. FR-010 (honest no-IG) → Task 4 comment + Task 5 doc. FR-011 (XML-only scope) → stated in Task 5 Step 3. SC-001→Task4; SC-002→Task0 Step4; SC-003→Tasks1/3/5; SC-004→Task3; SC-005→Task2; SC-006→Task4.
- All requirements map to a task. No gaps.

**Placeholder scan:** No TBD/"handle appropriately". The `// TODO:` lines are the spec-mandated faithful-mapping markers (sourced reason given), not plan placeholders. Task 4 Step 3 golden-seeding is described concretely (write-if-missing then revert).

**Type consistency:** `NhiUploadRecord{Msh,Mb1,Orders}`, `Msh{H1,H2,H3}`, `Mb1{A12,A13,A17,A18,D19,D20,...}`, `Mb2{P1,P2,P3,P5,P6,P7,P8}` consistent across Tasks 1-4. `NhiUploadXmlParser.Parse(byte[])` (Task 2) used by `NhiUploadPipeline.ToFhir(byte[])` (Task 4). `NhiUploadToMediaRecord.Map` returns `MediaRecord` consumed by `MediaDeclarationConverter.ToFhir` (verified signature from source). Media-ID keys written by the mapper (p3/p4/p7/p9/p10/p11) match exactly the keys the converter reads (`Get(p,"p3")` 醫令類別, `"p4"`, `"p7"`, `"p9"`, `"p10"`, `"p11"`).
