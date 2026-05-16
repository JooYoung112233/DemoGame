class AutomationScene extends Phaser.Scene {
    constructor() { super('AutomationScene'); }

    init(data) {
        this.gameState = data.gameState;
    }

    create() {
        const gs = this.gameState;
        GuildHallManager.ensureState(gs);
        AutomationManager.ensureState(gs);

        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);

        this.add.text(640, 25, '⚙ 자동화 설정', {
            fontSize: '20px', fontFamily: 'monospace', color: '#aaccff', fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            color: 0x335577, hoverColor: 0x446688, textColor: '#cceeff', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        this.add.text(1260, 25, `${gs.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0.5);

        const stage = gs.guildHall.automation || 0;
        this.add.text(640, 52, `자동화 단계: D${stage}/8`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#667788'
        }).setOrigin(0.5);

        this._drawContent();
    }

    _drawContent() {
        const gs = this.gameState;
        const settings = gs.automationSettings;
        const stage = gs.guildHall.automation || 0;

        let cy = 80;
        const leftX = 80;
        const rightX = 680;

        // === 좌측 패널: 장비 자동화 ===
        UIPanel.create(this, leftX - 10, cy - 5, 560, 300, { title: '장비 자동화' });
        cy += 30;

        // D1: 전체 최적화
        this._drawFeatureRow(leftX, cy, 'D1', '전체 최적화', stage >= 1, () => {
            const modeLabels = { class: '클래스 추천', atk: '최고 ATK', hpdef: 'HP+DEF', balanced: '균형' };
            const mode = settings.autoEquipMode || 'class';

            // 모드 선택 버튼들
            let bx = leftX + 180;
            for (const [key, label] of Object.entries(modeLabels)) {
                const active = mode === key;
                UIButton.create(this, bx, cy, 80, 22, label, {
                    color: active ? 0x4488aa : 0x333355,
                    hoverColor: 0x5599bb,
                    textColor: active ? '#ffffff' : '#888899',
                    fontSize: 9,
                    onClick: () => {
                        settings.autoEquipMode = key;
                        SaveManager.save(gs);
                        this.scene.restart({ gameState: gs });
                    }
                });
                bx += 90;
            }
        });
        cy += 30;

        // 전체 최적화 실행 버튼
        if (stage >= 1) {
            UIButton.create(this, leftX + 200, cy, 200, 30, '🔄 전체 최적화 실행', {
                color: 0x448844, hoverColor: 0x55aa55, textColor: '#ffffff', fontSize: 12,
                onClick: () => {
                    const mode = settings.autoEquipMode || 'class';
                    const changes = AutomationManager.autoEquipAll(gs, mode);
                    const msg = changes.length > 0
                        ? `${changes.length}명 장비 최적화 완료`
                        : '변경 사항 없음';
                    UIToast.show(this, msg);
                    if (changes.length > 0) {
                        GuildManager.addMessage(gs, `⚙ 자동 장착: ${changes.length}명 최적화`);
                    }
                }
            });
        }
        cy += 45;

        // D2: 자동 판매 룰
        this._drawSectionHeader(leftX, cy, 'D2', '자동 판매 룰', stage >= 2);
        cy += 25;

        if (stage >= 2) {
            const rules = settings.autoSellRules;
            this._drawCheckbox(leftX + 20, cy, 'common 자동 판매', rules.sellCommon, (v) => {
                rules.sellCommon = v; SaveManager.save(gs);
            });
            cy += 24;
            this._drawCheckbox(leftX + 20, cy, 'uncommon 자동 판매', rules.sellUncommon, (v) => {
                rules.sellUncommon = v; SaveManager.save(gs);
            });
            cy += 24;
            this._drawCheckbox(leftX + 20, cy, '잠금 안된 중복 자동 판매', rules.sellDuplicates, (v) => {
                rules.sellDuplicates = v; SaveManager.save(gs);
            });
            cy += 24;
            this._drawCheckbox(leftX + 20, cy, '보관함 80% 차면 common부터', rules.sellOnOverflow, (v) => {
                rules.sellOnOverflow = v; SaveManager.save(gs);
            });
            cy += 30;

            UIButton.create(this, leftX + 200, cy, 180, 26, '💰 자동 판매 실행', {
                color: 0x886644, hoverColor: 0xaa8855, textColor: '#ffffff', fontSize: 11,
                onClick: () => {
                    const sold = AutomationManager.runAutoSell(gs);
                    const totalGold = sold.reduce((s, r) => s + (r.gold || 0), 0);
                    UIToast.show(this, sold.length > 0
                        ? `${sold.length}개 판매 (+${totalGold}G)`
                        : '판매할 아이템 없음');
                }
            });
        }

        // === 우측 패널: 파견/관리 자동화 ===
        let ry = 80;
        UIPanel.create(this, rightX - 10, ry - 5, 560, 300, { title: '파견/관리 자동화' });
        ry += 30;

        // D3: 자동 회수
        this._drawToggleRow(rightX, ry, 'D3', '파견 완료 즉시 보상 회수', stage >= 3,
            settings.autoCollect, (v) => { settings.autoCollect = v; SaveManager.save(gs); });
        ry += 32;

        // D4: 출발 시 자동 장비 최적화
        this._drawToggleRow(rightX, ry, 'D4', '출발 시 자동 장비 최적화', stage >= 4,
            settings.autoEquipOnDispatch, (v) => { settings.autoEquipOnDispatch = v; SaveManager.save(gs); });
        ry += 32;

        // D5: 자동 재파견
        this._drawToggleRow(rightX, ry, 'D5', '완료 후 자동 재파견', stage >= 5,
            settings.autoRedispatch, (v) => { settings.autoRedispatch = v; SaveManager.save(gs); });
        ry += 32;

        // D6: 자동 위탁
        this._drawToggleRow(rightX, ry, 'D6', '잉여 장비 자동 위탁', stage >= 6,
            settings.autoConsign, (v) => { settings.autoConsign = v; SaveManager.save(gs); });
        ry += 32;

        // D7: 자동 치유
        this._drawToggleRow(rightX, ry, 'D7', '자동 치유/스테미너 회복', stage >= 7,
            settings.autoHeal, (v) => { settings.autoHeal = v; SaveManager.save(gs); });
        ry += 32;

        // D8: 완전 자동화
        this._drawToggleRow(rightX, ry, 'D8', '🏆 전체 자동 순환', stage >= 8,
            settings.fullAuto, (v) => { settings.fullAuto = v; SaveManager.save(gs); });

        // === 하단: 일괄 작업 ===
        const bottomY = 410;
        UIPanel.create(this, leftX - 10, bottomY - 5, 1140, 100, { title: '일괄 작업' });

        const btnY = bottomY + 50;
        UIButton.create(this, 180, btnY, 150, 30, '💰 잡템 일괄판매', {
            color: 0x886644, hoverColor: 0xaa8855, textColor: '#ffffff', fontSize: 11,
            onClick: () => {
                let count = 0, gold = 0;
                for (const item of [...gs.storage]) {
                    if (item.type !== 'equipment') continue;
                    if (item.locked) continue;
                    if (item.rarity === 'common') {
                        const r = StorageManager.sellItem(gs, item.id);
                        if (r.success) { count++; gold += r.gold; }
                    }
                }
                UIToast.show(this, count > 0 ? `${count}개 판매 (+${gold}G)` : '판매할 잡템 없음');
            }
        });

        UIButton.create(this, 380, btnY, 130, 30, '💚 전원 치유', {
            color: 0x448866, hoverColor: 0x55aa77, textColor: '#ffffff', fontSize: 11,
            onClick: () => {
                let count = 0, totalCost = 0;
                for (const merc of gs.roster) {
                    if (!merc.alive) continue;
                    const stats = merc.getStats();
                    if (merc.currentHp < stats.hp) {
                        const cost = Math.floor((stats.hp - merc.currentHp) * 0.5);
                        if (gs.gold >= cost) {
                            GuildManager.spendGold(gs, cost);
                            merc.currentHp = stats.hp;
                            merc.injured = false;
                            count++; totalCost += cost;
                        }
                    }
                }
                SaveManager.save(gs);
                UIToast.show(this, count > 0 ? `${count}명 치유 (-${totalCost}G)` : '치유할 용병 없음');
            }
        });

        UIButton.create(this, 560, btnY, 130, 30, '😴 전원 휴식', {
            color: 0x445588, hoverColor: 0x5566aa, textColor: '#ffffff', fontSize: 11,
            onClick: () => {
                UIToast.show(this, '스테미너 시스템 미구현');
            }
        });

        UIButton.create(this, 740, btnY, 160, 30, '🔄 자동화 전체 실행', {
            color: 0x664488, hoverColor: 0x8855aa, textColor: '#ffffff', fontSize: 11,
            disabled: stage < 8,
            onClick: () => {
                AutomationManager.runFullAuto(gs);
                GuildManager.addMessage(gs, '⚙ 완전 자동화 실행 완료');
                UIToast.show(this, '전체 자동화 실행 완료');
            }
        });
    }

    _drawFeatureRow(x, y, tag, label, unlocked, renderExtra) {
        const color = unlocked ? '#aaccee' : '#555566';
        const lockIcon = unlocked ? '' : '🔒 ';
        this.add.text(x, y, `[${tag}] ${lockIcon}${label}`, {
            fontSize: '12px', fontFamily: 'monospace', color, fontStyle: unlocked ? 'bold' : ''
        });
        if (unlocked && renderExtra) renderExtra();
    }

    _drawSectionHeader(x, y, tag, label, unlocked) {
        const color = unlocked ? '#aaccee' : '#555566';
        const lockIcon = unlocked ? '' : '🔒 ';
        this.add.text(x, y, `[${tag}] ${lockIcon}${label}`, {
            fontSize: '12px', fontFamily: 'monospace', color, fontStyle: unlocked ? 'bold' : ''
        });
        if (!unlocked) {
            this.add.text(x + 300, y, '길드회관에서 해금', {
                fontSize: '10px', fontFamily: 'monospace', color: '#664444'
            });
        }
    }

    _drawToggleRow(x, y, tag, label, unlocked, currentValue, onChange) {
        const color = unlocked ? '#aaccee' : '#555566';
        const lockIcon = unlocked ? '' : '🔒 ';
        this.add.text(x, y, `[${tag}] ${lockIcon}${label}`, {
            fontSize: '12px', fontFamily: 'monospace', color
        });

        if (unlocked) {
            const btnLabel = currentValue ? 'ON' : 'OFF';
            const btnColor = currentValue ? 0x448844 : 0x553333;
            UIButton.create(this, x + 420, y + 2, 50, 22, btnLabel, {
                color: btnColor, hoverColor: currentValue ? 0x55aa55 : 0x774444,
                textColor: '#ffffff', fontSize: 10,
                onClick: () => {
                    onChange(!currentValue);
                    this.scene.restart({ gameState: this.gameState });
                }
            });
        } else {
            this.add.text(x + 420, y, '—', {
                fontSize: '11px', fontFamily: 'monospace', color: '#444444'
            });
        }
    }

    _drawCheckbox(x, y, label, checked, onChange) {
        const icon = checked ? '☑' : '☐';
        const color = checked ? '#88cc88' : '#888899';
        const text = this.add.text(x, y, `${icon} ${label}`, {
            fontSize: '11px', fontFamily: 'monospace', color
        });

        const hitZone = this.add.zone(x + 150, y + 8, 300, 20).setInteractive({ useHandCursor: true });
        hitZone.on('pointerdown', () => {
            onChange(!checked);
            this.scene.restart({ gameState: this.gameState });
        });
    }
}
