const cql = require('cql-execution');
const cqlfhir = require('cql-exec-fhir');
const fs = require('fs');
const load = n => JSON.parse(fs.readFileSync(`elm/${n}.json`, 'utf8'));
(async () => {
  const lib = new cql.Library(load('BCAbemaciclibRule1'), new cql.Repository({
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
