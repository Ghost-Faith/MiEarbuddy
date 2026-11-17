/**
 * MiEarbuddy — Triple-Cell FastPair Battery Simulator
 * Dynamic color grading (>50% Green, 21-50% Orange, <=20% Red, Muted Disconnected)
 * Exactly reproduces the GDI+ logic in EarbudsCaseHeroView.cs
 */

document.addEventListener('DOMContentLoaded', () => {
  // Elements
  const leftFill = document.getElementById('battFillLeft');
  const caseFill = document.getElementById('battFillCase');
  const rightFill = document.getElementById('battFillRight');

  const leftText = document.getElementById('battValLeft');
  const caseText = document.getElementById('battValCase');
  const rightText = document.getElementById('battValRight');

  const leftBolt = document.getElementById('battBoltLeft');
  const caseBolt = document.getElementById('battBoltCase');
  const rightBolt = document.getElementById('battBoltRight');

  const sliderLeft = document.getElementById('sliderLeft');
  const sliderCase = document.getElementById('sliderCase');
  const sliderRight = document.getElementById('sliderRight');

  const sliderValLeft = document.getElementById('sliderValLeft');
  const sliderValCase = document.getElementById('sliderValCase');
  const sliderValRight = document.getElementById('sliderValRight');

  const presetChips = document.querySelectorAll('.preset-chip');

  let state = {
    left: 85,
    leftCharging: true,
    case: 100,
    caseCharging: false,
    right: 85,
    rightCharging: true,
    connected: true
  };

  function getColorClass(val, connected) {
    if (!connected || val < 0) return 'fill-disconnected';
    if (val > 50) return 'fill-green';
    if (val > 20) return 'fill-orange';
    return 'fill-red';
  }

  function render() {
    // 1. Left Earbud
    if (!state.connected || state.left < 0) {
      if (leftText) leftText.textContent = '--';
      if (leftFill) {
        leftFill.style.width = '0%';
        leftFill.className = 'physical-capsule-fill fill-disconnected';
      }
      if (leftBolt) leftBolt.style.display = 'none';
      if (sliderValLeft) sliderValLeft.textContent = '--';
    } else {
      if (leftText) leftText.textContent = `${state.left}%`;
      if (leftFill) {
        leftFill.style.width = `${state.left}%`;
        leftFill.className = `physical-capsule-fill ${getColorClass(state.left, true)}`;
      }
      if (leftBolt) leftBolt.style.display = state.leftCharging ? 'inline' : 'none';
      if (sliderValLeft) sliderValLeft.textContent = `${state.left}%`;
      if (sliderLeft) sliderLeft.value = state.left;
    }

    // 2. Charging Case
    if (!state.connected || state.case < 0) {
      if (caseText) caseText.textContent = '--';
      if (caseFill) {
        caseFill.style.width = '0%';
        caseFill.className = 'physical-capsule-fill fill-disconnected';
      }
      if (caseBolt) caseBolt.style.display = 'none';
      if (sliderValCase) sliderValCase.textContent = '--';
    } else {
      if (caseText) caseText.textContent = `${state.case}%`;
      if (caseFill) {
        caseFill.style.width = `${state.case}%`;
        caseFill.className = `physical-capsule-fill ${getColorClass(state.case, true)}`;
      }
      if (caseBolt) caseBolt.style.display = state.caseCharging ? 'inline' : 'none';
      if (sliderValCase) sliderValCase.textContent = `${state.case}%`;
      if (sliderCase) sliderCase.value = state.case;
    }

    // 3. Right Earbud
    if (!state.connected || state.right < 0) {
      if (rightText) rightText.textContent = '--';
      if (rightFill) {
        rightFill.style.width = '0%';
        rightFill.className = 'physical-capsule-fill fill-disconnected';
      }
      if (rightBolt) rightBolt.style.display = 'none';
      if (sliderValRight) sliderValRight.textContent = '--';
    } else {
      if (rightText) rightText.textContent = `${state.right}%`;
      if (rightFill) {
        rightFill.style.width = `${state.right}%`;
        rightFill.className = `physical-capsule-fill ${getColorClass(state.right, true)}`;
      }
      if (rightBolt) rightBolt.style.display = state.rightCharging ? 'inline' : 'none';
      if (sliderValRight) sliderValRight.textContent = `${state.right}%`;
      if (sliderRight) sliderRight.value = state.right;
    }
  }

  // Presets handling
  presetChips.forEach(chip => {
    chip.addEventListener('click', () => {
      presetChips.forEach(c => c.classList.remove('active'));
      chip.classList.add('active');

      const preset = chip.getAttribute('data-preset');
      if (preset === 'full') {
        state = { left: 100, leftCharging: true, case: 100, caseCharging: true, right: 100, rightCharging: true, connected: true };
      } else if (preset === 'balanced') {
        state = { left: 85, leftCharging: true, case: 100, caseCharging: false, right: 85, rightCharging: true, connected: true };
      } else if (preset === 'heavy') {
        state = { left: 35, leftCharging: false, case: 60, caseCharging: false, right: 28, rightCharging: false, connected: true };
      } else if (preset === 'low') {
        state = { left: 15, leftCharging: false, case: 20, caseCharging: false, right: 12, rightCharging: false, connected: true };
      } else if (preset === 'disconnected') {
        state = { left: -1, leftCharging: false, case: -1, caseCharging: false, right: -1, rightCharging: false, connected: false };
      }
      render();
    });
  });

  // Slider controls
  if (sliderLeft) {
    sliderLeft.addEventListener('input', (e) => {
      state.connected = true;
      state.left = parseInt(e.target.value, 10);
      state.leftCharging = state.left > 80;
      presetChips.forEach(c => c.classList.remove('active'));
      render();
    });
  }

  if (sliderCase) {
    sliderCase.addEventListener('input', (e) => {
      state.connected = true;
      state.case = parseInt(e.target.value, 10);
      state.caseCharging = state.case === 100;
      presetChips.forEach(c => c.classList.remove('active'));
      render();
    });
  }

  if (sliderRight) {
    sliderRight.addEventListener('input', (e) => {
      state.connected = true;
      state.right = parseInt(e.target.value, 10);
      state.rightCharging = state.right > 80;
      presetChips.forEach(c => c.classList.remove('active'));
      render();
    });
  }

  render();
});
