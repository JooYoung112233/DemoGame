# 캐릭터 모델링 작업 기준

## 달리기 검토 진행 — 2026-09-09

- 질문/제안: 걷기 v2의 몸 전체 움직임을 기준으로 다음 달리기를 하나만 만들까?
- 사용자 선택·결정: **“좋다 많이 개선되었네”**, 이어서 **“달리기해보자”**. 걷기 v2를 승인 기준으로 삼고 달리기만 새로 제작한다.
- 기준: 골반의 체중 이동, 어깨의 반대 회전, 머리·가방의 시간차를 유지한다. 달리기는 상체 전경, 굽힌 팔 스윙, 착지 압축과 반발, 뒷발 회수 및 짧은 공중 구간을 추가한다.
- 참고: 최초 영상 원본은 현재 Downloads에서 찾지 못해 이전에 추출한 11.208/11.833/12.458초 프레임을 다시 확인했다. 정밀 모션 캡처나 원본과 동일한 타이밍이라고 주장하지 않는다.
- 검토본: [RunReview/CompactSurvivor_RunReview.blend](../Assets/ChibiSurvivor/Player/RunReview/CompactSurvivor_RunReview.blend). 승인된 WalkReview를 기반으로 Run만 변경한다. 승인 걷기·기존 나머지 4개 액션과 외형/가중치는 보존한다. 원본 Player 및 Unity 연결 파일은 아직 교체하지 않는다.
- 제작 기준: 30fps, 1~25f, 0.8초 루프. 이동 참고 속도 약 1.094m/s는 애니메이션 접지용이며 게임 속도 결정이 아니다. 현재 달리기는 사용자 확인 전이다.
- 제작 확인: 루프 경계 정점 오차 0, 접지 높이/이동 보정 후 동일 접촉 정점 미끄러짐 0.000001m 미만, 강제 골반 높이 보정 0. 한 주기 중 양발 공중 프레임 4개. 손과 옷·바지·파우치·가방 표면 교차 없음. [실제 Blender 비교 영상](../Assets/ChibiSurvivor/Player/RunReview/RunComparison_v1.mp4)은 왼쪽 기존/오른쪽 새 달리기이며, 같은 카메라에서 각 클립 고유 속도로 재생한다.

## 현재 진행: 영상 기준으로 한 모션씩 컨펌 — 2026-09-09

- 사용자 결정: **“걷기, 달리기, 한손, 양손 으로 영상 다시 줄테니 애니 1개씩 만들고 컨펌 받자”**, **“걷기 만일단 해볼까”**. 걷기 v2는 후속 긍정 평가로 승인되었으며, 현재 달리기 한 개만 검토한다. 다음 공격 모션은 달리기 확인 후 진행한다.
- 최초 참고 파일: 사용자 Downloads의 `걷기_달리기도_다시만들어_주고_지금_만들어준_한손_양.mp4`, 새 걷기 구간 약 9.67~10.75초. 후속 기준은 `뭘만든거야_걷기부터_만들어달라니까.mp4`의 뒤쪽 걷기다. 남아 있는 Run 자막보다 실제 발걸음과 몸 움직임을 기준으로 삼는다. 정확한 모션 캡처 복제라고 주장하지 않는다.
- 검토안: [WalkReview/CompactSurvivor_WalkReview.blend](../Assets/ChibiSurvivor/Player/WalkReview/CompactSurvivor_WalkReview.blend). 기존 Player 제작 파일을 보존하고 별도 파일의 Walk만 수정했다. 외형/가중치 및 나머지 5개 액션을 유지한다.
- 후속 피드백: **“달라진게 없는데”**, **“몸이 움직이면서 자연스러운 느낌이 나오잖아”**. 첫 검토안은 미승인이다. 발 궤적 중심의 작은 수정으로 충분하다고 판단하지 않는다. 골반과 상체 회전을 과하게 상쇄하여 몸통이 고정되는 실패를 피한다.
- 현재 v2 방향: 지지하는 다리의 길이로 골반 높이를 결정하고, 골반/어깨의 반대 회전·좌우 체중 이동·머리/가방의 시간차를 적용한다. 실제 몸통 좌우 회전 폭 약 2→14도, 머리 좌우 이동 폭 약 3.6→6.2cm. 뒤꿈치 접지/발끝 밀기 유지. 30fps 1~37f, 1.2초 루프, 기준 이동 속도 약 0.472m/s. 게임 속도 변경 결정이 아닌 클립 제작 기준이다.
- 확인: 루프 경계 정점 오차 0, 공중에 양발이 뜨는 프레임 0, 실제 부츠 접지 높이/동일 접촉 정점의 이동 보정 후 미끄러짐 0.000001m 미만, 다리 도달 거리 때문에 골반을 강제로 낮춘 양 0. [자세 비교](../Assets/ChibiSurvivor/Player/WalkReview/WalkPoses.png). 걷기 v2는 승인되었으며 Unity 연결 전이다.
- 생성/현재 비교: `tools/build_walk_review.py`, `tools/preview_walk_body_review.py`. [v2 동일 프레임 비교](../Assets/ChibiSurvivor/Player/WalkReview/WalkBodyComparison_v2.png). v1은 `ArtSource/CharacterArchive/2026-09-09/WalkReview_v1`에 보존했다. 현재 Player 본체는 교체하지 않았으며 걷기 v2를 승인 기준으로 보존한다. 아래 이전 공격 결과도 새 참고 모션의 승인으로 간주하지 않는다.

