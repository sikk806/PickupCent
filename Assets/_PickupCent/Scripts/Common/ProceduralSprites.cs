using UnityEngine;

namespace PickupCent.Common
{
    /// <summary>아트 에셋 없이 기본 도형 스프라이트를 코드로 생성하기 위한 유틸리티.</summary>
    public static class ProceduralSprites
    {
        public static Sprite CreateCircle(int size, Color color, float worldDiameter = 1.2f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(size - 1, size - 1) * 0.5f;
            float radius = size * 0.5f - 1f;
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(Mathf.SmoothStep(1f, 0f, (d - (radius - 1f)) / 2f));
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            float pixelsPerUnit = size / worldDiameter;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        public static Sprite CreateSquare(int size, Color color, float worldSize = 1f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            float pixelsPerUnit = size / worldSize;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        // --- UI(uGUI)용 둥근 사각형/알약/그라디언트 버튼. 텍스처를 항상 목표 픽셀 크기 그대로 굽기 때문에
        // (9-slice 없이) Image.Type.Simple로 바로 써도 늘어나거나 뭉개지지 않는다. ---

        /// <summary>단색 둥근 사각형(카드/블록/보조버튼 배경 등에 사용).</summary>
        public static Sprite CreateRoundedRect(int width, int height, float cornerRadius, Color color)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float alpha = RoundedRectAlpha(x + 0.5f, y + 0.5f, width, height, cornerRadius);
                    pixels[y * width + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>완전히 둥근 캡슐(알약) 모양 — 모서리 반지름을 높이의 절반으로 고정한 CreateRoundedRect.</summary>
        public static Sprite CreatePill(int width, int height, Color color)
        {
            return CreateRoundedRect(width, height, height * 0.5f, color);
        }

        /// <summary>
        /// 사이드패널 블록/카드처럼 세로 크기가 내용물에 따라 늘어나는 곳에 쓰는 9-slice 둥근 사각형.
        /// CreateRoundedRect과 달리 Image.Type.Sliced로 표시해야 어떤 최종 크기에서도 모서리가
        /// 뭉개지지 않는다(정사각형 텍스처를 굽고 border를 모서리 반지름 기준으로 잡아준다).
        /// </summary>
        public static Sprite CreateRoundedRectSliced(int size, float cornerRadius, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = RoundedRectAlpha(x + 0.5f, y + 0.5f, size, size, cornerRadius);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            float border = cornerRadius + 2f;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        /// <summary>
        /// 기본 버튼(.btn) 룩용 텍스처: 위(topColor)→아래(bottomColor) 세로 그라디언트 + 둥근 모서리 +
        /// 맨 아래에 두꺼운 강조 보더(bottomBorderColor, bottomBorderThickness px). 눌린 상태를 표현하려면
        /// bottomBorderThickness를 더 작은 값으로 줘서 같은 함수로 "pressed" 텍스처를 따로 구우면 된다.
        /// </summary>
        public static Sprite CreateGradientButton(int width, int height, float cornerRadius,
            Color topColor, Color bottomColor, float bottomBorderThickness, Color bottomBorderColor)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                // 텍스처 y=0이 하단이 되도록 채우므로(Texture2D 관례), t=1일 때가 버튼 위쪽(밝은 색).
                float t = height > 1 ? y / (float)(height - 1) : 1f;
                Color baseCol = Color.Lerp(bottomColor, topColor, t);
                Color rowColor = y < bottomBorderThickness ? bottomBorderColor : baseCol;

                for (int x = 0; x < width; x++)
                {
                    float alpha = RoundedRectAlpha(x + 0.5f, y + 0.5f, width, height, cornerRadius);
                    pixels[y * width + x] = new Color(rowColor.r, rowColor.g, rowColor.b, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// CreateGradientButton의 9-slice 버전 — 버튼 너비가 텍스트 길이에 따라 달라지는 곳
        /// (상점 토글/구매 버튼, 도구 탭 버튼 등)에 쓴다. 정사각형으로 구운 뒤 좌우로 늘려 쓴다.
        /// </summary>
        public static Sprite CreateGradientButtonSliced(int size, float cornerRadius,
            Color topColor, Color bottomColor, float bottomBorderThickness, Color bottomBorderColor)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float t = size > 1 ? y / (float)(size - 1) : 1f;
                Color baseCol = Color.Lerp(bottomColor, topColor, t);
                Color rowColor = y < bottomBorderThickness ? bottomBorderColor : baseCol;

                for (int x = 0; x < size; x++)
                {
                    float alpha = RoundedRectAlpha(x + 0.5f, y + 0.5f, size, size, cornerRadius);
                    pixels[y * size + x] = new Color(rowColor.r, rowColor.g, rowColor.b, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            float border = cornerRadius + 2f;
            float bottomBorder = Mathf.Max(border, bottomBorderThickness + 1f);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, bottomBorder, border, border));
        }

        /// <summary>둥근 사각형 SDF: 경계에서 ~1px 안티에일리어싱되는 0~1 알파를 반환한다.</summary>
        private static float RoundedRectAlpha(float px, float py, float width, float height, float radius)
        {
            radius = Mathf.Max(0f, Mathf.Min(radius, Mathf.Min(width, height) * 0.5f));

            float cx = Mathf.Clamp(px, radius, width - radius);
            float cy = Mathf.Clamp(py, radius, height - radius);
            float dx = px - cx;
            float dy = py - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;

            return Mathf.Clamp01(0.5f - dist);
        }
    }
}
