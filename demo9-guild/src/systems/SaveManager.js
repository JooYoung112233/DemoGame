class SaveManager {
    static SAVE_KEY = 'demo9-guild-save';

    static save(gameState) {
        const data = {
            ...gameState,
            roster: gameState.roster.map(m => m.toJSON()),
            recruitPool: gameState.recruitPool.map(m => m.toJSON()),
            // v2: 활성 파견 / 미수령 결과 저장 (그대로 직렬화 가능)
            activeExpeditions: gameState.activeExpeditions || [],
            pendingResults: gameState.pendingResults || []
        };
        localStorage.setItem(SaveManager.SAVE_KEY, JSON.stringify(data));
    }

    static load() {
        const raw = localStorage.getItem(SaveManager.SAVE_KEY);
        if (!raw) return null;
        try {
            const data = JSON.parse(raw);
            data.roster = (data.roster || []).map(m => Mercenary.fromJSON(m));
            data.recruitPool = (data.recruitPool || []).map(m => Mercenary.fromJSON(m));
            // v2 신규 필드 호환성
            data.activeExpeditions = data.activeExpeditions || [];
            data.pendingResults = data.pendingResults || [];
            data.zoneClearCount = data.zoneClearCount || {};
            return data;
        } catch (e) {
            console.error('Save load failed:', e);
            return null;
        }
    }

    static hasSave() {
        return !!localStorage.getItem(SaveManager.SAVE_KEY);
    }

    static deleteSave() {
        localStorage.removeItem(SaveManager.SAVE_KEY);
    }
}
