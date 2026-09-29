// Wrap each raw ELM (../elm/*.json) as a FHIR Library resource and PUT it to the CQF server.
// Repeatable: run again after re-syncing ../elm to reload the rules (Tom's "re-run, don't rebuild"
// invariant). Usage: node load-libraries.mjs [baseUrl]  (default http://localhost:8095/fhir)
import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const elmDir = join(here, '..', 'elm');
const base = process.argv[2] || 'http://localhost:8095/fhir';

const files = readdirSync(elmDir).filter(f => f.endsWith('.json'));
for (const f of files) {
  const elm = JSON.parse(readFileSync(join(elmDir, f), 'utf8'));
  const id = elm.library.identifier.id;
  const version = elm.library.identifier.version;
  const b64 = Buffer.from(JSON.stringify(elm), 'utf8').toString('base64');
  // The stored Library.url MUST equal the canonical that OTHER libraries use to include it,
  // or HAPI-CR can't resolve the dependency ("could not load source ... version null").
  // FHIRHelpers is included via the HL7 path; the NHI libraries via <system>/<id>.
  // (Same include-path-vs-identity mismatch we bridged in the JS runner's LenientRepository.)
  const url = id === 'FHIRHelpers'
    ? 'http://hl7.org/fhir/FHIRHelpers'
    : `${elm.library.identifier.system}/${id}`;
  const lib = {
    resourceType: 'Library',
    id,
    url,
    version,
    name: id,
    status: 'active',
    type: { coding: [{ system: 'http://terminology.hl7.org/CodeSystem/library-type', code: 'logic-library' }] },
    content: [{ contentType: 'application/elm+json', data: b64 }],
  };
  const res = await fetch(`${base}/Library/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/fhir+json' },
    body: JSON.stringify(lib),
  });
  console.log(`  ${res.ok ? 'OK ' : 'ERR'} PUT Library/${id} v${version} -> HTTP ${res.status}`);
  if (!res.ok) console.log('     ', (await res.text()).slice(0, 300));
}
console.log('done.');
