/**
 * CharacterSprites — 캐릭터 PNG 스프라이트 로드 + 흰배경 제거.
 *
 * 사용:
 *   preload(): CharacterSprites.preload(this)
 *   create():  CharacterSprites.process(this)
 *   체크:      CharacterSprites.has(scene, classKey)
 *
 * 처리 후 텍스처 키: `char_${classKey}` (예: char_warrior)
 */
const CharacterSprites = {
    CLASSES: ['warrior', 'rogue', 'archer', 'mage', 'priest', 'alchemist'],

    preload(scene) {
        this.CLASSES.forEach(cls => {
            const rawKey = `char_${cls}_raw`;
            const dstKey = `char_${cls}`;
            if (scene.textures.exists(dstKey) || scene.textures.exists(rawKey)) return;
            scene.load.image(rawKey, `assets/characters/${cls}.png`);
        });
    },

    process(scene) {
        this.CLASSES.forEach(cls => {
            const dstKey = `char_${cls}`;
            const srcKey = `char_${cls}_raw`;
            if (scene.textures.exists(dstKey)) return;
            if (!scene.textures.exists(srcKey)) return;

            const src = scene.textures.get(srcKey).getSourceImage();
            const w = src.width, h = src.height;
            const canvas = scene.textures.createCanvas(dstKey, w, h);
            if (!canvas) return;
            const ctx = canvas.context;
            ctx.drawImage(src, 0, 0);
            const imageData = ctx.getImageData(0, 0, w, h);
            const data = imageData.data;
            for (let i = 0; i < data.length; i += 4) {
                const r = data[i], g = data[i+1], b = data[i+2];
                if (r > 235 && g > 235 && b > 235) {
                    data[i+3] = 0;
                } else if (r > 220 && g > 220 && b > 220) {
                    data[i+3] = Math.floor(((255 - r) + (255 - g) + (255 - b)) / 3 * 8);
                }
            }
            ctx.putImageData(imageData, 0, 0);
            canvas.refresh();
        });
    },

    has(scene, classKey) {
        return scene.textures.exists(`char_${classKey}`);
    }
};
