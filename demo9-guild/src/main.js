const DPR = window.devicePixelRatio || 1;

// 1) GameObjectFactory.text 재등록 — 모든 텍스트 객체가 DPR resolution으로 생성
Phaser.GameObjects.GameObjectFactory.register('text', function (x, y, text, style) {
    style = style || {};
    if (style.resolution === undefined) style.resolution = DPR;
    return this.displayList.add(new Phaser.GameObjects.Text(this.scene, x, y, text, style));
});

const config = {
    type: Phaser.AUTO,                         // WEBGL 우선 (텍스트 선명)
    width: 1280,
    height: 720,
    parent: 'game-container',
    backgroundColor: '#0a0a1a',
    scene: [TitleScene, TownScene, RecruitScene, RosterScene, StorageScene, ForgeScene, AuctionScene, TrainingScene, TempleScene, IntelScene, EliteRecruitScene, AffinityScene, DeployScene, PlaceholderBattleScene, BattleScene, CargoBattleScene, BlackoutBattleScene, EventScene, RunResultScene],
    scale: {
        mode: Phaser.Scale.NONE,                // 해상도 고정 (1280x720, 확대 X)
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

// 2) 캔버스 CSS 크기 명시 — 정확히 1280×720 픽셀로 표시 (브라우저 확대 방지)
game.events.once('ready', () => {
    const canvas = game.canvas;
    if (canvas) {
        canvas.style.width = '1280px';
        canvas.style.height = '720px';
    }
});
