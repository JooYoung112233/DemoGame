# Rendering System

## 2026-09-11 — 측면 몸 과조명 재피드백

- 사용자 질문·결정: 측면 플레이어 캡처와 “여전히너무 밝은ㄷ것같은데”. 별도 선택지 없음. 직전 가방 보정으로 외형 승인이 완료된 것은 아니며 몸의 남은 과조명을 추가 보정한다.
- 관찰·진행 기준: 첨부 화면에서는 가방뿐 아니라 목·어깨·팔이 밝게 뜬다. 직전 앞뒤 검수에 측면이 빠졌으므로 현재값을 기준으로 측면 광원별 분리 비교 및 다방향 낮밤 검수를 수행한다.
- 원인: 현재 값으로 양쪽 측면을 재현했을 때 BodyGlow를 끄면 팔·목의 흰 얼룩이 사라진다. LampSpill을 끈 비교에서는 남는다. 직전 가방의 LampSpill 문제와 별개로 몸 가까이 있는 BodyGlow가 과했다.
- 반영: PlayerRig의 `bodyGlowIntensity .35→.06`, `bodyGlowTowardCamera .8→1.1m`. 첫 후보 .02/1.2m는 밤 실루엣이 너무 어두워 .06/1.1m로 조정했다. 높이 .1·반경1.6·기존 발밑 퍼짐광·전방 빔·환경 조명/재질은 유지한다. 기존 WornLamp 런타임 로직과 C# 필드 기본값은 수정하지 않고 실제 PlayerRig 직렬화 값을 보정했다.
- 저장·검수: Unity MCP로 프리팹 저장 및 현재 WornLamp에 같은 값 적용. Play/위치/지역시간을 유지했다. 디스크 프리팹·현재 인스턴스 값, 낮밤 적용 및 실제 광원 위치 검증 통과. 62°에서 45° 간격 8방향 × 낮밤 16장을 저장 후 자체 확인했다. 측면 흰 얼룩이 줄고 밤의 실루엣은 어둡게 유지된다. 자료 `ArtWork/BodyFill62`. 이번에는 재시작하지 않았고 프리팹 저장값과 라이브 적용을 각각 검사했다. 실제 하이드아웃·모든 조명 환경·연속 이동 검수나 사용자 최종 외형 승인을 뜻하지 않는다.
- 검수 도구 보완: 모델/빔 방향을 돌릴 때 BodyGlow가 자식 Transform 회전을 따라 움직이지 않도록 매 촬영마다 실제 LateUpdate와 같은 **카메라 쪽 월드 위치**를 재설정한다. 이전 BackpackLight62의 앞뒤 비교는 이 위치 동기화와 측면 범위가 불충분했다. 현재 몸 조명의 비교 기준은 BodyFill62 자료다.

## 2026-09-11 — 가방 과조명 피드백

- 사용자 질문·결정: “다좋은데 주변광? 가방에 빛이 너무나네”. 별도 선택지 없음. 직전 분위기를 긍정 평가하면서 가방이 과하게 밝은 부분의 보정을 요청했다. 원인은 아직 주변광으로 확정하지 않는다.
- 확인: 실제 플레이어를 62°에서 촬영하고 BodyGlow/LampSpill을 각각 끄는 비교를 했다. BodyGlow를 꺼도 가방 윗면의 밝은 얼룩이 남고, LampSpill을 끄면 사라진다. 가방 `SimpleHero_Gear` 발광색은 검정·smoothness .08이다. 주원인은 가슴 높이의 그림자 없는 LampSpill이 가까운 가방 윗면을 직접 비추는 것이다.
- 반영: `WornLamp.spillHeightOffset=-.65m`로 퍼짐광을 등 아래(현재 플레이어 월드 y=.5m)에 두고, 빔 회전 후 월드 높이를 유지한다. spillFloor `.35→.08`, spillRatio `.16→.04`(기본 밤 실제 세기 `.544→.136`). 코드 기본값과 PlayerRig 프리팹을 함께 저장했다. 전방 빔·BodyGlow·퍼짐 반경2.4m·그림자 끔·환경 조명·가방 재질은 유지한다. 세기만 .25로 줄인 후보에도 밤 가방 얼룩이 남아 높이 보정을 채택했다.
- 검수: 처음 에디터는 정지 상태였고 Systems에서 검수 Play를 시작했다. 코드 재컴파일 성공 후 프리팹 저장·재실행, 실제 마을 플레이어 앞뒤 낮밤 재촬영. 가방 윗면의 자체 발광 같은 얼룩이 사라지고 기본 갈색 면이 읽힘을 자체 확인했다. 낮밤·실내 .15배·등급2배·월드 높이·신규 등 생성 검사 통과, GameLit 오류0. 자료는 `ArtWork/BackpackLight62`. 실내는 배율 로직 검사이며 하이드아웃 실제 화면 재검수는 아니다. 연속 이동/모든 장비 등급의 시각 검수는 별도이며 사용자 최종 외형 승인은 아직 받지 않았다.

## 2026-09-11 — 분위기 1차 결과 사용자 피드백 / 보완 진행

- 질문: “지금 화면에서 가장 아쉬운 느낌은 어느 쪽인가요?” 선택지: 전체적으로 밋밋하고 분위기가 약함 / 색감이나 조명이 원하는 느낌과 다름 / 건물·바닥이 단순하고 장난감처럼 보임.
- 사용자 선택·결정: “아직 좀 아쉬운데” → **“전체적으로 밋밋하고 분위기가 약함”**. 직전 분위기 보정 결과는 사용자 최종 승인 상태가 아니며 추가 보완한다.
- 저장 질문: 실행 중인 씬 저장을 Unity가 거부하여, 위치·시간대를 기록하고 저장 후 재실행할지 확인(선택지: 저장 후 플레이 다시 실행 / 지금 플레이 유지하고 비교 결과만 보기). 사용자 결정: **“저장 후 플레이 다시 실행”**. 진행 중 상호작용까지 완전 복원되는 것은 아님을 안내했다. 이는 저장·재실행 승인이고 외형 최종 승인과 별개다.
- 자체 비교 관찰: `ArtWork/Atmosphere62/Saved_Town_Night.png`는 가로등 아래 밝은 바닥에 비해 건물·길 사이 중간 명암이 약하다. 승인 기준 `ArtWork/HideoutReview/InGame.png`의 물체 형태·재료 구분·따뜻한 초점이 마을에서 충분히 읽히지 않는다. 실내/야외 및 카메라 방위 차이가 있으므로 동일한 화면으로 간주하지 않는다.
- 채택값(자체 검수 기준, 사용자 최종 외형 승인 전): 밤 주광 `.22→.38`, 색 `(.62,.50,.85)→(.66,.58,.85)`, 각도 `(60,200,0)→(58,-35,0)`로 카메라 쪽 입면을 읽게 한다. 주변광 `(.13,.10,.19)→(.20,.17,.27)`, 안개 밀도 `.04→.022`로 건물·길 사이 중간 명암을 살린다. 기존 보랏빛 안개색 유지. 첫 후보의 밤 고도 48°는 긴 그림자 때문에 58°로 수정했다.
- 조명 초점: 가로등 6개 밤 강도 `18→14` / 범위 `7.5→8` / 색 `(1,.78,.53)`로 밝은 바닥 원이 압도하는 인상을 줄였다. 전당포 현관등 밤 `4.8→7.5` / 범위 `5→6.5` / 색 `(1,.75,.48)`, Soft 그림자를 켜 입구·문턱에 초점을 만든다. 씬과 재사용 프리팹에 저장했다. 낮 강도는 각각 6/2.4이며 범위·색·그림자는 낮에도 적용된다. 낮 WeatherData·공용 Volume·GameLit 소스·재질·모델은 유지한다.
- 저장·재검수: 사용자 승인 후 Play 종료 → MCP로 원본 씬/프리팹/WeatherData 저장 → Systems Play → 정상 Safehouse 전환 → 원래 `(45.56,0,32.736)` 위치·방향·낮 복원. 저장 후 62° 전당포·마을·관리인·실내 낮밤 재촬영. 조명 7개 낮밤/재활성/야간 프리팹 생성 통과, 기존 게임플레이·콜라이더 직렬화 불변, GameLit 오류 0·누락 스크립트 0·임시 카메라 0·조회 시 콘솔 오류 0. `ArtWork/AtmosphereDepth62`에 비교·재현 코드·검증 기록 보존.
- 남은 부분: 넓고 반복적인 포장과 단순한 건물 덩어리의 밋밋함은 조명만으로 해소하지 못했다. 다음 아트 검토의 주요 대상이며 새 모델/배치의 구체 결정은 하지 않았다. 공용 밤 WeatherData의 다른 레이드 씬 영향, 현관 Point Light 그림자(최대 6면) 추가 비용·연속 이동 그림자는 미검증. 기존 AnomalyFog 누락 경고 해결 작업은 포함하지 않는다. 자체 검수 완료와 사용자 최종 외형 승인은 별개다.

## 2026-09-11 — 집 PC에서 마을 분위기 보정 재개

- 질문·사용자 결정: “하던 셰이더 작업 이어해볼까”. 직전 확인한 ‘마지막 아트 마감에서 이어서 분위기를 먼저 잡는다’는 방향으로 진행한다. 별도 선택지 없음.
- 비교: Systems에서 시작한 검수 Play → 기존 SceneTransitionManager로 Safehouse 진입. 같은 62° 카메라에서 기존값, 조명만 보정, 조명+색보정, 기존 가로등·현관등 보강을 비교했다. 야간 보랏빛/안개를 바꾼 초기 후보는 선택하지 않았다. 실내는 지붕을 숨길 때 실내등도 켜서 외부에 있을 때의 소등 조건과 구분한다.
- 선택한 구현값(사용자 최종 외형 승인과 별개): 낮 주광 .8→.95, RGB `(1,.90,.79)`→`(1,.93,.84)`, 주변광 `(.34,.35,.37)`→`(.29,.32,.35)`. 카메라 62°/0°, 낮 태양 58°, 밤 WeatherData 전체는 유지한다. 공용 Volume 대비 10→14, 채도 -8→-6, 노출 0→+.10EV, 블룸 .18→.15, 비네트 .20→.16. Neutral 톤매핑·필름 그레인·재질/텍스처·SSAO는 유지한다.
- 마을 기존 가로등 6개: 낮 강도 6 / 밤 18, 범위 6→7.5m, 색 `(1,.86,.70)`→`(1,.80,.57)`. 전당포 현관등: 낮 강도 2.4 / 밤 4.8, 범위 4.2→5m; 재사용 프리팹에도 맞췄다. 실내 전구 강도 4와 출입 점등 규칙은 유지한다. 현관등 7의 후보는 문 가장자리의 과한 밝기 때문에 낮췄다.
- 자체 재검수 수정: 보강한 실용등을 낮에도 유지하니 관리인 주변에 노란 과조명이 생겼다. `PropLight3D`를 가로등 6개와 현관등에 연결하여 **가로등 낮 6 / 밤 18, 현관등 낮 2.4 / 밤 4.8**로 정정한다. `OnPhaseChanged` 구독으로 전환하고, 새 맵 로드/재활성 시 현재 페이즈를 동기화한다. Start에서 에디터 값을 덮어쓰거나 매 프레임 폴링하지 않는다. 에디터에 구운 기본 강도는 낮 값이다.
- 범위·상태: 셰이더 계산 오류는 이번 진단에서 확인되지 않아 GameLit 소스는 변경하지 않았다. 조명/후처리로 분위기를 조정한 단계다. 비교 자료·재현 스크립트는 `ArtWork/Atmosphere62`.
- 저장 후 검수 완료: Systems Play에서 정상 Safehouse 전환 후 재촬영, 7개 조명의 낮→밤→낮/야간 재활성/야간 프리팹 신규 생성 검사 통과. 카메라 62°·HDR·볼륨 마스크/override 정상, GameLit 오류 0·누락 스크립트 0·임시 카메라 잔재 0·조회 시 콘솔 오류 0. 기존 콜라이더/게임플레이 직렬화 불변 검사 통과(신규 PropLight3D 제외). 낮/밤 실제 Main Camera와 착용등 화면 자체 검수 완료. 새 이동·거래·귀환 QA나 GPU/연속 이동 SSAO 검수 완료를 뜻하지 않으며, 공용 낮 조명/Volume 변경의 다른 씬 검수는 남아 있다. 기존 `BRB/AnomalyFog` 누락 경고는 반복되며 이번 작업에서 해결하지 않았다. 자체 검수 완료와 사용자 최종 외형 승인은 구분한다.

## 2026-09-11 — 마지막 아트 마감 이후 분위기 작업 재개

- 질문: “셰이더랑” 뒤에 이어서 손볼 대상이나 현재 화면에서 고치고 싶은 부분 확인. 선택지 없음.
- 사용자 선택·결정: “우리 작업한거 마지막 부분이어서 하자는말이였어 우리 분위기머저 잡아보자는거지”. 마지막 아트 마감 상태에서 이어서 전체 분위기를 먼저 잡는다.
- 진행 기준: 기존 저채도·낡은 아트와 62° 카메라 기준으로 전당포 주변 조명·색감·재질의 낮밤 화면을 비교한다. 구체 보정값은 자체 검수 후 확정하며, 이번 재개 요청만으로 새 시각 기준이 승인된 것은 아니다.
- 당시 재개 상태: `3024421f` 병합본에서 시작해 CLI·MCP·스킬 연결을 먼저 완료했다. 이후 분위기 보정·자체 검수 결과는 이 문서 최상단의 후속 항목을 따른다.

## 2026-09-11 — 원본 표면 질감 보완 착수

- 사용자 질문·승인: 1차 재질 보정에 “나쁘진 않네. 다음 단계 해볼까”로 후속 표면 질감 작업을 승인했다. 직전 제안은 옷 주름·봉제선, 목재 결, 국소 마모 보완이었다.
- 진행 범위: GPT 관련 원화와 기존 UV/텍스처를 먼저 대조하고, 기존 모델의 천·목재·금속 표면을 소규모 기준 구역에서 개선·비교한다. 승인된 62°/58° 시점·주광 및 직전 재질 기준을 유지한다. 대량 프랍 배치나 전투/스폰 로직 변경은 포함하지 않는다.
- 제작·검수 기준: 재질 특성이 게임 거리에서 읽히는 중간 크기의 결·봉제·국소 마모를 우선한다. 얼굴/UV 경계·실루엣·리그는 보존하며, 전체에 흙 얼룩이나 노이즈를 덮지 않는다. 결과는 별도 파일로 준비하여 Unity MCP에서 자체 검수 후 선택한 것만 적용한다. 진행 중 Play는 유지한다.

### 적용·자체 검수 결과

- 참조: `Assets/GPT/NPC/NPCALL7_1.png`의 관리인 의상과 `Assets/GPT/안전구역/주인공집/ChatGPT Image 2026년 6월 10일 오후 06_13_26.png`의 목재/철재 표현, 기존 관리인 UV 아틀라스, 승인된 `ArtWork/HideoutReview/InGame.png`를 확인했다. 배치·형태는 바꾸지 않고 표면 특징만 반영했다.
- 적용: 신규 BaseColor 3장을 `Assets/Art/SurfaceDetail62/Textures`에 보존하고 Town_Wood/Town_WoodDark/Pawnshop_Counter, Town_Steel, DistrictWarden의 총 5개 재질에 연결했다. 목재는 중간 크기 나뭇결, 철재는 절제된 긁힘·방향성 결, 관리인은 의상 주름/봉제와 머리카락 결을 보완했다. 균열·타이어도 공유하는 Town_Edge는 제외했다. 기존 노멀/금속·매끄러움 마스크와 리그/UV 메시, 다른 캐릭터는 유지한다.
- 보정: 새 목재·철재 이미지가 의도보다 어둡게 생성되어 RGB별 선형 평균 비율로 재질 색을 보정하고 재촬영했다. 초기 임의 스칼라 보정은 폐기했다. 구체 수치·원본 참조·프롬프트는 `ArtWork/SurfaceDetail62`에 기록했다.
- 검수: Play를 유지한 Safehouse 실제 런타임 오브젝트를 임시 62° 카메라로 전후 비교하고, 저장 후 관리인/전당포/마을/철재를 재촬영했다. 주광·메인 카메라·플레이어 위치·씬을 변경하지 않았다. 재질 5개 텍스처 참조·sRGB·mipmap·노멀/마스크 유지 검사 통과, GameLit shader 오류 0/Unity console 오류 0/임시 카메라 잔재 0.
- 판단·제한: 목재 카운터와 관리인 의상에서 단색 덩어리 인상이 완화됐다. 철재 변화는 절제되어 있다. 관리인 얼굴은 픽셀 동일 복원이 아니며 색상 소폭 차이가 있지만 검수 시 표정·UV 배치의 뚜렷한 깨짐은 보이지 않았다. 출력은 1254²/비압축 검수용이며 최종 플랫폼 압축·야간·이동 검증은 별도다. 새 표면 요철을 노멀에 추가한 단계는 아니다. 이 자체 검수와 사용자 최종 컨펌은 구분한다. 전 캐릭터/전 환경에 질감 작업이 완료됐다는 뜻은 아니다.

