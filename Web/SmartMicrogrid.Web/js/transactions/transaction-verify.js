/* ============================================================================
   Smart Microgrid Energy System - Transaction QR Verification
   ============================================================================ */

let verificationTransactionId = '';
let verificationQrData = '';
let verificationInProgress = false;

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('TransactionVerifier')) return;

  renderAppLayout('transactions', 'Verify Transaction');

  verificationTransactionId = new URLSearchParams(window.location.search).get('id')?.trim() || '';
  const cancelLink = document.getElementById('cancel-verification-link');

  document.getElementById('refresh-verification-transaction')
    .addEventListener('click', loadVerificationTransaction);
  document.getElementById('transaction-verification-form')
    .addEventListener('submit', prepareVerification);
  document.getElementById('confirm-verification-button')
    .addEventListener('click', submitVerification);
  document.getElementById('cancel-confirmation-button')
    .addEventListener('click', cancelConfirmation);

  if (!verificationTransactionId) {
    setPageMessage('Transaction ID is missing.');
    cancelLink.href = 'transactions.html';
    document.getElementById('refresh-verification-transaction').disabled = true;
    return;
  }

  cancelLink.href = `transaction-details.html?id=${encodeURIComponent(verificationTransactionId)}`;
  loadVerificationTransaction();
});

async function loadVerificationTransaction() {
  const refreshButton = document.getElementById('refresh-verification-transaction');
  refreshButton.disabled = true;
  refreshButton.textContent = 'Refreshing...';
  document.getElementById('verification-page-content').hidden = true;
  document.getElementById('verification-success-details').hidden = true;
  clearVerificationResult();
  setPageMessage('Loading transaction...');

  try {
    const response = await TransactionApi.getTransaction(verificationTransactionId);
    if (response?.success !== true || !response.data || !response.data.id || !response.data.status) {
      setPageMessage(getSafeResponseMessage(response) || 'Unable to load transaction. Please try again.');
      return;
    }

    renderVerificationSummary(response.data);
    document.getElementById('verification-page-message').hidden = true;
    document.getElementById('verification-page-content').hidden = false;
  } catch (error) {
    setPageMessage(getVerificationErrorMessage(error));
  } finally {
    refreshButton.disabled = false;
    refreshButton.textContent = 'Refresh';
  }
}

function renderVerificationSummary(transaction) {
  setValue('verification-transaction-id', transaction.id);
  setValue('verification-reservation-id', transaction.reservationId);
  setValue('verification-prosumer-id', transaction.prosumerId);
  setValue('verification-microgrid-id', transaction.microgridNodeId);
  setValue('verification-energy-slot-id', transaction.energySlotId);
  setValue('verification-energy-amount', formatEnergyAmount(transaction.energyAmount));

  const status = transaction.status || 'Unknown';
  const statusBadge = document.createElement('span');
  statusBadge.className = `status-badge transaction-status-${getStatusClass(status)}`;
  statusBadge.textContent = status;
  document.getElementById('verification-current-status').replaceChildren(statusBadge);

  const eligible = status === 'QRGenerated' || status === 'VerificationPending';
  document.getElementById('verification-form-card').hidden = !eligible;
  document.getElementById('verification-unavailable').hidden = true;
  if (!eligible) showVerificationUnavailable(getStatusMessage(status));
}

function prepareVerification(event) {
  event.preventDefault();
  if (verificationInProgress) return;

  const qrCodeData = document.getElementById('qr-code-data').value.trim();
  clearVerificationResult();

  if (!qrCodeData) {
    showVerificationResult('Enter the scanned QR code data before continuing.', 'error');
    return;
  }

  const qrTransactionId = getQrTransactionId(qrCodeData);
  if (qrTransactionId && qrTransactionId !== verificationTransactionId) {
    showVerificationResult('QR code does not belong to this transaction.', 'error');
    return;
  }

  verificationQrData = qrCodeData;
  document.getElementById('verification-confirmation').hidden = false;
  document.getElementById('confirm-verification-button').focus();
}

function getQrTransactionId(qrCodeData) {
  const match = qrCodeData.match(/^SMART-MICROGRID\|TRANSACTION\|([^|]+)\|([^|]+)$/);
  return match ? match[1] : null;
}

function cancelConfirmation() {
  if (verificationInProgress) return;
  document.getElementById('verification-confirmation').hidden = true;
  verificationQrData = '';
  document.getElementById('qr-code-data').focus();
}

