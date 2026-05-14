class StatusEffectSystem {
    static getEffectIcon(type) {
        switch (type) {
            case 'bleed': return { text: '🩸', color: 0xff4444 };
            case 'taunt': return { text: '⚔', color: 0x4488ff };
            case 'shield': return { text: '🛡', color: 0x88ccff };
            default: return { text: '?', color: 0xffffff };
        }
    }
}
