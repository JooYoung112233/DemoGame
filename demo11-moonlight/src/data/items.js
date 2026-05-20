const ITEM_DATA = {
    // === 약초 (숲) ===
    heal_herb: { name: '치유초', category: 'herb', basePrice: 8, icon: '🌿', desc: '기본적인 치유 약초' },
    poison_herb: { name: '독초', category: 'herb', basePrice: 12, icon: '☠️', desc: '독성이 있는 약초' },
    light_flower: { name: '빛꽃', category: 'herb', basePrice: 15, icon: '🌸', desc: '은은하게 빛나는 꽃' },
    wood: { name: '목재', category: 'herb', basePrice: 5, icon: '🪵', desc: '쓸만한 나무토막' },

    // === 광석 (광산) ===
    iron_ore: { name: '철광석', category: 'ore', basePrice: 15, icon: '⛏️', desc: '단단한 철광석' },
    silver_ore: { name: '은광석', category: 'ore', basePrice: 25, icon: '⬜', desc: '빛나는 은광석' },
    magic_stone: { name: '마석', category: 'ore', basePrice: 30, icon: '💎', desc: '마력이 깃든 돌' },

    // === 가죽 (들판) ===
    wolf_hide: { name: '늑대가죽', category: 'hide', basePrice: 12, icon: '🐺', desc: '질긴 늑대가죽' },
    bear_hide: { name: '곰가죽', category: 'hide', basePrice: 20, icon: '🐻', desc: '두꺼운 곰가죽' },

    // === 몬스터 드랍 ===
    goblin_tooth: { name: '고블린 이빨', category: 'monster', basePrice: 5, icon: '🦷', desc: '날카로운 이빨' },
    slime_jelly: { name: '슬라임 젤리', category: 'monster', basePrice: 8, icon: '🟢', desc: '끈적한 젤리' },
    bat_wing: { name: '박쥐 날개', category: 'monster', basePrice: 6, icon: '🦇', desc: '얇은 박쥐 날개' },

    // === 보석 (광산 희귀) ===
    ruby: { name: '루비', category: 'gem', basePrice: 50, icon: '🔴', desc: '붉게 빛나는 보석' },
    sapphire: { name: '사파이어', category: 'gem', basePrice: 60, icon: '🔵', desc: '깊은 파란빛 보석' },

    // === 유물 (폐허) ===
    ancient_coin: { name: '고대 동전', category: 'relic', basePrice: 35, icon: '🪙', desc: '오래된 문명의 동전' },
    rune_fragment: { name: '룬 파편', category: 'relic', basePrice: 45, icon: '🔮', desc: '마법 문자가 새겨진 파편' },

    // === 가공품 (제작) ===
    health_potion: { name: '체력 포션', category: 'crafted', basePrice: 30, icon: '❤️', desc: '체력을 회복시키는 포션' },
    antidote: { name: '해독제', category: 'crafted', basePrice: 25, icon: '💚', desc: '독을 치료하는 약' },
    iron_dagger: { name: '철 단검', category: 'crafted', basePrice: 55, icon: '🗡️', desc: '날카로운 단검' },
    silver_ring: { name: '은반지', category: 'crafted', basePrice: 70, icon: '💍', desc: '정교한 은 반지' },

    // === 연구 가공품 ===
    mana_potion: { name: '마나 포션', category: 'crafted', basePrice: 45, icon: '💙', desc: '마력을 회복시키는 포션' },
    enchanted_blade: { name: '마력 칼날', category: 'crafted', basePrice: 85, icon: '⚔️', desc: '룬의 힘이 깃든 칼날' },
    beast_armor: { name: '야수 갑옷', category: 'crafted', basePrice: 90, icon: '🛡️', desc: '야수 가죽으로 만든 갑옷' },
    gem_amulet: { name: '보석 부적', category: 'crafted', basePrice: 150, icon: '📿', desc: '강력한 보호의 부적' },
    elixir: { name: '만능약', category: 'crafted', basePrice: 60, icon: '🧪', desc: '만병통치약' },
};

const CATEGORIES = {
    herb: { name: '약초', color: '#44ff88' },
    ore: { name: '광석', color: '#aaaaff' },
    hide: { name: '가죽', color: '#cc8844' },
    monster: { name: '몬스터', color: '#ff6666' },
    gem: { name: '보석', color: '#ff44ff' },
    relic: { name: '유물', color: '#ffcc44' },
    crafted: { name: '가공품', color: '#44ccff' },
};
