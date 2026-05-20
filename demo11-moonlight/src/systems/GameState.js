class GameState {
    constructor() {
        this.day = 1;
        this.gold = 80;
        this.reputation = 0;
        this.inventory = [];
        this.shopSlots = 6;
        this.craftSlots = 2;
        this.totalEarned = 0;
        this.totalSold = 0;
        this.demandCategory = null;
        this.pendingCommissions = [];
        this.commissionResults = [];
        this.activeOrders = [];
        this.workers = [];
        this.workerCostPerDay = 0;

        // 날씨 & 이벤트
        this.weather = 'clear';
        this.dailyEvent = null;

        // 장인 등급
        this.artisanExp = {};
        ARTISAN_CATEGORIES.forEach(cat => { this.artisanExp[cat] = 0; });

        // 확장/투자 레벨
        this.upgradeLevels = {};
        Object.keys(UPGRADES).forEach(k => { this.upgradeLevels[k] = 0; });

        // 발견된 연구 레시피
        this.discoveredRecipes = {};

        // 모험가 성장 상태
        this.adventurerStates = {};
        ADVENTURER_DATA.forEach(adv => {
            this.adventurerStates[adv.id] = {
                exp: 0,
                level: 1,
                injuredUntil: 0,
                questProgress: 0,
                questComplete: false,
            };
        });

        this.addItems([
            { id: 'heal_herb', qty: 3 },
            { id: 'wood', qty: 2 },
            { id: 'poison_herb', qty: 1 },
        ]);
        this._rollDemand();
        this._rollWeather();
    }

    addItems(items) {
        items.forEach(item => {
            const existing = this.inventory.find(i => i.id === item.id);
            if (existing) existing.qty += (item.qty || 1);
            else this.inventory.push({ id: item.id, qty: item.qty || 1 });
        });
    }

    removeItem(itemId, count = 1) {
        const item = this.inventory.find(i => i.id === itemId);
        if (!item || item.qty < count) return false;
        item.qty -= count;
        if (item.qty <= 0) this.inventory = this.inventory.filter(i => i.qty > 0);
        return true;
    }

    getItemCount(itemId) {
        const item = this.inventory.find(i => i.id === itemId);
        return item ? item.qty : 0;
    }

    _rollDemand() {
        const idx = Math.floor((this.day - 1) / DEMAND_CYCLE_DAYS) % DEMAND_CATEGORIES.length;
        this.demandCategory = DEMAND_CATEGORIES[idx];
    }

    getDemandMultiplier(category) {
        let mult = category === this.demandCategory ? 1.5 : 1.0;

        // 날씨 수요 부스트
        const w = WEATHER_DATA[this.weather];
        if (w && w.demandBoost === category) mult *= 1.3;

        // 이벤트 가격 부스트
        if (this.dailyEvent?.effect?.priceBoost?.category === category) {
            mult *= this.dailyEvent.effect.priceBoost.mult;
        }

        return mult;
    }

    // === 날씨 시스템 ===

    _rollWeather() {
        const entries = Object.entries(WEATHER_DATA);
        const totalWeight = entries.reduce((s, [, w]) => s + w.weight, 0);
        let roll = Math.random() * totalWeight;
        for (const [id, w] of entries) {
            roll -= w.weight;
            if (roll <= 0) { this.weather = id; return; }
        }
        this.weather = 'clear';
    }

    getWeather() {
        return WEATHER_DATA[this.weather] || WEATHER_DATA.clear;
    }

    isExpeditionBlocked() {
        return !!WEATHER_DATA[this.weather]?.expeditionBlocked;
    }

    // === 일일 이벤트 ===

    _rollDailyEvent() {
        if (Math.random() > 0.35) {
            this.dailyEvent = null;
            return;
        }
        const eligible = DAILY_EVENTS.filter(e => this.day >= (e.minDay || 1));
        if (eligible.length === 0) { this.dailyEvent = null; return; }

        const totalWeight = eligible.reduce((s, e) => s + e.weight, 0);
        let roll = Math.random() * totalWeight;
        for (const evt of eligible) {
            roll -= evt.weight;
            if (roll <= 0) { this.dailyEvent = evt; return; }
        }
        this.dailyEvent = null;
    }

    applyDailyEventEffects() {
        const results = [];
        if (!this.dailyEvent) return results;
        const eff = this.dailyEvent.effect;

        // 세금
        if (eff.taxRate) {
            const tax = Math.floor(this.gold * eff.taxRate);
            this.gold -= tax;
            results.push({ text: `💰 세금 ${tax}G 납부`, color: '#ff6666' });
        }

        // 부상 치료
        if (eff.healAllAdventurers) {
            let healed = 0;
            Object.values(this.adventurerStates).forEach(state => {
                if (state.injuredUntil > this.day) {
                    state.injuredUntil = 0;
                    healed++;
                }
            });
            if (healed > 0) results.push({ text: `💊 ${healed}명의 모험가 부상 치료!`, color: '#44ff88' });
            else results.push({ text: `💊 치료할 모험가가 없었다.`, color: '#888888' });
        }

        // 길드 점검 보상
        if (eff.guildReward) {
            const tier = this.getReputationTier();
            const reward = Math.max(0, this.reputation * 2);
            if (reward > 0) {
                this.gold += reward;
                results.push({ text: `📋 길드 보상: +${reward}G (${tier.name})`, color: '#ffcc44' });
            } else {
                results.push({ text: `📋 길드: "아직 실적이 부족합니다."`, color: '#888888' });
            }
        }

        // 평판 보너스
        if (eff.repBonus) {
            this.reputation = Math.min(this.reputation + eff.repBonus, 50);
            results.push({ text: `⭐ 평판 +${eff.repBonus}`, color: '#ffcc44' });
        }

        return results;
    }

    // === 평판 단계 ===

    getReputationTier() {
        let result = REPUTATION_TIERS[0];
        for (const tier of REPUTATION_TIERS) {
            if (this.reputation >= tier.min) result = tier;
        }
        return result;
    }

    // === 모험가 시스템 ===

    getAdvState(advId) {
        return this.adventurerStates[advId];
    }

    isAdvInjured(advId) {
        const state = this.adventurerStates[advId];
        return state && state.injuredUntil > this.day;
    }

    getAdvInjuryDays(advId) {
        const state = this.adventurerStates[advId];
        if (!state || state.injuredUntil <= this.day) return 0;
        return state.injuredUntil - this.day;
    }

    getAdvLevel(advId) {
        const state = this.adventurerStates[advId];
        return state ? state.level : 1;
    }

    getAdvEffectiveStats(adv) {
        const state = this.adventurerStates[adv.id];
        if (!state) return { successRate: adv.successRate, lootMult: adv.lootMult };

        const levelData = ADV_LEVEL_TABLE[state.level - 1] || ADV_LEVEL_TABLE[0];
        let sr = adv.successRate + levelData.srBonus;
        let lm = adv.lootMult + levelData.lootBonus;

        // 퀘스트 보상 적용
        if (state.questComplete && adv.quest) {
            const r = adv.quest.reward;
            if (r.successRate) sr += r.successRate;
            if (r.lootMult) lm += r.lootMult;
        }

        // 날씨 보너스
        const w = WEATHER_DATA[this.weather];
        if (w) sr += w.successRateBonus;

        // 이벤트: 도적 출현
        if (this.dailyEvent?.effect?.successRateBonus) {
            sr += this.dailyEvent.effect.successRateBonus;
        }

        // 이벤트: 도적 출현 — 전리품 보너스
        if (this.dailyEvent?.effect?.lootBonus) {
            lm += this.dailyEvent.effect.lootBonus;
        }

        return {
            successRate: Math.min(Math.max(sr, 0.05), 0.99),
            lootMult: Math.round(lm * 100) / 100,
        };
    }

    getAdvZones(adv) {
        const state = this.adventurerStates[adv.id];
        let zones = [...adv.zones];
        if (state && state.questComplete && adv.quest?.reward?.newZone) {
            if (!zones.includes(adv.quest.reward.newZone)) {
                zones.push(adv.quest.reward.newZone);
            }
        }
        return zones;
    }

    addAdvExp(advId, amount) {
        const state = this.adventurerStates[advId];
        if (!state) return null;

        state.exp += amount;
        let leveledUp = false;

        while (state.level < ADV_MAX_LEVEL) {
            const nextLevel = ADV_LEVEL_TABLE[state.level];
            if (nextLevel && state.exp >= nextLevel.exp) {
                state.level++;
                leveledUp = true;
            } else break;
        }

        return leveledUp ? state.level : null;
    }

    injureAdv(advId, days) {
        const state = this.adventurerStates[advId];
        if (state) state.injuredUntil = this.day + days;
    }

    advanceQuestProgress(advId, zoneId) {
        const state = this.adventurerStates[advId];
        const adv = ADVENTURER_DATA.find(a => a.id === advId);
        if (!state || !adv?.quest || state.questComplete) return null;

        if (zoneId === adv.quest.zone) {
            state.questProgress++;
            if (state.questProgress >= adv.quest.required) {
                state.questComplete = true;
                return 'complete';
            }
            return 'progress';
        }
        return null;
    }

    getAvailableAdventurers() {
        return ADVENTURER_DATA.filter(a => !a.unlockDay || this.day >= a.unlockDay);
    }

    // === 장인 등급 ===

    getArtisanLevel(category) {
        const exp = this.artisanExp[category] || 0;
        let level = ARTISAN_LEVELS[0];
        for (const lv of ARTISAN_LEVELS) {
            if (exp >= lv.exp) level = lv;
        }
        return level;
    }

    getArtisanLevelIndex(category) {
        const exp = this.artisanExp[category] || 0;
        let idx = 0;
        for (let i = 0; i < ARTISAN_LEVELS.length; i++) {
            if (exp >= ARTISAN_LEVELS[i].exp) idx = i;
        }
        return idx;
    }

    addArtisanExp(category, amount) {
        if (!this.artisanExp[category] && this.artisanExp[category] !== 0) return null;
        const oldIdx = this.getArtisanLevelIndex(category);
        this.artisanExp[category] += amount;
        const newIdx = this.getArtisanLevelIndex(category);
        if (newIdx > oldIdx) return ARTISAN_LEVELS[newIdx];
        return null;
    }

    // === 확장/투자 ===

    getUpgradeLevel(upgradeId) {
        return this.upgradeLevels[upgradeId] || 0;
    }

    getUpgradeCost(upgradeId) {
        const up = UPGRADES[upgradeId];
        if (!up) return null;
        const lv = this.getUpgradeLevel(upgradeId);
        if (lv >= up.maxLevel) return null;
        return up.costs[lv];
    }

    buyUpgrade(upgradeId) {
        const cost = this.getUpgradeCost(upgradeId);
        if (cost === null || this.gold < cost) return false;
        this.gold -= cost;
        this.upgradeLevels[upgradeId]++;

        // 효과 적용
        const up = UPGRADES[upgradeId];
        if (up.effect === 'shopSlots') this.shopSlots++;
        if (up.effect === 'craftSlots') this.craftSlots++;

        return true;
    }

    // === 레시피 연구 ===

    isRecipeDiscovered(recipeId) {
        return !!this.discoveredRecipes[recipeId];
    }

    getResearchHints() {
        // 보유 재료 기반으로 힌트 제공
        const hints = [];
        Object.entries(RESEARCH_RECIPES).forEach(([id, recipe]) => {
            if (this.isRecipeDiscovered(id)) return;
            const hasAll = recipe.ingredients.every(ing => this.getItemCount(ing.id) >= ing.count);
            const hasSome = recipe.ingredients.some(ing => this.getItemCount(ing.id) > 0);
            if (hasSome) {
                hints.push({ id, recipe, hasAll, hint: recipe.hint });
            }
        });
        return hints;
    }

    tryResearch(recipeId) {
        const recipe = RESEARCH_RECIPES[recipeId];
        if (!recipe || this.isRecipeDiscovered(recipeId)) return { success: false, reason: '이미 발견됨' };

        // 재료 확인 및 소비
        const hasAll = recipe.ingredients.every(ing => this.getItemCount(ing.id) >= ing.count);
        if (!hasAll) return { success: false, reason: '재료 부족' };

        // 재료 소비
        recipe.ingredients.forEach(ing => this.removeItem(ing.id, ing.count));

        // 성공/실패 (70% 성공)
        if (Math.random() < 0.7) {
            this.discoveredRecipes[recipeId] = true;
            return { success: true, recipe };
        } else {
            return { success: false, reason: '실패... 재료를 낭비했다' };
        }
    }

    getAllAvailableRecipes() {
        const recipes = Object.entries(RECIPE_DATA).filter(([, r]) => this.day >= (r.unlockDay || 1));
        // 발견된 연구 레시피 추가
        Object.entries(RESEARCH_RECIPES).forEach(([id, r]) => {
            if (this.isRecipeDiscovered(id)) {
                recipes.push([id, r]);
            }
        });
        return recipes;
    }

    // === 손님 수 계산 ===

    getCustomerCount() {
        const base = 6;
        const repTier = this.getReputationTier();
        const repBonus = repTier.customerBonus;
        const dayBonus = Math.floor(this.day / 5);

        // 날씨 배율
        const weatherMult = WEATHER_DATA[this.weather]?.customerMult || 1.0;

        // 이벤트 배율
        let eventMult = 1.0;
        if (this.dailyEvent?.effect?.customerMult) {
            eventMult = this.dailyEvent.effect.customerMult;
        }

        const raw = (base + repBonus + dayBonus) * weatherMult * eventMult;
        return Math.min(Math.max(Math.round(raw), 2), 25);
    }

    // === 일반 시스템 ===

    advanceDay() {
        this.day++;
        this._rollDemand();
        this._rollWeather();
        this._rollDailyEvent();

        if (this.workers.length > 0) this.gold -= this.workerCostPerDay;

        this.activeOrders = this.activeOrders.filter(o => {
            if (this.day > o.deadlineDay) {
                this.reputation = Math.max(this.reputation - 3, -20);
                return false;
            }
            return true;
        });

        this.commissionResults = [];
        if (!this.isExpeditionBlocked()) {
            this.pendingCommissions.forEach(c => {
                const result = ExpeditionSystem.run(c.zoneId, c.adventurer, this);
                this.commissionResults.push(result);
            });
        } else {
            // 폭풍으로 원정 불가 — 비용 환불
            this.pendingCommissions.forEach(c => {
                this.gold += c.adventurer.cost;
                this.commissionResults.push({
                    success: false,
                    items: [],
                    log: [
                        { text: `${c.adventurer.icon} ${c.adventurer.name}`, delay: 0 },
                        { text: `⛈️ 폭풍으로 인해 원정이 취소되었습니다.`, delay: 500, color: '#ff8844' },
                        { text: `💰 의뢰 비용 ${c.adventurer.cost}G 환불`, delay: 1000, color: '#44ff88' },
                    ],
                    gold: 0,
                    advEvents: [],
                });
            });
        }
        this.pendingCommissions = [];
    }

    generateOrders() {
        const newOrders = [];
        if (this.activeOrders.length >= ORDER_CONFIG.maxActiveOrders) return newOrders;
        let added = 0;
        for (const template of ORDER_TEMPLATES) {
            if (added >= ORDER_CONFIG.maxNewPerDay) break;
            if (this.activeOrders.length + newOrders.length >= ORDER_CONFIG.maxActiveOrders) break;
            if (Math.random() > ORDER_CONFIG.newOrderChance) continue;
            const recipe = RECIPE_DATA[template.item];
            if (recipe && this.day < recipe.unlockDay) continue;
            newOrders.push({ ...template, deadlineDay: this.day + template.deadline });
            added++;
        }
        return newOrders;
    }

    acceptOrder(order) { this.activeOrders.push(order); }

    fulfillOrder(idx) {
        const order = this.activeOrders[idx];
        if (!order || this.getItemCount(order.item) < order.qty) return false;
        this.removeItem(order.item, order.qty);
        this.gold += order.reward;
        this.totalEarned += order.reward;
        this.reputation = Math.min(this.reputation + 2, 50);
        this.activeOrders.splice(idx, 1);
        return true;
    }

    getUnlockedZones() {
        return Object.entries(ZONE_DATA)
            .filter(([, z]) => this.day >= z.unlockDay)
            .map(([id, z]) => ({ id, ...z }));
    }
}
