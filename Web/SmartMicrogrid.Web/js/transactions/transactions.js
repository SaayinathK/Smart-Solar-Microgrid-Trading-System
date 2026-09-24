/* ============================================================================
   Smart Microgrid Energy System - Transaction List & History
   ============================================================================ */

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('TransactionVerifier')) return;

  const historyView = new URLSearchParams(window.location.search).get('view') === 'history';
  renderAppLayout(historyView ? 'history' : 'transactions', historyView ? 'Transaction History' : 'Energy Transactions');

  const refreshButton = document.getElementById('refresh-transactions');
  const filterSelect = document.getElementById('transaction-filter');
  const searchInput = document.getElementById('transaction-search');

  if (historyView) {
    filterSelect.value = 'history';
    document.getElementById('transactions-list-title').textContent = 'Transaction History';
    document.getElementById('transactions-page-description').textContent = 'Review completed and terminal energy transactions.';
  }

  refreshButton.addEventListener('click', loadTransactions);
  filterSelect.addEventListener('change', renderTransactions);
  let searchTimeout;
  searchInput.addEventListener('input', () => {
    window.clearTimeout(searchTimeout);
    searchTimeout = window.setTimeout(renderTransactions, 200);
  });

  loadTransactions();
});

let transactions = [];

async function loadTransactions() {
  const refreshButton = document.getElementById('refresh-transactions');
  refreshButton.disabled = true;
  refreshButton.textContent = 'Refreshing...';
  setTransactionsState('Loading transactions...');

  try {
    const response = await TransactionApi.getTransactions();
    if (response?.success !== true || !Array.isArray(response.data)) {
      throw new Error('Transactions could not be loaded.');
    }

    transactions = response.data;
    updateTransactionSummary();
    renderTransactions();
  } catch (error) {
    transactions = [];
    updateTransactionSummary();
    document.getElementById('transactions-table-body').replaceChildren();
    setTransactionsState(getTransactionsErrorMessage(error));
  } finally {
    refreshButton.disabled = false;
    refreshButton.textContent = 'Refresh';
  }
}

function updateTransactionSummary() {
  document.getElementById('total-transactions').textContent = transactions.length;
  document.getElementById('pending-transactions').textContent = transactions.filter(
    transaction => ['Pending', 'QRGenerated', 'VerificationPending'].includes(transaction.status)
  ).length;
  document.getElementById('verified-transactions').textContent = transactions.filter(
    transaction => transaction.status === 'Verified'
  ).length;
  document.getElementById('completed-transactions').textContent = transactions.filter(
    transaction => transaction.status === 'Completed'
  ).length;
}

function renderTransactions() {
  const filter = document.getElementById('transaction-filter').value;
  const search = document.getElementById('transaction-search').value.trim().toLowerCase();
  const filteredTransactions = transactions
    .filter(transaction => matchesStatusFilter(transaction, filter))
    .filter(transaction => !search || [
      transaction.id,
      transaction.reservationId,
      transaction.prosumerId,
      transaction.microgridNodeId
    ].some(value => String(value || '').toLowerCase().includes(search)));

  const tbody = document.getElementById('transactions-table-body');
  tbody.replaceChildren();

  if (filteredTransactions.length === 0) {
    setTransactionsState(transactions.length === 0
      ? 'No transactions found.'
      : 'No transactions match the selected search and filter.');
    return;
  }

  setTransactionsState('');
  filteredTransactions.forEach(transaction => tbody.appendChild(createTransactionRow(transaction)));
}

function createTransactionRow(transaction) {
  const row = document.createElement('tr');
  row.appendChild(createTextCell(transaction.id || 'Not available'));
  row.appendChild(createTextCell(transaction.reservationId || '—'));
  row.appendChild(createTextCell(transaction.prosumerId || '—'));
  row.appendChild(createTextCell(formatEnergyAmount(transaction.energyAmount)));

  const statusCell = document.createElement('td');
  const statusBadge = document.createElement('span');
  statusBadge.className = `status-badge transaction-status-${getStatusClass(transaction.status)}`;
  statusBadge.textContent = transaction.status || 'Unknown';
  statusCell.appendChild(statusBadge);
  row.appendChild(statusCell);

  row.appendChild(createTextCell(formatTransactionDate(transaction.createdAt)));
  row.appendChild(createTextCell(formatTransactionDate(transaction.verificationTime)));
  row.appendChild(createTextCell(formatTransactionDate(transaction.energyTransferTime)));

  const actionCell = document.createElement('td');
  actionCell.className = 'transaction-row-actions';
  const detailsLink = document.createElement('a');
  detailsLink.className = 'btn btn-secondary btn-sm';
  detailsLink.href = `transaction-details.html?id=${encodeURIComponent(transaction.id || '')}`;
  detailsLink.textContent = 'View Details';
  actionCell.appendChild(detailsLink);

  if (transaction.status === 'QRGenerated' || transaction.status === 'VerificationPending') {
    actionCell.appendChild(createRowAction('Verify', `transaction-verify.html?id=${encodeURIComponent(transaction.id || '')}`));
  } else if (transaction.status === 'Verified' || transaction.status === 'EnergyTransferInProgress') {
    actionCell.appendChild(createRowAction('Complete', `transaction-complete.html?id=${encodeURIComponent(transaction.id || '')}`));
  }

  row.appendChild(actionCell);

  return row;
}

function matchesStatusFilter(transaction, filter) {
  if (filter === 'all') return true;
  if (filter === 'pending') {
    return ['Pending', 'QRGenerated', 'VerificationPending'].includes(transaction.status);
  }
  if (filter === 'history') {
    return ['Completed', 'Rejected', 'Cancelled'].includes(transaction.status);
  }
  return transaction.status === filter;
}

function createRowAction(label, href) {
  const link = document.createElement('a');
  link.className = 'btn btn-primary btn-sm';
  link.href = href;
  link.textContent = label;
  return link;
}

function createTextCell(value) {
  const cell = document.createElement('td');
  cell.textContent = value;
  return cell;
}

function formatEnergyAmount(value) {
  const amount = Number(value);
  return Number.isFinite(amount) ? `${amount} kWh` : '—';
}

function formatTransactionDate(value) {
  if (!value) return '—';

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleString();
}

function getStatusClass(status) {
  return String(status || 'unknown').toLowerCase().replace(/[^a-z0-9-]/g, '-');
}

function setTransactionsState(message) {
  const state = document.getElementById('transactions-state');
  state.textContent = message;
  state.hidden = !message;
}

function getTransactionsErrorMessage(error) {
  if (error?.status === 401) return 'Your session has expired. Please sign in again.';
  if (error?.status === 403) return 'You do not have permission to view these transactions.';
  if (error?.status >= 400 && error?.status < 500 && error.message) return error.message;
  return 'Unable to load transactions right now. Please try again.';
}
