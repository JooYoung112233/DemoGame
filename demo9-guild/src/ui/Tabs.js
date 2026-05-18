class UITabs {
    /**
     * 탭 UI 컴포넌트
     * @param {Phaser.Scene} scene
     * @param {number} x - 시작 X
     * @param {number} y - 시작 Y
     * @param {Array<{key:string, label:string, icon?:string}>} tabs
     * @param {function(key:string)} onSelect - 탭 선택 콜백
     * @param {object} opts
     * @returns {Phaser.GameObjects.Container}
     */
    static create(scene, x, y, tabs, onSelect, opts = {}) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const {
            activeKey = tabs[0]?.key,
            tabWidth = 100,
            tabHeight = 28,
            gap = 4,
            depth = 0,
            fontSize = (T.fontSize && T.fontSize.body) || 12,
        } = opts;

        const container = scene.add.container(x, y);
        if (depth) container.setDepth(depth);

        const tabButtons = [];
        let currentKey = activeKey;

        const activeBg = T.buttonPrimary || 0x8a6a2a;
        const activeStroke = 0xaa9a5a;
        const inactiveBg = T.panelFill || 0x2a2218;
        const inactiveStroke = T.panelStroke || 0x5a4a2a;
        const activeText = T.buttonText || '#f0e8d0';
        const inactiveText = T.textSecondary || '#b8a888';

        tabs.forEach((tab, i) => {
            const tx = i * (tabWidth + gap);
            const bg = scene.add.graphics();
            const label = tab.icon ? `${tab.icon} ${tab.label}` : tab.label;
            const text = scene.add.text(tx + tabWidth / 2, tabHeight / 2, label, {
                fontSize: `${fontSize}px`,
                fontFamily: T.fontFamily || 'monospace',
                fontStyle: 'bold',
                color: tab.key === currentKey ? activeText : inactiveText,
            }).setOrigin(0.5);

            const drawTab = (isActive, isHover) => {
                bg.clear();
                const fill = isActive ? activeBg : (isHover ? (T.cardHover || 0x3a3020) : inactiveBg);
                const stroke = isActive ? activeStroke : inactiveStroke;
                bg.fillStyle(fill, 1);
                bg.fillRoundedRect(tx, 0, tabWidth, tabHeight, { tl: 5, tr: 5, bl: 0, br: 0 });
                if (isActive) {
                    bg.fillStyle(0xffffff, 0.05);
                    bg.fillRect(tx + 2, 2, tabWidth - 4, tabHeight * 0.4);
                }
                bg.lineStyle(1, stroke, isActive ? 0.8 : 0.4);
                bg.strokeRoundedRect(tx, 0, tabWidth, tabHeight, { tl: 5, tr: 5, bl: 0, br: 0 });
                // 활성 탭 하단선 숨김
                if (isActive) {
                    bg.fillStyle(T.panelFill || 0x2a2218, 1);
                    bg.fillRect(tx + 1, tabHeight - 1, tabWidth - 2, 2);
                }
            };

            drawTab(tab.key === currentKey, false);

            const zone = scene.add.zone(tx + tabWidth / 2, tabHeight / 2, tabWidth, tabHeight)
                .setInteractive({ useHandCursor: true });

            zone.on('pointerover', () => {
                if (tab.key !== currentKey) drawTab(false, true);
            });
            zone.on('pointerout', () => {
                if (tab.key !== currentKey) drawTab(false, false);
            });
            zone.on('pointerdown', () => {
                if (tab.key === currentKey) return;
                currentKey = tab.key;
                // 모든 탭 리드로우
                tabButtons.forEach((tb, j) => {
                    const isAct = tabs[j].key === currentKey;
                    tb.draw(isAct, false);
                    tb.text.setColor(isAct ? activeText : inactiveText);
                });
                if (onSelect) onSelect(currentKey);
            });

            container.add([bg, text, zone]);
            tabButtons.push({ bg, text, zone, draw: drawTab, key: tab.key });
        });

        // 하단 라인 (전체 폭)
        const totalWidth = tabs.length * (tabWidth + gap) - gap;
        const bottomLine = scene.add.graphics();
        bottomLine.lineStyle(1, inactiveStroke, 0.5);
        bottomLine.lineBetween(0, tabHeight, totalWidth, tabHeight);
        container.add(bottomLine);

        container._tabs = tabButtons;
        container._currentKey = () => currentKey;

        // setActiveTab: 외부에서 탭 전환
        container.setActiveTab = function(key) {
            currentKey = key;
            tabButtons.forEach((tb, j) => {
                const isAct = tabs[j].key === key;
                tb.draw(isAct, false);
                tb.text.setColor(isAct ? activeText : inactiveText);
            });
        };

        return container;
    }
}
