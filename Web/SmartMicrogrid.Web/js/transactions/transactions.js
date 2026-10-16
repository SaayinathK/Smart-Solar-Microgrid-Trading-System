/* ============================================================================
   Smart Microgrid Energy System - Transaction List & History
   ============================================================================ */

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireRole(['Admin', 'MicrogridOperator', 'Prosumer'])) return;

  renderAppLayout('transactions', 'Energy Transactions');

  const createTransactionCard = document.getElementById('create-transaction-card');
  const isOperator = SessionManager.getUserRole() === 'MicrogridOperator';
  createTransactionCard.hidden = !isOperator;
  document.getElementById('create-transaction-form').addEventListener('submit', createTransactionFromReservation);
  document.getElementById('reservation-id').addEventListener('input', event => {
    event.target.removeAttribute('aria-invalid');
  });

  const refreshButton = document.getElementById('refresh-transactions');
  const filterSelect = document.getElementById('transaction-filter');
  const searchInput = document.getElementById('transaction-search');
  applyTransactionView();
  document.querySelectorAll('[data-workspace="transactions"] a').forEach(link => {
    link.addEventListener('click', navigateTransactionView);
  });
  window.addEventListener('popstate', () => {
    applyTransactionView();
    searchInput.value = '';
    renderTransactions();
  });

  refreshButton.addEventListener('click', loadTransactions);
  filterSelect.addEventListener('change', () => {
    const url = new URL(window.location.href);
    url.searchParams.set('view', filterSelect.value);
    window.history.replaceState(null, '', url);
    updateTransactionView();
    renderTransactions();
  });
  let searchTimeout;
  searchInput.addEventListener('input', () => {
    window.clearTimeout(searchTimeout);
    searchTimeout = window.setTimeout(renderTransactions, 200);
  });

  loadTransactions();
});

let transactions = [];
let transactionsLoading = false;
let transactionLoadError = '';

function navigateTransactionView(event) {
  if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
  const view = new URL(event.currentTarget.href).searchParams.get('view');
  if (!['attention', 'all', 'history'].includes(view)) return;

  event.preventDefault();
  document.getElementById('transaction-filter').value = view;
  document.getElementById('transaction-search').value = '';
  const url = new URL(window.location.href);
  url.searchParams.set('view', view);
  if (url.href !== window.location.href) {
    try {
      window.history.pushState(null, '', url);
    } catch (error) {
      // Embedded previews can disallow URL changes; the view must still switch.
      if (error.name !== 'SecurityError') throw error;
    }
  }
  updateTransactionView();
  renderTransactions();
}

function applyTransactionView() {
  const filter = document.getElementById('transaction-filter');
  const requested = new URLSearchParams(window.location.search).get('view');
  const fallback = SessionManager.getUserRole() === 'MicrogridOperator' ? 'attention' : 'all';
  filter.value = Array.from(filter.options).some(option => option.value === requested) ? requested : fallback;
  updateTransactionView();
}

function updateTransactionView() {
  const filter = document.getElementById('transaction-filter').value;
  const view = ['attention', 'history'].includes(filter) ? filter : 'all';
  const descriptions = {
    attention: ['Needs attention', 'Generate QR codes, verify handoffs, and complete energy transfers.'],
    history: ['Transaction history', 'Review completed, rejected, and cancelled transactions.'],
    all: ['Transaction records', 'Review current and past energy transactions.']
  };
  document.getElementById('transactions-list-title').textContent = descriptions[view][0];
  document.getElementById('transactions-list-description').textContent = descriptions[view][1];
  document.querySelector('.transaction-table').classList.toggle('transaction-table-attention', view === 'attention');
  const isOperator = SessionManager.getUserRole() === 'MicrogridOperator';
  document.getElementById('transactions-page-description').textContent = isOperator
    ? 'Manage your assigned microgrids’ transaction queue, transfers, and history in one place.'
    : 'Monitor energy transactions and review the transfer ledger.';
  document.querySelectorAll('[data-workspace="transactions"] a').forEach(link => {
    if (new URL(link.href).searchParams.get('view') === view) link.setAttribute('aria-current', 'page');
    else link.removeAttribute('aria-current');
  });
}

