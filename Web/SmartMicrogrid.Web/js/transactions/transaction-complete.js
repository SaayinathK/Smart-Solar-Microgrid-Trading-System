/* ============================================================================
   Smart Microgrid Energy System - Transaction Completion
   ============================================================================ */

let completionTransactionId = '';
let completionInProgress = false;

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('TransactionVerifier')) return;

  renderAppLayout('transactions', 'Energy Transfer Confirmation');
  completionTransactionId = new URLSearchParams(window.location.search).get('id')?.trim() || '';

  document.getElementById('refresh-completion-transaction').addEventListener('click', loadCompletionTransaction);
  document.getElementById('energy-transfer-confirmed').addEventListener('change', updateCompleteButton);
  document.getElementById('complete-transaction-button').addEventListener('click', prepareCompletion);
  document.getElementById('confirm-completion-button').addEventListener('click', completeTransaction);
  document.getElementById('cancel-completion-button').addEventListener('click', cancelCompletionConfirmation);

  if (!completionTransactionId) {
    setCompletionPageMessage('Transaction ID is missing.');
    document.getElementById('refresh-completion-transaction').disabled = true;
    return;
  }

  document.getElementById('completion-view-details').href =
    `transaction-details.html?id=${encodeURIComponent(completionTransactionId)}`;
  loadCompletionTransaction();
});

async function loadCompletionTransaction() {
  const refreshButton = document.getElementById('refresh-completion-transaction');
  refreshButton.disabled = true;
  refreshButton.textContent = 'Refreshing...';
  document.getElementById('completion-page-content').hidden = true;
  document.getElementById('completion-success').hidden = true;
  document.getElementById('completion-result').hidden = true;
  document.getElementById('completion-final-confirmation').hidden = true;
  setCompletionPageMessage('Loading transaction...');

  try {
    const response = await TransactionApi.getTransaction(completionTransactionId);
    if (response?.success !== true || !response.data) {
      setCompletionPageMessage(response?.message || 'Unable to load transaction. Please try again.');
      return;
    }

    renderCompletionTransaction(response.data);
    document.getElementById('completion-page-message').hidden = true;
    document.getElementById('completion-page-content').hidden = false;
  } catch (error) {
    setCompletionPageMessage(getCompletionErrorMessage(error));
  } finally {
    refreshButton.disabled = false;
    refreshButton.textContent = 'Refresh';
  }
}

function renderCompletionTransaction(transaction) {
  setCompletionValue('completion-transaction-id', transaction.id);
  setCompletionValue('completion-reservation-id', transaction.reservationId);
  setCompletionValue('completion-prosumer-id', transaction.prosumerId);
  setCompletionValue('completion-energy-amount', formatCompletionEnergy(transaction.energyAmount));

  const status = transaction.status || 'Unknown';
  const statusBadge = document.createElement('span');
  statusBadge.className = `status-badge transaction-status-${getCompletionStatusClass(status)}`;
  statusBadge.textContent = status;
  document.getElementById('completion-current-status').replaceChildren(statusBadge);

  document.getElementById('completion-confirmation-card').hidden =
    status !== 'Verified' && status !== 'EnergyTransferInProgress';
  document.getElementById('completion-unavailable').hidden =
    status === 'Verified' || status === 'EnergyTransferInProgress';

  if (status !== 'Verified' && status !== 'EnergyTransferInProgress') {
    document.getElementById('completion-unavailable').textContent = getCompletionStatusMessage(status);
  }
}

function updateCompleteButton() {
  document.getElementById('complete-transaction-button').disabled =
    !document.getElementById('energy-transfer-confirmed').checked || completionInProgress;
}

function prepareCompletion() {
  if (completionInProgress || !document.getElementById('energy-transfer-confirmed').checked) return;
  document.getElementById('completion-result').hidden = true;
  document.getElementById('completion-final-confirmation').hidden = false;
  document.getElementById('confirm-completion-button').focus();
}

function cancelCompletionConfirmation() {
  if (completionInProgress) return;
  document.getElementById('completion-final-confirmation').hidden = true;
  document.getElementById('complete-transaction-button').focus();
}

