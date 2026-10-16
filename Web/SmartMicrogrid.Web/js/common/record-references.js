/* Display abbreviations only. API requests, QR checks and permissions use full IDs. */
const RecordReferences = (() => {
  const isObjectId = value => /^[a-f\d]{24}$/i.test(String(value || ''));
  function label(type, id, relatedIds = []) {
    if (!id) return '—';
    if (!isObjectId(id)) return String(id);
    const normalized = String(id).toUpperCase();
    const others = [...new Set(relatedIds.filter(isObjectId).map(value => String(value).toUpperCase()))].filter(value => value !== normalized);
    let length = 8;
    while (length < 24 && others.some(value => value.endsWith(normalized.slice(-length)))) length += 2;
    return `${type === 'RES' ? 'RES' : 'TX'}-${normalized.slice(-length)}`;
  }

  function renderInto(target, type, id, relatedIds = []) {
    if (!target) return;
    if (!isObjectId(id)) { target.textContent = label(type, id, relatedIds); return; }
    const name = type === 'RES' ? 'reservation' : 'transaction';
    const wrapper = document.createElement('span'); wrapper.className = 'record-reference';
    const text = document.createElement('span'); text.className = 'record-reference-label';
    text.textContent = label(type, id, relatedIds);
    text.title = `Abbreviated ${name} ID. Full ID: ${id}`;
    const button = document.createElement('button');
    button.type = 'button'; button.className = 'record-reference-copy';
    button.dataset.copyRecordId = String(id); button.dataset.recordType = name;
    button.title = `Copy full ${name} ID`;
    button.setAttribute('aria-label', button.title);
    button.innerHTML = '<svg aria-hidden="true" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><rect x="8" y="8" width="12" height="12" rx="2"/><path d="M16 8V5a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h3"/></svg><span class="record-copy-feedback" role="status"></span>';
    wrapper.append(text, button); target.replaceChildren(wrapper);
  }

  function resolve(type, value, permittedIds) {
    const text = String(value || '').trim();
    const match = new RegExp(`^${type === 'RES' ? 'RES' : 'TX'}-([a-f\\d]{8,24})$`, 'i').exec(text);
    if (!match) return text;
    const matches = [...new Set(permittedIds.filter(isObjectId))].filter(id => id.toUpperCase().endsWith(match[1].toUpperCase()));
    if (!matches.length) throw new Error('No accessible record matches this reference. Check it or paste the full ID.');
    if (matches.length > 1) throw new Error('This short reference matches multiple records. Use the copy button to paste the full ID.');
    return matches[0];
  }

  async function copyFullId(value, button) {
    try {
      if (!navigator.clipboard?.writeText) throw new Error('Clipboard unavailable');
      await navigator.clipboard.writeText(value);
      return;
    } catch {
      // Also supports the existing HTTP/IIS deployment without Clipboard API access.
      const input = document.createElement('textarea');
      input.value = value; input.readOnly = true;
      input.style.cssText = 'position:fixed;opacity:0;left:0;top:0;font-size:16px';
      (button.closest('dialog[open]') || document.body).append(input);
      input.select();
      const copied = document.execCommand('copy');
      input.remove(); button.focus({preventScroll:true});
      if (!copied) throw new Error('Copy unavailable. The full ID is available in the reference tooltip.');
    }
  }

  document.addEventListener('click', async event => {
    const button = event.target.closest('[data-copy-record-id]');
    if (!button || button.disabled) return;
    button.disabled = true;
    const feedback = button.querySelector('.record-copy-feedback');
    try {
      await copyFullId(button.dataset.copyRecordId, button);
      feedback.textContent = 'Copied';
      button.title = 'Full ID copied';
    } catch (error) { feedback.textContent = 'Copy failed'; button.title = error.message; }
    finally {
      button.disabled = false;
      setTimeout(() => { feedback.textContent = ''; button.title = `Copy full ${button.dataset.recordType} ID`; }, 1800);
    }
  });
  return { label, renderInto, isObjectId, resolve };
})();
