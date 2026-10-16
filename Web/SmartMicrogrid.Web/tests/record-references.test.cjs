const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const context = vm.createContext({ document: {addEventListener() {}} });
vm.runInContext(fs.readFileSync(path.join(__dirname, '../js/common/record-references.js'), 'utf8') + '\nglobalThis.references = RecordReferences;', context);
const R = context.references;
const first = '6ac3c10b8832101424f1987c';

test('Compact labels preserve the full source ID and do not alter legacy values', () => {
  assert.equal(R.label('TX', first), 'TX-24F1987C');
  assert.equal(R.label('RES', first), 'RES-24F1987C');
  assert.equal(R.label('TX', ''), '—');
  assert.equal(R.label('TX', 'legacy-id'), 'legacy-id');
  assert.equal(R.label('TX', first.toUpperCase()), R.label('TX', first));
  assert.equal(first, '6ac3c10b8832101424f1987c');
});

test('IDs sharing an ending receive distinct labels, including duplicate rows', () => {
  const second = '111111111111111124f1987c';
  const ids = [first, second, first];
  const a = R.label('RES', first, ids), b = R.label('RES', second, ids);
  assert.notEqual(a, b);
  assert(a.length > R.label('RES', first).length);
  assert.equal(R.resolve('RES', a, ids), first);
  assert.equal(R.resolve('RES', b, ids), second);
});

test('Reference resolution accepts only one matching ID from the supplied permitted records', () => {
  assert.equal(R.resolve('RES', ' res-24f1987c ', [first]), first);
  assert.equal(R.resolve('RES', first, []), first); // Full IDs still go through API authorization.
  assert.throws(() => R.resolve('RES', 'RES-24F1987C', []), /No accessible record/);
  assert.throws(() => R.resolve('RES', 'RES-24F1987C', [first, '111111111111111124f1987c']), /multiple records/);
});