## 현재 승인 상태 — 2026-09-08

- 사용자 방향 전환: **“다 필요없고 요걸 비율을 좀 줄이면 좋아보이는데 어때”**, [선택한 이미지](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/KakaoTalk_20260908_170010839.png) 기준으로 **“작업해줘”**.
- 질문/제안: 이 이미지의 머리·얼굴·모자를 유지하고 몸통 길이 약 10%, 하체 길이 약 20~25%를 줄여 약 2.6등신으로 만들까?
- 사용자 결정: 해당 비율 조정 작업 진행. 이전 ProportionStudy→FaceStudy→귀 유무 수정 흐름은 중단한다. 아래 얼굴 연구 기록은 현재 적용 지시가 아닌 이력이다.
- 확정된 원본: 이미지와 동일한 렌더를 생성한 [ThreeHeadSurvivor.blend](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/ThreeHeadSurvivor/ThreeHeadSurvivor.blend). 얼굴·귀·헤어·모자와 기존 색감/재질은 이 원본에서 유지한다.
- 현재 제작안: [CompactSurvivor.blend](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/CompactSurvivor.blend), [원본과 같은 축척의 비교 렌더](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Comparison.png). 사용자가 **“좋은데? 근데 모션 애니 작업이 가능할까”**라고 긍정 평가했다. 다음 리깅/모션 검토의 외형 기준으로 삼는다.
- 구현 수치: 모자 포함 머리 높이 0.6 유지, 전체 높이 1.8→1.5648(3.0→2.608등신). 몸통 길이 90%, 허리~바닥 높이 75%. X/Y 폭·깊이는 유지한다.
- 손·시계·벨트·허리 파우치는 길이로 눌러 변형하지 않고 새 위치로 옮긴다. 나머지 옷·팔·배낭·바지·부츠는 해당 높이 구간에 맞춰 줄인다. 원본과 13개 분리 부품 구조를 보존한다.
- 산출물: Blender, FBX, GLB 및 정면/사선/뒷면/비율 비교 렌더. 정적 모델 수정이며 리깅·애니메이션·Unity 연결 완료가 아니다. 재생성: [build_compact_survivor.py](../tools/build_compact_survivor.py).
- 앞으로도 적용할 원칙: 시안의 분위기 우선, 비율 먼저 확인, 부위별 수정, 모듈 분리, 조명·셰이더까지 고려. 특정 얼굴 연구안의 모양이나 귀 제거를 모든 캐릭터에 강제하지 않는다.

## 모션 제작 가능 여부 — 2026-09-08

- 사용자 질문: 현재 CompactSurvivor로 모션 애니메이션 작업이 가능한가?
- 사용자 후속 결정: **“해보자”**. 현재 외형에 리깅과 기본 대기·걷기·뛰기 3개를 먼저 제작한다. 공격/총 자세와 Unity 연결은 이후 단계다.
- 현재 작업: 원본 정적 모델과 별도로 `CompactSurvivor/Animated/`에 23개 뼈와 13개 분리 부품을 유지하는 골격 및 기본 모션을 제작했다. 사용자는 **“모션은 좋은데 손자체가좀이상한데?”**라고 모션을 긍정 평가하고 손 형태 수정을 요청했다.
- 제작 방향: 현재 외형에 공통 골격과 관절 가중치를 넣고, 모자/머리카락은 머리, 가방은 몸통, 시계는 손목을 따라가도록 연결한다. 옷·장비의 부품 분리는 유지한다.
- 제안 순서: 리깅과 관절 변형 확인 → Idle/Walk/Run → 앞서 논의한 한손/양손 근접 공격과 한손/양손 총 자세. 짧은 팔·다리에서 무기 손잡이 위치와 어깨/무릎 변형은 실제 포즈로 확인한다.
- 클립 방식: 제자리(in-place) 루프. 발이 땅에 닿는 구간의 이동 속도를 일정하게 만들고, 게임에서 캐릭터 이동 속도와 모션 재생 속도를 맞춘다. 미리보기 영상에는 발 접지를 보기 위한 가상 전진 이동을 적용할 수 있으나 FBX/GLB 클립에는 전진 루트 이동을 넣지 않는다.
- 골격: 몸통·팔·다리와 머리, 배낭을 연결하고 양손의 무기 부착 위치를 마련한다. 표정/손가락/공격 모션은 이번 기본 이동 범위에 포함하지 않는다.
- 완료 여부는 산출물과 아래 변경 이력을 따른다. Unity 게임 연결/검증 완료를 의미하지 않는다.

