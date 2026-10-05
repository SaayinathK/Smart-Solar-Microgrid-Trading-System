/* ==========================================================================
   Smart Microgrid Energy System - M4 System Activity (Audit Log) Controller
   ========================================================================== */

const activityState = { page: 1, pageSize: 20, totalPages: 1, request: 0 };

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('Admin')) return;

  const body = document.getElementById('activity-body');
  const moduleFilter = document.getElementById('module-filter');
  const actionFilter = document.getElementById('action-filter');
  const statusFilter = document.getElementById('status-filter');
  const fromFilter = document.getElementById('from-filter');
  const toFilter = document.getElementById('to-filter');

  [moduleFilter, actionFilter, statusFilter].forEach(el =>
    el.addEventListener('change', () => { activityState.page = 1; load(); }));

  [fromFilter, toFilter].forEach(el =>
    el.addEventListener('change', () => { activityState.page = 1; load(); }));

  document.getElementById('clear-btn').addEventListener('click', () => {
    moduleFilter.value = '';
    actionFilter.value = '';
    statusFilter.value = '';
    fromFilter.value = '';
    toFilter.value = '';
    activityState.page = 1;
    load();
  });

  document.getElementById('prev-btn').addEventListener('click', () => {
    if (activityState.page > 1) { activityState.page--; load(); }
  });

  document.getElementById('next-btn').addEventListener('click', () => {
    if (activityState.page < activityState.totalPages) { activityState.page++; load(); }
  });

  load();

  async function load() {
    const request = ++activityState.request;
    body.innerHTML = AdminUi.loadingRow(8);
    document.getElementById('prev-btn').disabled = true;
    document.getElementById('next-btn').disabled = true;

    const params = { page: activityState.page, pageSize: activityState.pageSize };
    if (moduleFilter.value) params.module = moduleFilter.value;
    if (actionFilter.value) params.action = actionFilter.value;
    if (statusFilter.value) params.status = statusFilter.value;
    if (fromFilter.value) params.from = new Date(fromFilter.value).toISOString();
    if (toFilter.value) {
      // Inclusive of the whole end day.
      const to = new Date(toFilter.value);
      to.setHours(23, 59, 59, 999);
      params.to = to.toISOString();
    }

    try {
      const response = await AdminApi.getActivity(params);
      if (request !== activityState.request) return;

      if (response.success && response.data) {
        render(response.data);
      } else {
        body.innerHTML = AdminUi.emptyRow(8, response.message || 'Failed to load activity records.');
        document.getElementById('page-label').textContent = 'Activity could not be loaded.';
      }
    } catch (err) {
      if (request !== activityState.request) return;
      body.innerHTML = AdminUi.emptyRow(8, err.message || 'Network error while loading activity records.');
      document.getElementById('page-label').textContent = 'Activity could not be loaded.';
    }
  }

  function render(result) {
    activityState.page = result.page;
    activityState.totalPages = Math.max(result.totalPages, 1);

    document.getElementById('page-label').textContent =
      `Page ${result.page} of ${activityState.totalPages} — ${AdminUi.formatNumber(result.totalItems)} record(s)`;

    document.getElementById('prev-btn').disabled = result.page <= 1;
    document.getElementById('next-btn').disabled = result.page >= activityState.totalPages;

    const items = result.items || [];

    if (!items.length) {
      body.innerHTML = AdminUi.emptyRow(8, 'No audit records match the selected filters.');
      return;
    }

    body.innerHTML = items.map(item => `
      <tr>
        <td>${AdminUi.formatDateTime(item.timestamp)}</td>
        <td>${escapeHtml(item.userName || '-')}</td>
        <td>${escapeHtml(item.role || '-')}</td>
        <td>${escapeHtml(item.action)}</td>
        <td>${escapeHtml(item.module)}</td>
        <td>${escapeHtml(item.description)}</td>
        <td>${escapeHtml(item.ipAddress || '-')}</td>
        <td>${AdminUi.statusBadge(item.status)}</td>
      </tr>
    `).join('');
  }
});
