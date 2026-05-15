class EventScene extends Phaser.Scene {
    constructor() { super('EventScene'); }

    init(data) {
        this.gameState = data.gameState;
    }

    create() {
        const gs = this.gameState;
        const eventKey = pickTownEvent(gs.guildLevel);
        this.eventData = TOWN_EVENTS[eventKey];

        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);

        this.add.text(640, 40, '📜 본진 이벤트', {
            fontSize: '22px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5);

        const tierColors = { 1: '#aaaacc', 2: '#ffaa44', 3: '#ff44aa' };
        const tierLabels = { 1: '일상', 2: '구역 연계', 3: '세계관' };
        this.add.text(640, 70, `[${tierLabels[this.eventData.tier]}]`, {
            fontSize: '11px', fontFamily: 'monospace', color: tierColors[this.eventData.tier]
        }).setOrigin(0.5);

        const iconSize = 60;
        const iconBg = this.add.rectangle(640, 160, iconSize + 20, iconSize + 20, 0x1a1a2a).setStrokeStyle(2, 0x444466);
        this.add.text(640, 160, this.eventData.icon, {
            fontSize: '36px'
        }).setOrigin(0.5);

        this.add.text(640, 220, this.eventData.name, {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(640, 260, this.eventData.desc, {
            fontSize: '13px', fontFamily: 'monospace', color: '#aaaacc',
            wordWrap: { width: 600 }, align: 'center'
        }).setOrigin(0.5);

        const canA = canAffordEventChoice(gs, this.eventData.choiceA);
        const canB = canAffordEventChoice(gs, this.eventData.choiceB);

        const btnW = 280, btnH = 50;
        const btnY = 360;

        // Choice A
        const aBg = this.add.graphics();
        const aColor = canA ? 0x1a2a3a : 0x1a1a22;
        const aBorder = canA ? 0x4488ff : 0x333344;
        aBg.fillStyle(aColor, 1);
        aBg.fillRoundedRect(640 - btnW - 20, btnY - btnH / 2, btnW, btnH, 6);
        aBg.lineStyle(2, aBorder, 0.8);
        aBg.strokeRoundedRect(640 - btnW - 20, btnY - btnH / 2, btnW, btnH, 6);

        this.add.text(640 - btnW / 2 - 20, btnY - 8, this.eventData.choiceA.label, {
            fontSize: '14px', fontFamily: 'monospace', color: canA ? '#4488ff' : '#555566', fontStyle: 'bold'
        }).setOrigin(0.5);

        if (canA) {
            const aZone = this.add.zone(640 - btnW / 2 - 20, btnY, btnW, btnH).setInteractive({ useHandCursor: true });
            aZone.on('pointerover', () => {
                aBg.clear();
                aBg.fillStyle(0x2a3a4a, 1);
                aBg.fillRoundedRect(640 - btnW - 20, btnY - btnH / 2, btnW, btnH, 6);
                aBg.lineStyle(3, 0x4488ff, 1);
                aBg.strokeRoundedRect(640 - btnW - 20, btnY - btnH / 2, btnW, btnH, 6);
            });
            aZone.on('pointerout', () => {
                aBg.clear();
                aBg.fillStyle(aColor, 1);
                aBg.fillRoundedRect(640 - btnW - 20, btnY - btnH / 2, btnW, btnH, 6);
                aBg.lineStyle(2, aBorder, 0.8);
                aBg.strokeRoundedRect(640 - btnW - 20, btnY - btnH / 2, btnW, btnH, 6);
            });
            aZone.on('pointerdown', () => this._selectChoice('A'));
        }

        if (!canA && this.eventData.choiceA.cost) {
            this.add.text(640 - btnW / 2 - 20, btnY + 12, '(자원 부족)', {
                fontSize: '10px', fontFamily: 'monospace', color: '#664444'
            }).setOrigin(0.5);
        }

        // Choice B
        const bBg = this.add.graphics();
        bBg.fillStyle(0x2a1a1a, 1);
        bBg.fillRoundedRect(640 + 20, btnY - btnH / 2, btnW, btnH, 6);
        bBg.lineStyle(2, 0x886644, 0.8);
        bBg.strokeRoundedRect(640 + 20, btnY - btnH / 2, btnW, btnH, 6);

        this.add.text(640 + btnW / 2 + 20, btnY - 8, this.eventData.choiceB.label, {
            fontSize: '14px', fontFamily: 'monospace', color: '#cc8866', fontStyle: 'bold'
        }).setOrigin(0.5);

        const bZone = this.add.zone(640 + btnW / 2 + 20, btnY, btnW, btnH).setInteractive({ useHandCursor: true });
        bZone.on('pointerover', () => {
            bBg.clear();
            bBg.fillStyle(0x3a2a2a, 1);
            bBg.fillRoundedRect(640 + 20, btnY - btnH / 2, btnW, btnH, 6);
            bBg.lineStyle(3, 0xcc8866, 1);
            bBg.strokeRoundedRect(640 + 20, btnY - btnH / 2, btnW, btnH, 6);
        });
        bZone.on('pointerout', () => {
            bBg.clear();
            bBg.fillStyle(0x2a1a1a, 1);
            bBg.fillRoundedRect(640 + 20, btnY - btnH / 2, btnW, btnH, 6);
            bBg.lineStyle(2, 0x886644, 0.8);
            bBg.strokeRoundedRect(640 + 20, btnY - btnH / 2, btnW, btnH, 6);
        });
        bZone.on('pointerdown', () => this._selectChoice('B'));

        this.add.text(640, 680, `💰 ${gs.gold}G  |  길드 Lv.${gs.guildLevel}`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#886666'
        }).setOrigin(0.5);
    }

    _selectChoice(choice) {
        const gs = this.gameState;
        const evt = this.eventData;
        let resultText;

        if (choice === 'A') {
            payEventCost(gs, evt.choiceA);
            resultText = evt.applyA(gs);
        } else {
            payEventCost(gs, evt.choiceB);
            resultText = evt.applyB(gs);
        }

        GuildManager.addMessage(gs, `[이벤트] ${evt.name}: ${resultText}`);
        SaveManager.save(gs);

        this.children.removeAll(true);
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);

        this.add.text(640, 200, evt.icon, { fontSize: '48px' }).setOrigin(0.5);
        this.add.text(640, 270, evt.name, {
            fontSize: '22px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);

        const choiceLabel = choice === 'A' ? evt.choiceA.label : evt.choiceB.label;
        this.add.text(640, 310, `선택: ${choiceLabel}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#886688'
        }).setOrigin(0.5);

        this.add.text(640, 380, resultText, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            wordWrap: { width: 600 }, align: 'center'
        }).setOrigin(0.5);

        UIButton.create(this, 640, 520, 200, 44, '본진으로', {
            color: 0xffaa44, hoverColor: 0xffcc66, textColor: '#000000', fontSize: 16,
            onClick: () => {
                MercenaryManager.generateRecruitPool(gs);
                SaveManager.save(gs);
                this.scene.start('TownScene', { gameState: gs });
            }
        });
    }
}
