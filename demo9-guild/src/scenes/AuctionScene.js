// === AuctionScene v2 ===
// 2단계 미니게임: 1단계(비공개 일회 입찰) → reveal → 2단계(가격 책정+손님 반응) → 정산.
// 동기 문서: docs/systems/auction.md
//
// MVP 범위 (현재 구현):
//   ✅ 한 사이클 (입장 → 1단계 → reveal → 2단계 → 정산 → 종료)
//   ✅ NPC 입찰 산출 (정규분포, 등급별)
//   ✅ 손님 placeholder 4명 + 표정/대사
//   ✅ 라운드 예산 + 입장료
// Backlog (다음 라운드):
//   ⬜ 위작/저주 트리거 + 감식
//   ⬜ VIP 경매장
//   ⬜ 골든 호감도 단계 보상 (감식 정확도, 손님 선호 노출)
//   ⬜ 학습형 손님 선호 노출
//   ⬜ 손님당 타이머 (현재는 무제한)

class AuctionScene extends Phaser.Scene {
    constructor() { super('AuctionScene'); }

    init(data) { this.gameState = data.gameState; this.round = null; }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);

        // 헤더
        this.add.text(640, 22, '🏛 경매장', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.goldText = this.add.text(1260, 22, `${this.gameState.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

        UIButton.create(this, 80, 22, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this._exitToTown()
        });

        this._contentObjs = [];
        this._drawLobby();
    }

    _exitToTown() {
        this.scene.start('TownScene', { gameState: this.gameState });
    }

    _refreshGold() { this.goldText.setText(`${this.gameState.gold}G`); }

    _clear() {
        if (this._contentObjs) this._contentObjs.forEach(o => o.destroy && o.destroy());
        this._contentObjs = [];
    }
    _add(o) { this._contentObjs.push(o); return o; }

    // === LOBBY: 경매 시작 전 ===
    _drawLobby() {
        this._clear();
        const gs = this.gameState;

        this._add(UIPanel.create(this, 240, 130, 800, 460, { title: '경매장 입장' }));

        this._add(this.add.text(640, 200, '비공개 입찰 → 가격 책정 2단계 미니게임', {
            fontSize: '14px', fontFamily: 'monospace', color: '#aaaacc'
        }).setOrigin(0.5));

        // 입장료 미리보기
        const feeRange = BALANCE.AUCTION.ENTRY_FEE;
        const budgetPreview = Math.min(
            Math.floor(gs.gold * BALANCE.AUCTION.ROUND_BUDGET_PCT),
            BALANCE.AUCTION.ROUND_BUDGET_MAX
        );

        const infoLines = [
            `입장료: ${feeRange.min}~${feeRange.max}G`,
            `이번 라운드 예산: ${budgetPreview}G  (보유 골드의 ${Math.round(BALANCE.AUCTION.ROUND_BUDGET_PCT*100)}%)`,
            `매물 수: ${BALANCE.AUCTION.ITEM_COUNT}장`,
            '',
            '1단계: 매물에 비공개로 한 번씩 입찰 → 동시 공개',
            '2단계: 낙찰 매물을 손님에게 가격 책정 판매'
        ];
        infoLines.forEach((line, i) => {
            this._add(this.add.text(640, 240 + i * 22, line, {
                fontSize: '12px', fontFamily: 'monospace', color: i >= 4 ? '#888899' : '#cccccc'
            }).setOrigin(0.5));
        });

        const canEnter = gs.gold >= feeRange.min && budgetPreview >= 10;

        this._add(UIButton.create(this, 640, 470, 220, 44, '경매 시작 →', {
            color: canEnter ? 0x664422 : 0x333333,
            hoverColor: canEnter ? 0x886644 : 0x333333,
            textColor: canEnter ? '#ffcc88' : '#555555',
            fontSize: 16,
            onClick: () => {
                if (!canEnter) { UIToast.show(this, '골드 부족 (입장료 + 예산 필요)', { color: '#ff6666' }); return; }
                this._beginRound();
            }
        }));

        this._add(this.add.text(640, 530, '※ 평균적으로 손해보는 도박 구조. 운/감으로 이득 노리세요.', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666677'
        }).setOrigin(0.5));
    }

    // === ROUND START ===
    _beginRound() {
        const gs = this.gameState;
        this.round = AuctionRound.newRound(gs);

        // 입장료 차감
        GuildManager.spendGold(gs, this.round.entryFee);
        this._refreshGold();
        UIToast.show(this, `입장료 -${this.round.entryFee}G`, { color: '#ffaa44' });

        this._drawBidding();
    }

    // === PHASE 1: BIDDING ===
    _drawBidding() {
        this._clear();
        const round = this.round;

        this.add.text && null; // placeholder

        // 헤더 패널
        this._add(UIPanel.create(this, 30, 60, 1220, 50, {}));
        this._add(this.add.text(50, 75, '1단계 — 비공개 입찰', {
            fontSize: '15px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }));
        this._add(this.add.text(50, 95, '각 매물에 입찰액을 정하고 [동시 공개]를 누르세요. NPC 입찰자와 비공개로 경쟁합니다.', {
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        }));

        const budgetUsed = this._budgetUsed();
        const budgetLeft = round.budget - budgetUsed;
        this._add(this.add.text(1230, 75, `예산: ${budgetUsed}/${round.budget}G`, {
            fontSize: '13px', fontFamily: 'monospace', color: budgetLeft >= 0 ? '#88ccaa' : '#ff6666', fontStyle: 'bold'
        }).setOrigin(1, 0));
        this._add(this.add.text(1230, 95, `남은: ${budgetLeft}G`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#666677'
        }).setOrigin(1, 0));

        // 매물 3장 (가로 배치)
        const cardW = 380, cardH = 380, gap = 20;
        const totalW = round.items.length * cardW + (round.items.length - 1) * gap;
        let cx = (1280 - totalW) / 2;
        round.items.forEach((item, idx) => {
            this._drawBidCard(item, cx, 140, cardW, cardH);
            cx += cardW + gap;
        });

        // 하단 버튼
        const allInBudget = budgetUsed <= round.budget;
        const hasAnyBid = Object.values(round.playerBids).some(v => v > 0);

        this._add(UIButton.create(this, 640, 670, 280, 44, allInBudget && hasAnyBid ? '동시 공개 →' : (hasAnyBid ? '예산 초과!' : '입찰 0건 (그냥 진행)'), {
            color: allInBudget ? 0x664422 : 0x664422,
            hoverColor: allInBudget ? 0x886644 : 0x886644,
            textColor: allInBudget ? '#ffcc88' : '#ff8866',
            fontSize: 14,
            onClick: () => {
                if (!allInBudget) { UIToast.show(this, '예산 초과! 입찰액을 줄이세요', { color: '#ff6666' }); return; }
                this._resolveBidding();
            }
        }));
    }

    _budgetUsed() {
        return Object.values(this.round.playerBids).reduce((a, b) => a + (b || 0), 0);
    }

    _drawBidCard(item, x, y, w, h) {
        const round = this.round;
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const curBid = round.playerBids[item.id] || 0;

        // 배경
        const bg = this._add(this.add.graphics());
        bg.fillStyle(0x1a1a2e, 1);
        bg.fillRoundedRect(x, y, w, h, 8);
        bg.lineStyle(2, rarity.color, 0.5);
        bg.strokeRoundedRect(x, y, w, h, 8);

        // 아이콘 + 이름
        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        this._add(this.add.text(x + 16, y + 16, typeIcons[item.type] || '?', { fontSize: '24px' }));
        this._add(this.add.text(x + 52, y + 18, item.name, {
            fontSize: '15px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));
        this._add(this.add.text(x + 52, y + 40, `[${rarity.name}]  ${item.type}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#888899'
        }));

        // 설명/스탯
        if (item.stats) {
            const statStr = Object.entries(item.stats).map(([k, v]) =>
                typeof v === 'number' && v < 1 ? `${k}+${Math.round(v * 100)}%` : `${k}+${v}`
            ).join('  ');
            this._add(this.add.text(x + 16, y + 70, statStr, {
                fontSize: '11px', fontFamily: 'monospace', color: '#aaaaa0', wordWrap: { width: w - 32 }
            }));
        }
        if (item.desc) {
            this._add(this.add.text(x + 16, y + 95, item.desc, {
                fontSize: '10px', fontFamily: 'monospace', color: '#778899', wordWrap: { width: w - 32 }
            }));
        }

        // 시세 정보 박스
        const infoY = y + 135;
        this._add(this.add.text(x + 16, infoY, '── 정보 ──', {
            fontSize: '10px', fontFamily: 'monospace', color: '#556677'
        }));
        this._add(this.add.text(x + 16, infoY + 16, `추정 시세:`, { fontSize: '11px', fontFamily: 'monospace', color: '#888899' }));
        this._add(this.add.text(x + 110, infoY + 16, `${item.marketPrice}G`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
        }));
        this._add(this.add.text(x + 16, infoY + 36, `NPC 경쟁자:`, { fontSize: '11px', fontFamily: 'monospace', color: '#888899' }));
        this._add(this.add.text(x + 110, infoY + 36, `${item.npcBidderCount}명`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ff8866'
        }));
        this._add(this.add.text(x + 16, infoY + 56, `입찰 범위:`, { fontSize: '11px', fontFamily: 'monospace', color: '#888899' }));
        this._add(this.add.text(x + 110, infoY + 56, `${item.minBid}~${item.maxBid}G`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
        }));

        // 입찰 입력 영역
        const bidY = y + h - 130;
        this._add(this.add.text(x + w/2, bidY, '내 입찰', {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaaacc'
        }).setOrigin(0.5));

        // 큰 입찰액 표시
        this._add(this.add.text(x + w/2, bidY + 25, curBid > 0 ? `${curBid}G` : '— G', {
            fontSize: '22px', fontFamily: 'monospace',
            color: curBid > 0 ? (curBid >= item.minBid ? '#44ff88' : '#ff6666') : '#555566',
            fontStyle: 'bold'
        }).setOrigin(0.5));

        // 조정 버튼들 (-50 / -10 / +10 / +50)
        const btnY = bidY + 60;
        const deltas = [-100, -10, 10, 100];
        let bx = x + 30;
        deltas.forEach(d => {
            const label = (d > 0 ? '+' : '') + d;
            this._add(UIButton.create(this, bx + 35, btnY, 70, 26, label, {
                color: 0x333344, hoverColor: 0x445566, textColor: '#cccccc', fontSize: 12,
                onClick: () => this._adjustBid(item, d)
            }));
            bx += 80;
        });

        // 빠른 입찰 (시세 0.7x / 1.0x / 1.3x)
        const quickY = btnY + 36;
        const quickPresets = [
            { label: '시세 70%', mult: 0.7 },
            { label: '시세 100%', mult: 1.0 },
            { label: '시세 130%', mult: 1.3 }
        ];
        let qx = x + 30;
        quickPresets.forEach(p => {
            this._add(UIButton.create(this, qx + 55, quickY, 110, 22, p.label, {
                color: 0x223344, hoverColor: 0x334455, textColor: '#88bbdd', fontSize: 10,
                onClick: () => this._setBid(item, Math.round(item.marketPrice * p.mult))
            }));
            qx += 116;
        });
    }

    _adjustBid(item, delta) {
        const round = this.round;
        const cur = round.playerBids[item.id] || 0;
        let v = cur + delta;
        if (v < 0) v = 0;
        if (v < item.minBid && v > 0) v = item.minBid;
        if (v > item.maxBid) v = item.maxBid;
        round.playerBids[item.id] = v;
        this._drawBidding();
    }
    _setBid(item, v) {
        if (v < item.minBid) v = item.minBid;
        if (v > item.maxBid) v = item.maxBid;
        this.round.playerBids[item.id] = v;
        this._drawBidding();
    }

    // === RESOLVE BIDDING ===
    _resolveBidding() {
        AuctionRound.resolveBids(this.round);
        // 골드 차감 (낙찰 매물의 paid)
        let totalPaid = 0;
        this.round.items.forEach(it => {
            if (it._result?.won) totalPaid += it._result.paid;
        });
        if (totalPaid > 0) {
            GuildManager.spendGold(this.gameState, totalPaid);
            this._refreshGold();
        }
        this.round.spent = totalPaid;
        this._drawReveal();
    }

    // === REVEAL ===
    _drawReveal() {
        this._clear();
        const round = this.round;

        this._add(UIPanel.create(this, 30, 60, 1220, 50, {}));
        this._add(this.add.text(50, 75, '결과 공개', {
            fontSize: '15px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }));
        const winCnt = round.items.filter(i => i._result?.won).length;
        this._add(this.add.text(50, 95, `${winCnt}/${round.items.length} 낙찰, 총 지출 ${round.spent}G`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        }));

        const cardW = 380, cardH = 320, gap = 20;
        const totalW = round.items.length * cardW + (round.items.length - 1) * gap;
        let cx = (1280 - totalW) / 2;
        round.items.forEach(item => {
            this._drawRevealCard(item, cx, 140, cardW, cardH);
            cx += cardW + gap;
        });

        const hasWon = winCnt > 0;
        this._add(UIButton.create(this, 640, 670, 280, 44, hasWon ? '손님 입장 →' : '정산 → (낙찰 없음)', {
            color: 0x664422, hoverColor: 0x886644, textColor: '#ffcc88', fontSize: 14,
            onClick: () => {
                if (hasWon) {
                    const customers = AuctionRound.pickCustomers(this.gameState);
                    AuctionRound.startSelling(this.round, customers);
                    this._drawSelling();
                } else {
                    AuctionRound.settle(this.round);
                    this._drawSettle();
                }
            }
        }));
    }

    _drawRevealCard(item, x, y, w, h) {
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const res = item._result || {};
        const won = res.won;

        const bg = this._add(this.add.graphics());
        bg.fillStyle(won ? 0x1a2a1a : 0x2a1a1a, 1);
        bg.fillRoundedRect(x, y, w, h, 8);
        bg.lineStyle(2, won ? 0x44ff88 : 0x886666, 0.6);
        bg.strokeRoundedRect(x, y, w, h, 8);

        this._add(this.add.text(x + 16, y + 16, item.name, {
            fontSize: '14px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));
        this._add(this.add.text(x + 16, y + 36, `[${rarity.name}]`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#888899'
        }));

        this._add(this.add.text(x + 16, y + 70, '내 입찰:', { fontSize: '11px', fontFamily: 'monospace', color: '#888899' }));
        this._add(this.add.text(x + 110, y + 70, `${this.round.playerBids[item.id] || 0}G`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#aaccff', fontStyle: 'bold'
        }));

        this._add(this.add.text(x + 16, y + 92, 'NPC 최고:', { fontSize: '11px', fontFamily: 'monospace', color: '#888899' }));
        this._add(this.add.text(x + 110, y + 92, `${res.maxNpc || 0}G`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#ff8866', fontStyle: 'bold'
        }));

        this._add(this.add.text(x + 16, y + 114, '시세:', { fontSize: '11px', fontFamily: 'monospace', color: '#888899' }));
        this._add(this.add.text(x + 110, y + 114, `${item.marketPrice}G`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaaaaa'
        }));

        // NPC 입찰 분포
        if (item.npcBids && item.npcBids.length) {
            this._add(this.add.text(x + 16, y + 146, `NPC 입찰가: ${item.npcBids.join(', ')}G`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#666677'
            }));
        } else {
            this._add(this.add.text(x + 16, y + 146, `NPC 입찰자 없음`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#666677'
            }));
        }

        // 결과 라벨
        const labelY = y + 210;
        if (won) {
            this._add(this.add.text(x + w/2, labelY, '✓ 낙찰!', {
                fontSize: '26px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
            }).setOrigin(0.5));
            this._add(this.add.text(x + w/2, labelY + 32, `−${res.paid}G`, {
                fontSize: '13px', fontFamily: 'monospace', color: '#aaccaa'
            }).setOrigin(0.5));
        } else {
            const reason = (this.round.playerBids[item.id] || 0) === 0 ? '미입찰' : '패찰';
            this._add(this.add.text(x + w/2, labelY, reason === '미입찰' ? '—' : '✗ 패찰', {
                fontSize: '24px', fontFamily: 'monospace', color: reason === '미입찰' ? '#666677' : '#ff6666', fontStyle: 'bold'
            }).setOrigin(0.5));
        }
    }

    // === PHASE 2: SELLING ===
    _drawSelling() {
        this._clear();
        const round = this.round;
        const customer = round.customers[round.customerIdx];
        const wonItems = round.wonItems.filter(it => !it._sold && !it._giveUp);

        // 모든 손님 다 끝났거나 매물 다 처분
        if (!customer || wonItems.length === 0) {
            // 남은 매물은 unsold로
            round.wonItems.forEach(it => { if (!it._sold && !it._giveUp) round.unsold.push(it); });
            AuctionRound.settle(round);
            this._drawSettle();
            return;
        }

        // 헤더
        this._add(UIPanel.create(this, 30, 60, 1220, 50, {}));
        this._add(this.add.text(50, 75, `2단계 — 손님 판매  (${round.customerIdx + 1}/${round.customers.length})`, {
            fontSize: '15px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }));
        this._add(this.add.text(50, 95, `남은 매물 ${wonItems.length}장. 손님에게 매물을 제시하고 가격을 정하세요.`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        }));

        // 손님 패널 (좌측 큰 카드)
        this._drawCustomerCard(customer, 50, 140, 380, 480);

        // 매물 그리드 (우측)
        this._drawSellingItemsGrid(wonItems, 460, 140, 780, 480);

        // 하단: "이 손님 그만" 버튼
        this._add(UIButton.create(this, 640, 670, 240, 36, '이 손님 그만 →', {
            color: 0x443344, hoverColor: 0x554455, textColor: '#ccaacc', fontSize: 12,
            onClick: () => {
                round.customerIdx++;
                round.selectedItemId = null;
                this._drawSelling();
            }
        }));
    }

    _drawCustomerCard(customer, x, y, w, h) {
        const bg = this._add(this.add.graphics());
        bg.fillStyle(0x1a1a2e, 1);
        bg.fillRoundedRect(x, y, w, h, 8);
        bg.lineStyle(2, 0x4488aa, 0.5);
        bg.strokeRoundedRect(x, y, w, h, 8);

        // 큰 이모지
        this._add(this.add.text(x + w/2, y + 70, customer.emoji, {
            fontSize: '64px'
        }).setOrigin(0.5));

        this._add(this.add.text(x + w/2, y + 150, customer.name, {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5));

        // 선호 정보 (placeholder — 학습형은 backlog. 현재는 항상 공개)
        const catLabels = { equipment: '장비', material: '소재', consumable: '소비' };
        const catStr = customer.prefCategories.map(c => catLabels[c] || c).join(', ');
        const rarLabels = { common:'일반', uncommon:'고급', rare:'희귀', epic:'에픽', legendary:'전설' };
        const rarStr = customer.prefRarities.map(r => rarLabels[r] || r).join('·');

        this._add(this.add.text(x + 16, y + 200, '선호 카테고리', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666677'
        }));
        this._add(this.add.text(x + 16, y + 215, catStr, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaccff'
        }));

        this._add(this.add.text(x + 16, y + 245, '선호 등급', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666677'
        }));
        this._add(this.add.text(x + 16, y + 260, rarStr, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaccff'
        }));

        const tendency = customer.priceMult >= 1.10 ? '후함 (비싸도 사줌)' :
                         customer.priceMult <= 0.95 ? '짬 (싸게 사려 함)' : '평범';
        this._add(this.add.text(x + 16, y + 290, '가격 성향', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666677'
        }));
        this._add(this.add.text(x + 16, y + 305, tendency, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaccff'
        }));

        // 최근 반응 (있으면)
        if (this._lastReaction) {
            const colors = { angry: '#ff6666', meh: '#ffaa66', happy: '#88ccaa', elated: '#44ff88' };
            const emojis = { angry: '😠', meh: '😐', happy: '🙂', elated: '😍' };
            this._add(this.add.text(x + w/2, y + 360, emojis[this._lastReaction.reaction], {
                fontSize: '38px'
            }).setOrigin(0.5));
            this._add(this.add.text(x + w/2, y + 410, `"${this._lastReaction.dialogue}"`, {
                fontSize: '13px', fontFamily: 'monospace', color: colors[this._lastReaction.reaction], fontStyle: 'italic',
                wordWrap: { width: w - 32 }, align: 'center'
            }).setOrigin(0.5));
        } else {
            this._add(this.add.text(x + w/2, y + 390, '매물을 골라 가격을 제시하세요', {
                fontSize: '11px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5));
        }
    }

    _drawSellingItemsGrid(wonItems, x, y, w, h) {
        const round = this.round;

        // 매물 카드 그리드 (2열)
        const cardW = (w - 30) / 2;
        const cardH = 110;
        wonItems.forEach((item, idx) => {
            const col = idx % 2, row = Math.floor(idx / 2);
            if (row > 1) return;  // 최대 4장 표시
            const ix = x + col * (cardW + 10);
            const iy = y + row * (cardH + 10);
            this._drawSellingItemCard(item, ix, iy, cardW, cardH);
        });

        // 선택된 매물 가격 입력 영역
        const inputY = y + 240;
        const selItem = round.selectedItemId ? wonItems.find(i => i.id === round.selectedItemId) : null;
        if (!selItem) {
            this._add(this.add.text(x + w/2, inputY + 100, '↑ 위에서 매물을 선택하세요', {
                fontSize: '13px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5));
            return;
        }

        // 선택된 매물 정보 + 가격 입력
        const bg = this._add(this.add.graphics());
        bg.fillStyle(0x2a2a3a, 1);
        bg.fillRoundedRect(x, inputY, w, h - 240, 8);

        const rarity = ITEM_RARITY[selItem.rarity] || ITEM_RARITY.common;
        this._add(this.add.text(x + 16, inputY + 12, `선택: ${selItem.name}`, {
            fontSize: '13px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));
        this._add(this.add.text(x + 16, inputY + 34, `시세 ${selItem.marketPrice}G  /  내가 산 가격 ${selItem._result.paid}G`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#888899'
        }));

        // 가격 입력
        if (!selItem._proposedPrice) selItem._proposedPrice = selItem.marketPrice;
        const price = selItem._proposedPrice;

        this._add(this.add.text(x + w/2, inputY + 70, `제시가: ${price}G`, {
            fontSize: '22px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
        }).setOrigin(0.5));

        const profit = price - selItem._result.paid;
        const profitColor = profit > 0 ? '#44ff88' : profit < 0 ? '#ff6666' : '#aaaaaa';
        this._add(this.add.text(x + w/2, inputY + 100, `예상 손익: ${profit >= 0 ? '+' : ''}${profit}G`, {
            fontSize: '11px', fontFamily: 'monospace', color: profitColor
        }).setOrigin(0.5));

        // 조정 버튼
        const deltas = [-100, -10, 10, 100];
        let bx = x + (w - 4 * 75 - 3 * 8) / 2;
        deltas.forEach(d => {
            const label = (d > 0 ? '+' : '') + d;
            this._add(UIButton.create(this, bx + 37, inputY + 140, 75, 26, label, {
                color: 0x333344, hoverColor: 0x445566, textColor: '#cccccc', fontSize: 12,
                onClick: () => {
                    selItem._proposedPrice = Math.max(1, selItem._proposedPrice + d);
                    this._drawSelling();
                }
            }));
            bx += 83;
        });

        // 제안 버튼
        this._add(UIButton.create(this, x + w/2, inputY + 178, 200, 32, '제시 →', {
            color: 0x446644, hoverColor: 0x558855, textColor: '#44ff88', fontSize: 14,
            onClick: () => this._evaluateSell(selItem)
        }));
    }

    _drawSellingItemCard(item, x, y, w, h) {
        const round = this.round;
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const selected = round.selectedItemId === item.id;

        const bg = this._add(this.add.graphics());
        bg.fillStyle(selected ? 0x2a3a3a : 0x1a1a2e, 1);
        bg.fillRoundedRect(x, y, w, h, 6);
        bg.lineStyle(selected ? 2 : 1, selected ? 0x44ffcc : rarity.color, selected ? 0.8 : 0.4);
        bg.strokeRoundedRect(x, y, w, h, 6);

        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        this._add(this.add.text(x + 10, y + 10, typeIcons[item.type] || '?', { fontSize: '16px' }));
        this._add(this.add.text(x + 38, y + 12, item.name, {
            fontSize: '12px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));
        this._add(this.add.text(x + 38, y + 30, `[${rarity.name}]`, {
            fontSize: '9px', fontFamily: 'monospace', color: '#888899'
        }));
        this._add(this.add.text(x + 10, y + 55, `시세 ${item.marketPrice}G  /  매입 ${item._result.paid}G`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#aaaaaa'
        }));

        if (item._lastReaction) {
            const emojis = { angry: '😠', meh: '😐', happy: '🙂', elated: '😍' };
            this._add(this.add.text(x + w - 28, y + 10, emojis[item._lastReaction], { fontSize: '16px' }));
        }

        // 클릭 영역
        const hit = this._add(this.add.rectangle(x + w/2, y + h/2, w, h, 0x000000, 0));
        hit.setInteractive({ useHandCursor: true });
        hit.on('pointerup', () => {
            round.selectedItemId = item.id;
            this._drawSelling();
        });
    }

    // === 제안 평가 ===
    _evaluateSell(item) {
        const round = this.round;
        const customer = round.customers[round.customerIdx];
        const price = item._proposedPrice;

        const res = AuctionRound.evaluateOffer(customer, item, price, item.marketPrice);
        this._lastReaction = res;
        item._lastReaction = res.reaction;

        // 만족이면 매각
        if (res.reaction === 'happy' || res.reaction === 'elated') {
            item._sold = true;
            item._soldPrice = price;
            GuildManager.addGold(this.gameState, price);
            this._refreshGold();
            UIToast.show(this, `${item.name} 판매! +${price}G`, { color: '#44ff88' });
            // 다음 손님으로 자동 이동? — MVP는 같은 손님 유지, 사용자가 다른 매물 시도 가능
            round.selectedItemId = null;
        } else {
            // 거절 — 가격 조정 횟수 차감 (MVP는 단순 카운트만)
            item._rejectCount = (item._rejectCount || 0) + 1;
            if (item._rejectCount >= BALANCE.AUCTION.CUSTOMER_PRICE_ADJUST_TRIES) {
                // 이 손님은 포기, 다음 손님으로 강제 이동
                UIToast.show(this, `${customer.name}이(가) 떠납니다`, { color: '#ff6666' });
                round.customerIdx++;
                round.selectedItemId = null;
                this._lastReaction = null;
                // 매물별 reject 카운트 초기화 (다른 손님에게 다시 시도)
                round.wonItems.forEach(it => { it._rejectCount = 0; it._lastReaction = null; });
            }
        }
        this._drawSelling();
    }

    // === SETTLE ===
    _drawSettle() {
        this._clear();
        const round = this.round;
        const gs = this.gameState;

        this._add(UIPanel.create(this, 240, 130, 800, 460, { title: '경매 정산' }));

        const earned = round.wonItems.reduce((s, it) => s + (it._soldPrice || 0), 0);
        const spent = round.entryFee + round.spent;
        const net = earned - spent;

        const lines = [
            `입장료: -${round.entryFee}G`,
            `1단계 입찰 지출: -${round.spent}G`,
            `2단계 판매 수익: +${earned}G`,
            '',
            `사이클 순손익: ${net >= 0 ? '+' : ''}${net}G`
        ];
        lines.forEach((line, i) => {
            const color = i === 4 ? (net >= 0 ? '#44ff88' : '#ff6666') :
                          i === 0 || i === 1 ? '#ff8866' :
                          i === 2 ? '#88ccaa' : '#888899';
            const size = i === 4 ? '18px' : '13px';
            this._add(this.add.text(640, 200 + i * 28, line, {
                fontSize: size, fontFamily: 'monospace', color, fontStyle: i === 4 ? 'bold' : 'normal'
            }).setOrigin(0.5));
        });

        // 안 팔린 매물 → 보관함
        const unsold = round.wonItems.filter(it => !it._sold);
        if (unsold.length > 0) {
            const cap = GuildManager.getStorageCapacity(gs);
            const addable = Math.max(0, cap - gs.storage.length);
            const moved = unsold.slice(0, addable);
            const dropped = unsold.slice(addable);

            moved.forEach(it => {
                const copy = { ...it };
                delete copy.marketPrice; delete copy.npcBids; delete copy.npcBidderCount;
                delete copy.minBid; delete copy.maxBid; delete copy._result;
                delete copy._proposedPrice; delete copy._rejectCount;
                delete copy._lastReaction; delete copy._sold; delete copy._soldPrice;
                delete copy._giveUp;
                StorageManager.addItem(gs, copy);
            });

            const msg = dropped.length > 0
                ? `안 팔린 ${moved.length}개 → 보관함 (보관함 부족: ${dropped.length}개 분실)`
                : `안 팔린 ${unsold.length}개 → 보관함`;
            this._add(this.add.text(640, 380, msg, {
                fontSize: '11px', fontFamily: 'monospace', color: dropped.length > 0 ? '#ff8866' : '#888899'
            }).setOrigin(0.5));
        }

        // 거래 기록
        gs.auctionHistory = gs.auctionHistory || [];
        gs.auctionHistory.unshift({
            action: 'cycle',
            entryFee: round.entryFee,
            spent: round.spent,
            earned,
            net,
            time: Date.now()
        });
        if (gs.auctionHistory.length > 20) gs.auctionHistory.length = 20;

        // NPC 호감도 (골든) 누적
        if (!gs.npcFavor) gs.npcFavor = {};
        gs.npcFavor.golden = (gs.npcFavor.golden || 0) + 0.5;
        const soldCnt = round.wonItems.filter(it => it._sold).length;
        if (soldCnt > 0) gs.npcFavor.golden += soldCnt * 1.0;

        SaveManager.save(gs);
        GuildManager.addMessage(gs, `경매장 사이클: ${net >= 0 ? '+' : ''}${net}G (지출 ${spent}, 수익 ${earned})`);

        // 버튼
        this._add(UIButton.create(this, 520, 510, 200, 40, '다시 입장', {
            color: 0x664422, hoverColor: 0x886644, textColor: '#ffcc88', fontSize: 14,
            onClick: () => { this.round = null; this._drawLobby(); }
        }));
        this._add(UIButton.create(this, 760, 510, 200, 40, '마을로 →', {
            color: 0x445566, hoverColor: 0x556677, textColor: '#aaccdd', fontSize: 14,
            onClick: () => this._exitToTown()
        }));
    }

    // === 호환성 stub: 구 위탁 정산 ===
    // RunResultScene이 호출. 신메커닉에 위탁 없음 — 빈 결과 반환.
    // 구 데이터(gs.consignedItems)가 남아 있어도 처리 안 함.
    static processConsignments(gs) {
        return [];
    }
}
