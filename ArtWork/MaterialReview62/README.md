# 찰흙 같은 표현 보정 — 2026-09-11

사용자 승인: “약간 게임이 찰흙 같아서 순서대로 해보자.” 재질 차이 → 입체감 → 낮밤 순서로 비교했다. 모델·배치·전투 로직 변경은 없다.

## 확인한 원인과 변경

- 마을 25개 GameLit 재질 중 기본 Town02 재질 18개는 표면 마스크가 연결만 되어 있고 비활성이었다. 철재도 metallic=0 및 specular off여서 다른 표면과 같은 무광으로 보였다. R=금속성/A=매끄러움 마스크를 켜고 스칼라를 1로 연결했다. 노멀 .35~.65, grime .09~.12, shadow desaturation .18. 건조한 지면/벽/천은 직접 반사 off, 목재/철재/도장 금속은 on. 환경 반사는 off를 유지했다.
- StoryNPC 4명: rim .045→.012, normal .35→.6, grime .12→.07, shadow desaturation .2→.14.
- 실제 사용하는 플레이어/밴딧/무기 재질 6개: rim .12→.018, grime .3→.10, shadow desaturation .45→.18. Holster smoothness .5→.2. SimpleBandit_Surface는 실제 근접·권총·소총 Resource 프리팹이 공유한다.
- 기존 SSAO 기능 활성화: intensity .4 / radius .18m / direct .1 / falloff 60m, full resolution, DepthNormals source. 검수에서 넓은 검은 테두리 대신 약한 접촉 음영만 남겼다. GameLit DepthNormals가 실제 노멀맵을 쓰도록 URP LitDepthNormalsPass로 맞췄다.
- GameLitConverter: 이미 검수한 GameLit 재질을 다시 기본 룩으로 덮어쓰지 않도록 수정. 처음 URP/Lit에서 전환할 때 활성 metallic map을 보존하고 GameLit의 금속 스칼라를 1로 설정한다. 노멀/발광/알파 처리와 투명 재질 제외는 유지한다.

## 캡처 구분

- `Before_*`, `Materials_*`, `After_*`: **Unity Editor 렌더**. 같은 62°/0° 임시 카메라로 Safehouse 실제 씬을 촬영. Materials는 재질만 보정, After는 SSAO까지 적용. 주광/주변광만 낮·밤 WeatherData로 바꾸고 모두 복원했다. Pawnshop 촬영 동안만 지붕 Renderer를 숨겼다가 복원했다. 게임 카메라의 URP 후처리 설정을 복사했다.
- `Board_Before/After`: 동일 구/상자에 왼쪽부터 콘크리트·목재·철재·천을 적용한 재질 비교. Game view 캡처가 아닌 Unity 내 테스트 렌더다.
- `Characters_Before/After_Day/Night`: 실제 플레이어·밴딧·관리인 시각 프리팹의 동일 조명 재질 비교. NPC 애니메이션·전투 동작 검증이 아니다. 밤 비교에는 착용등이나 주변 보조광을 넣지 않았으므로 실제 플레이 야간 화면보다 어둡다.
- `BeforeMaterials.json`은 수치와 슬롯 검사 기록. `AppliedMaterials.json`, `AppliedCharacters.json`은 변경 대상 목록. `tools`에 재현·검사 코드가 있다.
- `Runtime_district_warden.png` / `Runtime_veteran_scavenger.png`: 실제 Play 상태에서 게임 카메라 설정을 복사한 임시 카메라로 마을 안 NPC 재검수. `RuntimeValidation.json`에 62°/58° 및 shader compiler 오류 0을 기록했다. Unity console 오류 0 확인. 플레이어/게임 카메라는 이동시키지 않았다.
- `Runtime.png`는 정상 결과 공유용 캡처가 아니다. 촬영 사이 플레이어/카메라가 월드 원점 근처로 이동하여 마을 바닥 범위 `[0..80]×[0..56]` 밖의 검은 배경이 나타났다. SSAO를 꺼도 같은 현상이 나타났고, 카메라 `(-.30,22.07,-11.74)`/플레이어 `(-.30,0,0)` 실측으로 지면 바깥 촬영임을 확인했다. SSAO는 다시 켰으며, 마을 내부 임시 카메라 재검수는 정상이다. 위치가 바뀐 원인은 이번 재질 변경 범위 밖의 미해결 사항이다.

## 판단과 한계

재질 연결 오류와 과한 림/탈색은 해결했으나 전체 그림이 크게 바뀌는 작업은 아니다. 기존 저주파 표면 얼룩과 캐릭터의 단순한 색면·매끈한 형태는 남아 있다. 원화 수준의 천 주름·봉제선·국소 마모는 원본 텍스처/모델 후속 작업이 필요하며, 셰이더만으로 해결했다고 판정하지 않는다. 밤 조명 비교는 통과 여부를 밝기 하나로 판단하지 않으며, 착용등을 포함한 실전 가독성은 별도 게임 검증 대상이다. SSAO의 GPU 비용·이동 시 시간적 안정성은 아직 실측하지 않았다.

승인된 하이드아웃 `ArtWork/HideoutReview/InGame.png`를 재질·분위기 기준으로 다시 대조했다. Hideout02의 기존 URP/Lit 재질, 날씨 수치, 카메라, 그림자 길이 설정은 변경하지 않았다.
