using UnityEngine;

namespace TopDownMapEditor
{
    /// <summary>
    /// 스프라이트 기반 프롭을 런타임에 빌보드 쿼드로 생성한다. 프리팹 불필요.
    ///
    /// 구조:
    ///   root            — 빌보드 회전(카메라 정면). yRotation/scale은 호출측에서 root에 적용.
    ///                      위치는 항상 셀 바닥점(접지 보정 미포함) — 그린 접지 마커/정렬 기준.
    ///    ├─ Content      — 접지 오프셋(XYZ) 컨테이너. ApplyGroundOffset이 이 자식만 월드 오프셋만큼
    ///                      이동(InverseTransformVector로 root 회전·스케일 상쇄 → 순수 월드 XYZ).
    ///                      이미지와 콜라이더가 함께 따라온다. root는 제자리.
    ///    │   ├─ Visual   — SpriteRenderer. 풋프린트 폭에 맞춘 fit-scale + 하단 접지 offset(Y).
    ///    │   │            (스프라이트 rect 하단을 Content 원점=바닥에 올림. 회전은 0,0,0.)
    ///    │   └─ NavBlocker — (blocksWalkability일 때) 풋프린트 크기의 축 정렬 BoxCollider.
    ///    └─ GroundMarker — (호출측에서 부착) 셀 바닥 표시. root 직속이라 오프셋 영향 없음.
    ///
    /// 빛 차폐(ShadowProxy)는 사용하지 않는다 — 쿼드 프롭은 이동 차단(nav)만 담당한다.
    /// rendering.md / map-tool.md 2026-05-30 결정 참조.
    /// </summary>
    public static class PropQuadBuilder
    {
        /// <summary>
        /// 프롭 스프라이트의 고정 루트 회전 = 바닥에 눕히는 top-down 회전(TopDownGrid.SpriteFlatRotation).
        /// 스프라이트를 XZ 바닥 평면에 평평하게 깔아 윗면을 보여준다(타일과 동일 규약). 카메라가 위에서 내려다봄.
        /// (이전: Euler(35.264,45,0) 아이소 빌보드 → 2026-06-02 탑다운 결정으로 바닥 눕힘.)
        /// </summary>
        public static Quaternion BillboardRotation => TopDownGrid.SpriteFlatRotation;

        /// <summary>스프라이트 프롭이면 true (prefab 대신 쿼드로 그린다).</summary>
        public static bool UsesQuad(PropDefinition def) => def != null && def.sprite != null;

        /// <summary>
        /// 프롭 root에 적용할 최종 회전.
        ///   - 일반: yaw(수평 회전) × 빌보드(카메라 정면). 카메라를 따라 도는 장식물.
        ///   - 벽 부착(wallMounted): yaw만. 빌보드를 끄고 벽면에 납작하게 고정한다
        ///     (yaw로 어느 벽을 향할지 결정 — 카메라를 따라가지 않음).
        /// </summary>
        public static Quaternion RootRotation(float yawDegrees, bool wallMounted)
        {
            var yaw = Quaternion.Euler(0f, yawDegrees, 0f);
            return wallMounted ? yaw : yaw * BillboardRotation;
        }

        /// <summary>
        /// 스프라이트 프롭 비주얼 생성. 반환된 root의 rotation은 빌보드 회전으로 설정됨.
        /// (호출측은 기존 프리팹과 동일하게 yRotation을 곱하고 scale을 적용할 수 있다.)
        /// </summary>
        /// <param name="addNavCollider">true면 blocksWalkability일 때 nav 박스 콜라이더를 부착.</param>
        public static GameObject Build(PropDefinition def, GridSettings settings, bool addNavCollider)
        {
            float tileSize = settings != null ? settings.tileSize : 1f;
            // 풋프린트는 소수 허용(0.5=반 타일). 0/음수만 막고 작은 값은 그대로 둬 작은 프롭을 만든다.
            float fx = Mathf.Max(0.05f, def.footprint.x);
            float fy = Mathf.Max(0.05f, def.footprint.y);

            var root = new GameObject($"Prop_{def.propId}");
            root.transform.rotation = BillboardRotation;

            // ── Content (접지 오프셋 컨테이너) ──
            // 이미지+콜라이더를 함께 담아, 접지 보정 시 root는 제자리 두고 이 컨테이너만 월드 오프셋만큼 옮긴다.
            var content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);

