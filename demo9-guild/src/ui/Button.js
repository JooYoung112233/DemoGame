class UIButton {
    /**
     * 판타지 길드 스타일 버튼
     * opts.variant: 'primary'(default) | 'danger' | 'info' | 'ghost'
     */
    static create(scene, x, y, width, height, label, opts = {}) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const {
            variant = 'primary',
            color = null,           // 직접 지정 시 variant 무시
            hoverColor = null,
            disabledColor = T.buttonDisabled || 0x333028,
            textColor = null,
            disabledTextColor = T.buttonDisabledText || '#665e48',
            fontSize = (T.fontSize && T.fontSize.body) || 12,
            disabled = false,
            onClick = null,
            depth = 0,
            icon = null,            // 라벨 앞 아이콘 (이모지)
        } = opts;

        // variant별 색상
        const variants = {
            primary: {
                bg: T.buttonPrimary || 0x8a6a2a,
                hover: T.buttonHover || 0xaa8a3a,
                pressed: T.buttonPressed || 0x6a5020,
                text: T.buttonText || '#f0e8d0',
                stroke: 0xaa9a5a,
            },
            danger: {
                bg: T.buttonDanger || 0x8a2a2a,
                hover: T.buttonDangerHover || 0xaa3a3a,
                pressed: 0x6a1a1a,
                text: '#f0d0d0',
                stroke: 0xaa5a5a,
            },
            info: {
                bg: T.buttonInfo || 0x2a5a8a,
                hover: T.buttonInfoHover || 0x3a6a9a,
                pressed: 0x1a4a6a,
                text: '#d0e0f0',
                stroke: 0x5a8aaa,
            },
            ghost: {
                bg: 0x00000000,
                hover: 0x3a3020,
                pressed: 0x2a2018,
                text: T.textSecondary || '#b8a888',
                stroke: T.panelStroke || 0x5a4a2a,
            }
        };

        const v = variants[variant] || variants.primary;
        const bgColor = color || v.bg;
        const bgHover = hoverColor || v.hover;
        const bgPressed = v.pressed;
        const txtColor = textColor || v.text;

        const container = scene.add.container(x, y);
        if (depth) container.setDepth(depth);

        const bg = scene.add.graphics();
        const radius = Math.min(6, height / 3);

        const drawBg = (c, strokeC, strokeAlpha = 0.6) => {
            bg.clear();
            if (variant === 'ghost' && c === 0x00000000) {
                // ghost: 투명 배경, 테두리만
                bg.lineStyle(1, strokeC, 0.4);
                bg.strokeRoundedRect(-width / 2, -height / 2, width, height, radius);
            } else {
                bg.fillStyle(c, 1);
                bg.fillRoundedRect(-width / 2, -height / 2, width, height, radius);
                // 상단 하이라이트
                bg.fillStyle(0xffffff, 0.06);
                bg.fillRect(-width / 2 + 2, -height / 2 + 1, width - 4, Math.max(1, height * 0.3));
                // 테두리
                bg.lineStyle(1, strokeC, strokeAlpha);
                bg.strokeRoundedRect(-width / 2, -height / 2, width, height, radius);
            }
        };

        drawBg(disabled ? disabledColor : bgColor, disabled ? 0x444438 : v.stroke);

        const displayLabel = icon ? `${icon} ${label}` : label;
        const text = scene.add.text(0, 0, displayLabel, {
            fontSize: `${fontSize}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: disabled ? disabledTextColor : txtColor,
            fontStyle: 'bold'
        }).setOrigin(0.5);

        container.add([bg, text]);

        const hitZone = scene.add.zone(0, 0, width, height).setInteractive({ useHandCursor: !disabled });
        container.add(hitZone);

        // 상태 저장
        container._bg = bg;
        container._text = text;
        container._hitZone = hitZone;
        container._drawBg = drawBg;
        container._opts = { bgColor, bgHover, bgPressed, txtColor, disabledColor, disabledTextColor, v, variant, onClick };
        container._disabled = disabled;

        // 이벤트
        if (!disabled && onClick) {
            hitZone.on('pointerover', () => drawBg(bgHover, v.stroke, 0.8));
            hitZone.on('pointerout', () => drawBg(bgColor, v.stroke));
            hitZone.on('pointerdown', () => {
                drawBg(bgPressed, v.stroke);
                scene.time.delayedCall(100, () => drawBg(bgColor, v.stroke));
                onClick();
            });
        }

        // ── setEnabled: 런타임 상태 전환 ──
        container.setEnabled = function(enabled, newOnClick) {
            const o = container._opts;
            container._disabled = !enabled;

            // 이벤트 제거
            hitZone.removeAllListeners();
            hitZone.setInteractive({ useHandCursor: enabled });

            if (enabled) {
                const click = newOnClick || o.onClick;
                drawBg(o.bgColor, o.v.stroke);
                text.setColor(o.txtColor);
                if (click) {
                    hitZone.on('pointerover', () => drawBg(o.bgHover, o.v.stroke, 0.8));
                    hitZone.on('pointerout', () => drawBg(o.bgColor, o.v.stroke));
                    hitZone.on('pointerdown', () => {
                        drawBg(o.bgPressed, o.v.stroke);
                        scene.time.delayedCall(100, () => drawBg(o.bgColor, o.v.stroke));
                        click();
                    });
                }
            } else {
                drawBg(o.disabledColor, 0x444438);
                text.setColor(o.disabledTextColor);
            }
        };

        // ── setLabel: 텍스트 변경 ──
        container.setLabel = function(newLabel) {
            text.setText(icon ? `${icon} ${newLabel}` : newLabel);
        };

        return container;
    }
}
