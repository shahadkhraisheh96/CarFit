// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', () => {
    const themeToggleBtn = document.getElementById('theme-toggle');
    if (themeToggleBtn) {
        const themeIcon = document.getElementById('theme-icon');
        const themeText = document.getElementById('theme-text');
        
        // Check local storage for theme
        const currentTheme = localStorage.getItem('theme') || 'light';
        if (currentTheme === 'dark') {
            document.documentElement.setAttribute('data-theme', 'dark');
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            themeIcon.classList.replace('fa-moon', 'fa-sun');
            themeIcon.classList.replace('text-danger', 'text-warning');
            themeText.textContent = 'Light Mode';
        } else {
            document.documentElement.setAttribute('data-bs-theme', 'light');
        }

        themeToggleBtn.addEventListener('click', (e) => {
            e.stopPropagation(); // Keep dropdown open or not depending on preference, actually better to let it close or just prevent default if needed
            let theme = document.documentElement.getAttribute('data-theme');
            if (theme === 'dark') {
                document.documentElement.removeAttribute('data-theme');
                document.documentElement.setAttribute('data-bs-theme', 'light');
                localStorage.setItem('theme', 'light');
                themeIcon.classList.replace('fa-sun', 'fa-moon');
                themeIcon.classList.replace('text-warning', 'text-danger');
                themeText.textContent = 'Dark Mode';
            } else {
                document.documentElement.setAttribute('data-theme', 'dark');
                document.documentElement.setAttribute('data-bs-theme', 'dark');
                localStorage.setItem('theme', 'dark');
                themeIcon.classList.replace('fa-moon', 'fa-sun');
                themeIcon.classList.replace('text-danger', 'text-warning');
                themeText.textContent = 'Light Mode';
            }
        });
    }
});
