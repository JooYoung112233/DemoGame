# Chibi Survivor — 3D 캐릭터 패키지

## 결정 · 범위 (2026-09-07)

- 질문: 기존 2D 게임을 3D 쿼터뷰로 전환할 때 무엇부터 제작할까? 로우폴리/현실 비율 중 어떤 외형을 사용할까?
- 사용자 결정: 은신처 전환을 논의한 뒤 **캐릭터 모델과 애니메이션부터 제작**. **귀여운 로우폴리, 2~2.5등신**.
- 첫 제작안: 2.5등신 생존자. 큰 머리, 짧은 팔다리, 올리브 재킷, 주황 스카프, 카고팬츠, 갈색 부츠, 배낭. 색과 의상 세부는 첫 시안이며 별도 최종 승인을 뜻하지 않는다.
- 범위: 에셋 제작 완료. 현재 게임의 2D 이동/전투/Spine/은신처/렌더러를 교체하는 작업은 포함하지 않는다.

## 파일

| 경로 | 내용 |
|---|---|
| `Assets/Art/Characters/ChibiSurvivor/ChibiSurvivor.fbx` | 스킨 메시, Generic 리그, Idle/Walk/Run 세 가지 take |
| 같은 폴더의 `ChibiSurvivor.prefab` | 모델·Avatar·Animator가 연결된 프리팹 |
| 같은 폴더의 `Idle.anim`, `Walk.anim`, `Run.anim` | Unity에서 독립적으로 편집 가능한 루프 클립 |
| 같은 폴더의 `ChibiSurvivor.controller` | `Speed` float 값의 1D Blend Tree: 0=Idle, 1=Walk, 2=Run |
| `Assets/Editor/ChibiSurvivorAssets.cs` | 해당 FBX 전용 Generic/루프/재질 임포터와 에셋 베이크 메뉴 |
| `ArtSource/ChibiSurvivor/ChibiSurvivor.blend` | 골격, 메시, 액션, 촬영용 조명/카메라가 들어 있는 원본 |
| `ArtSource/ChibiSurvivor/ChibiSurvivor.glb` | Blender 설치 없이 확인 가능한 GLB 모델과 모션 |
| `ArtSource/ChibiSurvivor/CharacterSheet.png` | 정면·쿼터뷰·뒷모습 시트 |
| `ArtSource/ChibiSurvivor/Animations.gif` | 세 모션 비교. 개별 Idle/Walk/Run GIF도 제공 |
| `tools/build_chibi_survivor.py` | Blender로 모델·리그·애니메이션·렌더를 재생성하는 소스 |
| `tools/chibi_preview_sheet.py` | 렌더에서 시트/GIF를 조합하는 Pillow 스크립트 |

## Unity 사용

1. `ChibiSurvivor.prefab`을 **3D 테스트 씬**에 배치한다. 루트 스케일은 1이다.
2. Animator의 `Speed`를 0/1/2로 설정해 대기/걷기/달리기를 확인한다. 이 값은 모션 혼합 지표이며 실제 이동속도(m/s)가 아니다.
3. 모든 클립은 **제자리 모션**, `Apply Root Motion=false`. 이동 컨트롤러는 후속 연결 대상이다.
4. 일반 3D 조명으로 볼 때 URP **Universal Renderer(3D)**와 Lit 재질을 사용한다. 기존 게임의 URP 2D Renderer 설정은 이 패키지가 바꾸지 않는다. 임포터는 URP/Lit가 있으면 사용하고 Built-in 프로젝트에서는 Standard로 대체한다.
5. 새로 베이크하려면 `Tools > TopDown > Art > Bake Chibi Survivor Assets`. 이 메뉴는 패키지의 `.anim`과 프리팹을 다시 저장한다. 별도 커스터마이즈한 모션/프리팹은 먼저 다른 이름으로 복제한다. 기존 controller는 보존한다.
6. 이 리그는 **Generic**이다. Humanoid 자동 리타기팅용 T-pose/아바타가 아니다. `HandSocket.R`은 손 장비 연결 위치다. 배낭은 하나의 별도 본에 가중치가 연결되어 있으나 렌더 메시에는 합쳐져 있다.

## 제작 스펙

| 항목 | 값 |
|---|---|
| 높이 | 약 1.82 Unity 단위(머리카락 포함), 약 2.5등신 |
| 메시 | 원본 4,054 vertices / 7,766 triangles, 단일 SkinnedMeshRenderer |
| 골격 | 18 bones, rigid joint weights(관절을 겹쳐 틈을 가리는 토이 스타일) |
| 재질 | 15색 단색 팔레트, 15 material slots. 텍스처 없음 |
| Idle | 30 fps, 2.4초 루프 |
| Walk | 30 fps, 1초 루프 |
| Run | 30 fps, 약 0.667초 루프 |

현재 팔레트는 색 수정이 쉬운 여러 재질로 구성된다. 다수 NPC 렌더링 전에는 팔레트 텍스처/재질 병합 최적화가 후속 작업이다. 공격·피격·사망·장비별 그립·상하체 분리 마스크는 아직 제작하지 않았다.

## 재생성

프로젝트 폴더에서 Blender 4.5 LTS로:

```powershell
& '경로/blender.exe' --background --python tools/build_chibi_survivor.py
python tools/chibi_preview_sheet.py
```

이후 Unity에서 위 베이크 메뉴를 실행한다. Blender 원본은 `Assets` 밖에 두어 Unity가 Blender에 의존해 자동 임포트하지 않도록 했다. FBX/GLB에는 촬영용 바닥·카메라·조명을 내보내지 않는다. 생성 도구는 이 패키지 경로에만 산출물을 쓴다.

## 검증

- Blender 4.5.3: 전 정점 가중치 합=1, Idle/Walk/Run 전 프레임의 바닥 침범 없음, 시작/끝 메시 위치 오차 <0.00001m. 상세 수치: `ArtSource/ChibiSurvivor/validation.json`.
- Unity 6000.3.2f1 별도 검증 프로젝트: 에디터 코드 컴파일, FBX 임포트, Generic Avatar 유효성, SkinnedMeshRenderer 본/재질 연결, 3개 루프 클립 이름·길이·시작/끝 자세, controller/프리팹 베이크 통과.
- 원래 게임 프로젝트(6000.3.16f1)의 전체 컴파일/플레이 및 기존 URP 2D 씬 통합은 검증 범위 밖이다.
- Blender 렌더에서 정면/쿼터뷰/후면 및 모션 샘플 확인.

## 변경 로그

| 날짜 | 변경 |
|---|---|
| 2026-09-07 | 사용자 3D 쿼터뷰/캐릭터 우선/귀여운 2~2.5등신 로우폴리 결정 기록. 첫 모델·리깅·세 가지 루프·Unity 프리팹·원본·프리뷰 제작. |
