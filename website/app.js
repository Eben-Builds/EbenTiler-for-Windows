(() => {
  const releaseInstallerUrl = 'https://github.com/Eben-Builds/Tessdeck-for-Windows/releases/latest';
  const windowsLogo = '<svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true" focusable="false" style="display:block;fill:currentColor"><path d="M2 2h9v9H2V2zm11 0h9v9h-9V2zM2 13h9v9H2v-9zm11 0h9v9h-9v-9z"/></svg>';
  const links = document.querySelectorAll('a.download-button');

  document.querySelectorAll('.win-symbol').forEach((icon) => {
    icon.innerHTML = windowsLogo;
    icon.style.display = 'inline-flex';
    icon.style.alignItems = 'center';
    icon.style.justifyContent = 'center';
  });

  links.forEach((link) => {
    link.href = releaseInstallerUrl;
    link.removeAttribute('download');

    link.addEventListener('click', () => {
      if (!link.classList.contains('download-button')) return;

      link.dataset.busy = 'true';
      const original = link.innerHTML;
      link.innerHTML = `<span class="win-symbol" aria-hidden="true">${windowsLogo}</span> 최신 릴리스로 이동 중…`;
      window.setTimeout(() => {
        link.innerHTML = original;
        delete link.dataset.busy;
      }, 1800);
    });
  });
})();
