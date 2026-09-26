(() => {
  const user = SessionManager.getUser();
  const role = user?.role || 'Prosumer';

  // Role customizations on UI
  const roleEyebrow = document.getElementById('hero-role-eyebrow');
  const roleTitle = document.getElementById('hero-title');
  const roleSubtitle = document.getElementById('hero-subtitle');
  const roleBadge = document.getElementById('hero-role-badge');
  const toolbarHeading = document.getElementById('toolbar-heading');
  const toolbarSubtitle = document.getElementById('toolbar-subtitle');
  const prosumerNicField = document.getElementById('prosumer-nic-field');
  const btnQuickVerify = document.getElementById('btn-quick-verify');
  const btnNewReservation = document.getElementById('new-reservation');

  if (role === 'Admin') {
    if (roleEyebrow) roleEyebrow.textContent = 'SYSTEM AUDIT & OVERSIGHT';
    if (roleTitle) roleTitle.textContent = 'All Energy Reservations';
    if (roleSubtitle) roleSubtitle.textContent = 'Network-wide monitoring, validation, and control over all microgrid energy reservations.';
    if (roleBadge) roleBadge.textContent = 'System Administrator';
    if (toolbarHeading) toolbarHeading.textContent = 'Network Reservation Registry';
    if (btnQuickVerify) btnQuickVerify.style.display = 'inline-block';
  } else if (role === 'MicrogridOperator') {
    if (roleEyebrow) roleEyebrow.textContent = 'MICROGRID OPERATIONS';
    if (roleTitle) roleTitle.textContent = 'Microgrid Reservations Queue';
    if (roleSubtitle) roleSubtitle.textContent = 'Review, approve, and manage energy reservation requests across your assigned solar hubs.';
    if (roleBadge) roleBadge.textContent = 'Microgrid Operator';
    if (toolbarHeading) toolbarHeading.textContent = 'Hub Reservation Requests';
    if (btnQuickVerify) btnQuickVerify.style.display = 'inline-block';
  } else {
    // Prosumer
    if (roleEyebrow) roleEyebrow.textContent = 'PROSUMER ENERGY TRADING';
    if (roleTitle) roleTitle.textContent = 'My Energy Reservations';
    if (roleSubtitle) roleSubtitle.textContent = 'Search available solar slots, reserve green power, and track your active delivery passes.';
    if (roleBadge) roleBadge.textContent = 'Prosumer Portal';
    if (toolbarHeading) toolbarHeading.textContent = 'My Active Claims & History';
    if (prosumerNicField) prosumerNicField.style.display = 'none';
  }

  const body = document.getElementById('reservations-body');
  const esc = value => String(value ?? '').replace(/[&<>"']/g, ch => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[ch]));
  const idOf = r => r.id || r._id || '';
  const dateOf = value => value ? new Date(value) : null;
  const validDate = d => d && !Number.isNaN(d.getTime());
  const fmt = (value, options) => { const d = dateOf(value); return validDate(d) ? d.toLocaleString([], options) : '—'; };
  const localDate = value => { const d = dateOf(value); return validDate(d) ? `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}` : ''; };
  const unwrap = response => { const data = response?.data ?? response; return Array.isArray(data) ? data : (data?.items || data?.reservations || []); };
  const statusClass = status => `status-${String(status || 'unknown').toLowerCase().replace(/[^a-z]+/g,'-')}`;

  let reservations = [];
  let availableSlots = [];
  let activePillFilter = '';

  function render(rows) {
    const term = document.getElementById('reservation-search').value.trim().toLowerCase();
    const statusSelect = document.getElementById('reservation-status').value.toLowerCase();
    const effectiveStatus = (activePillFilter || statusSelect).toLowerCase();
    const date = document.getElementById('reservation-date').value;

    const filtered = rows.filter(r => {
      const vCode = r.verificationCode || (r.id ? `SMG-RES-${r.id.slice(-6).toUpperCase()}` : '');
      const haystack = [
        r.prosumerName,
        r.prosumerId,
        r.microgridName,
        r.microgridNodeName,
        r.microgridNodeId,
        vCode,
        idOf(r)
      ].join(' ').toLowerCase();

      const matchesTerm = !term || haystack.includes(term);
      let matchesStatus = true;
      if (effectiveStatus) {
        if (effectiveStatus === 'cancelled') {
          matchesStatus = ['cancelled', 'rejected', 'expired'].includes(String(r.status).toLowerCase());
        } else {
          matchesStatus = String(r.status).toLowerCase() === effectiveStatus;
        }
      }
      const matchesDate = !date || localDate(r.startTime || r.reservationDate) === date;

      return matchesTerm && matchesStatus && matchesDate;
    });

    document.getElementById('reservation-count').textContent = `${filtered.length} reservation${filtered.length === 1 ? '' : 's'}`;
    if (!filtered.length) {
      body.innerHTML = '<tr><td colspan="8" class="reservation-message">No reservations match these filters.</td></tr>';
      return;
    }

    const isOperator = role === 'MicrogridOperator';
    const isAdmin = role === 'Admin';
    const isProsumer = role === 'Prosumer';

    body.innerHTML = filtered.map(r => {
      const statusName = String(r.status || 'Pending');
      const id = esc(idOf(r));
      const vCode = esc(r.verificationCode || `SMG-RES-${(idOf(r)).slice(-6).toUpperCase()}`);
      const price = Number(r.pricePerUnit || 0);
      const estCost = Number(r.totalEstimatedCost || (r.energyAmount * price) || 0);
      const costDisplay = estCost > 0 ? `$${estCost.toFixed(2)}` : 'Market rate';

      // Role Action Matrix
      let actionButtons = [];

      // Operator Actions (Approve / Reject)
      if ((isOperator || isAdmin) && statusName === 'Pending') {
        actionButtons.push(`<button class="btn btn-primary btn-sm" data-action="approve" data-id="${id}">Approve</button>`);
        actionButtons.push(`<button class="btn btn-danger btn-sm" data-action="reject" data-id="${id}">Reject</button>`);
      }

      // Grid Operator Actions (verify and complete delivery)
      if ((isOperator || isAdmin) && statusName === 'Approved') {
        actionButtons.push(`<button class="btn btn-success btn-sm" data-action="complete" data-id="${id}">Verify & Complete</button>`);
      }

      // Prosumer & Admin Actions (Modify / Cancel)
      const canEdit = (isProsumer || isAdmin) && (statusName === 'Pending' || statusName === 'Approved');
      if (canEdit) {
        actionButtons.push(`<button class="btn btn-secondary btn-sm" data-action="modify" data-id="${id}" data-amount="${esc(r.energyAmount)}" data-slot="${esc(r.energySlotId)}">Modify</button>`);
        actionButtons.push(`<button class="btn btn-secondary btn-sm" data-action="cancel" data-id="${id}">Cancel</button>`);
      }

      return `
        <tr>
          <td class="prosumer-cell">
            <strong>${esc(r.prosumerName || 'Prosumer')}</strong>
            <span>NIC ${esc(r.prosumerId || '—')}</span>
          </td>
          <td class="node-cell">
            <strong>${esc(r.microgridName || r.microgridNodeName || 'Microgrid Hub')}</strong>
            <small>Node ID: ${esc(r.microgridNodeId || '—')}</small>
          </td>
          <td class="energy-cell">
            <strong>${esc(r.energyAmount ?? '—')} kWh</strong>
            <span class="cost-pill">${costDisplay}</span>
          </td>
          <td class="schedule-cell">
            <strong>${esc(fmt(r.startTime || r.reservationDate, { month: 'short', day: 'numeric', year: 'numeric' }))}</strong>
            <span>${esc(fmt(r.startTime, { hour: '2-digit', minute: '2-digit' }))}${r.endTime ? ` – ${esc(fmt(r.endTime, { hour: '2-digit', minute: '2-digit' }))}` : ''}</span>
          </td>
          <td>
            <span class="verification-code-badge" data-action="pass" data-id="${id}" title="Click to view Pass">
              🏷️ ${vCode}
            </span>
          </td>
          <td class="pass-cell">
            <button class="btn btn-outline-primary btn-sm pass-button" data-action="pass" data-id="${id}" title="View Digital Pass and QR">
              View pass
            </button>
          </td>
          <td>
            <span class="status-badge ${statusClass(statusName)}">${esc(statusName)}</span>
          </td>
          <td>
            <div class="reservation-actions">
              ${actionButtons.length ? actionButtons.join(' ') : '<span class="no-actions">No action required</span>'}
            </div>
          </td>
        </tr>
      `;
    }).join('');
  }

  async function load() {
    body.innerHTML = '<tr><td colspan="8" class="reservation-message">Loading reservations…</td></tr>';
    const refresh = document.getElementById('refresh-reservations');
    if (refresh) refresh.disabled = true;

    try {
      const [listResult, summaryResult] = await Promise.allSettled([
        ReservationApi.list(),
        ReservationApi.summary()
      ]);

      if (listResult.status === 'rejected') throw listResult.reason;
      reservations = unwrap(listResult.value);
      render(reservations);

      if (summaryResult.status === 'fulfilled') {
        const s = summaryResult.value?.data || summaryResult.value || {};
        const elPending = document.getElementById('stat-pending');
        if (elPending) elPending.textContent = s.pendingCount ?? reservations.filter(r => r.status === 'Pending').length;
        
        const elApproved = document.getElementById('stat-approved');
        if (elApproved) elApproved.textContent = s.approvedFutureCount ?? reservations.filter(r => r.status === 'Approved' && new Date(r.startTime) > new Date()).length;
        
        const elCompleted = document.getElementById('stat-completed');
        if (elCompleted) elCompleted.textContent = s.completedThisMonthCount ?? reservations.filter(r => r.status === 'Completed').length;
        
        const elEnergy = document.getElementById('stat-energy');
        if (elEnergy) elEnergy.textContent = `${Number(s.totalEnergyReserved ?? reservations.filter(r => ['Pending','Approved'].includes(r.status)).reduce((n,r) => n + Number(r.energyAmount || 0),0)).toLocaleString()} kWh`;
      } else {
        const elPending = document.getElementById('stat-pending');
        if (elPending) elPending.textContent = reservations.filter(r => r.status === 'Pending').length;
        
        const elApproved = document.getElementById('stat-approved');
        if (elApproved) elApproved.textContent = reservations.filter(r => r.status === 'Approved').length;
        
        const elCompleted = document.getElementById('stat-completed');
        if (elCompleted) elCompleted.textContent = reservations.filter(r => r.status === 'Completed').length;
        
        const elEnergy = document.getElementById('stat-energy');
        if (elEnergy) elEnergy.textContent = `${reservations.filter(r => ['Pending','Approved'].includes(r.status)).reduce((n,r) => n + Number(r.energyAmount || 0),0).toLocaleString()} kWh`;
      }
    } catch (err) {
      body.innerHTML = `<tr><td colspan="7" class="reservation-message error">${esc(err?.message || 'Unable to load reservations. Check backend connection.')}</td></tr>`;
      ApiClient.showToast(err?.message || 'Could not load reservations', 'error');
    } finally {
      if (refresh) refresh.disabled = false;
    }
  }

  // Load available slots into modal picker
  async function loadAvailableSlots() {
    const container = document.getElementById('slot-picker-container');
    if (!container) return;
    container.innerHTML = '<div style="padding: 12px; text-align: center; color: var(--text-secondary);">Loading available slots…</div>';

    try {
      const response = await ApiClient.get('/energy-availability');
      const slots = unwrap(response);
      availableSlots = slots.filter(s => Number(s.availableAmount || 0) > 0);

      if (!availableSlots.length) {
        container.innerHTML = '<div style="padding: 12px; text-align: center; color: var(--text-secondary);">No slots currently available for booking. Check back shortly.</div>';
        return;
      }

      container.innerHTML = availableSlots.map((slot, index) => {
        const slotId = slot.energySlotId || slot.id || '';
        const price = Number(slot.pricePerUnit || 0);
        return `
          <div class="slot-picker-item ${index === 0 ? 'selected' : ''}" data-slot-id="${esc(slotId)}" data-price="${price}" data-max="${slot.availableAmount}">
            <div class="slot-picker-info">
              <strong>${esc(slot.microgridName || 'Solar Microgrid')}</strong>
              <small>${esc(slot.location || 'Local Grid')} · ${fmt(slot.startTime, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}</small>
            </div>
            <div class="slot-picker-meta">
              <div class="slot-price">$${price.toFixed(2)}/kWh</div>
              <div class="slot-kwh">${Number(slot.availableAmount).toFixed(1)} kWh left</div>
            </div>
          </div>
        `;
      }).join('');

      // Auto-select first slot
      if (availableSlots.length > 0) {
        selectSlot(availableSlots[0]);
      }
    } catch (e) {
      container.innerHTML = `<div style="padding: 12px; text-align: center; color: var(--text-secondary);">Could not load slots: ${esc(e.message)}</div>`;
    }
  }

  function selectSlot(slot) {
    const slotId = slot.energySlotId || slot.id || '';
    const inputSlot = document.getElementById('input-slot-id');
    if (inputSlot) inputSlot.value = slotId;

    const price = Number(slot.pricePerUnit || 0);
    const amountInput = document.getElementById('input-energy-amount');
    if (amountInput) {
      if (!amountInput.value || Number(amountInput.value) <= 0) {
        amountInput.value = Math.min(5.0, Number(slot.availableAmount || 5.0)).toFixed(1);
      }
      updateEstimatedCost(price);
    }
  }

  function updateEstimatedCost(pricePerUnit = null) {
    const amountInput = document.getElementById('input-energy-amount');
    const costInput = document.getElementById('input-estimated-cost');
    if (!amountInput || !costInput) return;

    let price = pricePerUnit;
    if (price === null) {
      const selectedItem = document.querySelector('.slot-picker-item.selected');
      price = selectedItem ? Number(selectedItem.dataset.price || 0) : 0.25;
    }
    const amount = Number(amountInput.value || 0);
    const total = Math.max(0, amount * price);
    costInput.value = `$${total.toFixed(2)} ($${price.toFixed(2)}/kWh)`;
  }

  // QR Code generator (Vector SVG)
  function renderSvgQr(text) {
    // Generates a crisp deterministic 21x21 visual QR matrix
    const size = 21;
    let hash = 0;
    for (let i = 0; i < text.length; i++) {
      hash = ((hash << 5) - hash) + text.charCodeAt(i);
      hash |= 0;
    }

    const cells = [];
    const isCorner = (r, c) => (r < 7 && c < 7) || (r < 7 && c >= 14) || (r >= 14 && c < 7);
    const isCornerPattern = (r, c) => {
      const inBox = (r <= 6 && c <= 6) || (r <= 6 && c >= 14) || (r >= 14 && c <= 6);
      if (!inBox) return false;
      const ro = r >= 14 ? r - 14 : r;
      const co = c >= 14 ? c - 14 : c;
      if (ro === 0 || ro === 6 || co === 0 || co === 6) return true;
      if (ro >= 2 && ro <= 4 && co >= 2 && co <= 4) return true;
      return false;
    };

    for (let r = 0; r < size; r++) {
      for (let c = 0; c < size; c++) {
        if (isCorner(r, c)) {
          if (isCornerPattern(r, c)) cells.push(`M${c},${r}h1v1h-1z`);
        } else {
          const bit = Math.abs((hash ^ (r * 31 + c * 17) ^ (text.charCodeAt((r + c) % text.length))) % 3);
          if (bit === 0 || (r === 6 || c === 6)) {
            cells.push(`M${c},${r}h1v1h-1z`);
          }
        }
      }
    }

    return `<svg viewBox="0 0 ${size} ${size}" shape-rendering="crispEdges" fill="#0f172a"><path d="${cells.join('')}"/></svg>`;
  }

  // Open Digital Pass Modal
  function showDigitalPass(r) {
    const dialog = document.getElementById('digital-pass-dialog');
    if (!dialog) return;

    const id = idOf(r);
    const vCode = r.verificationCode || `SMG-RES-${id.slice(-6).toUpperCase()}`;
    const price = Number(r.pricePerUnit || 0);
    const estCost = Number(r.totalEstimatedCost || (r.energyAmount * price) || 0);

    document.getElementById('pass-verification-code').textContent = vCode;
    document.getElementById('pass-qr-svg').innerHTML = renderSvgQr(vCode);
    document.getElementById('pass-prosumer-name').textContent = r.prosumerName || 'Registered Prosumer';
    document.getElementById('pass-prosumer-nic').textContent = `NIC: ${r.prosumerId || '—'}`;
    document.getElementById('pass-node-name').textContent = r.microgridName || r.microgridNodeName || 'Microgrid Hub';
    document.getElementById('pass-energy-amount').textContent = `${r.energyAmount} kWh`;
    document.getElementById('pass-total-cost').textContent = estCost > 0 ? `$${estCost.toFixed(2)}` : 'Market Rate';
    document.getElementById('pass-schedule-window').textContent = `${fmt(r.startTime, { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' })} – ${fmt(r.endTime, { hour: '2-digit', minute: '2-digit' })}`;

    const statusBadge = document.getElementById('pass-status-badge');
    statusBadge.textContent = r.status || 'Pending';
    statusBadge.className = `status-badge ${statusClass(r.status)}`;

    // Update timeline steps
    const step1 = document.getElementById('step-1');
    const step2 = document.getElementById('step-2');
    const step3 = document.getElementById('step-3');
    const step4 = document.getElementById('step-4');

    [step1, step2, step3, step4].forEach(s => s.className = 'timeline-step');
    step1.classList.add('completed');

    if (r.status === 'Pending') {
      step2.classList.add('active');
    } else if (r.status === 'Approved') {
      step2.classList.add('completed');
      step3.classList.add('completed');
    } else if (r.status === 'Completed') {
      step2.classList.add('completed');
      step3.classList.add('completed');
      step4.classList.add('completed');
    }

    dialog.showModal();
  }

  // Event Listeners for Table Actions
  body.addEventListener('click', async event => {
    const button = event.target.closest('[data-action]');
    if (!button) return;
    const { action, id, amount, slot } = button.dataset;
    if (!id) return;

    const reservation = reservations.find(r => idOf(r) === id);

    if (action === 'pass') {
      if (reservation) showDigitalPass(reservation);
      return;
    }

    if (action === 'modify') {
      const newAmountStr = prompt('Enter updated energy amount (kWh):', amount);
      if (newAmountStr === null) return;
      const parsedAmount = Number(newAmountStr);
      if (!Number.isFinite(parsedAmount) || parsedAmount <= 0) {
        ApiClient.showToast('Please enter an energy amount greater than 0 kWh.', 'error');
        return;
      }
      button.disabled = true;
      try {
        await ReservationApi.update(id, { energyAmount: parsedAmount });
        ApiClient.showToast('Reservation updated successfully.', 'success');
        await load();
      } catch (err) {
        ApiClient.showToast(err?.message || 'Unable to update reservation.', 'error');
        button.disabled = false;
      }
      return;
    }

    if (action === 'cancel') {
      if (!confirm('Are you sure you want to cancel this reservation? Capacity will be returned to the grid.')) return;
      button.disabled = true;
      try {
        await ReservationApi.cancel(id);
        ApiClient.showToast('Reservation cancelled and capacity restored.', 'success');
        await load();
      } catch (err) {
        ApiClient.showToast(err?.message || 'Could not cancel reservation.', 'error');
        button.disabled = false;
      }
      return;
    }

    if (action === 'approve') {
      button.disabled = true;
      try {
        await ReservationApi.approve(id);
        ApiClient.showToast('Reservation approved.', 'success');
        await load();
      } catch (err) {
        ApiClient.showToast(err?.message || 'Approval failed.', 'error');
        button.disabled = false;
      }
      return;
    }

    if (action === 'reject') {
      const reason = prompt('Reason for rejection (optional):');
      if (reason === null) return;
      button.disabled = true;
      try {
        await ReservationApi.reject(id, reason);
        ApiClient.showToast('Reservation rejected.', 'success');
        await load();
      } catch (err) {
        ApiClient.showToast(err?.message || 'Rejection failed.', 'error');
        button.disabled = false;
      }
      return;
    }

    if (action === 'complete') {
      if (!confirm('Confirm energy delivery and complete this transaction?')) return;
      button.disabled = true;
      try {
        await ReservationApi.complete(id);
        ApiClient.showToast('Transaction confirmed and reservation marked Completed.', 'success');
        await load();
      } catch (err) {
        ApiClient.showToast(err?.message || 'Completion failed.', 'error');
        button.disabled = false;
      }
      return;
    }
  });

  // Slot Picker Selection in Create Modal
  const slotPickerContainer = document.getElementById('slot-picker-container');
  if (slotPickerContainer) {
    slotPickerContainer.addEventListener('click', e => {
      const item = e.target.closest('.slot-picker-item');
      if (!item) return;
      document.querySelectorAll('.slot-picker-item').forEach(i => i.classList.remove('selected'));
      item.classList.add('selected');
      const slotId = item.dataset.slotId;
      document.getElementById('input-slot-id').value = slotId;
      updateEstimatedCost(Number(item.dataset.price || 0));
    });
  }

  const inputEnergyAmount = document.getElementById('input-energy-amount');
  if (inputEnergyAmount) {
    inputEnergyAmount.addEventListener('input', () => updateEstimatedCost());
  }

  // Create Reservation Button & Form
  const btnNewRes = document.getElementById('new-reservation');
  if (btnNewRes) {
    btnNewRes.addEventListener('click', async () => {
      await loadAvailableSlots();
      document.getElementById('reservation-dialog').showModal();
    });
  }

  const reservationForm = document.getElementById('reservation-form');
  if (reservationForm) {
    reservationForm.addEventListener('submit', async event => {
      event.preventDefault();
      const form = new FormData(event.currentTarget);
      const submit = document.getElementById('save-reservation');
      submit.disabled = true;

      const payload = {
        energySlotId: form.get('energySlotId')?.toString().trim(),
        energyAmount: Number(form.get('energyAmount'))
      };

      const nicVal = form.get('prosumerId')?.toString().trim();
      if (nicVal) payload.prosumerId = nicVal;

      try {
        await ReservationApi.create(payload);
        document.getElementById('reservation-dialog').close();
        event.currentTarget.reset();
        ApiClient.showToast('Reservation submitted successfully.', 'success');
        await load();
      } catch (err) {
        ApiClient.showToast(err?.message || 'Unable to create reservation.', 'error');
      } finally {
        submit.disabled = false;
      }
    });
  }

  // Quick Verify Dialog (for Verifier & Admin)
  if (btnQuickVerify) {
    btnQuickVerify.addEventListener('click', () => {
      document.getElementById('input-verify-code').value = '';
      document.getElementById('verify-preview-box').style.display = 'none';
      document.getElementById('quick-verify-dialog').showModal();
    });
  }

  const quickVerifyForm = document.getElementById('quick-verify-form');
  if (quickVerifyForm) {
    quickVerifyForm.addEventListener('submit', async event => {
      event.preventDefault();
      const rawCode = document.getElementById('input-verify-code').value.trim();
      if (!rawCode) return;

      // Find matching reservation by verification code or id
      const target = reservations.find(r => {
        const vCode = r.verificationCode || `SMG-RES-${(idOf(r)).slice(-6).toUpperCase()}`;
        return vCode.toLowerCase() === rawCode.toLowerCase() || idOf(r).toLowerCase() === rawCode.toLowerCase();
      });

      if (!target) {
        ApiClient.showToast('No reservation found matching this code or ID.', 'error');
        return;
      }

      if (target.status !== 'Approved') {
        ApiClient.showToast(`Cannot verify: Reservation status is "${target.status}". Only Approved reservations can be verified.`, 'error');
        return;
      }

      const btnSubmit = document.getElementById('btn-submit-verify');
      btnSubmit.disabled = true;
      try {
        await ReservationApi.complete(idOf(target));
        ApiClient.showToast(`Reservation verified! Delivery of ${target.energyAmount} kWh confirmed.`, 'success');
        document.getElementById('quick-verify-dialog').close();
        await load();
      } catch (err) {
        ApiClient.showToast(err?.message || 'Verification failed.', 'error');
      } finally {
        btnSubmit.disabled = false;
      }
    });
  }

  // Quick Filter Pills
  const statusPills = document.getElementById('status-pills');
  if (statusPills) {
    statusPills.addEventListener('click', e => {
      const pill = e.target.closest('.filter-pill');
      if (!pill) return;
      document.querySelectorAll('.filter-pill').forEach(p => p.classList.remove('active'));
      pill.classList.add('active');
      activePillFilter = pill.dataset.filter || '';
      render(reservations);
    });
  }

  // Search & Filters inputs
  ['reservation-search', 'reservation-status', 'reservation-date'].forEach(id => {
    const el = document.getElementById(id);
    if (el) {
      el.addEventListener(id === 'reservation-search' ? 'input' : 'change', () => render(reservations));
    }
  });

  const refreshBtn = document.getElementById('refresh-reservations');
  if (refreshBtn) refreshBtn.addEventListener('click', load);

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', load, { once: true });
  } else {
    load();
  }
})();
