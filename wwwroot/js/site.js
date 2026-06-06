// Hover Preview System
(function () {
    var hoverTimer = null;
    var currentPopover = null;

    function positionPopover(popover, wrapper) {
        var rect = wrapper.getBoundingClientRect();
        var popW = 340;
        var popH = 300;

        // Try right side first
        var left = rect.right + 12;
        var top = rect.top;

        // If overflows right, show on left
        if (left + popW > window.innerWidth) {
            left = rect.left - popW - 12;
        }
        // If still overflows, show below
        if (left < 0) {
            left = rect.left;
            top = rect.bottom + 8;
        }
        // Prevent going below viewport
        if (top + popH > window.innerHeight) {
            top = window.innerHeight - popH - 10;
        }
        if (top < 10) top = 10;

        popover.style.left = left + 'px';
        popover.style.top = top + 'px';
    }

    function initHoverPreviews() {
        document.querySelectorAll('.hover-wrapper').forEach(function (wrapper) {
            wrapper.addEventListener('mouseenter', function () {
                var pop = wrapper.querySelector('.hover-popover');
                if (!pop) return;

                hoverTimer = setTimeout(function () {
                    // Load content if not loaded yet
                    if (pop.dataset.loaded === 'false') {
                        pop.dataset.loaded = 'loading';
                        var key = pop.dataset.previewKey;
                        var url = '/Dashboard/Preview?key=' + encodeURIComponent(key);
                        fetch(url)
                            .then(function(r) { return r.text(); })
                            .then(function(html) {
                                pop.innerHTML = html;
                                pop.dataset.loaded = 'true';
                            })
                            .catch(function() {
                                pop.innerHTML = '<div class="preview-card" style="padding:12px;color:#8892b0;font-size:0.8rem;">Erro ao carregar</div>';
                                pop.dataset.loaded = 'false';
                            });
                    }

                    positionPopover(pop, wrapper);
                    pop.classList.add('visible');
                    currentPopover = pop;
                }, 400); // 400ms delay before showing
            });

            wrapper.addEventListener('mouseleave', function () {
                clearTimeout(hoverTimer);
                var pop = wrapper.querySelector('.hover-popover');
                if (pop) {
                    pop.classList.remove('visible');
                }
                currentPopover = null;
            });
        });
    }

    // Init on page load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initHoverPreviews);
    } else {
        initHoverPreviews();
    }

    // Re-init after AJAX refresh
    window.initHoverPreviews = initHoverPreviews;
})();
