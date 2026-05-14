class WaveBattleScene extends Phaser.Scene {
    constructor() { super('WaveBattleScene'); }

    init(data) {
        this.partyKeys = data.party || ['tank', 'rogue', 'priest', 'mage'];
        this.appliedUpgrades = data.appliedUpgrades || [];
        this.waveManager = data.waveManager || new WaveManager();
    }

    create() {
        this.cameras.main.setBackgroundColor('#1a1a2e');
        this.drawBackground();

        this.allies = [];
        this.enemies = [];
        this.battleOver = false;
        this.battleTime = 0;
        this.totalDamage = 0;
        this.speedMultiplier = 1;

        const wave = this.waveManager.nextWave();

        this.waveText = this.add.text(640, 30, `웨이브 ${wave} / ${this.waveManager.maxWave}`, {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(100);

        this.timerText = this.add.text(640, 58, '0.0초', {
            fontSize: '14px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(0.5).setDepth(100);

        this.spawnAllies();
        this.spawnEnemies(wave);
        this.applyUpgrades();

        this.showWaveAnnounce(wave);

        const speedBtn = this.add.text(1220, 30, '×1', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            backgroundColor: '#333355', padding: { x: 8, y: 4 }
        }).setOrigin(0.5).setDepth(100).setInteractive();
        speedBtn.on('pointerdown', () => {
            this.speedMultiplier = this.speedMultiplier === 1 ? 2 : this.speedMultiplier === 2 ? 3 : 1;
            speedBtn.setText(`×${this.speedMultiplier}`);
        });
    }

    drawBackground() {
        const gfx = this.add.graphics();
        gfx.fillGradientStyle(0x1a1a2e, 0x1a1a2e, 0x2a2a4e, 0x2a2a4e);
        gfx.fillRect(0, 0, 1280, 720);
        gfx.fillStyle(0x222244);
        gfx.fillRect(0, 450, 1280, 270);
        gfx.fillStyle(0x2a2a5a);
        gfx.fillRect(0, 448, 1280, 4);
        for (let i = 0; i < 60; i++) {
            const sx = Phaser.Math.Between(0, 1280), sy = Phaser.Math.Between(0, 440);
            gfx.fillStyle(0xffffff, Phaser.Math.FloatBetween(0.2, 0.6));
            gfx.fillRect(sx, sy, 1, 1);
        }
    }

    spawnAllies() {
        const startX = 150;
        const spacing = 70;
        this.partyKeys.forEach((key, i) => {
            const data = WAVE_CHARS[key];
            const x = startX + i * spacing;
            const y = 430 + (i % 2 === 0 ? 0 : 15);
            const char = new WaveCharacter(this, data, 'ally', x, y);
            this.allies.push(char);
        });
    }

    spawnEnemies(wave) {
        const enemyData = this.waveManager.getWaveEnemies(wave);
        const startX = 1130;
        const spacing = 60;
        enemyData.forEach((data, i) => {
            const x = startX - i * spacing;
            const y = 430 + (i % 2 === 0 ? 0 : 15);
            const char = new WaveCharacter(this, data, 'enemy', x, y);
            this.enemies.push(char);
        });
    }

    applyUpgrades() {
        const aliveAllies = this.allies.filter(a => a.alive);
        this.appliedUpgrades.forEach(upg => {
            const upgrade = UPGRADES.find(u => u.id === upg);
            if (upgrade) upgrade.apply(aliveAllies);
        });
    }

    showWaveAnnounce(wave) {
        let label = `웨이브 ${wave}`;
        if (wave === 10) label = '최종 웨이브 - 보스!';
        const txt = this.add.text(640, 300, label, {
            fontSize: '40px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(200).setAlpha(0);
        this.tweens.add({
            targets: txt, alpha: 1, duration: 300,
            yoyo: true, hold: 600,
            onComplete: () => txt.destroy()
        });
    }

    getAllChars() {
        return [...this.allies, ...this.enemies];
    }

    update(time, delta) {
        if (this.battleOver) return;
        const dt = delta * this.speedMultiplier;
        this.battleTime += dt;
        this.timerText.setText((this.battleTime / 1000).toFixed(1) + '초');

        const all = this.getAllChars();
        this.allies.forEach(a => a.update(dt, all));
        this.enemies.forEach(e => e.update(dt, all));

        const alliesAlive = this.allies.some(a => a.alive);
        const enemiesAlive = this.enemies.some(e => e.alive);

        if (!enemiesAlive) {
            this.battleOver = true;
            this.time.delayedCall(800, () => this.onWaveCleared());
        } else if (!alliesAlive) {
            this.battleOver = true;
            this.time.delayedCall(800, () => this.onDefeat());
        }
    }

    onWaveCleared() {
        if (this.waveManager.isLastWave()) {
            this.scene.start('WaveResultScene', {
                victory: true,
                wave: this.waveManager.currentWave,
                time: this.battleTime,
                allies: this.allies,
                party: this.partyKeys,
                upgrades: this.appliedUpgrades
            });
        } else {
            this.scene.start('UpgradeScene', {
                party: this.partyKeys,
                waveManager: this.waveManager,
                appliedUpgrades: this.appliedUpgrades,
                allies: this.allies
            });
        }
    }

    onDefeat() {
        this.scene.start('WaveResultScene', {
            victory: false,
            wave: this.waveManager.currentWave,
            time: this.battleTime,
            allies: this.allies,
            party: this.partyKeys,
            upgrades: this.appliedUpgrades
        });
    }

    shutdown() {
        this.allies.forEach(a => a.destroy());
        this.enemies.forEach(e => e.destroy());
    }
}
