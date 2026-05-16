// === 경매장 라운드 시스템 (v2) ===
// 한 사이클의 상태 머신 + 산출 로직.
// 1단계(비공개 입찰): newRound() → 매물 3장 + NPC 입찰액 산출
// 2단계(가격 책정): pickCustomers() → 손님 N명, evaluateOffer() → 표정/대사
//
// 동기: docs/systems/auction.md §4 §5

const AuctionRound = (function () {
    const B = () => BALANCE.AUCTION;

    // === 헬퍼 ===
    function gaussianRandom(mean, stddev) {
        // Box-Muller
        const u1 = Math.random() || 1e-9;
        const u2 = Math.random();
        const z = Math.sqrt(-2 * Math.log(u1)) * Math.cos(2 * Math.PI * u2);
        return mean + z * stddev;
    }
    function clamp(v, lo, hi) { return Math.max(lo, Math.min(hi, v)); }
    function randInt(lo, hi) { return Math.floor(lo + Math.random() * (hi - lo + 1)); }
    function pickN(arr, n) {
        const copy = arr.slice();
        const out = [];
        while (out.length < n && copy.length) {
            out.push(copy.splice(Math.floor(Math.random() * copy.length), 1)[0]);
        }
        return out;
    }

    // === 시세 ===
    function getMarketPrice(gs, item) {
        const mod = (typeof getMarketPriceModifier === 'function')
            ? getMarketPriceModifier(gs, item.type) : 1.0;
        return Math.max(1, Math.round(item.value * mod));
    }

    // === NPC 입찰 ===
    // 단일 NPC의 입찰액 산출 (정규분포). 0이면 입찰 안함.
    function rollNpcBid(item, marketPrice) {
        if (Math.random() < B().NPC_NO_BID_CHANCE) return 0;
        const meanMult = B().NPC_BID_MEAN_MULT[item.rarity] || 1.0;
        const stdPct = B().NPC_BID_STDDEV_PCT[item.rarity] || 0.2;
        const mean = marketPrice * meanMult;
        const std = marketPrice * stdPct;
        const bid = Math.max(0, Math.round(gaussianRandom(mean, std)));
        return bid;
    }

    // 매물 1개에 대한 NPC 입찰자 풀 산출
    // 입찰자 수는 등급별 평균값 기준 ±1 변동 (1.0=1명, 1.2=20%로 2명, 3.0=항상 3명)
    function rollNpcBiddersFor(item, marketPrice) {
        const avg = B().NPC_BIDDER_COUNT_BY_RARITY[item.rarity] || 1.0;
        let n;
        if (avg <= 1.0) n = Math.random() < avg ? 1 : 0;
        else if (avg >= 3.0) n = 3;
        else {
            const base = Math.floor(avg);
            const frac = avg - base;
            n = base + (Math.random() < frac ? 1 : 0);
        }
        n = clamp(n, 0, 3);
        const bids = [];
        for (let i = 0; i < n; i++) {
            const b = rollNpcBid(item, marketPrice);
            if (b > 0) bids.push(b);
        }
        return bids; // 빈 배열 가능
    }

    // === 매물 풀 ===
    function generateItemForAuction(gs) {
        // 길드 레벨에 따라 희귀도 부스트
        const boost = Math.random() < 0.25 ? 1 : 0;
        const item = generateItem('common', gs.guildLevel, boost);
        // id 보장
        if (!item.id) item.id = Date.now() + Math.floor(Math.random() * 1e6);
        return item;
    }

    // === 손님 선택 ===
    function pickCustomers(_gs) {
        const min = B().CUSTOMER_PER_ROUND.min;
        const max = B().CUSTOMER_PER_ROUND.max;
        const n = randInt(min, max);
        return pickN(AUCTION_CUSTOMERS, Math.min(n, AUCTION_CUSTOMERS.length));
    }

    // === 손님 만족가 산출 ===
    // basePrice: marketPrice
    // priceMult: 캐릭터 기본 후함도
    // categoryMatch: ×1.15
    // rarityMatch: ×1.10
    // 그 위에 무작위 ±5% (CUSTOMER_SATISFY_MULT_RANGE 0.85~0.95)
    function computeSatisfyPrice(customer, item, marketPrice) {
        let p = marketPrice * customer.priceMult;
        if (customer.prefCategories.includes(item.type)) p *= 1.15;
        if (customer.prefRarities.includes(item.rarity)) p *= 1.10;
        const r = B().CUSTOMER_SATISFY_MULT_RANGE;
        const noise = r.min + Math.random() * (r.max - r.min);
        return Math.max(1, Math.round(p * noise));
    }

    // === 손님 반응 ===
    // ratio = playerPrice / satisfyPrice
    // <=0.9: elated, <=1.0: happy, <=1.15: meh, >1.15: angry
    function classifyReaction(playerPrice, satisfyPrice) {
        const r = playerPrice / Math.max(1, satisfyPrice);
        if (r <= 0.9) return 'elated';
        if (r <= 1.0) return 'happy';
        if (r <= 1.15) return 'meh';
        return 'angry';
    }

    function pickDialogue(reaction) {
        const pool = AUCTION_DIALOGUE[reaction] || [];
        if (!pool.length) return '';
        return pool[Math.floor(Math.random() * pool.length)];
    }

    function evaluateOffer(customer, item, playerPrice, marketPrice) {
        const satisfy = computeSatisfyPrice(customer, item, marketPrice);
        const reaction = classifyReaction(playerPrice, satisfy);
        return {
            reaction,
            dialogue: pickDialogue(reaction),
            satisfyPrice: satisfy,
            ratio: playerPrice / satisfy,
            // 호감도 60+에서 시세 힌트 노출
            priceHint: AUCTION_PRICE_HINT[reaction] || ''
        };
    }

    // === 1단계 라운드 생성 ===
    function newRound(gs) {
        // 입장료
        const feeRange = B().ENTRY_FEE;
        const entryFee = Math.round(feeRange.min + Math.random() * (feeRange.max - feeRange.min));

        // 예산 (보유 골드의 X%, 상한 적용)
        const rawBudget = Math.floor(gs.gold * B().ROUND_BUDGET_PCT);
        const budget = Math.min(rawBudget, B().ROUND_BUDGET_MAX);

        // 매물 N장
        const items = [];
        for (let i = 0; i < B().ITEM_COUNT; i++) {
            const item = generateItemForAuction(gs);
            const marketPrice = getMarketPrice(gs, item);
            const npcBids = rollNpcBiddersFor(item, marketPrice);
            items.push({
                ...item,
                marketPrice,
                npcBids,             // 동시 공개 전 비밀로 두지만, MVP에서는 사실상 계산 끝남
                npcBidderCount: npcBids.length,
                minBid: Math.max(1, Math.round(marketPrice * B().PLAYER_BID_MIN_MULT)),
                maxBid: Math.round(marketPrice * B().PLAYER_BID_MAX_MULT)
            });
        }

        return {
            phase: 'bidding',        // bidding → reveal → selling → settle
            entryFee,
            budget,
            spent: 0,                // 입찰 합계
            items,                   // 매물 + npcBids + 가격대
            playerBids: {},          // { itemId: amount }
            wonItems: [],            // 1단계 후 낙찰된 매물
            customers: [],           // 2단계 손님
            customerIdx: 0,
            sold: [],                // 2단계 결과
            unsold: [],
            startedAt: Date.now()
        };
    }

    // === 1단계 결과 처리 ===
    function resolveBids(round) {
        round.wonItems = [];
        for (const item of round.items) {
            const playerBid = round.playerBids[item.id] || 0;
            const maxNpc = item.npcBids.length ? Math.max(...item.npcBids) : 0;
            if (playerBid <= 0) {
                item._result = { won: false, reason: 'no_bid' };
                continue;
            }
            // 동률 시 NPC 우선 (TIE_GOES_TO_NPC)
            const wins = B().TIE_GOES_TO_NPC ? playerBid > maxNpc : playerBid >= maxNpc;
            if (wins) {
                item._result = { won: true, paid: playerBid, maxNpc };
                round.wonItems.push(item);
            } else {
                item._result = { won: false, paid: 0, maxNpc, playerBid };
            }
        }
        round.phase = 'reveal';
        return round;
    }

    function startSelling(round, customers) {
        round.customers = customers;
        round.customerIdx = 0;
        round.phase = 'selling';
        return round;
    }

    function settle(round) {
        round.phase = 'settle';
        round.endedAt = Date.now();
        return round;
    }

    return {
        // 매물/시세
        getMarketPrice,
        generateItemForAuction,
        // NPC
        rollNpcBid,
        rollNpcBiddersFor,
        // 라운드 생성/진행
        newRound,
        resolveBids,
        pickCustomers,
        startSelling,
        settle,
        // 손님 평가
        computeSatisfyPrice,
        classifyReaction,
        evaluateOffer,
        pickDialogue,
        // 헬퍼 (테스트용)
        _gaussianRandom: gaussianRandom
    };
})();

if (typeof module !== 'undefined' && module.exports) {
    module.exports = AuctionRound;
}
