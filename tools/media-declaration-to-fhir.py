#!/usr/bin/env python3
"""Reference converter: NHI medical-expense media-declaration (媒體/XML 申報) -> FHIR R4.

Parses a 門診 declaration record (t/d/p segments, keyed by the OFFICIAL field IDs from the spec,
版更 112.08.25) and emits a FHIR *collection* Bundle: Patient + Encounter + Condition(s) + Claim
(use=claim) + one MedicationRequest/Procedure per 醫令 line.

Faithful-or-TODO: only fields verified against the authoritative spec are mapped; code tables not yet
transcribed (案件分類/科別/給付類別...) are passed through as raw codes with a TODO, never invented.
The FHIR target is base R4 + TW Core (there is NO published 健保費用申報 FHIR IG). See
spec/docs/media-declaration-to-fhir.md for the full field-by-field mapping and honest scope.

Usage: python3 tools/media-declaration-to-fhir.py tools/sample-media-declaration.txt > claim.json
Then:  tools/validate.sh claim.json   |   tools/post-to-public-server.sh claim.json
"""
import json
import sys

NHI_ID = "https://nhicore.nhi.gov.tw"  # placeholder identifier namespaces (TODO: confirm official system URLs)


def parse(path):
    """Read the segment file -> {'t': {...}, 'd': {...}, 'p': [ {...}, ... ]}."""
    rec = {"t": {}, "d": {}, "p": []}
    for line in open(path, encoding="utf-8"):
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        seg, *pairs = line.split("|")
        fields = dict(p.split("=", 1) for p in pairs if "=" in p)
        if seg == "p":
            rec["p"].append(fields)
        else:
            rec[seg].update(fields)
    return rec


def roc_date(v):
    """ROC date 'YYYMMDD' or 'YYYMM' (民國, zero-padded) -> ISO. 西元 = 民國 + 1911. Per spec §6."""
    if not v or not v.isdigit():
        return None
    year = int(v[:3]) + 1911
    if len(v) >= 7:
        return f"{year:04d}-{v[3:5]}-{v[5:7]}"
    if len(v) >= 5:
        return f"{year:04d}-{v[3:5]}"
    return f"{year:04d}"


def entry(resource):
    ref = f"{resource['resourceType']}/{resource['id']}"
    return {"fullUrl": ref, "resource": resource}, ref


