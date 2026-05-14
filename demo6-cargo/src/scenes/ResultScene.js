class LCResultScene extends Phaser.Scene {
    constructor() { super('LCResultScene'); }

    init(data) {
        this.trainManager = data.trainManager;
        this.invasionSystem = data.invasionSystem;
        this.party = data.party;
    }

    create() {
        this.cameras.main.setBackgroundColor('#1a1510');
        const cx = 640;

        const survived = !this.trainManager.isGameOver();
        const victory = survived && this.invasionSystem.isLastRound();

        let title, titleColor;
        if (victory) {
            title = '🚂 목적지 도착! 생존 성공!';
            titleColor = '#44ff88';
        } else if (survived) {
            title = '⚠️ 여정 중단...';
            titleColor = '#ffaa44';
        } else {
            title = '💀 열차 전멸... 게임 오버';
            titleColor = '#ff4444';
        }

        this.add.text(cx, 60, title, {
            fontSize: '28px', fontFamily: 'monospace', color: titleColor, fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(cx, 100, `생존 라운드: ${this.invasionSystem.currentRound} / ${this.invasionSystem.maxRound}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#aa8866'
        }).setOrigin(0.5);

        this.add.text(cx, 140, '── 열차 최종 상태 ──', {
            fontSize: '14px', fontFamily: 'monospace', color: '#665533'
        }).setOrigin(0.5);

        const carWidth = 260, gap = 20;
        const totalW = 4 * carWidth + 3 * gap;
        const startX = cx - totalW / 2 + carWidth / 2;

        this.trainManager.cars.forEach((car, i) => {
            const x = startX + i * (carWidth + gap);
            const y = 240;
            const alpha = car.destroyed ? 0.3 : 1;

            this.add.rectangle(x, y, carWidth, 120, car.destroyed ? 0x111111 : 0x221a10, alpha)
                .setStrokeStyle(2, car.color);

            this.add.text(x, y - 40, car.name, {
                fontSize: '15px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5).setAlpha(alpha);

            if (car.destroyed) {
                this.add.text(x, y, '파괴됨', {
                    fontSize: '18px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
                }).setOrigin(0.5);
                this.add.text(x, y + 22, car.lostEffect, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#aa4444'
                }).setOrigin(0.5);
            } else {
                const hpRatio = car.hp / car.maxHp;
                const hpColor = hpRatio > 0.6 ? '#44ff88' : hpRatio > 0.3 ? '#ffaa00' : '#ff4444';
                this.add.text(x, y - 15, `HP: ${car.hp}/${car.maxHp}`, {
                    fontSize: '13px', fontFamily: 'monospace', color: hpColor
                }).setOrigin(0.5);

                const barW = carWidth - 30;
                this.add.rectangle(x, y + 5, barW, 6, 0x333333);
                const fillW = barW * hpRatio;
                if (fillW > 0) {
                    const barColor = hpRatio > 0.6 ? 0x44ff88 : hpRatio > 0.3 ? 0xffaa00 : 0xff4444;
                    this.add.rectangle(x - barW / 2 + fillW / 2, y + 5, fillW, 6, barColor);
                }

                if (car.module) {
                    this.add.text(x, y + 22, `${car.module.icon} ${car.module.name}`, {
                        fontSize: '11px', fontFamily: 'monospace', color: car.module.color
                    }).setOrigin(0.5);
                }

                if (car.doorBroken) {
                    this.add.text(x, y + 38, '⚠️ 문 파손', {
                        fontSize: '10px', fontFamily: 'monospace', color: '#ff6644'
                    }).setOrigin(0.5);
                }
            }
        });

        const operationalCount = this.trainManager.getOperationalCount();
        const destroyedCount = 4 - operationalCount;

        let yPos = 340;
        this.add.text(cx, yPos, '── 통계 ──', {
            fontSize: '14px', fontFamily: 'monospace', color: '#665533'
        }).setOrigin(0.5);

        yPos += 30;
        const stats = [
            `가동 중인 칸: ${operationalCount} / 4`,
            `파괴된 칸: ${destroyedCount}`,
            `남은 부품: ${this.trainManager.parts}`,
            `남은 탄약: ${this.trainManager.ammo}`,
            `남은 의료킷: ${this.trainManager.medkits}`,
            `수집한 드랍: ${this.trainManager.collectedDrops.length}개`
        ];

        stats.forEach((s, i) => {
            this.add.text(cx, yPos + i * 22, s, {
                fontSize: '13px', fontFamily: 'monospace', color: '#aa8866'
            }).setOrigin(0.5);
        });

        yPos += stats.length * 22 + 20;

        let grade, gradeColor;
        if (victory && destroyedCount === 0) { grade = 'S'; gradeColor = '#ffff44'; }
        else if (victory && destroyedCount <= 1) { grade = 'A'; gradeColor = '#44ff88'; }
        else if (victory) { grade = 'B'; gradeColor = '#88aaff'; }
        else if (this.invasionSystem.currentRound >= 7) { grade = 'C'; gradeColor = '#ffaa44'; }
        else { grade = 'D'; gradeColor = '#ff4444'; }

        this.add.text(cx, yPos, `등급: ${grade}`, {
            fontSize: '36px', fontFamily: 'monospace', color: gradeColor, fontStyle: 'bold'
        }).setOrigin(0.5);

        yPos += 50;
        const totalKills = this.trainManager.collectedDrops.length;
        const farmData = LC_FARMING.addRunRewards(totalKills, this.invasionSystem.currentRound, grade);

        this.add.text(cx, yPos, '── 파밍 보상 ──', {
            fontSize: '14px', fontFamily: 'monospace', color: '#665533'
        }).setOrigin(0.5);
        yPos += 25;

        const matDisplay = [
            `🔩 고철: ${farmData.scrap || 0}`,
            `⚡ 회로: ${farmData.circuit || 0}`,
            `💎 코어: ${farmData.core || 0}`
        ];
        this.add.text(cx, yPos, matDisplay.join('   |   '), {
            fontSize: '13px', fontFamily: 'monospace', color: '#ccaa66'
        }).setOrigin(0.5);
        yPos += 22;
        this.add.text(cx, yPos, `총 출격: ${farmData.totalRuns}회  |  최고 등급: ${farmData.bestGrade}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#887755'
        }).setOrigin(0.5);

        const retryBtn = this.add.rectangle(cx, 660, 200, 44, 0x664411)
            .setStrokeStyle(2, 0xff8844).setInteractive();
        this.add.text(cx, 660, '다시 도전', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold'
        }).setOrigin(0.5);

        retryBtn.on('pointerover', () => retryBtn.setFillStyle(0x884422));
        retryBtn.on('pointerout', () => retryBtn.setFillStyle(0x664411));
        retryBtn.on('pointerdown', () => this.scene.start('LCSetupScene'));

        this.add.text(cx, 700, '← 목록으로', {
            fontSize: '13px', fontFamily: 'monospace', color: '#665533'
        }).setOrigin(0.5).setInteractive()
            .on('pointerdown', () => { window.location.href = '../index.html'; });
    }
}
