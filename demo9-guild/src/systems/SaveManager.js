class SaveManager {
    static SAVE_KEY = 'demo9-guild-save';

    static save(gameState) {
        const data = {
            ...gameState,
            roster: gameState.roster.map(m => m.toJSON()),
            recruitPool: gameState.recruitPool.map(m => m.toJSON()),
            fallenMercs: (gameState.fallenMercs || []).map(m => m.toJSON()),
            activeExpeditions: gameState.activeExpeditions || [],
            pendingResults: gameState.pendingResults || [],
            savedParties: gameState.savedParties || []
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
            data.fallenMercs = (data.fallenMercs || []).map(m => Mercenary.fromJSON(m));
            data.activeExpeditions = data.activeExpeditions || [];
            data.pendingResults = data.pendingResults || [];
            data.zoneClearCount = data.zoneClearCount || {};
            data.savedParties = data.savedParties || [];
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
