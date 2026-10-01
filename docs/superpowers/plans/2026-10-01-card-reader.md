# 健保卡 Card Reader Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Read a 健保卡 基本資料段 over PC/SC and turn it into a base-R4 FHIR Patient, in a new CardReader project that keeps the FHIR core clean.

**Architecture:** Hexagonal. New `NhiPasFhir.CardReader` depends on the pure core; core depends on nothing. Pure, hardware-independent `byte[] → NhiCardBasic → Patient` path (fully tested); a thin PC/SC transport shell (SkippableFact, hardware-gated). `RocDate` moves to core as a shared 民國 util (both adapters use it).

**Tech Stack:** C# / .NET 8, Firely `Hl7.Fhir.R4` 6.5.0 (core), `PCSC` 7.0.1 + `System.Text.Encoding.CodePages` (CardReader only), xUnit + Xunit.SkippableFact.

## Global Constraints

- Git on `dev`. End every commit message with: `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`.
- English code/comments/commits; 健保 terms原文.
- No real patient data — fabricated fixtures only.
- **Never fabricate an APDU byte or a field offset.** Every APDU + offset cites `tw-nhi-icc-service` (magiclen, MIT). Uncertain → explicit `// TODO`, never a guessed byte (a wrong card command can return garbage or lock a card).
- **Fail loud**: short/garbage buffer or invalid sex byte throws; never emit a partial/guessed Patient.
- **Boundary**: core MUST NOT reference CardReader and MUST contain no PC/SC/APDU code. CardReader → core only; CardReader MUST NOT depend on the Ingest project (sibling adapters stay independent — hence RocDate moves to core).
- Verified facts (do not re-guess): SELECT `00 A4 04 00 10 D1 58 00 00 01 00 00 00 00 00 00 00 00 00 11 00`; READ `00 CA 11 00 02 00 00`; layout 卡號[0:12] / 姓名[12:32] Big5 NUL-term / 身分證[32:42] / 生日[42:49] ROC YYYMMDD / 性別[49] 'M'/'F' / 發卡日[50:57] ROC. pcsc-sharp: `ContextFactory.Instance.Establish(SCardScope.System)`, `ctx.GetReaders()`, `ctx.ConnectReader(name, SCardShareMode.Shared, SCardProtocol.Any)` → `ICardReader`, `reader.Transmit(byte[] send, byte[] recv) → int`.

---

### Task 0: Move `RocDate` to core + create `NhiPasFhir.CardReader`

**Files:**
- Move (git mv): `impl/csharp/src/NhiPasFhir.Ingest/MediaDeclaration/RocDate.cs` → `impl/csharp/src/NhiPasFhir/Core/RocDate.cs`
- Modify: the moved `RocDate.cs` — namespace → `NhiPasFhir.Core`; generalize the doc (generic 民國 util, used by media-declaration AND 健保卡)
- Modify: `impl/csharp/src/NhiPasFhir.Ingest/MediaDeclaration/MediaDeclarationConverter.cs` — add `using NhiPasFhir.Core;`
- Create: `impl/csharp/src/NhiPasFhir.CardReader/NhiPasFhir.CardReader.csproj`
- Modify: `impl/csharp/NhiPasFhir.sln`, `impl/csharp/tests/NhiPasFhir.Tests/NhiPasFhir.Tests.csproj` (ref CardReader)

**Interfaces:**
- Produces: `NhiPasFhir.Core.RocDate` (moved; same `ToIso`/`ToRoc` API), assembly `NhiPasFhir.CardReader`.

- [ ] **Step 1: Create the CardReader csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="PCSC" Version="7.0.1" />
    <PackageReference Include="System.Text.Encoding.CodePages" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\NhiPasFhir\NhiPasFhir.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Move RocDate to core + fix usings**

```bash
cd ~/Desktop/nhi-pas-fhir/impl/csharp
git mv src/NhiPasFhir.Ingest/MediaDeclaration/RocDate.cs src/NhiPasFhir/Core/RocDate.cs
```
Edit `src/NhiPasFhir/Core/RocDate.cs`: change `namespace NhiPasFhir.MediaDeclaration;` → `namespace NhiPasFhir.Core;` and widen the doc comment first line to: `/// <summary>ROC (民國) date conversion — a generic util (民國 = 西元 − 1911), shared by the media-declaration ingest and the 健保卡 card reader.`
Edit `src/NhiPasFhir.Ingest/MediaDeclaration/MediaDeclarationConverter.cs`: add `using NhiPasFhir.Core;` under the existing `using Hl7.Fhir.Model;`.

