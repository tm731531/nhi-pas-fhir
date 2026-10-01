using System.Runtime.CompilerServices;
using Hl7.Fhir.Model;
using NhiPasFhir.Core;
using static NhiPasFhir.Core.FhirBuild;   // Cc / Cd / R — shared FHIR datatype builders
using Task = Hl7.Fhir.Model.Task;   // disambiguate from System.Threading.Tasks.Task

namespace NhiPasFhir.Plugins;

// IG 對照: 重大傷病 (tw.gov.mohw.nhi.ci 1.0.2) — 官方範例 Bundle-bun-min (4 resources).
// Structure & every code/system transcribed from the official example (fabricated data, no PHI).

/// <summary>重大傷病 (catastrophic-illness) case — reproduces the official example Bundle-bun-min:
/// Task-twci (申請) → focus QuestionnaireResponse-twci (申請書, linkIds 1-7) → Patient-twci + Condition-twci.
/// A different IG from pas (nhi.ci): Task-based, not Claim-based — so it implements ICaseAssembler directly
/// with its own CI canonical, rather than the pas-shaped AbstractCaseAssembler.</summary>
public sealed class CatastrophicIllnessAssembler : IgAssemblerBase
{
    public const string IgId = "tw.gov.mohw.nhi.ci#1.0.2";
    public const string Case = "catastrophic-illness";
    public override string Ig => IgId;
    public override string CaseType => Case;
    protected override string CanonicalBase => Sys.CiBase;

    [ModuleInitializer]
    internal static void Register() => AssemblerFactory.Register(IgId, Case, () => new CatastrophicIllnessAssembler());

    // P from IgAssemblerBase; Cc / Cd / R from Fhir (using static). Cd = Coding, for QR answer valueCoding.

    // --- QuestionnaireResponse item helpers (keep the linkId 1-7 tree readable) ---
    private static QuestionnaireResponse.ItemComponent Grp(string linkId, string text, params QuestionnaireResponse.ItemComponent[] kids)
        => new() { LinkId = linkId, Text = text, Item = kids.ToList() };
    private static QuestionnaireResponse.ItemComponent A(string linkId, string text, DataType value)
        => new() { LinkId = linkId, Text = text, Answer = { new QuestionnaireResponse.AnswerComponent { Value = value } } };

    public override Bundle Assemble(PACase c)
    {
        var idCard = c.Patient.GetValueOrDefault("id_card", "A123456789");
        var name = c.Patient.GetValueOrDefault("name", "王大明");
        var gender = c.Patient.GetValueOrDefault("gender", "male");
        var birth = c.Patient.GetValueOrDefault("birth_date", "2001-01-01");
        var docId = c.Provider.GetValueOrDefault("doctor_id_card", "A234649456");
        var docName = c.Provider.GetValueOrDefault("doctor_name", "王小明");

        var patient = new Patient
        {
            Id = "pat-1", Meta = P("Patient-twci"),
            Identifier =
            {
                new Identifier { Use = Identifier.IdentifierUse.Official, Type = Cc(Sys.V2_0203, "NNxxx"), System = Sys.IdCard, Value = idCard },
                new Identifier { Use = Identifier.IdentifierUse.Official, Type = Cc(Sys.V2_0203, "MR"), System = Sys.Tpech, Value = "123456" },
            },
            Name = { new HumanName { Use = HumanName.NameUse.Usual, Text = name } },
            Telecom =
            {
                new ContactPoint { System = ContactPoint.ContactPointSystem.Sms, Value = "0912345678" },
                new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Value = "0227065866" },
                new ContactPoint { System = ContactPoint.ContactPointSystem.Email, Value = "a123456@nhi.gov.tw" },
            },
            Gender = Enum.Parse<AdministrativeGender>(gender, true),
            BirthDate = birth,
            Address =
            {
                new Address
                {
                    Text = "台北市大安區信義路三段140號",
                    PostalCodeElement = new FhirString { Extension = { new Extension(Sys.ExtTwPostalCode, Cc(Sys.TwcorePostal3, "106")) } },
                },
            },
        };

        var condition = new Condition
        {
            Id = "con-1", Meta = P("Condition-twci"),
            ClinicalStatus = Cc(Sys.ConditionClinical, "active"),
            Category = { Cc(Sys.CiCategory, "01") },
            Subject = R("Patient/pat-1"),
        };

