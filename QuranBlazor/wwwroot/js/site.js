function BlazorScrollToId(id) {
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
