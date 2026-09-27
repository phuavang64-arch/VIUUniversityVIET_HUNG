document.addEventListener('DOMContentLoaded', function () {
    var toggle = document.getElementById('adminSidebarToggle');
    var sidebar = document.querySelector('.admin-sidebar');
    if (toggle && sidebar) {
        toggle.addEventListener('click', function () {
            sidebar.classList.toggle('show');
        });
    }
});
