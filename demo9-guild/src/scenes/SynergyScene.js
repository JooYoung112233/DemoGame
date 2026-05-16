/**
 * SynergyScene — 모든 시너지 도감.
 * 클래스 조합 별로 전체 시너지를 미리 볼 수 있다 (현재 활성/비활성 구분).
 */
class SynergyScene extends Phaser.Scene {
    constructor() { super('SynergyScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.returnTo = data.returnTo || 'TownScene';
        this.returnData = data.returnData || { gameState: data.gameState };
    }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a14);
        this._drawUI();
    }

    _drawUI() {
        const gs = this.gameState;

        this.add.text(640, 22, '✨ 시너지 도감', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ccaaff', fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 22, 100, 30, '← 뒤로', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start(this.returnTo, this.returnData)
        });

        // 보유 클래스 (활성 여부 판단용)
        const ownedClasses = new Set();
        gs.roster.forEach(m => ownedClasses.add(m.classKey));
        this.add.text(640, 50, `보유 클래스: ${Array.from(ownedClasses).map(c => CLASS_DATA[c]?.icon || '?').join(' ')}  (${ownedClasses.size}/6)`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5);

        if (typeof SYNERGY_DATA !== 'object') {
            this.add.text(640, 360, '시너지 데이터 없음', {
                fontSize: '14px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5);
            return;
        }

        // 카테고리별로 분류
        const cat2 = [], cat3 = [], cat5 = [];
        for (const [key, syn] of Object.entries(SYNERGY_DATA)) {
            const item = { key, ...syn };
            if (syn.type === 2) cat2.push(item);
            else if (syn.type === 3) cat3.push(item);
            else if (syn.type === 5) cat5.push(item);
        }

        // 좌(2인), 중(3인), 우(5인) 컬럼 레이아웃
        this._drawCategory('✦ 2인 시너지', cat2, 20, 80, 410, ownedClasses, '#88ccff');
        this._drawCategory('★ 3인 시너지', cat3, 440, 80, 410, ownedClasses, '#ff88cc');
        this._drawCategory('⭐ 5인 시너지', cat5, 860, 80, 400, ownedClasses, '#ffcc44');
    }

    _drawCategory(title, items, x, y, w, ownedClasses, titleColor) {
        const h = 620;
        UIPanel.create(this, x, y, w, h, {
            fillColor: 0x101020, strokeColor: 0x333355,
            title: `${title} (${items.length})`
        });

        let cy = y + 40;
        items.forEach(syn => {
            if (cy > y + h - 60) return;

            // 활성 여부 — 모든 클래스가 보유되어 있는지
            const allOwned = (syn.classes || []).every(c => ownedClasses.has(c));
            const isActive = syn.type === 5
                ? this._is5ManActive(syn, ownedClasses)
                : allOwned;

            const cardH = 60;
            const cardBg = this.add.graphics();
            if (isActive) {
                cardBg.fillStyle(0x1a2a3a, 1);
                cardBg.fillRoundedRect(x + 10, cy, w - 20, cardH, 4);
                cardBg.lineStyle(2, Phaser.Display.Color.HexStringToColor(titleColor).color, 0.8);
                cardBg.strokeRoundedRect(x + 10, cy, w - 20, cardH, 4);
            } else {
                cardBg.fillStyle(0x141420, 1);
                cardBg.fillRoundedRect(x + 10, cy, w - 20, cardH, 4);
                cardBg.lineStyle(1, 0x333344, 0.5);
                cardBg.strokeRoundedRect(x + 10, cy, w - 20, cardH, 4);
            }

            // 이름
            this.add.text(x + 18, cy + 6, syn.name, {
                fontSize: '12px', fontFamily: 'monospace',
                color: isActive ? titleColor : '#666677', fontStyle: 'bold'
            });

            // 활성 표시
            if (isActive) {
                this.add.text(x + w - 18, cy + 6, '✓ 활성', {
                    fontSize: '10px', fontFamily: 'monospace', color: '#88ffaa', fontStyle: 'bold'
                }).setOrigin(1, 0);
            } else {
                // 부족한 클래스
                const owned = (syn.classes || []).filter(c => ownedClasses.has(c));
                const missing = (syn.classes || []).filter(c => !ownedClasses.has(c));
                if (syn.type === 5) {
                    this.add.text(x + w - 18, cy + 6, `(5인 조건)`, {
                        fontSize: '9px', fontFamily: 'monospace', color: '#666677'
                    }).setOrigin(1, 0);
                } else if (missing.length > 0) {
                    this.add.text(x + w - 18, cy + 6, `-${missing.map(c => CLASS_DATA[c]?.icon || '?').join('')}`, {
                        fontSize: '11px', fontFamily: 'monospace', color: '#aa6666'
                    }).setOrigin(1, 0);
                }
            }

            // 클래스 아이콘 표시
            if (syn.classes) {
                let icx = x + 18;
                syn.classes.forEach(c => {
                    const owned = ownedClasses.has(c);
                    const cls = CLASS_DATA[c];
                    const iconColor = owned ? '#ffffff' : '#555566';
                    this.add.text(icx, cy + 24, cls?.icon || '?', {
                        fontSize: '13px', fontFamily: 'monospace',
                        color: iconColor
                    });
                    icx += 18;
                });
            } else if (syn.type === 5) {
                this.add.text(x + 18, cy + 24, '5인 파티 조건부', {
                    fontSize: '10px', fontFamily: 'monospace', color: '#888899'
                });
            }

            // 효과 설명
            this.add.text(x + 18, cy + 42, syn.desc || '', {
                fontSize: '10px', fontFamily: 'monospace',
                color: isActive ? '#aaccdd' : '#666677',
                wordWrap: { width: w - 36 }
            });

            cy += cardH + 6;
        });
    }

    _is5ManActive(syn, ownedClasses) {
        // 5인 시너지는 check 함수 통과 + 클래스가 모두 보유되어 있을 때 활성으로 판단
        // ownedClasses만으로 알 수 없으니 보수적으로 false (실제 파티 편성에서 활성)
        if (!syn.classes) return false;
        return syn.classes.every(c => ownedClasses.has(c));
    }
}