## 2026-09-11 — 마을 바닥·프랍 작업 전 셰이더 우선순위 재검토

- 사용자 질문: “1, 2 하기 전에 우리 게임 셰이더 수정 안 해도 되나?” 앞서 제안한 1=바닥 정리, 2=생활 프랍 배치. 선택지 제시 없음.
- 사용자 결정: 후속 “약간 게임이 찰흙 같아서 순서대로 해보자”로 **재질 차이 → 입체감 → 낮밤** 순서의 원인 검토·셰이더/재질 보정을 승인했다. 마을 바닥·프랍 대량 배치 전에 기존 자산으로 비교 검수한다.
- 승인 후 진행 기준: 찰흙 같은 균일한 표면을 줄이되 승인된 저채도·낡은 아트와 62° 카메라/58° 주광을 유지한다. 과한 광택이나 전면 노이즈로 질감을 대신하지 않는다. Unity 작업은 MCP로 하며 진행 중 Play를 보존한다. 세부 구현 수치는 전후 검수 후 기록한다.
- 답변·제안: 현재 GameLit의 기본색/노멀/패킹 마스크·조명/그림자 표현은 적용되어 있으나 최종 완성 판정은 아니다. 마을 전체 배치 전에 기존 바닥·벽·캐릭터를 한 구역에서 비교해 재질 차이, 입체감, 낮밤 가독성을 검수하고, 발견한 원인이 셰이더 계산이면 그 부분을 보정한다. 셰이더/재질 기준 확인 → 작은 구역 바닥·프랍 완성 → 마을 확장 순서를 권장한다. 현재의 넓고 균일한 지면 인상을 셰이더만의 문제로 단정하거나, 프랍 배치만으로 모든 질감 문제가 해결된다고 보지 않는다.
- 완료 기준 제안: 목재/금속/콘크리트/천이 과한 반짝임 없이 구분되고, 62°에서 캐릭터와 배경이 어울리며, 낮밤에 얼굴·의상·바닥 명암이 과하게 뭉치지 않는지 확인한다. 실제 비교 전 구체적인 셰이더 변경 범위·수치는 미정이다.

### 승인 후 적용 — 재질·입체감 1차 보정

- 원인: Town02 기본 18개 GameLit 재질은 연결된 표면 마스크가 비활성이고, 철재도 metallic=0/specular off였다. 현재 마을 25개와 NPC 4개의 마스크 R/A를 실제 사용하도록 통일했다. 마스크 29개 참조·Linear 검사 통과. 노멀은 재료별 .35~.65, 마을 때 .09~.12/그늘 탈색 .18. 건조한 지면·벽·천의 직접 반사는 끄고 목재·철재·도장 금속은 켰다. 환경 반사 off 유지.
- NPC 4명: 노멀 .6 / 때 .07 / 그늘 탈색 .14 / 림 .012. 실제 사용 중인 플레이어·근접/권총/소총 밴딧의 몸·장비·무기 6개 재질은 때 .10 / 탈색 .18 / 림 .018로 조정했다. 홀스터 smoothness .5→.2. 몸체 마스크나 원본 텍스처를 새로 만들지는 않았다.
- 입체감: 기존 SSAO를 intensity .4, radius .18m, direct .1, falloff 60m, full-resolution/DepthNormals로 활성화했다. GameLit DepthNormals 패스도 노멀맵을 읽는 URP LitDepthNormalsPass로 맞췄다. 카메라·주광·그림자 길이·날씨 값은 유지한다.
- 재발 방지: GameLitConverter는 이미 GameLit인 재질의 검수값을 보존한다. 신규 URP/Lit 전환 시 활성 metallic mask와 금속 곱셈값 1을 유지한다. 임시 재질로 mapped/unmapped 전환 및 재실행 보존 3개 검사 통과. C# 재컴파일 완료, GameLit compiler 오류 0, 임시 카메라 잔재 0 확인.
- 자체 검수: `ArtWork/MaterialReview62`의 동일 62° Unity Editor 낮밤 조명 비교, 재질 구/상자 비교, 플레이어·밴딧·관리인 시각 프리팹 비교 완료. 밤 테스트 렌더에는 착용등을 넣지 않아 실제 플레이보다 어둡다. 캡처 종류·재현 코드·상세 제한은 해당 README를 따른다.
- 런타임 재검수: 정지된 에디터에서 검증 Play를 시작해 관리인·회수꾼 실제 런타임을 임시 카메라로 재촬영했다. shader compiler/Unity console 오류 0. 실제 게임 카메라 캡처 중 플레이어/카메라가 지면 밖 원점 근처로 이동한 별도 문제를 발견했다. SSAO를 꺼도 동일하고, 마을 내부 카메라 렌더는 정상이다. 위치 변경 원인은 미해결로 남기고 SSAO를 복원했다. 플레이어 이동/씬 전환 명령은 보내지 않았다.
- 판단: 잘못된 재질 연결과 과한 림/탈색을 고쳤지만 변화는 절제된 수준이다. 단순한 색면·매끈한 곡면·낮은 주파수의 표면 얼룩은 여전히 찰흙 느낌을 만든다. 원화 수준의 옷 주름/봉제선/국소 마모를 셰이더만으로 해결했다고 보지 않는다. 후속은 원본 표면 질감 보완이며, GPU 비용·이동 시 SSAO 안정성·실전 야간 가독성은 별도 검증 대상이다.

## 2026-09-11 — NPC 그림자 피드백

- 사용자 질문·피드백: 인게임 NPC 캡처와 함께 “그림자도 이상하네”. 선택지 없음.
- 사용자 결정: 그림자 문제 확인·보정 요청으로 진행한다. 진행 중 전투 플레이는 보존한다.
- 확인: NPC 4명의 기존 캡슐 Renderer는 비활성이고 실제 스킨만 그림자를 만든다. 낮 태양 고도 38°와 40m/2-cascade 그림자가 길고 뭉개지는 표현에 영향을 준다. 수정 전후 동일 62° 카메라로 비교했다.
- 적용: 공용 WeatherData 낮 태양 `(58,-30,0)`; 높이 1.9m 기준 지면 투영 길이 약 2.43m → 1.19m. URP-3D는 4 cascades, split `(.35,.60,.85)`, depth bias `.5`, normal bias `.25`. 거리 40m/주광 shadowmap 2048/Soft 유지. 밤 광원 각도·강도는 보존하며 공용 캐스케이드/바이어스는 밤에도 적용된다.
- 검수: `ArtWork/ShadowReview62/Before.png`와 `After.png`는 실제 Unity 씬의 임시 62° 카메라 렌더. 임시 카메라·주광·주변광·파이프라인 비교 상태를 모두 복원한 뒤 선택한 WeatherData/URP 설정만 MCP로 저장했다. 이전 2 cascades보다 그림자 렌더 패스가 늘어나므로 GPU 성능 실측은 별도 과제다.
- 재검수: 정지 상태에서 검증 Play를 시작해 실제 낮 태양 58°/강도 .8, 게임 카메라 62°/0°, 캐스케이드/바이어스 적용 및 GameLit 컴파일 오류 0을 확인했다. 런타임 관리인·회수꾼을 게임 카메라 설정을 복사한 임시 카메라로 촬영했다. 원래 플레이어/카메라는 이동하지 않았다. 검수 이후 Zone1로 전환되어 해당 플레이를 종료하지 않았다. 밤 재촬영·이동 중 캐스케이드 전환/GPU 실측은 이번 검수 범위 밖이다.

## 2026-09-11 — NPC 적용 후 표현 작업 우선순위 검토

- 사용자 질문: “너가 보기엔 이제 셰이더 작업해야 할까”. 선택지 제시 없음.
- 사용자 선택·결정: 후속 “진행해보자”로 **전당포 실내·야외 NPC 구역의 조명/색감 → 재질 구분 → 필요한 GameLit 개선 및 전후 검수**를 승인했다. 따뜻하고 낡은 기존 아트 방향과 62°/0° 카메라를 유지한다. 세부 수치는 비교 검수를 통해 정하며 최종 외형 컨펌과 구분한다.
- 검토 근거: `ArtWork/StoryNPC62/UnityIntegration`의 실제 Unity 62° 캡처에서 피부·의상·바닥이 황갈색으로 수렴하고, 야외 NPC의 어두운 머리·옷 구분이 약하며, 전당포 바닥·카운터는 밝고 균일한 면으로 보인다. 조명·색보정·재질 영향의 분리는 아직 비교 실험 전이다.
- 당시 제안: 동일 시간대의 조명·노출·색보정을 기준화하고 피부/천/목재/금속의 재질 차이를 조정한 뒤 필요한 GameLit 개선을 적용한다. 제안 당시 NPC Mask는 셰이더 슬롯 부재로 미연결이었고, 아래 승인 후 적용 단계에서 연결을 완료했다.

### 승인 후 적용된 표현 기준

- 낮 햇빛 `(1,.90,.79)`/강도 `.8`, 주변광 `(.34,.35,.37)`. 따뜻한 주광과 중립 주변광을 나눠 갈색 겹침을 줄였다. 밤 WeatherData는 그대로이며, 낮 태양 각도는 후속 그림자 피드백에 따라 위의 58° 기준을 적용한다.
- 공용 `PlayerRigVolume3D`: Neutral 유지, 노출 `0`, 대비 `10`, 채도 `-8`, 블룸 `.18`/문턱 `1.1`, 비네트 `.20`, 그레인 `.06`. 색조는 기존 원칙대로 조명에서 만든다.
- GameLit `_GAMELIT_PACKED_MASK` 선택 지원: `_MetallicGlossMap` R=금속성/A=매끄러움 × 기존 스칼라. 기본 꺼짐으로 기존 재질 계산을 보존한다. NPC 4명과 새 Town02/Rendering62 재질에 연결했고, **NPC Mask 미연결 상태는 해소했다**. 노멀 `.35`, 때 `.12`, 그림자 탈색 `.20`, 림 `.045`.
- 전당포 바닥/카운터의 단색 임시 재질을 기존 콘크리트/목재 텍스처 기반으로 교체했다. 바닥 반복 `8×6`, 카운터 `4×1`, 지면 `20×14`, 도로 약 4m 주기로 과한 텍스처 늘어짐을 줄였다.
- 기존 전당포 전구는 강도 `4`/범위 `9`/색 `(1,.88,.74)`, 거리 PracticalLight 6개는 강도 `6`/범위 `6`/색 `(1,.86,.70)`. 야간 확인 후 전구 8의 낮 과노출을 낮춰 재검수했다.
- 실제 게임 카메라(HDR/후처리 켜짐)의 62° 낮·밤 비교는 `ArtWork/RenderingReview62`. 새 이미지나 모델은 제작하지 않았고, 의상/소품 마스크와 기존 환경 텍스처를 활용했다. 공용 WeatherData/Volume 변경은 다른 게임플레이 씬에도 반영되며 상세 검수 구역은 전당포와 야외 NPC 구역이다.
- 검수 범위: 최종 저장·재시작 후 낮 화면 재확인, shader compiler/콘솔 오류 0, 마스크 11개 참조·Linear 임포트·프로필 값·NPC 대기 클립 검사 통과. 밤은 전당포 전구 8에서 중간 검수했고, 최종 4로 낮춘 뒤 낮 과노출을 재확인했다. 최종 야간 재촬영 직전 Zone1로 전환되어 전투 플레이를 보존했다. **전구 4 기준 최종 밤 화면 재확인은 남아 있다.**

## 2026-09-11 — 하이드아웃 인게임 표현 보정

- 질문·제안: 캐릭터가 지나치게 밝고 방이 차갑게 보이는 하이드아웃 시안을 조정할지?
- 사용자 결정: 인게임 평가 후 “수정해줘”로 비율·조명·생활감 개선 승인.
- 반영: 하이드아웃의 따뜻한 조명 아래에서는 `WornLamp.SetIndoorPresentation(true)`로 빔·주변광·몸 보조광의 강도를 0.15배로 낮춘다. 상시 점등과 랜턴 등급·낮밤 반응은 유지하며, 하이드아웃 종료 시 현재 낮밤·등급 기준 밝기로 복원한다. 씬 조명값과 검증 결과는 [hideout-3d.md](hideout-3d.md)에 기록한다.

## 2026-09-07 방향 결정

- 질문: 현재 2D 프로젝트를 3D 쿼터뷰로 만들 수 있을까?
- 사용자 결정: 기존 프로젝트의 3D 쿼터뷰 전환을 희망하며, 캐릭터 모델·애니메이션부터 제작한다.
- 1차 산출: 치비 생존자 모델·Idle/Walk/Run + **플레이어 표시만 3D로 교체**(`ChibiPlayerVisual`). 캐릭터 제작 기준은 [char-art.md](char-art.md), 패키지는 [chibi-survivor-3d.md](chibi-survivor-3d.md).

### 전환 방향 확정 (2026-09-07 2차)

| 질문 | 결정 |
|---|---|
| 전환 강도 | **완전 3D** — XZ 평면 + Rigidbody/3D 콜라이더 + URP 3D Renderer + 실제 조명·그림자 |
| 카메라 | **고정 쿼터뷰 · 오소그래픽** (퍼스펙티브·자유회전 폐기) |
| 높이 | **단차·엄폐까지.** 바닥은 평면, 2층·계단·옥상은 이번 범위 밖 |
| 아트 | **전부 3D 재제작**(적·NPC·프랍 63종). 아이템 아이콘 204종은 UI라 유지 |

> ✅ **3D 전환 완료(2026-09-07~09, Stage 0~4).** 지금 게임은 **URP 3D**로 돈다 — 계획·실측·남은 2D 잔재는 **[`3d-migration.md`](3d-migration.md)** 가 SSOT.
> 현재 구현은 바로 아래 「현재 구현 (3D)」 표. **그 아래 「한눈에」부터의 URP 2D 본문은 2026-06~09 2D 시절 기록**이다(3D 전환으로 대부분 폐기).

## 현재 구현 (3D) — 2026-09-11

| 항목 | 현재 |
|---|---|
| **렌더 파이프라인** | URP 3D · Forward (`Assets/Settings/URP-3D.asset` + `ForwardRenderer`) |
| **카메라** | 오소그래픽 · **62° 내려봄 · 방위 0°(정면 탑다운)** — `CameraFollow.viewPitch/viewYaw`. 결정 과정은 §쿼터뷰 카메라 |
| **좌표계** | 월드 = XZ 평면, 높이 = Y. 평면 로직은 `Vector2`(x=X, y=Z)로 두고 `Plan3D`에서 변환 |
| **이동/충돌** | Rigidbody + 3D 콜라이더. 이동은 화면 기준(`TopDownPlayer.CameraRelative`) |
| **셰이더** | 게임 전용 **`BRB/GameLit`** 1종(캐릭터+환경 공용, 어두운 사실풍) — §게임 전용 셰이더. 머티리얼 전환 = `GameLitConverter` |
| **후처리** | PlayerRig Volume — 색보정·톤매핑 + 블룸 + 비네팅·필름 그레인 |
| **조명** | 태양(`SunLight`) + 낮밤 `DayNightCycle`(색·세기 = `Resources/Data/WeatherData.asset` — 낮 주황 / 저녁 보라) + 착용등 `WornLamp`(앞쪽 빔 + 짧은 주변광) |
| **가시성** | `PlayerVision` — 시야 콘 밖 적 숨김, 눈높이 3D 시선 판정 |
| **가림 처리** | 화면공간 컷어웨이(3d-migration Stage 0 결정) |
| **없는 셰이더** | `BRB/AnomalyFog`·`VisionDarkness`·`SpriteFlash`·`DamageOverlay`·`WallPixel` — 코드가 찾지만 파일이 없다(정리 5단계) |


> **(2D 시절 기록)** 2026-06-02 **아이소메트릭(2.5D 하이브리드 3D) → 순수 탑다운 2D**로 전환했었다 — 2026-09 3D 재전환으로 아래 대부분이 폐기됐다.
> 전환 배경·단계는 [`topdown-migration.md`](topdown-migration.md) 참고. 아트 생성 스펙은 [`topdown-art-spec.md`](topdown-art-spec.md).

## 한눈에 (2D 시절 — 폐기, 기록)