### 손 형태 확정 방향 — 2026-09-08

- 질문/피드백: 모션은 좋으나 손 자체의 형태가 어색하다. 손가락 형태를 다듬을까?
- 사용자 결정: **“손가락 없애고 그냥 동그란 손으로 하자”**.
- 적용: 엄지와 네 손가락 표현을 모두 없애고 양손을 둥근 덩어리로 단순화한다. 손목 연결, 모션 키, 다른 부위와 13개 메시 분리는 유지한다. 이후 캐릭터의 손 표현에도 이 간결한 방향을 참고하되 개별 캐릭터 요청을 우선한다.
- 최신 수정안: [RoundHands/CompactSurvivor_Animated.blend](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated/RoundHands/CompactSurvivor_Animated.blend). 이전 애니메이션 파일은 원본으로 보존한다. 아래 기본 모션 파일 링크들은 손 수정 이전의 기반 버전이다.
- 확인 결과: 손 외 12개 메시와 모든 모션 키가 그대로임을 확인했다. 둥근 손의 FBX 재임포트/GLB 스킨·3개 클립 검사를 통과했다. [최신 모션 영상](../Assets/ChibiSurvivor/Player/LocomotionPreview.mp4)도 갱신했다.
- 재생성: [round_compact_hands.py](../tools/round_compact_hands.py). 영상/내보내기 검사는 기존 도구에 `-- --round-hands`를 붙여 최신 손 버전을 대상으로 한다.

### 현재 공격 구성: 한손 찌르기 + 양손 베기·내려찍기 — 2026-09-08

- 질문/제안: 참고 영상처럼 단검을 제외한 베기·내려찍기를 양손으로 만들고 무기 모델만 교체할까?
- 최신 사용자 결정: **“단검 빼곤 양손으로 해버리는게좋지않을까 모션은 총 3개로 고정하고 손에 든것바꿔서”**, **“작업 해보지”**. 제자리 공격 3개로 고정한다.
- 현재 클립: `Attack_OneHand_Thrust`(단검, 0.9초), `Attack_TwoHand_Slash`(검, 1.3초), `Attack_TwoHand_Chop`(도끼/망치, 1.4초). Idle/Walk/Run과 단검 키, 외형·둥근 손·13개 메시·23개 뼈를 유지한다. 이전 한손 베기/내려찍기는 현재 FBX/GLB에서 제외하고 보관한다.
- 양손 기준: 오른손 무기 좌표계의 원점에 주 손잡이, 로컬 Y=-0.11m에 왼손 보조 손잡이. 하나의 무기 좌표계로 두 손을 함께 계산하여 공격 중 간격·방향을 유지한다. 검과 도끼는 같은 간격을 쓰며 미리보기의 무기별 `Grip.R`/`Grip.L`에서 기준점을 확인한다. 다른 간격의 무기는 추후 손 위치 보정이 필요하다.
- 양손 모션은 두 손으로 잡은 준비 자세에서 시작/종료한다. 기존 이동/단검 동작과의 전환 및 무기 교체/IK는 Unity 연결 단계에 남아 있다. 모션 제작만으로 런타임 장착 시스템이 완료된 것은 아니다.
- 도구: `tools/build_twohand_attacks.py`, `tools/preview_onehand_set.py`. 현재 [Blender 작업 파일](../Assets/ChibiSurvivor/Player/CompactSurvivor_Combat.blend). 이전 한손 버전은 `ArtSource/CharacterArchive/2026-09-08/BeforeTwoHandAttacks`에 보존한다. 세부 모션은 사용자 검토 전이다.
- 제작 확인: 양손 위치 간격 오차 0.000001m 미만, 팔 길이 강제 제한 0, 발 접지/준비 자세 복귀 확인. 검·도끼·단검 111프레임의 날/무기 머리 표면 교차 없음. FBX 재임포트·GLB에 공격 3개와 이동 3개를 확인했다. [양손 베기 자세](../Assets/ChibiSurvivor/Player/Attack_TwoHand_Slash_Poses.png), [양손 내려찍기 자세](../Assets/ChibiSurvivor/Player/Attack_TwoHand_Chop_Poses.png). Unity 검증 전이다.

