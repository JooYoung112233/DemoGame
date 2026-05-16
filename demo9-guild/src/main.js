const DPR = Math.min(2, window.devicePixelRatio || 1);

// GameObjectFactory.text 후킹 — 모든 add.text() 호출에 자동 resolution 적용
const _origFactoryText = Phaser.GameObjects.GameObjectFactory.prototype.text;
Phaser.GameObjects.GameObjectFactory.prototype.text = function (x, y, text, style) {
    const txt = _origFactoryText.call(this, x, y, text, style);
    if (txt && typeof txt.setResolution === 'function') txt.setResolution(DPR);
    return txt;
};

const config = {
    type: Phaser.AUTO,                         // WEBGL 우선 (텍스트 선명)
    width: 1280,
    height: 720,
    parent: 'game-container',
    backgroundColor: '#0a0a1a',
    scene: [TitleScene, TownScene, RecruitScene, RosterScene, StorageScene, ForgeScene, AuctionScene, TrainingScene, TempleScene, IntelScene, EliteRecruitScene, AffinityScene, DeployScene, PlaceholderBattleScene, BattleScene, CargoBattleScene, BlackoutBattleScene, EventScene, RunResultScene],
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH
    },
    render: {
        pixelArt: false,                        // 텍스트가 픽셀 처리되지 않게
        antialias: true,                        // 안티앨리어싱 (글자 선명)
        antialiasGL: true,
        roundPixels: false
    },
    audio: { noAudio: true },
    disableVisibilityChange: true
};

const game = new Phaser.Game(config);
window.game = game;
