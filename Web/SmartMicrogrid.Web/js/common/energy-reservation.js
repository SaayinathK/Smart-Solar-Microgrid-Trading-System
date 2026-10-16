/* Direct booking from an availability offer. The API enforces identity and capacity. */
class EnergyReservationDialog {
  static bookingIssue(slot, now = Date.now()) {
    if (!slot || !(slot.energySlotId || slot.id)) return 'This energy offer could not be identified. Refresh availability and try again.';
    if (!['Available', 'PartiallyReserved'].includes(slot.status)) return 'This energy offer is no longer available for reservation.';
    if (!Number.isFinite(Number(slot.availableAmount)) || Number(slot.availableAmount) < 0.1) return 'This offer has less than the minimum 0.1 kWh available. Please choose another offer.';
    if (slot.pricePerUnit == null || !Number.isFinite(Number(slot.pricePerUnit)) || Number(slot.pricePerUnit) < 0) return 'The unit price is unavailable. Refresh availability and try again.';
    const start = new Date(slot.startTime).getTime(), end = new Date(slot.endTime).getTime();
    if (!slot.startTime || !slot.endTime || !Number.isFinite(start) || !Number.isFinite(end) || end <= start) return 'This offer has no valid delivery window. Please choose another offer.';
    if (start <= now) return 'This delivery window has already started. Please choose an offer that starts in the future.';
    if (start > now + 7 * 86400000) return 'This offer starts more than seven days from now. Please return when it is within the booking window.';
    return '';
  }

  constructor(onReserved) {
    this.onReserved = onReserved;
    this.saving = false;
    this.version = 0;
    this.dialog = document.createElement('dialog');
    this.dialog.id = 'reservation-dialog';
    this.dialog.className = 'reservation-dialog offer-booking-dialog';
    this.dialog.setAttribute('aria-labelledby', 'offer-booking-title');
    this.dialog.setAttribute('aria-describedby', 'reservation-dialog-description');
    this.dialog.innerHTML = `
      <form id="offer-booking-form">
        <header class="dialog-heading"><div><span class="reservation-eyebrow">ENERGY TRADING</span><h2 id="offer-booking-title">Reserve clean energy</h2><p id="reservation-dialog-description">Confirm the selected offer and enter the energy you need.</p></div><button class="dialog-close" type="button" data-close-booking aria-label="Close reservation">&times;</button></header>
        <div class="reservation-dialog-body">
          <section class="booking-offer-summary" aria-label="Selected energy offer"><span class="booking-eyebrow">SELECTED MICROGRID</span><h3 id="booking-microgrid"></h3><p id="booking-location"></p><div class="booking-supply-details"><div><span>Available energy</span><strong id="booking-available"></strong></div><div><span>Unit price</span><strong id="booking-price"></strong></div></div><div class="booking-delivery"><span>Delivery window</span><strong id="booking-window"></strong></div></section>
          <label id="booking-nic-field" for="booking-nic">Prosumer NIC<input id="booking-nic" name="prosumerId" class="form-control" maxlength="12" pattern="(?:[0-9]{9}[VvXx]|[0-9]{12})" placeholder="Registered prosumer NIC" aria-describedby="booking-nic-help"><small id="booking-nic-help" class="reservation-field-help">Enter the NIC registered to the prosumer's account.</small></label>
          <div class="reservation-amount-row"><label for="booking-amount">Energy amount (kWh)<input id="booking-amount" class="form-control" type="number" min="0.1" step="0.1" required placeholder="e.g. 10.0" aria-describedby="booking-amount-help"><small id="booking-amount-help" class="reservation-field-help"></small></label><label for="booking-total">Estimated total<output id="booking-total" class="form-control booking-estimate" for="booking-amount" aria-live="polite">LKR 0.00</output></label></div>
          <div class="dialog-note"><strong>Booking window</strong><p>Delivery must start in the future and within seven days. Requests require approval. Changes and cancellations need at least 12 hours' notice before delivery.</p></div>
          <p id="booking-error" class="reservation-form-error" role="alert" tabindex="-1" hidden></p>
        </div>
        <footer class="dialog-actions"><button type="button" class="btn btn-secondary" data-close-booking>Cancel</button><button type="submit" id="booking-submit" class="btn btn-primary" disabled>Confirm reservation</button></footer>
      </form>`;
    document.body.append(this.dialog);
    this.form = this.dialog.querySelector('form');
    this.body = this.dialog.querySelector('.reservation-dialog-body');
    this.amount = this.dialog.querySelector('#booking-amount');
    this.nic = this.dialog.querySelector('#booking-nic');
    this.error = this.dialog.querySelector('#booking-error');
    this.submit = this.dialog.querySelector('#booking-submit');
    this.dialog.querySelectorAll('[data-close-booking]').forEach(button => button.addEventListener('click', () => { if (!this.saving) this.dialog.close(); }));
    this.dialog.addEventListener('cancel', event => { if (this.saving) event.preventDefault(); });
    this.dialog.addEventListener('close', () => { ++this.version; if (this.opener?.isConnected) this.opener.focus({preventScroll:true}); });
    this.amount.addEventListener('input', () => this.updateTotal());
    this.form.addEventListener('submit', event => this.reserve(event));
  }