- [ ] **Step 3: Wire sln + references**

```bash
cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH
dotnet sln NhiPasFhir.sln add src/NhiPasFhir.CardReader/NhiPasFhir.CardReader.csproj
dotnet add tests/NhiPasFhir.Tests/NhiPasFhir.Tests.csproj reference src/NhiPasFhir.CardReader/NhiPasFhir.CardReader.csproj
```

- [ ] **Step 4: Boundary assert + build + full suite**

```bash
cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH
grep -qE "CardReader|PC/?SC|APDU" src/NhiPasFhir/NhiPasFhir.csproj && echo "VIOLATION" || echo "CORE clean: OK"
dotnet build NhiPasFhir.sln -v q && dotnet test NhiPasFhir.sln --nologo 2>&1 | grep -iE '通過|失敗|passed|failed|total'
```
Expected: `CORE clean: OK`; build 0 errors; all 84 existing tests still pass (RocDate move is transparent).

- [ ] **Step 5: Commit**

```bash
cd ~/Desktop/nhi-pas-fhir && git add -A impl/csharp
git commit -m "refactor(core): move RocDate to core as a shared 民國 util; scaffold NhiPasFhir.CardReader

RocDate is needed by both the ingest and card-reader adapters; a shared util
belongs in core, not in one adapter. Sibling adapters stay independent.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 1: `NhiCardBasic` + `NhiCardBasicParser` (bytes → record, fail-loud)

**Files:**
- Create: `impl/csharp/src/NhiPasFhir.CardReader/NhiCardBasic.cs`
- Test: `impl/csharp/tests/NhiPasFhir.Tests/NhiCardBasicParserTests.cs`

**Interfaces:**
- Consumes: `NhiPasFhir.Core.RocDate`.
- Produces: `NhiPasFhir.CardReader.CardSex` (enum Male/Female); `NhiPasFhir.CardReader.NhiCardBasic` (record: `string CardNo, string FullName, string IdNo, string BirthDateIso, CardSex Sex, string IssueDateIso`); `NhiPasFhir.CardReader.NhiCardBasicParser.Parse(byte[]) → NhiCardBasic`.

- [ ] **Step 1: Write the failing test**

```csharp
using System;
using System.Text;
using NhiPasFhir.CardReader;
using Xunit;

// 健保卡 基本資料段 byte layout (sourced from tw-nhi-icc-service, MIT). Parsed with fabricated bytes.
public class NhiCardBasicParserTests
{
    // Build a fabricated 57-byte basic segment: 卡號[0:12] 姓名[12:32]Big5 身分證[32:42] 生日[42:49] 性別[49] 發卡日[50:57]
    private static byte[] Segment(string cardNo, string name, string id, string birthRoc, char sex, string issueRoc)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var buf = new byte[57];
        void Put(int off, int len, byte[] src) { Array.Copy(src, 0, buf, off, Math.Min(len, src.Length)); }
        Put(0, 12, Encoding.ASCII.GetBytes(cardNo.PadRight(12)));
        Put(12, 20, Encoding.GetEncoding(950).GetBytes(name));          // Big5; remaining bytes stay 0 (NUL term)
        Put(32, 10, Encoding.ASCII.GetBytes(id));
        Put(42, 7, Encoding.ASCII.GetBytes(birthRoc));
        buf[49] = (byte)sex;
        Put(50, 7, Encoding.ASCII.GetBytes(issueRoc));
        return buf;
    }

    [Fact]
    public void Parses_all_fields_including_big5_name_and_roc_dates()
    {
        var c = NhiCardBasicParser.Parse(Segment("000012345678", "王小明", "A123456789", "0850312", 'M', "1050601"));
        Assert.Equal("000012345678", c.CardNo);
        Assert.Equal("王小明", c.FullName);                 // Big5, 0 mojibake
        Assert.Equal("A123456789", c.IdNo);
        Assert.Equal("1996-03-12", c.BirthDateIso);         // ROC 085 -> 1996
        Assert.Equal(CardSex.Male, c.Sex);
        Assert.Equal("2016-06-01", c.IssueDateIso);         // ROC 105 -> 2016
    }

    [Fact]
    public void Short_buffer_fails_loud()
    {
        var ex = Assert.Throws<ArgumentException>(() => NhiCardBasicParser.Parse(new byte[56]));
        Assert.Contains("57", ex.Message);
    }

    [Fact]
    public void Invalid_sex_byte_fails_loud()
    {
        Assert.Throws<ArgumentException>(() =>
            NhiCardBasicParser.Parse(Segment("000012345678", "王小明", "A123456789", "0850312", 'X', "1050601")));
    }
}
```

- [ ] **Step 2: Run → fails** (`NhiCardBasicParser` missing).

Run: `cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH && dotnet test NhiPasFhir.sln --filter NhiCardBasicParserTests -v q`

- [ ] **Step 3: Implement**

```csharp
using System.Text;
using NhiPasFhir.Core;

