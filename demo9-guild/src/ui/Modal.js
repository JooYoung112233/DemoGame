class UIModal {
    /**
     * 확인/취소 모달 다이얼로그
     * @param {Phaser.Scene} scene
     * @param {object} opts
     * @returns {Phaser.GameObjects.Container}
     */
    static confirm(scene, opts = {}) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const {
            title = '확인',
            message = '',           // 문자열 or 문자열 배열
            confirmLabel = '확인',
            cancelLabel = '취소',
            onConfirm = null,
            onCancel = null,
            width = 400,
            danger = false,         // 위험 동작 (빨간 확인 버튼)
            depth = 500,
        } = opts;

        const container = scene.add.container(0, 0).setDepth(depth);

        // 오버레이
        const overlay = scene.add.rectangle(640, 360, 1280, 720,
            T.modalOverlay || 0x000000, T.modalOverlayAlpha || 0.7)
            .setInteractive();  // 클릭 블로킹
        container.add(overlay);

        const lines = Array.isArray(message) ? message : message.split('\n');
        const contentHeight = Math.max(60, lines.length * 18 + 20);
        const totalHeight = 40 + contentHeight + 50;  // title + content + buttons

        // 모달 배경
        const bg = scene.add.graphics();
        const mx = 640 - width / 2;
        const my = 360 - totalHeight / 2;
        bg.fillStyle(T.modalBg || 0x2a2218, 1);
        bg.fillRoundedRect(mx, my, width, totalHeight, 8);
        // 내부 글로우
        bg.fillStyle(0xffffff, 0.03);
        bg.fillRoundedRect(mx + 2, my + 2, width - 4, 30, { tl: 6, tr: 6, bl: 0, br: 0 });
        // 테두리
        bg.lineStyle(2, T.modalStroke || 0x8a7a4a, 0.8);
        bg.strokeRoundedRect(mx, my, width, totalHeight, 8);
        // 제목 구분선
        bg.lineStyle(1, T.divider || 0x5a4a2a, 0.5);
        bg.lineBetween(mx + 12, my + 36, mx + width - 12, my + 36);
        container.add(bg);

        // 제목
        const titleText = scene.add.text(640, my + 18, title, {
            fontSize: `${(T.fontSize && T.fontSize.header) || 16}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: danger ? (T.textDanger || '#cc4422') : (T.textGold || '#ffcc44'),
            fontStyle: 'bold'
        }).setOrigin(0.5);
        container.add(titleText);

        // 메시지
        lines.forEach((line, i) => {
            const msgText = scene.add.text(640, my + 48 + i * 18, line, {
                fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textPrimary || '#e8d8c0',
            }).setOrigin(0.5, 0);
            container.add(msgText);
        });

        // 버튼
        const btnY = my + totalHeight - 28;
        const btnW = 110;
        const btnH = 30;

        const destroy = () => {
            container.destroy();
        };

        const cancelBtn = UIButton.create(scene, 640 - 65, btnY, btnW, btnH, cancelLabel, {
            variant: 'ghost',
            fontSize: (T.fontSize && T.fontSize.body) || 12,
            depth: depth + 1,
            onClick: () => { destroy(); if (onCancel) onCancel(); }
        });
        container.add(cancelBtn);

        const confirmBtn = UIButton.create(scene, 640 + 65, btnY, btnW, btnH, confirmLabel, {
            variant: danger ? 'danger' : 'primary',
            fontSize: (T.fontSize && T.fontSize.body) || 12,
            depth: depth + 1,
            onClick: () => { destroy(); if (onConfirm) onConfirm(); }
        });
        container.add(confirmBtn);

        container._destroy = destroy;
        return container;
    }

    /**
     * 알림 모달 (확인 버튼만)
     */
    static alert(scene, opts = {}) {
        return UIModal.confirm(scene, {
            ...opts,
            cancelLabel: null,
            onCancel: null,
        });
    }
}
