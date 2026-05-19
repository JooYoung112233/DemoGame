class CraftScene extends Phaser.Scene {
    constructor() { super('CraftScene'); }

    create() {
        const gs = window.gameState;
        const cx = 640;

        this.add.rectangle(cx, 360, 1280, 720, 0x0a0a1a);

        this.add.text(cx, 40, '🔨 제작', {
            fontSize: '28px', fontFamily: 'monospace', color: '#ff8844',
            stroke: '#000', strokeThickness: 3,
        }).setOrigin(0.5);

        const recipes = Object.entries(RECIPE_DATA).filter(([, r]) => gs.day >= r.unlockDay);

        if (recipes.length === 0) {
            this.add.text(cx, 300, '해금된 레시피가 없습니다.', {
                fontSize: '18px', fontFamily: 'monospace', color: '#555',
            }).setOrigin(0.5);
        } else {
            let y = 100;
            recipes.forEach(([recipeId, recipe]) => {
                const resultData = ITEM_DATA[recipe.result];
                const panel = this.add.rectangle(cx, y + 35, 500, 60, 0x1a1a35);
                panel.setStrokeStyle(1, 0x2a2a50);

                this.add.text(cx - 220, y + 15, `${resultData.icon} ${resultData.name}`, {
                    fontSize: '16px', fontFamily: 'monospace', color: '#ddd',
                });

                const ingredientStr = recipe.ingredients.map(ing => {
                    const d = ITEM_DATA[ing.id];
                    const have = gs.getItemCount(ing.id);
                    const color = have >= ing.count ? '#44ff88' : '#ff4444';
                    return `${d.icon}x${ing.count}(${have})`;
                }).join('  ');

                this.add.text(cx - 220, y + 38, ingredientStr, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#aaa',
                });

                this.add.text(cx + 120, y + 25, `→ ${resultData.basePrice}G`, {
                    fontSize: '14px', fontFamily: 'monospace', color: '#ffcc44',
                });

                const canCraft = recipe.ingredients.every(ing => gs.getItemCount(ing.id) >= ing.count);

                const craftBtn = new UIButton(this, cx + 210, y + 35, canCraft ? '제작' : '재료 부족', {
                    width: 100, height: 36,
                    bg: canCraft ? 0x224422 : 0x333333,
                    hoverBg: canCraft ? 0x336633 : 0x333333,
                    textColor: canCraft ? '#44ff88' : '#666',
                    fontSize: '13px',
                    onClick: () => {
                        if (!canCraft) return;
                        recipe.ingredients.forEach(ing => gs.removeItem(ing.id, ing.count));
                        gs.addItems([{ id: recipe.result, qty: 1 }]);
                        Toast.show(this, `${resultData.name} 제작 완료!`, { color: '#ff8844' });
                        this.scene.restart();
                    }
                });

                y += 80;
            });
        }

        new UIButton(this, 100, 680, '← 돌아가기', {
            width: 160, height: 40, bg: 0x333333, hoverBg: 0x555555,
            textColor: '#aaa', fontSize: '14px',
            onClick: () => this.scene.start('HubScene'),
        });
    }
}
