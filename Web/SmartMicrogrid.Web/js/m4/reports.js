/* ==========================================================================
   Smart Microgrid Energy System - M4 Administrative Reports Controller
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('Admin')) return;

  const output = document.getElementById('report-output');
  const typeSelect = document.getElementById('report-type');
  const roleFilter = document.getElementById('role-filter');
  const statusFilter = document.getElementById('status-filter');

  // Role and status filters only apply to the user report.
  document.getElementById('generate-btn').addEventListener('click', generate);
  document.getElementById('csv-btn').addEventListener('click', exportCsv);
  typeSelect.addEventListener('change', applyFilterVisibility);

  applyFilterVisibility();
  generate();

  function applyFilterVisibility() {
    const isUserReport = typeSelect.value === 'users';
    roleFilter.disabled = !isUserReport;
    statusFilter.disabled = !isUserReport;
  }

  function dateRangeParams() {
    const params = {};
    const from = document.getElementById('from-date').value;
    const to = document.getElementById('to-date').value;

    if (from) params.from = new Date(from).toISOString();
    if (to) {
      const end = new Date(to);
      end.setHours(23, 59, 59, 999);
      params.to = end.toISOString();
    }
    return params;
  }

  async function generate() {
    const button = document.getElementById('generate-btn');
    button.disabled = true;
    button.textContent = 'Generating...';
    output.innerHTML = '<div class="card"><p class="text-muted" style="margin:0;">Generating report...</p></div>';

    try {
      const type = typeSelect.value;

      if (type === 'users') {
        const params = Object.assign(dateRangeParams(), {
          role: roleFilter.value,
          status: statusFilter.value
        });
        const response = await AdminApi.getUserReport(params);
        handle(response, renderUserReport);
      } else if (type === 'roles') {
        const response = await AdminApi.getRoleReport();
        handle(response, renderRoleReport);
      } else if (type === 'activity') {
        const response = await AdminApi.getActivityReport(dateRangeParams());
        handle(response, renderActivityReport);
      } else {
        const response = await AdminApi.getPlatformReport(dateRangeParams());
        handle(response, renderPlatformReport);
      }
    } catch (err) {
      output.innerHTML = card(`<p class="text-muted" style="margin:0;">${escapeHtml(err.message || 'Network error.')}</p>`);
    } finally {
      button.disabled = false;
      button.textContent = 'Generate';
    }
  }

  function handle(response, renderer) {
    if (response.success && response.data) {
      renderer(response.data);
    } else {
      output.innerHTML = card(`<p class="text-muted" style="margin:0;">${escapeHtml(response.message || 'Report generation failed.')}</p>`);
    }
  }

  function card(inner) {
    return `<div class="card">${inner}</div>`;
  }

  function statRow(stats) {
    return `<div class="stats-grid" style="grid-template-columns: repeat(auto-fit, minmax(170px, 1fr)); margin-bottom:1.5rem;">
      ${stats.map(s => `
        <div class="stat-card" style="padding:1.1rem; border-top-width:3px;">
          <div class="stat-info">
            <h3>${escapeHtml(s.label)}</h3>
            <div class="stat-value">${escapeHtml(String(s.value))}</div>
          </div>
        </div>`).join('')}
    </div>`;
  }

  function table(headers, rows) {
    if (!rows.length) return '<p class="text-muted">No rows for this report.</p>';
    return `
      <div class="table-container" style="padding:0;">
        <table class="data-table">
          <thead><tr>${headers.map(h => `<th>${escapeHtml(h)}</th>`).join('')}</tr></thead>
          <tbody>${rows.map(r => `<tr>${r.map(c => `<td>${c}</td>`).join('')}</tr>`).join('')}</tbody>
        </table>
      </div>`;
  }

  function renderUserReport(data) {
    output.innerHTML = card(`
      <h3>User Summary Report</h3>
      <p class="text-muted">Generated ${AdminUi.formatDateTime(data.generatedAt)}</p>
      ${statRow([
        { label: 'Total Users', value: data.totalUsers },
        { label: 'Active', value: data.activeUsers },
        { label: 'Inactive', value: data.inactiveUsers },
        { label: 'Suspended', value: data.suspendedUsers },
        { label: 'Pending', value: data.pendingUsers },
        { label: 'New in Period', value: data.newUsersInPeriod }
      ])}
      <h4>By Role</h4>
      ${table(['Role', 'Users', 'Active Users'],
        (data.byRole || []).map(r => [escapeHtml(r.role), r.userCount, r.activeUserCount]))}
      <h4>By Status</h4>
      ${table(['Status', 'Users'],
        (data.byStatus || []).map(r => [escapeHtml(r.status), r.count]))}
    `);
  }

  function renderRoleReport(data) {
    output.innerHTML = card(`
      <h3>Role Distribution Report</h3>
      ${table(['Role', 'Description', 'Users', 'Active Users'],
        data.map(r => [escapeHtml(r.role), escapeHtml(r.description), r.userCount, r.activeUserCount]))}
    `);
  }

  function renderActivityReport(data) {
    const actions = Object.entries(data.byAction || {});
    const modules = Object.entries(data.byModule || {});

    output.innerHTML = card(`
      <h3>System Activity Report</h3>
      <p class="text-muted">Generated ${AdminUi.formatDateTime(data.generatedAt)}</p>
      ${statRow([
        { label: 'Total Events', value: data.totalEvents },
        { label: 'Success', value: data.successEvents },
        { label: 'Failure', value: data.failureEvents }
      ])}
      <h4>By Action</h4>
      ${table(['Action', 'Count'], actions.map(([k, v]) => [escapeHtml(k), v]))}
      <h4>By Module</h4>
      ${table(['Module', 'Count'], modules.map(([k, v]) => [escapeHtml(k), v]))}
      <h4>Most Recent</h4>
      ${table(['Timestamp', 'User', 'Action', 'Module', 'Status'],
        (data.recent || []).map(r => [
          AdminUi.formatDateTime(r.timestamp),
          escapeHtml(r.userName || '-'),
          escapeHtml(r.action),
          escapeHtml(r.module),
          escapeHtml(r.status)
        ]))}
    `);
  }

  function renderPlatformReport(data) {
    const p = data.platform || {};
    const u = data.users || {};

    output.innerHTML = card(`
      <h3>Platform Statistics Report</h3>
      <p class="text-muted">Generated ${AdminUi.formatDateTime(data.generatedAt)}</p>

      <h4>Users</h4>
      ${statRow([
        { label: 'Total Users', value: u.totalUsers },
        { label: 'Active', value: u.activeUsers },
        { label: 'Inactive', value: u.inactiveUsers },
        { label: 'Suspended', value: u.suspendedUsers }
      ])}

      <h4>Component Overview (read-only)</h4>
      ${statRow([
        { label: 'Microgrids (M1)', value: p.microgridCount },
        { label: 'Active Microgrids', value: p.activeMicrogridCount },
        { label: 'Reservations (M2)', value: p.reservationCount },
        { label: 'Pending Reservations', value: p.pendingReservationCount },
        { label: 'Transactions (M3)', value: p.transactionCount },
        { label: 'Completed', value: p.completedTransactionCount }
      ])}
      <p class="text-muted">${escapeHtml(p.microgrids ? p.microgrids.note : '')}</p>

      <h4>Role Distribution</h4>
      ${table(['Role', 'Users', 'Active Users'],
        (data.roleDistribution || []).map(r => [escapeHtml(r.role), r.userCount, r.activeUserCount]))}
    `);
  }

  function exportCsv() {
    // The rendered tables are the export source, so the CSV always matches the view.
    const tables = output.querySelectorAll('table');
    if (!tables.length) {
      ApiClient.showToast('Generate a report before exporting.', 'error');
      return;
    }

    const lines = [];
    tables.forEach(table => {
      const heading = table.closest('.card')?.querySelector('h3')?.textContent;
      if (heading) lines.push(csvCell(heading));

      table.querySelectorAll('tr').forEach(row => {
        const cells = Array.from(row.querySelectorAll('th, td'))
          .map(cell => csvCell(cell.textContent.trim()));
        lines.push(cells.join(','));
      });
      lines.push('');
    });

    const blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = url;
    link.download = `m4-report-${typeSelect.value}-${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    ApiClient.showToast('CSV exported.', 'success');
  }

  function csvCell(value) {
    const text = String(value ?? '').replace(/"/g, '""');
    return /[",\n]/.test(text) ? `"${text}"` : text;
  }
});
