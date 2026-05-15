class IntelScene extends Phaser.Scene {
    constructor() { super('IntelScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        const gs = this.gameState;

        this.add.text(640, 25, '🔍 정보소', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.goldText = this.add.text(1260, 25, `${gs.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        this.add.text(640, 55, '구역 정보를 확인하고, 미션을 수락할 수 있습니다', {
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5);

        this._drawZoneInfo(gs);
        this._drawMissions(gs);
    }

    _drawZoneInfo(gs) {
        const zones = Object.entries(ZONE_DATA);
        let cx = 40;

        zones.forEach(([key, zone]) => {
            const level = gs.zoneLevel[key] || 0;
            const unlocked = level > 0;
            const maxRounds = unlocked ? getMaxRounds(level) : '?';

            const panel = UIPanel.create(this, cx, 80, 390, 240, { title: `${zone.icon} ${zone.name}` });

            const color = unlocked ? '#aaaacc' : '#555566';
            this.add.text(cx + 15, 115, unlocked ? `구역 Lv.${level}` : '미해금', {
                fontSize: '13px', fontFamily: 'monospace', color: unlocked ? '#44aaff' : '#ff4444', fontStyle: 'bold'
            });

            this.add.text(cx + 15, 138, `${zone.subtitle}`, {
                fontSize: '11px', fontFamily: 'monospace', color: color
            });

            this.add.text(cx + 15, 158, `최대 라운드: ${maxRounds}`, {
                fontSize: '11px', fontFamily: 'monospace', color: color
            });

            this.add.text(cx + 15, 178, `기본 보상: ${zone.baseGoldReward}G / ${zone.baseXpReward}XP`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ffcc44'
            });

            this.add.text(cx + 15, 198, `사망 확률: ${Math.floor(zone.deathChance * 100)}%`, {
                fontSize: '11px', fontFamily: 'monospace', color: zone.deathChance > 0.15 ? '#ff4444' : '#ffaa44'
            });

            const { composition } = unlocked
                ? getEnemyComposition(maxRounds, level, key)
                : { composition: [] };
            if (composition.length > 0) {
                const enemies = composition.map(c => `${ENEMY_DATA[c.type]?.name || c.type}×${c.count}`).join(', ');
                this.add.text(cx + 15, 220, `보스전: ${enemies}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#ff6666'
                });
            }

            this.add.text(cx + 15, 245, zone.desc, {
                fontSize: '10px', fontFamily: 'monospace', color: '#886666',
                wordWrap: { width: 360 }
            });

            cx += 410;
        });
    }

    _drawMissions(gs) {
        const panel = UIPanel.create(this, 40, 340, 1200, 340, { title: '의뢰 게시판' });

        if (!gs.missions) {
            gs.missions = this._generateMissions(gs);
            SaveManager.save(gs);
        }

        if (gs.missions.length === 0) {
            this.add.text(640, 510, '현재 의뢰 없음 — 탐사를 진행하면 새 의뢰가 등록됩니다', {
                fontSize: '12px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5);
            return;
        }

        let cy = 375;
        gs.missions.forEach((mission, idx) => {
            if (cy > 650) return;
            const done = this._checkMissionComplete(gs, mission);

            const bg = this.add.graphics();
            bg.fillStyle(done ? 0x1a2a1a : 0x1a1a2e, 1);
            bg.fillRoundedRect(55, cy, 1170, 50, 3);
            bg.lineStyle(1, done ? 0x44aa44 : 0x333355, 0.4);
            bg.strokeRoundedRect(55, cy, 1170, 50, 3);

            this.add.text(70, cy + 8, mission.name, {
                fontSize: '13px', fontFamily: 'monospace', color: done ? '#44ff88' : '#aaaacc', fontStyle: 'bold'
            });

            this.add.text(70, cy + 28, mission.desc, {
                fontSize: '10px', fontFamily: 'monospace', color: '#888899'
            });

            this.add.text(700, cy + 8, `보상: ${mission.reward}G`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44'
            });

            if (done && !mission.claimed) {
                UIButton.create(this, 1130, cy + 25, 80, 26, '수령', {
                    color: 0x446644, hoverColor: 0x558855, textColor: '#44ff88', fontSize: 11,
                    onClick: () => {
                        mission.claimed = true;
                        GuildManager.addGold(gs, mission.reward);
                        GuildManager.addMessage(gs, `의뢰 완료: ${mission.name} (+${mission.reward}G)`);
                        SaveManager.save(gs);
                        UIToast.show(this, `의뢰 완료! +${mission.reward}G`, { color: '#44ff88' });
                        this.goldText.setText(`${gs.gold}G`);
                        this.scene.restart({ gameState: gs });
                    }
                });
            } else if (mission.claimed) {
                this.add.text(1130, cy + 18, '완료', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#448844'
                }).setOrigin(0.5);
            } else {
                this.add.text(1130, cy + 18, '진행 중', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#888899'
                }).setOrigin(0.5);
            }

            cy += 58;
        });
    }

    _generateMissions(gs) {
        const missions = [];
        const runCount = gs.runCount || 0;

        missions.push({
            id: 'mission_explore',
            name: '탐사 수행',
            desc: `탐사를 ${runCount + 2}회 이상 완료하세요`,
            type: 'runCount',
            target: runCount + 2,
            reward: 150 + gs.guildLevel * 50,
            claimed: false
        });

        if (gs.zoneLevel.bloodpit >= 1) {
            missions.push({
                id: 'mission_level',
                name: '길드 성장',
                desc: `길드 레벨 ${gs.guildLevel + 1} 달성`,
                type: 'guildLevel',
                target: gs.guildLevel + 1,
                reward: 200 + gs.guildLevel * 80,
                claimed: false
            });
        }

        missions.push({
            id: 'mission_gold',
            name: '골드 축적',
            desc: `보유 골드 ${Math.floor((gs.gold + 500) / 100) * 100}G 이상 보유`,
            type: 'gold',
            target: Math.floor((gs.gold + 500) / 100) * 100,
            reward: 100,
            claimed: false
        });

        return missions;
    }

    _checkMissionComplete(gs, mission) {
        switch (mission.type) {
            case 'runCount': return (gs.runCount || 0) >= mission.target;
            case 'guildLevel': return gs.guildLevel >= mission.target;
            case 'gold': return gs.gold >= mission.target;
            default: return false;
        }
    }
}
