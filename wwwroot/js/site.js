// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Smooth scroll to anchor when coming from another page
document.addEventListener('DOMContentLoaded', function() {
    // Check if URL has an anchor hash
    if (window.location.hash) {
        const hash = window.location.hash;
        // Wait a bit for page to fully load
        setTimeout(function() {
            const element = document.querySelector(hash);
            if (element) {
                element.scrollIntoView({ 
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        }, 100);
    }
    
    // Handle anchor links with smooth scroll
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            if (href !== '#' && href.length > 1) {
                const target = document.querySelector(href);
                if (target) {
                    e.preventDefault();
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                    // Update URL without jumping
                    history.pushState(null, null, href);
                }
            }
        });
    });
    
    // Mobile menu optimization
    const navbarCollapse = document.getElementById('navbarNavAltMarkup');
    const navbarToggler = document.querySelector('.navbar-toggler');
    
    if (navbarCollapse && navbarToggler) {
        // Close mobile menu when clicking on a nav link
        const navLinks = navbarCollapse.querySelectorAll('.nav-link:not(.dropdown-toggle)');
        navLinks.forEach(link => {
            link.addEventListener('click', function() {
                // Check if we're on mobile (screen width < 992px, Bootstrap lg breakpoint)
                if (window.innerWidth < 992) {
                    const bsCollapse = bootstrap.Collapse.getInstance(navbarCollapse);
                    if (bsCollapse && bsCollapse._isShown()) {
                        bsCollapse.hide();
                    }
                }
            });
        });
        
        // Close mobile menu when clicking outside
        document.addEventListener('click', function(e) {
            if (window.innerWidth < 992) {
                const isClickInsideNav = navbarCollapse.contains(e.target) || navbarToggler.contains(e.target);
                if (!isClickInsideNav) {
                    const bsCollapse = bootstrap.Collapse.getInstance(navbarCollapse);
                    if (bsCollapse && bsCollapse._isShown()) {
                        bsCollapse.hide();
                    }
                }
            }
        });
        
        // Prevent body scroll when menu is open on mobile
        navbarCollapse.addEventListener('show.bs.collapse', function() {
            if (window.innerWidth < 992) {
                document.body.style.overflow = 'hidden';
            }
        });
        
        navbarCollapse.addEventListener('hide.bs.collapse', function() {
            document.body.style.overflow = '';
        });
        
        // Handle window resize
        let resizeTimer;
        window.addEventListener('resize', function() {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function() {
                if (window.innerWidth >= 992) {
                    // On desktop, ensure menu is visible and body scroll is enabled
                    document.body.style.overflow = '';
                    const bsCollapse = bootstrap.Collapse.getInstance(navbarCollapse);
                    if (bsCollapse) {
                        bsCollapse.show();
                    }
                }
            }, 250);
        });
    }
    
    // Prevent zoom on double tap and pinch (iOS/Android)
    let lastTouchEnd = 0;
    let lastTouchDistance = 0;
    
    document.addEventListener('touchstart', function(event) {
        if (event.touches.length > 1) {
            // Pinch zoom detected
            const touch1 = event.touches[0];
            const touch2 = event.touches[1];
            lastTouchDistance = Math.hypot(
                touch2.clientX - touch1.clientX,
                touch2.clientY - touch1.clientY
            );
        }
    }, { passive: false });
    
    document.addEventListener('touchmove', function(event) {
        if (event.touches.length > 1) {
            // Prevent pinch zoom
            event.preventDefault();
        }
    }, { passive: false });
    
    document.addEventListener('touchend', function(event) {
        const now = Date.now();
        // Prevent double-tap zoom
        if (now - lastTouchEnd <= 300) {
            event.preventDefault();
        }
        lastTouchEnd = now;
        lastTouchDistance = 0;
    }, { passive: false });
    
    // Disable zoom on input focus (iOS)
    const inputs = document.querySelectorAll('input, textarea, select');
    inputs.forEach(input => {
        input.addEventListener('focus', function() {
            if (window.innerWidth < 992) {
                this.style.fontSize = '16px';
            }
        });
    });
    
    // Fix viewport height on mobile browsers
    function setViewportHeight() {
        const vh = window.innerHeight * 0.01;
        document.documentElement.style.setProperty('--vh', `${vh}px`);
    }
    
    setViewportHeight();
    window.addEventListener('resize', setViewportHeight);
    window.addEventListener('orientationchange', setViewportHeight);
});
