const BP_EVENTS = {
    treasure_chest: {
        name: '보물 상자',
        desc: '낡은 상자를 발견했다. 보물이 들어있을 수도, 함정일 수도 있다.',
        icon: '📦',
        choices: [
            {
                label: '열기',
                desc: '랜덤 아이템 1~3개 (10% 함정: 전원 HP -20%)',
                execute(scene) {
                    if (Math.random() < 0.1) {
                        scene.prevAllies.forEach(a => {
                            if (a.alive) a.hp = Math.floor(a.hp * 0.8);
                        });
                        return { text: '💥 함정이었다! 전원 HP -20%', color: '#ff4444' };
                    }
                    const count = 1 + Math.floor(Math.random() * 3);
                    const drops = LootGenerator.generate('demo5', scene.dangerSystem.level, count);
                    drops.forEach(d => StashManager.addToStash({ itemId: d.itemId, rarity: d.rarity, enhanceLevel: 0 }));
                    const names = drops.map(d => (ItemRegistry.get(d.itemId) || d).name);
                    return { text: `✨ ${names.join(', ')} 획득!`, color: '#44ff88' };
                }
            },
            {
                label: '무시',
                desc: '안전하게 통과',
                execute() { return { text: '안전하게 지나쳤다.', color: '#888888' }; }
            }
        ]
    },
    wounded_soldier: {
        name: '부상당한 병사',
        desc: '피투성이 병사가 쓰러져 있다. 살려줄 것인가?',
        icon: '🩹',
        choices: [
            {
                label: '치료 (50G)',
                desc: '로스터에 랜덤 캐릭터 추가 (Lv3~5)',
                execute(scene) {
                    if (StashManager.getGold() < 50) return { text: '골드가 부족하다.', color: '#ff4444' };
                    if (BP_ROSTER.roster.length >= BP_ROSTER.maxRoster) return { text: '로스터가 가득 찼다.', color: '#ff8844' };
                    StashManager.spendGold(50);
                    const classKey = BP_CLASS_POOL[Math.floor(Math.random() * BP_CLASS_POOL.length)];
                    const level = 3 + Math.floor(Math.random() * 3);
                    const char = BP_ROSTER._createCharacter(classKey, level);
                    BP_ROSTER.roster.push(char);
                    BP_ROSTER.save();
                    return { text: `${BP_ALLIES[classKey].name} "${char.name}" (Lv.${level}) 합류!`, color: '#44ff88' };
                }
            },
            {
                label: '약탈',
                desc: '골드 30~80 획득, 랜덤 아이템 1개',
                execute(scene) {
                    const gold = 30 + Math.floor(Math.random() * 51);
                    StashManager.addGold(gold);
                    const drops = LootGenerator.generate('demo5', scene.dangerSystem.level, 1);
                    if (drops.length > 0) StashManager.addToStash({ itemId: drops[0].itemId, rarity: drops[0].rarity, enhanceLevel: 0 });
                    const itemName = drops.length > 0 ? (ItemRegistry.get(drops[0].itemId) || drops[0]).name : '';
                    return { text: `💰 ${gold}G + ${itemName} 획득`, color: '#ffcc44' };
                }
            }
        ]
    },
    mysterious_altar: {
        name: '신비한 제단',
        desc: '붉은 빛이 나는 제단이 있다. 무언가 주문을 걸 수 있을 것 같다.',
        icon: '⛩️',
        choices: [
            {
                label: '기도 (HP 10% 소모)',
                desc: '랜덤 파티원 ATK +15% 영구',
                execute(scene) {
                    const alive = scene.prevAllies.filter(a => a.alive);
                    alive.forEach(a => a.hp = Math.floor(a.hp * 0.9));
                    if (alive.length > 0) {
                        const target = alive[Math.floor(Math.random() * alive.length)];
                        target.atk = Math.floor(target.atk * 1.15);
                        return { text: `🙏 ${target.name}의 ATK +15%!`, color: '#aa44ff' };
                    }
                    return { text: '기도했지만 아무 일도...', color: '#888888' };
                }
            },
            {
                label: '파괴',
                desc: '에픽 재료 1개 획득',
                execute() {
                    StashManager.addToStash({ itemId: 'pit_essence', rarity: 'epic', enhanceLevel: 0 });
                    return { text: '🔮 핏 에센스 획득!', color: '#aa44ff' };
                }
            }
        ]
    },
    merchant_ambush: {
        name: '상인 습격',
        desc: '부유해 보이는 상인이 호위를 요청한다. 호위하면 보상을 주겠다고 한다.',
        icon: '🤝',
        choices: [
            {
                label: '호위 수락',
                desc: '승리 시 상점 가격 30% 할인 (이번 런)',
                execute(scene) {
                    scene.runManager.shopDiscount = 0.3;
                    return { text: '🛒 이후 상점 30% 할인 적용!', color: '#44ff88' };
                }
            },
            {
                label: '거절',
                desc: '안전하게 통과',
                execute() { return { text: '상인은 홀로 떠났다.', color: '#888888' }; }
            }
        ]
    },
    training_ground: {
        name: '훈련장',
        desc: '버려진 훈련장을 발견했다. 이곳에서 전투 훈련을 할 수 있을 것 같다.',
        icon: '⚔️',
        choices: [
            {
                label: '훈련',
                desc: '파티 전원 EXP +50',
                execute(scene) {
                    scene.partyKeys.forEach(key => {
                        const char = BP_ROSTER.roster.find(c => c.classKey === key && c.status === 'ready');
                        if (char) BP_ROSTER.addExp(char.id, 50);
                    });
                    return { text: '💪 파티 전원 EXP +50!', color: '#ffcc44' };
                }
            },
            {
                label: '수색',
                desc: '랜덤 장비 1개 발견',
                execute(scene) {
                    const drops = LootGenerator.generate('demo5', scene.dangerSystem.level, 1);
                    if (drops.length > 0) {
                        StashManager.addToStash({ itemId: drops[0].itemId, rarity: drops[0].rarity, enhanceLevel: 0 });
                        const name = (ItemRegistry.get(drops[0].itemId) || drops[0]).name;
                        return { text: `🔍 ${name} 발견!`, color: '#4488ff' };
                    }
                    return { text: '아무것도 찾지 못했다.', color: '#888888' };
                }
            }
        ]
    },
    blood_fountain: {
        name: '피의 샘',
        desc: '핏빛 샘물이 솟아오른다. 마시면 힘을 얻을 수 있을지도 모르지만...',
        icon: '🩸',
        choices: [
            {
                label: '마시기',
                desc: '랜덤 1명 HP +30% 영구, 다른 1명 HP -20% 영구',
                execute(scene) {
                    const alive = scene.prevAllies.filter(a => a.alive);
                    if (alive.length < 2) return { text: '생존자가 너무 적다.', color: '#ff4444' };
                    const shuffled = Phaser.Utils.Array.Shuffle([...alive]);
                    const buffed = shuffled[0];
                    const debuffed = shuffled[1];
                    buffed.maxHp = Math.floor(buffed.maxHp * 1.3);
                    buffed.hp = Math.min(buffed.hp, buffed.maxHp);
                    debuffed.maxHp = Math.floor(debuffed.maxHp * 0.8);
                    debuffed.hp = Math.min(debuffed.hp, debuffed.maxHp);
                    return { text: `${buffed.name} HP+30% / ${debuffed.name} HP-20%`, color: '#ff4488' };
                }
            },
            {
                label: '채취',
                desc: '고대 재료 1개 (판매가 높음)',
                execute() {
                    StashManager.addToStash({ itemId: 'ancient_relic', rarity: 'ancient', enhanceLevel: 0 });
                    return { text: '🏺 고대의 유물 획득!', color: '#ff4488' };
                }
            }
        ]
    }
};

