class StorageManager {
    static addItem(state, item) {
        const cap = GuildManager.getStorageCapacity(state);
        if (state.storage.length >= cap) return false;
        state.storage.push(item);
        return true;
    }

    static removeItem(state, itemId) {
        const idx = state.storage.findIndex(i => i.id === itemId);
        if (idx === -1) return null;
        return state.storage.splice(idx, 1)[0];
    }

    static addToSecure(state, item) {
        const cap = GuildManager.getSecureContainerCapacity(state);
        if (state.secureContainer.length >= cap) return false;
        state.secureContainer.push(item);
        return true;
    }

    static removeFromSecure(state, itemId) {
        const idx = state.secureContainer.findIndex(i => i.id === itemId);
        if (idx === -1) return null;
        return state.secureContainer.splice(idx, 1)[0];
    }

    static sellItem(state, itemId) {
        const item = StorageManager.removeItem(state, itemId);
        if (!item) return { success: false, msg: '아이템을 찾을 수 없습니다' };
        const sellValue = Math.floor(item.value * 0.7);
        GuildManager.addGold(state, sellValue);
        GuildManager.addMessage(state, `${item.name} 판매 (+${sellValue}G)`);
        SaveManager.save(state);
        return { success: true, gold: sellValue, msg: `${item.name} 판매 (+${sellValue}G)` };
    }

    static getEquippableItems(state, slot) {
        return state.storage.filter(i => i.type === 'equipment' && i.slot === slot);
    }
}