### 이전 한손 공격 구성 — 2026-09-08

- 질문/피드백: 이동하면서 공격하도록 나눌까? **“그리고 좀더 역동적이여도 좋을것 같아”**.
- 최신 사용자 결정: **“베기는 검일때 쓰고 단검은 찌르기 도끼같은건 내려찍기로”**, **“한손 모션은 요 3개면”**, **“멈춰서 공격해도 좋을것 같네”**. 이번 제작 기준은 제자리 전신 공격 3종으로 전환한다. 이동 공격 레이어는 보류한다.
- 구성: 검 `Attack_OneHand`, 단검 `Attack_OneHand_Thrust`, 도끼 `Attack_OneHand_Chop`. 현재 파일은 [Player/CompactSurvivor_Combat.blend](../Assets/ChibiSurvivor/Player/CompactSurvivor_Combat.blend). 기존 두 공격과 Idle/Walk/Run, 외형·둥근 손을 유지하고 내려찍기만 추가한다.
- 내려찍기 제작안: 30fps, 1~40f(1.3초), 타격 기준 17f. 어깨를 들어 올려 준비하고 몸통 회전·무릎 압축을 겹쳐 내리친 뒤 천천히 회수한다. 양발 접지와 Root 위치는 고정한다. 세부 모션은 사용자 확인 전이다.
- 재사용: 중간 포즈마다 정지하지 않도록 연속 경로를 사용하고, 무게감은 준비/가속/회수의 시간 차와 몸통·팔의 시차로 표현한다. 모션 검토용 무기는 별도 Preview 파일에만 둔다. Unity 연결·타격 판정·무기별 선택 코드는 아직 구현하지 않았다.
- 도구: `tools/build_onehand_chop.py`, `tools/preview_onehand_set.py`. 보류한 이동 공격 실험은 `ArtSource/CharacterArchive/2026-09-08/DeferredMovingAttack`에 보존한다.
- 당시 Blender 확인: 기존 5개 액션·13개 메시·가중치 보존, 내려찍기 팔 길이 강제 제한 0, 양발 위치 오차 약 0.0000002m, 첫/끝 자세 오차 0. FBX 재임포트와 GLB에서 총 6개 클립을 확인했고, 세 공격 108프레임의 검토용 날·도끼 머리 표면 교차는 없었다. [이전 내려찍기 자세](../ArtSource/CharacterArchive/2026-09-08/BeforeTwoHandAttacks/SupersededPlayerFiles/Attack_OneHand_Chop_Poses.png). Unity 검증 전이다.

### 한손 찌르기 비교안 — 2026-09-08

- 사용자 피드백: **“나쁘진않은데 찌르기가 좀 좋지않을까”**. 베기를 남기고 찌르기 비교안을 제작한다. 기본 공격을 찌르기로 최종 확정한 것은 아니다.
- 후속 피드백: **“너무 단계적인 찌르기 같네요”**. 준비·최대 신전·회수마다 멈추던 초기 1.1초안은 비교용으로 보존하고 연결감을 수정한다.
- 최신 작업 파일: [Combat/Thrust/Fluid/CompactSurvivor_Combat.blend](../Assets/ChibiSurvivor/Player/CompactSurvivor_Combat.blend). 기존 베기·이동 네 모션과 외형·스킨 가중치를 보존한 `Attack_OneHand_Thrust` 수정안이다.
- 현재 제작안: 30fps, 1~28프레임(0.9초), 최대 찌르기 11f. 준비 자세를 통과하면서 가속하고 최대 신전의 정지를 제거했다. 회수는 아래로 이어지는 곡선이며 골반·몸통이 팔보다 먼저 움직이고 반대 손·배낭은 시차를 두고 안정된다. 발 접지는 고정한다. 전투 판정/대미지 이벤트는 미연결이다.
- [수정 Blender 미리보기](../Assets/ChibiSurvivor/Player/Attack_OneHand_Thrust_Preview.mp4), [동일 각도 이전/수정 비교](../Assets/ChibiSurvivor/Player/Thrust_Comparison.mp4). 임시 칼은 별도 미리보기 파일에만 포함한다.
- 재생성/자세/영상/내보내기 검사/칼 간섭 검사 도구는 베기와 같으며 최신안에는 `-- --fluid` 옵션을 쓴다. 초기 찌르기는 `-- --stab`. 13개 메시·23개 뼈·기존 네 액션 보존, 5개 액션 내보내기, 접지·복귀 및 28프레임 칼 표면 간섭 검사를 통과했다. 팔 도달 거리 강제 제한 없이 구동하며 주요 동작 중 손이 정지하는 프레임 간격은 없다. Unity 검증 전이다.
- 재사용 원칙: 모든 중간 포즈에 속도 0으로 끝나는 easing을 반복 적용하지 않는다. 지나가는 준비·회수 포즈에는 연속 접선을 사용하고, 몸통과 손의 정점 시점을 겹치되 일치시키지 않는다. 정지가 필요한 타격/연출 구간만 의도적으로 둔다.