class BPEventScene extends Phaser.Scene {
    constructor() { super('BPEventScene'); }

    init(data) {
        this.partyKeys = data.party;
        this.dangerSystem = data.dangerSystem;
        this.dropSystem = data.dropSystem;
        this.runManager = data.runManager;
        this.appliedDrops = data.appliedDrops || [];
        this.prevAllies = data.allies || [];
        this.battleTime = data.time || 0;
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');
        const cx = 640;
        const round = this.runManager.getCurrentRound();
        const eventData = BP_EVENTS[round.eventId];

        if (!eventData) {
            this._goNext();
            return;
        }

        this.add.text(cx, 30, `${eventData.icon} ${eventData.name}`, {
            fontSize: '30px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(cx, 75, `라운드 ${round.round}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#666688'
        }).setOrigin(0.5);

        this.add.text(cx, 140, eventData.desc, {
            fontSize: '16px', fontFamily: 'monospace', color: '#aaaacc',
            wordWrap: { width: 800 }, align: 'center'
        }).setOrigin(0.5);

        // Choice buttons
        const buttonY = 280;
        const buttonWidth = 400;
        const gap = 20;
        const totalH = eventData.choices.length * 100 + (eventData.choices.length - 1) * gap;
        const startY = buttonY;

        eventData.choices.forEach((choice, i) => {
            const y = startY + i * (100 + gap);
            const btn = this.add.rectangle(cx, y, buttonWidth, 90, 0x111133)
                .setStrokeStyle(3, 0x4444aa).setInteractive();

            this.add.text(cx, y - 20, choice.label, {
                fontSize: '22px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5);
            this.add.text(cx, y + 15, choice.desc, {
                fontSize: '13px', fontFamily: 'monospace', color: '#8888aa',
                wordWrap: { width: buttonWidth - 40 }, align: 'center'
            }).setOrigin(0.5);

            btn.on('pointerover', () => btn.setFillStyle(0x222244));
            btn.on('pointerout', () => btn.setFillStyle(0x111133));
            btn.on('pointerdown', () => {
                this._executeChoice(choice);
            });
        });
    }

    _executeChoice(choice) {
        const result = choice.execute(this);

        // Clear UI and show result
        this.children.removeAll();
        this.cameras.main.setBackgroundColor('#0a0a1a');

        const cx = 640;
        this.add.text(cx, 280, result.text, {
            fontSize: '24px', fontFamily: 'monospace', color: result.color, fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4,
            wordWrap: { width: 900 }, align: 'center'
        }).setOrigin(0.5);

        this.add.text(cx, 340, `💰 ${StashManager.getGold()}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44'
        }).setOrigin(0.5);

        const btn = this.add.rectangle(cx, 440, 200, 50, 0x222244)
            .setStrokeStyle(3, 0x4444aa).setInteractive();
        this.add.text(cx, 440, '▶ 계속', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);

        btn.on('pointerdown', () => this._goNext());
    }

    _goNext() {
        const nextRound = this.runManager.advanceRound();
        if (!nextRound) {
            this.scene.start('BPResultScene', {
                victory: true, dangerLevel: this.dangerSystem.level,
                drops: this.dropSystem.collectedDrops, allies: this.prevAllies,
                party: this.partyKeys, time: this.battleTime
            });
            return;
        }
        const data = {
            party: this.partyKeys, dangerSystem: this.dangerSystem,
            dropSystem: this.dropSystem, runManager: this.runManager,
            appliedDrops: this.appliedDrops, allies: this.prevAllies, time: this.battleTime
        };
        const sceneMap = { battle: 'BPBattleScene', boss: 'BPBattleScene', shop: 'BPShopScene', forge: 'BPForgeScene', event: 'BPEventScene' };
        this.scene.start(sceneMap[nextRound.type] || 'BPBattleScene', data);
    }
}
