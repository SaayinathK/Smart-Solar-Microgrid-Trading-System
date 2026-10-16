const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const context = vm.createContext({Date});
vm.runInContext(fs.readFileSync(path.join(__dirname, '../js/common/energy-reservation.js'), 'utf8') + '\nglobalThis.booking = EnergyReservationDialog;', context);
const now = Date.parse('2026-10-16T09:00:00Z');
const offer = overrides => ({energySlotId:'slot',status:'Available',availableAmount:100,pricePerUnit:25,startTime:'2026-10-16T10:00:00Z',endTime:'2026-10-16T12:00:00Z',...overrides});
const issue = value => context.booking.bookingIssue(value, now);

test('Both available and partially reserved future offers can be booked', () => {
  assert.equal(issue(offer()), '');
  assert.equal(issue(offer({status:'PartiallyReserved', availableAmount:0.1, pricePerUnit:0})), '');
});

test('Booking follows the API future-start and seven-day window rules', () => {
  assert.match(issue(offer({startTime:new Date(now).toISOString()})), /already started/);
  assert.match(issue(offer({startTime:'2026-10-24T10:00:00Z',endTime:'2026-10-24T12:00:00Z'})), /seven days/);
  assert.equal(issue(offer({startTime:'2026-10-23T09:00:00Z',endTime:'2026-10-23T12:00:00Z'})), '');
});

test('Sold-out, closed and invalid offers cannot enable confirmation', () => {
  for (const status of ['Cancelled','Expired','FullyReserved']) assert.match(issue(offer({status})), /no longer available/);
  for (const availableAmount of [0,-5,0.05,NaN,Infinity]) assert.match(issue(offer({availableAmount})), /minimum/);
  for (const pricePerUnit of [null,-1,NaN,Infinity]) assert.match(issue(offer({pricePerUnit})), /price/);
});

test('Missing and invalid delivery windows cannot be submitted', () => {
  assert.match(issue(null), /identified/);
  assert.match(issue(offer({startTime:null})), /valid delivery window/);
  assert.match(issue(offer({startTime:'invalid'})), /valid delivery window/);
  assert.match(issue(offer({endTime:'2026-10-16T09:30:00Z'})), /valid delivery window/);
});
