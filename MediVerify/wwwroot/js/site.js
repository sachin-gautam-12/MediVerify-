(function () {
    'use strict';

    // ---------- Theme ----------
    const root = document.documentElement;
    const savedTheme = localStorage.getItem('mv-theme');
    if (savedTheme) root.setAttribute('data-theme', savedTheme);

    window.toggleTheme = function () {
        const next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        root.setAttribute('data-theme', next);
        localStorage.setItem('mv-theme', next);
        document.querySelectorAll('[data-theme-icon]').forEach(function (el) {
            el.className = next === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
        });
    };

    // ---------- Sidebar ----------
    window.toggleSidebar = function () {
        document.body.classList.toggle('sidebar-open');
    };

    document.addEventListener('click', function (e) {
        const backdrop = e.target.closest('.mv-sidebar-backdrop');
        if (backdrop) document.body.classList.remove('sidebar-open');
    });

    // ---------- Table search ----------
    document.querySelectorAll('[data-table-search]').forEach(function (input) {
        const table = document.querySelector(input.getAttribute('data-table-search'));
        if (!table) return;
        input.addEventListener('input', function () {
            const q = input.value.toLowerCase().trim();
            table.querySelectorAll('tbody tr').forEach(function (row) {
                row.style.display = row.textContent.toLowerCase().includes(q) ? '' : 'none';
            });
        });
    });

    // ---------- Table sort ----------
    document.querySelectorAll('table.table-sortable').forEach(function (table) {
        table.querySelectorAll('th[data-sort]').forEach(function (th) {
            th.addEventListener('click', function () {
                const idx = Array.prototype.indexOf.call(th.parentNode.children, th);
                const asc = !th.classList.contains('sort-asc');
                table.querySelectorAll('th').forEach(function (h) { h.classList.remove('sort-asc', 'sort-desc'); });
                th.classList.add(asc ? 'sort-asc' : 'sort-desc');
                const rows = Array.prototype.slice.call(table.querySelectorAll('tbody tr'));
                rows.sort(function (a, b) {
                    const av = (a.children[idx] ? a.children[idx].textContent : '').trim();
                    const bv = (b.children[idx] ? b.children[idx].textContent : '').trim();
                    const an = parseFloat(av.replace(/[^0-9.\-]/g, ''));
                    const bn = parseFloat(bv.replace(/[^0-9.\-]/g, ''));
                    if (!isNaN(an) && !isNaN(bn)) return asc ? an - bn : bn - an;
                    return asc ? av.localeCompare(bv) : bv.localeCompare(av);
                });
                rows.forEach(function (r) { r.parentNode.appendChild(r); });
            });
        });
        // client-side pagination
        const perPage = parseInt(table.getAttribute('data-page-size') || '0', 10);
        if (perPage > 0) {
            const tbody = table.querySelector('tbody');
            const rows = Array.prototype.slice.call(tbody.querySelectorAll('tr'));
            let page = 1;
            const pages = Math.max(1, Math.ceil(rows.length / perPage));
            let pager = table.parentNode.querySelector('[data-pager]');
            if (!pager) {
                pager = document.createElement('div');
                pager.className = 'd-flex justify-content-between align-items-center mt-3';
                pager.setAttribute('data-pager', '');
                table.parentNode.appendChild(pager);
            }
            function render() {
                rows.forEach(function (r, i) { r.style.display = (i >= (page - 1) * perPage && i < page * perPage) ? '' : 'none'; });
                pager.innerHTML = '<small class="text-muted">Page ' + page + ' of ' + pages + '</small>' +
                    '<div><button class="btn btn-sm btn-outline-secondary me-1" data-prev ' + (page === 1 ? 'disabled' : '') + '>Prev</button>' +
                    '<button class="btn btn-sm btn-outline-secondary" data-next ' + (page === pages ? 'disabled' : '') + '>Next</button></div>';
                const prev = pager.querySelector('[data-prev]');
                const next = pager.querySelector('[data-next]');
                if (prev) prev.onclick = function () { if (page > 1) { page--; render(); } };
                if (next) next.onclick = function () { if (page < pages) { page++; render(); } };
            }
            if (rows.length) render();
        }
    });

    // ---------- Confirm delete ----------
    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            const msg = form.getAttribute('data-confirm') || 'Are you sure?';
            if (!window.confirm(msg)) e.preventDefault();
        });
    });

    // ---------- Form spinner ----------
    document.querySelectorAll('form[data-spinner]').forEach(function (form) {
        form.addEventListener('submit', function () {
            form.querySelectorAll('button[type=submit]').forEach(function (btn) {
                btn.disabled = true;
                const text = btn.innerHTML;
                btn.dataset.original = text;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Please wait…';
            });
        });
    });

    // ---------- Auto-dismiss alerts ----------
    setTimeout(function () {
        document.querySelectorAll('.alert-auto-dismiss').forEach(function (el) {
            el.classList.remove('show');
            setTimeout(function () { el.remove(); }, 300);
        });
    }, 4500);
})();
