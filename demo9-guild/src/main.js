// 게임 좌표계: 1280×720
// 캔버스 해상도: 1280×720 × zoom → CSS FIT으로 뷰포트 맞춤
// zoom ≥ 뷰포트/좌표 비율이면 CSS는 다운스케일(선명)만 함
const BASE_W = 1280, BASE_H = 720;
const DPR = window.devicePixelRatio || 1;

// 뷰포트 대비 필요 zoom 계산 — 업스케일 방지
const viewScale = Math.min(window.innerWidth / BASE_W, window.innerHeight / BASE_H);
const ZOOM = Math.max(1, Math.ceil(viewScale * DPR * 10) / 10);  // 올림으로 넉넉하게

// 텍스트 선명도
Phaser.GameObjects.GameObjectFactory.register('text', function (x, y, text, style) {
    style = style || {};
    if (style.resolution === undefined) style.resolution = Math.max(DPR, ZOOM);
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
        autoCenter: Phaser.Scale.CENTER_BOTH,
        zoom: ZOOM,
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