async function completeTransaction() {
  if (completionInProgress || !document.getElementById('energy-transfer-confirmed').checked) return;

  completionInProgress = true;
  setCompletionControlsDisabled(true);
  document.getElementById('completion-final-confirmation').hidden = true;
  document.getElementById('completion-progress').textContent = 'Completing transaction...';
  document.getElementById('completion-progress').hidden = false;
  document.getElementById('completion-result').hidden = true;

  try {
    const response = await TransactionApi.completeTransaction(completionTransactionId);
    if (response?.success !== true || !response.data || !response.data.id || !response.data.status) {
      showCompletionResult(response?.message || 'Transaction completion failed. Please try again.', 'error');
      return;
    }

    if (response.data.status === 'Completed') {
      renderCompletionSuccess(response.data);
      document.getElementById('completion-confirmation-card').hidden = true;
      document.getElementById('completion-success').hidden = false;
      showCompletionResult('Transaction Completed Successfully', 'success');
    } else {
      showCompletionResult(`The backend returned status: ${response.data.status}.`, 'info');
    }
  } catch (error) {
    showCompletionResult(getCompletionErrorMessage(error), 'error');
  } finally {
    completionInProgress = false;
    document.getElementById('completion-progress').hidden = true;
    setCompletionControlsDisabled(false);
    updateCompleteButton();
  }
}

function renderCompletionSuccess(transaction) {
  setCompletionValue('completed-transaction-id', transaction.id);
  setCompletionValue('completed-reservation-id', transaction.reservationId);
  setCompletionValue('completed-energy-amount', formatCompletionEnergy(transaction.energyAmount));
  setCompletionValue('completed-transfer-time', formatCompletionDate(transaction.energyTransferTime));
  setCompletionValue('completed-final-status', transaction.status);
  setCompletionValue('completed-updated-at', formatCompletionDate(transaction.updatedAt));
  document.getElementById('completed-view-transaction').href =
    `transaction-details.html?id=${encodeURIComponent(transaction.id)}`;
}

function setCompletionControlsDisabled(disabled) {
  document.getElementById('refresh-completion-transaction').disabled = disabled;
  document.getElementById('energy-transfer-confirmed').disabled = disabled;
  document.getElementById('complete-transaction-button').disabled = disabled;
  document.getElementById('confirm-completion-button').disabled = disabled;
  document.getElementById('cancel-completion-button').disabled = disabled;
}

function getCompletionStatusMessage(status) {
  const messages = {
    Pending: 'This transaction has not been verified yet.',
    QRGenerated: 'Verify the transaction before confirming energy transfer.',
    VerificationPending: 'Verify the transaction before confirming energy transfer.',
    Completed: 'Transaction is already completed.',
    Rejected: 'Transaction was rejected.',
    Cancelled: 'Transaction was cancelled.'
  };
  return messages[status] || 'This transaction is not ready for completion.';
}

function setCompletionValue(elementId, value) {
  document.getElementById(elementId).textContent =
    value === null || value === undefined || value === '' ? 'Not available' : value;
}

function formatCompletionEnergy(value) {
  if (value === null || value === undefined || value === '') return 'Not available';
  const amount = Number(value);
  return Number.isFinite(amount) ? `${amount} kWh` : 'Not available';
}

function formatCompletionDate(value) {
  if (!value) return 'Not available';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Not available' : date.toLocaleString();
}

function getCompletionStatusClass(status) {
  return String(status || 'unknown').toLowerCase().replace(/[^a-z0-9-]/g, '-');
}

function setCompletionPageMessage(message) {
  const element = document.getElementById('completion-page-message');
  element.textContent = message;
  element.hidden = false;
}

function showCompletionResult(message, type) {
  const result = document.getElementById('completion-result');
  result.replaceChildren();
  result.className = `transaction-verification-result transaction-result-${type}`;
  const paragraph = document.createElement('p');
  paragraph.textContent = message;
  result.appendChild(paragraph);
  result.hidden = false;
}

function getCompletionErrorMessage(error) {
  if (error?.status === 401) return 'Your session has expired. Please log in again.';
  if (error?.status === 403) return 'You are not authorized to complete this transaction.';
  if (error?.status === 404) return 'Transaction was not found.';
  if (error?.status === 409) return 'This transaction cannot be processed in its current state.';
  if (error?.status === 502) return 'The reservation service is temporarily unavailable.';
  if (error?.status >= 500) return 'Something went wrong. Please try again.';
  if (error?.status >= 400 && error.message) return error.message;
  return 'Unable to connect to the server. Please check your connection.';
}
