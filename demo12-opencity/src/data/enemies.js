// Enemy type definitions
const ENEMY_DATA = {
    infected: {
        id: 'infected',
        name: '감염자',
        icon: '🧟',
        hp: 30,
        damage: 8,
        speed: 0.6,        // tiles per second
        sightRange: 4,     // tiles
        chaseRange: 6,     // tiles (gives up chase beyond this)
        color: 0x884444,
        desc: '느리지만 끈질기게 쫓아온다'
    },
    runner: {
        id: 'runner',
        name: '질주자',
        icon: '💀',
        hp: 20,
        damage: 12,
        speed: 1.2,
        sightRange: 5,
        chaseRange: 8,
        color: 0xaa4444,
        desc: '빠르고 위험하다. 마주치면 도망쳐라'
    },
    brute: {
        id: 'brute',
        name: '거수',
        icon: '👹',
        hp: 60,
        damage: 15,
        speed: 0.4,
        sightRange: 3,
        chaseRange: 5,
        color: 0x664444,
        desc: '느리지만 한 대가 아프다'
    },
};

// Night spawn configuration
const NIGHT_SPAWNS = {
    // [enemyType, count, minDistFromPlayer]
    wave1: [
        { type: 'infected', count: 3, minDist: 8 },
    ],
    wave2: [
        { type: 'infected', count: 2, minDist: 7 },
        { type: 'runner', count: 1, minDist: 10 },
    ],
    wave3: [
        { type: 'infected', count: 2, minDist: 6 },
        { type: 'runner', count: 1, minDist: 8 },
        { type: 'brute', count: 1, minDist: 10 },
    ],
};
