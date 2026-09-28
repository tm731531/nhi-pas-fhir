# 03 — Artifacts Catalogue (完整對應 IG)

> SoT: <https://nhicore.nhi.gov.tw/pas/artifacts.html> · IG v1.2.6
> Every IG artifact is listed here so nothing is silently dropped. `impl` = code status in `src/`.

## Profiles / StructureDefinitions (~40)

### Bundle
| Profile | Purpose | impl |
|---|---|---|
| 癌藥事前審查-Bundle TWPAS | cancer-drug request bundle | core |
| 事前審查回覆-Bundle Response TWPAS | response bundle | core |
| 免疫製劑事前審查-Bundle Immunologic Agent TWPAS | immunologic-agent request bundle | TODO |

### Claim / ClaimResponse
| Profile | Purpose | impl |
|---|---|---|
| 癌藥事前審查-Claim TWPAS | cancer-drug application | core |
| 免疫製劑事前審查-Claim Immunologic Agent TWPAS | immunologic-agent application | TODO |
| 事前審查回覆-ClaimResponse TWPAS | adjudication response | core |
| 自主審查-ClaimResponse Self Assessment TWPAS | self-assessment (心/肝移植) | TODO |

### Medication
| Profile | Purpose | impl |
|---|---|---|
| 事前審查品項-MedicationRequest Apply TWPAS | applied item | core |
| 用藥品項-MedicationRequest Treat TWPAS | treatment medication | core |

### Party (Patient / Practitioner / Organization)
| Profile | Purpose | impl |
|---|---|---|
| 病人資訊-Patient TWPAS | patient | core |
| 醫事人員-Practitioner TWPAS | applying / signing physician | core |
| 醫事機構-Organization TWPAS | institution | core |
| 基因檢測機構-Organization Genetic Testing TWPAS | genetic-testing lab | TODO |

### Encounter
| Profile | Purpose | impl |
|---|---|---|
| 就醫科別-Encounter TWPAS | service department | TODO |
| 門診病歷-Encounter OPD TWPAS | OPD encounter | TODO |

### Observation
| Profile | Purpose | impl |
|---|---|---|
| 基因資訊-Observation Diagnostic TWPAS | genetic info | TODO |
| 檢驗檢查-Observation Laboratory Result TWPAS | lab result | TODO |
| 病人狀態評估-Observation Patient Assessment TWPAS | CTCAE / NYHA / PDAI | TODO |
| 治療後疾病狀態評估-Observation Treatment Assessment TWPAS | post-treatment status | TODO |
| 癌症分期量表-Observation Cancer Stage TWPAS | TNM / FIGO / CNS | TODO |
| 主觀描述-Observation Subjective TWPAS | SOAP-S (免疫製劑) | TODO |
| 客觀描述-Observation Objective TWPAS | SOAP-O | TODO |
| 血型-Observation Blood Group TWPAS | blood group | TODO |

### DiagnosticReport / Imaging
| Profile | Purpose | impl |
|---|---|---|
| 檢查報告-DiagnosticReport TWPAS | disease report | TODO |
| 影像報告-DiagnosticReport Image TWPAS | image report | TODO |
| DICOM影像-ImagingStudy TWPAS | DICOM study | TODO |
| 非DICOM影像-Media TWPAS | non-DICOM media | TODO |

### Procedure / Substance
| Profile | Purpose | impl |
|---|---|---|
| 放射治療-Procedure TWPAS | radiotherapy | TODO |
| 照光治療-Procedure Phototherapy TWPAS | phototherapy | TODO |
| 放射治療總劑量-Substance TWPAS | total radiotherapy dose | TODO |
| 照光治療總次數-Substance Phototherapy TWPAS | total phototherapy count | TODO |

### Other
| Profile | Purpose | impl |
|---|---|---|
| 健保事前審查計畫-Coverage TWPAS | NHI PA plan | core |
| 基因檢測檢體-Specimen TWPAS | genetic specimen | TODO |
| 文件參照-DocumentReference TWPAS | treatment plan / gene report | TODO |
| 病情診斷-Condition TWPAS | primary disease + comorbidity | core |
| 計畫-CarePlan TWPAS | SOAP-P | TODO |
| 評估-ClinicalImpression TWPAS | SOAP-A | TODO |
| 過敏史-AllergyIntolerance TWPAS | allergy history | TODO |
| 門診病歷-Composition OPD TWPAS | OPD composition | TODO |
| 系統回應訊息-OperationOutcome TWPAS | system messages | core |

### Logical Models (3)
- 申請(Apply)癌症用藥事前審查之資料模型
- 回覆(Response)事前審查之資料模型
- 申請(Apply)免疫製劑事前審查之資料模型

## Extensions (3)
`ClaimEncounter` (就醫科別) · `ClaimResponseRequestor` (審查委員身分證號) · `RequestedService` (事前審查品項)

## Terminology
- **ValueSets: 44** — see `docs/valueset-index.md` (TODO to expand). Families: 影像/程序, 藥品,
  基因, 評估/診斷, 核定/審查, 文件/機構, 申報/部位, 適應症/續用/醫令/補充資訊/照光.
- **CodeSystems: 22** — 19 NHI (核定註記, 案件受理狀態, 用藥品項, 用藥線別, 申報類別, 申請案件類別,
  申請部位, 病人狀態評估項目, 給付適應症, 續用註記, 藥品使用頻率, 藥物類型, 補充資訊類別, 身體部位,
  醫令類別, 文件類型, 治療後疾病狀態評估項目, 照光治療種類, 特約醫事機構) + INCa 基因突變類型, NCI Thesaurus.
- **ConceptMaps: 2** — 身體部位→SNOMED CT · 醫療服務給付項目→LOINC.

## Interop / API
- **CapabilityStatements: 2** — TWPAS Server, TWPAS Client → see `docs/02-interface.md`.
- **SearchParameters: 14** — see `docs/02-interface.md`.
- **Operations:** none defined; a "預檢規則 (FHIR CQL)" pre-check artifact is referenced.

## Examples
- **65 example instances** in the IG (patients, applications, diagnoses/staging, lab/imaging,
  genetics, radiotherapy/phototherapy, responses, documents). Mirror selectively into `examples/`.
