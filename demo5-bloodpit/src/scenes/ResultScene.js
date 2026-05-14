class BPResultScene extends Phaser.Scene {
    constructor() { super('BPResultScene'); }

    init(data) {
        this.victory = data.victory;
        this.escaped = data.escaped || false;
        this.dangerLevel = data.dangerLevel || 0;
        this.drops = data.drops || [];
        this.allies = data.allies || [];
        this.partyIds = data.party;
        this.battleTime = data.time || 0;
    }

    create() {
        this.cameras.main.setBackgroundColor('#1a0a0a');
        this.cameras.main.fadeIn(400);
        const cx = 640;

        let titleText, titleColor;
        if (this.victory) {
            titleText = '💀 보스 격파! 완전 승리!';
            titleColor = '#ffaa00';
        } else if (this.escaped) {
            titleText = '🚪 탈출 성공!';
            titleColor = '#44ff88';
        } else {
            titleText = '☠️ 전멸...';
            titleColor = '#ff4444';
        }

        this.add.text(cx, 35, titleText, {
            fontSize: '30px', fontFamily: 'monospace', color: titleColor, fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(cx, 70, `도달 위험도: ${this.dangerLevel} / 20  |  시간: ${(this.battleTime / 1000).toFixed(1)}초`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#cccccc'
        }).setOrigin(0.5);

        StashManager.recordRaid({ extracted: this.victory, totalValue: this.victory ? StashManager.getGold() : 0 });

        // character results
        this.add.text(cx, 105, '── 캐릭터 성적 ──', {
            fontSize: '12px', fontFamily: 'monospace', color: '#884444'
        }).setOrigin(0.5);

        this.allies.forEach((a, i) => {
            const y = 128 + i * 26;
            const alive = a.alive ? '생존' : '전사';
            const aliveColor = a.alive ? '#44ff88' : '#ff4444';

            this.add.text(180, y, a.name, { fontSize: '12px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold' });
            this.add.text(340, y, alive, { fontSize: '11px', fontFamily: 'monospace', color: aliveColor });
            this.add.text(400, y, a.alive ? `HP ${a.hp}/${a.maxHp}` : '', { fontSize: '10px', fontFamily: 'monospace', color: '#aaaaaa' });
            this.add.text(540, y, `데미지: ${a.totalDamageDealt || 0}`, { fontSize: '10px', fontFamily: 'monospace', color: '#ffcc44' });

            const maxDmg = Math.max(...this.allies.map(c => c.totalDamageDealt || 0), 1);
            const barW = ((a.totalDamageDealt || 0) / maxDmg) * 250;
            const bar = this.add.graphics();
            bar.fillStyle(Phaser.Display.Color.ValueToColor(aliveColor).color, 0.3);
            bar.fillRect(680, y + 2, barW, 4);
        });

        // roster status summary
        let ry = 260;
        this.add.text(cx, ry, '── 로스터 상태 ──', {
            fontSize: '12px', fontFamily: 'monospace', color: '#884444'
        }).setOrigin(0.5);
        ry += 22;

        BP_ROSTER.roster.forEach((char, i) => {
            const base = BP_ALLIES[char.classKey];
            if (!base) return;
            const statusLabel = char.status === 'dead' ? `💀 사망(${char.recoveryRounds})` :
                char.status === 'injured' ? `🩹 부상(${char.recoveryRounds})` : '✅ 준비';
            const statusColor = char.status === 'ready' ? '#44ff88' : '#ff6644';
            this.add.text(280, ry + i * 18, `${base.name} ${char.name}  Lv.${char.level}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#aaaaaa'
            });
            this.add.text(640, ry + i * 18, statusLabel, {
                fontSize: '10px', fontFamily: 'monospace', color: statusColor
            });
        });

        // collected drops
        ry = ry + BP_ROSTER.roster.length * 18 + 15;
        if (this.drops.length > 0) {
            this.add.text(cx, ry, '── 인게임 보상 ──', {
                fontSize: '11px', fontFamily: 'monospace', color: '#664444'
            }).setOrigin(0.5);
            ry += 20;
            const names = this.drops.map(d => d.name);
            this.add.text(cx, ry, names.join('  |  '), {
                fontSize: '10px', fontFamily: 'monospace', color: '#886666',
                wordWrap: { width: 1000 }, align: 'center'
            }).setOrigin(0.5);
            ry += 25;
        }

        // gold / stash
        const gold = StashManager.getGold();
        const stash = StashManager.getStash();
        this.add.text(cx, Math.min(ry + 10, 580), `💰 총 골드: ${gold}  |  📦 스태시: ${stash.length}/${FARMING.STASH_CAPACITY}`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffaa44'
        }).setOrigin(0.5);

        // buttons
        const btnY = Math.min(ry + 55, 630);
        const retryBtn = this.add.rectangle(440, btnY, 200, 45, 0x882222).setStrokeStyle(2, 0xff4444).setInteractive();
        this.add.text(440, btnY, '다시 도전', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);
        retryBtn.on('pointerover', () => retryBtn.setFillStyle(0xaa3333));
        retryBtn.on('pointerout', () => retryBtn.setFillStyle(0x882222));
        retryBtn.on('pointerdown', () => this.scene.start('BPSetupScene'));

        const menuBtn = this.add.rectangle(840, btnY, 200, 45, 0x443333).setStrokeStyle(2, 0x886666).setInteractive();
        this.add.text(840, btnY, '편성 변경', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);
        menuBtn.on('pointerover', () => menuBtn.setFillStyle(0x554444));
        menuBtn.on('pointerout', () => menuBtn.setFillStyle(0x443333));
        menuBtn.on('pointerdown', () => this.scene.start('BPSetupScene'));

        this.add.text(cx, Math.min(btnY + 40, 690), '← 목록으로', { fontSize: '11px', fontFamily: 'monospace', color: '#664444' })
            .setOrigin(0.5).setInteractive()
            .on('pointerdown', () => { window.location.href = '../index.html'; });
    }
}
