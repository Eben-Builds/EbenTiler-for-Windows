(() => {
  const releaseInstallerUrl = 'https://github.com/Eben-Builds/EbenTiler-for-Windows/releases/latest/download/EbenTiler-Setup.exe';
  const links = document.querySelectorAll('a[href$="EbenTiler-Setup.exe"]');

  links.forEach((link) => {
    link.href = releaseInstallerUrl;
    link.removeAttribute('download');

    link.addEventListener('click', () => {
      if (!link.classList.contains('download-button')) return;

      link.dataset.busy = 'true';
      const original = link.innerHTML;
      link.innerHTML = '<span class="win-symbol" aria-hidden="true">⊞</span> 안전한 릴리스로 이동 중…';
      window.setTimeout(() => {
        link.innerHTML = original;
        delete link.dataset.busy;
      }, 1800);
    });
  });
})();
