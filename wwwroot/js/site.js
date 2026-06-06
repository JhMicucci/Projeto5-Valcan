// Hover Preview System
(function () {
    var hoverTimer = null;
    var currentPopover = null;

    function hideAllPopovers() {
        document.querySelectorAll('.hover-popover.visible').forEach(function (p) {
            p.classList.remove('visible');
        });
        currentPopover = null;
    }

    function positionPopover(popover, wrapper) {
        var rect = wrapper.getBoundingClientRect();
        var popW = 340;
        var popH = 300;

        var left = rect.right + 12;
        var top = rect.top;

        if (left + popW > window.innerWidth) {
            left = rect.left - popW - 12;
        }
        if (left < 0) {
            left = rect.left;
            top = rect.bottom + 8;
        }
        if (top + popH > window.innerHeight) {
            top = window.innerHeight - popH - 10;
        }
        if (top < 10) top = 10;

        popover.style.left = left + 'px';
        popover.style.top = top + 'px';
    }

    function onWrapperEnter(e) {
        var wrapper = e.currentTarget;
        var pop = wrapper.querySelector('.hover-popover');
        if (!pop) return;

        clearTimeout(hoverTimer);
        hideAllPopovers();

        hoverTimer = setTimeout(function () {
            if (pop.dataset.loaded === 'false') {
                pop.dataset.loaded = 'loading';
                var key = pop.dataset.previewKey;
                fetch('/Dashboard/Preview?key=' + encodeURIComponent(key))
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
        }, 400);
    }

    function onWrapperLeave(e) {
        clearTimeout(hoverTimer);
        hoverTimer = null;
        var pop = e.currentTarget.querySelector('.hover-popover');
        if (pop) pop.classList.remove('visible');
        currentPopover = null;
    }

    function initHoverPreviews() {
        document.querySelectorAll('.hover-wrapper').forEach(function (wrapper) {
            // Skip if already initialized - prevents duplicate listeners
            if (wrapper.dataset.hoverInit === 'true') return;
            wrapper.dataset.hoverInit = 'true';

            wrapper.addEventListener('mouseenter', onWrapperEnter);
            wrapper.addEventListener('mouseleave', onWrapperLeave);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initHoverPreviews);
    } else {
        initHoverPreviews();
    }

    window.initHoverPreviews = initHoverPreviews;
})();
