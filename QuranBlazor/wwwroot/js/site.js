﻿function BlazorScrollToId(id) {
    console.log("Scrolling element id is " + id);
    const element = document.getElementById(id);
    if (element instanceof HTMLElement) {
        element.scrollIntoView({
            behavior: "auto",
            block: "start",
            inline: "center"
        });
    }
}
// When the user clicks the button, the page scrolls to the top
function BlazorScrollToTop() {
    console.log("Scrolling element to top");
    window.scrollTo(0, 0);
}
function saveScrollPosition() {
    const scrollPosition = window.scrollY || document.documentElement.scrollTop;
    console.log("Scroll position that try to save is " + scrollPosition);
    sessionStorage.setItem('scrollPosition', scrollPosition);
    console.log("Scroll position saved " + sessionStorage.getItem('scrollPosition'));
}

function restoreScrollPosition() {
    const scrollPosition = sessionStorage.getItem('scrollPosition');
    console.log("Scroll position that try to restore is " + scrollPosition);
    if (scrollPosition) {
        window.scrollTo(0, parseInt(scrollPosition, 10));
        sessionStorage.removeItem('scrollPosition');
    }
}