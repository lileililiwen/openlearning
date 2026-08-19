/* Sidebar + topbar chrome behavior. */
(function () {
    'use strict';
    var sidebar = document.getElementById('appSidebar');
    if (!sidebar) {
        return;
    }

    function openSidebar() {
        sidebar.classList.add('open');
        document.body.classList.add('sidebar-open');
    }

    function closeSidebar() {
        sidebar.classList.remove('open');
        document.body.classList.remove('sidebar-open');
    }

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
})();
