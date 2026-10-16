/* M3 is the only QR contract. Clients render the server's stored payload unchanged. */
const TransactionQr = (() => {
  function parse(payload) {
    const match = /^SMART-MICROGRID\|TRANSACTION\|([a-f\d]{24})\|([^|\s]+)$/.exec(String(payload || ''));
    return match ? { transactionId: match[1], transactionCode: match[2] } : null;
  }
  function canDisplay(transaction) {
    const payload = parse(transaction?.qrCodeData);
    return Boolean(payload && payload.transactionId === transaction.id && payload.transactionCode === transaction.transactionCode &&
      ['QRGenerated', 'VerificationPending', 'Verified', 'EnergyTransferInProgress', 'Completed'].includes(transaction.status));
  }
  function render(target, transaction) {
    target.replaceChildren();
    if (!canDisplay(transaction)) {
      target.textContent = transaction?.status === 'Pending' ? 'Awaiting operator QR generation' : 'No transaction QR available';
      return false;
    }
    const code = qrcode(0, 'M');
    code.addData(transaction.qrCodeData, 'Byte');
    code.make();
    target.innerHTML = code.createSvgTag({ cellSize: 4, margin: 16, scalable: true });
    const svg = target.querySelector('svg');
    svg.setAttribute('role', 'img');
    svg.setAttribute('aria-label', 'M3 transaction QR code');
    return true;
  }
  async function findForReservation(reservationId) {
    const response = await TransactionApi.getTransactions();
    if (response?.success !== true || !Array.isArray(response.data)) throw new Error(response?.message || 'Unable to load the reservation transaction. Please retry.');
    return response.data.find(transaction => transaction.reservationId === reservationId) || null;
  }
  async function openForReservation(reservation) {
    let transaction = await findForReservation(reservation.id || reservation._id);
    if (!transaction && SessionManager.getUserRole() === 'MicrogridOperator' && reservation.status === 'Approved') {
      try {
        const response = await TransactionApi.createTransaction(reservation.id || reservation._id);
        if (response?.success !== true || !response.data?.id) throw new Error(response?.message || 'Unable to start the transaction.');
        transaction = response.data;
      } catch (error) {
        // A second operator tab may have created the transaction first.
        if (error?.status !== 409) throw error;
        transaction = await findForReservation(reservation.id || reservation._id);
        if (!transaction) throw error;
      }
    }
    if (!transaction) throw new Error('The assigned operator has not started a transaction for this reservation yet.');
    window.location.href = getWebAppUrl(`pages/M3/transaction-details.html?id=${encodeURIComponent(transaction.id)}`);
  }
  return { parse, canDisplay, render, findForReservation, openForReservation };
})();
