/**
 * MiEarbuddy — Main Script (Apple Aesthetic & Interactions)
 */

document.addEventListener('DOMContentLoaded', () => {
  // 1. IntersectionObserver for smooth Apple-style scroll reveal
  if ('IntersectionObserver' in window) {
    const revealObserver = new IntersectionObserver((entries, observer) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-revealed');
          observer.unobserve(entry.target);
        }
      });
    }, {
      root: null,
      rootMargin: '0px 0px -50px 0px',
      threshold: 0.1
    });

    document.querySelectorAll('.reveal-on-scroll, .reveal-scale').forEach(el => {
      revealObserver.observe(el);
    });
  } else {
    document.querySelectorAll('.reveal-on-scroll, .reveal-scale').forEach(el => {
      el.classList.add('is-revealed');
    });
  }

  // 2. Scroll Spy for navigation links
  const sections = document.querySelectorAll('section[id]');
  const navLinks = document.querySelectorAll('.subnav-menu .subnav-link');

  function updateActiveNavLink() {
    const scrollPos = window.scrollY + 120;
    sections.forEach(section => {
      const top = section.offsetTop;
      const height = section.offsetHeight;
      const id = section.getAttribute('id');

      if (scrollPos >= top && scrollPos < top + height) {
        navLinks.forEach(link => {
          link.classList.remove('active');
          if (link.getAttribute('href') === `#${id}`) {
            link.classList.add('active');
          }
        });
      }
    });
  }
  window.addEventListener('scroll', updateActiveNavLink, { passive: true });

  // 3. Copy CLI command helper
  document.querySelectorAll('.copy-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const codeText = btn.getAttribute('data-copy');
      if (!codeText) return;

      navigator.clipboard.writeText(codeText).then(() => {
        const originalHtml = btn.innerHTML;
        btn.innerHTML = `<span style="color: var(--status-green); font-size: 0.6875rem; font-weight: 600;">✓</span>`;
        setTimeout(() => {
          btn.innerHTML = originalHtml;
        }, 1500);
      });
    });
  });

  // 4. Hero Earbuds Reveal Interaction (Replay on click)
  const heroWrap = document.getElementById('heroMonumentalWrap');

  if (heroWrap) {
    heroWrap.addEventListener('click', () => {
      heroWrap.classList.remove('is-replaying');
      void heroWrap.offsetWidth;
      heroWrap.classList.add('is-replaying');
    });
  }

  // 5. Mobile Subnav Dropdown Toggle
  const mobileToggle = document.querySelector('.mobile-subnav-toggle');
  const mobileDropdown = document.getElementById('mobileSubnavDropdown');

  if (mobileToggle && mobileDropdown) {
    mobileToggle.addEventListener('click', () => {
      const isOpen = mobileDropdown.classList.toggle('is-open');
      mobileToggle.setAttribute('aria-expanded', isOpen);
    });

    mobileDropdown.querySelectorAll('a').forEach(link => {
      link.addEventListener('click', () => {
        mobileDropdown.classList.remove('is-open');
        mobileToggle.setAttribute('aria-expanded', 'false');
      });
    });
  }
});
