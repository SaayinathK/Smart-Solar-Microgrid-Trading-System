const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const context = vm.createContext({Intl, Date});
vm.runInContext(fs.readFileSync(path.join(__dirname, '../js/common/energy-visuals.js'), 'utf8') + '\nglobalThis.visuals = EnergyVisuals;', context);
const V = context.visuals;
const slot = (start, end, availableAmount) => ({startTime: `2026-10-16T${start}:00Z`, endTime: `2026-10-16T${end}:00Z`, availableAmount});

test('Missing readings stay unknown, zero is retained, and zero capacity has no percentage', () => {
  for (const value of [null, undefined, '', '   ', false, -1, NaN, Infinity]) assert.equal(V.number(value), null);
  assert.equal(V.number('0'), 0);
  assert.equal(V.percent(0, 100), 0);
  assert.equal(V.percent(0, 0), null);
  assert.equal(V.percent(null, 100), null);
  assert.equal(V.sum([{value: 12}, {}], 'value'), null);
  assert.equal(V.sum([{value: 12}, {value: 0}], 'value'), 12);
});

test('Delivery graph adds overlapping windows and releases supply at their actual end', () => {
  const result = V.scheduleSeries([slot('08:00', '10:00', 40), slot('09:00', '11:00', 25)]);
  assert.equal(result.peak, 65);
  assert.deepEqual(Array.from(result.points, point => point.value), [0, 40, 40, 65, 65, 25, 25, 0]);
  assert.equal(result.validCount, 2);
});

test('Adjacent windows do not double-count their shared boundary', () => {
  const result = V.scheduleSeries([slot('08:00', '09:00', 40), slot('09:00', '10:00', 25)]);
  assert.equal(result.peak, 40);
  assert.deepEqual(Array.from(result.points, point => point.value), [0, 40, 40, 25, 25, 0]);
});

test('Invalid delivery windows and missing energy are excluded with coverage reported', () => {
  const result = V.scheduleSeries([slot('08:00', '10:00', 10), slot('10:00', '08:00', 5), slot('08:00', '08:00', 5), slot('08:00', '09:00', null), {startTime: 'invalid', endTime: 'invalid', availableAmount: 5}]);
  assert.equal(result.peak, 10);
  assert.equal(result.validCount, 1);
  assert.equal(result.excludedCount, 4);
  assert.match(V.scheduleChart([]), /No delivery windows to chart/);
  assert.doesNotMatch(V.scheduleChart([slot('08:00', '09:00', 0)]), /NaN|Infinity/);
});

test('Gauge keeps actual reported percentages and clamps only its visual arc', () => {
  const markup = V.gauge(125, 'Charge');
  assert.match(markup, /125%/);
  assert.match(markup, /stroke-dashoffset="0"/);
  assert.match(V.gauge(null), /not available/);
  assert.doesNotMatch(V.gauge(null), />0%/);
});

test('Chart and gauge labels escape markup and generated fills have unique IDs', () => {
  const chart = V.comparisonChart([{name: '<img src=x onerror=alert(1)>', total: 10, values: [5]}], [{label: 'Stored', color: '#10b981'}], 'Storage');
  assert.doesNotMatch(chart, /<img/);
  assert.match(chart, /&lt;img/);
  assert.doesNotMatch(V.gauge(20, '<script>'), /<script>/);
  const a = V.scheduleChart([slot('08:00', '09:00', 20)]).match(/id="(energy-area-\d+)"/)[1];
  const b = V.scheduleChart([slot('08:00', '09:00', 20)]).match(/id="(energy-area-\d+)"/)[1];
  assert.notEqual(a, b);
});

test('Compact comparison retains actual values and proportional storage segments', () => {
  const chart = V.comparisonChart([{name: 'Solar Hub', total: 200, values: [150, 50]}], [{label: 'Stored', color: '#10b981'}, {label: 'Headroom', color: '#bfdbfe'}], 'Battery comparison', {compact: true});
  assert.match(chart, /Solar Hub; total 200 kWh; Stored 150 kWh; Headroom 50 kWh/);
  assert.match(chart, /width:75%;/);
  assert.match(chart, /width:25%;/);
  assert.doesNotMatch(chart, /<svg|NaN|Infinity/);
});

test('Compact comparison limits the largest systems, escapes names, and handles zero readings', () => {
  const rows = Array.from({length: 7}, (_, i) => ({name: `Grid ${i}`, total: i * 100, values: [i * 100]}));
  const chart = V.comparisonChart(rows, [{label: 'Available', color: '#10b981'}], 'Capacity', {compact: true});
  assert.equal((chart.match(/class="energy-comparison-row"/g) || []).length, 6);
  assert.match(chart, /Largest 6 of 7 microgrids/);
  assert.ok(chart.indexOf('Grid 6') < chart.indexOf('Grid 1'));
  const zero = V.comparisonChart([{name: '<img src=x>', total: 0, values: [0]}], [{label: 'Stored', color: '#10b981'}], 'Storage', {compact: true});
  assert.doesNotMatch(zero, /<img|NaN|Infinity/);
  assert.match(zero, /&lt;img/);
  assert.match(zero, /width:0%/);
});