async function createTransactionFromReservation(event) {
  event.preventDefault();
  if (SessionManager.getUserRole() !== 'MicrogridOperator') return;

  const reservationIdInput = document.getElementById('reservation-id');
  const createButton = document.getElementById('create-transaction-button');
  const createLabel = document.getElementById('create-transaction-label');
  const state = document.getElementById('create-transaction-state');
  if (createButton.disabled) return;
  let reservationId = reservationIdInput.value.trim();
  if (!reservationId) return;

  createButton.disabled = true;
  createButton.setAttribute('aria-busy', 'true');
  createLabel.textContent = 'Creating…';
  reservationIdInput.readOnly = true;
  reservationIdInput.removeAttribute('aria-invalid');
  state.dataset.kind = 'loading';
  state.textContent = 'Creating transaction...';
  state.hidden = false;

  try {
    if (/^RES-/i.test(reservationId)) {
      const list = await ReservationApi.list();
      if (!list?.success || !Array.isArray(list.data)) throw new Error('Could not look up this reservation reference. Try again or paste the full ID.');
      reservationId = RecordReferences.resolve('RES', reservationId, list.data.map(item => item.id || item._id));
    }
    const response = await TransactionApi.createTransaction(reservationId);
    if (response?.success !== true || !response.data) {
      throw new Error(response?.message || 'Transaction could not be created. Confirm the reservation is approved.');
    }

    state.dataset.kind = 'success';
    state.textContent = `Transaction created: ${RecordReferences.label('TX', response.data.id)}. Open its details to generate a QR code.`;
    if (response.data.id) {
      const details = document.createElement('a');
      details.href = getWebPageUrl(`transaction-details.html?id=${encodeURIComponent(response.data.id)}`);
      details.textContent = 'View transaction →';
      state.appendChild(details);
    }
    reservationIdInput.value = '';
    await loadTransactions();
  } catch (error) {
    state.dataset.kind = 'error';
    state.textContent = error?.message || 'Unable to create transaction. Confirm the reservation is approved.';
    reservationIdInput.setAttribute('aria-invalid', 'true');
    state.focus();
  } finally {
    createButton.disabled = false;
    createButton.removeAttribute('aria-busy');
    createLabel.textContent = 'Create transaction';
    reservationIdInput.readOnly = false;
  }
}

async function loadTransactions() {
  transactionsLoading = true;
  transactionLoadError = '';
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
    transactionsLoading = false;
    updateTransactionSummary();
    renderTransactions();
  } catch (error) {
    transactionsLoading = false;
    transactionLoadError = getTransactionsErrorMessage(error);
    transactions = [];
    updateTransactionSummary();
    document.getElementById('transactions-table-body').replaceChildren();
    setTransactionsState(transactionLoadError);
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
    transaction => ['Verified', 'EnergyTransferInProgress'].includes(transaction.status)
  ).length;
  document.getElementById('completed-transactions').textContent = transactions.filter(
    transaction => transaction.status === 'Completed'
  ).length;
}

function renderTransactions() {
  if (transactionsLoading || transactionLoadError) {
    setTransactionsState(transactionsLoading ? 'Loading transactions...' : transactionLoadError);
    return;
  }
  const filter = document.getElementById('transaction-filter').value;
  const search = document.getElementById('transaction-search').value.trim().toLowerCase();
  const filteredTransactions = transactions
    .filter(transaction => matchesStatusFilter(transaction, filter))
    .filter(transaction => !search || [
      transaction.id,
      transaction.reservationId,
      RecordReferences.label('TX', transaction.id, transactions.map(item => item.id)),
      RecordReferences.label('RES', transaction.reservationId, transactions.map(item => item.reservationId)),
      transaction.transactionCode,
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
  const transactionCell = document.createElement('td');
  RecordReferences.renderInto(transactionCell, 'TX', transaction.id, transactions.map(item => item.id));
  row.appendChild(transactionCell);
  const reservationCell = document.createElement('td');
  RecordReferences.renderInto(reservationCell, 'RES', transaction.reservationId, transactions.map(item => item.reservationId));
  row.appendChild(reservationCell);
  row.appendChild(createTextCell(transaction.prosumerId || '—'));
  row.appendChild(createTextCell(formatEnergyAmount(transaction.energyAmount)));

  const statusCell = document.createElement('td');
  const statusBadge = document.createElement('span');
  statusBadge.className = `status-badge transaction-status-${getStatusClass(transaction.status)}`;
  statusBadge.textContent = transaction.status || 'Unknown';
  statusCell.appendChild(statusBadge);
  row.appendChild(statusCell);

  const actionCell = document.createElement('td');
  actionCell.className = 'transaction-row-actions';
  const detailsLink = document.createElement('a');
  detailsLink.className = 'btn btn-secondary btn-sm';
  detailsLink.href = getWebPageUrl(`transaction-details.html?id=${encodeURIComponent(transaction.id || '')}`);
  detailsLink.textContent = 'View Details';
  actionCell.appendChild(detailsLink);

  const isOperator = SessionManager.getUserRole() === 'MicrogridOperator';
  if (isOperator && transaction.status === 'Pending') {
    actionCell.appendChild(createRowAction('Generate QR', `transaction-details.html?id=${encodeURIComponent(transaction.id || '')}`));
  } else if (isOperator && (transaction.status === 'QRGenerated' || transaction.status === 'VerificationPending')) {
    actionCell.appendChild(createRowAction('Verify', `transaction-verify.html?id=${encodeURIComponent(transaction.id || '')}`));
  } else if (SessionManager.getUserRole() === 'MicrogridOperator' &&
      (transaction.status === 'Verified' || transaction.status === 'EnergyTransferInProgress')) {
    actionCell.appendChild(createRowAction('Complete', `transaction-complete.html?id=${encodeURIComponent(transaction.id || '')}`));
  }

  row.appendChild(actionCell);
  for (const value of [transaction.createdAt, transaction.verificationTime, transaction.energyTransferTime]) {
    const cell = createTextCell(formatTransactionDate(value));
    cell.className = 'transaction-date';
    row.appendChild(cell);
  }

  return row;
}

function matchesStatusFilter(transaction, filter) {
  if (filter === 'all') return true;
  if (filter === 'attention') {
    return ['Pending', 'QRGenerated', 'VerificationPending', 'Verified', 'EnergyTransferInProgress'].includes(transaction.status);
  }
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
  link.href = getWebPageUrl(href);
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
