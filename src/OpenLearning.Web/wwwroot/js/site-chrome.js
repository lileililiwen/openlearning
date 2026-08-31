/* Sidebar + topbar chrome behavior, shared UI feedback, and progressive enhancement. */
(function () {
    'use strict';

    // --- Sidebar chrome -------------------------------------------------
    var sidebar = document.getElementById('appSidebar');
    if (sidebar) {
        var openBtn = document.querySelector('[data-sidebar-open]');
        var focusableSelector = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

        function openSidebar() {
            sidebar.classList.add('open');
            document.body.classList.add('sidebar-open');
            if (openBtn) openBtn.setAttribute('aria-expanded', 'true');
            var first = sidebar.querySelector(focusableSelector);
            if (first) first.focus();
        }

        function closeSidebar() {
            sidebar.classList.remove('open');
            document.body.classList.remove('sidebar-open');
            if (openBtn) {
                openBtn.setAttribute('aria-expanded', 'false');
                openBtn.focus();
            }
        }

        sidebar.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && sidebar.classList.contains('open')) {
                closeSidebar();
                return;
            }
            if (e.key === 'Tab' && sidebar.classList.contains('open')) {
                var focusable = sidebar.querySelectorAll(focusableSelector);
                if (focusable.length === 0) return;
                var first = focusable[0];
                var last = focusable[focusable.length - 1];
                if (e.shiftKey && document.activeElement === first) {
                    e.preventDefault();
                    last.focus();
                } else if (!e.shiftKey && document.activeElement === last) {
                    e.preventDefault();
                    first.focus();
                }
            }
        });

        document.querySelectorAll('[data-sidebar-open]').forEach(function (el) {
            el.addEventListener('click', openSidebar);
        });
        document.querySelectorAll('[data-sidebar-close]').forEach(function (el) {
            el.addEventListener('click', closeSidebar);
        });

        // Persist group collapse state.
        document.querySelectorAll('.nav-group-toggle').forEach(function (toggle) {
            toggle.addEventListener('click', function () {
                var group = toggle.closest('.nav-group');
                var key = toggle.getAttribute('data-group-key');
                var willCollapse = !group.classList.contains('collapsed');
                toggle.setAttribute('aria-expanded', willCollapse ? 'false' : 'true');
                group.classList.toggle('collapsed', willCollapse);
                fetch('/nav/toggle?group=' + encodeURIComponent(key), {
                    method: 'POST',
                    credentials: 'same-origin'
                }).catch(function () { /* best-effort */ });
            });
        });
    }

    // --- Global toast auto-dismiss --------------------------------------
    var toast = document.getElementById('global-toast');
    if (toast) {
        setTimeout(function () {
            var closeBtn = toast.querySelector('[data-bs-dismiss="alert"]');
            if (closeBtn) {
                closeBtn.click();
            }
        }, 5000);
    }

    // --- Programmatic toast (used by progressive-enhancement responses) ---
    function showToast(message, type) {
        type = type || 'success';
        var host = document.createElement('div');
        host.innerHTML =
            '<div class="alert alert-' + type + ' alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3 shadow" ' +
            'role="status" aria-live="polite" style="z-index: 1080; min-width: 280px; max-width: 90vw;">' +
            message +
            '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>' +
            '</div>';
        var el = host.firstElementChild;
        document.body.appendChild(el);
        setTimeout(function () {
            var closeBtn = el.querySelector('[data-bs-dismiss="alert"]');
            if (closeBtn) {
                closeBtn.click();
            }
        }, 5000);
    }

    // --- Destructive-action confirmation --------------------------------
    // Any element carrying [data-confirm] must be confirmed before its
    // action proceeds. Forms submit is intercepted; anchors navigate only
    // after confirmation (original href preserved in data-confirm-href).
    document.addEventListener('submit', function (e) {
        var form = e.target.closest('form[data-confirm]');
        if (form && !window.confirm(form.getAttribute('data-confirm'))) {
            e.preventDefault();
        }
    });
    document.addEventListener('click', function (e) {
        var trigger = e.target.closest('a[data-confirm]');
        if (!trigger) {
            return;
        }
        e.preventDefault();
        if (window.confirm(trigger.getAttribute('data-confirm'))) {
            var href = trigger.getAttribute('data-confirm-href') || trigger.getAttribute('href');
            if (href && href !== '#') {
                window.location.href = href;
            }
        }
    });

    // --- Progressive enhancement for in-page actions --------------------
    // A form marked [data-pe] posts via fetch and swaps its [data-pe-target]
    // region with the returned HTML instead of reloading the page. The original
    // <form method="post"> is retained so the action still works without JS.
    document.addEventListener('submit', function (e) {
        var form = e.target.closest('form[data-pe]');
        if (!form) {
            return;
        }
        e.preventDefault();

        var target = document.querySelector(form.getAttribute('data-pe-target') || '#' + form.id);
        var button = form.querySelector('button[type="submit"]');
        var originalHtml = button ? button.innerHTML : null;
        if (button) {
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';
        }

        fetch(form.action, {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            body: new FormData(form)
        })
            .then(function (resp) {
                if (!resp.ok) {
                    throw new Error('bad-status');
                }
                if (target) {
                    return resp.text().then(function (html) {
                        target.innerHTML = html;
                    });
                }
                return resp.text();
            })
            .then(function () {
                if (button) {
                    button.disabled = false;
                    button.innerHTML = originalHtml;
                }
            })
            .catch(function () {
                if (button) {
                    button.disabled = false;
                    button.innerHTML = originalHtml;
                }
                showToast('Action failed. Please try again.', 'danger');
            });
    });
})();