namespace NhiPasFhir.CardReader;

/// <summary>健保卡 性別.</summary>
public enum CardSex { Male, Female }

/// <summary>Parsed 健保卡 基本資料段. Fields + offsets transcribed from tw-nhi-icc-service (magiclen, MIT)
/// src/card/nhi_card_basic.rs. ROC dates are already converted to ISO (西元). No PHI — fabricated only.</summary>
public sealed record NhiCardBasic(
    string CardNo, string FullName, string IdNo, string BirthDateIso, CardSex Sex, string IssueDateIso);

/// <summary>Parses the 健保卡 基本資料段 byte buffer. Layout (source: tw-nhi-icc-service, MIT):
/// 卡號[0:12] / 姓名[12:32] Big5 NUL-terminated / 身分證[32:42] / 生日[42:49] ROC YYYMMDD / 性別[49] 'M'/'F'
/// / 發卡日[50:57] ROC YYYMMDD. Fails loud on a buffer shorter than 57 or an invalid sex byte.</summary>
public static class NhiCardBasicParser
{
    static NhiCardBasicParser() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static NhiCardBasic Parse(byte[] data)
    {
        if (data.Length < 57)
            throw new ArgumentException($"健保卡 基本資料段 must be at least 57 bytes; got {data.Length}", nameof(data));

        var cardNo = Encoding.ASCII.GetString(data, 0, 12).Trim();

        // 姓名: Big5, NUL-terminated within [12:32].
        var nameEnd = 12;
        while (nameEnd < 32 && data[nameEnd] != 0) nameEnd++;
        var fullName = Encoding.GetEncoding(950).GetString(data, 12, nameEnd - 12).Trim();

        var idNo = Encoding.ASCII.GetString(data, 32, 10).Trim();
        var birthIso = RocDate.ToIso(Encoding.ASCII.GetString(data, 42, 7))
                       ?? throw new ArgumentException("健保卡 生日 is not a valid 民國 date", nameof(data));
        var sex = data[49] switch
        {
            (byte)'M' => CardSex.Male,
            (byte)'F' => CardSex.Female,
            _ => throw new ArgumentException($"健保卡 性別 byte invalid: 0x{data[49]:X2} (expected 'M'/'F')", nameof(data)),
        };
        var issueIso = RocDate.ToIso(Encoding.ASCII.GetString(data, 50, 7))
                       ?? throw new ArgumentException("健保卡 發卡日 is not a valid 民國 date", nameof(data));

        return new NhiCardBasic(cardNo, fullName, idNo, birthIso, sex, issueIso);
    }
}
```

- [ ] **Step 4: Run → passes.**

- [ ] **Step 5: Commit**

```bash
cd ~/Desktop/nhi-pas-fhir
git add impl/csharp/src/NhiPasFhir.CardReader/NhiCardBasic.cs impl/csharp/tests/NhiPasFhir.Tests/NhiCardBasicParserTests.cs
git commit -m "feat(cardreader): parse 健保卡 基本資料段 bytes -> NhiCardBasic (Big5 name, ROC dates, fail-loud)

Layout sourced from tw-nhi-icc-service (MIT). Fabricated fixtures only.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: `CardToPatient` — NhiCardBasic → base-R4 FHIR Patient

