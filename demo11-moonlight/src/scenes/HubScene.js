class HubScene extends Phaser.Scene {
    constructor() { super('HubScene'); }

    create() {
        const gs = window.gameState;
        const cx = 640;

        this.add.rectangle(cx, 360, 1280, 720, 0x0a0a1a);

        // 상단 정보바
        this.add.rectangle(cx, 30, 1280, 60, 0x151530);
        this.dayText = this.add.text(20, 18, '', { fontSize: '16px', fontFamily: 'monospace', color: '#ffeebb' });
        this.goldText = this.add.text(200, 18, '', { fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44' });
        this.repText = this.add.text(400, 18, '', { fontSize: '16px', fontFamily: 'monospace', color: '#44ccff' });
        this.demandText = this.add.text(600, 18, '', { fontSize: '14px', fontFamily: 'monospace', color: '#aaa' });

        this._updateInfo();

        // 메인 타이틀
        this.add.text(cx, 100, '🌙 Moonlight', {
            fontSize: '36px', fontFamily: 'monospace', color: '#ffeebb',
            stroke: '#000', strokeThickness: 3,
        }).setOrigin(0.5);

        // 하루 흐름 버튼들
        this.add.text(cx, 170, `Day ${gs.day} — 무엇을 하시겠습니까?`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#888',
        }).setOrigin(0.5);

        // 낮: 원정
        new UIButton(this, cx, 260, '☀️  원정 떠나기 (낮)', {
            width: 340, height: 60, bg: 0x2a2a1a, hoverBg: 0x3a3a2a,
            textColor: '#ffcc44', fontSize: '18px',
            onClick: () => this.scene.start('ExpeditionScene'),
        });

        this.add.text(cx, 300, '구역을 선택하고 재료를 수집합니다', {
            fontSize: '12px', fontFamily: 'monospace', color: '#666',
        }).setOrigin(0.5);

        // 밤: 상점
        new UIButton(this, cx, 380, '🌙  상점 열기 (밤)', {
            width: 340, height: 60, bg: 0x1a1a2a, hoverBg: 0x2a2a3a,
            textColor: '#aaaaff', fontSize: '18px',
            onClick: () => this.scene.start('ShopScene'),
        });

        this.add.text(cx, 420, '아이템을 진열하고 손님에게 판매합니다', {
            fontSize: '12px', fontFamily: 'monospace', color: '#666',
        }).setOrigin(0.5);

        // 인벤토리
        new UIButton(this, cx, 490, '🎒  인벤토리', {
            width: 340, height: 50, bg: 0x1a2a1a, hoverBg: 0x2a3a2a,
            textColor: '#44ff88', fontSize: '16px',
            onClick: () => this.scene.start('InventoryScene'),
        });

        // 제작 (Day 5+)
        if (gs.day >= 5) {
            new UIButton(this, cx, 555, '🔨  제작', {
                width: 340, height: 50, bg: 0x2a1a1a, hoverBg: 0x3a2a2a,
                textColor: '#ff8844', fontSize: '16px',
                onClick: () => this.scene.start('CraftScene'),
            });
        }

        // 하단 통계
        this.add.text(cx, 650, `총 매출: ${gs.totalEarned}G  |  총 판매: ${gs.totalSold}건`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#555',
        }).setOrigin(0.5);
    }

    _updateInfo() {
        const gs = window.gameState;
        this.dayText.setText(`📅 Day ${gs.day}`);
        this.goldText.setText(`💰 ${gs.gold}G`);
        this.repText.setText(`⭐ 평판 ${gs.reputation}`);
        const cat = CATEGORIES[gs.demandCategory];
        this.demandText.setText(`📈 수요: ${cat ? cat.name : '?'}`);
    }
}
