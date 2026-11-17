/**
 * MiEarbuddy — Windows 11 System Tray & Taskbar Simulator
 * Replicates the authentic hover tooltip and dark context menu behavior
 */

document.addEventListener('DOMContentLoaded', () => {
  const trayTarget = document.getElementById('traySimTarget');
  const hoverTooltip = document.getElementById('trayHoverTooltip');
  const contextMenu = document.getElementById('simContextMenu');
  const autoStartItem = document.getElementById('trayAutoStartItem');
  const autoStartBadge = document.getElementById('trayAutoStartBadge');
  const clockEl = document.getElementById('taskbarClock');

  let autoStartEnabled = true;

  // Live Windows taskbar clock
  function updateClock() {
    if (!clockEl) return;
    const now = new Date();
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    const dateStr = now.toLocaleDateString(undefined, { day: '2-digit', month: '2-digit', year: 'numeric' });
    clockEl.innerHTML = `${hours}:${minutes}<br><span style="font-size: 0.6875rem; color: #787880;">${dateStr}</span>`;
  }

  setInterval(updateClock, 1000);
  updateClock();

  if (trayTarget) {
    // Hover Tooltip behavior
    trayTarget.addEventListener('mouseenter', () => {
      if (!contextMenu.classList.contains('open')) {
        hoverTooltip.classList.add('visible');
      }
    });

    trayTarget.addEventListener('mouseleave', () => {
      hoverTooltip.classList.remove('visible');
    });

    // Click Context Menu behavior
    trayTarget.addEventListener('click', (e) => {
      e.stopPropagation();
      hoverTooltip.classList.remove('visible');
      contextMenu.classList.toggle('open');
      trayTarget.classList.toggle('active', contextMenu.classList.contains('open'));
    });
  }

  // Toggle "Start with Windows" [ ON ] / [ OFF ]
  if (autoStartItem && autoStartBadge) {
    autoStartItem.addEventListener('click', (e) => {
      e.stopPropagation();
      autoStartEnabled = !autoStartEnabled;
      if (autoStartEnabled) {
        autoStartBadge.textContent = '[ ON ]';
        autoStartBadge.className = 'sim-menu-badge';
      } else {
        autoStartBadge.textContent = '[ OFF ]';
        autoStartBadge.className = 'sim-menu-badge off';
      }
    });
  }

  // Mode radio toggles inside context menu
  const menuModeItems = document.querySelectorAll('.sim-menu-mode-item');
  menuModeItems.forEach(item => {
    item.addEventListener('click', (e) => {
      e.stopPropagation();
      menuModeItems.forEach(i => {
        i.classList.remove('active');
        const check = i.querySelector('.mode-check');
        if (check) check.textContent = '○';
      });
      item.classList.add('active');
      const check = item.querySelector('.mode-check');
      if (check) check.textContent = '●';
    });
  });

  // Close context menu when clicking outside
  document.addEventListener('click', (e) => {
    if (contextMenu && !contextMenu.contains(e.target) && e.target !== trayTarget) {
      contextMenu.classList.remove('open');
      if (trayTarget) trayTarget.classList.remove('active');
    }
  });
});
