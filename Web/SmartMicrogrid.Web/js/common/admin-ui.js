/* ==========================================================================
   Smart Microgrid Energy System - M4 Shared UI Helpers
   ========================================================================== */

const AdminUi = {
  /**
   * Renders a health/stat status indicator. Components that are not reachable
   * show their note so the dashboard explains itself instead of showing a bare 0.
   */
  statusBadge(value, status) {
    const safeValue = escapeHtml(String(value ?? '-'));
    if (!status || status === 'Live') {
      return `<span class="status-badge status-approved">${safeValue}</span>`;
    }
    return `<span class="status-badge status-cancelled" title="${escapeHtml(status)}">${safeValue}</span>`;
  },

  componentNote(stat) {
    if (!stat) return '';
    if (stat.status === 'Live') {
      return `<small class="text-muted">${escapeHtml(stat.note || '')}</small>`;
    }
    return `<small class="text-muted">${escapeHtml(stat.note || 'Component data is not reachable.')}</small>`;
  },

  healthPill(value) {
    const text = String(value || 'Unknown');
    const cls = /^(healthy|online|ok|live|connected)$/i.test(text) ? 'status-approved'
      : /degraded|warning/i.test(text) ? 'status-pending'
      : 'status-rejected';
    return `<span class="status-badge ${cls}">${escapeHtml(text)}</span>`;
  },

  setHealthState(element, value) {
    const card = element?.closest('.alert-item');
    if (!card) return;
    const text = String(value || 'Unknown');
    card.classList.remove('healthy', 'warning', 'critical');
    card.classList.add(/^(healthy|online|ok|live|connected)$/i.test(text)
      ? 'healthy' : /degraded|warning/i.test(text) ? 'warning' : 'critical');
  },

  formatDateTime(value) {
    if (!value) return '-';
    const date = new Date(value);
    if (isNaN(date.getTime())) return escapeHtml(String(value));
    return date.toLocaleString();
  },

  formatDate(value) {
    if (!value) return '-';
    const date = new Date(value);
    if (isNaN(date.getTime())) return escapeHtml(String(value));
    return date.toLocaleDateString();
  },

  formatNumber(value, decimals = 0) {
    const num = Number(value);
    if (!Number.isFinite(num)) return '-';
    return num.toLocaleString(undefined, {
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals
    });
  },

  emptyRow(colspan, message) {
    return `<tr>
      <td colspan="${colspan}" class="reservation-message">${escapeHtml(message)}</td>
    </tr>`;
  },

  loadingRow(colspan) {
    return `<tr>
      <td colspan="${colspan}" class="reservation-message">Loading...</td>
    </tr>`;
  },

  /** Collects filters from a container of named inputs, dropping empty values. */
  collectFilters(ids) {
    const params = {};
    ids.forEach(id => {
      const el = document.getElementById(id);
      if (!el) return;
      const value = el.value ? el.value.trim() : '';
      if (value) params[id.replace('-filter', '').replace('-from', 'From').replace('-to', 'To')] = value;
    });
    return params;
  }
};

function escapeHtml(value) {
  if (value === null || value === undefined) return '';
  return String(value)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}
