# 시스템: UI 디자인 가이드

> 2026-05-19 확정. 전체 씬 UI 전면 개편.

## 1. 비주얼 톤: 판타지 길드

- 따뜻한 어두운 배경 (차가운 네이비 → 따뜻한 갈색-검정)
- 양피지/가죽 질감 (그라데이션으로 표현, 이미지 없이)
- 금색/청동 액센트 테두리
- 장식적 구분선
- 전체적으로 RPG 길드 운영 분위기

## 2. 색상 팔레트 (UI_THEME)

| 용도 | 색상코드 | 설명 |
|------|----------|------|
| bg | 0x1a1510 | 메인 배경 (따뜻한 갈-검) |
| panelFill | 0x2a2218 | 패널 배경 (다크 레더) |
| panelStroke | 0x5a4a2a | 패널 테두리 (청동) |
| cardFill | 0x231e14 | 카드 배경 |
| cardHover | 0x3a3020 | 카드 호버 |
| cardSelected | 0x4a3a20 | 카드 선택 |
| textPrimary | #e8d8c0 | 주 텍스트 (양피지 백) |
| textSecondary | #b8a888 | 보조 텍스트 |
| textMuted | #887860 | 흐린 텍스트 |
| gold | #ffcc44 | 골드 강조 |
| accent | #cc8833 | 청동 액센트 |
| danger | #cc4422 | 위험/빨강 |
| success | #44aa44 | 성공/초록 |
| disabled | 0x333028 / #665e48 | 비활성 |
| headerBg | 0x1e1810 | 헤더 배경 |
| buttonPrimary | 0x8a6a2a | 버튼 기본 (골드) |
| buttonHover | 0xaa8a3a | 버튼 호버 |
| buttonText | #1a1510 | 버튼 텍스트 |

## 3. 폰트 규격

| 용도 | 크기 | 비고 |
|------|------|------|
| title | 20px | 씬 제목 |
| header | 16px | 섹션 헤더 |
| body | 12px | 본문 |
| caption | 10px | 작은 캡션 |
| tiny | 9px | 최소 텍스트 |

전체 fontFamily: `'monospace'` 유지

## 4. 공용 컴포넌트

| 컴포넌트 | 파일 | 역할 |
|----------|------|------|
| UI_THEME | ui/Theme.js | 색상/폰트 상수 |
| UISceneBase | ui/SceneBase.js | 공용 헤더/뒤로가기/골드 표시 |
| UITabs | ui/Tabs.js | 탭 UI |
| UIModal | ui/Modal.js | 확인 다이얼로그 |
| UIScrollList | ui/ScrollList.js | 스크롤 가능 리스트 |
| UIButton | ui/Button.js | 개선 (setEnabled 추가) |
| UIPanel | ui/Panel.js | 장식적 테두리 옵션 |

## 5. 결정 사항

| 날짜 | 질문 | 선택 | 근거 |
|------|------|------|------|
| 2026-05-19 | 개편 범위? (인프라 먼저/타운부터/한방에) | 한방에 전부 | 사용자 선택 |
| 2026-05-19 | 비주얼 톤? (현재유지/모던다크/판타지길드) | 판타지 길드 느낌 | 양피지/금속 테두리 질감, 게임 세계관 일치 |
