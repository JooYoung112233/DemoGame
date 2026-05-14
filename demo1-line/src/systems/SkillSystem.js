class SkillSystem {
    static getSkillsForCharacter(charKey) {
        switch (charKey) {
            case 'tank': return ['taunt'];
            case 'rogue': return ['bleed'];
            case 'priest': return ['heal', 'shield'];
            case 'mage': return ['meteor'];
            default: return [];
        }
    }
}