**Files:**
- Create: `impl/csharp/src/NhiPasFhir.CardReader/CardToPatient.cs`
- Test: `impl/csharp/tests/NhiPasFhir.Tests/CardToPatientTests.cs`

**Interfaces:**
- Consumes: `NhiCardBasic` (Task 1), `NhiPas.ToJson` + `FhirJson.Parse` (core), `Hl7.Fhir.Model`.
- Produces: `NhiPasFhir.CardReader.CardToPatient.ToPatient(NhiCardBasic) → Hl7.Fhir.Model.Patient`.

- [ ] **Step 1: Write the failing test**

```csharp
using Hl7.Fhir.Model;
using NhiPasFhir;
using NhiPasFhir.Core;
using NhiPasFhir.CardReader;
using Xunit;

// 健保卡 基本資料 -> base-R4 FHIR Patient (identifier=身分證, birthDate, gender). No profile claimed.
public class CardToPatientTests
{
    private static NhiCardBasic Card() =>
        new("000012345678", "王小明", "A123456789", "1996-03-12", CardSex.Male, "2016-06-01");

    [Fact]
    public void Maps_identifier_birthdate_and_gender()
    {
        var p = CardToPatient.ToPatient(Card());
        Assert.Equal("A123456789", p.Identifier.Single().Value);
        Assert.Equal("1996-03-12", p.BirthDate);
        Assert.Equal(AdministrativeGender.Male, p.Gender);
    }

    [Fact]
    public void Output_is_structurally_valid_base_r4()
    {
        var json = NhiPas.ToJson(CardToPatient.ToPatient(Card()));
        var reparsed = FhirJson.Parse<Patient>(json);   // throws if structurally invalid
        Assert.Equal("A123456789", reparsed.Identifier.Single().Value);
    }
}
```

- [ ] **Step 2: Run → fails.**

- [ ] **Step 3: Implement**

```csharp
using Hl7.Fhir.Model;

namespace NhiPasFhir.CardReader;

/// <summary>Builds a base-R4 FHIR Patient from a 健保卡 基本資料段 record. A card read is demographics, not
/// a PA case, so NO profile is claimed (no Patient-twpas / TWCorePatient) — just structurally-valid base R4.
/// 身分證 identifier system is the NHI national-id namespace; TODO: confirm the official canonical URL.</summary>
public static class CardToPatient
{
    // TODO: confirm official identifier system URL for the 身分證 (national id).
    private const string NationalIdSystem = "https://nhicore.nhi.gov.tw/national-id";

    public static Patient ToPatient(NhiCardBasic c) => new()
    {
        Id = "pat-card",
        Identifier = { new Identifier(NationalIdSystem, c.IdNo) },
        Name = { new HumanName { Text = c.FullName } },
        BirthDate = c.BirthDateIso,
        Gender = c.Sex == CardSex.Male ? AdministrativeGender.Male : AdministrativeGender.Female,
    };
}
```

- [ ] **Step 4: Run → passes.**

- [ ] **Step 5: Commit**

```bash
cd ~/Desktop/nhi-pas-fhir
git add impl/csharp/src/NhiPasFhir.CardReader/CardToPatient.cs impl/csharp/tests/NhiPasFhir.Tests/CardToPatientTests.cs
git commit -m "feat(cardreader): NhiCardBasic -> base-R4 FHIR Patient (identifier/birthDate/gender)

No profile claimed (card read = demographics, not a PA case). Passes strict reparse.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: `NhiCardReader` — PC/SC transport (SkippableFact live test)

**Files:**
- Create: `impl/csharp/src/NhiPasFhir.CardReader/NhiCardReader.cs`
- Test: `impl/csharp/tests/NhiPasFhir.Tests/NhiCardReaderIntegrationTests.cs`

**Interfaces:**
- Consumes: `PCSC` (`ContextFactory`, `SCardScope`, `SCardShareMode`, `SCardProtocol`, `ICardReader`), `NhiCardBasicParser`, `CardToPatient`.
- Produces: `NhiPasFhir.CardReader.NhiCardReader` with `IReadOnlyList<string> ListReaders()`, `NhiCardBasic ReadFirstCard()`, and APDU constants `ApduSelect` / `ApduRead`.

- [ ] **Step 1: Write the failing test (SkippableFact — skips without hardware)**

```csharp
using System.Linq;
using NhiPasFhir.CardReader;
using Xunit;

