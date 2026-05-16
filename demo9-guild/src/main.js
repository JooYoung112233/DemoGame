// 게임 좌표계: 1280×720 (모든 씬 코드 그대로)
// Backing store: 1920×1080 (네이티브 렌더)
// 카메라 zoom 1.5로 1280×720 좌표를 1920×1080 viewport에 매핑
// 텍스트 resolution = DPR × ZOOM → 카메라 확대 후에도 선명
const BASE_W = 1280, BASE_H = 720;
const CANVAS_W = 1920, CANVAS_H = 1080;
const CAM_ZOOM = CANVAS_W / BASE_W;             // 1.5
const DEVICE_DPR = window.devicePixelRatio || 1;
const TEXT_DPR = DEVICE_DPR * CAM_ZOOM;          // 카메라 zoom 보상

// GameObjectFactory.text 재등록
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
    scene: [TitleScene, TownScene, RecruitScene, RosterScene, EquipmentScene, StorageScene, ForgeScene, AuctionScene, TrainingScene, TempleScene, IntelScene, EliteRecruitScene, AffinityScene, BondScene, SynergyScene, GuildHallScene, DeployScene, PlaceholderBattleScene, BattleScene, ManualBattleScene, CargoFloorSelectScene, CargoBattleScene, BlackoutBattleScene, BlackoutProtoSelectScene, BlackoutGridScene, BlackoutLaneScene, BlackoutRuneScene, EventScene, RunResultScene],
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

// 모든 씬에 카메라 zoom + centerOn(640, 360) 자동 적용
// → 1280×720 좌표를 1920×1080에 펼침 (좌상단 (0,0))
const applyCamera = (scene) => {
    if (scene && scene.cameras && scene.cameras.main) {
        scene.cameras.main.setZoom(CAM_ZOOM);
        scene.cameras.main.centerOn(BASE_W / 2, BASE_H / 2);
    }
};

game.events.once('ready', () => {
    game.scene.scenes.forEach(scene => {
        applyCamera(scene);                                    // 즉시 (이미 활성 씬)
        scene.events.on('create', () => applyCamera(scene));   // 이후 재시작 시
        scene.events.on('wake',   () => applyCamera(scene));   // 깨어날 때
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