| 항목 | 결정 |
|---|---|
| **렌더 파이프라인** | URP **2D Renderer**(Renderer2D). `URP-2D.asset` |
| **카메라** | **2D Orthographic, Z축 정면 직시** (3D 틸트 없음) |
| **원근감** | 카메라가 아니라 **스프라이트 아트에 ~80° 틸트를 베이크**(near-overhead, 레퍼: Darkwood). 정면/측면이 살짝 보임 |
| **오브젝트 배치** | 타일·프롭·캐릭터 모두 **회전 (0,0,0)** (순수 2D, 스프라이트가 카메라 정면. 빌보드/눕힘 없음) |
| **좌표계** | 월드 = **XY 평면**, 깊이 = **sortingOrder** (정사영이라 Z는 화면에 안 보이고 정렬용) |
| **이동/충돌** | **Rigidbody2D + Collider2D** (NavMesh 폐기) |
| **조명** | URP **2D Light**(Light2D) — 글로벌(앰비언트) + 시야 콘(플레이어 FOV) |
| **가시성** | **좀보이드식 시야(FOV)** — 적은 플레이어 바라보는 부채꼴 밖이면 안 보임/어둑. 손전등 폐기 |
| **빛 차폐** | **ShadowCaster2D** (벽/구조물에 부착, Light2D·시야 차단) |
| **맵** | **Unity Tilemap**(바닥/벽) + TilemapCollider2D + CompositeCollider2D, 프롭은 **Prop2D 카탈로그** |

## 왜 탑다운 2D인가

- 아이소(2.5D 하이브리드)는 깊이정렬·접지·2D-in-3D 이질감을 오래 싸웠고, AI로 아이소 모듈 스프라이트를 뽑으면 투영각이 매번 달라 맞물리지 않음(2인 제작 비현실).
- 탑다운 2D는 Unity 네이티브 2D(Sprite/Rigidbody2D/Tilemap/Light2D)로 단순·견고. 각도/정렬/떠보임 문제가 원천 소멸.
- 입체감/높이감은 약간 포기하고 **명료함·제작 용이**를 택함. 분위기는 **시야 콘 + 어둠 + 밀도**가 책임.

## 카메라 / 좌표계

- ~~카메라 = 2D 정렬축(`CameraSortSetup`)~~ **폐기(2026-09-09)** — URP-3D 전환으로 `TransparencySortMode`는 Default(카메라 거리)다. `CameraSortSetup`은 삭제됨.
- 모든 위치/투영/그림자 계산은 **XY 기준**. (구 "XZ 바닥평면 + 90° 눕힌 쿼드" 전제는 폐기.)
- **모든 스프라이트(타일/프롭/캐릭터)는 회전 (0,0,0)** — 카메라 정면을 향하는 순수 2D. 원근은 아트가 담당하므로 트랜스폼 회전·빌보드 불필요.
- **플레이어 카메라 = PlayerRig 프리팹에 포함, 한 세트(2026-06-02 결정 변경).** ~~씬 카메라 자동 장착~~ → **카메라+플레이어+라이트+후처리(Volume)를 `Resources/PlayerRig.prefab` 한 세트로 묶어 DontDestroyOnLoad**로 모든 씬 공유. Bootstrap이 1개만 스폰. `CameraFollow`가 씬 로드 시 **자기(PlayerRig 카메라) 외 다른 Camera/AudioListener를 비활성**해 2개 충돌을 막음. (추적+셰이크/줌·후처리·피격 Volume 모두 프리팹 카메라에 내장.)
  - 빌더 `TopDownPlayerBuilder`(메뉴 `Tools ▸ TopDown ▸ Build ▸ Player Rig`)가 PlayerRig 전체를 코드 조립.
  - → 어느 씬(InGame/Safehouse/MapTool2D)에서 Play해도 동일한 카메라·조명·후처리로 동작.
  - **씬 빌더는 카메라를 안 만든다**(PlayerRig가 제공). 3D에선 깊이가 정렬을 맡으므로 별도 정렬축 설정이 없다.
  - **글로벌 Light2D의 주인 = Systems 부트 씬**(0.22 어둠). PlayerRig는 플레이어 점광(point)만 들고 오므로 글로벌 앰비언트가 없으면 URP 2D가 빛 반경 밖을 **새까맣게** 렌더 → 글로벌이 필요하지만, **게임플레이 씬(InGame/Safehouse)은 글로벌을 안 만든다**(Systems가 공급, 씬은 맵/프롭/스폰만). **자체 글로벌을 갖는 씬은 Systems + MapTool(밝게) + CombatSandbox(테스트)뿐.** 글로벌이 2개 이상 활성이면 URP가 `More than one global light on layer ...` 경고 → **런타임은 `SystemsSceneEnforcer`, 에디트 모드는 `SystemsGlobalLightEditorEnforcer`**(Systems 로드 시 그 글로벌만 남기고 나머지 비활성)가 중복을 막는다.

## 조명 / 가시성 (시야 FOV) — 2026-06-02 전환

> **손전등(주광원) 개념 폐기 → 좀보이드식 시야(FOV).** 기존 손전등 코드(`FlashlightController`/`FlashlightBeam` 셰이더/손전등 Light2D)는 **완전 제거 후 시야 시스템 신규 작성**.

- **가시성 모델 = 하이브리드** (확정):
  - 평소엔 적당히 보이되, **멀거나 그늘·실내·밤**은 어둑.
  - 플레이어가 **바라보는 방향 부채꼴(시야 콘) 밖의 적은 안 보임/어둑** — "어디서 적이 튀어나오나"의 긴장.
  - 가시성은 **지역/시간대별로 다름**(`WorldRegionCatalog`/`RegionTimeManager`/`DayNightCycle` 연동).
- **시야 콘 구현 방향**: 플레이어 facing(마우스 방향) 기준 부채꼴 Light2D(또는 시야 마스크). `TopDownPlayer.FacingDirection`으로 회전. 적 가시성은 시야 콘 안/밖 판정으로 sprite 알파·표시 토글.
- **글로벌 Light2D**: 앰비언트(밤·실내는 어둑, 낮·야외는 밝게). 하이브리드라 intensity는 지역/시간대별 가변.
- **짙은 현상 ≠ 낮/밤 (✅ 2026-06-04, 풀스크린 오버레이)**: 짙은현상([gdd-core §5.1](gdd-core.md))은 **시간 무관** 빛·공간 침식("어두움 = 시간대가 아니라 침식 정도", 낮에도 밤처럼). **낮/밤과 완전 독립**으로 구현:
  - **`DenseAnomalyController`**(싱글톤) → 카메라 앞 **풀스크린 fog 쿼드**(`BRB/AnomalyFog`) 생성. 화면 위에 어둠+안개를 알파로 덧씌움 → `DayNightCycle`의 글로벌 Light2D를 **안 건드림**(충돌 0, 최종 화면 = 낮밤 × 현상). **낮에도 밤처럼** 어두워짐.
  - 셰이더: **노이즈 주도 패치 안개**(2겹 fbm 드리프트, 월드 앵커) — 방사형 원이 아니라 불규칙 결이라 "동그란 vignette"로 안 보임. 플레이어(`_ClearCenter`) 주변 소프트 클리어를 **노이즈로 경계 깸**(원 안 보이게) + 멀수록 살짝 더. **어두운 톤**(밝은 원반 X) + 미세 밝은 wisp 결(밤엔 움직임으로 인지). `_Density`로 강도, `_FogColor`/`_HazeStrength`/`_NearClear`/`_FarFull`/`_NoiseScale` 튜닝. ⚠️ 옛 방사형 거리 falloff(`_ClearRadius`·edge항·`_EdgeDark`)는 원/도넛 링으로 보여 폐기.
  - 강도 = 수동(`SetIntensity`/이벤트) **max** 지역 기본 침식도(`WorldRegionCatalog.anomaly` 0~1, 5지역 그라데이션: 폐상가 0.1·침묵생활 0.2·묻힌정비창 0.4·기억의극장 0.65·중앙영야 0.95). 부드럽게 lerp. 테스트 토글키 `G`.
  - 범위 = **지역/씬 단위**(활성 지역 anomaly) **+ 구간(`AnomalyZone`)**: 트리거 영역에 플레이어가 들어가면 그 강도로 현상 발동 → 전체 맵이 아니라 골목·건물 등 **구간별 지정**(겹치면 max, `DenseAnomalyController._zones`). 메뉴 `Tools▸TopDown▸Map▸Create Anomaly Zone`(6×6 트리거 생성).
  - 참고(레거시): `WeatherData`의 fog는 `RenderSettings.fog`(3D)라 2D 미적용=死, `PostProcessController.Anomaly`는 후처리 룩(시간에 묶임)이라 현상과 별개.
- **라이트 모양 = 텍스쳐 쿠키(Sprite 라이트).** 플레이어 주변광/콘·프롭 발광 모두 **절차 생성 쿠키**(`LightCookieGenerator` → `Resources/LightCookies/cookie_radial`·`cookie_cone`)를 **Sprite Light2D**에 물려 부드럽게(다크우드). Point/Spot 파라미터 콘(딱딱한 경계)은 폐기. 쿠키는 흰색+알파(모양), 빛 색은 `Light2D.color`로 틴트. 콘 쿠키는 +Y 기준·피벗 하단(꼭지)이라 `lightAngleOffset=-90`로 facing에 맞춤. 프롭 발광은 Prop2D 카탈로그(`emitsLight`)로 프롭별 설정(램프/창문/네온), `lightNightOnly`면 밤에만(`PropLight2D`+DayNightCycle).
- **그림자/차폐**: 벽·구조물에 **ShadowCaster2D** → 시야 콘과 빛을 막음(벽 뒤는 안 보임). (3D 스팟라이트 + ShadowsOnly 박스 방식 폐기.)
- 낮/밤 반응 컴포넌트(`DayNightCycle.OnPhaseChanged` 구독): `PostProcessController`, `NeonSign`, `RainController` 등. **Editor State Preservation** 규칙 유지 — `Start()`에서 값을 적용하지 않고 이벤트로만 변경.
- **랜턴 (장비 기반 시야 강화) — 2026-06-05**: 시야(주변광·콘)의 **크기·밝기를 착용 광원 장비로 가변**.
  - **기본(랜턴 미착용)**: 좁은 주변광(원형 쿠키, ~2칸) + 약한/없는 콘 — 밤엔 코앞만 보임.
  - **랜턴 착용**: 주변광 반경↑·밝기↑(예: 4~5칸) + **콘 쿠키 길이·각도·밝기↑**(facing 부채꼴). 주변광·콘 둘 다 동시 강화.
  - **장비 슬롯**: 랜턴 = 착용 장비(장비창 1슬롯). **토글 아님 — 차고 있으면 상시 점등**(구 손전등 F토글/단일 빔 메커니즘 폐기 계승).
  - **등급**: 약한 랜턴 → 밝은 랜턴(반경·콘·밝기 차등) = 시야 성장 축.
  - **3D 구현(2026-09-09)**: `WornLamp`(PlayerRig의 `PlayerLamp3D`). **점광이 아니라 스포트**다 — 점광은 사방을 고르게 비춰 완전한 원을 만들고, 그러면 몸에 단 등이 아니라 **머리 위에 떠 있는 전등**으로 읽힌다(사용자 지적). 바라보는 방향(`FacingDirection`)을 향하고 34° 아래로 숙여 발 앞을 비춘다. **그림자를 켠다** — 2D의 `ShadowCaster2D`가 하던 "벽에서 빛이 끊긴다"를 대신한다. 끄면 빛이 건물 벽을 통과해 골목 밖까지 새고, 그것도 "공중에 뜬 조명" 느낌의 원인이다. 값: 사거리 5.5m · 콘 96° · 밤 1.5 / 낮 0.2(토글 없음, 밝기만 낮밤에 반응). 등급은 `SetGrade(반경배율, 밝기배율)`로 들어온다.
  - **구현**: 착용 랜턴 등급 파라미터로 `TopDownPlayer`의 주변광/콘 `Light2D`(쿠키) `radius/intensity/cone angle`을 세팅(LanternModifier). 미착용 시 기본값으로 복귀.
  - **연료(옵션, 추후)**: 기름·배터리 소모형으로 자원 압박을 줄지 추후 결정(현재 가안 = 착용 패시브 상시 점등).
  - **현상 예고 점멸 (✅ 2026-06-08 기획)**: 짙은 현상 구간 접근 시 착용 랜턴 `Light2D` intensity **펄스**(가까울수록 빠름). 별도 `phenom_meter` 아이템 **폐기**. `DenseAnomalyController`/구간 거리 → `LanternModifier`. S-013·S-020 튜토리얼. [→ navigation.md §4, story-script.md S-013]
  - **빛 = 노출(추후)**: 밝을수록 멀리·넓게 보지만 적에게도 들키기 쉬운 트레이드오프 여지(§적 은신). 추후.

> ⚠️ 데모 폴더명 `demo13-flashlight`는 역사적 이름. **손전등(F토글 단일 빔) 메커니즘은 폐기** — 빛은 시야 FOV + **착용 랜턴**(위)으로 대체.

### FOV 시야콘 구현 1차 (2026-06-19)
> **결정: 좀보이드식 시야콘 — 정면 부채꼴 + 근접 360° 밖의 적은 안 보임(몸체 렌더러 숨김). AI·충돌은 유지(적은 존재하되 안 보일 뿐).**

- **`PlayerVision`**(신규, 부팅 자가생성): 매 프레임(LateUpdate) `EnemyController.All` 순회 → 가시성 판정 → `EnemyController.SetVisionVisible(bool)`(spriteRenderer + HP바 토글). 안전구역은 적이 없어 무영향.
- **판정**: `사거리 안 && (근접반경 안 || 콘각도 안) && (LOS 켜졌으면 벽 안 막힘)`. 콘은 `TopDownPlayer.FacingDirection`(마우스) 기준. LOS = `Physics2D.Linecast`(Player/Enemy/IgnoreRaycast 제외, 트리거 무시 → 솔리드 벽만 차단).
- **튜닝(GameTuning)**: `visionEnabled`(킬스위치) / `visionFovDegrees`(150) / `visionRange`(9) / `visionNearRadius`(2.2) / `visionLineOfSight`(true). Control Panel에서 조절.
- **2026-06-05 '항상 은신 + 말풍선' 실험 정리**: FOV가 가시성 권위가 되며 `EnemySpeechBubble.Enabled` **기본 OFF**(둘 다 bodySprite를 만져 동시 ON 금지). 말풍선을 off-screen 단서로 FOV와 **결합**하려면 EnemySpeechBubble의 body-hide를 떼고 PlayerVision에만 맡기는 방향(후속 옵션).
- **⚠️ 미완(다음 FOV 단계)**: 시각적 어둠 오버레이(콘 밖 어둑/포그 — 현재는 적 몸체만 숨김, 화면 자체는 안 어두움). 지역/시간대별 가시성 가변. 착용 랜턴(Light2D) 연동.

### 적 은신 + 머리 위 말풍선 (2026-06-05, 실험 토글)

> 시야 콘을 "적을 드러내는" 장치로 쓰지 않고, **적은 항상 안 보이게(몸체 숨김) 하되 "말할 때"만 머리 위 말풍선으로 존재를 흘리는** 방향 실험.

- **몸체 = 항상 숨김**: 적(밴딧/몬스터) `EnemySprite` 렌더러를 끔 → 플레이어는 적 몸을 못 봄.
- **말풍선 = "말할 때만"**: 콘 안/밖과 무관하게 적이 말하는 순간에만 머리 위에 뜸. **플레이어를 "만나는"(발견 = 순찰→추격) 순간에만 또렷한 대사**(`encounterLines`), 그 외 평소엔 **순찰 중 근접 혼잣말 중얼거림**(`mutterLines`, 멀면 침묵 → 위치 노출 방지)만. 공격/피격/스턴/사망 전용 대사는 두지 않음.
- **내용 = 콘 의존**: 플레이어 시야 콘 **안**이면 **대사 전문**, **밖**이면 **"..."** 만. 말하는 도중 콘 안/밖이 바뀌면 실시간 갱신.
- **on/off 토글**: 전역 `EnemySpeechBubble.Enabled`(기본 ON). 디버그 키 **B**(DebugTestUI가 단독 폴링) + 전투 탭 버튼. OFF면 몸체 보이고 말풍선 끔.
- **구현**: `EnemySpeechBubble`(적별 컴포넌트, `EnemyController.Awake`가 자동 부착 → 프리팹/씬 수정 불필요). 콘 판정은 **게임플레이용 별도 파라미터**(`ConeHalfAngleDeg=45`, `ConeRange=8`)로 `TopDownPlayer.FacingDirection` 기준 — 실제 라이트 렌더(콘 쿠키)와 분리. 대사는 컴포넌트 SerializeField 풀(데이터화, 적별 교체 가능; 추후 `UnitStatData`/`NPCData`로 이관 여지). 한글은 프로젝트 표준 `LegacyRuntime.ttf`(`TextMesh`+동적폰트 머티리얼).
- ⚠️ 미구현/후속: 벽 차폐(LoS) 미적용 — 콘 안이면 벽 너머도 전문 표시됨, 추후 ShadowCaster/레이캐스트 보강. 콘 판정값을 실제 콘 라이트 각도와 동기화할지 추후 결정. HP/그로기 바는 은신과 무관하게 유지(피격 시에만 노출).

## 맵 / 프롭

