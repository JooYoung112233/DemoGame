class HotelTitleScene extends Phaser.Scene {
    constructor() {
        super('HotelTitleScene');
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');
        this.selectedWeapon = 'pistol';

        this.add.text(640, 50, '호텔 100층', {
            fontSize: '48px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#ff4466'
        }).setOrigin(0.5);

        this.add.text(640, 105, '턴제 확률 전투 로그라이트', {
            fontSize: '16px', fontFamily: 'monospace',
            color: '#888888'
        }).setOrigin(0.5);

        this.add.text(640, 135, '무기를 선택하고 도전하라', {
            fontSize: '14px', fontFamily: 'monospace',
            color: '#666666'
        }).setOrigin(0.5);

        const weapons = ['pistol', 'shotgun', 'energy_rifle'];
        const startX = 200;
        const cardWidth = 260;
        const gap = 30;
        this.weaponCards = [];

        weapons.forEach((key, i) => {
            const w = WEAPONS[key];
            const cx = startX + i * (cardWidth + gap);
            const cy = 350;

            const card = this.add.graphics();
            card.setDepth(1);

            this.add.text(cx, cy - 110, w.name, {
                fontSize: '24px', fontFamily: 'monospace', fontStyle: 'bold',
                color: w.color
            }).setOrigin(0.5).setDepth(2);

            const lightAction = w.actions.find(a => a.id === 'light');
            const heavyAction = w.actions.find(a => a.id === 'heavy');
            const dodgeAction = w.actions.find(a => a.id === 'dodge');

            const lines = [
                '',
                '[ 약공격 ]  ' + Math.floor(lightAction.hitRate * 100) + '%  ' + lightAction.dmgMin + '~' + lightAction.dmgMax + '뎀',
                '[ 강공격 ]  ' + Math.floor(heavyAction.hitRate * 100) + '%  ' + heavyAction.dmgMin + '~' + heavyAction.dmgMax + '뎀',
                '[ 회피율 ]  ' + Math.floor(dodgeAction.hitRate * 100) + '%',
                '',
                '스태미나    ' + w.stamina,
                '',
                w.desc
            ];

            this.add.text(cx, cy + 10, lines.join('\n'), {
                fontSize: '13px', fontFamily: 'monospace',
                color: '#bbbbbb', lineSpacing: 5, align: 'center',
                wordWrap: { width: cardWidth - 30 }
            }).setOrigin(0.5).setDepth(2);

            const hitArea = this.add.rectangle(cx, cy, cardWidth, 260, 0x000000, 0)
                .setInteractive({ useHandCursor: true }).setDepth(3);

            hitArea.on('pointerdown', () => {
                this.selectedWeapon = key;
                this.updateCards();
            });

            this.weaponCards.push({ key, card, hitArea, cx, cy, cardWidth });
        });

        this.updateCards();

        const startBtn = this.add.text(640, 540, '▶ 도전 시작!', {
            fontSize: '24px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#ff4466', backgroundColor: '#2a0a1a',
            padding: { x: 30, y: 12 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        startBtn.on('pointerover', () => startBtn.setColor('#ff6688'));
        startBtn.on('pointerout', () => startBtn.setColor('#ff4466'));
        startBtn.on('pointerdown', () => this.startGame());

        const backBtn = this.add.text(640, 610, '← 메인으로', {
            fontSize: '14px', fontFamily: 'monospace',
            color: '#666666'
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        backBtn.on('pointerdown', () => {
            window.location.href = '../';
        });

        this.add.text(640, 660, '행동 선택 → 확률 판정 → 적 턴 → 반복', {
            fontSize: '12px', fontFamily: 'monospace',
            color: '#444444'
        }).setOrigin(0.5);

        this.add.text(640, 685, '운 + 판단 + 리스크 관리', {
            fontSize: '12px', fontFamily: 'monospace',
            color: '#555555'
        }).setOrigin(0.5);
    }

    updateCards() {
        this.weaponCards.forEach(c => {
            c.card.clear();
            const selected = c.key === this.selectedWeapon;
            const w = WEAPONS[c.key];
            const borderColor = selected ? Phaser.Display.Color.HexStringToColor(w.color).color : 0x333355;
            const bgColor = selected ? 0x1a1020 : 0x111122;

            c.card.fillStyle(bgColor, 1);
            c.card.fillRoundedRect(c.cx - c.cardWidth / 2, c.cy - 130, c.cardWidth, 260, 10);
            c.card.lineStyle(selected ? 3 : 1, borderColor, selected ? 1 : 0.5);
            c.card.strokeRoundedRect(c.cx - c.cardWidth / 2, c.cy - 130, c.cardWidth, 260, 10);
        });
    }

    startGame() {
        const weapon = JSON.parse(JSON.stringify(WEAPONS[this.selectedWeapon]));
        const runState = {
            weapon,
            floor: 0,
            coins: 0,
            hp: 100,
            maxHp: 100,
            stamina: weapon.stamina,
            maxStamina: weapon.maxStamina,
            critRate: 0.05,
            critDmg: 1.5,
            bonusDmg: 0,
            bonusHitRate: 0,
            bonusDodge: 0,
            defendReduction: 0.5,
            staminaRegen: 0,
            staminaDiscount: 0,
            lifesteal: 0,
            poisonDmg: 0,
            poisonDuration: 0,
            counterRate: 0,
            counterDmgMin: 0,
            counterDmgMax: 0,
            focusMultiplier: 2,
            desperatePower: false,
            onKillHeal: 0,
            dodgeCounter: false,
            dodgeCounterMin: 0,
            dodgeCounterMax: 0,
            hasShield: false,
            appliedUpgrades: [],
            totalKills: 0,
            totalDamage: 0,
            totalTime: 0
        };

        this.scene.start('HotelBattleScene', { runState });
    }
}
