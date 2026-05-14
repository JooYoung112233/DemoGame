class ATBResultScene extends Phaser.Scene {
    constructor() { super({ key: 'ATBResultScene' }); }

    init(data) {
        this.victory = data.victory;
        this.stats = data.stats || [];
        this.battleTime = data.battleTime || 0;
        this.totalTurns = data.totalTurns || 0;
        this.party = data.party;
        this.enemies = data.enemies;
    }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x111122);

        const titleColor = this.victory ? '#44ff88' : '#ff4444';
        const titleText = this.victory ? '승 리 !' : '패 배...';
        this.add.text(640, 70, titleText, {
            fontSize: '48px', fontFamily: 'monospace', color: titleColor,
            fontStyle: 'bold', stroke: '#000000', strokeThickness: 6
        }).setOrigin(0.5);

        const mins = Math.floor(this.battleTime / 60000);
        const secs = Math.floor((this.battleTime % 60000) / 1000);
        this.add.text(640, 130, '전투 시간: ' + String(mins).padStart(2,'0') + ':' + String(secs).padStart(2,'0') + '  |  총 턴: ' + this.totalTurns, {
            fontSize: '15px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(0.5);

        this.add.text(640, 175, '[ 전투 통계 ]', { fontSize: '18px', fontFamily: 'monospace', color: '#fff', stroke: '#000', strokeThickness: 2 }).setOrigin(0.5);

        const allies = this.stats.filter(s => s.team === 'ally');
        const enemies = this.stats.filter(s => s.team === 'enemy');

        this.add.text(320, 210, '아군', { fontSize: '16px', fontFamily: 'monospace', color: '#44ff88' }).setOrigin(0.5);
        allies.forEach((s, i) => {
            const y = 250 + i * 60;
            this.add.text(180, y, s.name, { fontSize: '14px', fontFamily: 'monospace', color: '#fff' });
            this.add.text(300, y, s.alive ? '생존' : '전사', { fontSize: '12px', fontFamily: 'monospace', color: s.alive ? '#44ff88' : '#ff4444' });
            this.add.text(180, y + 18, 'HP:' + Math.floor(s.hp) + '/' + s.maxHp + '  딜:' + Math.floor(s.damageDealt) + '  턴:' + s.turns, {
                fontSize: '10px', fontFamily: 'monospace', color: '#888'
            });
            const r = s.hp / s.maxHp;
            this.add.rectangle(280, y + 38, 200, 5, 0x333333).setOrigin(0, 0.5);
            this.add.rectangle(280, y + 38, 200 * r, 5, r > 0.5 ? 0x44ff44 : r > 0.25 ? 0xffaa00 : 0xff4444).setOrigin(0, 0.5);
        });

        this.add.text(900, 210, '적군', { fontSize: '16px', fontFamily: 'monospace', color: '#ff6644' }).setOrigin(0.5);
        enemies.forEach((s, i) => {
            const y = 250 + i * 50;
            this.add.text(760, y, s.name, { fontSize: '14px', fontFamily: 'monospace', color: '#ccaaaa' });
            this.add.text(880, y, s.alive ? '생존' : '처치', { fontSize: '12px', fontFamily: 'monospace', color: s.alive ? '#ff6644' : '#666' });
            this.add.text(760, y + 18, 'HP:' + Math.floor(s.hp) + '/' + s.maxHp, { fontSize: '10px', fontFamily: 'monospace', color: '#888' });
        });

        const retryBtn = this.add.rectangle(440, 640, 200, 45, 0x4466aa).setStrokeStyle(2, 0x5588cc).setInteractive({ useHandCursor: true });
        this.add.text(440, 640, '↻ 재도전', { fontSize: '18px', fontFamily: 'monospace', color: '#fff', fontStyle: 'bold' }).setOrigin(0.5);
        retryBtn.on('pointerover', () => retryBtn.setFillStyle(0x5577bb));
        retryBtn.on('pointerout', () => retryBtn.setFillStyle(0x4466aa));
        retryBtn.on('pointerdown', () => this.scene.start('ATBBattleScene', { party: this.party, enemies: this.enemies, speed: 1.5 }));

        const setupBtn = this.add.rectangle(840, 640, 200, 45, 0xaa8800).setStrokeStyle(2, 0xccaa22).setInteractive({ useHandCursor: true });
        this.add.text(840, 640, '⚙ 조합 변경', { fontSize: '18px', fontFamily: 'monospace', color: '#fff', fontStyle: 'bold' }).setOrigin(0.5);
        setupBtn.on('pointerover', () => setupBtn.setFillStyle(0xbb9911));
        setupBtn.on('pointerout', () => setupBtn.setFillStyle(0xaa8800));
        setupBtn.on('pointerdown', () => this.scene.start('ATBSetupScene'));
    }
}
