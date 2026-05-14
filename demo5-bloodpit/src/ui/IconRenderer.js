const IconRenderer = {
    drawWeaponIcon(gfx, id, x, y, size = 24) {
        const s = size / 24;
        gfx.save();
        gfx.translateCanvas(x, y);
        gfx.scaleCanvas(s, s);

        switch (id) {
            case 'sword':
                // blade
                gfx.fillStyle(0xccccdd);
                gfx.fillRect(-2, -10, 4, 16);
                gfx.fillStyle(0xeeeeff);
                gfx.fillRect(-1, -10, 2, 14);
                // guard
                gfx.fillStyle(0x886622);
                gfx.fillRect(-5, 5, 10, 3);
                // grip
                gfx.fillStyle(0x664411);
                gfx.fillRect(-2, 8, 4, 5);
                // pommel
                gfx.fillStyle(0xffcc44);
                gfx.fillCircle(0, 14, 2);
                break;

            case 'axe':
                // handle
                gfx.fillStyle(0x885533);
                gfx.fillRect(-1, -4, 3, 18);
                // axe head
                gfx.fillStyle(0xaaaabb);
                gfx.fillRect(-8, -8, 10, 4);
                gfx.fillRect(-10, -6, 4, 8);
                gfx.fillRect(-8, -4, 8, 3);
                // edge highlight
                gfx.fillStyle(0xddddee);
                gfx.fillRect(-10, -5, 2, 6);
                break;

            case 'spear':
                // shaft
                gfx.fillStyle(0x886644);
                gfx.fillRect(-1, -6, 2, 22);
                // spearhead
                gfx.fillStyle(0xccccdd);
                gfx.beginPath();
                gfx.moveTo(0, -12);
                gfx.lineTo(-4, -4);
                gfx.lineTo(4, -4);
                gfx.closePath();
                gfx.fillPath();
                // highlight
                gfx.fillStyle(0xeeeeff);
                gfx.fillRect(-1, -10, 2, 5);
                break;

            case 'shotgun':
                // barrel
                gfx.fillStyle(0x555566);
                gfx.fillRect(-2, -10, 4, 14);
                gfx.fillStyle(0x666677);
                gfx.fillRect(-3, -10, 6, 3);
                // stock
                gfx.fillStyle(0x774422);
                gfx.fillRect(-3, 4, 6, 8);
                gfx.fillStyle(0x663311);
                gfx.fillRect(-2, 8, 5, 5);
                // muzzle flash hint
                gfx.fillStyle(0xffaa44, 0.4);
                gfx.fillCircle(0, -12, 3);
                break;

            default: // fist
                gfx.fillStyle(0xffcc99);
                gfx.fillCircle(0, 0, 7);
                gfx.fillStyle(0xeebb88);
                gfx.fillRect(-5, -2, 10, 6);
                gfx.fillStyle(0xffcc99);
                for (let i = 0; i < 4; i++) {
                    gfx.fillRect(-5 + i * 3, -4, 2, 4);
                }
                break;
        }

        gfx.scaleCanvas(1/s, 1/s);
        gfx.translateCanvas(-x, -y);
        gfx.restore();
    },

    drawPassiveIcon(gfx, tag, x, y, size = 24) {
        const s = size / 24;
        gfx.save();
        gfx.translateCanvas(x, y);
        gfx.scaleCanvas(s, s);

        switch (tag) {
            case 'bleed':
                // blood drop
                gfx.fillStyle(0xff2222);
                gfx.beginPath();
                gfx.moveTo(0, -8);
                gfx.lineTo(-6, 2);
                gfx.arc(0, 3, 6, Math.PI, 0, false);
                gfx.lineTo(0, -8);
                gfx.closePath();
                gfx.fillPath();
                // highlight
                gfx.fillStyle(0xff6666, 0.5);
                gfx.fillCircle(-2, 1, 2);
                break;

            case 'dodge':
                // wind swirl
                gfx.lineStyle(2, 0x44ffff, 0.8);
                gfx.beginPath();
                gfx.arc(0, 0, 8, -0.5, Math.PI * 1.2, false);
                gfx.strokePath();
                gfx.beginPath();
                gfx.arc(2, -1, 5, 0, Math.PI * 1.0, false);
                gfx.strokePath();
                // small dot
                gfx.fillStyle(0x88ffff);
                gfx.fillCircle(6, -4, 2);
                break;

            case 'corpse':
                // skull
                gfx.fillStyle(0xddddcc);
                gfx.fillCircle(0, -2, 7);
                // eyes
                gfx.fillStyle(0x111111);
                gfx.fillCircle(-3, -3, 2);
                gfx.fillCircle(3, -3, 2);
                // nose
                gfx.fillStyle(0x222211);
                gfx.beginPath();
                gfx.moveTo(0, -1);
                gfx.lineTo(-1, 1);
                gfx.lineTo(1, 1);
                gfx.closePath();
                gfx.fillPath();
                // jaw
                gfx.fillStyle(0xccccbb);
                gfx.fillRect(-5, 3, 10, 4);
                // teeth
                gfx.fillStyle(0x111111);
                for (let i = 0; i < 4; i++) gfx.fillRect(-4 + i * 3, 3, 1, 4);
                break;

            default:
                // generic star/buff icon
                this._drawStar(gfx, 0, 0, 5, 10, 5, 0xffcc44);
                break;
        }

        gfx.scaleCanvas(1/s, 1/s);
        gfx.translateCanvas(-x, -y);
        gfx.restore();
    },

    drawStatIcon(gfx, statId, x, y, size = 20) {
        const s = size / 20;
        gfx.save();
        gfx.translateCanvas(x, y);
        gfx.scaleCanvas(s, s);

        switch (statId) {
            case 'atk_up':
                // crossed swords
                gfx.fillStyle(0xff6644);
                gfx.fillRect(-1, -8, 2, 14);
                gfx.fillRect(-4, -3, 8, 2);
                // arrow up
                gfx.beginPath();
                gfx.moveTo(0, -10);
                gfx.lineTo(-3, -6);
                gfx.lineTo(3, -6);
                gfx.closePath();
                gfx.fillPath();
                break;

            case 'spd_up':
                // lightning bolt
                gfx.fillStyle(0xffcc44);
                gfx.beginPath();
                gfx.moveTo(2, -9);
                gfx.lineTo(-4, 0);
                gfx.lineTo(0, 0);
                gfx.lineTo(-2, 9);
                gfx.lineTo(4, 0);
                gfx.lineTo(0, 0);
                gfx.closePath();
                gfx.fillPath();
                break;

            case 'hp_up':
                // heart
                gfx.fillStyle(0xff4466);
                gfx.fillCircle(-4, -3, 5);
                gfx.fillCircle(4, -3, 5);
                gfx.beginPath();
                gfx.moveTo(-8, -1);
                gfx.lineTo(0, 9);
                gfx.lineTo(8, -1);
                gfx.closePath();
                gfx.fillPath();
                break;

            case 'crit_up':
            case 'crit_dmg':
                // exclamation / critical hit
                gfx.fillStyle(0xff8844);
                this._drawStar(gfx, 0, 0, 4, 9, 6, 0xff8844);
                gfx.fillStyle(0xffcc88);
                gfx.fillCircle(0, 0, 3);
                break;

            case 'lifesteal':
                // fang + blood
                gfx.fillStyle(0xff2244);
                gfx.beginPath();
                gfx.moveTo(-3, -6);
                gfx.lineTo(-5, 4);
                gfx.lineTo(-1, 0);
                gfx.closePath();
                gfx.fillPath();
                gfx.beginPath();
                gfx.moveTo(3, -6);
                gfx.lineTo(5, 4);
                gfx.lineTo(1, 0);
                gfx.closePath();
                gfx.fillPath();
                // drop
                gfx.fillStyle(0xff4466);
                gfx.fillCircle(0, 6, 3);
                break;

            case 'thorns':
                // shield with spikes
                gfx.fillStyle(0x888899);
                gfx.fillRect(-6, -5, 12, 10);
                gfx.beginPath();
                gfx.moveTo(-6, 5);
                gfx.lineTo(0, 10);
                gfx.lineTo(6, 5);
                gfx.closePath();
                gfx.fillPath();
                // spikes
                gfx.fillStyle(0xffaa44);
                gfx.beginPath(); gfx.moveTo(-8, -3); gfx.lineTo(-12, 0); gfx.lineTo(-8, 3); gfx.closePath(); gfx.fillPath();
                gfx.beginPath(); gfx.moveTo(8, -3); gfx.lineTo(12, 0); gfx.lineTo(8, 3); gfx.closePath(); gfx.fillPath();
                break;

            default:
                this._drawStar(gfx, 0, 0, 4, 8, 5, 0xffcc44);
                break;
        }

        gfx.scaleCanvas(1/s, 1/s);
        gfx.translateCanvas(-x, -y);
        gfx.restore();
    },

    drawRarityGlow(gfx, x, y, w, h, rarityColor, alpha = 0.15) {
        for (let i = 3; i >= 0; i--) {
            gfx.fillStyle(rarityColor, alpha * (1 - i * 0.2));
            gfx.fillRoundedRect(x - w/2 - i*2, y - h/2 - i*2, w + i*4, h + i*4, 6 + i);
        }
    },

    drawItemCard(scene, x, y, item, cardW = 280, cardH = 200) {
        const rarity = BP_RARITIES[item.rarity] || BP_RARITIES.common;
        const container = scene.add.container(x, y);

        // glow background
        const glowGfx = scene.add.graphics();
        IconRenderer.drawRarityGlow(glowGfx, 0, 0, cardW, cardH, rarity.borderColor, 0.12);
        container.add(glowGfx);

        // card background
        const bg = scene.add.rectangle(0, 0, cardW, cardH, 0x1a0a0a)
            .setStrokeStyle(3, rarity.borderColor);
        container.add(bg);

        // rarity stripe at top
        const stripe = scene.add.rectangle(0, -cardH/2 + 3, cardW - 6, 6, rarity.borderColor, 0.6);
        container.add(stripe);

        // icon area
        const iconGfx = scene.add.graphics();
        const iconBg = scene.add.circle(0, -cardH/2 + 42, 22, 0x221111)
            .setStrokeStyle(2, rarity.borderColor, 0.5);
        container.add(iconBg);

        if (item.type === 'weapon') {
            IconRenderer.drawWeaponIcon(iconGfx, item.id, 0, -cardH/2 + 42, 30);
        } else if (item.type === 'passive') {
            if (item.tag) {
                IconRenderer.drawPassiveIcon(iconGfx, item.tag, 0, -cardH/2 + 42, 30);
            } else {
                IconRenderer.drawStatIcon(iconGfx, item.id, 0, -cardH/2 + 42, 28);
            }
        } else {
            // consumable
            IconRenderer.drawConsumableIcon(iconGfx, item.id, 0, -cardH/2 + 42, 28);
        }
        container.add(iconGfx);

        // rarity label
        container.add(scene.add.text(0, -cardH/2 + 70, `[${rarity.name}]`, {
            fontSize: '11px', fontFamily: 'monospace', color: rarity.color
        }).setOrigin(0.5));

        // item name
        container.add(scene.add.text(0, -cardH/2 + 88, item.name, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5));

        // type label
        const typeLabel = { weapon: '⚔️ 무기', passive: '🔮 패시브', consumable: '🧪 소비' }[item.type] || '';
        container.add(scene.add.text(0, -cardH/2 + 106, typeLabel, {
            fontSize: '11px', fontFamily: 'monospace', color: '#777777'
        }).setOrigin(0.5));

        // description
        container.add(scene.add.text(0, -cardH/2 + 135, item.desc, {
            fontSize: '12px', fontFamily: 'monospace', color: '#cccccc',
            wordWrap: { width: cardW - 30 }, align: 'center'
        }).setOrigin(0.5));

        // tag badge
        if (item.tag) {
            const tagColor = { bleed: '#ff4444', dodge: '#44ffff', corpse: '#ff8844' }[item.tag] || '#888888';
            const tagBg = scene.add.rectangle(0, cardH/2 - 48, 90, 18, 0x000000, 0.5)
                .setStrokeStyle(1, Phaser.Display.Color.HexStringToColor(tagColor).color, 0.5);
            container.add(tagBg);
            container.add(scene.add.text(0, cardH/2 - 48, `${item.tag} 빌드`, {
                fontSize: '10px', fontFamily: 'monospace', color: tagColor
            }).setOrigin(0.5));
        }

        // select button
        const selectBtn = scene.add.rectangle(0, cardH/2 - 20, 130, 32, rarity.borderColor, 0.3)
            .setStrokeStyle(2, rarity.borderColor).setInteractive();
        container.add(selectBtn);
        container.add(scene.add.text(0, cardH/2 - 20, '선택', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5));

        // hover effects
        bg.setInteractive();
        bg.on('pointerover', () => { bg.setFillStyle(0x2a1515); selectBtn.setFillStyle(rarity.borderColor, 0.5); });
        bg.on('pointerout', () => { bg.setFillStyle(0x1a0a0a); selectBtn.setFillStyle(rarity.borderColor, 0.3); });

        return { container, bg, selectBtn };
    },

    drawConsumableIcon(gfx, id, x, y, size = 24) {
        const s = size / 24;
        gfx.save();
        gfx.translateCanvas(x, y);
        gfx.scaleCanvas(s, s);

        switch (id) {
            case 'bandage':
                gfx.fillStyle(0xeeeeee);
                gfx.fillRect(-2, -8, 4, 16);
                gfx.fillRect(-8, -2, 16, 4);
                gfx.fillStyle(0xff4444);
                gfx.fillRect(-1, -6, 2, 12);
                gfx.fillRect(-6, -1, 12, 2);
                break;

            case 'bomb':
                gfx.fillStyle(0x333333);
                gfx.fillCircle(0, 2, 8);
                gfx.fillStyle(0x444444);
                gfx.fillCircle(-2, 0, 3);
                // fuse
                gfx.lineStyle(2, 0x886644);
                gfx.beginPath();
                gfx.moveTo(3, -6);
                gfx.lineTo(6, -10);
                gfx.strokePath();
                // spark
                gfx.fillStyle(0xffaa22);
                gfx.fillCircle(6, -10, 3);
                gfx.fillStyle(0xffff44);
                gfx.fillCircle(6, -10, 1.5);
                break;

            case 'antidote':
                // potion bottle
                gfx.fillStyle(0x44aa66);
                gfx.fillRect(-5, -2, 10, 10);
                gfx.fillRect(-3, -6, 6, 5);
                gfx.fillStyle(0x886644);
                gfx.fillRect(-3, -8, 6, 3);
                // liquid highlight
                gfx.fillStyle(0x66cc88, 0.5);
                gfx.fillRect(-3, 0, 3, 6);
                break;

            default:
                gfx.fillStyle(0x888888);
                gfx.fillCircle(0, 0, 8);
                gfx.fillStyle(0xaaaaaa);
                gfx.fillCircle(-2, -2, 3);
                break;
        }

        gfx.scaleCanvas(1/s, 1/s);
        gfx.translateCanvas(-x, -y);
        gfx.restore();
    },

    _drawStar(gfx, cx, cy, innerR, outerR, points, color) {
        gfx.fillStyle(color);
        gfx.beginPath();
        for (let i = 0; i < points * 2; i++) {
            const r = i % 2 === 0 ? outerR : innerR;
            const angle = (Math.PI * 2 * i) / (points * 2) - Math.PI / 2;
            const px = cx + Math.cos(angle) * r;
            const py = cy + Math.sin(angle) * r;
            if (i === 0) gfx.moveTo(px, py);
            else gfx.lineTo(px, py);
        }
        gfx.closePath();
        gfx.fillPath();
    }
};
