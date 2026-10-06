namespace NhiPasFhir.Core;

/// <summary>Single source of truth for the 藥碼 → CQL 規則 mapping (1:N — a drug may be gated by several
/// rules, all of which must pass). This is the index the CQL pre-check (查) uses to pick which official
/// rule(s) to run for a given drug.
///
/// **Provenance (zero fabrication).** Every 健保藥碼 below is transcribed from the official CQL IG
/// `tw.gov.mohw.nhi.cql` (v0.0.1). The faithful derivation per rule is: read the rule Library's CQL →
/// find the ATC CodeConcept define it uses to identify the *applied-for* drug (the `CurrentDrugCodes`
/// first argument of `HasConcurrentMedicationOrder`/`HasPriorMedicationUse`/… , or the drug's own
/// `LatestMedicationPlan`/`HasMedicationUse` define) → look that ATC up in `Library-BCCodeConcept`'s
/// `/*ATC Code to NHI Code*/` section → collect its `BC######`/`KC######`/… NHI codes → map each to the
/// rule id. ATCs that appear only as clinical-history or combination-partner criteria are NOT mapped.
/// This round covers **all 乳癌 (BC*), 大腸直腸癌 (CRC*) and 肝癌 (HCC*) drug rules**. CRC codes come from
/// `Library-CRCCodeConcept`, HCC codes from `Library-HCCCodeConcept`. Tumor-agnostic / shared drugs
/// (Larotrectinib L01EX12, Regorafenib L01EX05, Pembrolizumab L01FF02) carry one key with the rule ids
/// of every cancer that gates them. Every rule here has its Library vendored in cql-engine/rules/ and
/// proven to load + evaluate on CQF-Ruler.</summary>
public static class DrugRuleMap
{
    /// <summary>藥碼 → 1..N ruleId. Every rule here has its Library resource vendored in cql-engine/rules/.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Default =
        new Dictionary<string, IReadOnlyList<string>>
        {
            // 乳癌 Abemaciclib — ATC L01EF03 (CodeConcept."L01EF03"). CORRECTION: the prior seed used
            // BC27730100, which is NOT in the IG's L01EF03 define — replaced with the IG's 4 codes.
            ["BC27640100"] = new[] { "BCAbemaciclibRule1" },
            ["BC27641100"] = new[] { "BCAbemaciclibRule1" },
            ["BC27642100"] = new[] { "BCAbemaciclibRule1" },
            ["BC27643100"] = new[] { "BCAbemaciclibRule1" },
            // 乳癌 Palbociclib — ATC L01EF01 (CDK4/6 inhibitor, gated by BCCDK46Rule1 applied-drug define)
            ["BC27102100"] = new[] { "BCCDK46Rule1" },
            ["BC27103100"] = new[] { "BCCDK46Rule1" },
            ["BC27104100"] = new[] { "BCCDK46Rule1" },
            // 乳癌 Ribociclib — ATC L01EF02 (CDK4/6 inhibitor, gated by BCCDK46Rule1 applied-drug define)
            ["BC27320100"] = new[] { "BCCDK46Rule1" },
            // 乳癌 Everolimus — CodeConcept."Everolimus" = ATC L01EG02 union L04AH02
            ["B025165100"] = new[] { "BCEverolimusRule1" }, // L01EG02
            ["B025166100"] = new[] { "BCEverolimusRule1" }, // L01EG02
            ["BC25165100"] = new[] { "BCEverolimusRule1" }, // L01EG02
            ["BC25166100"] = new[] { "BCEverolimusRule1" }, // L01EG02
            ["V000020100"] = new[] { "BCEverolimusRule1" }, // L01EG02
            ["V000021100"] = new[] { "BCEverolimusRule1" }, // L01EG02
            ["VC00020100"] = new[] { "BCEverolimusRule1" }, // L01EG02
            ["VC00021100"] = new[] { "BCEverolimusRule1" }, // L01EG02
            ["B024770100"] = new[] { "BCEverolimusRule1" }, // L04AH02
            ["B024771100"] = new[] { "BCEverolimusRule1" }, // L04AH02
            ["B024772100"] = new[] { "BCEverolimusRule1" }, // L04AH02
            ["BC24770100"] = new[] { "BCEverolimusRule1" }, // L04AH02
            ["BC24771100"] = new[] { "BCEverolimusRule1" }, // L04AH02
            ["BC24772100"] = new[] { "BCEverolimusRule1" }, // L04AH02
            // Larotrectinib — ATC L01EX12. Tumor-agnostic (NTRK fusion): the SAME 4 NHI codes appear in
            // BC/CRC/HCC CodeConcept."L01EX12" (identical set) and are gated by the Larotrectinib rule of
            // each cancer. A 健保碼 maps to 1..N rules, so these keys carry all three rule ids.
            ["BC27746148"] = new[] { "BCLarotrectinibRule1", "CRCLarotrectinibRule1", "HCCLarotrectinibRule1" },
            ["BC27746155"] = new[] { "BCLarotrectinibRule1", "CRCLarotrectinibRule1", "HCCLarotrectinibRule1" },
            ["BC27747100"] = new[] { "BCLarotrectinibRule1", "CRCLarotrectinibRule1", "HCCLarotrectinibRule1" },
            ["BC27748100"] = new[] { "BCLarotrectinibRule1", "CRCLarotrectinibRule1", "HCCLarotrectinibRule1" },
            // 乳癌 Olaparib — ATC L01XK01 (CodeConcept."L01XK01"); gated by both Olaparib rules
            ["BC27445100"] = new[] { "BCOlaparibRule1", "BCOlaparibRule2" },
            ["BC27446100"] = new[] { "BCOlaparibRule1", "BCOlaparibRule2" },
            // Pembrolizumab — ATC L01FF02. The single NHI code KC01025219 is the IG's only L01FF02 code
            // and appears in both BC and CRC CodeConcept."L01FF02" (identical), gated by each cancer's
            // Pembrolizumab rule → both rule ids. (HCC has no Pembrolizumab rule.)
            ["KC01025219"] = new[] { "BCPembrolizumabRule", "CRCPembrolizumabRule1" },
            // 乳癌 Pertuzumab — ATC L01FD02 (CodeConcept."L01FD02"); gated by both Pertuzumab rules
            ["KC00942233"] = new[] { "BCPertuzumabRule1", "BCPertuzumabRule2" },
            // 乳癌 Pertuzumab+Trastuzumab SC fixed combo — ATC L01FY01 (CodeConcept."L01FY01")
            ["KC01172235"] = new[] { "BCPertuzumabTrastuzumabSCRule1", "BCPertuzumabTrastuzumabSCRule2" },
            ["KC01173229"] = new[] { "BCPertuzumabTrastuzumabSCRule1", "BCPertuzumabTrastuzumabSCRule2" },
            // 乳癌 Sacituzumab govitecan — ATC L01FX17 (CodeConcept."L01FX17")
            ["KC01206262"] = new[] { "BCSacituzumabGovitecanRule" },
            // 乳癌 Talazoparib — ATC L01XK04 (CodeConcept."L01XK04")
            ["BC27801100"] = new[] { "BCTalazoparibRule1" },
            ["BC27802100"] = new[] { "BCTalazoparibRule1" },
            // 乳癌 Trastuzumab deruxtecan — ATC L01FD04 (CodeConcept."L01FD04"); gated by both T-DXd rules
            ["KC01179255"] = new[] { "BCTrastuzumabDeruxtecanRule1", "BCTrastuzumabDeruxtecanRule2" },
            // 乳癌 Trastuzumab emtansine — ATC L01FD03 (CodeConcept."L01FD03"); gated by both T-DM1 rules
            ["KC00949255"] = new[] { "BCTrastuzumabEmtansineRule1", "BCTrastuzumabEmtansineRule2" },
            ["KC009492AX"] = new[] { "BCTrastuzumabEmtansineRule1", "BCTrastuzumabEmtansineRule2" },
            // 乳癌 Trastuzumab — ATC L01FD01 (CodeConcept."L01FD01"); gated by all three Trastuzumab rules
            ["K0006252B5"] = new[] { "BCTrastuzumabRule1", "BCTrastuzumabRule2", "BCTrastuzumabRule3" },
            ["K000790261"] = new[] { "BCTrastuzumabRule1", "BCTrastuzumabRule2", "BCTrastuzumabRule3" },
            ["K0009612B5"] = new[] { "BCTrastuzumabRule1", "BCTrastuzumabRule2", "BCTrastuzumabRule3" },
            ["JC00154261"] = new[] { "BCTrastuzumabRule1", "BCTrastuzumabRule2", "BCTrastuzumabRule3" },
            ["KC01065221"] = new[] { "BCTrastuzumabRule1", "BCTrastuzumabRule2", "BCTrastuzumabRule3" },
            ["KC006252B5"] = new[] { "BCTrastuzumabRule1", "BCTrastuzumabRule2", "BCTrastuzumabRule3" },
            ["KC009612B5"] = new[] { "BCTrastuzumabRule1", "BCTrastuzumabRule2", "BCTrastuzumabRule3" },
            ["KC010892B5"] = new[] { "BCTrastuzumabRule1", "BCTrastuzumabRule2", "BCTrastuzumabRule3" },
            ["KC011112DE"] = new[] { "BCTrastuzumabRule1", "BCTrastuzumabRule2", "BCTrastuzumabRule3" },
            ["KC011162B5"] = new[] { "BCTrastuzumabRule1", "BCTrastuzumabRule2", "BCTrastuzumabRule3" },
            ["KC011362B5"] = new[] { "BCTrastuzumabRule1", "BCTrastuzumabRule2", "BCTrastuzumabRule3" },
            ["KC011592DE"] = new[] { "BCTrastuzumabRule1", "BCTrastuzumabRule2", "BCTrastuzumabRule3" },

            // ========================= 大腸直腸癌 CRC — codes from Library-CRCCodeConcept =========================
            // CRC Bevacizumab — ATC L01FG01. Rule1 applies to CodeConcept."L01FG01" (12 codes); Rule2 applies
            // to CodeConcept."L01FG01exceptKC01146219" (same set minus KC01146219). So KC01146219 → Rule1 only;
            // the other 11 codes → both rules. (L01FG01 is the applied-drug define: HasMedicationPlanRecord/
            // Timing/DurationWithin + IsMedicationPlan("本次申請紀錄", L01FG01).)
            ["KC01146219"] = new[] { "CRCBevacizumabRule1" },
            ["K000807219"] = new[] { "CRCBevacizumabRule1", "CRCBevacizumabRule2" },
            ["K000874219"] = new[] { "CRCBevacizumabRule1", "CRCBevacizumabRule2" },
            ["KC00807219"] = new[] { "CRCBevacizumabRule1", "CRCBevacizumabRule2" },
            ["KC00874219"] = new[] { "CRCBevacizumabRule1", "CRCBevacizumabRule2" },
            ["KC01117219"] = new[] { "CRCBevacizumabRule1", "CRCBevacizumabRule2" },
            ["KC01156219"] = new[] { "CRCBevacizumabRule1", "CRCBevacizumabRule2" },
            ["KC01185219"] = new[] { "CRCBevacizumabRule1", "CRCBevacizumabRule2" },
            ["KC01193219"] = new[] { "CRCBevacizumabRule1", "CRCBevacizumabRule2" },
            ["KC01193236"] = new[] { "CRCBevacizumabRule1", "CRCBevacizumabRule2" },
            ["KC01245219"] = new[] { "CRCBevacizumabRule1", "CRCBevacizumabRule2" },
            ["KC01245236"] = new[] { "CRCBevacizumabRule1", "CRCBevacizumabRule2" },
            // CRC Cetuximab — ATC L01FE01 (applied-drug define in both rules); gated by both Cetuximab rules.
            ["K000819248"] = new[] { "CRCCetuximabRule1", "CRCCetuximabRule2" },
            ["K000877238"] = new[] { "CRCCetuximabRule1", "CRCCetuximabRule2" },
            ["KC00877238"] = new[] { "CRCCetuximabRule1", "CRCCetuximabRule2" },
            // CRC Panitumumab — ATC L01FE02 (CodeConcept."L01FE02")
            ["KC00941221"] = new[] { "CRCPanitumumabRule1" },
            // CRC Trifluridine/Tipiracil — ATC L01BC59 (CodeConcept."L01BC59")
            ["BC27281100"] = new[] { "CRCTrifluridineTipiracilRule1" },
            ["BC27282100"] = new[] { "CRCTrifluridineTipiracilRule1" },
            // CRC Regorafenib — ATC L01EX05. The single code BC26168100 is the IG's only L01EX05 code and
            // appears in both CRC and HCC CodeConcept."L01EX05" (identical), gated by each cancer's
            // Regorafenib rule → both rule ids.
            ["BC26168100"] = new[] { "CRCRegorafenibRule1", "HCCRegorafenibRule1" },
            // (CRC Larotrectinib L01EX12 + CRC Pembrolizumab L01FF02 share codes with BC — see the merged
            //  BC27746148/55/BC27747100/BC27748100 and KC01025219 entries above.)

            // ============================ 肝癌 HCC — codes from Library-HCCCodeConcept ============================
            // HCC Atezolizumab/Durvalumab — rule applies to EITHER applied drug: ATC L01FF05 (atezolizumab:
            // KC01050238, KC01258235) or L01FF03 (durvalumab: KC01088229). LatestMedicationPlan(L01FF05) OR
            // LatestMedicationPlan(L01FF03) + HasMedicationPlanRecord/Timing on both.
            ["KC01050238"] = new[] { "HCCAtezoDurvaRule1" }, // L01FF05 atezolizumab
            ["KC01258235"] = new[] { "HCCAtezoDurvaRule1" }, // L01FF05 atezolizumab
            ["KC01088229"] = new[] { "HCCAtezoDurvaRule1" }, // L01FF03 durvalumab
            // HCC Lenvatinib — ATC L01EX08 (CodeConcept."L01EX08")
            ["BC26933100"] = new[] { "HCCLenvatinibRule1" },
            ["BC26934100"] = new[] { "HCCLenvatinibRule1" },
            // HCC Pemigatinib — ATC L01EN02 (CodeConcept."L01EN02")
            ["BC28063100"] = new[] { "HCCPemigatinibRule1" },
            ["BC28064100"] = new[] { "HCCPemigatinibRule1" },
            ["BC28065100"] = new[] { "HCCPemigatinibRule1" },
            // HCC Ramucirumab — ATC L01FG02 (CodeConcept."L01FG02")
            ["KC00999229"] = new[] { "HCCRamucirumabRule1" },
            ["KC00999248"] = new[] { "HCCRamucirumabRule1" },
            // HCC Sorafenib — ATC L01EX02 (CodeConcept."L01EX02")
            ["B024727100"] = new[] { "HCCSorafenibRule1" },
            ["BC24727100"] = new[] { "HCCSorafenibRule1" },
            ["BC28536100"] = new[] { "HCCSorafenibRule1" },
            ["BC28614100"] = new[] { "HCCSorafenibRule1" },
            ["BC28192100"] = new[] { "HCCSorafenibRule1" },
            // (HCC Regorafenib L01EX05 shares BC26168100 with CRC — see the merged entry above. HCC
            //  Larotrectinib L01EX12 shares codes with BC/CRC — see the merged BC Larotrectinib entries above.)
        };

    /// <summary>Drug codes that currently have a loaded CQL rule (the 查 step can predict 核刪/補件 for these).</summary>
    public static IReadOnlyCollection<string> CoveredDrugs => Default.Keys.ToList();
}
