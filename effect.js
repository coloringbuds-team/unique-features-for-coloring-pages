document.addEventListener("DOMContentLoaded", function () {
  // --- Logo wobble ---
initLogoWobble();  
});
function initLogoWobble() {
    const logo = document.querySelector('.logo-footer');
    if (!logo) return;
    let animationInterval = null;
    const runAction = () => {
        if (!logo.classList.contains('loaded')) return;
        setTimeout(() => {
            const randomAngle = Math.random() > 0.5 ? 180 : -180;
            logo.style.setProperty('--rotate-angle', `${randomAngle}deg`);
            logo.classList.remove('logo-wobble');
            void logo.offsetWidth;
            logo.classList.add('logo-wobble');
            setTimeout(() => { logo.classList.remove('logo-wobble'); }, 5000);
        }, 200);
    };
    const start = () => {
        if (!animationInterval) {
            runAction();
            animationInterval = setInterval(runAction, 10000);
        }
    };
    const stop = () => {
        clearInterval(animationInterval);
        animationInterval = null;
    };
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) start();
            else stop();
        });
    }, { threshold: 0 });
    observer.observe(logo);
    const classObserver = new MutationObserver(() => {
        if (logo.classList.contains('loaded')) {
            const rect = logo.getBoundingClientRect();
            if (rect.top < window.innerHeight && rect.bottom > 0) start();
        }
    });
    classObserver.observe(logo, { attributes: true });
}
