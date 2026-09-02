using UnityEngine;
using UnityEngine.UIElements;

namespace Janggi.UI
{
    public class HexagonElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<HexagonElement, UxmlTraits> { }

        private static readonly CustomStyleProperty<Color> s_BorderColor = new CustomStyleProperty<Color>("--hex-border-color");
        private static readonly CustomStyleProperty<float> s_BorderWidth = new CustomStyleProperty<float>("--hex-border-width");

        private Color m_BorderColor = Color.clear;
        private float m_BorderWidth = 0f;

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

            // 기존 CSS에서 background-color 대신 -unity-background-image-tint-color를 색상값으로 활용합니다.
            Color fillColor = resolvedStyle.unityBackgroundImageTintColor;
            Color strokeColor = m_BorderColor;
            float strokeWidth = m_BorderWidth;

            // 정육각형(Regular Flat-top Hexagon) 비례 계산
            // 너비(W) = 2 * R, 높이(H) = sqrt(3) * R
            float centerX = rect.width * 0.5f;
            float centerY = rect.height * 0.5f;

            // 컨테이너 안에 딱 맞는 최대 반지름(R) 계산
            float R = Mathf.Min(rect.width / 2f, rect.height / Mathf.Sqrt(3f));

            float halfR = R * 0.5f;
            float heightHalf = R * (Mathf.Sqrt(3f) / 2f);

            painter.BeginPath();
            painter.MoveTo(new Vector2(centerX - halfR, centerY - heightHalf)); // Top-Left
            painter.LineTo(new Vector2(centerX + halfR, centerY - heightHalf)); // Top-Right
            painter.LineTo(new Vector2(centerX + R, centerY));                  // Right
            painter.LineTo(new Vector2(centerX + halfR, centerY + heightHalf)); // Bottom-Right
            painter.LineTo(new Vector2(centerX - halfR, centerY + heightHalf)); // Bottom-Left
            painter.LineTo(new Vector2(centerX - R, centerY));                  // Left
            painter.ClosePath();

            // 내부 채우기 (Fill)
            if (fillColor.a > 0)
            {
                painter.fillColor = fillColor;
                painter.Fill();
            }

            // 테두리 선 그리기 (Stroke)
            if (strokeWidth > 0 && strokeColor.a > 0)
            {
                painter.strokeColor = strokeColor;
                painter.lineWidth = strokeWidth;
                painter.lineJoin = LineJoin.Bevel; // 육각형 모서리가 깔끔하게 꺾이도록 Bevel 사용
                painter.Stroke();
            }
        }
    }
}