def convert(rec):
    d, t = rec["d"], rec["t"]
    entries, refs = [], {}

    # t2 服務機構代號 -> filing Organization
    org, refs["org"] = entry({
        "resourceType": "Organization", "id": "org-filer",
        "identifier": [{"system": f"{NHI_ID}/hospital-id", "value": t.get("t2", "")}],
    })
    entries.append(org)

    # d3 身分證統一編號 / d11 出生年月日 -> Patient
    patient = {
        "resourceType": "Patient", "id": "pat-1",
        "identifier": [{"system": f"{NHI_ID}/national-id", "value": d.get("d3", "")}],
    }
    if roc_date(d.get("d11")):
        patient["birthDate"] = roc_date(d["d11"])
    p_entry, refs["pat"] = entry(patient)
    entries.append(p_entry)

    # d30 診治醫事人員代號 -> Practitioner
    prac, refs["doc"] = entry({
        "resourceType": "Practitioner", "id": "pra-1",
        "identifier": [{"system": f"{NHI_ID}/practitioner-id", "value": d.get("d30", "")}],
    })
    entries.append(prac)

    # 健保 payer + Coverage — base R4 Claim requires insurance[1..*]. (d14 給付類別 -> Coverage.type: TODO.)
    nhi, refs["nhi"] = entry({
        "resourceType": "Organization", "id": "org-nhi",
        "identifier": [{"system": f"{NHI_ID}/payer", "value": "NHI"}],
    })
    entries.append(nhi)
    cov, refs["cov"] = entry({
        "resourceType": "Coverage", "id": "cov-1", "status": "active",
        "beneficiary": {"reference": refs["pat"]}, "payor": [{"reference": refs["nhi"]}],
    })
    entries.append(cov)

    # d9/d10 就醫/治療結束日期, d8 科別, d29 就醫序號 -> Encounter
    enc = {
        "resourceType": "Encounter", "id": "enc-1", "status": "finished",
        "class": {"system": "http://terminology.hl7.org/CodeSystem/v3-ActCode", "code": "AMB"},
        "subject": {"reference": refs["pat"]},
        "period": {k: roc_date(d.get(v)) for k, v in (("start", "d9"), ("end", "d10")) if roc_date(d.get(v))},
    }
    if d.get("d29"):
        enc["identifier"] = [{"system": f"{NHI_ID}/ic-card-seq", "value": d["d29"]}]
    # d8 就醫科別 -> serviceType (TODO: 科別 code table, 註13)
    enc_entry, refs["enc"] = entry(enc)
    entries.append(enc_entry)

    # d19 主診斷 + d20-d23 次診斷 -> Condition[] + Claim.diagnosis[]
    diagnoses = []
    for i, fid in enumerate(("d19", "d20", "d21", "d22", "d23")):
        icd = d.get(fid)
        if not icd:
            continue
        cid = f"cond-{i}"
        cond, cref = entry({
            "resourceType": "Condition", "id": cid,
            # ICD-10-CM per spec (小數點免填). TODO: confirm official CodeSystem canonical for 健保 ICD.
            "code": {"coding": [{"system": "http://hl7.org/fhir/sid/icd-10-cm", "code": icd}]},
            "subject": {"reference": refs["pat"]},
        })
        entries.append(cond)
        diagnoses.append({
            "sequence": i + 1,
            "diagnosisReference": {"reference": cref},
            "type": [{"coding": [{
                "system": "http://terminology.hl7.org/CodeSystem/ex-diagnosistype",
                "code": "principal" if fid == "d19" else "secondary"}]}],
        })

    # 醫令清單段 p -> Claim.item[] (+ MedicationRequest for 用藥, Procedure for 診療)
    items = []
    for p in rec["p"]:
        seq = int(p.get("p13", len(items) + 1))
        cls = p.get("p3", "")  # 醫令類別: 0診察費 1用藥 2診療 3特材 4不另計價 9藥事服務費
        item = {
            "sequence": seq,
            # p4 藥品(項目)代號 = 健保藥品/支付標準碼. TODO: confirm official CodeSystem canonical.
            "productOrService": {"coding": [{"system": f"{NHI_ID}/nhi-code", "code": p.get("p4", "")}]},
        }
        if p.get("p10"):
            item["quantity"] = {"value": int(p["p10"])}
        if p.get("p11"):
            item["unitPrice"] = {"value": float(p["p11"])}  # 點值
        if p.get("p12"):
            item["net"] = {"value": float(p["p12"])}  # 點數
        items.append(item)

        if cls == "1":  # 用藥明細 -> MedicationRequest
            mr = {
                "resourceType": "MedicationRequest", "id": f"med-{seq}", "status": "completed",
                "intent": "order",
                "medicationCodeableConcept": {"coding": [{"system": f"{NHI_ID}/nhi-drug-code", "code": p.get("p4", "")}]},
                "subject": {"reference": refs["pat"]},
            }
            # p5 用量 / p7 頻率 / p9 途徑 = 健保藥品使用標準碼 (TODO: map to Dosage codings)
            dosage = {}
            if p.get("p7"):
                dosage["text"] = f"freq={p['p7']} route={p.get('p9','')} dose={p.get('p5','')}"
            if dosage:
                mr["dosageInstruction"] = [dosage]
            entries.append(entry(mr)[0])
        elif cls == "2":  # 診療明細 -> Procedure
            proc = {
                "resourceType": "Procedure", "id": f"proc-{seq}", "status": "completed",
                "code": {"coding": [{"system": f"{NHI_ID}/nhi-code", "code": p.get("p4", "")}]},
                "subject": {"reference": refs["pat"]},
            }
            entries.append(entry(proc)[0])
        # cls 0/3/4/9 -> Claim.item only (TODO: 特材->Device, 診察/藥事服務費 lines)

    # d -> Claim (use=claim; base R4 — no billing IG to bind to)
    claim = {
        "resourceType": "Claim", "id": "cla-1", "status": "active",
        "use": "claim",
        "type": {"coding": [{"system": "http://terminology.hl7.org/CodeSystem/claim-type", "code": "institutional"}]},
        "patient": {"reference": refs["pat"]},
        "provider": {"reference": refs["org"]},
        "priority": {"coding": [{"system": "http://terminology.hl7.org/CodeSystem/processpriority", "code": "normal"}]},
        "created": roc_date(t.get("t6")) or roc_date(d.get("d9")),
        "careTeam": [{"sequence": 1, "provider": {"reference": refs["doc"]}}],
        "insurance": [{"sequence": 1, "focal": True, "coverage": {"reference": refs["cov"]}}],
        "diagnosis": diagnoses,
        "item": items,
        # d1 案件分類 -> subType (TODO: 案件分類 code table, 註11/註19)
        "subType": {"coding": [{"system": f"{NHI_ID}/case-category", "code": d.get("d1", "")}]},
        "identifier": [{"system": f"{NHI_ID}/claim-seq", "value": d.get("d2", "")}],
    }
    entries.append(entry(claim)[0])

    return {"resourceType": "Bundle", "type": "collection", "entry": entries}


if __name__ == "__main__":
    src = sys.argv[1] if len(sys.argv) > 1 else "tools/sample-media-declaration.txt"
    print(json.dumps(convert(parse(src)), indent=2, ensure_ascii=False))
