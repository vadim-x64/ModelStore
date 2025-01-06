function checkVisibility() {
    const emptySection = document.querySelector('.empty-section');
    const lineContainer = document.querySelector('.line-container');
    const gifContainer = document.querySelector('.gif-container');
    const emptyText = document.querySelector('.empty-text');
    const rect = emptySection.getBoundingClientRect();
    const isVisible = rect.top <= window.innerHeight && rect.bottom >= 0;
    if (isVisible) {
        emptySection.style.opacity = 1;
        lineContainer.style.opacity = 1;
        lineContainer.style.transform = 'scaleX(1)';
        gifContainer.style.opacity = 1;
        emptyText.style.opacity = 1;
    }
}
window.addEventListener('scroll', checkVisibility);
document.addEventListener('DOMContentLoaded', checkVisibility);