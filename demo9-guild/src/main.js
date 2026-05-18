// 캔버스: 1920×1080 네이티브 렌더링
// 좌표계: 1280×720 (카메라 zoom 1.5)
// Phaser.Scale.FIT → 브라우저 뷰포트에 자동 맞춤 (CSS만, 캔버스 해상도 유지)
const BASE_W = 1280, BASE_H = 720;
const RENDER_W = 1920, RENDER_H = 1080;
const CAM_ZOOM = RENDER_W / BASE_W;   // 1.5
const DPR = window.devicePixelRatio || 1;

// 텍스트 선명도 — 카메라 zoom + DPR 보상
Phaser.GameObjects.GameObjectFactory.register('text', function (x, y, text, style) {
    style = style || {};
    if (style.resolution === undefined) style.resolution = DPR * CAM_ZOOM;
    return this.displayList.add(new Phaser.GameObjects.Text(this.scene, x, y, text, style));
});

const config = {
    type: Phaser.CANVAS,
    width: RENDER_W,
    height: RENDER_H,
    parent: 'game-container',
    backgroundColor: '#1a1510',
    scene: [TitleScene, TownScene, RecruitScene, RosterScene, EquipmentScene, StorageScene, ForgeScene, AuctionScene, TrainingScene, TempleScene, IntelScene, EliteRecruitScene, AffinityScene, DeployScene, PlaceholderBattleScene, BattleScene, ManualBattleScene, CargoFloorSelectScene, CargoBattleScene, BlackoutBattleScene, EventScene, RunResultScene, GuildHallScene, AutomationScene, CodexScene],
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH
    },
    render: {
        pixelArt: false,
        antialias: true,
        roundPixels: false
    },
    audio: { noAudio: true },
    disableVisibilityChange: true
};

const game = new Phaser.Game(config);
window.game = game;

// 모든 씬에 카메라 zoom 1.5 + 중앙 정렬 적용
const applyCamera = (scene) => {
    if (scene && scene.cameras && scene.cameras.main) {
        scene.cameras.main.setZoom(CAM_ZOOM);
        scene.cameras.main.centerOn(BASE_W / 2, BASE_H / 2);
    }
};
game.events.once('ready', () => {
    game.scene.scenes.forEach(scene => {
        applyCamera(scene);
        scene.events.on('create', () => applyCamera(scene));
        scene.events.on('wake', () => applyCamera(scene));
    });
});
