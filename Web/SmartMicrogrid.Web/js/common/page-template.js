/* One title, description and action area for every workspace page.
 * Move existing nodes so IDs, role-controlled actions and live values survive. */
function renderPageIntro(content, activePage, pageTitle) {
  const source = content?.querySelector('[data-page-intro]');
  if (!content || !source) return;

  const group = activePage.startsWith('m4-') || activePage === 'users' ? 'administration'
    : ['transactions', 'history', 'm3-dashboard', 'reservations'].includes(activePage) ? 'trading'
    : activePage === 'battery' ? 'storage' : 'generation';
  const imagery = {
    administration: ['bg_energy_home.jpg', 'Community operations'],
    trading: ['img_battery_storage.jpg', 'Connected energy'],
    storage: ['img_battery_storage.jpg', 'Energy storage'],
    generation: ['img_solar_farm.jpg', 'Solar network']
  };
  const [filename, caption] = imagery[group];
  const inner = source.querySelector('.hero-image-overlay') || source;
  const originalHeading = inner.querySelector('h1, h2');
  const description = inner.querySelector('p');
  const heading = document.createElement('h1');
  if (originalHeading) {
    // Dynamic names (overview greeting, microgrid name, role-specific reservations)
    // retain their content and IDs; all other pages use their canonical route title.
    if (originalHeading.id || originalHeading.querySelector('[id]')) {
      heading.append(...originalHeading.childNodes);
    } else heading.textContent = pageTitle;
    if (originalHeading.id) heading.id = originalHeading.id;
    originalHeading.remove();
  } else heading.textContent = pageTitle;
  heading.className = 'page-intro-title';

  inner.querySelectorAll('.eyebrow, .hero-eyebrow, .reservation-eyebrow').forEach(eyebrow => {
    if (eyebrow.id) eyebrow.hidden = true;
    else eyebrow.remove();
  });
  inner.querySelectorAll('img').forEach(img => img.remove());

  const intro = document.createElement('section');
  intro.className = 'page-intro';
  intro.setAttribute('aria-label', pageTitle);
  const body = document.createElement('div');
  body.className = 'page-intro-body';
  const category = document.createElement('span');
  category.className = 'page-intro-category';
  category.textContent = group === 'trading' ? 'Energy marketplace' : caption;
  body.append(category, heading);
  if (description) {
    description.className = 'page-intro-description';
    description.removeAttribute('style');
    body.append(description);
  }

  // Remove empty layout wrappers, preserving any nodes used by page controllers.
  const emptyWrappers = Array.from(inner.querySelectorAll('div')).reverse();
  emptyWrappers.forEach(node => {
    if (!node.id && !node.textContent.trim() && !node.querySelector('a, button, input, [id]')) node.remove();
  });
  const actions = document.createElement('div');
  actions.className = 'page-intro-actions';
  actions.append(...inner.childNodes);
  body.append(actions);

  const photo = document.createElement('figure');
  photo.className = 'page-intro-photo';
  const image = document.createElement('img');
  image.src = getWebAppUrl(`assets/images/${filename}`);
  image.alt = ''; // Contextual decoration; the page text carries the information.
  image.width = 640;
  image.height = 360;
  image.decoding = 'async';
  const label = document.createElement('figcaption');
  label.textContent = caption;
  photo.append(image, label);
  intro.append(body, photo);
  source.remove();
  content.prepend(intro);
}
