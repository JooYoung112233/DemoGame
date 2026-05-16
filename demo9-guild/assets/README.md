# Demo9 Assets

## characters/ — 클래스 스프라이트
| 파일 | 클래스 |
|---|---|
| `warrior.png` | 전사 |
| `rogue.png` | 도적 |
| `archer.png` | 궁수 |
| `mage.png` | 마법사 |
| `priest.png` | 사제 |
| `alchemist.png` | 연금술사 |

- 해상도: 자유 (정사각형 권장)
- 흰 배경 자동 제거 (R/G/B > 235 픽셀 → 투명)
- 코드에서 80px로 자동 스케일

## backgrounds/ — 전투 배경
| 파일 | 구역 |
|---|---|
| `bloodpit.png` | Blood Pit (혈투장) |
| `cargo.png` | Cargo (화물 열차) |
| `blackout.png` | Blackout (저주받은 저택) |

- **권장 해상도: 1920×1080** (카메라 zoom 1.5에서 1:1 픽셀 매핑)
- 게임 좌표 1280×720에 fit으로 표시 (자동 스케일)
- 코드에서 비네트(상하단 어둡게) + 80% 밝기 자동 적용 (캐릭터 가시성)
- 파일 없으면 zone별 색조 그라데이션 폴백

## 파일 추가 시
1. 위 폴더에 파일 저장 (정확한 이름)
2. 브라우저 강제 새로고침 (Ctrl+Shift+R)
3. 자동 로드 + 적용 (코드 수정 불요)