            // ── Visual (SpriteRenderer) ──
            var visual = new GameObject("Visual");
            visual.transform.SetParent(content.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = def.sprite;
            if (def.material != null)
                sr.sharedMaterial = def.material;
            if (def.sprite != null)
            {
                // 스프라이트 폭을 풋프린트 폭(셀×타일크기)에 맞춰 종횡비 유지 스케일.
                var size = def.sprite.bounds.size;
                float s = size.x > 0.0001f ? (fx * tileSize) / size.x : 1f;
                visual.transform.localScale = new Vector3(s, s, 1f);

                // top-down(바닥 눕힘): 스프라이트를 셀 원점에 중심 정렬한다(피벗 기준 그대로).
                // 바닥에 깔린 평면이라 '서 있는' 접지(하단을 바닥에 올리는) 개념이 없고,
                // 풋프린트 = 셀 중심에 놓인 윗면이다. (iso 시절엔 rect 하단을 셀 바닥점에 올렸음.)
                // ※ 미세 보정(groundOffset/Z)은 ApplyGroundOffset이 Content를 월드 오프셋만큼 옮긴다.
                visual.transform.localPosition = Vector3.zero;
            }

            // ── NavBlocker (선택) ──
            if (addNavCollider && def.blocksWalkability)
            {
                var nav = new GameObject("NavBlocker");
                nav.transform.SetParent(content.transform, false);
                // 빌보드 틸트를 제거해 풋프린트가 바닥 축에 정렬되도록 한다.
                // (이후 root에 yRotation이 곱해지면 nav는 yaw만 따라간다.)
                nav.transform.rotation = Quaternion.identity;

                var box = nav.AddComponent<BoxCollider>();
                box.center = new Vector3((fx - 1) * tileSize * 0.5f, 0.5f, (fy - 1) * tileSize * 0.5f);
                box.size = new Vector3(fx * tileSize, 1f, fy * tileSize);
            }

            return root;
        }

        /// <summary>
        /// 접지 보정(XYZ)을 root가 아니라 내부 Content(이미지+콜라이더)에만 적용한다.
        /// worldOffset을 root 기준 로컬로 환산(InverseTransformVector — 회전·스케일 상쇄)해
        /// Content.localPosition에 절대값으로 넣으므로, 순수 월드 XYZ로 이동하며 누적되지 않는다.
        /// root(=셀 바닥점, 그린 마커/정렬 기준)는 제자리. **root의 회전·스케일이 정해진 뒤** 호출할 것.
        /// </summary>
        public static void ApplyGroundOffset(GameObject root, Vector3 worldOffset)
        {
            if (root == null) return;
            var content = root.transform.Find("Content");
            if (content == null) return;
            content.localPosition = worldOffset == Vector3.zero
                ? Vector3.zero
                : root.transform.InverseTransformVector(worldOffset);
        }

        /// <summary>
        /// 빌보드 쿼드 프롭의 스프라이트를 좌우 반전(미러)한다. Build로 만든 root에 적용.
        /// 커스텀 Prop 셰이더는 SpriteRenderer.flipX(빌트인 sprite의 _Flip 벡터 의존)를 반영하지
        /// 않으므로, Visual 트랜스폼의 X 스케일 부호로 미러링한다(지오메트리 레벨 → 셰이더 무관).
        /// Abs를 써서 멱등(여러 번 호출해도 안정). 기존 sr.flipX는 이중 반전 방지로 꺼 둔다.
        /// </summary>
        public static void ApplyFlip(GameObject root, bool flipX)
        {
            if (root == null) return;
            var sr = root.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return;
            sr.flipX = false;
            var t = sr.transform;
            var s = t.localScale;
            float ax = Mathf.Abs(s.x);
            t.localScale = new Vector3(flipX ? -ax : ax, s.y, s.z);
        }
    }
}
