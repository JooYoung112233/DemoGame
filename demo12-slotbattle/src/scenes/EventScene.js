class EventScene extends Phaser.Scene {
    constructor() {
        super('EventScene');
    }

    init(data) {
        this.act = data.act;
        this.map = data.map;
        this.playerState = data.playerState;
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a1a');

        const events = this._getEventPool();
        this.event = events[Phaser.Math.Between(0, events.length - 1)];

        this.add.text(W / 2, 80, '❓', { fontSize: '56px' }).setOrigin(0.5);
        this.add.text(W / 2, 140, this.event.title, {
            fontSize: '26px', fontFamily: 'monospace', color: '#cc88ff', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(W / 2, 190, this.event.desc, {
            fontSize: '14px', fontFamily: 'monospace', color: '#aaaaaa',
            wordWrap: { width: 600 }, align: 'center'
        }).setOrigin(0.5);

        const startY = 280;
        for (let i = 0; i < this.event.choices.length; i++) {
            const choice = this.event.choices[i];
            const y = startY + i * 70;

            const btn = this.add.text(W / 2, y, choice.label, {
                fontSize: '18px', fontFamily: 'monospace', color: choice.color || '#cccccc',
                backgroundColor: '#1a1a35', padding: { x: 24, y: 10 },
                wordWrap: { width: 500 }, align: 'center'
            }).setOrigin(0.5).setInteractive({ useHandCursor: true });

            btn.on('pointerover', () => btn.setColor('#ffffff'));
            btn.on('pointerout', () => btn.setColor(choice.color || '#cccccc'));
            btn.on('pointerdown', () => this._applyChoice(choice));
        }
    }

    _applyChoice(choice) {
        const W = 1280;
        const e = choice.effect;
        const results = [];

        if (e.gold) { this.playerState.gold += e.gold; results.push(`🪙 ${e.gold > 0 ? '+' : ''}${e.gold} 골드`); }
        if (e.hp) { this.playerState.hp = Math.min(this.playerState.maxHp, this.playerState.hp + e.hp); results.push(`❤️ ${e.hp > 0 ? '+' : ''}${e.hp} HP`); }
        if (e.maxHp) { this.playerState.maxHp += e.maxHp; this.playerState.hp += e.maxHp; results.push(`💖 최대HP ${e.maxHp > 0 ? '+' : ''}${e.maxHp}`); }
        if (e.addSymbol) {
            this.playerState.symbolPool.push(e.addSymbol);
            this.playerState.codex[e.addSymbol] = true;
            const s = SYMBOL_DATA[e.addSymbol];
            results.push(`${s ? s.icon : ''} ${s ? s.name : e.addSymbol} 획득`);
        }
        if (e.removeRandom) {
            if (this.playerState.symbolPool.length > 3) {
                const idx = Phaser.Math.Between(0, this.playerState.symbolPool.length - 1);
                const removed = this.playerState.symbolPool.splice(idx, 1)[0];
                const s = SYMBOL_DATA[removed];
                results.push(`${s ? s.icon : ''} ${s ? s.name : removed} 상실`);
            }
        }
        if (e.addSkull) {
            this.playerState.symbolPool.push('skull');
            results.push('💀 해골 추가됨');
        }
        if (e.block) { this.playerState.block += e.block; results.push(`🛡️ +${e.block} 방어`); }

        const resultText = results.join('\n');
        this.add.text(W / 2, 520, resultText, {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffffff',
            align: 'center', stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5);

        this.time.delayedCall(800, () => this._goBack());
    }

    _goBack() {
        this.scene.start('MapScene', {
            act: this.act,
            map: this.map,
            playerState: this.playerState
        });
    }

    _getEventPool() {
        return [
            {
                title: '수상한 상인',
                desc: '낡은 망토를 쓴 상인이 거래를 제안한다.',
                choices: [
                    { label: '💰 골드를 지불하고 좋은 심볼 획득 (-10G)', color: '#ffcc00',
                      effect: { gold: -10, addSymbol: 'lightning' } },
                    { label: '🗡️ 위협하여 공짜로 가져간다 (해골 추가)', color: '#ff4444',
                      effect: { addSymbol: 'bomb', addSkull: true } },
                    { label: '무시하고 지나간다', color: '#888888', effect: {} },
                ]
            },
            {
                title: '버려진 제단',
                desc: '고대의 제단에서 신비한 기운이 느껴진다.',
                choices: [
                    { label: '🙏 기도한다 (HP +15)', color: '#44ff88',
                      effect: { hp: 15 } },
                    { label: '💎 제물을 바친다 (-5G, 최대HP +5)', color: '#ffcc00',
                      effect: { gold: -5, maxHp: 5 } },
                    { label: '⚔️ 부순다 (+15G, -10 HP)', color: '#ff4444',
                      effect: { gold: 15, hp: -10 } },
                ]
            },
            {
                title: '떠돌이 대장장이',
                desc: '대장장이가 무기를 하나 만들어주겠다고 한다.',
                choices: [
                    { label: '⚔️ 검을 만든다 (검 추가)', color: '#ff4444',
                      effect: { addSymbol: 'axe' } },
                    { label: '🛡️ 방패를 만든다 (성벽 추가)', color: '#4488ff',
                      effect: { addSymbol: 'fortress' } },
                    { label: '💰 재료를 팔아 골드를 번다 (+12G)', color: '#ffcc00',
                      effect: { gold: 12 } },
                ]
            },
            {
                title: '저주받은 상자',
                desc: '검은 기운이 감도는 상자가 놓여 있다.',
                choices: [
                    { label: '📦 연다 (랜덤 심볼 + 해골)', color: '#cc88ff',
                      effect: { addSymbol: 'heart', addSkull: true } },
                    { label: '🔥 불태운다 (+8G)', color: '#ff8800',
                      effect: { gold: 8 } },
                    { label: '지나친다', color: '#888888', effect: {} },
                ]
            },
            {
                title: '마법의 샘',
                desc: '맑은 물이 솟아나는 샘을 발견했다.',
                choices: [
                    { label: '💧 마신다 (HP 전부 회복)', color: '#44ff88',
                      effect: { hp: 999 } },
                    { label: '🧪 물을 담는다 (포션 추가)', color: '#88ff44',
                      effect: { addSymbol: 'potion' } },
                ]
            },
            {
                title: '도박꾼의 텐트',
                desc: '"한 판 하겠나?" 도박꾼이 웃으며 말한다.',
                choices: [
                    { label: '🎲 건다 (-8G, 50% 확률로 +20G)', color: '#ffcc00',
                      effect: Math.random() < 0.5 ? { gold: 12 } : { gold: -8 } },
                    { label: '거절한다', color: '#888888', effect: {} },
                ]
            },
        ];
    }
}
