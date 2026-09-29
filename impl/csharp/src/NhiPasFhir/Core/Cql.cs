using Hl7.Fhir.Model;

namespace NhiPasFhir.Core;

// IG 對照 (spec/docs/IG-TRACEABILITY.md): 預檢規則 (FHIR CQL) — tw.gov.mohw.nhi.cql 的 Rule Libraries (ELM)。
// 這是「查 Bundle」那一站接官方 CQL 深腦的插槽。設計反映 verify-tom-understanding 校準後的理解:
//   • 送前自查 = 組完 Bundle 後、POST 前,用健保『同一套』官方 ELM 對 Bundle 跑一次 (高信心預測,非保證)。
//   • 輸出三態,不是 pass/fail:條件不符=核刪、必要資料未填=補件 — 自查最能自動擋的是「資料未填」型。
//   • 引擎跑的是 ELM (非 .cql 文字);.NET 無成熟原生引擎 → 接 sidecar (JS cql-execution / Java cqframework / CQF-Ruler)。
//   • ⚠️ Bundle 必須是「縱貫病歷」:續用/回診規則會 retrieve 前次申請/治療;Bundle 漏病史 → retrieve 撈空 → 靜默判錯。

/// <summary>自查結果三態 (+ 未評估)。NotEvaluated = CQL 沒啟動或沒對應規則。</summary>
public enum CqlOutcome { NotEvaluated, Pass, WouldBeRejected /*核刪:條件不符*/, DataMissing /*補件:必要資料未填*/ }

public sealed record CqlFinding(CqlOutcome Outcome, string RuleIds, IReadOnlyList<string> Reasons);

/// <summary>ELM 執行引擎 (= SQL 的 SQL Server 角色)。本 lib 不內含 —— 接一個 sidecar。實作者務必:
/// (1) 載入該規則的『整個依賴閉包』(BCReusable / BCCodeConcept / FHIRHelpers…),只載單檔會失敗;
/// (2) context Patient 對整份 Bundle 執行;(3) 回傳『具名 expression 值』(bool/tuple),不是現成核准/核刪。</summary>
public interface ICqlEngine
{
    /// <summary>對 bundle 跑 ruleId 的 ELM(含依賴閉包),回具名 expression 結果。</summary>
    IReadOnlyDictionary<string, object?> Evaluate(string ruleId, Bundle bundle);
}

/// <summary>框架面的 CQL 預檢插槽。Enabled=false 代表「沒啟動 CQL」。</summary>
public interface ICqlPreCheck
{
    bool Enabled { get; }
    CqlFinding Evaluate(Bundle bundle, string drugCode);
}

/// <summary>『沒啟動 CQL』— 預設。無引擎 → 一律回 NotEvaluated(pipeline 照舊只跑種子 PreCheck)。</summary>
public sealed class NoCqlPreCheck : ICqlPreCheck
{
    public static readonly NoCqlPreCheck Instance = new();
    public bool Enabled => false;
    public CqlFinding Evaluate(Bundle bundle, string drugCode)
        => new(CqlOutcome.NotEvaluated, "", new[] { "CQL 預檢未啟動(未接引擎)。" });
}

/// <summary>『啟動 CQL』— 接一個 <see cref="ICqlEngine"/> + 藥碼→規則索引。編碼校準後的理解:
/// 藥碼→規則是 1:N(一藥多規 Rule1/2/3,全部要過);每條由引擎載入依賴閉包執行;引擎回的具名值由此類
/// 詮釋成三態。規則本身不進 codebase —— 只有這段『挑檔+呼叫+詮釋』的膠水。</summary>
public sealed class CqlPreCheck : ICqlPreCheck
{
    private readonly ICqlEngine _engine;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _drugToRules; // 藥碼 → 1..N ruleId

    public CqlPreCheck(ICqlEngine engine, IReadOnlyDictionary<string, IReadOnlyList<string>> drugToRules)
        => (_engine, _drugToRules) = (engine, drugToRules);

    public bool Enabled => true;

    public CqlFinding Evaluate(Bundle bundle, string drugCode)
    {
        if (!_drugToRules.TryGetValue(drugCode, out var rules) || rules.Count == 0)
            return new(CqlOutcome.NotEvaluated, "", new[] { $"藥碼 {drugCode} 無對應 CQL 規則。" });

        var reasons = new List<string>();
        var worst = CqlOutcome.Pass;                         // 一藥多規:任一沒過就沒過;資料未填優先於條件不符
        foreach (var ruleId in rules)
        {
            var named = _engine.Evaluate(ruleId, bundle);    // 引擎載入 ELM + 依賴閉包並執行
            var (outcome, why) = Interpret(ruleId, named);
            reasons.AddRange(why);
            if (outcome == CqlOutcome.DataMissing) worst = CqlOutcome.DataMissing;
            else if (outcome == CqlOutcome.WouldBeRejected && worst != CqlOutcome.DataMissing)
                worst = CqlOutcome.WouldBeRejected;
        }
        return new(worst, string.Join(",", rules), reasons);
    }

    /// <summary>把引擎回的『具名 expression 值』詮釋成三態。命名慣例取自實際規則的輸出 define:
    /// 「…申請結果_布林」= 最終核准布林;「…報告總結」= 人可讀總結;凡「…資料存在 / 有填…」= false → 屬資料未填。</summary>
    private static (CqlOutcome, IReadOnlyList<string>) Interpret(string ruleId, IReadOnlyDictionary<string, object?> named)
    {
        // 必要資料未填 → 補件 (自查最能自動擋的一態)
        var missing = named.Where(kv => (kv.Key.Contains("資料存在") || kv.Key.Contains("有填")) && kv.Value is false)
                           .Select(kv => $"[{ruleId}] 缺:{kv.Key}").ToList();
        if (missing.Count > 0) return (CqlOutcome.DataMissing, missing);

        var result = named.FirstOrDefault(kv => kv.Key.Contains("申請結果_布林")).Value;
        if (result is true) return (CqlOutcome.Pass, new[] { $"[{ruleId}] 通過" });

        var summary = named.FirstOrDefault(kv => kv.Key.Contains("報告總結")).Value as string;
        return (CqlOutcome.WouldBeRejected, new[] { $"[{ruleId}] 條件不符:{summary ?? "見規則"}" });
    }
}

/// <summary>未接引擎時的佔位:啟動 CQL 但還沒接 sidecar → 呼叫時明確 fail-loud,不假裝跑過。</summary>
public sealed class NotWiredCqlEngine : ICqlEngine
{
    public IReadOnlyDictionary<string, object?> Evaluate(string ruleId, Bundle bundle)
        => throw new NotSupportedException(
            "CQL 引擎尚未接上。請接一個 sidecar(JS cql-execution / Java cqframework / CQF-Ruler)," +
            "載入規則 ELM 的完整依賴閉包後執行。見 spec/docs/cql-integration-notes.md。");
}
