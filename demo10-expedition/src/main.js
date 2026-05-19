const config = {
    type: Phaser.AUTO,
    parent: 'game-container',
    width: 1280,
    height: 720,
    backgroundColor: '#0a0a1a',
    scene: [SafeHouseScene, ExpeditionScene, BattleScene],
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH
    },
    render: {
        pixelArt: false,
        antialias: true
    }
};

const game = new Phaser.Game(config);
game.events.on('ready', () => {
    if (game.canvas) game.canvas.setAttribute('tabindex', '0');
    setTimeout(() => { if (game.canvas) game.canvas.focus(); }, 100);
});
