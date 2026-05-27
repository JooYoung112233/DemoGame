# 아이콘 원본 그리드 (raw)

여기에 그리드 원본 PNG 2장을 저장:

| 파일명 | 내용 | 그리드 |
|---|---|---|
| `skills.png` | 6 스킬 | 2행 × 3열 |
| `attacks.png` | 공격/지원 액션 + 잉여 | 4행 × 5열 |

저장 후 워크트리 루트에서:

```powershell
pwsh -File demo9-guild/tools/slice-icons.ps1
```

결과:
- 매칭 셀 → `demo9-guild/assets/actions/<action_id>.png` (ActionIcons 로더가 자동 인식)
- 매칭 없는 셀 → `demo9-guild/assets/actions/_unused/<tag>_r{R}_c{C}.png`

매핑 규칙: [`docs/systems/skill-icons.md` §6](../../../docs/systems/skill-icons.md).
