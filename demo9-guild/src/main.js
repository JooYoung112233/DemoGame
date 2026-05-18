// 게임 좌표계 = 캔버스 = 1280×720
// Phaser.Scale.FIT → 브라우저 창에 맞게 CSS 스케일링 (비율 유지)
// 텍스트 resolution = DPR → HiDPI에서도 선명
const BASE_W = 1280, BASE_H = 720;
const DEVICE_DPR = window.devicePixelRatio || 1;

// GameObjectFactory.text 재등록 — HiDPI 텍스트
Phaser.GameObjects.GameObjectFactory.register('text', function (x, y, text, style) {
    style = style || {};
    if (style.resolution === undefined) style.resolution = DEVICE_DPR;
    return this.displayList.add(new Phaser.GameObjects.Text(this.scene, x, y, text, style));
});

const config = {
    type: Phaser.CANVAS,
    width: BASE_W,
    height: BASE_H,
    parent: 'game-container',
    backgroundColor: '#1a1510',
    scene: [TitleScene, TownScene, RecruitScene, RosterScene, EquipmentScene, StorageScene, ForgeScene, AuctionScene, TrainingScene, TempleScene, IntelScene, EliteRecruitScene, AffinityScene, DeployScene, PlaceholderBattleScene, BattleScene, ManualBattleScene, CargoBattleScene, BlackoutBattleScene, EventScene, RunResultScene, GuildHallScene, AutomationScene, CodexScene],
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
