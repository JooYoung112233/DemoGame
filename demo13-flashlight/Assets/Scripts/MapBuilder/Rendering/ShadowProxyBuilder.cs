using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricMapEditor
{
    /// <summary>
    /// 2D 스프라이트 구조물(벽/컨테이너)이 실제 스포트라이트(플래시라이트) 빛을 막도록,
    /// 렌더되지 않는(ShadowsOnly) 얇은 3D 박스를 그림자 프록시로 배치한다.
    /// 박스는 프롭 비주얼의 자식으로 붙어 프롭의 yRotation/scale을 그대로 상속한다.
    /// 대각선/ㅅ/V자 벽은 각 박스의 yaw로 비스듬히 눕혀 처리.
    /// rendering.md 2026-05-29 결정 참조.
    /// </summary>
    public static class ShadowProxyBuilder
    {
        public enum ShadowShape { Straight, Diagonal, Peak, Valley }

        /// <summary>
        /// 프롭 정의의 그림자 박스들을 parent 아래 자식으로 생성한다.
        /// </summary>
        public static void Build(PropDefinition def, Transform parent, bool editorPreview = false)
        {
            if (def == null || !def.castsShadow || def.shadowBoxes == null) return;
            for (int i = 0; i < def.shadowBoxes.Length; i++)
                CreateBox(def.shadowBoxes[i], parent, i, editorPreview);
        }

        public static GameObject CreateBox(ShadowBox box, Transform parent, int index = 0, bool editorPreview = false)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"ShadowProxy_{index}";
            if (editorPreview)
                cube.hideFlags = HideFlags.DontSave;
            if (parent != null)
                cube.transform.SetParent(parent, false);

            cube.transform.localPosition = box.offset;
            cube.transform.localRotation = Quaternion.Euler(0, box.yaw, 0);
            cube.transform.localScale = box.size.sqrMagnitude > 0.0001f
                ? box.size
                : new Vector3(1f, 2.4f, 0.1f);

            // 콜라이더 제거: 빛 차폐만 담당. 이동 차단은 PropDefinition.blocksWalkability가 별도 처리.
            var col = cube.GetComponent<Collider>();
            if (col != null)
            {
                if (editorPreview) Object.DestroyImmediate(col);
                else Object.Destroy(col);
            }

            var mr = cube.GetComponent<MeshRenderer>();
            if (editorPreview)
            {
                // 에디터에선 차폐 볼륨이 보이도록 반투명 주황 슬랩으로 표시(배치 확인용).
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                    mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(1f, 0.5f, 0.1f, 1f);
                mat.hideFlags = HideFlags.DontSave;
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.On;
            }
            else
            {
                // 런타임: 렌더 안 하고 그림자만 던짐.
                mr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }

            return cube;
        }

        /// <summary>
        /// 형태별 기본 박스 프리셋. tileSize=1 기준. 디자이너가 값은 추후 미세조정.
        /// </summary>
        public static ShadowBox[] GetPreset(ShadowShape shape)
        {
            const float h = 2.4f;   // 높이
            const float t = 0.1f;   // 두께
            const float hy = 1.2f;  // 높이 절반(바닥에서 박스 중심까지)

            switch (shape)
            {
                case ShadowShape.Diagonal: // 대각선 (셀 모서리~모서리)
                    return new[]
                    {
                        new ShadowBox { size = new Vector3(1.414f, h, t), offset = new Vector3(0, hy, 0), yaw = 45f },
                    };

                case ShadowShape.Peak: // ㅅ자 (꼭짓점이 +Z쪽)
                    return new[]
                    {
                        new ShadowBox { size = new Vector3(0.78f, h, t), offset = new Vector3(-0.28f, hy, 0.28f), yaw = -45f },
                        new ShadowBox { size = new Vector3(0.78f, h, t), offset = new Vector3( 0.28f, hy, 0.28f), yaw =  45f },
                    };

                case ShadowShape.Valley: // V자 (꼭짓점이 -Z쪽)
                    return new[]
                    {
                        new ShadowBox { size = new Vector3(0.78f, h, t), offset = new Vector3(-0.28f, hy, -0.28f), yaw =  45f },
                        new ShadowBox { size = new Vector3(0.78f, h, t), offset = new Vector3( 0.28f, hy, -0.28f), yaw = -45f },
                    };

                default: // Straight (직선)
                    return new[]
                    {
                        new ShadowBox { size = new Vector3(1f, h, t), offset = new Vector3(0, hy, 0), yaw = 0f },
                    };
            }
        }
    }
}
