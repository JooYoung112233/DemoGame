class ResultScene extends Phaser.Scene {
    constructor() {
        super('ResultScene');
    }

    init(data) {
        this.survived = data.survived || false;
        this.items = data.items || [];
        this.totalValue = data.totalValue || 0;
        this.timeElapsed = data.timeElapsed || 0;
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a0a');

        if (this.survived) {
            this.add.text(W / 2, 80, '🚪 탈출 성공!', {
                fontSize: '36px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
            }).setOrigin(0.5);
        } else {
            this.add.text(W / 2, 80, '💀 사망', {
                fontSize: '36px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
            }).setOrigin(0.5);
            this.add.text(W / 2, 120, '감염자에게 당했다...', {
                fontSize: '16px', fontFamily: 'monospace', color: '#888888'
            }).setOrigin(0.5);
        }

        // Stats
        const minutes = Math.floor(this.timeElapsed / 60);
        const seconds = Math.floor(this.timeElapsed % 60);
        this.add.text(W / 2, 160, `탐색 시간: ${minutes}분 ${seconds}초`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        // Loot summary
        if (this.survived && this.items.length > 0) {
            this.add.text(W / 2, 210, '── 확보한 물자 ──', {
                fontSize: '18px', fontFamily: 'monospace', color: '#cc8844'
            }).setOrigin(0.5);

            const cols = 4;
            const startX = W / 2 - (cols - 1) * 140 / 2;
            for (let i = 0; i < this.items.length && i < 20; i++) {
                const item = this.items[i];
                const col = i % cols;
                const row = Math.floor(i / cols);
                const x = startX + col * 140;
                const y = 260 + row * 60;

                this.add.text(x, y, item.icon, { fontSize: '24px' }).setOrigin(0.5);
                const rarityColor = item.rarity === 'rare' ? '#ffaa44' :
                                    item.rarity === 'uncommon' ? '#44aaff' : '#aaaaaa';
                this.add.text(x, y + 20, item.name, {
                    fontSize: '11px', fontFamily: 'monospace', color: rarityColor
                }).setOrigin(0.5);
            }

            this.add.text(W / 2, 560, `총 가치: 💰 ${this.totalValue}`, {
                fontSize: '22px', fontFamily: 'monospace', color: '#ffcc00', fontStyle: 'bold'
            }).setOrigin(0.5);
        } else if (this.survived) {
            this.add.text(W / 2, 300, '빈손으로 돌아왔다...', {
                fontSize: '18px', fontFamily: 'monospace', color: '#666666'
            }).setOrigin(0.5);
        }

        // Grade
        if (this.survived) {
            const grade = this.totalValue >= 300 ? 'S' :
                          this.totalValue >= 200 ? 'A' :
                          this.totalValue >= 100 ? 'B' :
                          this.totalValue >= 50 ? 'C' : 'D';
            const gradeColor = grade === 'S' ? '#ffcc00' :
                               grade === 'A' ? '#44ff88' :
                               grade === 'B' ? '#44aaff' : '#888888';
            this.add.text(W / 2, 600, `등급: ${grade}`, {
                fontSize: '28px', fontFamily: 'monospace', color: gradeColor, fontStyle: 'bold'
            }).setOrigin(0.5);
        }

        // Retry button
        const retryBtn = this.add.text(W / 2, 670, '[ 다시 출발 ]', {
            fontSize: '24px', fontFamily: 'monospace', color: '#cc8844',
            backgroundColor: '#1a1a10', padding: { x: 24, y: 8 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });
        retryBtn.on('pointerover', () => retryBtn.setColor('#ffcc88'));
        retryBtn.on('pointerout', () => retryBtn.setColor('#cc8844'));
        retryBtn.on('pointerdown', () => this.scene.start('TitleScene'));
    }
}
