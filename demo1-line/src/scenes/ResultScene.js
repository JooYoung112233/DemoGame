class ResultScene extends Phaser.Scene {
    constructor() {
        super({ key: 'ResultScene' });
    }

    init(data) {
        this.victory = data.victory;
        this.stats = data.stats || [];
        this.battleTime = data.battleTime || 0;
        this.party = data.party;
        this.enemies = data.enemies;
    }

    create() {
        this.createBackground();
        this.showResult();
        this.showStats();
        this.createButtons();
    }

    createBackground() {
        const g = this.add.graphics();
        g.fillStyle(0x111122, 0.95);
        g.fillRect(0, 0, 1280, 720);

        if (this.victory) {
            for (let i = 0; i < 30; i++) {
                const x = Math.random() * 1280;
                const y = Math.random() * 720;
                const star = this.add.circle(x, y, 1 + Math.random() * 2, 0xffcc44, 0.5 + Math.random() * 0.5);
                this.tweens.add({
                    targets: star,
                    alpha: 0,
                    duration: 1000 + Math.random() * 2000,
                    yoyo: true,
                    repeat: -1
                });
            }
        }
    }

    showResult() {
        const titleColor = this.victory ? '#44ff88' : '#ff4444';
        const titleText = this.victory ? '승 리 !' : '패 배...';

        this.add.text(640, 80, titleText, {
            fontSize: '48px',
            fontFamily: 'monospace',
            color: titleColor,
            fontStyle: 'bold',
            stroke: '#000000',
            strokeThickness: 6
        }).setOrigin(0.5);

        const mins = Math.floor(this.battleTime / 60000);
        const secs = Math.floor((this.battleTime % 60000) / 1000);
        this.add.text(640, 140, `전투 시간: ${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`, {
            fontSize: '16px',
            fontFamily: 'monospace',
            color: '#aaaaaa'
        }).setOrigin(0.5);
    }

    showStats() {
        this.add.text(640, 190, '[ 전투 통계 ]', {
            fontSize: '18px',
            fontFamily: 'monospace',
            color: '#ffffff',
            stroke: '#000000',
            strokeThickness: 2
        }).setOrigin(0.5);

        const allies = this.stats.filter(s => s.team === 'ally');
        const enemies = this.stats.filter(s => s.team === 'enemy');

        this.add.text(320, 230, '아군', {
            fontSize: '16px', fontFamily: 'monospace', color: '#44ff88'
        }).setOrigin(0.5);

        allies.forEach((s, i) => {
            const y = 270 + i * 55;
            const alive = s.alive ? '생존' : '전사';
            const aliveColor = s.alive ? '#44ff88' : '#ff4444';

            this.add.text(200, y, s.name, {
                fontSize: '14px', fontFamily: 'monospace', color: '#ffffff'
            });
            this.add.text(300, y, alive, {
                fontSize: '12px', fontFamily: 'monospace', color: aliveColor
            });
            this.add.text(200, y + 18, `HP: ${Math.floor(s.hp)}/${s.maxHp}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#888888'
            });
            this.add.text(350, y + 18, `딜: ${Math.floor(s.damageDealt)}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#cc8844'
            });

            const hpRatio = s.hp / s.maxHp;
            const barBg = this.add.rectangle(280, y + 35, 200, 6, 0x333333).setOrigin(0, 0.5);
            const bar = this.add.rectangle(280, y + 35, 200 * hpRatio, 6,
                hpRatio > 0.5 ? 0x44ff44 : hpRatio > 0.25 ? 0xffaa00 : 0xff4444).setOrigin(0, 0.5);
        });

        this.add.text(900, 230, '적군', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ff6644'
        }).setOrigin(0.5);

        enemies.forEach((s, i) => {
            const y = 270 + i * 55;
            const alive = s.alive ? '생존' : '처치';
            const aliveColor = s.alive ? '#ff6644' : '#666666';

            this.add.text(780, y, s.name, {
                fontSize: '14px', fontFamily: 'monospace', color: '#ccaaaa'
            });
            this.add.text(880, y, alive, {
                fontSize: '12px', fontFamily: 'monospace', color: aliveColor
            });
            this.add.text(780, y + 18, `HP: ${Math.floor(s.hp)}/${s.maxHp}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#888888'
            });
        });
    }

    createButtons() {
        const retryBtn = this.add.rectangle(440, 630, 200, 45, 0x4466aa)
            .setStrokeStyle(2, 0x5588cc)
            .setInteractive({ useHandCursor: true });

        this.add.text(440, 630, '↻ 재도전', {
            fontSize: '18px',
            fontFamily: 'monospace',
            color: '#ffffff',
            fontStyle: 'bold'
        }).setOrigin(0.5);

        retryBtn.on('pointerover', () => retryBtn.setFillStyle(0x5577bb));
        retryBtn.on('pointerout', () => retryBtn.setFillStyle(0x4466aa));
        retryBtn.on('pointerdown', () => {
            this.scene.start('BattleScene', {
                party: this.party,
                enemies: this.enemies,
                speed: 1.5
            });
        });

        const setupBtn = this.add.rectangle(840, 630, 200, 45, 0x44aa44)
            .setStrokeStyle(2, 0x66cc66)
            .setInteractive({ useHandCursor: true });

        this.add.text(840, 630, '⚙ 조합 변경', {
            fontSize: '18px',
            fontFamily: 'monospace',
            color: '#ffffff',
            fontStyle: 'bold'
        }).setOrigin(0.5);

        setupBtn.on('pointerover', () => setupBtn.setFillStyle(0x55bb55));
        setupBtn.on('pointerout', () => setupBtn.setFillStyle(0x44aa44));
        setupBtn.on('pointerdown', () => {
            this.scene.start('SetupScene');
        });
    }
}
