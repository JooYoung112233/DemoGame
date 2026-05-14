class ResultScene extends Phaser.Scene {
    constructor() {
        super({ key: 'ResultScene' });
    }

    init(data) {
        this.runState = data.runState;
        this.victory = data.victory;
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');

        const title = this.victory ? '마왕 토벌 성공!' : '여정의 끝...';
        const titleColor = this.victory ? '#44ff88' : '#ff4444';

        this.add.text(640, 60, title, {
            fontSize: '36px', fontFamily: 'monospace', fontStyle: 'bold',
            color: titleColor, stroke: '#000000', strokeThickness: 6
        }).setOrigin(0.5);

        const subtitle = this.victory
            ? '마왕을 물리치고 세상에 평화가 찾아왔다!'
            : '마차는 더 이상 나아갈 수 없었다...';
        this.add.text(640, 110, subtitle, {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        const mapManager = new MapManager();
        const nodesReached = Math.min(this.runState.currentNode + 1, mapManager.getNodeCount());

        const stats = [
            ['도달 노드', `${nodesReached} / ${mapManager.getNodeCount()}`],
            ['전투 횟수', `${this.runState.totalBattles}회`],
            ['총 징집', `${this.runState.totalRecruits}명`],
            ['총 골드 획득', `${this.runState.totalGoldEarned}G`],
            ['남은 골드', `${this.runState.gold}G`],
            ['생존 파티원', `${this.runState.party.length}명`],
        ];

        stats.forEach(([label, value], i) => {
            const y = 180 + i * 36;
            this.add.text(460, y, label, {
                fontSize: '14px', fontFamily: 'monospace', color: '#888888'
            });
            this.add.text(780, y, value, {
                fontSize: '14px', fontFamily: 'monospace', fontStyle: 'bold',
                color: '#ffffff'
            }).setOrigin(1, 0);
        });

        if (this.runState.party.length > 0) {
            this.add.text(640, 410, '[ 생존 파티원 ]', {
                fontSize: '14px', fontFamily: 'monospace', fontStyle: 'bold', color: '#ffaa44'
            }).setOrigin(0.5);

            this.runState.party.forEach((unit, i) => {
                const stats = unit.getStats();
                const y = 440 + i * 28;
                const colorHex = '#' + stats.color.toString(16).padStart(6, '0');
                const traits = unit.traits.map(t => t.name).join(', ');

                this.add.text(300, y, `${stats.name} [T${unit.tier}]`, {
                    fontSize: '12px', fontFamily: 'monospace', fontStyle: 'bold', color: colorHex
                });
                this.add.text(500, y, `HP:${unit.currentHp}/${stats.hp} ATK:${stats.atk} DEF:${stats.def}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#888888'
                });
                if (traits) {
                    this.add.text(780, y, traits, {
                        fontSize: '10px', fontFamily: 'monospace', color: '#aa88cc'
                    });
                }
            });
        }

        const retryBtn = this.add.rectangle(640, 660, 180, 44, 0x3a2a1a)
            .setStrokeStyle(2, 0xffaa44)
            .setInteractive({ useHandCursor: true });

        const retryText = this.add.text(640, 660, '다시 도전', {
            fontSize: '18px', fontFamily: 'monospace', fontStyle: 'bold', color: '#ffaa44'
        }).setOrigin(0.5);

        retryBtn.on('pointerover', () => { retryBtn.setFillStyle(0x5a4a2a); retryText.setColor('#ffcc66'); });
        retryBtn.on('pointerout', () => { retryBtn.setFillStyle(0x3a2a1a); retryText.setColor('#ffaa44'); });
        retryBtn.on('pointerdown', () => {
            this.scene.start('CaravanTitleScene');
        });
    }
}
