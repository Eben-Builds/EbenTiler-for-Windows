(() => {
  const links = document.querySelectorAll('.download-button');
  links.forEach((link) => {
    link.addEventListener('click', () => {
      link.dataset.busy = 'true';
      const original = link.innerHTML;
      link.innerHTML = '<span class="win-symbol" aria-hidden="true">⊞</span> 다운로드 시작 중…';
      window.setTimeout(() => {
        link.innerHTML = original;
        delete link.dataset.busy;
      }, 1800);
    });
  });
})();