### 한손 베기 제작안 — 2026-09-08

- 요청: **“다음모션 만들어볼까”**. 앞서 논의한 순서에 따라 한손 근접 공격을 다음 작업으로 해석했다. 이번 동작의 세부 타이밍과 형태는 사용자 확인 전 제작안이다.
- 최신 작업 파일: [Combat/CompactSurvivor_Combat.blend](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated/Combat/CompactSurvivor_Combat.blend). 승인된 RoundHands 버전에 `Attack_OneHand`만 추가했으며 13개 메시·가중치와 기존 Idle/Walk/Run 키는 그대로다.
- 오른손 사선 베기: 30fps, 1~40프레임(1.3초). 준비 11f → 베기 중심 15f → 마무리 18f → 기존 Idle 첫 자세로 복귀 40f. 제자리 단발 동작이며 전투 판정/대미지 이벤트는 연결하지 않았다.
- 양발 접지는 고정하고 몸통과 어깨를 회전한다. 복귀하는 손은 허리 파우치 앞쪽을 돌아가도록 조정했다. 같은 골격과 `HandSocket.R`를 사용하며 둥근 손에 손가락/엄지를 다시 만들지 않는다.
- [실제 Blender 영상](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated/Combat/Attack_OneHand_Preview.mp4), [단계별 자세](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated/Combat/Attack_OneHand_Poses.png). 임시 칼은 궤적 확인용으로 별도 `Attack_OneHand_Preview.blend`에만 포함하며 캐릭터 FBX/GLB에서는 제외한다.
- 재생성: `tools/animate_compact_onehand.py`; 자세/영상: `tools/preview_compact_onehand.py -- --poses` 또는 `-- --movies`; FBX/GLB 검사: `tools/check_compact_animation_export.py -- --onehand`. 기존 루프/외형 보존, 새 모션 복귀·접지, 내보내기 후 4개 클립을 확인했다. 임시 칼의 40프레임 메시 표면 교차 검사도 통과했다. Unity 연결/검증은 다음 단계다.

### 기본 모션 산출물과 재사용 정보

- [CompactSurvivor_Animated.blend](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated/CompactSurvivor_Animated.blend): 원본 외형에 스킨을 연결한 작업 파일. `CompactSurvivor_Rig`의 액션에서 Idle/Walk/Run 선택. 기본 재생 범위는 Idle이다.
- [CompactSurvivor_Animated.fbx](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated/CompactSurvivor_Animated.fbx), [GLB](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated/CompactSurvivor_Animated.glb): 세 클립을 포함하는 내보내기 파일. 기본 포즈에서 기존 메시 형상을 유지한다.
- [모션 미리보기 영상](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated/LocomotionPreview.mp4): 대기→걷기→뛰기 순서, 약 9.33초. 실제 Blender 애니메이션을 EEVEE로 렌더했으며 캐릭터가 걷는 모습 확인을 위해 영상에만 가상 전진 이동을 적용했다. [여러 동작 단계 비교](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated/MotionPoses.png).
- [AnimationCheck.json](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated/AnimationCheck.json): 정점 가중치·기본 외형·루프 경계·실제 변형된 발바닥 접지·보행 속도 일치 검사. [ExportCheck.json](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated/ExportCheck.json): FBX 재임포트 후 13개 스킨 메시와 3개 모션의 실제 변형/루프, GLB 스킨/클립 포함 검사.

| 클립 | 30fps 재생 범위 | 한 주기 | 제자리 모션 기준 전진 속도 |
|---|---|---|---|
| Idle | 1~91 | 3초 | 0 |
| Walk | 1~31 | 1초 | 0.60m/s |
| Run | 1~21 | 약 0.667초 | 약 1.417m/s |