- **바닥·벽 = Unity Tilemap**. 벽 타일맵에 `TilemapCollider2D` + `CompositeCollider2D`(Static Rigidbody2D)로 이동 차단. 씬 골격은 `GameSceneBuilder`(에디터 메뉴)가 생성.
- **프롭 = Prop2D 카탈로그**(개별 스프라이트 + Collider2D). 막힘(못 가는 곳) = 비-트리거 Collider2D. walkability 그리드/NavMesh 없음.
  - `Prop2DDefinition`(SO): 스프라이트·머티리얼·sortingOffset + **콜라이더 모드**(None/Box/Polygon/Composite, 프롭마다 선택).
  - `Prop2DBuilder`(static): 정의→GameObject 런타임·에디터 공용 생성. Polygon은 `sprite.GetPhysicsShape`로 외곽선 자동.
  - 맵 도구는 [`map-tool.md`](map-tool.md) 참고.

## 캐릭터 렌더 (Spine 제거됨, 2026-09-07)

> **현 상태 = 진실.** 플레이어 비주얼 = **3D 치비 모델**(`ChibiPlayerVisual`). **Spine은 완전 제거됨.**

- **Spine 제거(2026-09-07)**: Unity 6.6(6000.6) 업그레이드에서 Spine 4.2 소스가 `Object.GetInstanceID()`
  obsolete-as-error(CS0619)로 컴파일 불가. 3D 전환 계획상 어차피 제거 대상이라 앞당겨 들어냄.
  삭제: `Assets/Spine`(689파일) · `Assets/Resources/Charater`(cha) · `Editor/SpinePlayerSetup.cs` ·
  `Shaders/SpineLitURP.shader` · `PlayerRig.prefab`의 `PlayerSpine` 노드 · asmdef 참조.
- **현재 플레이어 표시**: `TopDownPlayer.character3DPrefab`이 물려 있으면 `ChibiPlayerVisual`이 3D 모델을
  띄우고 SpriteRenderer를 끈다. 없으면 스프라이트 → 그레이박스 몸통 순으로 폴백.
- ⚠️ **이월된 한계**: `HitFlash`(피격 흰 플래시)·`InjuryVFX`(통증 깜빡임)가 **SpriteRenderer 바디를 가정**한다.
  3D 표시에선 그 SpriteRenderer가 꺼져 있어 **플레이어 몸에 피격 연출이 안 보인다**(화면 효과는 정상).
  Spine 시절과 같은 구멍이 그대로 넘어옴 → 3D 전환 Stage 4에서 메시 렌더러 기준으로 재배선 필요.
- ⚠️ **미제작 모션**: 3D 캐릭터는 idle/walk/run 3종뿐. **공격·피격·사망·앉기 없음.**
  구 Spine 구동부가 갖고 있던 상태→모션 매핑(구르기=`roll`, 약/강공격=`attack`, 앉기=`sit`/`sit_walk`,
  이동=`walk`/`run`, 정지=`idle`)은 3D 애니메이터로 다시 구현해야 한다 → `3d-migration.md` Stage 4.

## 셰이더 (네임스페이스 `BRB/`)

URP 2D 렌더러는 `Tags{ "LightMode"="Universal2D" }` 패스만 그린다. 유지 셰이더는 모두 Universal2D 패스 보유(2026-06-02).

**맵 (사용 중)** — 픽셀화 + 색단계 + Light2D. (픽셀 외곽선 기능은 2026-06-02 전부 제거)
| 셰이더 | 용도 | 조명 |
|---|---|---|
| `BRB/FloorPixel` | 바닥(타일) — 불투명·컷아웃 없음, 정점컬러 | Light2D 반응 |
| `BRB/WallPixel` | 벽 — 알파 컷아웃 + 문 흰색뚫기 `_WHITE_CUTOUT` + 오클루전 페이드 `_Alpha`. `_SHADOW_MODE`는 `GroundShadow2D`(정적 발밑)가 재사용 | Light2D 반응 / 그림자모드 Unlit |
| `BRB/PropPixel` | 프롭·오브젝트 — 알파 컷아웃 | Light2D 반응 |
| `BRB/DecalPixel` | 데칼(바닥 오버레이) — 핏자국·그을음·발자국·금·포스터·발광. Transparent 큐, sortingOrder로 앞뒤. 블렌드 프리셋 Alpha/Multiply/Additive(`DecalPixelGUI` 원클릭) + `_Alpha`·정점컬러로 페이드 | Light2D 반응(토글) |
| `BRB/DamageOverlay` | 파괴/폐허 오버레이 — 베이스 위에 덧씌우는 균열·그을음(절차적, 아트 불필요). `_Damage`(0~1)로 단계별 진해짐, 베이스 알파로 마스킹. `Breakable`(부서짐)·`Weathered`(상시 낡음/녹) 둘 다 자식 오버레이로 자동 설치 → **모든 베이스 셰이더 호환** | Light2D 반응(토글) |
| `BRB/AnomalyFog` | 짙은 현상 전체화면 오버레이 — 카메라 앞 풀스크린 fog+어둠(`DenseAnomalyController`가 생성). 시간 무관 침식, 낮/밤 글로벌 라이트와 독립. 애니 노이즈+중앙 클리어+가장자리 짙음 | 언릿(화면 최상단) |

**캐릭터/이펙트 (사용 중)**
| 셰이더 | 용도 | 조명 |
|---|---|---|
| `BRB/PlayerSprite` | 플레이어/캐릭터(표준 SpriteRenderer) — 외곽선 + 최소광(어둠 가독성) + 픽셀화/색단계(옵션) + `_FlashAmount`(HitFlash 연동). 수동 시트UV 제거 | Light2D 반응 |
| `BRB/SpriteSheet` | 스프라이트 시트 UV | Light2D 반응 |
| `BRB/SpriteBillboard` | SpriteRenderer용 | Light2D 반응 |
| `BRB/SpriteFlash` | 타격감 흰 플래시(HitFlash 런타임 설치) | Light2D 반응 |

**삭제됨(2026-06-02)**: `Pixelated`(PropPixel/FloorPixel로 대체), `OcclusionOutline`(iso 잔재), `FlashlightBeam`(손전등 폐기), `ShadowProjector`(2D 미렌더). 0 참조 확인 후 제거.

- **모듈 그림자(✅ 2026-06-02, 네이티브 전환)**: 가짜 `FlatShadow`(스프라이트 복제/기울임) 폐기 → **URP 2D 네이티브 동적 캐스트 + 정적 발밑**의 2겹.
  - **① 동적 캐스트 = `ShadowCaster2D` + `Light2D` 그림자**: 엔진이 **실제로** 계산 — 물체가 빛을 막아 빛 반대편에 그림자 영역. 방향·길이는 라이트 위치가 결정, 여러 라이트 각각, 진짜 3D식(좀보이드/다크우드). 프롭/벽에 `ShadowCaster2D`. 에디터 `[ExecuteInEditMode]` Awake가 Collider2D/SpriteRenderer로 shadow shape 자동 설정→프리팹 직렬화. **플레이어 `PlayerConeLight`+`PlayerAmbientLight`에 `shadowsEnabled`+`shadowIntensity=1`**(빌더, 완전 차단).
    - **빛 차단량 = 라이트의 `shadowIntensity`(전역, 0~1)**. 1=벽 너머 완전 차단, 0.75=25% 샘. ⚠️ **Global Light2D는 그림자 무시**(전체 균일 조명) → 벽 뒤 어둡게 하려면 글로벌 intensity 낮게.
    - **프롭별 빛 통과 제어**(`Prop2DDefinition`): `shadowCasting`(Casting Option) = CastShadow(완전차단)/NoShadow(완전통과)/SelfShadow 등 + `shadowAlphaCutoff`(스프라이트 알파가 이보다 낮으면 그림자 안 만듦 = **창문/반투명으로 빛 통과**, 값↑=더 통과). ⚠️ **사물별 "부분(%)" 차단은 URP 2D 미지원**(shadowIntensity는 라이트당) — on/off + 알파(창문 구멍)로 표현.
  - **② 정적 발밑 접지 = `GroundShadow2D`**: 물체 바로 밑에 항상 깔리는 어두운 실루엣(빛 무관 고정) → 빛 없는/밝은 곳에서도 안 떠 보임. 자식 SpriteRenderer/Mesh 복제 + `WallPixel(_SHADOW_MODE)`(shear 0, dir 0) 어둡게, `offsetY`만큼 아래, `sortingOrder−2`. `[ExecuteAlways]`(에디터 미리보기), 자식은 `HideFlags.DontSave`.
    - **에디터 라이브 동기화(✅ 2026-06-03)**: 씬에서 베이스 SpriteRenderer(스프라이트·flip·Tiled size·정렬·Cutoff)를 편집하면 `Update()`가 **변경 감지 시에만** 그림자 자식을 인플레이스로 재동기화 → 맵툴 배치본뿐 아니라 **씬에서 손수 수정한 벽**도 그림자가 따라옴. (런타임 비용 0 — 에디트 모드 전용.)
  - **부착**: `Prop2DDefinition.castShadow` ON → `Prop2DBuilder`가 ShadowCaster2D + GroundShadow2D 둘 다 부착. 카탈로그는 `Wall`·`Prop` 기본 ON(바닥/마커 OFF).
  - **왜 네이티브?**: FlatShadow는 탑다운(위에서 봄)에서 스프라이트 기울이면 "쓰러지는" 가짜라 진짜 3D 그림자가 안 됨(쌍둥이/스냅/플립 등 한계). ShadowCaster2D가 정석.
  - **삭제**: `FlatShadow.cs`/`FlatShadowDirector.cs`/`FlatShadowEditor.cs`, `BRB/ShadowProjector`(2D 미렌더). `WallPixel(_SHADOW_MODE)`은 GroundShadow2D가 정적 발밑에만 재사용.
- **데칼(✅ 2026-06-03)**: 전용 **`BRB/DecalPixel`**(프롭/바닥과 별개). 바닥 위에 깔리는 오버레이 — 핏자국·그을음·발자국·금·포스터·이상현상 발광 등. `Transparent` 큐, 앞뒤는 `sortingOrder`(보통 바닥 위·프롭 아래). 픽셀화/색단계는 BRB 공통, 소프트 가장자리(`_Cutoff` 기본 0).
  - **블렌드 프리셋(머티리얼별)** — `DecalPixelGUI` 인스펙터 상단 드롭다운 원클릭(`_SrcBlend`/`_DstBlend`/`_MULTIPLY_ON` 한 번에):
    - **Alpha**(기본): 일반 스프라이트 데칼(핏자국·발자국·포스터). Light2D 반응(바닥과 같은 빛).
    - **Multiply**: 얼룩·그을음 — 바닥을 어둡게 물들임. 알파 인식(알파 낮은 곳=흰색=변화 없음). 언릿(바닥의 빛을 곱함). `Blend DstColor Zero`.
    - **Additive**: 발광 데칼(룬·이상현상) — 밝게 더함. 보통 Light2D OFF. `Blend SrcAlpha One`.
    - **Screen**: 검정 배경 밝은 오염을 부드럽게 더함(Additive보다 덜 과함). `Blend OneMinusDstColor One`.
  - **마스크 모드(✅ 2026-06-03, 낡음/녹/폐허)** — `_MASK_MODE` 토글: 텍스처를 **흑백(밝기=세기)** 으로만 쓰고 색은 `_Color`(Tint)로 결정. 검정 배경=효과 없음. **회색조 텍스처 1장으로 녹·이끼·그을음·물때를 만들고 Tint 색만 바꿔 재활용**(에셋 최소·리컬러 자유). 권장 조합 = **Multiply**(바닥/벽을 그 색으로 물들임). 작화 컨벤션 3종 중 **①흑백 마스크+셰이더 틴트**를 표준으로 채택(②컬러 베이크+Alpha, ③검정 배경+Additive/Screen는 그대로 지원).
  - **페이드**: `_Alpha`(머티리얼) 또는 `SpriteRenderer.color.a`(인스턴스)로 시간 경과 사라짐(핏자국 마름 등). `_LIT_ON` 토글로 조명 반응 on/off.
  - ⚠️ 부착/배치 자동화는 아직 없음(머티리얼만). 런타임 스폰(피격 위치 핏자국 등)은 추후 데칼 매니저로.
- **타격감 흰 플래시(✅ 2026-06-02)**: 전용 **`BRB/SpriteFlash`**(URP 2D sprite-lit + `_FlashColor`/`_FlashAmount`) 신설. `HitFlash.cs`가 바디 스프라이트에 머티리얼 자가설치 후 `_FlashAmount`로 적중 시 흰색 깜빡. `SpriteRenderer.color` 대신 셰이더 레벨이라 베이스 색·조명·틴트와 무관. (기존 BRB 셰이더는 캐릭터가 안 쓰므로 미수정.) ⚠️ 빌드 시 `Shader.Find` 위해 Always Included Shaders 등록 필요.
- **삭제됨**: `CityBuilding`/`CityWall`/`RuinFloor`/`RoadFloor`/`WetFloor`/`Prop`(3D Forward 전용), `RuinPixel`, 구 `InkCity/*`(SpriteOutline/InkShadow/NightOverlay/PanelWiggle/InkDissolve/InkFloor/DotFloor).
- ⚠️ 스프라이트 임포트: **Mesh Type = Full Rect** 권장(Tight면 시트UV/shear 틀어짐). URP 17.3 / Unity 6.
- ⚠️ `ShadowProjector`/`OcclusionOutline`은 iso 시절 잔재 — 순수 2D 전제에선 대부분 불필요. `FlashlightBeam`은 손전등 폐기로 삭제 예정.

## 변경 로그

- 2026-09-11: 측면 캡처에서 남은 몸 과조명 재확인. BodyGlow .06/카메라 쪽1.1m로 보정, Play 유지 저장·8방향 낮밤 자체 검수. 검수 카메라에서 보조광 월드 위치 동기화 보완.

- 2026-09-11: 가방 과조명 피드백. LampSpill 분리 비교로 원인 확인 후 발밑 높이(-.65m)·낮밤 세기(.08/.136) 보정. 전방 빔·BodyGlow·환경은 유지하고 코드/PlayerRig 저장 및 재실행 검수 완료.

- 2026-09-11: “전체적으로 밋밋하고 분위기가 약함” 피드백 반영. 밤 중간 명암과 입면 방향광 보완, 가로등 대비 완화·전당포 입구 초점/그림자 강화. 긴 그림자 후보 수정 후 저장·재시작·낮밤 자체 재검수. 사용자 승인으로 Play 재실행 및 위치·낮 복원.

- 2026-09-11: 집 PC에서 분위기 작업 재개. 낮 WeatherData·공용 Volume 소폭 보정, 기존 가로등/현관등 7개에 낮밤 강도 분리 적용. 과한 현관등과 낮 과조명 후보 수정 후 재시작·페이즈/재활성/야간 생성·62° 게임 화면 자체 검수 완료. 밤 팔레트/안개와 GameLit 소스는 유지.

- 2026-09-11: 후속 표면 질감 승인에 따라 목재·철재·관리인 BaseColor 3장과 재질 5개를 MCP로 적용했다. 밝기 재보정·Play 유지 전후/저장 후 검수를 완료했다.

- 2026-09-11: “찰흙 같아서 순서대로 해보자” 승인. 마을 표면 마스크·재료별 반사, 캐릭터 림/탈색, 작은 접촉 음영 보정 및 재질 변환 도구의 검수값 보존 적용. 62° 낮밤/재질/캐릭터 비교 수행.

- 2026-09-11: “그림자도 이상하네” 피드백으로 낮 태양 58°, 4 cascades/바이어스 보정. Unity 62° 전후 비교와 런타임 관리인·회수꾼 검수 완료.

- 2026-09-11: “진행해보자” 승인에 따라 마을 조명/후처리/기존 텍스처 반복/재질을 보정하고 GameLit 선택형 표면 마스크를 추가했다. 저장 누락 마스크 재연결과 낮 전구 과노출을 수정하고 낮/밤 게임 카메라로 재검수했다.

- 2026-09-11: NPC 인게임 적용 후 셰이더 작업 시점 질문 기록. 조명·색보정 기준화 → 재질 구분 → 필요한 GameLit 개선 순서를 제안했으며 구현 승인은 별도다.

