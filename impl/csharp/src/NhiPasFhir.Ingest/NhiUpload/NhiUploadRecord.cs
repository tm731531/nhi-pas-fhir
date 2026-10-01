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
