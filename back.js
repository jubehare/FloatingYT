(function () {
  if (document.getElementById('fyt-bar')) return;

  const Z_TOOLBAR         = 2147483647;
  const Z_PLAYER          = 9000;
  const MOUSE_TRIGGER_Y   = 60;
  const TOOLBAR_HIDE_DELAY = 2500;

  // Toolbar
  const bar = document.createElement('div');
  bar.id = 'fyt-bar';
  bar.style.cssText = `
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    height: 48px;
    background: linear-gradient(to bottom, rgba(0,0,0,0.85), transparent);
    display: flex;
    align-items: center;
    padding: 0 14px;
    gap: 10px;
    z-index: ${Z_TOOLBAR};
    opacity: 0;
    transition: opacity .2s, transform .2s;
    transform: translateY(-4px);
    pointer-events: none;
    font-family: Segoe UI, sans-serif;
  `;

  // Back button
  const btn = document.createElement('button');
  btn.textContent = 'Back';
  btn.style.cssText = `
    background: rgba(255,255,255,0.12);
    color: #fff;
    border: 1px solid rgba(255,255,255,0.2);
    padding: 4px 14px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 12px;
    font-family: inherit;
  `;
  btn.onmouseover = () => btn.style.background = 'rgba(255,255,255,0.22)';
  btn.onmouseout  = () => btn.style.background = 'rgba(255,255,255,0.12)';
  btn.onclick     = () => { window.location.href = 'about:blank'; };
  bar.appendChild(btn);

  // Volume control
  const sep = document.createElement('div');
  sep.style.cssText = 'width:1px;height:16px;background:rgba(255,255,255,0.2);flex-shrink:0';
  bar.appendChild(sep);

  const volIcon = document.createElement('span');
  volIcon.textContent = '🔊';
  volIcon.style.cssText = 'font-size:14px;cursor:default;user-select:none';
  bar.appendChild(volIcon);

  const volSlider = document.createElement('input');
  volSlider.type = 'range';
  volSlider.min  = '0';
  volSlider.max  = '100';
  volSlider.style.cssText = 'width:80px;cursor:pointer;accent-color:#fff;opacity:0.85';
  volSlider.oninput = () => {
    const v = document.querySelector('video');
    if (v) v.volume = volSlider.value / 100;
    volIcon.textContent = volSlider.value == 0 ? '🔇' : '🔊';
  };
  // Poll until video element is available, then sync slider
  const initVol = () => {
    const v = document.querySelector('video');
    if (v) { volSlider.value = Math.round(v.volume * 100); return; }
    setTimeout(initVol, 500);
  };
  initVol();
  bar.appendChild(volSlider);

  document.body.appendChild(bar);

  // Show toolbar when mouse is near top edge, hide after delay
  let t;
  document.addEventListener('mousemove', e => {
    if (e.clientY < MOUSE_TRIGGER_Y) {
      bar.style.opacity = '1';
      bar.style.transform = 'translateY(0)';
      bar.style.pointerEvents = 'all';
      clearTimeout(t);
      t = setTimeout(() => {
        bar.style.opacity = '0';
        bar.style.transform = 'translateY(-4px)';
        bar.style.pointerEvents = 'none';
      }, TOOLBAR_HIDE_DELAY);
    }
  });

  // Fullscreen CSS
  const host = window.location.hostname;
  let css = '::-webkit-scrollbar{display:none!important}html,body{scrollbar-width:none!important}';

  if (host.includes('youtube.com')) {
    css += `
      html,body{overflow:hidden!important}
      #masthead-container,#guide,tp-yt-app-drawer,ytd-mini-guide-renderer,
      #secondary,#chat,ytd-watch-flexy #below,#related,
      ytd-comments,#comments,#playlist{display:none!important}
      ytd-app,ytd-page-manager,ytd-watch-flexy,
      #page-manager,#columns,#primary,#primary-inner,
      #player,#player-container,#player-container-outer{
        transform:none!important;will-change:auto!important;
        filter:none!important;contain:none!important;isolation:auto!important}
      #movie_player{
        position:fixed!important;top:0!important;left:0!important;
        width:100vw!important;height:100vh!important;z-index:${Z_PLAYER}!important}`;
  } else if (host.includes('twitch.tv')) {
    css += 'html,body{overflow:hidden!important}';
    css += `
      .top-nav,.side-nav,.side-bar-container,
      .stream-chat,.chat-shell,.right-column--default{display:none!important}
      .video-player__container,.video-player__overlay,.persistent-player{
        position:fixed!important;top:0!important;left:0!important;
        width:100vw!important;height:100vh!important;z-index:${Z_PLAYER}!important}`;

    const tryTheatre = (attempts) => {
      const btn = document.querySelector(
        'button[data-a-target="player-theatre-mode-button"],' +
        '[data-a-target="theatre-mode-toggle"],' +
        'button[aria-label="Theatre Mode (alt+t)"]'
      );
      if (btn) { btn.click(); return; }
      if (attempts > 0) setTimeout(() => tryTheatre(attempts - 1), 800);
    };
    setTimeout(() => tryTheatre(6), 1500);

  } else if (host.includes('bilibili.com')) {
    css += `
      html,body{overflow:hidden!important}
      .bili-header,.bili-header-m,#nav-header,.fixed-header,
      .video-info-container,.video-info-v1,
      .video-page-side-bar--wrap,.comment-container,
      .rec-list,.recommend-list-container,
      #reco_list{display:none!important}
      #bilibili-player .bpx-player-control-entity[data-shadow-show="false"],
      #bilibili-player .bpx-player-control-entity[data-shadow-show="false"] .bpx-player-control-bottom,
      #bilibili-player .bpx-player-control-entity[data-shadow-show="false"] .bpx-player-control-top,
      #bilibili-player .bpx-player-shadow-progress-area{
        opacity:1!important;visibility:visible!important;
        pointer-events:auto!important}`;

    const reappendBar = () => document.body.appendChild(bar);

    new MutationObserver(mutations => {
      for (const m of mutations)
        for (const node of m.addedNodes)
          if (node !== bar && node.nodeType === 1) { reappendBar(); return; }
    }).observe(document.body, { childList: true });

    const tryWebFs = (attempts) => {
      const selectors = [
        '.bpx-player-ctrl-web-fullscreen',
        '.bpx-player-ctrl-full-web',
        '[aria-label="网页全屏"]',
        '[data-key="web-fullscreen"]',
      ];
      for (const sel of selectors) {
        const el = document.querySelector(sel);
        if (el) {
          el.click();
          setTimeout(() => {
            const s = document.getElementById('fyt-fill');
            if (s) { s.remove(); document.head.appendChild(s); }
          }, 1500);
          setTimeout(reappendBar, 800);
          setTimeout(reappendBar, 1600);
          return;
        }
      }
      const all = document.querySelectorAll('.bpx-player-ctrl-btn,.bpx-player-ctrl-btn-icon');
      for (const el of all) {
        const tip = el.getAttribute('data-title') || el.getAttribute('title') || el.textContent;
        if (tip.includes('网页全屏') || tip.includes('web')) {
          el.click();
          setTimeout(() => {
            const s = document.getElementById('fyt-fill');
            if (s) { s.remove(); document.head.appendChild(s); }
          }, 1500);
          setTimeout(reappendBar, 800);
          setTimeout(reappendBar, 1600);
          return;
        }
      }
      if (attempts > 0) setTimeout(() => tryWebFs(attempts - 1), 800);
    };
    setTimeout(() => tryWebFs(8), 1200);
  }

  const fillStyle = document.createElement('style');
  fillStyle.id = 'fyt-fill';
  fillStyle.textContent = css;
  const applyFill = () => {
    if (!document.getElementById('fyt-fill'))
      document.head?.appendChild(fillStyle);
  };
  applyFill();
  setTimeout(applyFill, 1500);

  // YouTube ad skip
  setInterval(() => {
    const s = document.querySelector(
      '.ytp-skip-ad-button,.ytp-ad-skip-button,.ytp-ad-skip-button-modern'
    );
    if (s) s.click();
    const v = document.querySelector('video');
    if (v && document.querySelector('.ad-showing') &&
        v.duration > 0 && !isNaN(v.duration))
      v.currentTime = v.duration;
  }, 300);

})();