| 날짜 | 결정 | 근거 |
|---|---|---|
| 2026-09-11 | **문서 현행화 — 상단 「현재 구현 (3D)」 표 신설.** "아래 본문(URP 2D)이 여전히 현 구현, 전환 미착수" 안내가 3D 전환 완료 후에도 남아 새 세션이 틀린 정보로 시작했다. 2D 본문은 지우지 않고 기록으로 표시 | 시스템 정리 1단계(입구 문서) — `dev-roadmap.md` §시스템 정리 |
| 2026-09-11 | **카메라 정면 탑다운 62°(방위 0°)로 변경** — 대각선 45°에서 되돌림. 4장 비교 후 사용자 선택. §쿼터뷰 카메라 · 착용등 · 분위기 · 반짝임 | 사용자 "탑다운 뷰가 더 이득일 것 같다" — 총격전 거리감·루팅 가독성·기존 시스템(미니맵·간판)이 정면 기준 |
| 2026-09-11 | **쿼터뷰 카메라 대각선 45° · 착용등 앞쪽 빔 중심 · 낮 주황/저녁 보라 · 반짝임 줄이기.** §쿼터뷰 카메라 · 착용등 · 분위기 · 반짝임 | 사용자 피드백(쿼터뷰가 아직 아님 · 대리석 같은 반짝임 · 머리 위 태양 같은 주변광 · 너무 밝은 분위기) |
| 2026-09-11 | **게임 전용 셰이더 + 포스트 프로세싱 결정** — 캐릭터+환경 공용 Lit 셰이더 1개, 어두운 사실풍, 색보정·톤매핑 + 블룸 + 비네팅·필름 그레인. §게임 전용 셰이더 + 포스트 프로세싱 | 사용자 요청 "우리 게임에 맞는 셰이더 하나 + 포스트 프로세싱". Into the Dead 분위기, 툰 폐기 이후 |
| 2026-05-23~31 | (폐기) 벽/건물 = 3D 큐브, 바닥 = 2D Plane 하이브리드. 스텐실 바닥, 스팟라이트 차폐, BuildingInterior 알파 페이드, WallOcclusionOutline 아웃라인, ShadowProxyBuilder, 커스텀 MapBuilder(WallBuilder/PropQuadBuilder/층 시스템). | 전부 2026-06-02 탑다운 2D 전환으로 **삭제**. 상세 이력은 git history 참고. |
| 2026-06-01 | (폐기) 비주얼 = 2D 평면, 빛 차폐 = 안 보이는 3D 박스(ShadowsOnly) 하이브리드. | URP 2D 렌더러 + Light2D 전환으로 ShadowCaster2D가 대체. |
| 2026-06-02 | **탑다운 2D 전환 확정.** 렌더 파이프라인 URP-3D→URP-2D, 카메라 2D Orthographic, 좌표계 XY, 조명 Light2D, 맵 Tilemap, 이동 Rigidbody2D. 아이소 식별자 전면 정리(`Isometric*`→`TopDown*`), 3D 큐브/스텐실/스팟라이트/오클루전/MapBuilder 제거. | [`topdown-migration.md`](topdown-migration.md) 참고. |
| 2026-06-02 | 셰이더 네임스페이스 `InkCity/`·`Custom/`→`BRB/` 통일, 3D Forward 전용 셰이더 삭제, 유지 8개에 Universal2D 패스 추가. | URP 2D 렌더러는 Universal2D 패스만 그림. |
| 2026-06-02 | **아트 각도·배치 회전 확정.** 아트에 **~80° 틸트 베이크**(카메라는 2D 정면, 안 기울임), 타일·모든 오브젝트는 **회전 (0,0,0)** 배치(순수 2D, 빌보드/눕힘 없음). | [`topdown-art-spec.md`](topdown-art-spec.md) 참고. 카메라·트랜스폼은 단순 유지, 입체감은 아트가 전담. |
| 2026-06-02 | **손전등 폐기 → 좀보이드식 시야(FOV).** 적은 플레이어 바라보는 부채꼴 시야 밖이면 안 보임/어둑. 어둠=하이브리드(지역/시간대별 가변). 손전등 코드(FlashlightController/FlashlightBeam/손전등 Light2D) 완전 제거 후 시야 시스템 신규. | 손전등 단일 주광원보다 FOV 가시성이 긴장감·전투 무대로 더 강함. 타격감 설계와 연동([`combat.md`](combat.md)). |
| 2026-06-02 | **플레이어 상시 라이트 + 글로벌 어둠 (FOV 전 단계).** 손전등 컨트롤러 제거 → 플레이어 자식 `PlayerLight`(Point Light2D, 상시, 반경 0.6~4.5, 따뜻한 흰색)가 주변 상시 밝힘. 씬엔 어두운 `Global Light 2D`(intensity 0.22, 차가운 밤색)로 대비 → "주변만 밝고 나머지 어둠"(Darkwood 룩). 두 라이트 모두 **모든 Sorting Layer 타겟**. 빌더: `TopDownPlayerBuilder`(PlayerLight 상시, FlashlightController 미부착·PlayerEquipment 추가), `SceneLightingBuilder`(Tools▸TopDown 2D▸Setup Scene Lighting). | 사용자 요청(이미지 레퍼). FOV 시야 콘 전까지 원형 주변광으로 분위기 확보. FlashlightController 컴포넌트는 프리팹에서 제외(클래스는 GameHUD 배터리바 호환 위해 잔존). |
| 2026-06-02 | **플레이어 라이트 2종(다크우드식): 주변 원형 + 앞 부채꼴.** 단일 원형을 2개로 분리 — ①`PlayerAmbientLight`(Point 360°, intensity 0.6, 반경 0.2~2.3, 중심 고정): 본인 바로 주변 은은하게. ②`PlayerConeLight`(Point 부채꼴 inner 35°/outer 80°, intensity 1.3, 반경 0.4~6.5): 마우스 방향으로 회전 + 원점 앞 오프셋(`lightForwardOffset` 0.45)으로 앞을 멀리·밝게. `TopDownPlayer.lightPivot`=콘(회전 대상), 원형은 고정. 콘 방향 안 맞으면 `lightAngleOffset` 조정. | 다크우드는 "주변 약한 원형 + 앞 부채꼴" 둘 다임. 부채꼴만 두면 본인 주변이 너무 깜깜. |
| 2026-06-02 | **PlayerRig 한 세트 프리팹 + 다크우드 후처리(결정 변경).** 카메라 "씬 자동 장착" → **카메라+플레이어+라이트+Volume을 `Resources/PlayerRig.prefab` 한 세트**로 묶어 DontDestroyOnLoad(Bootstrap 1개 스폰). `CameraFollow`가 씬 로드 시 다른 Camera/AudioListener 비활성(충돌 방지). 카메라 `allowHDR` + ConeLight intensity 1.6(HDR). **후처리 Volume**(`PlayerRigVolume.asset`): Bloom(threshold 0.5/intensity 1.3/scatter/warm tint) + Vignette 0.42 + ColorAdjustments(따뜻·대비) + FilmGrain — "빛이 번지는 맛". `TopDownPlayer.DontDestroyOnLoad(transform.root)`. | 사용자 결정: 3개 한 세트 일관 동작. "맛없는 빛"은 bloom 미적용(threshold 높음·HDR off)이 원인 → 프리팹에 다크우드 Volume 내장으로 모든 씬 일관. |
| 2026-06-02 | **낮/밤 2D 조명 연동 + 에디터 전환 버튼.** `DayNightCycle`이 3D `directionalLight`/`RenderSettings.fog`만 제어 → **2D `Global Light2D`(intensity·color) 제어 추가**(낮 1.0/밤 0.18, 밤=차가운 파랑). `SetNight(bool)`/`ToggleDayNight()` public(에디터·런타임 공용 — 글로벌 라이트 즉시 적용 + OnPhaseChanged + RegionTime 동기화). `DayNightCycleEditor`(커스텀 인스펙터): ☀낮/🌙밤/⟳토글 버튼 — 씬(에디터)·플레이 둘 다 즉시 미리보기. 수치는 인스펙터에서 조절. `SceneLightingBuilder`가 Global Light2D 생성 시 `DayNightCycle` 부착+연결. T키 유지. | 맵툴/테스트에서 낮↔밤을 씬·인게임 둘 다 즉석 전환·튜닝하려는 사용자 요청. 글로벌 2개(DayNightCycle+RegionTimeManager) 중 시각 적용은 DayNightCycle 담당. |
| 2026-06-02 | **프롭/벽 그림자 방향 = 플레이어 시선(콘) 반대로(`Facing` 모드).** 기존 위치 기반(`Player`/`NearestLight`)은 라이트 "위치"(=플레이어, 보통 아래) 반대로 깔려서, 콘이 위를 비추는데 그림자가 위로 가는 "빛이 아래 있는 듯" 버그. → `FlatShadow.DirMode.Facing` 신설(기본): `dir = -FlatShadowDirector.Facing`(=`TopDownPlayer.FacingDirection` 반대). 밝은 콘 쪽 반대에 그림자. 거리 길이/진하기(`proximityStretch`/`distanceFade`)는 플레이어 거리로. 위치 기반 모드도 옵션 유지. | 콘은 "위치(플레이어)"와 "비추는 방향(시선)"이 달라서, 위치 기반이면 직관과 반대. 사용자가 "콘 비추는 쪽 기준" 선택. |
| 2026-06-02 | **그림자 = URP 2D 네이티브 ShadowCaster2D로 전환(가짜 FlatShadow 폐기).** FlatShadow(스프라이트 복제/기울임)는 탑다운에서 진짜 3D 그림자가 안 됨(쌍둥이/스냅/벽 갇힘) → ① `ShadowCaster2D`+`Light2D`(동적 캐스트, 엔진 계산) ② `GroundShadow2D`(정적 발밑 접지)로 분리. Prop2DBuilder가 castShadow면 둘 다 부착. PlayerConeLight에 shadowsEnabled+shadowIntensity. `FlatShadow`/`FlatShadowDirector`/`FlatShadowEditor`·`ShadowProjector` 삭제, Prop2DDefinition 그림자 필드도 castShadow만 남김. | 사용자: "이건 3D 그림자가 아니다". 네이티브가 방향·길이·다중라이트 전부 정확. 정적 발밑은 빛 없을 때 접지감 유지용으로 별도 유지. |
| 2026-06-03 | **데칼 전용 셰이더 `BRB/DecalPixel` 신설(별개).** 바닥 오버레이(핏자국·그을음·발자국·금·포스터·발광). Transparent 큐 + sortingOrder. 블렌드 프리셋 Alpha/Multiply/Additive를 `DecalPixelGUI` 인스펙터 드롭다운으로 원클릭(`_SrcBlend`/`_DstBlend`/`_MULTIPLY_ON` 동시 세팅). Multiply는 알파 인식(흰색 lerp) 언릿, Additive는 보통 언릿. `_Alpha`+정점컬러로 페이드, `_LIT_ON` 토글. 픽셀화/색단계 BRB 공통. | 사용자 요청("데칼용 셰이더 별개로 필요"). 프롭/바닥과 분리해 블렌드·페이드·발광을 데칼 전용으로. 배치 자동화/런타임 스포너는 추후. |
| 2026-06-04 | **짙은 현상 = 낮/밤과 독립 레이어 구현(풀스크린 fog 오버레이).** `DenseAnomalyController`(싱글톤)가 카메라 앞 풀스크린 쿼드(`BRB/AnomalyFog`)로 화면에 어둠+안개를 덧씌움 → `DayNightCycle` 글로벌 Light2D 안 건드림(충돌 0, "낮에도 밤처럼"). 애니 노이즈+중앙 클리어 버블+가장자리 짙음, 월드 앵커. 강도 = 수동(`SetIntensity`)·이벤트 max 지역 침식도(`WorldRegionCatalog.anomaly` 신규 필드, 성역 0.7/중앙영야 0.9 등). 범위=지역/씬 단위, 테스트키 G. | 사용자 결정: 짙은현상은 시간 무관(낮밤과 별개) fog+어둠. 글로벌 라이트와 충돌 피하려 화면 오버레이(곱 합성)로. gdd-core §5.1. |
| 2026-06-04 | **플레이어 셰이더 `BRB/PlayerSprite` 재작성.** 옛 버전은 수동 스프라이트시트 UV(`_Columns/_Rows/_CurrentFrame`)라 일반 SpriteRenderer에 쓰면 텍스처 한 칸만 잘려 사실상 못 씀 → **표준 SpriteRenderer**(단일 스프라이트·유니티 애니 호환)로 재작성. Light2D 2D 패스 + 외곽선(어둠 속 가독성) + `_MinLight`(어두워도 최소 가시) + 픽셀화/색단계(옵션·기본 OFF) + `_FlashColor/_FlashAmount`(HitFlash가 SpriteFlash 교체 없이 그대로 사용). 정점색 곱(틴트/페이드). 메뉴 `Tools▸TopDown▸Setup▸Create & Assign Player Material`로 머티리얼 생성+PlayerRig 프리팹 PlayerSprite에 적용. | 플레이어가 전용 셰이더 없이(또는 깨진 시트UV로) 렌더돼 "셰이더 없음" 상태였음. |
| 2026-06-03 | **바닥 타일 반복 깨기 `FloorPixel._BREAKUP_ON`.** 월드 좌표 fbm 노이즈로 바닥 명암을 변주 → 같은 타일이 깔려도 격자 반복감이 사라짐(`_BreakupScale`/`_BreakupAmount`). 화면이 아닌 **월드 기준**이라 카메라 이동에도 안정. 2D 패스에 worldXY 배리잉 추가. + 데칼 스캐터 브러시(`DecalScatterBrush`)·가장자리 자동 디테일은 [`map-tool.md`](map-tool.md). | 통짜로 그린 참조 맵의 불규칙 바닥을 Tilemap에서도. 스플랫맵은 무거워 제외, 노이즈+데칼 조합 채택. |
| 2026-06-03 | **상시 풍화 `Weathered` 신설 — 이미지 0장 낡음/녹/폐허.** 부서짐과 별개로, 부서지지 않아도 항상 절차적 녹·그을음을 덧씌우는 컴포넌트(`BRB/DamageOverlay` 재사용, `_Damage` 고정 + `tint`). 색만 바꿔 녹(주황갈)/이끼(초록)/그을음(검정)/물때(갈색). 카탈로그 "Aged · 낡음/녹/폐허" 섹션(`Prop2DDefinition.weathered`). | 사용자가 "이미지 없이 녹슨·폐허"를 원함. 텍스처(DecalPixel 마스크) 없이도 분위기 확보 — 셰이더가 이미 색 입힘. 텍스처 마스크와 공존(나중에 업그레이드). |
| 2026-06-03 | **데칼 마스크 모드(`_MASK_MODE`) 추가 — 텍스처 기반 낡음/녹/폐허.** 사용자가 텍스처로 작화하기로 결정. 작화 컨벤션 3종(①흑백 마스크+셰이더 틴트 ②컬러 베이크+Alpha ③검정 배경+Additive/Screen) 중 **①을 표준 채택**(회색조 1장→Tint 색만 바꿔 녹·이끼·그을음 재활용). `DecalPixel`에 `_MASK_MODE`(루미넌스=세기, 색=Tint) + GUI에 Screen 프리셋 추가. 권장 = 마스크 모드 + Multiply. | 색별 텍스처를 따로 안 그려도 됨(리컬러 자유·에셋 최소). 별도 셰이더 안 만들고 데칼에 통합 — 데칼 하나로 핏자국·그을음·녹·이끼·폐허 전부. |
| 2026-06-03 | **파괴 시스템 `Breakable` + `BRB/DamageOverlay` 신설.** 상자 등을 때리면 1→2→3 단계로 부서지고 마지막에 파괴(파편). 손상 비주얼 = 베이스 위 **자식 오버레이**(균열·그을음 절차적) → 베이스 셰이더 안 건드림 = **모든 셰이더 호환**. Health 있으면 자동 연동, 없으면 `Hit()`/`ApplyDamage()`. 카탈로그(`Prop2DDefinition.breakable`)에서 프롭별 ON. 상세 [`destructible.md`](destructible.md). | 사용자 요청("모든 셰이더에 같이 쓸 수 있게 부서짐/폐허 효과, 단계별로"). 오버레이 방식이라 셰이더별 수정 불필요. 절차적이라 아트 없이도 동작. |
| 2026-06-03 | **글로벌 Light2D 주인 = Systems 씬으로 확정 + 에디터 중복 경고 해결.** "글로벌은 씬이 둔다"(이전 메모) 정정 → **Systems 부트 씬이 글로벌을 단독 소유**, 게임플레이 씬(InGame/Safehouse) 빌더는 글로벌을 안 만듦(이미 그러함). 자체 글로벌은 Systems + MapTool + CombatSandbox(단독 실행용)만. 글로벌 2개↑ 활성 시 URP `More than one global light on layer ...` 경고 → 런타임 `SystemsSceneEnforcer`에 더해 **에디트 모드용 `SystemsGlobalLightEditorEnforcer`(신규, `[InitializeOnLoad]`)** 추가: Systems 로드 시 그 글로벌만 남기고 다른 씬 글로벌을 에디터에서도 비활성(Systems 언로드 시 복구, 강제 저장 안 함). | 에디터에서 Systems+게임플레이/샌드박스 씬을 함께 열면 enforcer(런타임 전용)가 안 돌아 씬뷰 repaint마다 경고. PlayerRig는 점광만 가져오므로 글로벌 owner는 PlayerRig가 아니라 Systems 씬. |
| 2026-06-04 | **텍스쳐(쿠키) 라이트 전환 — 플레이어 + 프롭 발광.** Point/Spot 파라미터 라이트의 딱딱한 콘 → URP 2D **Sprite 라이트 + 절차적 쿠키**(부드러운 그라데이션, 다크우드 룩). `LightCookieGenerator`(Tools▸TopDown▸Map▸Generate Light Cookies)가 `Resources/LightCookies/`에 cookie_radial(중앙 피벗)·cookie_cone(+Y·하단 피벗=꼭지) PNG 절차 생성(흰색+알파, 색은 Light2D.color, PPU=256→1유닛·스케일로 크기). PlayerRig 빌더: 주변광=라디얼·콘=콘 쿠키로 교체(회전·그림자 유지, lightAngleOffset=-90로 +Y→facing). 프롭=Prop2D 카탈로그 통합: `Prop2DDefinition`에 emitsLight/shape/color/intensity/radius/offset/angle/castsShadows/nightOnly 추가, `Prop2DBuilder.ApplyLight`가 Sprite Light2D 자식 부착(쿠키·정렬레이어는 URP 공개 setter 없어 reflection으로, 넣은 뒤 enable 토글로 메시 갱신), 밤전용은 `PropLight2D`(DayNightCycle 연동). | 사용자: 콘이 딱딱해 다크우드 느낌 안 남 + 램프/창문 발광 '기능' 필요. 텍스쳐 라이트가 2D 분위기 조명 표준. 절차 생성이라 무에셋·튜닝·스왑(직접 그린 PNG로 교체) 가능. |
| 2026-06-05 | **적 은신 + 머리 위 말풍선(실험 토글).** 시야 콘이 적을 "드러내는" 대신, 적 몸체(`EnemySprite`)는 항상 숨기고 적이 "말할 때"만 머리 위 말풍선. 콘 안=대사 전문 / 콘 밖="..."(말하는 중 실시간 갱신). 플레이어를 "만나는"(발견=순찰→추격) 순간에만 또렷한 대사, 그 외 평소엔 순찰 중 근접 혼잣말 중얼거림만(공격/피격/스턴/사망 전용 대사 없음 — 2026-06-05 결정). 전역 `EnemySpeechBubble.Enabled`(기본 ON, 디버그 B키/전투탭 토글). `EnemyController.Awake` 자동 부착, 콘 판정은 게임플레이용 별도 파라미터(라이트 렌더와 분리). | 사용자 요청: 적을 못 보게 하고 말풍선으로만 존재를 흘리는 긴장감. 콘은 "무슨 말인지 알아듣는" 범위로 의미. 나중에 on/off 원함. 벽 차폐(LoS)는 후속. |
| 2026-06-04 | **건물 개념 변경 — 천장 컷어웨이 폐기 → 건물=씬 전환.** "천장 없음 + 건물은 무조건 전환 이벤트" 확정 → 천장 시스템(Ceiling 카테고리·`CeilingFader`·반투명 토글·트리거 크기/오프셋·`gb_roof`/ceiling 에셋) **전면 삭제**. 건물 = **프롭 + `Function.Trigger`**(영역 밟으면 `MapTriggerZone2D`→`SceneTransitionManager.TransitionTo` 자동 전환). 입구가 80° 틸트 밑둥에 있을 수 있어 Trigger에 `triggerOffset` 추가(영역을 입구로 이동). 천장 컷어웨이 관련 항목들은 폐기됨(아래 천장 로그는 히스토리). | 사용자 결정: 실내를 같은 화면에 보여주는 컷어웨이 대신, 건물 진입=별도 씬 전환(타르코프식)으로 단순화. |
| 2026-06-04 | _(폐기)_ **천장 컷어웨이 트리거 크기/오프셋 override.** 80° 틸트 아트라 건물 **밑둥(앞면)이 아래로 길어** 입구로 들어와도 지붕 footprint 트리거 밖이라 페이드가 늦음 → `Prop2DDefinition.ceilingTriggerSize`(0,0=footprint 자동)·`ceilingTriggerOffset` 추가. 세로를 키우거나 Y-오프셋을 음수로 내려 입구/밑둥까지 덮으면 **진입 즉시 페이드**. `CeilingFader.OnDrawGizmosSelected`가 트리거 영역을 청록 박스로 표시(시각 튜닝). 배치본의 BoxCollider2D를 씬에서 직접 늘려도 됨. | 사용자: 밑둥이 길어 입구 진입 시 바로 천장 투명 원함. |
| 2026-06-03 | **천장(지붕) 컷어웨이 시스템 신설.** 옛 3D `BuildingInterior 알파 페이드`는 탑다운 전환 때 삭제됐고 현재 없음 → 새로 구축. **새 `Ceiling` 카테고리**(카탈로그 천장 탭, enum 끝에 추가, ID `ceiling_`): 콜라이더 None(막힘X)·그림자 OFF·**최상단 정렬(`Ceiling` Sorting Layer)**. 빌더가 천장 프롭에 **트리거 콜라이더(스프라이트/타일 footprint)** + `CeilingFader` 자동 부착. 동작: 플레이어가 건물 안(트리거)에 들어오면 지붕 알파 **1→0 부드럽게 페이드아웃**, 나가면 복귀. **건물 단위 그룹화**(`ceilingGroupId` 같은 조각들이 한꺼번에 페이드 — 한 조각 트리거에만 들어와도 그룹 전체). 정렬은 데칼(엔티티 아래)과 정반대(엔티티 위)라 전용 레이어. | 사용자 결정(질문 3): 새 천장 탭 / 진입 시 부드러운 페이드아웃 / 건물 단위. 좀보이드·타르코프식 실내 진입 가시성. FOV "실내 어둑"과 상보적(추후 연동). |
| 2026-06-18 | **Spine-Unity 런타임 4.2 고정(다운그레이드).** 캐릭터 에셋 `cha`(`Assets/Resources/Charater/cha.json·atlas·png`)가 Spine 4.2.43 익스포트인데 프로젝트엔 4.3.81 런타임 → `Data version 4.2.43 / Required 4.3` 로드 에러. **데이터 재익스포트 대신 런타임을 공식 `spine-unity 4.2.120`(`spine-unity-4.2-2026-05-29.unitypackage`)로 다운그레이드**해 맞춤. `Assets/Spine`·`Assets/Spine Examples` 전체 교체(asmdef GUID 동일→참조 유지). **앞으로 Spine 익스포트는 4.2 타깃 유지.** 선택지: ⓐ 데이터 4.3 재익스포트 vs ⓑ 런타임 4.2 다운그레이드 → **ⓑ 채택**. | `cha` 데이터 버전을 못 바꾸는 상황이라 런타임을 데이터에 맞춤. |
| 2026-06-18 | **플레이어 비주얼 = 단일 SpriteRenderer → Spine 스켈레톤(`cha`).** `PlayerRig.prefab`에 `PlayerSpine`(SkeletonAnimation) 자식 추가, 기존 `PlayerSprite`는 렌더러만 끔(오브젝트 유지). `TopDownPlayer`가 이동/전투 상태로 Spine 애니 구동(걷기=walk·달리기=run·약/강공격=attack·구르기=roll; `idle` 없어 정지 시 셋업 포즈), 좌우 플립=Skeleton.ScaleX 부호. 적용 메뉴 `Tools/TopDown/초기설정/Spine 플레이어 적용 (cha)`. **알려진 한계(후속)**: `BRB/SpineLitURP`에 flash 프로퍼티 없어 HitFlash 흰 플래시·InjuryVFX 통증 깜빡임이 플레이어 바디엔 미표시(화면 효과는 정상) → 셰이더에 flash 지원 추가 필요. | CLAUDE.md 명시 원래 방향(2D 스프라이트 + Spine animation)대로 플레이어를 Spine으로. |
| 2026-06-05 | **랜턴(장비 기반 시야 강화) 도입.** 손전등 F토글/단일 빔 폐기 계승. 시야(주변광 원형 + facing 콘)의 **반경·각도·밝기를 착용 랜턴 등급으로 가변** — 미착용=좁은 주변광(코앞)/착용=주변광·콘 동시 확대·증광. 토글 아닌 착용 패시브(상시 점등). 연료 소모·'빛=노출' 트레이드오프는 추후 옵션. | 사용자 결정: '손전등 폐기, 랜턴 차면 라이트·콘 커지고 밝아짐'. 시야가 장비 성장 축이 됨. 구현은 PlayerRig 주변광/콘 Light2D(쿠키) 파라미터를 LanternModifier로 조절. [→ items.md 랜턴, story-script S-013/S-020] |

