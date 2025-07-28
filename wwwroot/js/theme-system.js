// ===================================
// D'CINE THEME SYSTEM
// ===================================

window.DCineThemeSystem = (function() {
    'use strict';

    // Constants
    const STORAGE_KEYS = {
        THEME: 'dcine-theme-preference'
    };

    const THEMES = {
        LIGHT: 'light',
        DARK: 'dark'
    };

    // State
    let currentTheme = THEMES.LIGHT;
    let isInitialized = false;

    // DOM References
    let themeToggleBtn = null;

    // ===================================
    // THEME MANAGEMENT
    // ===================================

    function initializeTheme() {
        // Get saved theme or detect system preference
        const savedTheme = localStorage.getItem(STORAGE_KEYS.THEME);
        
        if (savedTheme && Object.values(THEMES).includes(savedTheme)) {
            currentTheme = savedTheme;
        } else {
            // Auto-detect system theme preference
            if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
                currentTheme = THEMES.DARK;
            } else {
                currentTheme = THEMES.LIGHT;
            }
        }

        applyTheme(currentTheme);
        updateThemeToggleButton();
    }

    function applyTheme(theme) {
        const html = document.documentElement;
        
        // Add transitioning class to prevent flash
        html.classList.add('theme-transitioning');
        
        // Apply theme
        if (theme === THEMES.DARK) {
            html.setAttribute('data-theme', 'dark');
        } else {
            html.removeAttribute('data-theme');
        }

        currentTheme = theme;
        
        // Save preference
        localStorage.setItem(STORAGE_KEYS.THEME, theme);
        
        // Remove transitioning class after animation
        setTimeout(() => {
            html.classList.remove('theme-transitioning');
        }, 300);

        // Dispatch event for other components
        window.dispatchEvent(new CustomEvent('themeChanged', {
            detail: { theme: theme }
        }));

        console.log(`🎨 Theme switched to: ${theme}`);
    }

    function toggleTheme() {
        const newTheme = currentTheme === THEMES.LIGHT ? THEMES.DARK : THEMES.LIGHT;
        applyTheme(newTheme);
        updateThemeToggleButton();
        
        // Show toast notification
        showThemeChangeNotification(newTheme);
    }

    function updateThemeToggleButton() {
        if (!themeToggleBtn) return;

        const icon = themeToggleBtn.querySelector('i');
        const text = themeToggleBtn.querySelector('.theme-text');
        
        if (currentTheme === THEMES.DARK) {
            if (icon) {
                icon.className = 'fas fa-sun';
            }
            if (text) {
                text.textContent = 'Light Mode';
            }
            themeToggleBtn.setAttribute('title', 'Light Mode');
        } else {
            if (icon) {
                icon.className = 'fas fa-moon';
            }
            if (text) {
                text.textContent = 'Dark Mode';
            }
            themeToggleBtn.setAttribute('title', 'Dark Mode');
        }
    }

    function showThemeChangeNotification(theme) {
        const message = theme === THEMES.DARK ? 'Dark Mode' : 'Light Mode';
        showToast(`🎨 Theme: ${message}`, 'success');
    }



    // ===================================
    // UTILITY FUNCTIONS
    // ===================================

    function getLocalizedText(key) {
        // This would ideally get text from server-side localization
        // For now, provide fallback translations
        const translations = {
            [LANGUAGES.VI]: {
                'Theme': 'Giao diện',
                'DarkMode': 'Chế độ tối',
                'LightMode': 'Chế độ sáng',
                'Language': 'Ngôn ngữ',
                'Settings': 'Cài đặt'
            },
            [LANGUAGES.EN]: {
                'Theme': 'Theme',
                'DarkMode': 'Dark Mode',
                'LightMode': 'Light Mode',
                'Language': 'Language',
                'Settings': 'Settings'
            }
        };

        return translations[currentLanguage]?.[key] || key;
    }

    function showToast(message, type = 'info') {
        // Create toast element if doesn't exist
        let toastContainer = document.getElementById('toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toast-container';
            toastContainer.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                z-index: 9999;
            `;
            document.body.appendChild(toastContainer);
        }

        const toast = document.createElement('div');
        toast.className = `alert alert-${type} alert-dismissible fade show`;
        toast.style.cssText = `
            margin-bottom: 10px;
            min-width: 300px;
            box-shadow: var(--shadow-lg);
        `;
        
        toast.innerHTML = `
            <div>${message}</div>
            <button type="button" class="btn-close" aria-label="Close"></button>
        `;

        toastContainer.appendChild(toast);

        // Auto dismiss
        setTimeout(() => {
            toast.remove();
        }, 3000);

        // Manual dismiss
        const closeBtn = toast.querySelector('.btn-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => toast.remove());
        }
    }

    function bindEventListeners() {
        // Theme toggle button
        themeToggleBtn = document.querySelector('.theme-toggle-btn');
        if (themeToggleBtn) {
            themeToggleBtn.addEventListener('click', (e) => {
                e.preventDefault();
                toggleTheme();
            });
        }



        // System theme change detection
        if (window.matchMedia) {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            mediaQuery.addEventListener('change', (e) => {
                // Only auto-switch if user hasn't manually set a preference
                const savedTheme = localStorage.getItem(STORAGE_KEYS.THEME);
                if (!savedTheme) {
                    const newTheme = e.matches ? THEMES.DARK : THEMES.LIGHT;
                    applyTheme(newTheme);
                    updateThemeToggleButton();
                }
            });
        }

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            // Ctrl+Shift+T for theme toggle
            if (e.ctrlKey && e.shiftKey && e.key === 'T') {
                e.preventDefault();
                toggleTheme();
            }
        });
    }

    // ===================================
    // PUBLIC API
    // ===================================

    function initialize() {
        if (isInitialized) {
            console.warn('DCineThemeSystem is already initialized');
            return;
        }

        console.log('🎨 Initializing D\'Cine Theme System...');
        
        initializeTheme();
        bindEventListeners();
        
        isInitialized = true;
        
        console.log('✅ D\'Cine Theme System initialized successfully');
        console.log(`Current theme: ${currentTheme}`);
    }

    function getCurrentTheme() {
        return currentTheme;
    }

    function setTheme(theme) {
        if (Object.values(THEMES).includes(theme)) {
            applyTheme(theme);
            updateThemeToggleButton();
        }
    }

    // ===================================
    // AUTO INITIALIZATION
    // ===================================

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        // DOM is already ready
        setTimeout(initialize, 0);
    }

    // Public API
    return {
        initialize,
        getCurrentTheme,
        setTheme,
        toggleTheme,
        THEMES
    };

})();

// ===================================
// INTEGRATION WITH EXISTING SITE.JS
// ===================================

// Extend existing DCineApp object if it exists
if (window.DCineApp) {
    window.DCineApp.ThemeSystem = window.DCineThemeSystem;
    
    // Add theme-aware formatting functions
    window.DCineApp.getThemeClass = function(baseClass) {
        const theme = window.DCineThemeSystem.getCurrentTheme();
        return `${baseClass} ${baseClass}--${theme}`;
    };
    
    window.DCineApp.isLightTheme = function() {
        return window.DCineThemeSystem.getCurrentTheme() === window.DCineThemeSystem.THEMES.LIGHT;
    };
    
    window.DCineApp.isDarkTheme = function() {
        return window.DCineThemeSystem.getCurrentTheme() === window.DCineThemeSystem.THEMES.DARK;
    };
}

// Global shortcut functions
window.toggleTheme = function() {
    window.DCineThemeSystem.toggleTheme();
};

console.log('🚀 Theme System JavaScript loaded successfully!'); 