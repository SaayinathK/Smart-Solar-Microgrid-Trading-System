/* ==========================================================================
   Smart Microgrid Energy System - Theme Switcher (Light / Dark Mode)
   ========================================================================== */

const ThemeManager = {
  THEME_KEY: 'smart_microgrid_theme',

  init() {
    const savedTheme = localStorage.getItem(this.THEME_KEY) || 'light';
    this.applyTheme(savedTheme);
  },

  getTheme() {
    return localStorage.getItem(this.THEME_KEY) || 'light';
  },

  applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem(this.THEME_KEY, theme);
    this.updateToggleIcon(theme);
  },

  toggleTheme() {
    const currentTheme = this.getTheme();
    const newTheme = currentTheme === 'light' ? 'dark' : 'light';
    this.applyTheme(newTheme);
  },

  updateToggleIcon(theme) {
    const btn = document.getElementById('theme-toggle-btn');
    if (!btn) return;
    
    if (theme === 'dark') {
      btn.innerHTML = `
        <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" width="18" height="18">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"></path>
        </svg>
        <span>Light Mode</span>
      `;
    } else {
      btn.innerHTML = `
        <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" width="18" height="18">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"></path>
        </svg>
        <span>Dark Mode</span>
      `;
    }
  }
};

// Initialize theme immediately to prevent white/dark flicker
ThemeManager.init();
