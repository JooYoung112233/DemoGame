// 캔버스: 1920×1080 (대부분 모니터에서 CSS 1:1 또는 다운스케일 = 선명)
// 좌표계: 1280×720 → 카메라 zoom 1.5
// Scale.FIT → CSS로 뷰포트 꽉 채움
const BASE_W = 1280, BASE_H = 720;
const CANVAS_W = 1920, CANVAS_H = 1080;
const CAM_ZOOM = CANVAS_W / BASE_W;  // 1.5
const DPR = window.devicePixelRatio || 1;

// 텍스트 고해상도 (zoom + DPR 보상)
Phaser.GameObjects.GameObjectFactory.register('text', function (x, y, text, style) {
    style = style || {};
    if (style.resolution === undefined) style.resolution = Math.max(2, DPR * CAM_ZOOM);
    return this.displayList.add(new Phaser.GameObjects.Text(this.scene, x, y, text, style));
});

const config = {
    type: Phaser.CANVAS,
    width: CANVAS_W,
    height: CANVAS_H,
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

// 카메라 zoom: 1280×720 좌표 → 1920×1080 캔버스
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