        // NOTE: QuestionnaireResponse-twci FIXES every item.text — the strings below are transcribed
        // VERBATIM from the profile (via the official example); abbreviating them fails validation.
        // medCertBookDate (1.4) has a time-relative invariant (must be within 30 days of the system
        // date). The sample uses a fixed recent date so the golden stays byte-stable; a real caller
        // sets it to the submission date, and re-validating this fixture later needs a fresh value.
        var medCertBookDate = c.Data.TryGetValue("med_cert_book_date", out var mcbd) ? (string)mcbd : "2026-09-15";
        var qr = new QuestionnaireResponse
        {
            Id = "qr-1", Meta = P("QuestionnaireResponse-twci"),
            Questionnaire = Sys.CiQuestionnaire,
            Status = QuestionnaireResponse.QuestionnaireResponseStatus.Completed,
            Item =
            {
                Grp("1", "hosp|院所資訊",
                    A("1.1", "hosp.applMode|申報方式", Cd(Sys.CiApplyMode, "2", "院所代辦")),
                    A("1.2", "hosp.applType|申報類別", Cd(Sys.CiApplyType, "1", "送核")),
                    A("1.3", "hosp.applDate|申請日期", new Date("2024-01-01")),
                    A("1.4", "hosp.medCertBookDate|開立診斷書申請日期", new Date(medCertBookDate)),
                    A("1.5", "hosp.hospId|醫事機構代碼", Cd(Sys.CiOrgId, "0131060029", "衛生福利部臺北醫院")),
                    A("1.6", "hosp.acptNo|受理編號", new FhirString("11218899999")),
                    A("1.7", "hosp.acptNum|受理次數", new Integer(1))),
                A("2", "patient|病人資訊", R("Patient/pat-1")),
                Grp("3", "doctor|醫師資訊",
                    A("3.1", "doctor.diagPrsnId|醫師身分證號", new FhirString(docId)),
                    A("3.2", "doctor.diagPrsnName|診斷醫師姓名", new FhirString(docName))),
                Grp("4", "diagnosis|疾病資訊",
                    A("4.1", "diagnosis.icd10cmCode|主診斷代碼", Cd(Sys.Icd10cmTw, "C49.6", "軀幹結締及軟組織之惡性腫瘤")),
                    Grp("4.2", "diagnosis.examinationReport|檢查報告",
                        A("4.2.1", "diagnosis.examinationReport.reportType|報告類型。當LOINC無法具體描述檢體種類（例如：`47526-9`時），請填寫及補充說明檢體種類。", Cd(Sys.Loinc, "66117-3")),
                        A("4.2.2", "diagnosis.examinationReport.speType|檢體種類", new FhirString("Prostate ; Stomach")),
                        A("4.2.3", "diagnosis.examinationReport.reportResultString|報告結果-文數字", new FhirString("Prostate biopsy: adenocarcinoma, Gleason score 3+3=6. (report text abbreviated)")),
                        A("4.2.4", "diagnosis.examinationReport.reportResultPdf|檢查報告檔案，請填寫完整檔案路徑。填寫格式：「file://檔名.副檔名」。", new FhirString("file://PathologyReport01.pdf")),
                        A("4.2.5", "diagnosis.examinationReport.reportResultPdfTitle|檢查報告名稱", new FhirString("PathologyReport01")),
                        A("4.2.6", "diagnosis.examinationReport.reportDate|報告日期，YYYY-MM-DD。", new Date("2024-01-01"))),
                    Grp("4.3", "diagnosis.medrec|病歷資料",
                        A("4.3.1", "diagnosis.medrec.medrec|病歷資料", new FhirString("file://Medicalrecord01.pdf")),
                        A("4.3.2", "diagnosis.medrec.medrecTitle|病歷資料名稱", new FhirString("Medicalrecord01"))),
                    Grp("4.4", "diagnosis.imageStudy|影像報告",
                        A("4.4.1", "diagnosis.imageStudy.imgItem|影像報告", Cd(Sys.Icd10pcsTw, "B34JZZ3")),
                        A("4.4.2", "diagnosis.imageStudy.imgResult|影像報告結果", new FhirString("CT of chest: small pleural nodules and bone metastases. (report text abbreviated)")),
                        A("4.4.3", "diagnosis.imageStudy.imgDate|影像報告日期", new Date("2024-01-01")),
                        A("4.4.4", "diagnosis.imageStudy.imgBodySite|影像檢查的身體部位", Cd(Sys.Snomed, "774007", "Head and neck structure")),
                        Grp("4.4.5", "diagnosis.imageStudy.imgDicom|DICOM影像",
                            A("4.4.5.1", "diagnosis.imageStudy.imgDicom.studyUid|整項影像檢查的識別碼", new FhirString("urn:oid:2.16.886.2102.54.4546465747.465465465")),
                            Grp("4.4.5.2", "diagnosis.imageStudy.imgDicom.series|每項影像檢查有一個或多個系列(series)的實例",
                                A("4.4.5.2.1", "diagnosis.imageStudy.imgDicom.series.uid|此系列的DICOM系列實例UID", new FhirString("2.16.886.2102.54.4546465747.465465466")),
                                A("4.4.5.2.2", "diagnosis.imageStudy.imgDicom.series.modality|此系列實例所使用的成像儀器", Cd(Sys.Dcm, "CT")),
                                Grp("4.4.5.2.3", "diagnosis.imageStudy.imgDicom.series.instance|系列中的一個SOP實例",
                                    A("4.4.5.2.3.1", "diagnosis.imageStudy.imgDicom.series.instance.uid|DICOM影像", new FhirString("2.25.88017001449189502323411118737039844241")),
                                    A("4.4.5.2.3.2", "diagnosis.imageStudy.imgDicom.series.instance.sopClass|DICOM class 類型", Cd(Sys.RfcUri, "urn:oid:1.2.840.10008.5.1.4.1.1.2"))))),
                        Grp("4.4.6", "diagnosis.imageStudy.imgNonDicom|非DICOM影像",
                            A("4.4.6.1", "diagnosis.imageStudy.imgNonDicom.imgNonDicom|非DICOM影像", new FhirString("file://US01.jpg")),
                            A("4.4.6.2", "diagnosis.imageStudy.imgNonDicom.imgNonDicomMimeType|非DICOM影像MimeType", Cd(Sys.BcpImg, "image/jpeg"))))),
                A("5", "ci|重大傷病", R("Condition/con-1")),
                Grp("6", "cancerStage|癌症期別",
                    A("6.1", "cancerStage.cancerStage|癌症期別，醫院自行填入癌症期別(1~4)，若為不適用者填9(不適用)。", Cd(Sys.CiCancerStage, "1", "第一期")),
                    A("6.2", "cancerStage.assessScore|癌症分期分數或結果", new FhirString("T1")),
                    A("6.3", "cancerStage.assessDate|癌症分期量表評估日期，YYYY-MM-DD，西元年月日，民國前為負數。", new Date("2024-01-01"))),
                Grp("7", "illness|惡性腫瘤重大傷病換發評估表",
                    A("7.1", "illness.oriCancerCode|原發癌症診斷碼，最長為7碼。", Cd(Sys.Icd10cmTw, "C49.4", "腹(部)結締及軟組織之惡性腫瘤")),
                    A("7.2", "illness.oriCancerDxDate|癌症最初診斷日期，西元年月日；不得大於系統日。", new Date("2017-03-16")),
                    A("7.3", "illness.oriCancerAjcc|癌症最初診斷AJCC分期(病理分期或未接受治療前的臨床分期)，依期別填入；若不是用此分類而用其他分類，則填寫9。", Cd(Sys.CiCancerStaging, "9")),
                    A("7.4", "illness.oriCancerAjcc1|癌症最初診斷AJCC分期_補充說明欄位，若前述欄位為9，則請於此欄位描述其他系統之其他分期為何。", new FhirString("T1")),
                    A("7.5", "illness.cancerStatus|目前癌症狀態", Cd(Sys.CiCancerStageStatus, "4", "癌症遠端轉移")),
                    A("7.6", "illness.cancerTreatment|後續治療評估，可複選。", Cd(Sys.CiCancerTreatment, "5", "癌症後遺症及併發症治療")),
                    A("7.7", "illness.cancerTreatmentPlan|後續治療計劃，可複選。", Cd(Sys.CiCancerTreatmentPlan, "2", "放射線治療")),
                    A("7.8", "illness.cancerTreatmentText|補充說明。", new FhirString("補充說明"))),
            },
        };

        var task = new Task
        {
            Id = "task-1", Meta = P("Task-twci"),
            BasedOn = { R("Condition/con-1") },
            Status = Task.TaskStatus.Requested,
            BusinessStatus = Cc(Sys.CiApproveResult, "5"),   // 核定結果: 同意42天
            Intent = Task.TaskIntent.Order,
            Focus = R("QuestionnaireResponse/qr-1"),
            For = R("Patient/pat-1"),
        };

        var ordered = new List<Resource> { task, qr, patient, condition };
        return WrapBundle("bun-1", "Bundle-twci", Bundle.BundleType.Collection, ordered);
    }
}
