const NAME_PREFIX = [
    '용감한', '그림자', '붉은', '강철', '황금', '은빛', '폭풍', '얼음',
    '불꽃', '바람', '달빛', '별의', '검은', '푸른', '하얀', '고독한',
    '신속한', '교활한', '잔혹한', '충직한', '광기의', '고요한', '맹렬한', '지혜로운',
    '방랑하는', '저주받은', '축복받은', '잊혀진', '전설의', '고대의'
];

const NAME_SUFFIX = [
    '칼날', '방패', '화살', '주먹', '그림자', '발걸음', '눈동자', '이빨',
    '손길', '망토', '투구', '심장', '영혼', '발톱', '뿔', '날개',
    '불꽃', '서리', '번개', '대지', '바람', '파도', '독', '가시',
    '맹세', '복수', '희망', '운명', '예언', '전쟁'
];

function generateMercName() {
    const prefix = NAME_PREFIX[Math.floor(Math.random() * NAME_PREFIX.length)];
    const suffix = NAME_SUFFIX[Math.floor(Math.random() * NAME_SUFFIX.length)];
    return `${prefix} ${suffix}`;
}
