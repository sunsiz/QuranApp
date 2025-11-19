function BlazorScrollToId(id) {
    const element = document.getElementById(id);
    if (element instanceof HTMLElement) {
        element.scrollIntoView({
            behavior: "auto",
            block: "start",
            inline: "nearest"
        });
    }
}

// Try to scroll to element, returns true if successful, false if element not found
function tryScrollToElement(id) {
    const element = document.getElementById(id);
    if (element instanceof HTMLElement) {
        element.scrollIntoView({
            behavior: "auto",
            block: "start",
            inline: "nearest"
        });
        return true;
    }
    return false;
}

// Scroll to top of page
function BlazorScrollToTop() {
    window.scrollTo({ top: 0, behavior: 'auto' });
}

// Save scroll position to sessionStorage
function saveScrollPosition() {
    const scrollPosition = window.scrollY || document.documentElement.scrollTop;
    console.log('=== SAVING SCROLL POSITION ===');
    console.log('Current URL:', window.location.href);
    console.log('Scroll position:', scrollPosition);
    
    // Don't save if we're at the very top - likely just navigated here
    if (scrollPosition < 50) {
        console.log('Scroll position too low (< 50px), skipping save to avoid overwriting');
        return;
    }
    
    // For AyaList pages, find the top visible Aya and save it
    if (window.location.pathname.endsWith('/AyaList')) {
        const topAyaId = getTopVisibleAyaId();
        console.log('Top visible Aya ID:', topAyaId);
        
        if (topAyaId) {
            // Save with the Aya ID we're currently viewing
            const keyWithAya = window.location.pathname + window.location.search + '#' + topAyaId;
            sessionStorage.setItem('scrollPosition-' + keyWithAya, scrollPosition);
            console.log('Saved as:', keyWithAya, '→', scrollPosition);
            return;
        }
    }
    
    // Default: save by pathname + search
    const key = 'scrollPosition-' + window.location.pathname + window.location.search;
    sessionStorage.setItem(key, scrollPosition);
    console.log('Saved (default) as:', key, '→', scrollPosition);
}

// Get the top visible Aya ID on AyaList page
function getTopVisibleAyaId() {
    if (!window.location.pathname.endsWith('/AyaList')) {
        console.log('Not on AyaList page, path is:', window.location.pathname);
        return null;
    }
    
    const ayas = document.querySelectorAll('.row.my-3[id]');
    console.log('Found', ayas.length, 'ayas on the page');
    if (!ayas.length) return null;

    // Find the Aya that's currently at the top of the viewport
    for (const aya of ayas) {
        const rect = aya.getBoundingClientRect();
        // If this Aya is visible in viewport (top is within screen)
        if (rect.top >= 0 && rect.top < window.innerHeight) {
            console.log('Found top visible aya:', aya.id, 'at top:', rect.top);
            return aya.id;
        }
        // Or if we scrolled past it but it's the closest one
        if (rect.bottom > 0) {
            console.log('Found aya in view:', aya.id, 'bottom:', rect.bottom);
            return aya.id;
        }
    }
    
    console.log('Defaulting to first aya:', ayas[0]?.id);
    return ayas[0]?.id || null;
}

// Restore scroll position from sessionStorage
function restoreScrollPosition() {
    console.log('=== RESTORING SCROLL POSITION ===');
    console.log('Current URL:', window.location.href);
    
    // Try with current URL including fragment first
    let key = 'scrollPosition-' + window.location.pathname + window.location.search + window.location.hash;
    let scrollPosition = sessionStorage.getItem(key);
    console.log('Trying key with hash:', key, '→', scrollPosition);
    
    // If not found, try without fragment
    if (!scrollPosition) {
        key = 'scrollPosition-' + window.location.pathname + window.location.search;
        scrollPosition = sessionStorage.getItem(key);
        console.log('Trying key without hash:', key, '→', scrollPosition);
    }
    
    if (scrollPosition) {
        console.log('Restoring to position:', scrollPosition);
        window.scrollTo(0, parseInt(scrollPosition, 10));
    } else {
        console.log('No saved position found');
    }
}

// Keyboard navigation helper for buttons
function enableKeyboardNavigation() {
    // Add Enter/Space key handlers to elements with role="button"
    document.addEventListener('keydown', function (event) {
        const target = event.target;
        
        // Handle Space/Enter on elements with role="button" that aren't actual buttons
        if (target.getAttribute('role') === 'button' && 
            (event.key === 'Enter' || event.key === ' ')) {
            event.preventDefault();
            target.click();
        }
        
        // Handle Escape key to close modals if present
        if (event.key === 'Escape') {
            const modal = document.querySelector('.modal.show');
            if (modal) {
                const closeBtn = modal.querySelector('.btn-close, [data-bs-dismiss="modal"]');
                if (closeBtn) closeBtn.click();
            }
        }
    });
    
    // Improve focus visibility for keyboard navigation
    document.addEventListener('keydown', function(event) {
        if (event.key === 'Tab') {
            document.body.classList.add('keyboard-navigation');
        }
    });
    
    document.addEventListener('mousedown', function() {
        document.body.classList.remove('keyboard-navigation');
    });
}

(function () {
    if (window.quranScrollInterop) {
        return;
    }

    function buildPredicate(options) {
        if (!options) {
            return () => true;
        }

        if (options.selector) {
            const selector = options.selector;
            return anchor => anchor.matches && anchor.matches(selector);
        }

        if (options.hrefContains) {
            const fragment = options.hrefContains;
            return anchor => !!anchor.href && anchor.href.includes(fragment);
        }

        if (options.hrefStartsWith) {
            const prefix = options.hrefStartsWith;
            return anchor => !!anchor.href && anchor.href.startsWith(prefix);
        }

        return () => true;
    }

    const scrollInterop = {
        handler: null,
        registerScrollSaver(options) {
            this.unregisterScrollSaver();
            const predicate = buildPredicate(options);
            this.handler = function (event) {
                const anchor = event.target?.closest?.('a');
                if (!anchor || !predicate(anchor)) {
                    return;
                }
                saveScrollPosition();
            };
            document.addEventListener('click', this.handler);
        },
        registerAyaListLinkSaver() {
            this.registerScrollSaver({ hrefContains: '/AyaList' });
        },
        hasSavedPosition(url) {
            if (!url) {
                return false;
            }

            try {
                return sessionStorage.getItem('scrollPosition-' + url) !== null;
            } catch (error) {
                console.error('Unable to access sessionStorage', error);
                return false;
            }
        },
        getSavedPosition(url) {
            if (!url) {
                return null;
            }

            try {
                const value = sessionStorage.getItem('scrollPosition-' + url);
                return value ? parseInt(value, 10) : null;
            } catch (error) {
                console.error('Unable to read sessionStorage', error);
                return null;
            }
        },
        unregisterScrollSaver() {
            if (!this.handler) {
                return;
            }
            document.removeEventListener('click', this.handler);
            this.handler = null;
        }
    };

    window.quranScrollInterop = scrollInterop;
})();

// Initialize keyboard navigation on load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', enableKeyboardNavigation);
} else {
    enableKeyboardNavigation();
}