- 마지막 프레임은 첫 프레임과 같은 루프 경계 포즈다. 게임 이동 속도를 바꾸면 재생 속도도 기준 속도에 비례해 조정해야 발 미끄러짐을 줄일 수 있다. 위 속도는 모션의 기준값이며 게임 디자인 이동 속도를 변경한 것이 아니다.
- 뼈의 FK 키는 편집 가능하다. 다리는 두 관절 IK 계산 결과를 키프레임으로 구워 접지시킨 구조이며 실시간 IK 컨트롤러가 구현된 것은 아니다.
- 손목 각도는 달리기 중 손바닥이 위로 들리지 않도록 조정했다. 양손 `HandSocket.L`/`HandSocket.R`는 이후 무기 부착용으로 확보했다.
- 생성: [rig_compact_survivor.py](../tools/rig_compact_survivor.py). 영상: [preview_compact_animation.py](../tools/preview_compact_animation.py). 내보내기 검사: [check_compact_animation_export.py](../tools/check_compact_animation_export.py).
- 영상 조립 시 이미 렌더된 색상에 AgX를 이중 적용하지 않는다. [finalize_compact_preview.py](../tools/finalize_compact_preview.py)가 Standard 변환으로 조립하고 각 영상의 프레임 수와 대표 장면을 확인한다.

## 이전 연구: 귀 유무 비교 — 2026-09-08

- 사용자 질문: **“귀가 있어서 별로인가 좀 너무 어색한데”**.
- 해석: 귀의 존재/형태가 어색함의 원인인지 확인하자는 피드백이며, 귀를 영구 제거하라는 확정 결정은 아니다.
- 비교 작업: 기존 사각형 얼굴의 귀 두 개만 일시적으로 숨긴다. 얼굴형·표정·재질·카메라·조명을 동일하게 유지해 귀의 영향을 비교한다. 귀 메시는 보존하고 다시 표시할 수 있게 한다.
- 검토안: [EarStudy.blend](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/EarVisibilityStudy/EarStudy.blend). 기존 중립/어두운 조명 씬과 귀를 숨긴 중립/어두운 조명 씬을 함께 보존한다. 사용자 판단 전에는 귀 제거를 다음 캐릭터의 기본 규칙으로 삼지 않는다.

## 이전 연구: 얼굴형 수정 — 2026-09-08

- 사용자 피드백: **“얼굴형이 별론데 실제 얼굴같지가 않아 시안 보면 약간 사각형느낌인데”**.
- 결정: 현재의 아래로 급격히 좁아지는 얼굴형을 수정한다. 아래 볼과 턱 양옆을 살리고, 턱 끝은 짧고 평평한 부드러운 사각형으로 잡는다. 날카로운 역삼각형이나 상자처럼 각진 얼굴은 피한다.
- 범위: 승인된 전체 키·머리 높이/최대 폭·몸/다리 비율은 유지한다. 얼굴 안의 볼/턱 정점만 수정하고 단순한 눈·귀·코·입 표현은 유지한다. 기존 비율 고정 원칙에 대한 사용자의 명시적 국소 수정 요청이다.
- 현재 검토안: [FaceStudySquare/FaceStudy.blend](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/FaceStudySquare/FaceStudy.blend), `build_face_study.py -- --square-jaw`. 이전 얼굴안은 보존한다. 이 얼굴형은 사용자 승인 전이다.
- 기술 확인: 기본 메시 중 `Head`만 변경되었는지 검사하고, 얼굴 표면 디테일은 새 표면에 다시 투영한다. 정면/사선 및 중립/어두운 조명에서 확인한다.

## 시안 해석과 디테일 수준 — 2026-09-08

- 질문/요청: 시안을 그대로 재현해야 할까, 분위기를 유지하며 디테일을 줄일까?
- 사용자 결정: **“얼굴도 시안이랑 똑같지 않아도 된다 내가 원하는건 시안의 느낌이지 동일하게 아니야”**. 귀 안쪽처럼 인상에 불필요한 디테일은 생략한다.
- 적용: 시안은 분위기와 큰 비율의 기준이다. 얼굴의 선·색 영역·장식 위치를 픽셀 단위로 복제하는 것을 목표로 삼지 않는다. 이미 승인한 전체 비율은 유지한다.
- 후속 사용자 결정: **“얼굴은 좀더 단순하게 해도 좋을것 같아 다만 배경에 맞게 주인공 얼굴로”**. 얼굴 세부 묘사를 더 덜어내되 아포칼립스 배경에 맞는 주인공 인상을 유지한다.
- 이번 구현안: 단색의 짙은 눈동자, 얇은 눈꺼풀/눈썹, 작은 코, 짧은 입선, 작은 단일 볼 밴드. 귀 내부·눈 테두리·눈꺼풀 잔주름·동공의 추가 색 층을 생략한다. 지나치게 해맑거나 멍한 표정보다 차분한 생존자 인상을 목표로 하며 최종 표정은 사용자 확인 전이다.
- 작업 해석: 귀여운 비율, 담담하고 조금 지친 생존자 인상, 차분한 흙빛 색감, 적당히 면이 드러나는 무광 표현을 우선한다. 눈·코·입과 장비의 작은 형태는 이 인상에 맞게 자유롭게 단순화한다.
- 상세 묘사를 추가하기 전 캐릭터 인상과 게임 화면에서의 식별에 필요한지 판단한다. 반복적인 장식·귀 내부 무늬·눈 주변 잔선은 기본적으로 생략한다.
- 현재 단순화안: [FaceStudySimple/FaceStudy.blend](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/FaceStudySimple/FaceStudy.blend). 이전 얼굴 1차안도 별도 보존한다. 재생성은 `build_face_study.py -- --simple`.

