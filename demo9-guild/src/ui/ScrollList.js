class UIScrollList {
    /**
     * 스크롤 가능 리스트 컨테이너
     * @param {Phaser.Scene} scene
     * @param {number} x - 좌상단 X
     * @param {number} y - 좌상단 Y
     * @param {number} width - 가시 영역 폭
     * @param {number} height - 가시 영역 높이
     * @param {object} opts
     * @returns {{ container, content, scrollTo, refresh, destroy }}
     */
    static create(scene, x, y, width, height, opts = {}) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const {
            scrollbarWidth = 8,
            showScrollbar = true,
            depth = 0,
        } = opts;

        const container = scene.add.container(x, y);
        if (depth) container.setDepth(depth);

        // 콘텐츠 컨테이너 (이 안에 아이템 추가)
        const content = scene.add.container(0, 0);
        container.add(content);

        // 마스크
        const maskShape = scene.make.graphics({ x: 0, y: 0, add: false });
        maskShape.fillStyle(0xffffff);
        // 카메라 줌 보정 (1280→1920 기준)
        const cam = scene.cameras.main;
        const zoom = cam ? cam.zoom : 1;
        const maskX = (x - (cam ? cam.scrollX : 0)) * zoom + (cam ? cam.x : 0);
        const maskY = (y - (cam ? cam.scrollY : 0)) * zoom + (cam ? cam.y : 0);
        maskShape.fillRect(maskX, maskY, width * zoom, height * zoom);
        const mask = maskShape.createGeometryMask();
        content.setMask(mask);

        let scrollY = 0;
        let contentHeight = 0;
        let maxScroll = 0;

        // 스크롤바
        let scrollbar = null;
        let thumb = null;
        let trackHeight = height - 4;

        if (showScrollbar) {
            scrollbar = scene.add.graphics();
            container.add(scrollbar);
        }

        const drawScrollbar = () => {
            if (!scrollbar || !showScrollbar) return;
            scrollbar.clear();
            if (contentHeight <= height) return;

            const sbx = width - scrollbarWidth - 2;
            // 트랙
            scrollbar.fillStyle(T.scrollTrack || 0x2a2218, 0.5);
            scrollbar.fillRoundedRect(sbx, 2, scrollbarWidth, trackHeight, 3);

            // 썸
            const thumbH = Math.max(20, (height / contentHeight) * trackHeight);
            const thumbY = 2 + (scrollY / maxScroll) * (trackHeight - thumbH);
            scrollbar.fillStyle(T.scrollThumb || 0x6a5a3a, 0.8);
            scrollbar.fillRoundedRect(sbx, thumbY, scrollbarWidth, thumbH, 3);
        };

        const clampScroll = () => {
            scrollY = Math.max(0, Math.min(maxScroll, scrollY));
        };

        const applyScroll = () => {
            clampScroll();
            content.y = -scrollY;
            drawScrollbar();
        };

        // 마우스 휠
        const hitZone = scene.add.zone(width / 2, height / 2, width, height)
            .setInteractive();
        container.add(hitZone);

        hitZone.on('wheel', (pointer, dx, dy) => {
            scrollY += dy * 0.5;
            applyScroll();
        });

        // 드래그 스크롤
        let dragging = false;
        let dragStartY = 0;
        let dragStartScroll = 0;

        hitZone.on('pointerdown', (pointer) => {
            dragging = true;
            dragStartY = pointer.y;
            dragStartScroll = scrollY;
        });

        scene.input.on('pointermove', (pointer) => {
            if (!dragging) return;
            const dy = dragStartY - pointer.y;
            scrollY = dragStartScroll + dy;
            applyScroll();
        });

        scene.input.on('pointerup', () => {
            dragging = false;
        });

        /** 콘텐츠 높이 갱신 (아이템 추가/제거 후 호출) */
        const refresh = (newContentHeight) => {
            contentHeight = newContentHeight;
            maxScroll = Math.max(0, contentHeight - height);
            clampScroll();
            applyScroll();
        };

        /** 특정 위치로 스크롤 */
        const scrollTo = (targetY) => {
            scrollY = targetY;
            applyScroll();
        };

        /** 정리 */
        const destroy = () => {
            scene.input.off('pointermove');
            scene.input.off('pointerup');
            content.clearMask();
            maskShape.destroy();
            container.destroy();
        };

        return { container, content, scrollTo, refresh, destroy, hitZone };
    }
}