  showError(message) { this.error.textContent = message; this.error.hidden = false; }
  updateTotal() {
    const total = Math.max(0, Number(this.amount.value) || 0) * Number(this.selected?.pricePerUnit || 0);
    this.dialog.querySelector('#booking-total').textContent = `LKR ${total.toLocaleString(undefined, {minimumFractionDigits:2, maximumFractionDigits:2})}`;
  }
  showOffer(slot) {
    const V = EnergyVisuals;
    this.dialog.querySelector('#booking-microgrid').textContent = slot.microgridName || 'Solar microgrid';
    this.dialog.querySelector('#booking-location').textContent = slot.location || 'Location unavailable';
    this.dialog.querySelector('#booking-available').textContent = `${V.format(slot.availableAmount)} kWh`;
    this.dialog.querySelector('#booking-price').textContent = `LKR ${Number(slot.pricePerUnit || 0).toFixed(2)} / kWh`;
    this.dialog.querySelector('#booking-window').textContent = `${V.date(slot.startTime)} to ${V.date(slot.endTime)}`;
    this.amount.max = Math.min(Number(slot.availableAmount) || 0, 1000000);
    this.dialog.querySelector('#booking-amount-help').textContent = `Up to ${V.format(this.amount.max)} kWh from this offer.`;
  }
  async refresh(version) {
    const response = await MicrogridApi.getEnergySlotById(this.slotId);
    if (version !== this.version || !this.dialog.open) return;
    if (!response?.success || !response.data) throw new Error(response?.message || 'This energy offer could not be refreshed. Close the form and try again.');
    this.selected = {...this.selected, ...response.data, energySlotId:this.slotId};
    this.showOffer(this.selected);
    if (!this.amount.value) this.amount.value = Math.min(5, Math.floor(Number(this.selected.availableAmount) * 10) / 10 || 0);
    this.updateTotal();
    const issue = EnergyReservationDialog.bookingIssue(this.selected);
    this.submit.disabled = Boolean(issue);
    if (issue) this.showError(issue);
  }
  async open(slot, opener) {
    if (this.dialog.open || !SessionManager.isAuthenticated() || !['Prosumer','Admin','MicrogridOperator'].includes(SessionManager.getUserRole())) return;
    this.opener = opener;
    this.slotId = slot.energySlotId || slot.id;
    this.selected = slot;
    this.form.reset(); this.error.hidden = true;
    const staff = ['Admin','MicrogridOperator'].includes(SessionManager.getUserRole());
    this.dialog.querySelector('#booking-nic-field').hidden = !staff;
    this.nic.disabled = !staff; this.nic.required = staff;
    this.showOffer(slot); this.updateTotal();
    this.submit.disabled = true;
    this.submit.textContent = 'Checking supply…';
    this.body.inert = true;
    const version = ++this.version;
    this.dialog.showModal(); this.body.scrollTop = 0;
    try { await this.refresh(version); }
    catch (error) { if (version === this.version) this.showError(error.message || 'Unable to check this energy offer. Close and try again.'); }
    finally {
      if (version === this.version && this.dialog.open) {
        this.body.inert = false; this.submit.textContent = 'Confirm reservation';
        if (!this.error.hidden) this.error.focus(); else (staff ? this.nic : this.amount).focus();
      }
    }
  }
  async reserve(event) {
    event.preventDefault();
    if (this.saving || this.submit.disabled || !this.form.reportValidity()) return;
    const issue = EnergyReservationDialog.bookingIssue(this.selected);
    if (issue) { this.showError(issue); this.submit.disabled = true; this.error.focus(); return; }
    const payload = {energySlotId:this.slotId, energyAmount:Number(this.amount.value)};
    if (['Admin','MicrogridOperator'].includes(SessionManager.getUserRole())) payload.prosumerId = this.nic.value.trim().toUpperCase();
    this.saving = true; this.error.hidden = true; this.body.inert = true;
    this.form.setAttribute('aria-busy','true');
    this.dialog.querySelectorAll('button').forEach(button => {button.disabled = true;});
    this.submit.textContent = 'Reserving…';
    const version = this.version;
    let saved = false;
    try {
      const response = await ReservationApi.create(payload);
      if (!response?.success) throw new Error(response?.message || 'The reservation could not be placed. Please try again.');
      saved = true; this.dialog.close(); this.form.reset();
    } catch (error) {
      this.showError(error.message || 'Unable to place the reservation. Your entries have been kept. Please try again.');
      try { await this.refresh(version); } catch { /* Keep the booking error and draft if refresh also fails. */ }
    } finally {
      this.saving = false; this.body.inert = false; this.form.removeAttribute('aria-busy');
      this.dialog.querySelectorAll('[data-close-booking]').forEach(button => {button.disabled = false;});
      this.submit.disabled = Boolean(EnergyReservationDialog.bookingIssue(this.selected));
      this.submit.textContent = 'Confirm reservation';
      if (!this.error.hidden && this.dialog.open) this.error.focus();
    }
    if (saved) await this.onReserved(this.slotId);
  }
}