## 조명·셰이더까지 포함한 판단 — 2026-09-08

- 사용자 요청: **“조명과 쉐이더 로 전체적인 모델 의 느낌이 변경될수 있으니 거기까지 생각하고”**.
- 적용: 조형의 최종 인상을 단일 스튜디오 렌더로 확정하지 않는다. 중립 조명에서 형태/색을 확인하고, 어두운 환경 조명에서 눈매와 실루엣이 읽히는지도 본다. 재질은 같은 상태로 유지하여 조명의 영향을 구분한다.
- 그림자나 조명 반사를 따라 과한 홈/장식을 모델에 새기지 않는다. 무광의 바랜 색과 큰 면을 기본으로 두고 최종 명암/환경색은 게임 조명·셰이더까지 포함해 조정한다.
- 현재 확인용 파일: [FaceLightingStudy.blend](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/FaceStudySimple/FaceLightingStudy.blend). 중립 조명과 어두운 따뜻한 키라이트/차가운 주변광의 두 씬. 어두운 조명은 작업용 예시이며 게임 조명 확정안이 아니다.
- **현재는 Blender 재질·조명 검토 단계다. Unity 셰이더 구현/검증 완료가 아니다.** 이후 실제 게임 카메라 거리와 렌더러에서 재질·노출·그림자·색 관리를 함께 확인한 뒤 최종 외형을 판단한다.

## 다음 캐릭터를 만들 때도 적용할 순서

1. 먼저 시안을 보여주고 외형 방향을 확인받는다. 이미지 생성 결과와 실제 Blender 렌더를 명확히 구분한다.
   시안의 느낌을 캐릭터에 옮기되 똑같은 복제는 목표로 삼지 않는다. 세부 형태의 차이만으로 실패라고 판단하지 않는다.
2. 원본 정면의 머리 위·턱·허리·무릎·발바닥 및 머리/어깨/골반 폭을 측정한다. '몇 등신'이라는 이름에 억지로 맞추지 않는다. 측면 깊이는 별도로 확인한다.
3. 얼굴/옷 장식 없는 비율 모델을 만든다. 같은 카메라 방향과 크기로 원본과 비교해 먼저 승인받는다.
4. 승인한 모델은 보존하고 별도 단계 파일에서 작업한다. 승인한 머리·몸·다리 크기와 위치를 유지하며 한 번에 한 부위만 디테일을 추가한다.
5. 각 부위의 실제 정면·사선 렌더를 보고 수정한다. 담당 부위의 사용자 확인 후 다음 부위로 넘어간다. 복잡한 디테일을 한꺼번에 얹어 비율 문제를 가리지 않는다.
6. 모자·헤어·상의·하의·신발·가방·손목 장비는 독립 수정/교체할 수 있게 분리한다. 리깅 시에도 공통 골격과 부착 위치를 유지한다. 현재 비율/얼굴 단계는 정적 모델이며 교체 시스템 구현 완료가 아니다.
7. 외형이 확인되면 리깅·애니메이션·Unity 연결로 진행한다. 사용자 요청에 따라 모델 제작 뒤 검증 단계에서 멈추고 사용자가 모델을 바꿀 수 있게 한다.

## 조형에서 반복하지 않을 실패

