# 맵 프리팹

## ScrapMarket_Greybox

**생성**: Unity 메뉴 `Tools > Dev Tools > Map > Build Scrap Market Greybox Prefab`

### 구성

| 그룹 | 내용 |
|------|------|
| Floors | `Asphalt_Floor.mat` — 26×18 타일 Quad (Zone 1과 동일 3D 바닥) |
| Walls | `CityBuilding.mat` — 편의점 외벽 + 골목 가이드벽, NavMeshObstacle |
| Props_Greybox | `props.mat` — 진열대·카운터·검문소·잔해 큐브 |
| Gameplay | Spawn(`default`, `scrap_market`), Exit→Safehouse, 쪽지, 루팅 상자×2, ItemSpawn×5 |

### 씬에 넣기

1. `InGameScene` 열기
2. `Tools > Dev Tools > Map > Place Scrap Market Greybox In Scene`  
   또는 `ScrapMarket_Greybox.prefab`을 `Map_NewMap` 자식으로 드래그
3. **NavMesh** 오브젝트 선택 → Bake (벽 Obstacle 반영)
4. 기존 `NavGround`/`ExitPoint`가 겹치면 비활성화

### 머티리얼 경로

- `Assets/IsometricMapEditor/MapData/Mat/Asphalt_Floor.mat`
- `Assets/IsometricMapEditor/MapData/Mat/CityBuilding.mat`
- `Assets/IsometricMapEditor/MapData/Mat/props.mat`
