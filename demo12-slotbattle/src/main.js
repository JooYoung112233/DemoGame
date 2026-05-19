const config = {
    type: Phaser.CANVAS,
    width: 1280,
    height: 720,
    parent: 'game-container',
    backgroundColor: '#0a0a1a',
    scene: [TitleScene, BattleScene, RewardScene, GameOverScene],
};

const game = new Phaser.Game(config);
