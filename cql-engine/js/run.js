const cql = require('cql-execution');
const cqlfhir = require('cql-exec-fhir');
const fs = require('fs');
const path = require('path');
// ELM lives in the language-neutral ../elm folder (this js/ dir is just one engine binding).
const elmDir = path.join(__dirname, '..', 'elm');
const load = n => JSON.parse(fs.readFileSync(path.join(elmDir, `${n}.json`), 'utf8'));

// The NHI package is internally inconsistent about FHIRHelpers: BCReusable does
//   `include FHIRHelpers version '4.0.1'` with the HL7 path http://hl7.org/fhir/FHIRHelpers,
// but the FHIRHelpers library it ships self-identifies under system
//   https://nhicore.nhi.gov.tw/cql (so its URI is .../cql/FHIRHelpers).
// Stock cql-execution Repository.resolve only matches the full URI or the bare id, so the
// include fails to resolve -> ctx.get('FHIRHelpers') is undefined -> the ToInteger FunctionRef
// crashes on `functionDefs.filter(...)`. We bridge it at the binding layer (NOT by editing the
// official ELM): if the full path misses, retry with the trailing id segment (which resolve
// already accepts via its `path === id` branch). Bounded + unambiguous (one FHIRHelpers).
class LenientRepository extends cql.Repository {
  resolve(p, version) {
    return super.resolve(p, version) ?? super.resolve(String(p).split('/').pop(), version);
  }
}

(async () => {
  const lib = new cql.Library(load('BCAbemaciclibRule1'), new LenientRepository({
    FHIRHelpers: load('FHIRHelpers'), BCCodeConcept: load('BCCodeConcept'), BCReusable: load('BCReusable') }));
  const psource = cqlfhir.PatientSource.FHIRv401();
  psource.loadBundles([JSON.parse(fs.readFileSync(process.argv[2],'utf8'))]);
  try {
    const result = await new cql.Executor(lib).exec(psource);
    const pr = result.patientResults || {};
    const pid = Object.keys(pr)[0];
    console.log('  ✓ 執行完成,病人:', pid);
    const r = pr[pid] || {};
    console.log('  求值成功的 define 數:', Object.keys(r).length);
    for (const k of ['ICD代碼檢核_布林值','有Abemaciclib申請醫令','是否滿18歲','乳癌Abemaciclib申請結果_布林','報告總結']) {
      if (k in r) { let v=r[k]; if(typeof v==='string'&&v.length>150)v=v.slice(0,150)+'…'; console.log(`    ${k} =`, v); }
    }
  } catch(e) {
    console.log('  ✗ 執行中報錯:', e.message);
    console.log('    (在', e.libraryId||'?', '的', e.expression||'?', ')');
  }
})();
