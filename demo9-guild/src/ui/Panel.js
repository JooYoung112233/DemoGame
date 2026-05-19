class UIPanel {
    /**
     * 판타지 길드 스타일 패널
     * @param {Phaser.Scene} scene
     * @param {number} x - 좌상단 X
     * @param {number} y - 좌상단 Y
     * @param {number} width
     * @param {number} height
     * @param {object} opts
     */
    static create(scene, x, y, width, height, opts = {}) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const {
            fillColor = T.panelFill || 0x2a2218,
            strokeColor = T.panelStroke || 0x5a4a2a,
            alpha = 1,
            title = null,
            titleColor = T.textGold || '#ffcc44',
            titleSize = (T.fontSize && T.fontSize.header) || 14,
            cornerRadius = T.borderRadius || 6,
            depth = 0,
            ornament = false,       // 장식적 테두리 (이중선)
            innerGlow = false,      // 내부 글로우 효과
        } = opts;

        const container = scene.add.container(x, y);
        if (depth) container.setDepth(depth);

        const bg = scene.add.graphics();

        // 메인 배경
        bg.fillStyle(fillColor, alpha);
        bg.fillRoundedRect(0, 0, width, height, cornerRadius);

        // 내부 글로우 (상단 밝은 줄)
        if (innerGlow) {
            bg.fillStyle(0xffffff, 0.03);
            bg.fillRoundedRect(2, 2, width - 4, Math.min(40, height * 0.15), cornerRadius - 1);
        }

        // 테두리
        bg.lineStyle(1.5, strokeColor, 0.8);
        bg.strokeRoundedRect(0, 0, width, height, cornerRadius);

        // 장식적 이중 테두리
        if (ornament) {
            bg.lineStyle(0.5, strokeColor, 0.3);
            bg.strokeRoundedRect(3, 3, width - 6, height - 6, cornerRadius - 1);
        }

        container.add(bg);

        // 제목바
        if (title) {
            // 제목 배경 바
            const titleBg = scene.add.graphics();
            titleBg.fillStyle(T.panelTitleBg || 0x352a1a, 0.8);
            titleBg.fillRoundedRect(1, 1, width - 2, 28, { tl: cornerRadius - 1, tr: cornerRadius - 1, bl: 0, br: 0 });
            // 제목 하단 구분선
            titleBg.lineStyle(1, strokeColor, 0.5);
            titleBg.lineBetween(8, 29, width - 8, 29);
            container.add(titleBg);

            // 장식 다이아몬드
            const deco = scene.add.text(8, 14, '◆', {
                fontSize: '8px', fontFamily: T.fontFamily || 'monospace',
                color: T.textAccent || '#cc8833'
            }).setOrigin(0, 0.5);
            container.add(deco);

            const decoR = scene.add.text(width - 8, 14, '◆', {
                fontSize: '8px', fontFamily: T.fontFamily || 'monospace',
                color: T.textAccent || '#cc8833'
            }).setOrigin(1, 0.5);
            container.add(decoR);

            const titleText = scene.add.text(width / 2, 14, title, {
                fontSize: `${titleSize}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: titleColor,
                fontStyle: 'bold'
            }).setOrigin(0.5, 0.5);
            container.add(titleText);

            container._titleText = titleText;
        }

        container._bg = bg;
        container._width = width;
        container._height = height;
        return container;
    }

    /** 구분선 그리기 헬퍼 */
    static drawDivider(scene, x, y, width, label) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const container = scene.add.container(x, y);

        const line = scene.add.graphics();
        line.lineStyle(1, T.divider || 0x5a4a2a, T.dividerAlpha || 0.5);

        if (label) {
            const text = scene.add.text(width / 2, 0, label, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860',
            }).setOrigin(0.5, 0.5);
            const tw = text.width;
            line.lineBetween(0, 0, width / 2 - tw / 2 - 8, 0);
            line.lineBetween(width / 2 + tw / 2 + 8, 0, width, 0);
            container.add([line, text]);
        } else {
            line.lineBetween(0, 0, width, 0);
            container.add(line);
        }

        return container;
    }
}
