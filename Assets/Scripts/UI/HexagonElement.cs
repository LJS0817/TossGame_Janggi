using UnityEngine;
using UnityEngine.UIElements;

namespace Janggi.UI
{
    /// <summary>
    /// 장기 기물 및 섀도우를 위한 정통 8각형(Octagon) 2D 렌더러 엘리먼트.
    /// 기존 색상 체계를 100% 유지하면서 로고 스타일의 정통 8각 형태와 고급스러운 이중 림(Inner Rim) 라인을 렌더링합니다.
    /// </summary>
    public class HexagonElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<HexagonElement, UxmlTraits> { }

        private static readonly CustomStyleProperty<Color> s_BorderColor = new CustomStyleProperty<Color>("--hex-border-color");
        private static readonly CustomStyleProperty<float> s_BorderWidth = new CustomStyleProperty<float>("--hex-border-width");
        private static readonly CustomStyleProperty<Color> s_InnerBorderColor = new CustomStyleProperty<Color>("--hex-inner-border-color");
        private static readonly CustomStyleProperty<float> s_InnerBorderWidth = new CustomStyleProperty<float>("--hex-inner-border-width");

        private Color m_BorderColor = Color.clear;
        private float m_BorderWidth = 0f;
        private Color m_InnerBorderColor = Color.clear;
        private float m_InnerBorderWidth = 0f;

        // 정8각형 꼭짓점 비례 상수 (tan(22.5도) = sqrt(2) - 1 ≈ 0.41421356f)
        private const float OctagonCornerRatio = 0.41421356f;

        public HexagonElement()
        {
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            // Register the callback to draw the custom mesh
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            bool repainted = false;
            if (e.customStyle.TryGetValue(s_BorderColor, out var c))
            {
                m_BorderColor = c;
                repainted = true;
            }
            if (e.customStyle.TryGetValue(s_BorderWidth, out var w))
            {
                m_BorderWidth = w;
                repainted = true;
            }
            if (e.customStyle.TryGetValue(s_InnerBorderColor, out var ic))
            {
                m_InnerBorderColor = ic;
                repainted = true;
            }
            if (e.customStyle.TryGetValue(s_InnerBorderWidth, out var iw))
            {
                m_InnerBorderWidth = iw;
                repainted = true;
            }

            if (repainted)
            {
                MarkDirtyRepaint();
            }
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            if (painter == null) return;

            var rect = contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;

            // 기존 CSS 색상 활용 (background-color 대신 -unity-background-image-tint-color)
            Color fillColor = resolvedStyle.unityBackgroundImageTintColor;
            Color strokeColor = m_BorderColor;
            float strokeWidth = m_BorderWidth;

            float centerX = rect.width * 0.5f;
            float centerY = rect.height * 0.5f;

            // 테두리 두께가 바운딩 박스를 벗어나지 않도록 패딩 계산
            float padding = strokeWidth > 0 ? strokeWidth * 0.5f + 0.5f : 0f;
            float R = Mathf.Min(rect.width * 0.5f, rect.height * 0.5f) - padding;
            if (R <= 0) return;

            float d = OctagonCornerRatio * R;

            // 1. 외곽 8각형 패스 (시계방향 8개 꼭짓점)
            painter.BeginPath();
            painter.MoveTo(new Vector2(centerX - d, centerY - R)); // 상단 좌
            painter.LineTo(new Vector2(centerX + d, centerY - R)); // 상단 우
            painter.LineTo(new Vector2(centerX + R, centerY - d)); // 우측 상
            painter.LineTo(new Vector2(centerX + R, centerY + d)); // 우측 하
            painter.LineTo(new Vector2(centerX + d, centerY + R)); // 하단 우
            painter.LineTo(new Vector2(centerX - d, centerY + R)); // 하단 좌
            painter.LineTo(new Vector2(centerX - R, centerY + d)); // 좌측 하
            painter.LineTo(new Vector2(centerX - R, centerY - d)); // 좌측 상
            painter.ClosePath();

            // 2. 내부 채우기 (기존 흰색/틴트 색상 유지)
            if (fillColor.a > 0)
            {
                painter.fillColor = fillColor;
                painter.Fill();
            }

            // 3. 외곽 테두리 (Stroke)
            if (strokeWidth > 0 && strokeColor.a > 0)
            {
                painter.strokeColor = strokeColor;
                painter.lineWidth = strokeWidth;
                painter.lineJoin = LineJoin.Bevel;
                painter.Stroke();

                // 4. 로고 스타일의 은은한 이중 림(Inner Rim) 라인 추가
                float innerPadding = Mathf.Max(2.2f, strokeWidth * 1.15f);
                float Rin = R - innerPadding;
                if (Rin > 4f)
                {
                    float din = OctagonCornerRatio * Rin;

                    painter.BeginPath();
                    painter.MoveTo(new Vector2(centerX - din, centerY - Rin));
                    painter.LineTo(new Vector2(centerX + din, centerY - Rin));
                    painter.LineTo(new Vector2(centerX + Rin, centerY - din));
                    painter.LineTo(new Vector2(centerX + Rin, centerY + din));
                    painter.LineTo(new Vector2(centerX + din, centerY + Rin));
                    painter.LineTo(new Vector2(centerX - din, centerY + Rin));
                    painter.LineTo(new Vector2(centerX - Rin, centerY + din));
                    painter.LineTo(new Vector2(centerX - Rin, centerY - din));
                    painter.ClosePath();

                    Color innerRimColor = (m_InnerBorderColor.a > 0)
                        ? m_InnerBorderColor
                        : new Color(strokeColor.r, strokeColor.g, strokeColor.b, strokeColor.a * 0.45f);
                    float innerRimWidth = (m_InnerBorderWidth > 0) ? m_InnerBorderWidth : 1f;

                    painter.strokeColor = innerRimColor;
                    painter.lineWidth = innerRimWidth;
                    painter.lineJoin = LineJoin.Bevel;
                    painter.Stroke();
                }
            }
        }
    }
}