async function submitVerification() {
  if (verificationInProgress || !verificationQrData) return;

  verificationInProgress = true;
  setVerificationControlsDisabled(true);
  document.getElementById('verification-confirmation').hidden = true;
  document.getElementById('verification-progress').textContent = 'Verifying transaction...';
  document.getElementById('verification-progress').hidden = false;
  clearVerificationResult();

  try {
    const response = await TransactionApi.verifyTransaction(
      verificationTransactionId,
      verificationQrData
    );

    if (response?.success !== true || !response.data || !response.data.id || !response.data.status) {
      showVerificationResult(
        getSafeResponseMessage(response) || 'Transaction verification failed.',
        'error'
      );
      return;
    }

    const transaction = response.data;
    const status = transaction.status || 'Unknown';
    renderReturnedStatus(status);

    if (status === 'Verified') {
      showVerificationResult('Transaction Verified', 'success');
      renderVerificationSuccess(transaction);
      document.getElementById('verification-form-card').hidden = true;
    } else {
      showVerificationResult(`Verification request completed. Current status: ${status}.`, 'info');
    }
  } catch (error) {
    showVerificationResult(getVerificationErrorMessage(error), 'error');
  } finally {
    verificationInProgress = false;
    document.getElementById('verification-progress').hidden = true;
    setVerificationControlsDisabled(false);
  }
}

function renderReturnedStatus(status) {
  const badge = document.createElement('span');
  badge.className = `status-badge transaction-status-${getStatusClass(status)}`;
  badge.textContent = status;
  document.getElementById('verification-current-status').replaceChildren(badge);
}

function renderVerificationSuccess(transaction) {
  const transactionId = transaction.id;
  setVerificationValue('verified-transaction-id', transactionId);
  setVerificationValue('verified-reservation-id', transaction.reservationId);
  setVerificationValue('verified-energy-amount', formatVerificationEnergy(transaction.energyAmount));
  setVerificationValue('verified-by', transaction.verifiedBy);
  setVerificationValue('verified-at', formatVerificationDate(transaction.verificationTime));
  setVerificationValue('verified-status', transaction.status);

  document.getElementById('verified-view-transaction').href =
    `transaction-details.html?id=${encodeURIComponent(transactionId)}`;
  document.getElementById('verified-continue-transfer').href =
    `transaction-complete.html?id=${encodeURIComponent(transactionId)}`;
  document.getElementById('verification-success-details').hidden = false;
}

function setVerificationValue(elementId, value) {
  document.getElementById(elementId).textContent =
    value === null || value === undefined || value === '' ? 'Not available' : value;
}

function formatVerificationEnergy(value) {
  if (value === null || value === undefined || value === '') return 'Not available';
  const amount = Number(value);
  return Number.isFinite(amount) ? `${amount} kWh` : 'Not available';
}

function formatVerificationDate(value) {
  if (!value) return 'Not available';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Not available' : date.toLocaleString();
}

function setVerificationControlsDisabled(disabled) {
  document.getElementById('qr-code-data').disabled = disabled;
  document.getElementById('verify-transaction-button').disabled = disabled;
  document.getElementById('confirm-verification-button').disabled = disabled;
  document.getElementById('cancel-confirmation-button').disabled = disabled;
}

function showVerificationUnavailable(message) {
  const element = document.getElementById('verification-unavailable');
  element.textContent = message;
  element.hidden = false;
}

function getStatusMessage(status) {
  const messages = {
    Pending: 'QR code has not been generated yet.',
    Verified: 'Transaction has already been verified.',
    EnergyTransferInProgress: 'Energy transfer is in progress.',
    Completed: 'Transaction is already completed.',
    Rejected: 'Transaction was rejected.',
    Cancelled: 'Transaction was cancelled.'
  };
  return messages[status] || 'This transaction is not available for QR verification.';
}

function setValue(elementId, value) {
  document.getElementById(elementId).textContent =
    value === null || value === undefined || value === '' ? 'Not available' : value;
}

function formatEnergyAmount(value) {
  if (value === null || value === undefined || value === '') return 'Not available';
  const amount = Number(value);
  return Number.isFinite(amount) ? `${amount} kWh` : 'Not available';
}

function getStatusClass(status) {
  return String(status || 'unknown').toLowerCase().replace(/[^a-z0-9-]/g, '-');
}

function setPageMessage(message) {
  const element = document.getElementById('verification-page-message');
  element.textContent = message;
  element.hidden = false;
}

function clearVerificationResult() {
  const result = document.getElementById('verification-result');
  result.replaceChildren();
  result.hidden = true;
  document.getElementById('verification-success-details').hidden = true;
}

function showVerificationResult(message, type) {
  const result = document.getElementById('verification-result');
  result.replaceChildren();
  result.className = `transaction-verification-result transaction-result-${type}`;
  const messageElement = document.createElement('p');
  messageElement.textContent = message;
  result.appendChild(messageElement);
  result.hidden = false;
}

function getSafeResponseMessage(response) {
  return typeof response?.message === 'string' ? response.message : '';
}

function getVerificationErrorMessage(error) {
  if (error?.status === 401) return 'Your session has expired. Please log in again.';
  if (error?.status === 403) return 'You are not authorized to verify this transaction.';
  if (error?.status === 404) return 'Transaction was not found.';
  if (error?.status === 409) return 'Transaction verification could not be completed because the transaction state has changed.';
  if (error?.status === 502) return 'The reservation service is currently unavailable.';
  if (error?.status >= 500) return 'Transaction verification failed. Please try again.';
  if (error?.status >= 400 && error.message) return error.message;
  return 'Unable to connect to the server. Please try again.';
}
