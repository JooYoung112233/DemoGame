class WaveResultScene extends Phaser.Scene {
    constructor() { super('WaveResultScene'); }

    init(data) {
        this.victory = data.victory;
        this.wave = data.wave;
        this.battleTime = data.time;
        this.allies = data.allies || [];
        this.partyKeys = data.party;
        this.upgrades = data.upgrades || [];
    }

    create() {
        this.cameras.main.setBackgroundColor('#1a1a2e');
        const cx = 640;

        const titleColor = this.victory ? '#44ff88' : '#ff4444';
        const titleText = this.victory ? '승리! 모든 웨이브 클리어!' : '패배...';
        this.add.text(cx, 60, titleText, {
            fontSize: '36px', fontFamily: 'monospace', color: titleColor, fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(cx, 110, `도달 웨이브: ${this.wave} / 10`, {
            fontSize: '18px', fontFamily: 'monospace', color: '#cccccc'
        }).setOrigin(0.5);

        const totalTime = (this.battleTime / 1000).toFixed(1);
        this.add.text(cx, 145, `총 전투 시간: ${totalTime}초`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(0.5);

        this.add.text(cx, 200, '── 캐릭터 성적 ──', {
            fontSize: '16px', fontFamily: 'monospace', color: '#8888aa'
        }).setOrigin(0.5);

        this.allies.forEach((a, i) => {
            const y = 250 + i * 55;
            const alive = a.alive ? '생존' : '전사';
            const aliveColor = a.alive ? '#44ff88' : '#ff4444';
            const hpText = a.alive ? `HP ${a.hp}/${a.maxHp}` : '';

            this.add.text(200, y, a.name, {
                fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            });
            this.add.text(360, y, alive, {
                fontSize: '14px', fontFamily: 'monospace', color: aliveColor
            });
            this.add.text(460, y, hpText, {
                fontSize: '13px', fontFamily: 'monospace', color: '#aaaaaa'
            });
            this.add.text(620, y, `데미지: ${a.totalDamageDealt}`, {
                fontSize: '13px', fontFamily: 'monospace', color: '#ffcc44'
            });

            const bar = this.add.graphics();
            const maxDmg = Math.max(...this.allies.map(c => c.totalDamageDealt), 1);
            const barW = (a.totalDamageDealt / maxDmg) * 300;
            bar.fillStyle(Phaser.Display.Color.ValueToColor(aliveColor).color, 0.3);
            bar.fillRect(200, y + 22, barW, 6);
        });

        if (this.upgrades.length > 0) {
            this.add.text(cx, 490, '── 적용된 강화 ──', {
                fontSize: '14px', fontFamily: 'monospace', color: '#6666aa'
            }).setOrigin(0.5);

            const counts = {};
            this.upgrades.forEach(id => { counts[id] = (counts[id] || 0) + 1; });
            const labels = Object.entries(counts).map(([id, cnt]) => {
                const u = UPGRADES.find(u => u.id === id);
                return cnt > 1 ? `${u.name} ×${cnt}` : u.name;
            });
            this.add.text(cx, 525, labels.join('  |  '), {
                fontSize: '12px', fontFamily: 'monospace', color: '#8888aa',
                wordWrap: { width: 1000 }, align: 'center'
            }).setOrigin(0.5);
        }

        const retryBtn = this.add.rectangle(440, 620, 200, 50, 0x44aa44).setStrokeStyle(2, 0x66cc66).setInteractive();
        this.add.text(440, 620, '다시 도전', { fontSize: '18px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold' }).setOrigin(0.5);
        retryBtn.on('pointerover', () => retryBtn.setFillStyle(0x55bb55));
        retryBtn.on('pointerout', () => retryBtn.setFillStyle(0x44aa44));
        retryBtn.on('pointerdown', () => {
            this.scene.start('WaveSetupScene');
        });

        const changeBtn = this.add.rectangle(840, 620, 200, 50, 0x4466aa).setStrokeStyle(2, 0x6688cc).setInteractive();
        this.add.text(840, 620, '편성 변경', { fontSize: '18px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold' }).setOrigin(0.5);
        changeBtn.on('pointerover', () => changeBtn.setFillStyle(0x5577bb));
        changeBtn.on('pointerout', () => changeBtn.setFillStyle(0x4466aa));
        changeBtn.on('pointerdown', () => {
            this.scene.start('WaveSetupScene');
        });

        this.add.text(cx, 690, '← 메인으로', { fontSize: '14px', fontFamily: 'monospace', color: '#6666aa' })
            .setOrigin(0.5).setInteractive()
            .on('pointerdown', () => { window.location.href = '../index.html'; });
    }
}
