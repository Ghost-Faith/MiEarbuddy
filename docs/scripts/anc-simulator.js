/**
 * MiEarbuddy — Active Noise Cancellation (ANC) Simulator & Waveform Canvas
 * Renders realistic acoustic DSP response and mode switching
 */

document.addEventListener('DOMContentLoaded', () => {
  const canvas = document.getElementById('ancWaveCanvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');

  let currentMode = 'anc'; // 'anc', 'transparency', 'off'
  let phase = 0;

  // Responsive canvas sizing
  function resizeCanvas() {
    const rect = canvas.getBoundingClientRect();
    const dpr = window.devicePixelRatio || 1;
    canvas.width = rect.width * dpr;
    canvas.height = rect.height * dpr;
    ctx.scale(dpr, dpr);
  }

  window.addEventListener('resize', resizeCanvas);
  resizeCanvas();

  // Mode switch pills
  const switchPills = document.querySelectorAll('.anc-switch-pill');
  const captionLead = document.getElementById('ancCaptionLead');
  const captionBody = document.getElementById('ancCaptionBody');

  function setMode(mode) {
    currentMode = mode;

    switchPills.forEach(pill => {
      const pillMode = pill.getAttribute('data-mode');
      pill.classList.toggle('active', pillMode === mode);
    });

    if (captionLead && captionBody) {
      if (mode === 'anc') {
        captionLead.textContent = window.i18n ? (window.i18n.getTranslation('ancMode1Title') + ':') : 'Active Noise Cancellation:';
        captionBody.textContent = window.i18n ? window.i18n.getTranslation('ancVisualizerStatus1') : 'Anti-phase acoustic frequencies actively neutralizing external background hum (-52 dB).';
      } else if (mode === 'transparency') {
        captionLead.textContent = window.i18n ? (window.i18n.getTranslation('ancMode2Title') + ':') : 'Transparency Mode:';
        captionBody.textContent = window.i18n ? window.i18n.getTranslation('ancVisualizerStatus2') : 'Environmental frequencies passing through wideband acoustic microphones with natural clarity.';
      } else {
        captionLead.textContent = window.i18n ? (window.i18n.getTranslation('ancMode3Title') + ':') : 'Off Mode:';
        captionBody.textContent = window.i18n ? window.i18n.getTranslation('ancVisualizerStatus3') : 'DSP filtering bypassed. Standard passive acoustic isolation engaged.';
      }
    }
  }

  switchPills.forEach(pill => {
    pill.addEventListener('click', () => {
      const mode = pill.getAttribute('data-mode');
      setMode(mode);
    });
  });

  window.addEventListener('languageChanged', () => {
    setMode(currentMode);
  });

  // Canvas Waveform Animation Loop
  function drawWaveforms() {
    const rect = canvas.getBoundingClientRect();
    const width = rect.width;
    const height = rect.height;
    const centerY = height / 2;

    ctx.clearRect(0, 0, width, height);

    // Subtle center axis
    ctx.beginPath();
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.06)';
    ctx.lineWidth = 1;
    ctx.moveTo(0, centerY);
    ctx.lineTo(width, centerY);
    ctx.stroke();

    phase += 0.04;

    if (currentMode === 'anc') {
      // 1. External ambient noise wave (faint red)
      ctx.beginPath();
      ctx.strokeStyle = 'rgba(255, 90, 90, 0.2)';
      ctx.lineWidth = 1.5;
      for (let x = 0; x < width; x++) {
        const y = centerY + Math.sin(x * 0.03 + phase) * 34 + Math.cos(x * 0.015 - phase * 0.5) * 16;
        if (x === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.stroke();

      // 2. Inverted Anti-Phase Cancellation Wave (faint cyan)
      ctx.beginPath();
      ctx.strokeStyle = 'rgba(0, 200, 240, 0.2)';
      ctx.lineWidth = 1.5;
      for (let x = 0; x < width; x++) {
        const y = centerY - (Math.sin(x * 0.03 + phase) * 34 + Math.cos(x * 0.015 - phase * 0.5) * 16);
        if (x === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.stroke();

      // 3. Resulting Neutralized Acoustic Wave (flat, calm, steady blue line)
      ctx.beginPath();
      ctx.strokeStyle = '#0071e3';
      ctx.lineWidth = 2.5;
      ctx.shadowColor = '#0071e3';
      ctx.shadowBlur = 10;
      for (let x = 0; x < width; x++) {
        const y = centerY + Math.sin(x * 0.08 + phase * 2) * 2;
        if (x === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.stroke();
      ctx.shadowBlur = 0;

    } else if (currentMode === 'transparency') {
      // Flowing natural ambient harmonics in cyan
      ctx.beginPath();
      ctx.strokeStyle = '#00c7be';
      ctx.lineWidth = 2.5;
      ctx.shadowColor = '#00c7be';
      ctx.shadowBlur = 8;
      for (let x = 0; x < width; x++) {
        const y = centerY + Math.sin(x * 0.025 + phase) * 36 + Math.sin(x * 0.05 + phase * 1.5) * 14;
        if (x === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.stroke();
      ctx.shadowBlur = 0;

    } else {
      // Off Mode: standard baseline passive wave
      ctx.beginPath();
      ctx.strokeStyle = '#6e6e73';
      ctx.lineWidth = 2;
      for (let x = 0; x < width; x++) {
        const y = centerY + Math.sin(x * 0.02 + phase * 0.8) * 18;
        if (x === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.stroke();
    }

    if (!window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      requestAnimationFrame(drawWaveforms);
    }
  }

  drawWaveforms();
  setMode('anc');
});
