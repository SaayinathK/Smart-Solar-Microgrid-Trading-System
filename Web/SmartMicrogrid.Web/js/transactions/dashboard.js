/* ============================================================================
   Smart Microgrid Energy System - Transaction Verifier Dashboard
   ============================================================================ */

let dashboardTransactions = [];

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('TransactionVerifier')) return;

  renderAppLayout('m3-dashboard', 'Transaction Verifier Dashboard');
  document.getElementById('refresh-verifier-dashboard').addEventListener('click', loadDashboardTransactions);
  loadDashboardTransactions();
});

async function loadDashboardTransactions() {
  const refreshButton = document.getElementById('refresh-verifier-dashboard');
  refreshButton.disabled = true;
  refreshButton.textContent = 'Refreshing...';
  setDashboardState('Loading transactions...');

  try {
    const response = await TransactionApi.getTransactions();
    if (response?.success !== true || !Array.isArray(response.data)) {
      throw new Error('Unable to load transactions.');
    }

    dashboardTransactions = response.data;
    updateDashboardCounts();
    renderAttentionTransactions();
  } catch (error) {
    dashboardTransactions = [];
    updateDashboardCounts();
    document.getElementById('dashboard-attention-body').replaceChildren();
    setDashboardState(getDashboardErrorMessage(error));
  } finally {
    refreshButton.disabled = false;
    refreshButton.textContent = 'Refresh';
  }
}

function updateDashboardCounts() {
  const counts = {
    'dashboard-total': dashboardTransactions.length,
    'dashboard-pending': countDashboardStatuses(['Pending', 'QRGenerated', 'VerificationPending']),
    'dashboard-verified': countDashboardStatuses(['Verified']),
    'dashboard-completed': countDashboardStatuses(['Completed']),
    'dashboard-rejected': countDashboardStatuses(['Rejected'])
  };

  Object.entries(counts).forEach(([elementId, count]) => {
    document.getElementById(elementId).textContent = count;
  });
}

function countDashboardStatuses(statuses) {
  return dashboardTransactions.filter(transaction => statuses.includes(transaction.status)).length;
}

function renderAttentionTransactions() {
  const attentionStatuses = ['QRGenerated', 'VerificationPending', 'Verified', 'EnergyTransferInProgress'];
  const attentionTransactions = dashboardTransactions.filter(transaction => attentionStatuses.includes(transaction.status));
  const tbody = document.getElementById('dashboard-attention-body');
  tbody.replaceChildren();

  if (attentionTransactions.length === 0) {
    setDashboardState(dashboardTransactions.length === 0
      ? 'No transactions found.'
      : 'No transactions currently require attention.');
    return;
  }

  setDashboardState('');
  attentionTransactions.forEach(transaction => tbody.appendChild(createAttentionRow(transaction)));
}

function createAttentionRow(transaction) {
  const row = document.createElement('tr');
  row.appendChild(createDashboardCell(transaction.id));
  row.appendChild(createDashboardCell(transaction.reservationId));
  row.appendChild(createDashboardCell(transaction.prosumerId));
  row.appendChild(createDashboardCell(formatDashboardEnergy(transaction.energyAmount)));

  const statusCell = document.createElement('td');
  const badge = document.createElement('span');
  badge.className = `status-badge transaction-status-${getDashboardStatusClass(transaction.status)}`;
  badge.textContent = transaction.status || 'Unknown';
  statusCell.appendChild(badge);
  row.appendChild(statusCell);

  const actionCell = document.createElement('td');
  actionCell.className = 'transaction-row-actions';
  actionCell.appendChild(createDashboardLink('View Details', 'btn btn-secondary btn-sm',
    `transaction-details.html?id=${encodeURIComponent(transaction.id || '')}`));

  if (transaction.status === 'QRGenerated' || transaction.status === 'VerificationPending') {
    actionCell.appendChild(createDashboardLink('Verify', 'btn btn-primary btn-sm',
      `transaction-verify.html?id=${encodeURIComponent(transaction.id || '')}`));
  } else {
    actionCell.appendChild(createDashboardLink('Complete', 'btn btn-primary btn-sm',
      `transaction-complete.html?id=${encodeURIComponent(transaction.id || '')}`));
  }

  row.appendChild(actionCell);
  return row;
}

function createDashboardCell(value) {
  const cell = document.createElement('td');
  cell.textContent = value === null || value === undefined || value === '' ? 'Not available' : value;
  return cell;
}

function createDashboardLink(label, className, href) {
  const link = document.createElement('a');
  link.className = className;
  link.href = href;
  link.textContent = label;
  return link;
}

function formatDashboardEnergy(value) {
  if (value === null || value === undefined || value === '') return 'Not available';
  const amount = Number(value);
  return Number.isFinite(amount) ? `${amount} kWh` : 'Not available';
}

function getDashboardStatusClass(status) {
  return String(status || 'unknown').toLowerCase().replace(/[^a-z0-9-]/g, '-');
}

function setDashboardState(message) {
  const state = document.getElementById('dashboard-transactions-state');
  state.textContent = message;
  state.hidden = !message;
}

function getDashboardErrorMessage(error) {
  if (error?.status === 401) return 'Your session has expired. Please log in again.';
  if (error?.status === 403) return 'You are not authorized to view these transactions.';
  if (error?.status === 404) return 'Transaction list was not found.';
  if (error?.status >= 500) return 'Something went wrong. Please try again.';
  if (error?.status >= 400 && error.message) return error.message;
  return 'Unable to connect to the server. Please check your connection.';
}
