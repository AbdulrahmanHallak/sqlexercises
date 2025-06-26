// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

/**
 * Enables zoom and pan functionality for an image inside a container.
 * @param {HTMLImageElement} img - The image element to enable zoom on.
 * @param {HTMLElement} container - The container element for the image.
 * @param {number} scale - The zoom scale (default 1.5)
 */
function enableImageZoom(img, container, scale = 1.5) {
    let isZoomed = false;
    let offsetX = 0, offsetY = 0;
    let startX = 0, startY = 0;
    let isDragging = false;
    let moved = false;

    function clamp() {
        const c = container.getBoundingClientRect();
        const i = img.getBoundingClientRect();
        const maxX = (i.width - c.width) / 2;
        const maxY = (i.height - c.height) / 2;
        offsetX = Math.max(-maxX, Math.min(maxX, offsetX));
        offsetY = Math.max(-maxY, Math.min(maxY, offsetY));
    }

    img.addEventListener('click', function (e) {
        if (moved) { moved = false; return; }
        const rect = img.getBoundingClientRect();
        const clickX = e.clientX - rect.left;
        const clickY = e.clientY - rect.top;
        if (!isZoomed) {
            const cx = (clickX / rect.width - 0.5) * (scale - 1) * rect.width;
            const cy = (clickY / rect.height - 0.5) * (scale - 1) * rect.height;
            offsetX = -cx; offsetY = -cy;
            clamp();
            img.style.transform = `scale(${scale}) translate(${offsetX}px, ${offsetY}px)`;
            img.style.cursor = 'grab';
            isZoomed = true;
        } else {
            offsetX = offsetY = 0;
            img.style.transform = `scale(1) translate(0,0)`;
            img.style.cursor = 'zoom-in';
            isZoomed = false;
        }
    });

    img.addEventListener('mousedown', function (e) {
        if (!isZoomed) return;
        isDragging = true;
        moved = false;
        img.style.cursor = 'grabbing';
        startX = e.clientX - offsetX;
        startY = e.clientY - offsetY;
        e.preventDefault();
    });

    window.addEventListener('mousemove', function (e) {
        if (!isDragging) return;
        const dx = e.clientX - (startX + offsetX);
        const dy = e.clientY - (startY + offsetY);
        if (!moved && (Math.abs(dx) > 3 || Math.abs(dy) > 3)) moved = true;
        offsetX = e.clientX - startX;
        offsetY = e.clientY - startY;
        clamp();
        img.style.transform = `scale(${scale}) translate(${offsetX}px, ${offsetY}px)`;
    });

    window.addEventListener('mouseup', function () {
        if (!isZoomed) return;
        isDragging = false;
        img.style.cursor = 'grab';
    });

    img.addEventListener('dragstart', function (e) { e.preventDefault(); });

    // Set initial cursor
    img.style.cursor = 'zoom-in';
}
