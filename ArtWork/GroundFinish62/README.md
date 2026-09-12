# GroundFinish62 — Blender 바닥·입구 마감 6종

2026-09-12. **Blender 제작 후 최신 Demo/demo13의 하이드아웃 서쪽 입구에 Unity 적용·자체 검수 완료.**
사용자 지시: Blender 선제작 후 “unity 켜도된다”, 적용 대상은 “최신 Demo/demo13에 적용”으로 확정.

## 파일

- `BlenderSource~/GroundFinish62.blend`: 편집 가능한 원본. 미터 단위, 관련 PBR 이미지 포함.
- 기본 `Scene`: 기존 Town02 수리점 외관을 재사용한 입구 배치 예시, 62° 검수 카메라.
- `Asset_*` 씬 6개: 원점에 있는 각 모델의 편집 가능한 개별 조각. 배경 건물·카메라 없이 선택/수정 가능.
- `Models/*.fbx`: 신규 모듈 6개만 내보낸 파일. 건물·배경·조명은 포함하지 않는다.
- `Review/KitOverview62.png`: 전체 6종. `EntranceExample62.png`: 기존 수리점과 배치 예시.
- `Review/EdgeDetail62.png`, `SurfaceLowAngle.png`: 62° 확대 및 25° 낮은 시점 표면 검사.
- `ModelValidation.json`, `FBXValidation.json`: 생성 및 독립 재임포트 검사 기록.

## 구성

| 모델 | 용도 |
|---|---|
| CrackBranch_A | 길게 갈라지는 가는 아스팔트 균열 |
| CrackBranch_B | 방향이 휘는 분기 균열 |
| BrokenPaving_A | 약하게 부서진 포장 가장자리, 약 3.6m 길이 |
| BrokenPaving_B | 국소 파손이 큰 포장 가장자리, 약 3.6m 길이 |
| DirtGravel_Transition | 흙·먼지가 옅어지며 흩어지는 작은 자갈 |
| WallWeeds_Dust | 벽 밑 먼지와 낮은 잡초, 최고 약 0.24m |

고정된 뼈/리그가 필요 없는 정적 배경 모델이다. 포장 모듈은 약 7cm 두께이며,
완성된 바닥 위에 모두 겹쳐 놓기보다 기존 가장자리 구간을 대체하도록 배치한다.
잡초와 자갈을 출입 동선 전체에 고르게 뿌리지 않고 벽 밑·깨진 끝단에 모은다.

## 원화와 재사용

제작 전에 직접 연 원화:

- `Assets/GPT/안전구역/ChatGPT Image 2026년 6월 11일 오후 02_52_23.png`: 앞선 검토에서 확인한 전체 마을의 포장 경계·생활 흔적.
- `Assets/GPT/데칼/안전구역데칼1.png`: 깨진 포장과 흩어진 작은 파편.
- `Assets/GPT/데칼/안전구역데칼3.png`: 흙·자갈이 흐트러지는 경계와 국소 오염.
- `Assets/GPT/안전구역/ChatGPT Image 2026년 6월 11일 오후 06_59_29.png`: 수리점 현관·차고 주변의 재질 기준.

원화의 혈흔·유리·타이어 자국은 이번 세트에 포함하지 않았다. 기존 Town02의
Asphalt/Plaster Base·Normal·Mask를 재사용하고, 반짝임 없는 작은 석재·풀의 재질을 구성했다.
원화 이미지는 편집하거나 모델 텍스처로 잘라 붙이지 않았다. 수리점 shell/roof는
기존 `.blend`의 참고 배경을 재사용했으며 새로 제작한 건물로 계산하지 않는다.

## 자체 검수 및 수정

- 첫 렌더의 종이 조각 같은 흙을 **가장자리 정점 알파가 0으로 내려가는 얇은 표면**으로 변경.
- 밝게 솟아 보이던 균열 테두리의 폭·대비를 낮추고 표면의 불필요한 그림자를 제거.
- 배치 예시에서 깨진 포장 면이 도로 쪽을 향하도록 회전.
- FBX에서 겹친 잡초 뒷면이 소실되는 현상을 실제 1.5mm 잎 두께로 수정.
- 62° 전체/확대 및 25° 낮은 시점으로 수정 결과 재검수.
- 각 모델 UV0·탄젠트 생성, 퇴화 삼각형 0, 독립 FBX 재임포트의 크기·삼각형 수·정점 알파 검사.

자체 검수는 사용자 최종 아트 승인을 대신하지 않는다. 아래 Unity 검수까지 완료했다.

## Unity 적용 및 검사

- `Assets/Art/Environments/GroundFinish62`: 모델 6개, 재질 9개, 개별 프리팹 6개와 `HideoutGroundFinish62` 조합 프리팹.
- `GroundFade` 정점 Alpha를 전용 `BRB/GroundFadeLit`의 투명도에 연결했다. 기존 GameLit의 조명·노멀·마스크 계산을 재사용한다.
- 얇은 균열·먼지 표면은 그림자를 받되 별도 떠 있는 그림자를 만들지 않도록 설정한다.
- 원점은 Blender Z-up/미터 기준이며 FBX는 -Z forward/Y up으로 내보낸다.
- Safehouse 하이드아웃 서쪽 기존 포장 3칸의 Renderer 9개를 숨기고 새 모듈 8개를 배치했다. 기존 충돌/입구 값은 보존, 신규 콜라이더는 0개다.
- 포장 명도를 기존 판에 맞추고 상면을 약 0.044m로 내려 배수구를 노출했다. 원래 FBX 축 보정을 유지하면서 파손 면을 바깥으로 돌렸다.
- `Unity/Runtime_*.png`: 실제 씬 재진입 후 별도 62° 검수 카메라 렌더. `Night_*.png`: 임시 야간 조명 검수 후 조명 복원. `Review/` 이미지는 Blender 렌더다.
- `Unity/playtest.json`: 새 포장 3.833m 통과 → 입구 트리거 진입 → 나가기/확인 버튼으로 귀환 통과. 최종 출입 재검사 경고 0·오류 0, 셰이더 오류 0.
- 초기 테스트의 동일 Safehouse 중복 로드로 태양 중복 경고가 발생해 검사 스크립트에서 이미 로드된 마을을 재로드하지 않게 수정하고 재검사했다.
- 재현 도구는 Unity MCP `run_script`로 실행한다. 새 구역에 최초 배치할 때 `import.json` → `place.json` → `refine.json` 순서. `Placement.json`은 초기값, `Refinement.json`이 최종 보정이다. 현재 씬은 이미 적용되어 있어 Place를 다시 실행하면 중복 방지로 중단한다. 플레이 검사는 `run-playtest.json`이 입력, `playtest.json`이 결과다.

재생성: Blender 4.5 LTS에서 `--background --python-exit-code 1 --python build_ground_finish.py`.
검사: 같은 방식으로 `validate_ground_finish.py`. 모든 산출물은 현재 폴더에만 기록한다.