- 무조건 적은 폴리곤 수를 목표로 하지 않는다. 둥근 외곽선과 필요한 눈꺼풀/옷 접힘을 표현할 기하 구조를 확보한다.
- 전체에 매끈한 스무딩/서브디비전을 적용해 점토 인형처럼 만들지 않는다. 시안의 **큰 면, 부드러운 외곽, 무광의 바랜 색감**을 함께 살린다.
- 눈을 튀어나온 구슬/두꺼운 테두리로 만들지 않는다. 시안의 반쯤 감긴 눈, 낮은 눈썹, 작은 코와 입을 우선한다.
- 가슴 포켓·끈·봉제선 수를 늘리는 것으로 유사도를 대신하지 않는다. 실루엣→얼굴 인상→옷 큰 형태→장식 순으로 확인한다.
- 시안과 똑같다고 단정하지 않는다. 실제 렌더에서 남은 차이를 밝히고 사용자 판단을 받는다.

## 이번 승인 비율의 재현 정보

원본 1536×1024 정면에서 읽은 근사값. 모자 포함 머리 높이이며 해부학적 등신비가 아니다.

| 기준 | 원본 Y 픽셀 | 전체 높이 중 비율 |
|---|---:|---:|
| 모자 위 | 66 | 0% |
| 턱 | 333 | 위에서 30.76% |
| 허리 | 530 | 위에서 53.46% |
| 무릎 | 744 | 위에서 78.11% |
| 발바닥 | 934 | 100% |

- 전체 높이 868px → Blender 1.8 단위. 정면 중심 X=297px.
- 머리(모자 포함) 약 30.76%, 턱~허리 약 22.70%, 허리~발바닥 약 46.54%. 약 3.25등신으로 읽히는 시안이다.
- 깊이는 시안 측면에서 추정한 값이다. 원본은 정확히 보정된 정투영 도면이 아니므로 픽셀 측정만으로 모든 방향 일치를 보장하지 않는다.
- 비율 재생성: [build_reference_proportions.py](../tools/build_reference_proportions.py).
- 얼굴 단계 재생성: [build_face_study.py](../tools/build_face_study.py). 승인된 기본 메시의 정점/변환 해시를 비교해 비율 보존을 확인한다.
- 얼굴 작업 파일: [FaceStudy.blend](../ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/FaceStudy/FaceStudy.blend). 승인 베이스와 별도 보존.

## 변경 이력

- 2026-09-08: 단검 한손 찌르기를 유지하고 검 베기·도끼 내려찍기를 양손으로 교체. 공격 3개 고정, 공통 손잡이 기준과 양손 결합 경로 도입. 이전 한손 베기/내려찍기와 미리보기는 Assets 밖에 보존.
- 2026-09-08: 한손 공격을 검 베기·단검 찌르기·도끼 내려찍기 3종으로 정리. 최신 요청에 따라 제자리 공격을 제작 기준으로 삼고 이동 공격 실험은 보류. 기존 두 공격/이동/외형을 유지한 내려찍기 추가.
- 2026-09-08: 단계적으로 끊긴다는 피드백에 따라 찌르기를 연속 경로와 신체 부위 간 시차로 수정(1.1→0.9초). 초기 찌르기를 보존하고 Fluid에 분리.
- 2026-09-08: 한손 공격을 찌르기로 비교해 보자는 피드백 반영. 원래 베기와 이동 클립을 보존하고 1.1초 정면 찌르기를 Combat/Thrust에 추가.
- 2026-09-08: 둥근 손과 기존 이동 모션을 유지한 한손 사선 베기 제작안 추가. 복귀 중 파우치를 통과하던 칼 경로 수정. 실제 Blender 영상과 4개 클립 FBX/GLB를 Combat 폴더에 분리.
- 2026-09-08: CompactSurvivor에 23본 리그와 Idle/Walk/Run 3개 제작. 13개 메시 분리 유지, 제자리 모션 접지/루프 확인 및 FBX 재임포트·GLB 확인 완료. Unity 연결은 보류.
- 2026-09-08: 사용자 선택 이미지에 해당하는 ThreeHeadSurvivor로 기준을 전환. 이전 얼굴 수정 작업 중단, 원본 머리를 유지한 2.608등신 CompactSurvivor 제작 및 Blender 렌더 확인.
- 2026-09-08: 얼굴형 피드백에 따라 볼 아래·턱 폭을 넓힌 부드러운 사각형 수정안 진행. 전체 비율 유지, 얼굴 내부 형상 수정 허용 범위를 명시.
- 2026-09-08: 얼굴 1차 피드백 반영. 시안과 동일한 재현이 아닌 느낌 유지가 목표임을 명시하고, 귀 등 불필요한 세부 묘사를 축소하는 기준 추가.
- 2026-09-08: 사용자 비율 승인 및 다음 캐릭터에도 재사용할 작업 기준 기록 요청 반영. 이 기록은 작업 방식 기억을 위한 것으로, 이전 요청대로 전체 문서 정리와 Git 작업은 여전히 보류한다.
