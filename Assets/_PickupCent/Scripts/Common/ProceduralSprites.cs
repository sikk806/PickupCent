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
    }
}
