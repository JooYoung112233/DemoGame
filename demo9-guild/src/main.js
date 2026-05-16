// 게임 좌표계: 1280×720 (모든 씬 코드 그대로)
// Backing store / CSS: 1920×1080 (네이티브 렌더링 → 선명)
// 카메라 zoom 1.5로 1280×720 좌표를 1920×1080 viewport에 매핑
const BASE_W = 1280, BASE_H = 720;
const CANVAS_W = 1920, CANVAS_H = 1080;
const CAM_ZOOM = CANVAS_W / BASE_W;             // 1.5
const DEVICE_DPR = window.devicePixelRatio || 1;
const TEXT_DPR = DEVICE_DPR;                     // 캔버스가 이미 큼 → DPR만

// GameObjectFactory.text 재등록 — 텍스트는 DPR resolution
Phaser.GameObjects.GameObjectFactory.register('text', function (x, y, text, style) {
    style = style || {};
    if (style.resolution === undefined) style.resolution = TEXT_DPR;
    return this.displayList.add(new Phaser.GameObjects.Text(this.scene, x, y, text, style));
});

const config = {
    type: Phaser.AUTO,
    width: CANVAS_W,                             // backing store 1920×1080
    height: CANVAS_H,
    parent: 'game-container',
    backgroundColor: '#0a0a1a',
    scene: [TitleScene, TownScene, RecruitScene, RosterScene, StorageScene, ForgeScene, AuctionScene, TrainingScene, TempleScene, IntelScene, EliteRecruitScene, AffinityScene, DeployScene, PlaceholderBattleScene, BattleScene, CargoBattleScene, BlackoutBattleScene, EventScene, RunResultScene],
    scale: {
        mode: Phaser.Scale.NONE,
        autoCenter: Phaser.Scale.CENTER_BOTH
    },
    render: {
        pixelArt: false,
        antialias: true,
        antialiasGL: true,
        roundPixels: false
    },
    audio: { noAudio: true },
    disableVisibilityChange: true
};

const game = new Phaser.Game(config);
window.game = game;

// 모든 씬에 카메라 zoom 1.5 자동 적용 — 1280×720 좌표를 1920×1080에 펼침
game.events.once('ready', () => {
    game.scene.scenes.forEach(scene => {
        scene.events.on('create', () => {
            if (scene.cameras && scene.cameras.main) {
                scene.cameras.main.setZoom(CAM_ZOOM).setScroll(
                    -(CANVAS_W - BASE_W * CAM_ZOOM) / 2 / CAM_ZOOM,
                    -(CANVAS_H - BASE_H * CAM_ZOOM) / 2 / CAM_ZOOM
                );
            }
        });
    });
});

// 캔버스 CSS 크기
game.events.once('ready', () => {
    const canvas = game.canvas;
    if (canvas) {
        canvas.style.width = CANVAS_W + 'px';
        canvas.style.height = CANVAS_H + 'px';
    }
});