## 2026-07-11 — FOV 어둠 오버레이 (`VisionDarkness`)

> 사용자 지적: "적군이 제대로 안 보이던데 스프라이트 빠졌나?" → **스프라이트 정상.** `PlayerVision`이 시야콘 밖 적의 렌더러를 끄는 설계대로였는데, **주변이 밝아서 적이 그냥 사라진 것처럼** 보였다. 사용자 결정: *"시야콘이 비추지 않는 곳을 좀 더 어둡게."*

- **신규 `Combat/VisionDarkness.cs`** — 플레이어 중심 부채꼴 메시를 매 프레임 생성해 **시야콘 밖을 어두운 반투명으로 덮는다.** 자가부트 싱글턴(DontDestroyOnLoad), 맵툴 씬 제외, **안전가옥에선 자동 비활성**.
  - 콘 안(정면 ±fov/2) → `visionRange`까지 밝음 / 근접 `visionNearRadius` 안 → 각도 무관 밝음 / 그 외 → 어둠
  - 콘 경계는 `half~half+soft` 구간을 보간해 계단현상 없이 부드럽게
  - **판정 수치를 `PlayerVision`과 동일한 GameTuning 필드에서 읽는다** — 시각과 판정이 어긋나면 그게 더 큰 버그
  - Light2D를 건드리지 않는 **독립 오버레이**(Unlit 반투명, sortingOrder 100)라 글로벌 라이트·낮밤 셋업과 충돌하지 않음
- **노브**: `GameTuning.visionDarkAlpha`(기본 0.72, 0=끔) — 어둠 농도. 기존 `visionEnabled/FovDegrees/Range/NearRadius`와 함께 동작.
- ⚠️ 알려진 별건: `GameTuning.asset`에 시야 필드가 **아직 기록돼 있지 않아** 코드 기본값으로 돈다(패널에서 조절하려면 에셋에 값이 써져야 함).

## 2026-07-11 — 그레이박스 식별 라벨: "적" ↔ "시체" (`UnitLabel`)

> 질문/지적: *"시체는 시체라고 표기해서 적은 적이라고 해주고, 적이 죽으면 시체겠지?"*
> **결정**: 살아있는 적은 머리 위 **"적"**, 죽으면 같은 라벨을 **"시체"** 로 교체한다. 아트가 붙기 전까지의 임시 식별 장치.

- **신규 `Combat/UnitLabel.cs`** — 머리 위 월드 텍스트(TextMesh) 한 곳. `Attach/Set/SetVisible`.
  - 부착 위치 = **몸체 스프라이트의 자식**, 부모 스케일을 보정해 어떤 크기의 적이든 글자 크기 동일(월드 0.13)
  - 색: 적 = 연분홍 / 시체 = 회색. 사망 시 **몸체 스프라이트도 45%로 어둡게** — 라벨만 바뀌면 여전히 붉은 적처럼 보인다
- **생성 주체를 `EnemyController.Awake` 한 곳으로 통일** — 기존엔 `EnemySpawner`가 런타임 그레이박스 적에만 붙여서 **프리팹 적엔 라벨이 없었다.**
- **디버그 시체 스폰(F1)** 도 같은 라벨을 쓴다 — 사망 경로와 눈으로 비교 가능.

### 이번에 같이 잡은 두 버그
1. **라벨이 아예 안 그려짐** — 스크립트로 `AddComponent<TextMesh>()` 하면 `font`가 null이라 머티리얼이 비어 **한 글자도 렌더되지 않는다**(인스펙터로 붙일 때와 다름). `LegacyRuntime.ttf` + 그 머티리얼을 명시 지정해 해결. `EnemySpeechBubble`이 같은 이유로 이미 폰트를 명시하고 있었다.
2. **시야 밖인데 라벨만 떠서 위치 노출** — `PlayerVision`/`SetVisionVisible`은 **SpriteRenderer '컴포넌트'만** 끄므로 자식 MeshRenderer(라벨)는 안 꺼진다. `SetVisionVisible`에서 라벨도 같이 껐다 켜도록 수정. (시체는 항상 보이는 월드 오브젝트라 사망 시 강제 표시.)

---

> ⚠️ **이 절 전체는 폐기됐다(2026-09-10).** 카툰을 접고 리얼리티로 갔다가, 룩 작업 자체를
> 백지에서 다시 하기로 했다. 아래는 왜 그 길을 갔고 무엇에 부딪혔는지의 기록으로만 남긴다.
> 현재 상태는 이 문서 맨 아래 **"룩 작업 초기화"** 절을 볼 것.

## 카툰 룩 전환 (2026-09-10, 폐기됨)

| 날짜 | 질문 | 결정 |
|------|------|------|
| 2026-09-10 | 모델 완성도가 들쭉날쭉한데 후처리로 덮을까? 아웃라인 진한 카툰처럼? | **채택 — `BRB/Toon` 신설.** ①**외곽선 = 인버티드 헐**(Cull Front + 클립공간 법선 압출, 화면 픽셀 기준 5.5px). 화면공간 엣지검출과 달리 렌더러 기능이 필요 없고 실루엣이 굵게 떨어진다. 오브젝트공간이 아니라 **클립공간**에서 미는 게 핵심 — 오브젝트공간이면 중장 밴딧(1.3배)만 선이 두꺼워진다. ②**셀 음영 = 밴딩**(3단, 경계는 `_BandSoft`로 아주 좁게만 풀어 계단 지글거림 방지). 림·그림자틴트·구운AO·추가광원·SSAO는 `BRB/Stylized`와 같은 규약 — 두 셰이더가 한 화면에 섞여도 룩이 안 갈린다. 플레이어(`ChibiPlayerVisual`의 셰이더 교체 훅)와 밴딧(`BanditEnemyVisual`) 양쪽에 적용. |
| 2026-09-10 | 카메라가 너무 멀다 | **오소 크기 7 → 3.6**(사용자: "2배는 가까워야"). 뷰 각도는 그대로 — pitch 55° · yaw 0°(대각선은 2026-09-07에 이미 폐기). |
| 2026-09-10 | 착용 랜턴이 "하늘에 뜬 조명" 같다 | **빔으로 다시 잡음.** 콘 96°→**56°**, 아래 숙임 34°→**16°**(34°는 발 앞 1.7m에 원을 만들어 캐릭터를 두른 웅덩이가 됐다), 사거리 5.5→**10m**, 밤 밝기 1.5→**3.4**, 콘 안쪽 비율 0.32→**0.74**(경계가 서야 손전등으로 읽힌다). 여기에 **`LampSpill`**(빔의 16%, 그림자 없는 점광 2.6m) 추가 — 빔만 있으면 착용자가 검은 종이가 되어 "어디선가 쏘는 조명"이 된다. |
| 2026-09-10 | 화면 전체에 노이즈가 낀다 | **SSAO 강도 2.6 → 1.05**(반경 0.2→0.35, 직접광 영향 0.7→0.55). 강도가 과해 AO의 디더 노이즈가 모델·바닥에 그대로 드러났다. |
| 2026-09-10 | 룩을 어디서 판단하나 | **룩 체크 씬(`MapTool_LookDev`)** 신설 — `Tools ▸ TopDown ▸ 개발 ▸ 룩 체크 씬`. 인게임은 55° 부감에 캐릭터가 작아 외곽선·밴딩을 판단할 수 없다(사용자 제안). 씬 이름의 **MapTool**이 부트스트랩을 전부 끈다. 구성: 플레이어·밴딧 일반·밴딧 중장을 **인게임과 같은 배율**로 나란히(크기 비율 조정용) + 1.8m 기준봉 + 카메라 4대(1 정면 / 2 측면 / 3 **쿼터뷰=게임 설정 그대로** / 4 랜턴 검증 레인) + **N키 낮/밤**(값은 게임과 같은 `WeatherData`에서 읽는다). 랜턴 레인은 전방 2·4·6·8m 상자 + 10m 벽 — 빔은 맞는 면이 있어야 감쇠·그림자·콘이 보인다. |

> **전투 테스트는 여기서 하지 않는다**(2026-09-10 사용자: "룩씬은 별도로 있으면 된다"). 이 씬은 시스템 부트스트랩이 꺼져 있어 플레이어 컨트롤러·입력·전투가 아예 없다 — 전투는 시스템이 살아 있는 씬에서.

### 팔·소매·방망이가 검게 보이던 것 — 원인은 셰이더의 **텍스처 입력 부재** (2026-09-10)

> 사용자 지적: *"여전히 팔뚝이 검정색으로 꽉차는데 이거 셰이더 다른거 써야할까?"*, *"방망이도 검정색으로 보이네"*

