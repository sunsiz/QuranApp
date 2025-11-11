function BlazorScrollToId(id) {
    console.log("Scrolling element id is " + id);
    const element = document.getElementById(id);
    if (element instanceof HTMLElement) {
        element.scrollIntoView({
            behavior: "auto", // "smooth" can sometimes interfere with subsequent JS if not awaited, "auto" is safer
            block: "start",
            inline: "nearest" // "center" might not always be what's desired for top item
        });
    }
}
// When the user clicks the button, the page scrolls to the top
function BlazorScrollToTop() {
    console.log("Scrolling element to top");
    window.scrollTo({ top: 0, behavior: 'auto' }); // Use object for consistency, behavior auto
}
function saveScrollPosition() {
    // Only save if on a page that needs general scroll saving (e.g., not AyaList if it uses fragments)
    // Or make this generic and let AyaList decide not to call it.
    // For now, keeping it generic.
    const scrollPosition = window.scrollY || document.documentElement.scrollTop;
    console.log("Scroll position that try to save is " + scrollPosition + " for page " + window.location.pathname);
    sessionStorage.setItem('scrollPosition-' + window.location.pathname + window.location.search, scrollPosition); // Store per-URL
    // console.log("Scroll position saved " + sessionStorage.getItem('scrollPosition-' + window.location.pathname + window.location.search));
}

function restoreScrollPosition() {
    const key = 'scrollPosition-' + window.location.pathname + window.location.search;
    const scrollPosition = sessionStorage.getItem(key);
    console.log("Scroll position that try to restore is " + scrollPosition + " for page " + window.location.pathname);
    if (scrollPosition) {
        window.scrollTo(0, parseInt(scrollPosition, 10));
        // sessionStorage.removeItem(key); // Consider removing only if it's a one-time restore per load.
        // Keeping it allows restore on soft reloads/re-renders if Blazor doesn't re-trigger full load.
        // For back/forward, browser might handle it. If Blazor re-renders, this helps.
    }
}


function getTopVisibleAyaIdForAyaList() {
    if (!window.location.pathname.endsWith('/ayalist')) {
        return null;
    }
    const ayas = Array.from(document.querySelectorAll('.row.my-3[id]'));
    if (!ayas.length) return null;

    let bestCandidate = null;
    // A small offset from the top of the viewport. If an Aya's top is within this, it's a good candidate.
    const viewportTopThreshold = 20; // pixels. Consider Ayas whose top is at or just above/below viewport top.

    for (const aya of ayas) {
        const rect = aya.getBoundingClientRect();
        // Check if the Aya is at all visible and its top is near the viewport top.
        if (rect.bottom > 0 && rect.top < window.innerHeight) { // Element is at least partially visible
            if (bestCandidate === null) { // First visible element
                bestCandidate = aya;
            }
            // If this element is closer to the viewportTopThreshold (from above or below) than the current bestCandidate
            if (Math.abs(rect.top) < Math.abs(bestCandidate.getBoundingClientRect().top)) {
                bestCandidate = aya;
            }
            // If an element is fully visible and very close to the top, prefer it.
            if (rect.top >= 0 && rect.top <= viewportTopThreshold) {
                bestCandidate = aya;
                break; // Found a good candidate starting in viewport near top
            }
        }
    }
    return bestCandidate ? bestCandidate.id : null;
}

function updateUrlWithTopVisibleAyaFragmentForAyaList() {
    if (!window.location.pathname.endsWith('/ayalist')) {
        return;
    }
    const topAyaId = getTopVisibleAyaIdForAyaList();
    if (topAyaId) {
        const currentUrl = new URL(window.location.href);
        if (currentUrl.hash !== `#${topAyaId}`) {
            currentUrl.hash = topAyaId;
            history.replaceState(null, '', currentUrl.toString());
            console.log('AyaList: Replaced URL with fragment: #' + topAyaId);
        }
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

// Initialize keyboard navigation on load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', enableKeyboardNavigation);
} else {
    enableKeyboardNavigation();
}