// Live PC/SC read — requires a physical reader + 健保卡. Skipped (never failed) when absent, mirroring
// the CQF-Ruler integration pattern. Hardware absence is not a code failure.
public class NhiCardReaderIntegrationTests
{
    [SkippableFact]
    public void Reads_a_present_card_into_a_patient()
    {
        var reader = new NhiCardReader();
        Skip.If(reader.ListReaders().Count == 0, "no PC/SC reader attached");

        NhiCardBasic card;
        try { card = reader.ReadFirstCard(); }
        catch (System.Exception e) { throw SkipException.ForSkip($"no readable 健保卡: {e.Message}"); }

        Assert.False(string.IsNullOrWhiteSpace(card.IdNo));
        var patient = CardToPatient.ToPatient(card);
        Assert.Equal(card.IdNo, patient.Identifier.Single().Value);
    }

    [Fact]
    public void Apdu_constants_match_the_sourced_reference()
    {
        // Sourced from tw-nhi-icc-service (MIT) — guard against accidental edits.
        Assert.Equal(new byte[] { 0x00, 0xA4, 0x04, 0x00, 0x10, 0xD1, 0x58, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00 }, NhiCardReader.ApduSelect);
        Assert.Equal(new byte[] { 0x00, 0xCA, 0x11, 0x00, 0x02, 0x00, 0x00 }, NhiCardReader.ApduRead);
    }
}
```

- [ ] **Step 2: Run → the `Apdu_constants` fact fails (type missing); the SkippableFact cannot compile yet.**

- [ ] **Step 3: Implement**

```csharp
using PCSC;

namespace NhiPasFhir.CardReader;

/// <summary>PC/SC transport for the 健保卡 基本資料段. SELECT (健保 AID) + READ (GET DATA) APDUs are
/// transcribed verbatim from tw-nhi-icc-service (magiclen, MIT) src/card/mod.rs — do not edit without
/// re-verifying against a real card (a wrong APDU can return garbage or lock a card). Reads only the basic
/// segment (no 醫事人員卡 needed); 就醫序號/寫卡/上傳 need HCA+SAM and are out of scope.</summary>
public sealed class NhiCardReader
{
    // Source: tw-nhi-icc-service (MIT), src/card/mod.rs APDU_SELECT / APDU_READ.
    public static readonly byte[] ApduSelect =
        { 0x00, 0xA4, 0x04, 0x00, 0x10, 0xD1, 0x58, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00 };
    public static readonly byte[] ApduRead = { 0x00, 0xCA, 0x11, 0x00, 0x02, 0x00, 0x00 };

    public IReadOnlyList<string> ListReaders()
    {
        using var ctx = ContextFactory.Instance.Establish(SCardScope.System);
        return ctx.GetReaders();
    }

    /// <summary>Connect to the first reader, SELECT + READ the basic segment, parse it. Throws if no reader,
    /// no card, or the card does not respond with a valid basic segment (fail loud — never a fake Patient).</summary>
    public NhiCardBasic ReadFirstCard()
    {
        using var ctx = ContextFactory.Instance.Establish(SCardScope.System);
        var readers = ctx.GetReaders();
        if (readers.Length == 0) throw new InvalidOperationException("no PC/SC reader attached");

        using var reader = ctx.ConnectReader(readers[0], SCardShareMode.Shared, SCardProtocol.Any);

        var resp = new byte[258];
        var sel = reader.Transmit(ApduSelect, resp);
        if (sel < 2) throw new InvalidOperationException("健保卡 SELECT failed (unsupported reader or no card)");

        var read = reader.Transmit(ApduRead, resp);
        if (read < 57) throw new InvalidOperationException($"健保卡 READ returned {read} bytes (<57); not a 健保卡?");

        // Drop the trailing status word (SW1 SW2) if present beyond the 57-byte segment.
        var segment = resp[..57];
        return NhiCardBasicParser.Parse(segment);
    }
}
```

- [ ] **Step 4: Run → `Apdu_constants` passes; SkippableFact skips (no hardware).**

Run: `cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH && dotnet test NhiPasFhir.sln --filter NhiCardReaderIntegrationTests -v q`
Expected: 1 passed (APDU constants), 1 skipped (live read).

- [ ] **Step 5: Full suite + commit**

```bash
cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH && dotnet test NhiPasFhir.sln --nologo 2>&1 | grep -iE '通過|失敗|略過|total'
cd ~/Desktop/nhi-pas-fhir
git add impl/csharp/src/NhiPasFhir.CardReader/NhiCardReader.cs impl/csharp/tests/NhiPasFhir.Tests/NhiCardReaderIntegrationTests.cs
git commit -m "feat(cardreader): PC/SC transport (SELECT+READ basic segment); live read = SkippableFact

