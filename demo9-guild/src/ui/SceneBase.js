/**
 * UISceneBase — 모든 메뉴 씬의 베이스 클래스
 *
 * 제공 기능:
 * - 배경 자동 생성
 * - 공용 헤더 (제목 + 뒤로가기 + 골드)
 * - 장식적 구분선
 * - 골드 텍스트 자동 갱신
 * - _contentObjects 관리 (탭 전환 시 정리)
 */
class UISceneBase extends Phaser.Scene {
    constructor(key) {
        super(key);
        this._contentObjects = [];
    }

    init(data) {
        this.gameState = data.gameState || data;
    }

    /** 배경 + 헤더 그리기. create() 최상단에 호출. */
    _drawBase(title, opts = {}) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const {
            backTarget = 'TownScene',
            backLabel = '← 마을',
            showGold = true,
            showGuildLevel = false,
        } = opts;

        // ── 배경 ──
        this._drawBackground();

        // ── 헤더 바 ──
        const headerH = T.headerHeight || 55;
        const headerBg = this.add.graphics();
        headerBg.fillStyle(T.headerBg || 0x1e1810, 1);
        headerBg.fillRect(0, 0, 1280, headerH);
        // 하단 장식선
        headerBg.lineStyle(1, T.ornament || 0x8a7a4a, T.ornamentAlpha || 0.4);
        headerBg.lineBetween(0, headerH, 1280, headerH);
        // 이중선
        headerBg.lineStyle(0.5, T.divider || 0x5a4a2a, 0.3);
        headerBg.lineBetween(0, headerH + 2, 1280, headerH + 2);

        // ── 제목 ──
        if (title) {
            // 제목 장식
            this.add.text(640, 27, title, {
                fontSize: `${(T.fontSize && T.fontSize.title) || 20}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textGold || '#ffcc44',
                fontStyle: 'bold',
                stroke: '#000000',
                strokeThickness: 2
            }).setOrigin(0.5);

            // 제목 양쪽 장식
            const tw = title.length * 12;  // 대략적 폭
            this.add.text(640 - tw / 2 - 20, 27, '◈', {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                color: T.textAccent || '#cc8833'
            }).setOrigin(0.5);
            this.add.text(640 + tw / 2 + 20, 27, '◈', {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                color: T.textAccent || '#cc8833'
            }).setOrigin(0.5);
        }

        // ── 뒤로가기 ──
        if (backTarget) {
            UIButton.create(this, 65, 27, 100, 28, backLabel, {
                variant: 'ghost',
                fontSize: (T.fontSize && T.fontSize.body) || 12,
                onClick: () => this.scene.start(backTarget, { gameState: this.gameState })
            });
        }

        // ── 골드 ──
        if (showGold) {
            const gs = this.gameState;
            this._goldText = this.add.text(1260, 20, `💰 ${(gs.gold || 0).toLocaleString()}G`, {
                fontSize: `${(T.fontSize && T.fontSize.header) || 16}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textGold || '#ffcc44',
                fontStyle: 'bold'
            }).setOrigin(1, 0);
        }

        // ── 길드 레벨 ──
        if (showGuildLevel) {
            const gs = this.gameState;
            this.add.text(20, 38, `길드 Lv.${gs.guildLevel || 1}`, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888'
            });
        }
    }

    /** 판타지 길드 배경 */
    _drawBackground() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        // 메인 배경
        this.add.rectangle(640, 360, 1280, 720, T.bg || 0x1a1510);

        // 미묘한 비네팅 효과 (모서리 어둡게)
        const vignette = this.add.graphics();
        vignette.fillStyle(0x000000, 0.15);
        vignette.fillRect(0, 0, 1280, 4);
        vignette.fillRect(0, 716, 1280, 4);
        vignette.fillRect(0, 0, 4, 720);
        vignette.fillRect(1276, 0, 4, 720);

        // 코너 장식 (미묘한 금색 모서리)
        const corner = this.add.graphics();
        corner.lineStyle(1, T.ornament || 0x8a7a4a, 0.15);
        // 좌상
        corner.lineBetween(0, 20, 20, 0);
        corner.lineBetween(0, 30, 30, 0);
        // 우상
        corner.lineBetween(1260, 0, 1280, 20);
        corner.lineBetween(1250, 0, 1280, 30);
        // 좌하
        corner.lineBetween(0, 700, 20, 720);
        corner.lineBetween(0, 690, 30, 720);
        // 우하
        corner.lineBetween(1260, 720, 1280, 700);
        corner.lineBetween(1250, 720, 1280, 690);
    }

    /** 골드 텍스트 갱신 */
    _updateGold() {
        if (this._goldText) {
            this._goldText.setText(`💰 ${(this.gameState.gold || 0).toLocaleString()}G`);
        }
    }

    /** 콘텐츠 정리 (탭 전환용) */
    _clearContent() {
        this._contentObjects.forEach(obj => {
            if (obj && obj.destroy) obj.destroy();
        });
        this._contentObjects = [];
    }

    /** 콘텐츠 객체 등록 */
    _addObj(obj) {
        this._contentObjects.push(obj);
        return obj;
    }

    /** 장식적 섹션 구분선 */
    _drawSectionDivider(y, label) {
        return UIPanel.drawDivider(this, 20, y, 1240, label);
    }

    /** 토스트 메시지 */
    _toast(msg) {
        if (typeof UIToast !== 'undefined') {
            UIToast.show(this, msg);
        }
    }
}
