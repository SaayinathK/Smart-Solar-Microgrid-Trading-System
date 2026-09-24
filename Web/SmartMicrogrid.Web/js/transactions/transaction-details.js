/* ============================================================================
   Smart Microgrid Energy System - Transaction Details
   ============================================================================ */

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('TransactionVerifier')) return;

  renderAppLayout('transactions', 'Transaction Details');

  const transactionId = new URLSearchParams(window.location.search).get('id')?.trim();
  const refreshButton = document.getElementById('refresh-transaction');

  refreshButton.addEventListener('click', () => loadTransaction(transactionId));

  if (!transactionId) {
    setTransactionMessage('Transaction ID is missing.');
    refreshButton.disabled = true;
    return;
  }

  loadTransaction(transactionId);
});

async function loadTransaction(transactionId) {
  const refreshButton = document.getElementById('refresh-transaction');
  refreshButton.disabled = true;
  refreshButton.textContent = 'Refreshing...';
  document.getElementById('transaction-content').hidden = true;
  setTransactionMessage('Loading transaction...');

  try {
    const response = await TransactionApi.getTransaction(transactionId);
    if (response?.success !== true || !response.data) {
      document.getElementById('transaction-content').hidden = true;
      setTransactionMessage('Unable to load transaction. Please try again.');
      return;
    }

    renderTransaction(response.data);
    document.getElementById('transaction-message').hidden = true;
    document.getElementById('transaction-content').hidden = false;
  } catch (error) {
    document.getElementById('transaction-content').hidden = true;
    setTransactionMessage(getTransactionErrorMessage(error));
  } finally {
    refreshButton.disabled = false;
    refreshButton.textContent = 'Refresh';
  }
}

function renderTransaction(transaction) {
  setDetailValue('detail-id', transaction.id);
  setDetailValue('detail-code', transaction.transactionCode);
  setDetailValue('detail-reservation', transaction.reservationId);
  setDetailValue('detail-prosumer', transaction.prosumerId);
  setDetailValue('detail-microgrid', transaction.microgridNodeId);
  setDetailValue('detail-slot', transaction.energySlotId);
  setDetailValue('detail-energy', formatEnergyAmount(transaction.energyAmount));
  setDetailValue('detail-verified-by', transaction.verifiedBy || 'Not verified');
  setDetailValue('detail-verification-time', formatTransactionDate(transaction.verificationTime));
  setDetailValue('detail-created', formatTransactionDate(transaction.createdAt));
  setDetailValue('detail-updated', formatTransactionDate(transaction.updatedAt));
  setDetailValue('detail-transfer-time', formatTransactionDate(transaction.energyTransferTime));

  const status = transaction.status || 'Unknown';
  const statusContainer = document.getElementById('transaction-status');
  const statusBadge = document.createElement('span');
  statusBadge.className = `status-badge transaction-status-${getStatusClass(status)}`;
  statusBadge.textContent = status;
  statusContainer.replaceChildren(statusBadge);

  const qrData = document.getElementById('detail-qr');
  qrData.textContent = transaction.qrCodeData || 'QR code has not been generated.';

  renderTransactionAction(status, transaction.id);
}

function renderTransactionAction(status, transactionId) {
  const action = document.getElementById('transaction-action');
  action.replaceChildren();
  action.classList.remove('transaction-action-terminal');

  if (status === 'QRGenerated' || status === 'VerificationPending') {
    action.appendChild(createActionLink(
      'Verify Transaction',
      `transaction-verify.html?id=${encodeURIComponent(transactionId || '')}`
    ));
    return;
  }

  if (status === 'Verified' || status === 'EnergyTransferInProgress') {
    action.appendChild(createActionLink(
      status === 'Verified' ? 'Confirm Energy Transfer' : 'Continue to Completion',
      `transaction-complete.html?id=${encodeURIComponent(transactionId || '')}`
    ));
    return;
  }

  if (status === 'Completed') {
    action.textContent = 'Transaction Completed';
    action.classList.add('transaction-action-terminal');
    return;
  }

  if (status === 'Rejected' || status === 'Cancelled') {
    action.textContent = `Transaction ${status}`;
    action.classList.add('transaction-action-terminal');
    return;
  }

  if (status === 'Pending') {
    action.textContent = 'QR generation is required before verification.';
  }
}

function createActionLink(label, href) {
  const link = document.createElement('a');
  link.className = 'btn btn-primary';
  link.href = href;
  link.textContent = label;
  return link;
}

function setDetailValue(elementId, value) {
  document.getElementById(elementId).textContent =
    value === null || value === undefined || value === '' ? 'Not available' : value;
}

function formatEnergyAmount(value) {
  if (value === null || value === undefined || value === '') return 'Not available';
  const amount = Number(value);
  return Number.isFinite(amount) ? `${amount} kWh` : 'Not available';
}

function formatTransactionDate(value) {
  if (!value) return 'Not available';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Not available' : date.toLocaleString();
}

function getStatusClass(status) {
  return String(status || 'unknown').toLowerCase().replace(/[^a-z0-9-]/g, '-');
}

function setTransactionMessage(message) {
  const messageElement = document.getElementById('transaction-message');
  messageElement.textContent = message;
  messageElement.hidden = false;
}

function getTransactionErrorMessage(error) {
  if (error?.status === 401) return 'Your session has expired. Please sign in again.';
  if (error?.status === 403) return 'You are not authorized to view this transaction.';
  if (error?.status === 404) return 'Transaction not found.';
  if (error?.status >= 500) return 'Unable to load transaction right now. Please try again.';
  return 'Unable to connect to the server. Please try again.';
}
