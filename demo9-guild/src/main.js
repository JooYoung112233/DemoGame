// 게임 좌표계: 1280×720 (코드 그대로)
// 표시 해상도: 1920×1080 (zoom 1.5배)
// 텍스트 resolution: DPR × ZOOM 으로 보강 → 확대해도 선명
const ZOOM = 1.5;
const DEVICE_DPR = window.devicePixelRatio || 1;
const TEXT_DPR = DEVICE_DPR * ZOOM;

// 1) GameObjectFactory.text 재등록 — 모든 텍스트 객체가 더 높은 resolution으로 생성
Phaser.GameObjects.GameObjectFactory.register('text', function (x, y, text, style) {
    style = style || {};
    if (style.resolution === undefined) style.resolution = TEXT_DPR;
    return this.displayList.add(new Phaser.GameObjects.Text(this.scene, x, y, text, style));
});

const config = {
    type: Phaser.AUTO,
    width: 1280,
    height: 720,
    parent: 'game-container',
    backgroundColor: '#0a0a1a',
    scene: [TitleScene, TownScene, RecruitScene, RosterScene, StorageScene, ForgeScene, AuctionScene, TrainingScene, TempleScene, IntelScene, EliteRecruitScene, AffinityScene, DeployScene, PlaceholderBattleScene, BattleScene, CargoBattleScene, BlackoutBattleScene, EventScene, RunResultScene],
    scale: {
        mode: Phaser.Scale.NONE,
        autoCenter: Phaser.Scale.CENTER_BOTH,
        zoom: ZOOM                               // 1.5배 확대 표시 (1920×1080)
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

// 2) 캔버스 CSS 크기 명시 — 1920×1080 표시
game.events.once('ready', () => {
    const canvas = game.canvas;
    if (canvas) {
        canvas.style.width = '1920px';
        canvas.style.height = '1080px';
    }
});