APDU bytes transcribed verbatim from tw-nhi-icc-service (MIT); guarded by a constant-match test.

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Docs + test-count sync

**Files:**
- Create: `spec/docs/nhi-card-reader.md`
- Modify: `README.md`, `TESTING.md`, `AGENTS.md` (test counts)
- Modify: `spec/specs/003-card-reader/spec.md` (Status → Implemented)

- [ ] **Step 1: Write the card-reader doc**

Create `spec/docs/nhi-card-reader.md` with: flow (card → PC/SC SELECT+READ → bytes → NhiCardBasicParser → base-R4 Patient); the sourced layout table + APDU constants (cite tw-nhi-icc-service, MIT); honest notes — basic segment readable without 醫事人員卡, 就醫序號/寫卡/上傳 gated (HCA+SAM, #13), live read is SkippableFact (hardware), Patient is base R4 (no profile); why PC/SC not WebUSB (OS claims the CCID reader).

- [ ] **Step 2: Sync test counts**

```bash
cd ~/Desktop/nhi-pas-fhir/impl/csharp && export PATH=$HOME/.dotnet:$PATH
dotnet test NhiPasFhir.sln --nologo 2>&1 | grep -iE '通過|total'
```
Update the count in `README.md` / `TESTING.md` / `AGENTS.md` to the new totals (added: 3 parser + 2 CardToPatient + 1 APDU-const + 1 live-skippable). The live read is a new SkippableFact (treated as live-integration, like CQF-Ruler).

- [ ] **Step 3: Flip spec status** in `spec/specs/003-card-reader/spec.md` → `Implemented 2026-10-01 — basic-segment read (pure path tested; live read SkippableFact); 就醫序號/寫卡/送件 credential-gated`.

- [ ] **Step 4: Commit**

```bash
cd ~/Desktop/nhi-pas-fhir
git add spec/docs/nhi-card-reader.md README.md TESTING.md AGENTS.md spec/specs/003-card-reader/spec.md
git commit -m "docs(cardreader): document 健保卡 basic-segment -> Patient + sourced APDU/layout + honest gates; sync test count

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage:** FR-001→Task1; FR-002(Big5)→Task1; FR-003(RocDate)→Task0 move + Task1; FR-004(fail-loud)→Task1; FR-005(base-R4 Patient + reparse)→Task2; FR-006(new project/boundary)→Task0; FR-007(PC/SC + SkippableFact)→Task3; FR-008(sourced APDU/offset + TODO)→Task1/Task3 comments + APDU-const test + Task4 doc; FR-009(no PHI)→fabricated fixtures; FR-010(honest gates)→Task3 comment + Task4 doc. SC-001→Task1; SC-002→Task2; SC-003→Task0 Step4; SC-004→Task1; SC-005→Task3. All mapped.

**Placeholder scan:** The two `// TODO` lines (national-id system URL; re-verify APDU on real card) are sourced-mapping markers, not plan gaps. No "handle appropriately".

**Type consistency:** `NhiCardBasic(CardNo,FullName,IdNo,BirthDateIso,Sex:CardSex,IssueDateIso)` used identically in Tasks 1/2/3. `NhiCardBasicParser.Parse(byte[])→NhiCardBasic`, `CardToPatient.ToPatient(NhiCardBasic)→Patient`, `NhiCardReader.{ListReaders,ReadFirstCard,ApduSelect,ApduRead}` consistent across Task 3 test + impl. `RocDate.ToIso` resolves from `NhiPasFhir.Core` after the Task 0 move (tests already `using NhiPasFhir.Core;`).
