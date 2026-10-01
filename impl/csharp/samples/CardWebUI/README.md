# 插卡 → FHIR 一條龍 demo (網頁 + 小工具)

Two decoupled pieces, each on its own port (mirrors the tw-nhi-icc service + app split):

| piece | port | role | touches reader? |
|---|---|---|---|
| **小工具 `card-helper`** | `:8531` | reads the 健保卡 基本資料段 via PC/SC → JSON | ✅ only this |
| **網頁 `card-web-ui`** | `:8530` | serves the UI + runs the FHIR chain; calls `:8531` for card | ❌ |

The browser only opens `:8530`; it never touches the reader. `:8530` calls `:8531` for card data.

## Package it (once) — self-contained binaries, no .NET SDK on the target

```bash
tools/publish-card-demo.sh linux-x64 dist      # or: win-x64 | osx-arm64 | osx-x64
```

This produces two **standalone single-file executables** (the target machine needs **no .NET SDK, no
source**) under `dist/`:
- `dist/card-helper/card-helper`      (小工具, :8531)
- `dist/card-web-ui/card-web-ui` + `wwwroot/`  (網頁, :8530)

## Run it (on the machine that has the 健保卡 reader)

Prereqs: a PC/SC reader + its driver + the PC/SC daemon running (Linux: `pcscd`; Windows/macOS: the
built-in Smart Card service), and a 健保卡 inserted. **Just run the two binaries** — no `dotnet`:

```bash
# terminal 1 — the card bridge (keep it running, card inserted)
./dist/card-helper/card-helper        # → http://localhost:8531/card

# terminal 2 — the web UI
./dist/card-web-ui/card-web-ui        # → http://localhost:8530
```

(On Windows: `card-helper.exe` / `card-web-ui.exe`, double-click or run from a terminal.)

> Dev-only shortcut (needs the SDK): `dotnet run --project samples/CardHelper` /
> `dotnet run --project samples/CardWebUI`.

Open <http://localhost:8530> →
- **插卡讀取(真卡)** — reads YOUR card via the 小工具 and runs the chain with your real demographics.
- **示範模式(假卡)** — uses a fabricated card, so the page runs even with no reader.

Each run shows: ① 讀卡 → ② 進料/組 FHIR → ③ 驗證 → ④ 送件(dry-run), plus the would-send Bundle JSON.

## Honest notes

- 🔒 **Real data**: 真卡 mode reads only the **基本資料段** (姓名/身分證/生日/性別 — no 醫事人員卡 needed).
  It flows in-memory and is shown in the browser (localhost); **nothing is written to disk or the repo**.
- **病情/醫令 is a fabricated sample** — your card has no claim data; only your identity is real.
- **送件 is DRY-RUN** — the payload is assembled + validated but **not transmitted**. Real submission needs
  an HCA 醫事憑證 + 健保 VPN (醫事機構身分), work-order #13 — swap `DryRunPasSubmitter` → `HttpPasSubmitter`.
- **No 媒體申報/上傳 FHIR IG exists** → the Bundle is base-R4 structurally valid, not profile-conformant.
- **Why a 小工具, not WebUSB**: the browser can't drive a CCID smart-card reader the OS PC/SC stack owns;
  the native 小工具 is the card bridge. See `spec/docs/nhi-card-reader.md`.
