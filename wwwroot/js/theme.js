/**
 * PicoPlus Design System — Theme Engine
 * 
 * Responsibilities:
 *  1. Detect and apply initial theme (light / dark / telegram)
 *  2. Integrate with Telegram WebApp (TMA) when present
 *  3. Map TMA color_scheme changes → data-theme attribute
 *  4. Persist preference to localStorage
 *  5. Expose window.picoTheme API for Blazor interop
 *
 * Usage from Blazor:
 *   window.picoTheme.setTheme('dark');
 *   window.picoTheme.getTheme();
 *   window.picoTheme.isTelegram();
 */

(function () {
    'use strict';

    const STORAGE_KEY  = 'picoplus-theme';
    const ATTR         = 'data-theme';
    const ROOT         = document.documentElement;

    // ── Helpers ────────────────────────────────────────────────────
    function isTelegramWebApp() {
        try {
            return !!(
                window.Telegram &&
                window.Telegram.WebApp &&
                window.Telegram.WebApp.initData
            );
        } catch {
            return false;
        }
    }

    function getTelegramColorScheme() {
        try {
            return window.Telegram.WebApp.colorScheme || 'light';
        } catch {
            return 'light';
        }
    }

    function applyTelegramSchemeCssClass(scheme) {
        ROOT.classList.remove('tg-light', 'tg-dark');
        ROOT.classList.add(scheme === 'dark' ? 'tg-dark' : 'tg-light');
    }

    // ── Core theme apply ───────────────────────────────────────────
    function applyTheme(theme) {
        ROOT.setAttribute(ATTR, theme);

        if (theme === 'telegram') {
            applyTelegramSchemeCssClass(getTelegramColorScheme());
        }

        // Notify meta theme-color (PWA / browser chrome)
        var metaTheme = document.querySelector('meta[name="theme-color"]');
        if (metaTheme) {
            var bg = getComputedStyle(ROOT).getPropertyValue('--bg-surface').trim();
            if (bg) metaTheme.setAttribute('content', bg);
        }
    }

    // ── Initial resolution ─────────────────────────────────────────
    function resolveInitialTheme() {
        // 1. If running inside Telegram → always use telegram theme
        if (isTelegramWebApp()) {
            return 'telegram';
        }

        // 2. Stored user preference
        var stored = null;
        try { stored = localStorage.getItem(STORAGE_KEY); } catch {}
        if (stored && ['light', 'dark', 'telegram'].includes(stored)) {
            return stored;
        }

        // 3. OS preference
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }

        return 'light';
    }

    // ── Telegram integration ───────────────────────────────────────
    function initTelegramTheme() {
        var twa = window.Telegram.WebApp;

        // Signal that app is ready
        try { twa.ready(); } catch {}

        // Expand to full height
        try { twa.expand(); } catch {}

        // Listen for dynamic theme changes (user switches system theme)
        try {
            twa.onEvent('themeChanged', function () {
                applyTelegramSchemeCssClass(getTelegramColorScheme());
                // Re-inject TG CSS vars if needed (handled by browser natively)
            });
        } catch {}
    }

    // ── OS dark mode listener ──────────────────────────────────────
    function watchOsTheme() {
        if (!window.matchMedia) return;
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
            // Only auto-switch if user hasn't pinned a preference
            var stored = null;
            try { stored = localStorage.getItem(STORAGE_KEY); } catch {}
            if (!stored) {
                applyTheme(e.matches ? 'dark' : 'light');
            }
        });
    }

    // ── Public API ─────────────────────────────────────────────────
    window.picoTheme = {
        /**
         * Set the active theme.
         * @param {'light'|'dark'|'telegram'} theme
         * @param {boolean} [persist=true] — save to localStorage
         */
        setTheme: function (theme, persist) {
            if (!['light', 'dark', 'telegram'].includes(theme)) {
                console.warn('[PicoTheme] Unknown theme:', theme);
                return;
            }
            applyTheme(theme);
            if (persist !== false) {
                try { localStorage.setItem(STORAGE_KEY, theme); } catch {}
            }
        },

        /** @returns {'light'|'dark'|'telegram'} */
        getTheme: function () {
            return ROOT.getAttribute(ATTR) || 'light';
        },

        /** @returns {boolean} */
        isTelegram: function () {
            return isTelegramWebApp();
        },

        /**
         * Toggle between light and dark.
         * Useful for a theme toggle button.
         */
        toggle: function () {
            var current = this.getTheme();
            this.setTheme(current === 'dark' ? 'light' : 'dark');
        },

        /**
         * Called from Blazor after navigation to re-evaluate
         * in case the page context changed.
         */
        refresh: function () {
            var current = ROOT.getAttribute(ATTR);
            if (current) applyTheme(current);
        }
    };

    // ── Bootstrap ──────────────────────────────────────────────────
    var initialTheme = resolveInitialTheme();
    applyTheme(initialTheme);

    if (initialTheme === 'telegram' && isTelegramWebApp()) {
        initTelegramTheme();
    }

    watchOsTheme();

    // Expose for Blazor JS interop
    window.picoThemeSetTheme  = function (t) { window.picoTheme.setTheme(t); };
    window.picoThemeGetTheme  = function ()  { return window.picoTheme.getTheme(); };
    window.picoThemeToggle    = function ()  { window.picoTheme.toggle(); };
    window.picoThemeIsTelegram = function () { return window.picoTheme.isTelegram(); };

})();