| 날짜 | 질문 | 결정 |
|------|------|------|
| 2026-09-10 | 팔·소매·방망이가 통째로 검게 나온다. 셰이더를 바꿔야 하나? | **바꾸지 않는다 — `BRB/Toon`에 `_BaseMap`(알베도) + `_EmissionColor`를 추가한다.** 원인은 셰이더가 아니라 **입력**이었다. 모델에 UV가 없어 파트마다 단색 머티리얼이었고, 그 단색이 그늘 램프를 곱하면 그대로 검게 읽혔다. 사용자가 UV를 붙인 뒤에도 `BRB/Toon`에 텍스처 프로퍼티가 **아예 없어** 그 작업이 통째로 버려지고 있었다(실측: 적용 직후 플레이어 51개 머티리얼 중 맵을 문 것 0개 → 수정 후 51/51). |
| 2026-09-10 | 램프의 그늘 하한 | **0.13 → 0.30.** 0.13이면 원래 어두운 재질(소매 0.27·방망이 0.34)이 곱해져 0.2 밑으로 가라앉아, 밝은 면 옆에서 형태가 사라진다. 어둡되 **형태는 읽히는** 선이 0.30 부근. `_Desaturate`도 0.30 → **0.18**(알베도가 이미 바랜 색이라 더 빼면 옷·피부 구분까지 죽는다). |
| 2026-09-10 | 노멀맵도 받을까 | **안 받는다.** `OutlineNormals`가 외곽선 밀기 방향을 `tangent`에 덮어써서 탄젠트 공간이 이미 깨져 있다. 쿼터뷰 거리에서 얻는 것도 거의 없다. |
| 2026-09-10 | 발광 파트(랜턴 액센트)가 단색 얼룩으로 보인다 | **`_EmissionColor` 추가**, 셀 음영 **뒤에** 더한다(램프에 섞으면 빛나는 느낌이 사라진다). 옮길 때 `_EMISSION` 키워드가 켜진 머티리얼만 — 끈 채로 색만 남아 있는 파트가 통째로 빛나는 걸 막는다. |

- **머티리얼 복사 경로 두 곳을 같이 고쳐야 한다** — `ToonMaterial.From`(적·소품)과 `ChibiPlayerVisual.Initialize`(플레이어). 색만 복사하던 코드라, 셰이더에 프로퍼티를 추가하는 것만으로는 아무것도 안 바뀐다.
- **`PlayerRig.character3DShader`를 다시 `BRB/Toon`으로.** 텍스처가 먹히지 않던 동안 비워 둔 상태라 플레이어만 URP/Lit(외곽선 없음)으로 나와 밴딧과 룩이 갈려 있었다.

#### 앞서 잘못 짚었던 것 (기록용)
- **"앰비언트 프로브가 낡았다"는 오진.** `DynamicGI.UpdateEnvironment()`를 넣고 다시 재보니 낮 L0가 **똑같이** 0.143/0.167/0.223, 밤은 0.004/0.005/0.008 — 프로브는 원래부터 설정을 잘 따라가고 있었다(0.143은 감마 0.52 하늘의 선형 SH 상수). 해당 수정은 되돌렸다.
- **외곽선도 원인이 아니었다.** 런타임 머티리얼 249개의 `_OutlineWidth`를 0으로 만들어도 팔은 그대로 어두웠다.

#### 남은 것
- **밴딧 모델은 아직 UV·텍스처가 없다**(실측: 66개 머티리얼 중 맵 0개, 메시 대부분 `UV 없음`). 지금은 파트 단색이라 그늘 쪽이 여전히 뭉친다 — 아트 작업이 붙어야 풀린다.

### 어깨가 통째로 검던 것 — 원인은 **메시 안팎 뒤집힘** (2026-09-10)

> 사용자: *"그럼 툰셰이더 안쓰면 되는거 아니냐 다른 셰이더를 구매해서 해볼까?"* → **셰이더 문제가 아니었다.**

`Hero_Sleeve_1/-1`(어깨 캡)이 어떤 조명·어떤 셰이더에서도 검게 나왔다. 단계별로 좁힌 기록:

| 테스트 | 결과 | 배제된 것 |
|---|---|---|
| 알베도만(URP/Unlit) 렌더 | 정상 올리브 0.27 | 텍스처·UV |
| 태양 그림자 끔 | 0.134 → 0.134 | 그림자 |
| 머티리얼 비교 | 재킷·롤소매와 **같은 인스턴스, 같은 값** | 머티리얼 |
| 법선 측정 | 정점 428개 **100%** 안쪽 (바깥향 −0.879 / 재킷 +0.833) | ← 원인 ① |
| 법선 수정 후 URP/Lit | 정상 라이팅 | 셰이더 |
| 법선 수정 후 툰 + 외곽선 패스 끔 | 정상 | ← 원인 ② 외곽선 |
| `Offset 1,200` 깊이 바이어스 | **여전히 검정** | 깊이 z-파이팅 |

**결론: 법선과 면 감김이 둘 다 뒤집혀 있었다(안팎 뒤집힘).** 그러면 `Cull Back`인 본체 패스는
안쪽면(먼 쪽)을, `Cull Front`인 외곽선 패스는 바깥면(가까운 쪽)을 그린다. 외곽선이 **항상 앞**이라
깊이를 아무리 밀어도 본체를 덮는다. 법선만 뒤집으면 라이팅 계산은 맞아 보여도 우리가 보는 면은
여전히 뒷면이라 증상이 그대로다 — 실제로 그 단계에서 한 번 헛짚었다.

| 날짜 | 질문 | 결정 |
|------|------|------|
| 2026-09-10 | 이 결함을 어디서 고치나 | **임포트 후처리(`Editor/MeshNormalRepair.cs`).** 근본 수정은 블렌더(Recalculate Normals Outside)지만 이 프로젝트의 캐릭터는 `tools/*.py`로 수시로 재익스포트된다 — 후처리로 두면 FBX를 다시 뽑아도 계속 유효하고, 같은 결함이 있는 다른 모델도 자동으로 걸린다. 판정은 **메시 중심 기준 바깥향 평균 ≤ −0.5**. 실측이 확실히 갈린다(정상 재킷 +0.833·롤소매 +0.804·팔뚝 +0.605 vs 결함 소매 −0.879, 그다음으로 가까운 값이 −0.076이라 오탐 여지 없음). 걸리면 법선·탄젠트 반전 **+ 삼각형 감김 반전**. |
| 2026-09-10 | 다른 셰이더를 사면 되나 | **아니다.** 법선은 모든 라이팅 셰이더의 입력이라 URP/Lit이든 유료 에셋이든 똑같이 검게 나온다. 실제로 `character3DShader`가 비어 URP/Lit으로 렌더되던 시점에도 어깨는 이미 검었다. |
| 2026-09-10 | 외곽선 헐을 미는 방향 | `GetWorldSpaceViewDir`(그 점 → 카메라 **위치**) → **카메라 정면 축**(`-UNITY_MATRIX_V[2].xyz`). 이 게임은 오소 카메라라 화면 중앙에서 벗어날수록 예전 방식이 헐을 **가로로** 밀었다. 여기에 `Offset 1, 4`를 z-파이팅 보험으로 추가(URP가 SRPDefaultUnlit을 UniversalForward 뒤에 그려서, 깊이가 비기면 외곽선이 이긴다). |

#### 판정식에서 뺀 것
와인딩과 정점 법선을 비교하는 판정을 먼저 넣었다가 뺐다. 임포트·런타임 모두 인덱스 버퍼를 못 읽어
`GetTriangles`가 0개를 돌려주는 메시가 대부분이라, 그 조건을 먼저 검사하면 중심 판정에 도달하지도
못한다(후처리기가 아무것도 못 잡았다). 지표로만 남겨 뒀다.

#### 남은 것
- **`Hero_Lapel_1/-1`** — 60% 안쪽이고 실측 휘도 0.076으로 어둡다. 정점 5개짜리 평면이라 중심 기준
  판정이 성립하지 않아 후처리기가 건드리지 않는다(정점 24개 미만 제외). 블렌더에서 고쳐야 한다.
- **밴딧 모델은 여전히 UV·텍스처가 없다** — 파트 단색이라 그늘 쪽이 뭉친다.
- 팔뚝의 흰 세로줄은 셰이더가 아니라 **새 텍스처에 칠해진 하이라이트**다(스펙큘러 없이도 남아 있다).

## 카툰 폐기 → 리얼리티 전환 (2026-09-10)

> 사용자: *"우리 세계관에 툰은 별로다 / 툰쉐이더 버리고 약간 리얼리티 느낌으로 가는 건 어때"*

| 날짜 | 질문 | 결정 |
|------|------|------|
| 2026-09-10 | 카툰을 계속 갈까 | **폐기.** 카툰을 고른 원래 이유는 "모델 완성도를 덮으려고"였는데, 그 사이 UV·텍스처·**노멀맵·마스크맵**이 저작됐다. `BRB/Toon`은 노멀맵을 일부러 안 받고(외곽선이 tangent를 덮어써서) 마스크맵도 안 읽어서, 그 작업을 통째로 버리고 있었다. 리얼 쪽으로 가면 그대로 쓰인다. |
| 2026-09-10 | 캐릭터를 뭘로 그리나 | **FBX에 저작된 URP/Lit 머티리얼을 그대로 쓴다.** 전환 = 오버라이드를 그만두는 것. ①`PlayerRig.character3DShader` → 비움(ChibiPlayerVisual은 셰이더가 null이면 프리팹 머티리얼을 유지) ②`BanditEnemyVisual`의 `ToonMaterial.ApplyTo` 제거 ③`LookDevScene.ApplyToon` → `PrepareRenderers`(그림자 설정만). `BRB/Toon`과 램프 빌더는 지우지 않고 남겨 뒀다 — 되돌릴 여지. |
| 2026-09-10 | 포스트프로세싱 | **신설 `Editor/PostProcessProfileBuilder.cs`** → `Resources/PlayerRigVolume3D.asset`. 이 프로파일은 **컴포넌트가 하나도 없는 빈 에셋**이었다. 톤매핑이 없으면 URP는 선형 값을 그대로 잘라 밝은 쪽이 뭉치고 전체가 물빠져 보인다 — 카툰일 땐 램프가 톤을 지정해서 티가 안 났지만 PBR에서는 룩의 절반이다. 구성: 톤매핑 Neutral(ACES는 대비를 세게 밀어 이미 어두운 팔레트에서 그림자가 뭉친다) · 노출 +0.35 · 대비 +12 · 채도 −18 · 화이트밸런스 −8 · 그림자 차갑게/하이라이트 탁한 온색 · 블룸 문턱 1.15(낮으면 밝은 옷까지 번진다) · 비네트 0.26 · 필름그레인 0.16. 룩 체크 씬도 **같은 프로파일**을 쓰는 전역 Volume을 갖는다. |
| 2026-09-10 | 옷이 코팅된 덩어리처럼 보인다 (사용자 지적) | **신설 `Editor/HeroMaterialTuner.cs`** — 이름 기준으로 재질을 분리한다. 원인: 저작된 머티리얼이 전부 `_Smoothness=1`·`_Metallic=1`이고 실제 값은 **하나의 공유 마스크맵**이 정해서(실측 A 0.14~0.40, R 평균 0.007), 천·가죽·금속이 **같은 곡선**을 탄다. 스칼라는 마스크에 곱해지므로 역할별로 눌러 주면 마스크의 결은 살리고 재질만 갈린다. 천 0.22 / 가죽 0.45~0.50 / 고무 0.28 / 나무 0.30 / 피부 0.30 / 머리 0.38 / 눈 0.70~0.80 / 금속 0.80~0.85(metallic 1) / 잉크 0.05. 105개 조정. |
| 2026-09-10 | 월드 셰이더의 앰비언트·림 (사용자 지적) | `BRB/Stylized`의 **앰비언트 ×1.25 → ×1.0**, **림 0.18 → 0.07**. 앰비언트를 임의로 부풀리면 명암이 씻겨 형태가 뭉근해지고, 림이 세면 모든 표면 가장자리가 똑같이 밝아져 재료 구분이 사라진다. 카툰일 땐 램프가 가려 줬지만 PBR에서는 그대로 드러난다. |

> ⚠️ **재익스포트 뒤에는 `캐릭터 재질 분리` 메뉴를 다시 돌릴 것.** 캐릭터 머티리얼은 `tools/*.py`가
> FBX를 뽑을 때 같이 재생성돼서, .mat을 손으로 고치면 다음 익스포트에 날아간다. 이 도구는 몇 번
> 돌려도 같은 결과라 재실행이 안전하다. (`법선 뒤집힘` 보정은 임포트 후처리라 자동으로 적용된다.)

#### 사용자 지적 중 대상이 달랐던 것
- **"림 0.3 / 앰비언트 ×1.25"** 는 `BRB/Toon`의 값이다. 캐릭터는 이미 툰을 안 쓰므로 그 값들은
  캐릭터에 닿지 않는다. 다만 **같은 두 항목이 `BRB/Stylized`(월드·소품)에도 있어서** 그쪽은 실재하는
  지적이었고, 위 표대로 고쳤다.
- **"재킷 0.55 vs 얼굴 0.28"** — 저작된 세트에 그 값은 없었다(실측: 전부 `_Smoothness 1`, 예외로
  `Hero_Face`만 0.1). 다만 "재질이 안 갈린다"는 관찰 자체는 맞았고, 원인은 공유 마스크맵이었다.

## 룩 작업 초기화 (2026-09-10)

> 사용자: *"우리 지금 만든 라이트 셰이더 이런거 다 초기화 해줄래 다시 만들꺼야"* — 모델을 새로
> 만들 예정이라, 룩 작업을 백지에서 다시 시작하기로 했다.

**현재 상태 = 캐릭터는 FBX에 저작된 URP/Lit 머티리얼로 그대로 렌더된다. 커스텀 룩 레이어 없음.**

### 지운 것
| 대상 | 무엇이었나 |
|---|---|
| `Shaders/Toon.shader` | 카툰 셀 셰이더(외곽선 인버티드 헐 + 램프) |
| `Editor/ToonRampBuilder.cs`, `Resources/Shaders/ToonRamp.png` | 라이트 램프 텍스처와 생성기 |
| `Scripts/Rendering/ToonMaterial.cs` | 카툰 머티리얼 생성 단일 창구 |
| `Scripts/Rendering/OutlineNormals.cs` | 외곽선용 평균 법선을 tangent에 굽던 것 |
| `Editor/PostProcessProfileBuilder.cs` | 포스트프로세싱 프로파일 생성기 |
| `Editor/HeroMaterialTuner.cs` | 이름 기준 재질 분리(천·가죽·금속) |

`PlayerRigVolume3D.asset`은 지우지 않고 **빈 프로파일로 되돌렸다**(PlayerRig가 참조한다).

### 남긴 것과 이유
- **`Editor/MeshNormalRepair.cs`** — 룩이 아니라 **모델 버그 픽스**다. 지우면 어깨가 다시 검게 나온다.
  임포트 후처리라 새 모델에도 자동으로 걸린다.
- **`BRB/Stylized`** — 그레이박스 월드 전체(`GreyboxMesh`, `Greybox3D`, `Safehouse3DLayout`)가 쓴다.
  지우면 맵이 통째로 깨진다. 이번 세션에 넣은 앰비언트 ×1.0·림 0.07 조정도 그대로 둔다.
- **`LookDevScene` / `LookDevCameraSwitcher`** — 룩 판단 도구. 새 룩을 만들 때 그대로 쓴다.
  카메라에 `renderPostProcessing = true`가 켜져 있어, 프로파일만 채우면 바로 보인다.
- **기존 조명 시스템**(`WornLamp`, `DayNightCycle`, `Lighting3D`, `WeatherData`) — 이번 세션 산물이 아니다.

### 다시 만들 때 참고
- **쿼터뷰 기준으로 판단할 것**(2026-09-10 사용자). `LookDevScene`의 `Cam_3_쿼터뷰(게임)`가
  게임 설정(pitch 55° · yaw 0° · ortho 3.6)을 그대로 갖고 있다. 정면샷으로만 맞추면 인게임에서 갈린다.
  ⚠️ 그 세 상수는 PlayerRig와 **수동 동기화**다.
- **포스트프로세싱 프로파일은 빈 상태**다. 톤매핑이 없으면 URP는 선형 값을 그대로 잘라
  밝은 쪽이 뭉치고 물빠져 보인다 — PBR 룩에서는 이게 먼저다.
- Forward+ 전환은 광원 제한을 풀지만, `BRB/Stylized`가 `GetAdditionalLightsCount()`를 그대로 도는
  방식이라 **광원을 놓친다**(이 저장소가 이미 겪은 버그 `637e7cc`). 셰이더 수정이 동반돼야 한다.

## 셰이더 전면 백지화 (2026-09-10)

> 사용자: *"지금까지 만들어둔 셰이더 다 제거해버려 처음부터 만들테니까 … 한개씩"*

**커스텀 셰이더 13개를 전부 삭제했다.** `Assets/Shaders/`가 비었다.

```
BRB/Stylized   BRB/VisionDarkness  Spike/OccluderFX   BRB/AnomalyFog
BRB/DamageOverlay  BRB/SpriteFlash  BRB/PlayerSprite  BRB/SpriteSheet
BRB/SpriteBillboard  BRB/FloorPixel  BRB/WallPixel  BRB/PropPixel  BRB/DecalPixel
```

