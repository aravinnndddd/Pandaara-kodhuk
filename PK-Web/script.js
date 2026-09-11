/**
 * PANDARA KOTHUK - INTERACTIVE GAME LANDING PAGE ENGINE
 * Pure Vanilla JavaScript (No libraries)
 * Features:
 *  - Organic multi-creature flight/crawl simulation
 *  - Procedural comic blood splatters on canvas
 *  - Web Audio API procedural slap/squish sound synthesizer
 *  - Real-time swatting & kill counter HUD
 *  - Trailer interactive sandbox modal
 *  - Responsive mobile navigation
 */

(function() {
  'use strict';

  // --- AUDIO SYNTHESIZER (Zero external file dependencies) ---
  class SoundEngine {
    constructor() {
      this.ctx = null;
      this.muted = false;
      this.initialized = false;
    }

    init() {
      if (this.initialized) return;
      try {
        const AudioCtx = window.AudioContext || window.webkitAudioContext;
        if (AudioCtx) {
          this.ctx = new AudioCtx();
          this.initialized = true;
        }
      } catch (e) {
        console.warn('Web Audio not available', e);
      }
    }

    playSquish() {
      if (this.muted) return;
      this.init();
      if (!this.ctx) return;
      if (this.ctx.state === 'suspended') {
        this.ctx.resume();
      }

      const now = this.ctx.currentTime;
      // 1. Noise burst for the physical impact slap
      const bufferSize = this.ctx.sampleRate * 0.08;
      const buffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
      const data = buffer.getChannelData(0);
      for (let i = 0; i < bufferSize; i++) {
        data[i] = Math.random() * 2 - 1;
      }
      const noise = this.ctx.createBufferSource();
      noise.buffer = buffer;
      const noiseGain = this.ctx.createGain();
      noiseGain.gain.setValueAtTime(0.35, now);
      noiseGain.gain.exponentialRampToValueAtTime(0.01, now + 0.08);
      noise.connect(noiseGain);
      noiseGain.connect(this.ctx.destination);
      noise.start(now);

      // 2. Low-frequency punch for meaty slap
      const osc = this.ctx.createOscillator();
      const oscGain = this.ctx.createGain();
      osc.type = 'triangle';
      osc.frequency.setValueAtTime(140, now);
      osc.frequency.exponentialRampToValueAtTime(35, now + 0.12);
      oscGain.gain.setValueAtTime(0.4, now);
      oscGain.gain.exponentialRampToValueAtTime(0.01, now + 0.12);
      osc.connect(oscGain);
      oscGain.connect(this.ctx.destination);
      osc.start(now);
      osc.stop(now + 0.12);
    }

    playBuzz() {
      if (this.muted) return;
      this.init();
      if (!this.ctx) return;
      if (this.ctx.state === 'suspended') this.ctx.resume();

      const now = this.ctx.currentTime;
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      osc.type = 'sawtooth';
      osc.frequency.setValueAtTime(180, now);
      osc.frequency.linearRampToValueAtTime(220, now + 0.15);
      gain.gain.setValueAtTime(0.08, now);
      gain.gain.linearRampToValueAtTime(0.01, now + 0.2);
      osc.connect(gain);
      gain.connect(this.ctx.destination);
      osc.start(now);
      osc.stop(now + 0.2);
    }
  }

  const audio = new SoundEngine();

  // --- BLOOD CANVAS SPLATTER SYSTEM ---
  class BloodCanvas {
    constructor() {
      this.canvas = document.getElementById('splatter-canvas');
      if (!this.canvas) return;
      this.ctx = this.canvas.getContext('2d');
      this.splatters = [];
      this.resize();
      window.addEventListener('resize', () => this.resize());
      this.loop = this.loop.bind(this);
      requestAnimationFrame(this.loop);
    }

    resize() {
      if (!this.canvas) return;
      this.canvas.width = window.innerWidth;
      this.canvas.height = window.innerHeight;
    }

    addSplatter(x, y, scale = 1, isBoss = false) {
      const droplets = [];
      const count = isBoss ? 28 : Math.floor(8 + Math.random() * 8);

      for (let i = 0; i < count; i++) {
        const angle = Math.random() * Math.PI * 2;
        const dist = (Math.random() * 35 + 10) * scale;
        droplets.push({
          x: x + Math.cos(angle) * dist,
          y: y + Math.sin(angle) * dist,
          r: (Math.random() * 4 + 2) * scale,
          dripLen: Math.random() > 0.6 ? Math.random() * 15 * scale : 0
        });
      }

      this.splatters.push({
        x: x,
        y: y,
        r: (Math.random() * 12 + 10) * scale,
        droplets: droplets,
        color: Math.random() > 0.3 ? '#d32f2f' : '#b71c1c',
        alpha: 1.0,
        createdAt: performance.now(),
        duration: isBoss ? 6500 : 4500
      });
    }

    loop(timestamp) {
      if (!this.ctx) return;
      this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

      for (let i = this.splatters.length - 1; i >= 0; i--) {
        const s = this.splatters[i];
        const elapsed = timestamp - s.createdAt;
        if (elapsed > s.duration) {
          this.splatters.splice(i, 1);
          continue;
        }

        // Fade out in the last 1.5 seconds
        const fadeStart = s.duration - 1500;
        let a = 1.0;
        if (elapsed > fadeStart) {
          a = 1.0 - (elapsed - fadeStart) / 1500;
        }

        this.ctx.save();
        this.ctx.globalAlpha = a;
        this.ctx.fillStyle = s.color;

        // Central puddle
        this.ctx.beginPath();
        this.ctx.arc(s.x, s.y, s.r, 0, Math.PI * 2);
        this.ctx.fill();

        // Radiating droplets
        for (const d of s.droplets) {
          this.ctx.beginPath();
          this.ctx.arc(d.x, d.y, d.r, 0, Math.PI * 2);
          this.ctx.fill();

          // Drip lines
          if (d.dripLen > 0) {
            this.ctx.beginPath();
            this.ctx.moveTo(d.x, d.y);
            this.ctx.lineTo(d.x, d.y + d.dripLen);
            this.ctx.strokeStyle = s.color;
            this.ctx.lineWidth = d.r * 0.9;
            this.ctx.lineCap = 'round';
            this.ctx.stroke();
          }
        }
        this.ctx.restore();
      }

      requestAnimationFrame(this.loop);
    }
  }

  const bloodCanvas = new BloodCanvas();

  // --- CREATURE SIMULATION ENGINE ---
  const CREATURE_TYPES = [
    { type: 'mosquito', emoji: '🦟', className: 'bug-mosquito', speed: 1.8, pauseChance: 0.04, scale: 1 },
    { type: 'spider', emoji: '🕷️', className: 'bug-spider', speed: 1.1, pauseChance: 0.08, scale: 1.3 },
    { type: 'fly', emoji: '🪰', className: 'bug-fly', speed: 2.8, pauseChance: 0.02, scale: 0.9 },
    { type: 'bat', emoji: '🦇', className: 'bug-bat', speed: 2.2, pauseChance: 0.01, scale: 1.5 },
    { type: 'ant', emoji: '🐜', className: 'bug-ant', speed: 0.9, pauseChance: 0.05, scale: 0.7 },
    { type: 'boss', emoji: '👑', className: 'bug-boss', speed: 1.4, pauseChance: 0.03, scale: 2.2 }
  ];

  class LiveCreature {
    constructor(container, forcedType = null) {
      this.container = container;
      this.dead = false;

      // Select creature config
      if (forcedType) {
        this.config = CREATURE_TYPES.find(c => c.type === forcedType) || CREATURE_TYPES[0];
      } else {
        // Weighted random: Mosquitoes and flies are most common
        const rand = Math.random();
        if (rand < 0.40) this.config = CREATURE_TYPES[0]; // Mosquito
        else if (rand < 0.65) this.config = CREATURE_TYPES[1]; // Spider
        else if (rand < 0.85) this.config = CREATURE_TYPES[2]; // Fly
        else if (rand < 0.93) this.config = CREATURE_TYPES[3]; // Bat
        else if (rand < 0.98) this.config = CREATURE_TYPES[4]; // Ant
        else this.config = CREATURE_TYPES[5]; // Rare Boss
      }

      this.el = document.createElement('div');
      this.el.className = `live-bug ${this.config.className}`;
      this.el.textContent = this.config.emoji;
      this.el.title = `Click to SWAT this ${this.config.type}!`;

      // Spawn at boundary edge
      const bounds = this.container.getBoundingClientRect();
      this.w = bounds.width || window.innerWidth;
      this.h = bounds.height || 600;

      // Position
      this.x = Math.random() * (this.w - 60) + 30;
      this.y = Math.random() * (this.h - 60) + 30;

      // Movement vector
      const angle = Math.random() * Math.PI * 2;
      this.vx = Math.cos(angle) * this.config.speed;
      this.vy = Math.sin(angle) * this.config.speed;
      this.isPaused = false;
      this.pauseTimer = 0;

      this.updateStyle();
      this.container.appendChild(this.el);

      // Swat event handler
      this.el.addEventListener('click', (e) => this.swat(e));
      this.el.addEventListener('touchstart', (e) => {
        e.preventDefault();
        this.swat(e.touches[0]);
      }, { passive: false });
    }

    updateStyle() {
      // Rotate facing direction of travel
      let rot = Math.atan2(this.vy, this.vx) * (180 / Math.PI) + 90;
      this.el.style.transform = `translate3d(${this.x}px, ${this.y}px, 0) rotate(${rot}deg)`;
    }

    update() {
      if (this.dead) return;

      // Recalculate container bounds periodically
      const bounds = this.container.getBoundingClientRect();
      const maxW = bounds.width - 40;
      const maxH = bounds.height - 40;

      if (this.isPaused) {
        this.pauseTimer--;
        if (this.pauseTimer <= 0) {
          this.isPaused = false;
          const angle = Math.random() * Math.PI * 2;
          this.vx = Math.cos(angle) * this.config.speed;
          this.vy = Math.sin(angle) * this.config.speed;
        }
        return;
      }

      // Random twitch / pause check
      if (Math.random() < this.config.pauseChance) {
        this.isPaused = true;
        this.pauseTimer = Math.floor(Math.random() * 25 + 10);
        return;
      }

      // Wander / steer slightly
      const steer = (Math.random() - 0.5) * 0.45;
      const curAngle = Math.atan2(this.vy, this.vx) + steer;
      const speed = Math.hypot(this.vx, this.vy);
      this.vx = Math.cos(curAngle) * speed;
      this.vy = Math.sin(curAngle) * speed;

      this.x += this.vx;
      this.y += this.vy;

      // Bounce off boundaries
      if (this.x < 10) { this.x = 10; this.vx = Math.abs(this.vx); }
      if (this.x > maxW) { this.x = maxW; this.vx = -Math.abs(this.vx); }
      if (this.y < 10) { this.y = 10; this.vy = Math.abs(this.vy); }
      if (this.y > maxH) { this.y = maxH; this.vy = -Math.abs(this.vy); }

      this.updateStyle();
    }

    swat(e) {
      if (this.dead) return;
      this.dead = true;
      e.stopPropagation && e.stopPropagation();

      // Audio feedback
      audio.playSquish();

      // Coordinates for splatter
      const rect = this.el.getBoundingClientRect();
      const clickX = rect.left + rect.width / 2;
      const clickY = rect.top + rect.height / 2;

      // Blood splatter on global canvas
      const isBoss = this.config.type === 'boss';
      bloodCanvas.addSplatter(clickX, clickY, this.config.scale, isBoss);

      // Squish animation on bug element
      this.el.classList.add('dead');

      // Update Kill Counter
      incrementKillCount();

      // Remove after squish animation
      setTimeout(() => {
        if (this.el.parentNode) {
          this.el.parentNode.removeChild(this.el);
        }
      }, 600);

      // Respawn a new bug in container after delay
      setTimeout(() => {
        if (this.container && this.container.isConnected) {
          creaturesList.push(new LiveCreature(this.container));
        }
      }, Math.random() * 2000 + 1500);
    }
  }

  // --- POPULATION MANAGEMENT ---
  const creaturesList = [];
  let killCount = 0;

  function incrementKillCount() {
    killCount++;
    const killEl = document.getElementById('kill-count');
    if (killEl) {
      killEl.textContent = killCount;
      const hud = document.getElementById('bug-kill-counter');
      if (hud) {
        hud.style.transform = 'scale(1.2) rotate(3deg)';
        setTimeout(() => {
          hud.style.transform = '';
        }, 200);
      }
    }
  }

  function initCreatures() {
    const heroZone = document.getElementById('hero-creature-zone');
    if (!heroZone) return;

    // Responsive spawn density
    const isMobile = window.innerWidth < 768;
    const initialCount = isMobile ? 5 : 10;

    for (let i = 0; i < initialCount; i++) {
      creaturesList.push(new LiveCreature(heroZone));
    }

    // Main animation loop
    function animateCreatures() {
      for (let i = creaturesList.length - 1; i >= 0; i--) {
        const c = creaturesList[i];
        if (c.dead && !c.el.isConnected) {
          creaturesList.splice(i, 1);
        } else {
          c.update();
        }
      }
      requestAnimationFrame(animateCreatures);
    }
    requestAnimationFrame(animateCreatures);

    // Periodic stray buzzer spawner (keeps the page lively)
    setInterval(() => {
      const maxCreatures = isMobile ? 6 : 14;
      if (creaturesList.length < maxCreatures && heroZone.isConnected) {
        creaturesList.push(new LiveCreature(heroZone));
        if (Math.random() < 0.25) audio.playBuzz();
      }
    }, 4500);
  }

  // --- CARD POKE BUTTON INTERACTIONS ---
  function initCardPokes() {
    const cards = document.querySelectorAll('.creature-card');
    cards.forEach(card => {
      const btn = card.querySelector('.poke-btn');
      const cType = card.dataset.creature || 'mosquito';
      if (!btn) return;

      btn.addEventListener('click', (e) => {
        const rect = btn.getBoundingClientRect();
        audio.playSquish();
        bloodCanvas.addSplatter(rect.left + rect.width / 2, rect.top + rect.height / 2, 1.4, cType === 'boss');
        incrementKillCount();

        // Comic visual shake
        card.style.transform = 'scale(0.96) rotate(-3deg)';
        setTimeout(() => {
          card.style.transform = '';
        }, 180);

        // Spawn that creature directly into hero!
        const heroZone = document.getElementById('hero-creature-zone');
        if (heroZone) {
          creaturesList.push(new LiveCreature(heroZone, cType));
        }
      });
    });
  }

  // --- MOBILE NAVIGATION TOGGLE ---
  function initMobileMenu() {
    const menuBtn = document.getElementById('menu-toggle');
    const navMenu = document.getElementById('nav-menu');
    if (!menuBtn || !navMenu) return;

    menuBtn.addEventListener('click', () => {
      const isOpen = navMenu.classList.toggle('active');
      menuBtn.setAttribute('aria-expanded', isOpen);
    });

    // Close menu when any nav link is clicked
    const links = navMenu.querySelectorAll('.nav-link');
    links.forEach(link => {
      link.addEventListener('click', () => {
        navMenu.classList.remove('active');
        menuBtn.setAttribute('aria-expanded', 'false');
      });
    });
  }

  // --- TRAILER MODAL WITH LIVE BUG SANDBOX ---
  function initTrailerModal() {
    const trailerBtn = document.getElementById('trailer-btn');
    const modal = document.getElementById('trailer-modal');
    const closeBtn = document.getElementById('close-modal-btn');
    const sandbox = document.getElementById('sandbox-canvas');
    const modalDownloadBtn = document.getElementById('modal-download-btn');

    if (!trailerBtn || !modal) return;

    let sandboxCreatures = [];
    let sandboxLoopId = null;

    function openModal() {
      modal.classList.add('open');
      modal.setAttribute('aria-hidden', 'false');
      startSandbox();
    }

    function closeModal() {
      modal.classList.remove('open');
      modal.setAttribute('aria-hidden', 'true');
      stopSandbox();
    }

    function startSandbox() {
      if (!sandbox) return;
      // Clear old bugs
      sandbox.querySelectorAll('.live-bug').forEach(b => b.remove());
      sandboxCreatures = [];

      // Spawn 5 frantic bugs in sandbox
      for (let i = 0; i < 6; i++) {
        sandboxCreatures.push(new LiveCreature(sandbox));
      }

      function loop() {
        if (!modal.classList.contains('open')) return;
        sandboxCreatures.forEach(c => c.update());
        sandboxLoopId = requestAnimationFrame(loop);
      }
      loop();
    }

    function stopSandbox() {
      if (sandboxLoopId) cancelAnimationFrame(sandboxLoopId);
      sandboxCreatures = [];
    }

    trailerBtn.addEventListener('click', openModal);
    if (closeBtn) closeBtn.addEventListener('click', closeModal);
    if (modalDownloadBtn) modalDownloadBtn.addEventListener('click', closeModal);

    modal.addEventListener('click', (e) => {
      if (e.target === modal) closeModal();
    });

    window.addEventListener('keydown', (e) => {
      if (e.key === 'Escape' && modal.classList.contains('open')) {
        closeModal();
      }
    });
  }

  // --- SOUND TOGGLE BUTTON ---
  function initSoundToggle() {
    const toggleBtn = document.getElementById('sound-toggle');
    if (!toggleBtn) return;

    toggleBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      audio.muted = !audio.muted;
      toggleBtn.textContent = audio.muted ? '🔇' : '🔊';
      toggleBtn.title = audio.muted ? 'Sound Muted' : 'Sound Enabled';
    });
  }

  // --- INITIALIZE ALL MODULES ON DOM READY ---
  document.addEventListener('DOMContentLoaded', () => {
    initCreatures();
    initCardPokes();
    initMobileMenu();
    initTrailerModal();
    initSoundToggle();
    console.log('🦟 Pandara Kothuk Web Engine initialized. Ready to swat bugs!');
  });
})();
