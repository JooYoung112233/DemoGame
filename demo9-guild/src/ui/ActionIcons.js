/**
 * ActionIcons — 액션(공격/스킬) 아이콘 로더 + 렌더 헬퍼.
 *
 * 사용법:
 *   ActionIcons.preload(this);                     // Scene.preload() 안에서 1회
 *   ActionIcons.render(this, x, y, action, 28);    // 위치/크기로 아이콘 렌더 (image or text)
 *   ActionIcons.renderInline(this, x, y, action);  // 텍스트 옆 인라인 (작은 사이즈)
 *
 * 동작:
 * - `assets/actions/<actionId>.png` 자동 로드 시도
 * - PNG 존재 시 → Phaser Image 반환
 * - 없으면 → action.icon (이모지) 텍스트로 폴백
 * - loaderror는 콘솔만 찍고 무시 (일부 PNG만 있어도 OK)
 */
const ActionIcons = {
    /** 모든 액션 ID에 대해 PNG 로드 시도 */
    preload(scene) {
        if (typeof ACTION_DATA !== 'object') return;
        Object.keys(ACTION_DATA).forEach(id => {
            const key = `act_${id}`;
            if (!scene.textures.exists(key)) {
                scene.load.image(key, `assets/actions/${id}.png`);
            }
        });
        // 한 번만 등록 — 중복 방지
        if (!scene._actionIconErrHooked) {
            scene._actionIconErrHooked = true;
            scene.load.on('loaderror', (file) => {
                if (file.key && file.key.startsWith('act_')) {
                    // 디버그용 콘솔만, 폴백 자동 처리됨
                    // console.debug('액션 아이콘 폴백:', file.key);
                }
            });
        }
    },

    /**
     * 아이콘 그리기.
     * @param scene Phaser 씬
     * @param x, y 위치 (중심 기준)
     * @param action ACTION_DATA의 객체
     * @param size 픽셀 크기 (기본 28)
     * @returns Phaser GameObject (image or text)
     */
    render(scene, x, y, action, size = 28) {
        if (!action) return null;
        const aid = action.id || action._id || null;
        if (!aid) return scene.add.text(x, y, action.icon || '?', {
            fontSize: `${Math.floor(size * 0.85)}px`, fontFamily: 'monospace'
        }).setOrigin(0.5);
        const key = `act_${aid}`;
        if (scene.textures.exists(key)) {
            const img = scene.add.image(x, y, key);
            img.setDisplaySize(size, size);
            img.setOrigin(0.5);
            return img;
        }
        // 폴백 — 이모지 텍스트
        const fontSize = Math.floor(size * 0.85);
        return scene.add.text(x, y, action.icon || '?', {
            fontSize: `${fontSize}px`,
            fontFamily: 'monospace'
        }).setOrigin(0.5);
    },

    /**
     * 텍스트 라인 시작 위치에 인라인 아이콘 — 작은 크기, 텍스트 옆에 붙임.
     * 반환 객체와 함께 폭(width)을 알 수 있어서 텍스트 위치 계산용.
     */
    renderInline(scene, x, y, action, size = 16) {
        if (!action) return { obj: null, width: 0 };
        const aid = action.id || action._id || null;
        if (!aid) {
            const fb = scene.add.text(x, y, action.icon || '', {
                fontSize: `${Math.floor(size * 0.95)}px`, fontFamily: 'monospace'
            }).setOrigin(0, 0);
            return { obj: fb, width: fb.width + 4 };
        }
        const key = `act_${aid}`;
        if (scene.textures.exists(key)) {
            const img = scene.add.image(x + size/2, y + size/2, key);
            img.setDisplaySize(size, size);
            img.setOrigin(0.5);
            return { obj: img, width: size + 4 };
        }
        const fontSize = Math.floor(size * 0.95);
        const txt = scene.add.text(x, y, action.icon || '', {
            fontSize: `${fontSize}px`,
            fontFamily: 'monospace'
        }).setOrigin(0, 0);
        return { obj: txt, width: txt.width + 4 };
    },

    /** PNG 보유 여부 — UI에서 분기용 */
    hasPng(scene, actionId) {
        return scene.textures.exists(`act_${actionId}`);
    }
};
