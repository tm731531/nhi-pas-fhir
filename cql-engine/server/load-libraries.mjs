// PUT the official rule Library resources (../rules/Library-*.json) into the CQF server.
// These are the official NHI CQL libraries kept to their text/cql content; HAPI-CR translates the
// CQL itself. (Our earlier ELM-only wrap did NOT work: HAPI-CR $evaluate loads source from CQL
// text, not raw application/elm+json — it errored "Could not load source ... version null".)
//
// Repeatable: re-run after re-syncing ../rules to reload the rules — Tom's "re-run, don't rebuild"
// invariant. The rules live here, not in C#. Usage: node load-libraries.mjs [baseUrl]
import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const rulesDir = join(here, '..', 'rules');
const base = process.argv[2] || 'http://localhost:8095/fhir';

const files = readdirSync(rulesDir).filter(f => f.startsWith('Library-') && f.endsWith('.json'));
for (const f of files) {
  const lib = JSON.parse(readFileSync(join(rulesDir, f), 'utf8'));
  const res = await fetch(`${base}/Library/${lib.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/fhir+json' },
    body: JSON.stringify(lib),
  });
  console.log(`  ${res.ok ? 'OK ' : 'ERR'} PUT Library/${lib.id} v${lib.version} -> HTTP ${res.status}`);
  if (!res.ok) console.log('     ', (await res.text()).slice(0, 300));
}
console.log(`done (${files.length} libraries).`);
