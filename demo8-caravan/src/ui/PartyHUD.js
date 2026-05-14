class PartyHUD {
    static draw(scene, runState, y) {
        const party = runState.party;
        const startX = 640 - (party.length * 50) / 2;

        party.forEach((unit, i) => {
            const x = startX + i * 50;
            const stats = unit.getStats();
            const hpRatio = unit.currentHp / stats.hp;

            const bg = scene.add.rectangle(x, y, 40, 50, 0x1a1a2e).setStrokeStyle(1, 0x444466).setDepth(20);

            const bodyColor = UNIT_DATA[unit.classKey].color;
            const body = scene.add.rectangle(x, y - 8, 12, 14, bodyColor).setDepth(21);
            const head = scene.add.rectangle(x, y - 20, 8, 8, 0xffcc99).setDepth(21);

            const hpBg = scene.add.rectangle(x, y + 16, 30, 4, 0x333333).setDepth(21);
            let hpColor = 0x44ff44;
            if (hpRatio <= 0.3) hpColor = 0xff4444;
            else if (hpRatio <= 0.6) hpColor = 0xffaa00;
            const hpBar = scene.add.rectangle(x - 15, y + 16, 30 * hpRatio, 4, hpColor).setOrigin(0, 0.5).setDepth(22);

            const tierText = unit.tier > 1 ? `T${unit.tier}` : '';
            if (tierText) {
                scene.add.text(x + 16, y - 25, tierText, {
                    fontSize: '8px', fontFamily: 'monospace', color: '#ffcc44',
                    stroke: '#000000', strokeThickness: 1
                }).setOrigin(0.5).setDepth(23);
            }
        });

        scene.add.text(640, y + 32, `🪙 ${runState.gold}G`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc44',
            stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5).setDepth(20);
    }
}
