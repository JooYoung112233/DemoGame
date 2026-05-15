const ITEM_REGISTRY = {};

const ItemRegistry = {
    register(item) {
        ITEM_REGISTRY[item.itemId] = item;
    },

    registerAll(items) {
        items.forEach(item => this.register(item));
    },

    get(itemId) {
        return ITEM_REGISTRY[itemId] || null;
    },

    getAll() {
        return Object.values(ITEM_REGISTRY);
    },

    getByDemo(demoId) {
        return Object.values(ITEM_REGISTRY).filter(i => i.sourceDemos.includes(demoId));
    },

    getByCategory(category) {
        return Object.values(ITEM_REGISTRY).filter(i => i.category === category);
    },

    getBySlot(slot) {
        return Object.values(ITEM_REGISTRY).filter(i => i.slot === slot);
    },

    getValue(item) {
        const base = item.baseValue || 10;
        const mult = FARMING.RARITIES[item.rarity]?.valueMultiplier || 1;
        return Math.floor(base * mult);
    }
};

// ── Demo5 (Blood Pit) 파밍 아이템 ──

ItemRegistry.registerAll([
    // 거래재 (Materials)
    { itemId: 'blood_gem', name: '혈석', desc: '핏빛으로 물든 보석', category: 'material', slot: null, baseValue: 15, weight: 1, rarity: 'uncommon', icon: '🔴', sourceDemos: ['demo5'], stats: null },
    { itemId: 'beast_fang', name: '야수의 송곳니', desc: '날카로운 이빨', category: 'material', slot: null, baseValue: 8, weight: 1, rarity: 'common', icon: '🦷', sourceDemos: ['demo5'], stats: null },
    { itemId: 'gladiator_medal', name: '투사의 증표', desc: '생존자의 훈장', category: 'material', slot: null, baseValue: 25, weight: 1, rarity: 'rare', icon: '🏅', sourceDemos: ['demo5'], stats: null },
    { itemId: 'pit_essence', name: '핏 에센스', desc: '투기장의 정수', category: 'material', slot: null, baseValue: 50, weight: 2, rarity: 'epic', icon: '🩸', sourceDemos: ['demo5'], stats: null },
    { itemId: 'pitlord_heart', name: '핏로드의 심장', desc: '보스의 핵심 장기', category: 'material', slot: null, baseValue: 100, weight: 3, rarity: 'legendary', icon: '❤️‍🔥', sourceDemos: ['demo5'], stats: null },
    { itemId: 'bone_fragment', name: '뼈 조각', desc: '부서진 해골 파편', category: 'material', slot: null, baseValue: 5, weight: 1, rarity: 'common', icon: '🦴', sourceDemos: ['demo5'], stats: null },
    { itemId: 'dark_crystal', name: '암흑 수정', desc: '어둠이 응축된 결정', category: 'material', slot: null, baseValue: 40, weight: 2, rarity: 'rare', icon: '🔮', sourceDemos: ['demo5'], stats: null },

    // 장비 - 무기
    { itemId: 'iron_sword', name: '무쇠 검', desc: 'ATK+8', category: 'equipment', slot: 'weapon', baseValue: 20, weight: 3, rarity: 'common', icon: '⚔️', sourceDemos: ['demo5'], stats: { atk: 8 } },
    { itemId: 'blood_blade', name: '피의 칼날', desc: 'ATK+15, 출혈+5%', category: 'equipment', slot: 'weapon', baseValue: 40, weight: 3, rarity: 'uncommon', icon: '⚔️', sourceDemos: ['demo5'], stats: { atk: 15, bleedChance: 0.05 } },
    { itemId: 'venom_dagger', name: '독 단검', desc: 'ATK+12, CRIT+8%', category: 'equipment', slot: 'weapon', baseValue: 45, weight: 2, rarity: 'uncommon', icon: '🗡️', sourceDemos: ['demo5'], stats: { atk: 12, critRate: 0.08 } },
    { itemId: 'pit_cleaver', name: '투기장 도끼', desc: 'ATK+25, 치명뎀+30%', category: 'equipment', slot: 'weapon', baseValue: 80, weight: 4, rarity: 'rare', icon: '🪓', sourceDemos: ['demo5'], stats: { atk: 25, critDmg: 0.3 } },
    { itemId: 'demon_scythe', name: '악마의 낫', desc: 'ATK+40, 흡혈5%', category: 'equipment', slot: 'weapon', baseValue: 150, weight: 5, rarity: 'epic', icon: '⚔️', sourceDemos: ['demo5'], stats: { atk: 40, lifesteal: 0.05 } },
    { itemId: 'pitlord_fang', name: '핏로드의 이빨', desc: 'ATK+60, 출혈+15%, 흡혈8%', category: 'equipment', slot: 'weapon', baseValue: 300, weight: 5, rarity: 'legendary', icon: '⚔️', sourceDemos: ['demo5'], stats: { atk: 60, bleedChance: 0.15, lifesteal: 0.08 } },

    // 장비 - 방어구
    { itemId: 'leather_vest', name: '가죽 조끼', desc: 'DEF+5, HP+30', category: 'equipment', slot: 'armor', baseValue: 15, weight: 3, rarity: 'common', icon: '🛡️', sourceDemos: ['demo5'], stats: { def: 5, hp: 30 } },
    { itemId: 'chain_mail', name: '사슬 갑옷', desc: 'DEF+10, HP+60', category: 'equipment', slot: 'armor', baseValue: 35, weight: 5, rarity: 'uncommon', icon: '🛡️', sourceDemos: ['demo5'], stats: { def: 10, hp: 60 } },
    { itemId: 'bone_armor', name: '뼈 갑옷', desc: 'DEF+15, 가시5', category: 'equipment', slot: 'armor', baseValue: 70, weight: 5, rarity: 'rare', icon: '🛡️', sourceDemos: ['demo5'], stats: { def: 15, thorns: 5 } },
    { itemId: 'blood_plate', name: '혈철 판금', desc: 'DEF+22, HP+120, 흡혈3%', category: 'equipment', slot: 'armor', baseValue: 140, weight: 6, rarity: 'epic', icon: '🛡️', sourceDemos: ['demo5'], stats: { def: 22, hp: 120, lifesteal: 0.03 } },

    // 장비 - 악세서리
    { itemId: 'bone_ring', name: '뼈 반지', desc: 'CRIT+5%', category: 'equipment', slot: 'accessory', baseValue: 12, weight: 1, rarity: 'common', icon: '💍', sourceDemos: ['demo5'], stats: { critRate: 0.05 } },
    { itemId: 'blood_amulet', name: '피의 부적', desc: '출혈+8%, ATK+5', category: 'equipment', slot: 'accessory', baseValue: 30, weight: 1, rarity: 'uncommon', icon: '📿', sourceDemos: ['demo5'], stats: { bleedChance: 0.08, atk: 5 } },
    { itemId: 'dodge_charm', name: '회피의 부적', desc: '회피+10%, 이속+10', category: 'equipment', slot: 'accessory', baseValue: 55, weight: 1, rarity: 'rare', icon: '💍', sourceDemos: ['demo5'], stats: { dodgeRate: 0.10, moveSpeed: 10 } },
    { itemId: 'pit_sigil', name: '투기장 문양', desc: 'ATK+15, CRIT+10%, 치명뎀+20%', category: 'equipment', slot: 'accessory', baseValue: 120, weight: 1, rarity: 'epic', icon: '💍', sourceDemos: ['demo5'], stats: { atk: 15, critRate: 0.10, critDmg: 0.20 } },

    // 소모품
    { itemId: 'pit_bandage', name: '핏 붕대', desc: '전원 HP 25% 회복', category: 'consumable', slot: null, baseValue: 10, weight: 1, rarity: 'common', icon: '🩹', sourceDemos: ['demo5'], stats: null },
    { itemId: 'fire_bomb', name: '화염탄', desc: '적 전체 100 피해', category: 'consumable', slot: null, baseValue: 18, weight: 2, rarity: 'uncommon', icon: '💣', sourceDemos: ['demo5'], stats: null },
    { itemId: 'war_potion', name: '전쟁의 물약', desc: 'ATK 30% 증가 (1라운드)', category: 'consumable', slot: null, baseValue: 25, weight: 1, rarity: 'rare', icon: '🧪', sourceDemos: ['demo5'], stats: null },

    // 탈출킷
    { itemId: 'escape_kit', name: '탈출킷', desc: '사용 시 사냥터에서 안전하게 탈출', category: 'consumable', slot: null, baseValue: 15, weight: 1, rarity: 'uncommon', icon: '🚪', sourceDemos: ['demo5'], stats: null },

    // 고대 등급 장비 (드랍으로만 획득, danger 15+)
    { itemId: 'ancient_blade', name: '고대의 검', desc: 'ATK+80, 출혈+20%, 흡혈10%', category: 'equipment', slot: 'weapon', baseValue: 500, weight: 5, rarity: 'ancient', icon: '⚔️', sourceDemos: ['demo5'], stats: { atk: 80, bleedChance: 0.20, lifesteal: 0.10 } },
    { itemId: 'ancient_plate', name: '고대의 판금', desc: 'DEF+30, HP+200, 가시10', category: 'equipment', slot: 'armor', baseValue: 450, weight: 6, rarity: 'ancient', icon: '🛡️', sourceDemos: ['demo5'], stats: { def: 30, hp: 200, thorns: 10 } },
    { itemId: 'ancient_sigil', name: '고대의 문양', desc: 'ATK+25, CRIT+15%, 치명뎀+40%', category: 'equipment', slot: 'accessory', baseValue: 400, weight: 1, rarity: 'ancient', icon: '💍', sourceDemos: ['demo5'], stats: { atk: 25, critRate: 0.15, critDmg: 0.40 } },
    { itemId: 'ancient_relic', name: '고대의 유물', desc: '태고의 힘이 깃든 유물', category: 'material', slot: null, baseValue: 200, weight: 2, rarity: 'ancient', icon: '🏺', sourceDemos: ['demo5'], stats: null },

    // 신화 등급 장비 (조합으로만 생성: 전설+전설 또는 고대+고대)
    { itemId: 'mythical_destroyer', name: '신화: 멸절자', desc: 'ATK+120, 출혈+25%, 흡혈15%, CRIT+20%', category: 'equipment', slot: 'weapon', baseValue: 1000, weight: 5, rarity: 'mythical', icon: '⚔️', sourceDemos: ['demo5'], stats: { atk: 120, bleedChance: 0.25, lifesteal: 0.15, critRate: 0.20 } },
    { itemId: 'mythical_aegis', name: '신화: 이지스', desc: 'DEF+45, HP+350, 가시20, 흡혈5%', category: 'equipment', slot: 'armor', baseValue: 900, weight: 6, rarity: 'mythical', icon: '🛡️', sourceDemos: ['demo5'], stats: { def: 45, hp: 350, thorns: 20, lifesteal: 0.05 } },
    { itemId: 'mythical_crown', name: '신화: 왕관', desc: 'ATK+40, CRIT+25%, 치명뎀+60%, 회피+10%', category: 'equipment', slot: 'accessory', baseValue: 800, weight: 1, rarity: 'mythical', icon: '👑', sourceDemos: ['demo5'], stats: { atk: 40, critRate: 0.25, critDmg: 0.60, dodgeRate: 0.10 } },

    // 전설 등급 방어구/악세 (기존에 없던 것 추가)
    { itemId: 'blood_fortress', name: '혈철 요새', desc: 'DEF+28, HP+180, 가시12, 흡혈5%', category: 'equipment', slot: 'armor', baseValue: 250, weight: 6, rarity: 'legendary', icon: '🛡️', sourceDemos: ['demo5'], stats: { def: 28, hp: 180, thorns: 12, lifesteal: 0.05 } },
    { itemId: 'pitlord_crown', name: '핏로드의 왕관', desc: 'ATK+20, CRIT+15%, 치명뎀+40%, 회피+8%', category: 'equipment', slot: 'accessory', baseValue: 220, weight: 1, rarity: 'legendary', icon: '👑', sourceDemos: ['demo5'], stats: { atk: 20, critRate: 0.15, critDmg: 0.40, dodgeRate: 0.08 } },

    // 금고 키카드 (보물 금고 입장용)
    { itemId: 'vault_key_uncommon', name: '녹슨 키카드', desc: '보물 금고 입장 (하급)', category: 'keycard', slot: null, baseValue: 30, weight: 1, rarity: 'uncommon', icon: '🔑', sourceDemos: ['demo5'], stats: null },
    { itemId: 'vault_key_rare', name: '은빛 키카드', desc: '보물 금고 입장 (중급)', category: 'keycard', slot: null, baseValue: 60, weight: 1, rarity: 'rare', icon: '🗝️', sourceDemos: ['demo5'], stats: null },
    { itemId: 'vault_key_epic', name: '황금 키카드', desc: '보물 금고 입장 (상급)', category: 'keycard', slot: null, baseValue: 120, weight: 1, rarity: 'epic', icon: '🗝️', sourceDemos: ['demo5'], stats: null },
    { itemId: 'vault_key_legendary', name: '혈룡의 키카드', desc: '보물 금고 입장 (최상급)', category: 'keycard', slot: null, baseValue: 250, weight: 1, rarity: 'legendary', icon: '🗝️', sourceDemos: ['demo5'], stats: null }
]);
