
document.addEventListener('DOMContentLoaded', function() {
    var dropdowns = document.querySelectorAll('.dropdown-toggle');
    
    dropdowns.forEach(function(dropdown) {
        dropdown.addEventListener('click', function(event) {
            event.preventDefault();
            var target = document.querySelector(dropdown.getAttribute('data-bs-target') || '#' + dropdown.getAttribute('id') + 'Menu');
            if (target) {
                target.classList.toggle('show');
                dropdown.setAttribute('aria-expanded', target.classList.contains('show'));
            }
        });
    });
});