코드 12개 파일의 `Shader.Find("BRB/…")` / `Shader.Find("Spike/…")` 를 전부
**`Universal Render Pipeline/Lit`** 으로 돌렸다 — 안 그러면 `Shader.Find`가 null을 반환해
맵이 통째로 마젠타로 뜬다.

### 같이 사라진 기능 (다시 만들어야 함)
컷어웨이(`Spike/OccluderFX` — 씬·프리팹 18곳 참조) · 시야 어둠 · 피격 플래시 ·
파손/풍화 오버레이 · 이상현상 안개.
**씬·프리팹에 guid로 박힌 머티리얼은 마젠타로 뜬다** — 그레이박스 씬은 빌더 메뉴로 재생성하면 된다.

### 다시 만들 때 반드시 피해야 할 함정 두 개 (실측으로 확인함)

**① 추가 광원 그림자 — `GetAdditionalLight`는 오버로드를 골라 써야 한다.**
사용자 지적: *"라이트가 전등에 달렸는데 마치 하늘에서 보는 것마냥 장애물 건너편도 보이더라"*.
원인은 URP 소스에 있었다(`RealtimeLights.hlsl:245`):
```hlsl
Light GetAdditionalLight(uint i, float3 positionWS)                   // 2인자
{ return GetAdditionalPerObjectLight(lightIndex, positionWS); }       // ← shadowAttenuation 안 건드림

Light GetAdditionalLight(uint i, float3 positionWS, half4 shadowMask) // 3인자
{ ... light.shadowAttenuation = AdditionalLightShadow(...); }         // ← 여기서만 그림자
```
옛 `BRB/Stylized`는 2인자를 써서 `AL.shadowAttenuation`이 **항상 1**이었다. 그래서 착용 랜턴
빛이 벽·상자를 그냥 통과했다. **반드시 `half4(1,1,1,1)`을 넘기는 3인자 쪽을 쓸 것.**

**② Forward+로 바꾸면 `GetAdditionalLightsCount()`가 0을 돌려준다.**
같은 파일 292행 — 클러스터 방식에서는 개수를 미리 셀 수 없어 0이다. 지금처럼
`for (i < GetAdditionalLightsCount())`로 도는 셰이더는 Forward+에서 **추가 광원이 통째로 사라진다.**
Forward+를 쓰려면 URP의 `LIGHT_LOOP_BEGIN/END` 매크로로 바꿔야 한다.
(이 저장소는 이미 비슷한 사고를 한 번 겪었다 — `637e7cc` "추가 광원을 아예 안 받던 문제".)

### 2D 잔재 정리 — **여기까지만 가능했다**
지운 것: `Settings/Renderer2D.asset`, `Settings/URP-2D.asset`(둘 다 어디서도 참조 안 됨),
`Editor/MapTool2DSceneBuilder.cs`(참조 0).

⚠️ **나머지 2D는 못 지운다. 3D 맵 빌더가 그 위에 서 있다.**
`GreyboxBuild.PrefabRoot = "Props2D/Prefabs/"` 이고, `Zone1GreyboxLayout`·`InteriorBuild`·
`Zone1Interiors`·`Map3DBuild`가 전부 `GreyboxBuild.Floor/Wall/Prop/Car/…`를 부른다.
즉 3D 그레이박스 맵은 **2D 프롭 프리팹 63개를 인스턴스화해서** 만들어지고, 이미 저장된
씬들도 그 프리팹을 guid로 물고 있다. 한 번 지웠다가 이 사실을 발견하고 되돌렸다.
`Prop2D*` · `PropLight2D` · `GroundShadow2D` · `MapTriggerZone2D` · `GreyboxPaletteBuilder` ·
`Prop2DCatalogEditor` · `PropSyncMenu` · `Resources/Props2D` 는 **전부 살아 있는 자산이다.**
진짜로 걷어내려면 `GreyboxBuild`를 `GreyboxMesh`(절차적 박스) 위로 옮기는 포팅이 선행돼야 한다.

## 게임 전용 셰이더 + 포스트 프로세싱 (2026-09-11 결정 · 구현 중)

> 사용자: *"다 작업하고 우리 게임에 맞는 셰이더 하나 만들어보자, 포스트 프로세싱도 그렇고"* — 방향성 [gdd-core §게임 방향성](gdd-core.md).
> 룩 레퍼런스: Into the Dead: Our Darkest Days 분위기(사용자 "분위기는 딱 내가 원하는 건데"), 툰은 폐기(2026-09-10).

| 날짜 | 질문(선택지) | 사용자 결정 |
|---|---|---|
| 2026-09-11 | 적용 대상 — 캐릭터+환경 공용 / 캐릭터만 / 환경만 | **캐릭터 + 환경 공용** — 게임 전용 Lit 셰이더 하나를 플레이어·밴딧·건물·소품에 같이 |
| 2026-09-11 | 룩 — 어두운 사실풍 / 살짝 스타일라이즈드 / 레퍼런스 제공 | **어두운 사실풍** — 채도 낮고 그림자 깊게, 빛이 닿는 곳만 살아나고 먼지·때 질감 |
| 2026-09-11 | 포스트 프로세싱 — 색보정·톤매핑 / 비네팅·필름 그레인 / 블룸 / 깊이 안개·피격 효과 (복수) | **색보정·톤매핑 + 블룸 + 비네팅·필름 그레인** (깊이 안개·피격 효과는 제외) |
| 2026-09-11 | 에디터 — 다른 세션이 끝나면 / 지금 같이 | **지금 같이 써도 됨** |

- 새 셰이더는 위 **함정 두 개**를 처음부터 피한다: 추가 광원은 3인자 `GetAdditionalLight(i, posWS, half4(1,1,1,1))`로 그림자를 받고, 루프는 `LIGHT_LOOP_BEGIN/END`(Forward+ 대응).

### 구현 (2026-09-11)

**셰이더 `BRB/GameLit`** (`Assets/Shaders/GameLit/`) — 조명은 URP `UniversalFragmentPBR`에 맡긴다. 그러면 위 함정 두 개가
구조적으로 사라진다(URP 내부가 그림자 받는 추가 광원 오버로드와 `_CLUSTER_LIGHT_LOOP` 루프를 쓴다 — URP 17, Unity 6.6 확인).
이 셰이더가 더하는 건 조명 앞뒤의 룩뿐:
1. **때(grime)** — 절차적 노이즈. 잘게(두 옥타브), 벽 밑동(바닥 가까운 세운 면)에 모이고, 윗면엔 약하게. 더러운 곳은 무광.
   캐릭터는 오브젝트 공간(걸어도 무늬가 몸에 붙어 있다), 환경은 월드 공간.
2. **그늘 채도 빼기** — 받은 빛이 적을수록 잿빛(`_ShadowDesaturation`). "빛이 닿는 곳만 살아난다".
3. **실루엣 림** — 캐릭터만(0.12). 어둠 속에서도 형태가 읽히게. 쿼터뷰(오소 55°) 카메라 기준 가장자리.
- 속성 이름은 URP/Lit과 같다(`_BaseMap`·`_BaseColor`·`_BumpMap`·`_Smoothness`·`_Metallic`·`_OcclusionMap`·`_EmissionColor`) —
  셰이더만 바꿔도 값이 넘어오고, `_BaseColor` MaterialPropertyBlock(피격 틴트 등)도 그대로. SRP Batcher 호환(코드 0 확인).
- 패스: ForwardLit · ShadowCaster · DepthOnly · DepthNormals(SSAO용). 불투명 전용 — 투명 머티리얼은 URP/Lit 유지.
- **룩 A/B 전역 스위치** `Shader.SetGlobalFloat("_GameLitLookOff", 1)` → 때·채도·림을 끄고 순수 PBR. 머티리얼을 안 건드리고 비교할 때.

**전환 도구 `Tools ▸ TopDown ▸ 렌더 ▸ 게임 셰이더로 전환`** (`Editor/GameLitConverter.cs`) — URP/Lit → GameLit, 키워드(노멀·AO·발광·알파 자르기) 자동,
캐릭터(경로에 ChibiSurvivor·Characters·Bandit)/환경 룩 값. 이미 GameLit이면 룩 값만 다시 넣는다. **되돌리기** 메뉴도 있다.
씬은 추가로 열었다 닫는다(열려 있는 작업 씬을 안 닫게). 다른 세션 작업 중인 `Hideout02` 폴더·`Hideout` 씬은 건너뛴다.
- 2026-09-11 실행: 머티리얼 에셋 캐릭터 60 · 환경 88, 씬에 박힌 것 Safehouse 35 · Zone1 6 · 고철시장 5 · 실내 15곳 4~5 · 룩씬 9.

**포스트 프로세싱** (`Editor/PostProfileBuilder.cs` → `Resources/PlayerRigVolume3D.asset`, PlayerRig의 전역 Volume 하나) —
원칙은 그대로(색조는 조명이 만든다 — 파랑/노랑 밀기 없음). NPC 적용 후 검수 기준: 노출 **0** · 대비 **10** · 채도 **−8** ·
블룸 문턱 **1.10**/세기 **0.18** · 비네트 **0.20** · 그레인 **0.06** · 톤매핑 Neutral 유지 ·
**색 보정 HDR**(URP 에셋이 HDR인데 그레이딩만 LDR이던 것).

**확인 (Systems에서 시작)**
- Zone1: 전환 전 GameLit 4 / URP/Lit 51 → 후 GameLit 53 / URP/Lit 0(주변 머티리얼 슬롯). 콘솔 에러 0.
- 마을(Safehouse 씬, 사용자 "셰이더는 마을에서 확인하면 될 듯"): GameLit 2605 슬롯, 콘솔 에러 0. 포스트 프로세싱 on/off 차이는 뚜렷(어둡고 바랜 색),
  룩 효과는 텍스처 있는 바닥에서 잔 때 얼룩 정도로 **은은하다** — 세기 조절은 사용자 확인 후.
- ⚠️ 1차 때 값(낮은 주파수 0.6 + 바닥·윗면 가산)은 바닥·지붕에 큰 얼룩이 깔려 **구름·안개처럼** 보였다 → 잘게(2.5)·약하게(0.35)·벽 밑동 위주로 수정.

⚠️ **병합 순서** — 다른 세션이 작업 중인(아직 커밋 안 된) `Town02` 머티리얼과 `Safehouse.unity`에 박힌 머티리얼도 전환 도구가 GameLit으로 바꿨다.
그 작업을 이 셰이더보다 **먼저** 병합하면 해당 머티리얼이 마젠타가 된다 → 셰이더 PR을 먼저 병합하거나, 되돌리기 메뉴로 URP/Lit으로 돌린 뒤 병합할 것.

**아직 셰이더가 없어 꺼져 있는 기능**(2026-09-10 백지화 때 사라진 것, 이번 범위 밖): 이상현상 안개(`BRB/AnomalyFog` — 매 프레임 경고),
시야 어둠(`BRB/VisionDarkness`), 피격 플래시(`BRB/SpriteFlash`), 파손 오버레이(`BRB/DamageOverlay`), 벽 픽셀(`BRB/WallPixel`).

## 쿼터뷰 카메라 · 착용등 · 분위기 · 반짝임 (2026-09-11)

| 날짜 | 질문(선택지) / 사용자 지시 | 사용자 결정 |
|---|---|---|
| 2026-09-11 | "그 쿼터뷰가 아닌데 아직" — 같은 자리 4장 비교: ① 55°·정북(지금) ② 40°·정북 ③ 대각선 45°·45° ④ 대각선 35°·45° | ~~③ 대각선(방위 45°) · 45° 내려봄~~ → 같은 날 아래 결정으로 교체. 이동은 화면 기준(W = 화면 위) 그대로 |
| 2026-09-11 | "탑다운이 나을 것 같은 느낌" — 전당포 앞 같은 자리·줌 4장 비교: ① 대각선 45°(지금) ② 정면 55° ③ 정면 62° ④ 정면 70°. 추천 ③(총격전 거리감이 방향 무관하게 고름 — 세로 압축 45° 71% → 62° 88%, 건물 앞면도 보임, 70°는 캐릭터가 차양 밑에 묻힘) | **③ 정면 탑다운 62° (방위 0°)** |
| 2026-09-11 | "빛반사가 좀 심하네, 반짝반짝 대리석 같아" | 반짝임 줄이기 — 환경 반사(하늘) 끄고, 금속 아닌 환경의 스펙 하이라이트 끔 |
| 2026-09-11 | "주변광이 머리 위에 태양 있어요 하는 것처럼 되어 있는데 이상하다, 바닥에 모듈처럼 그림자가 나온다" + "캐릭터 전등은 몸에 달려 있는 곳에서, 앞을 보는 빛 — 좀보이드 같은 거" | **착용등 = 몸에 단 등의 앞쪽 빔(좀보이드식)이 주인공.** 머리 위 1m에서 내리비추던 몸 글로우는 가슴 높이·카메라 쪽으로 옮기고 약하게, 둘레 주변광은 좁히고(4.6→2.4m) 그림자를 꺼 바닥의 방사형 그림자를 없앤다 |
| 2026-09-11 | "좀 밝긴 해, 조명 자체라기보단 분위기 자체가 — 낮엔 따뜻한 주황 느낌, 저녁엔 어둑어둑한 보라색 느낌" | **낮 = 따뜻한 주황, 저녁(밤 페이즈) = 어둑한 보라.** 전체 밝기도 낮춘다. 색은 조명(WeatherData)에서 만든다 — 포스트 프로세싱 원칙(색조는 조명이) 그대로 |

- 카메라: `CameraFollow.viewPitch/viewYaw`(**62/0**)가 진실 — Start에서 돌린 뒤 추적 오프셋을 잡는다. PlayerRig 프리팹 카메라·룩씬 상수(`LookDevScene.GamePitch/GameYaw`)도 같은 값. 이동·패드 조준 스틱은 `TopDownPlayer.CameraRelative`로 화면 기준.
- 반짝임: `BRB/GameLit`에 `_ENVIRONMENTREFLECTIONS_OFF`(전 머티리얼) · `_SPECULARHIGHLIGHTS_OFF`(금속 아닌 환경) — 전환 도구가 켠다. 쿼터뷰는 바닥을 비스듬히 봐서 하늘 반사(프레넬)가 크고, 몸에 단 등이 바닥에 하이라이트를 찍었다.
- 착용등: 앞쪽 빔(스포트 56°, 그림자 켬, 그림자 해상도 높음 등급) 유지. 주변광은 그림자 끔 — 반경이 2.4m로 작아 벽 너머로 새는 거리가 짧다(좀보이드식 "내 발밑은 보인다"만).
  PlayerRig 프리팹 직렬화 값: 주변광 반경 4.6→**2.4** · 그림자 **끔** / 몸 글로우 반경 2.6→**1.6** · 세기 0.9→**0.35** · 높이 1.0→**0.1**(+카메라 쪽 0.8m).
- **카메라 전환 후속**(방위 0 가정을 찾아 고친 것): 나침반 바늘(`NavigationHUD` — 화면 방향으로 잰다) · 적 "?" 표시 빌보드 ·
  카메라 셰이크·데미지 숫자 흔들림을 카메라 축으로 · PlayerRig 프리팹 카메라도 45°/45°로 굽기 · 그림자 거리 30→40 ·
  실내 태양 방위 25°→−40°(카메라와 20°밖에 안 벌어져 그림자가 물건 뒤로 숨었다) · 룩씬 카메라 상수 45/45 · QA 봇 이동 역변환.
  덤으로 잡은 버그: 적 넉백이 `z`를 지워(2D 잔재) X축으로만 밀리던 것 → 평면(`y` 제거).
- 정면 62°로 돌아오며 풀린 것: 미니맵·지도(정북 고정)가 화면과 같은 방향 · 마을 간판(55°로 굳음)과 7° 차이뿐이라 정면으로 읽힌다.
  **남은 것**: 문·간판은 **남쪽 면**(카메라 쪽)에 둬야 보인다 — 북쪽 면은 가려지고 동·서쪽 면은 옆으로 누워 거의 안 보인다(맵 배치 규칙).
- 분위기 = `Resources/Data/WeatherData.asset`(낮/밤 두 페이즈 — 밤 페이즈를 "저녁"으로):

| | 이전 | 지금 |
|---|---|---|
| 낮 태양 | 주황(1, .78, .55) | 세기 **0.95** · **따뜻한 빛**(1, .93, .84) · 고도 **58°** (2026-09-11 분위기 후속) |
| 낮 앰비언트 | 갈색(.30, .25, .20) | **차분한 주변광**(.29, .32, .35) — 따뜻한 주광과 분리 |
| 밤 빛 | 세기 0.18 · 청색(.55, .62, .85) | 세기 **0.38** · **보라**(.66, .58, .85) · 각도 **(58,-35,0)** |
| 밤 앰비언트 | (.10, .11, .15) | **보라**(.20, .17, .27) |
| 밤 안개 | 거의 검정(.02, .02, .03) | **보랏빛 어둠**(.06, .04, .09), 밀도 **0.022** |
