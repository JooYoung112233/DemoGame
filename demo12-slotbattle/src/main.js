const config = {
    type: Phaser.CANVAS,
    width: 1280,
    height: 720,
    parent: 'game-container',
    backgroundColor: '#0a0a1a',
    scene: [TitleScene, MapScene, BattleScene, ShopScene, TreasureScene, RestScene, EventScene, HelpScene, CodexScene, GameOverScene],
};

const game = new Phaser.Game(config);
