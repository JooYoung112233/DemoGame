const RECIPE_DATA = {
    health_potion: {
        name: '체력 포션',
        result: 'health_potion',
        ingredients: [
            { id: 'heal_herb', count: 2 },
        ],
        unlockDay: 1,
    },
    antidote: {
        name: '해독제',
        result: 'antidote',
        ingredients: [
            { id: 'poison_herb', count: 1 },
            { id: 'heal_herb', count: 1 },
        ],
        unlockDay: 3,
    },
    iron_dagger: {
        name: '철 단검',
        result: 'iron_dagger',
        ingredients: [
            { id: 'iron_ore', count: 2 },
            { id: 'wood', count: 1 },
        ],
        unlockDay: 7,
    },
    silver_ring: {
        name: '은반지',
        result: 'silver_ring',
        ingredients: [
            { id: 'silver_ore', count: 2 },
        ],
        unlockDay: 10,
    },
};
