using System.IO;
using UnityEditor;
using UnityEngine;

namespace GreenShop.EditorTools
{
    public static class UISpriteGenerator
    {
        private const string OutputDir = "Assets/Art/UI";

        [MenuItem("Tools/GreenShop/Сгенерировать UI спрайты")]
        public static void GenerateAllSprites()
        {
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }

            CreateCircleSprite($"{OutputDir}/circle.png", 256);
            CreateCircleOutlineSprite($"{OutputDir}/circle_outline.png", 256, 6);
            CreateFrameSprite($"{OutputDir}/frame_rect.png", 64, 64, 4);
            CreateCoinSprite($"{OutputDir}/icon_coin.png", 128);
            CreateLockSprite($"{OutputDir}/icon_lock.png", 128);
            CreateDropSprite($"{OutputDir}/icon_drop.png", 128);
            CreateFertilizerSprite($"{OutputDir}/icon_fertilizer.png", 128);
            CreateWateringCanSprite($"{OutputDir}/icon_watering_can.png", 128);
            CreateSeedSprite($"{OutputDir}/icon_seed.png", 128);
            CreateArrowRightSprite($"{OutputDir}/arrow_right.png", 64, 96);
            CreateArrowUpSprite($"{OutputDir}/arrow_up.png", 140, 36);
            CreateFlowerPotSprite($"{OutputDir}/icon_flower_pot.png", 128, false);
            CreateFlowerPotSprite($"{OutputDir}/icon_flower_gray.png", 128, true);
            CreateTooltipPointerSprite($"{OutputDir}/tooltip_pointer.png", 48, 24);
            CreateTooltipPanelSprite($"{OutputDir}/tooltip_panel.png", 32);
            CreateAvailableRingSprite($"{OutputDir}/node_ring_available.png", 256, 14);

            AssetDatabase.Refresh();

            // Настраиваем импортеры
            ConfigureImporter($"{OutputDir}/circle.png");
            ConfigureImporter($"{OutputDir}/circle_outline.png");
            ConfigureImporter($"{OutputDir}/frame_rect.png", new Vector4(8, 8, 8, 8));
            ConfigureImporter($"{OutputDir}/icon_coin.png");
            ConfigureImporter($"{OutputDir}/icon_lock.png");
            ConfigureImporter($"{OutputDir}/icon_drop.png");
            ConfigureImporter($"{OutputDir}/icon_fertilizer.png");
            ConfigureImporter($"{OutputDir}/icon_watering_can.png");
            ConfigureImporter($"{OutputDir}/icon_seed.png");
            ConfigureImporter($"{OutputDir}/arrow_right.png");
            ConfigureImporter($"{OutputDir}/arrow_up.png");
            ConfigureImporter($"{OutputDir}/icon_flower_pot.png");
            ConfigureImporter($"{OutputDir}/icon_flower_gray.png");
            ConfigureImporter($"{OutputDir}/tooltip_pointer.png");
            ConfigureImporter($"{OutputDir}/tooltip_panel.png", new Vector4(6, 6, 6, 6));
            ConfigureImporter($"{OutputDir}/node_ring_available.png");

            AssetDatabase.SaveAssets();
            Debug.Log("[UISpriteGenerator] Все UI-спрайты успешно сгенерированы в Assets/Art/UI!");
        }

        private static void ConfigureImporter(string path, Vector4? border = null)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                if (border.HasValue)
                {
                    importer.spriteBorder = border.Value;
                }
                importer.SaveAndReimport();
            }
        }

        private static void CreateCircleSprite(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float radius = center - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            SaveTex(tex, path);
        }

        private static void CreateCircleOutlineSprite(string path, int size, int thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float outerR = center - 2f;
            float innerR = outerR - thickness;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float aOuter = Mathf.Clamp01(outerR - dist + 0.5f);
                    float aInner = Mathf.Clamp01(dist - innerR + 0.5f);
                    float alpha = Mathf.Min(aOuter, aInner);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            SaveTex(tex, path);
        }

        private static void CreateFrameSprite(string path, int w, int h, int border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x < border || x >= w - border || y < border || y >= h - border);
                    tex.SetPixel(x, y, isBorder ? Color.white : Color.clear);
                }
            }
            SaveTex(tex, path);
        }

        private static void CreateCoinSprite(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float rOuter = center - 4f;
            float rInner = rOuter - 8f;

            Color goldEdge = new Color(0.85f, 0.65f, 0.12f, 1f);
            Color goldFill = new Color(1.0f, 0.84f, 0.22f, 1f);
            Color goldLight = new Color(1.0f, 0.92f, 0.45f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > rOuter + 0.5f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float alpha = Mathf.Clamp01(rOuter - dist + 0.5f);
                        Color c;
                        if (dist > rInner)
                        {
                            c = goldEdge;
                        }
                        else
                        {
                            float grad = (x + y) / (2f * size);
                            c = Color.Lerp(goldFill, goldLight, grad);
                        }
                        c.a *= alpha;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            SaveTex(tex, path);
        }

        private static void CreateLockSprite(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            // Заливаем прозрачным
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Тело замка (прямоугольник внизу: x 32..96, y 16..68)
            for (int y = 16; y <= 68; y++)
            {
                for (int x = 32; x <= 96; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }

            // Дужка замка (полукруг сверху: центр (64, 68), внешний R 28, внутренний R 18)
            Vector2 shackleCenter = new Vector2(64, 68);
            for (int y = 68; y <= 112; y++)
            {
                for (int x = 32; x <= 96; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), shackleCenter);
                    if (dist <= 28.5f && dist >= 17.5f)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
            }

            // Скважина в центре тела замка
            for (int y = 40; y <= 54; y++)
            {
                for (int x = 61; x <= 67; x++)
                {
                    tex.SetPixel(x, y, new Color(0.1f, 0.1f, 0.1f, 0.9f));
                }
            }

            SaveTex(tex, path);
        }

        private static void CreateDropSprite(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2((size - 1) * 0.5f, 44f);
            float circleR = 36f;
            Vector2 top = new Vector2(c.x, 116f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    bool inCircle = Vector2.Distance(p, c) <= circleR;
                    bool inTop = false;

                    if (y >= c.y && y <= top.y)
                    {
                        float t = (y - c.y) / (top.y - c.y);
                        float halfW = Mathf.Lerp(circleR * 0.95f, 0f, Mathf.Pow(t, 0.85f));
                        inTop = Mathf.Abs(x - c.x) <= halfW;
                    }

                    if (inCircle || inTop)
                    {
                        float grad = (float)y / size;
                        Color col = Color.Lerp(new Color(0.3f, 0.75f, 0.98f, 1f), new Color(0.6f, 0.9f, 1f, 1f), grad);
                        tex.SetPixel(x, y, col);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            SaveTex(tex, path);
        }

        private static void CreateFertilizerSprite(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Мешок / пакет с удобрением
            for (int y = 18; y <= 95; y++)
            {
                float t = (float)(y - 18) / (95 - 18);
                float halfW = Mathf.Lerp(44f, 32f, t);
                for (int x = (int)(64 - halfW); x <= (int)(64 + halfW); x++)
                {
                    tex.SetPixel(x, y, new Color(0.95f, 0.85f, 0.65f, 1f));
                }
            }
            // Горловина
            for (int y = 96; y <= 112; y++)
            {
                for (int x = 44; x <= 84; x++)
                {
                    tex.SetPixel(x, y, new Color(0.85f, 0.72f, 0.50f, 1f));
                }
            }
            // Символ ростка/листочка в центре
            Vector2 leafCenter = new Vector2(64, 55);
            for (int y = 40; y <= 72; y++)
            {
                for (int x = 50; x <= 78; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), leafCenter) <= 12f)
                    {
                        tex.SetPixel(x, y, new Color(0.35f, 0.75f, 0.35f, 1f));
                    }
                }
            }
            SaveTex(tex, path);
        }

        private static void CreateWateringCanSprite(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Тело лейки: x 30..85, y 25..85
            for (int y = 25; y <= 85; y++)
            {
                for (int x = 32; x <= 85; x++)
                {
                    tex.SetPixel(x, y, new Color(0.40f, 0.75f, 0.90f, 1f));
                }
            }
            // Ручка сзади (слева): x 15..32, y 35..85
            for (int y = 35; y <= 85; y++)
            {
                for (int x = 16; x <= 32; x++)
                {
                    if (x <= 22 || y <= 42 || y >= 78)
                        tex.SetPixel(x, y, new Color(0.30f, 0.60f, 0.78f, 1f));
                }
            }
            // Носик справа: от (85, 45) к (115, 95)
            for (int y = 45; y <= 95; y++)
            {
                float t = (float)(y - 45) / (95 - 45);
                int nx = (int)Mathf.Lerp(85, 112, t);
                for (int dx = -3; dx <= 3; dx++)
                {
                    tex.SetPixel(nx + dx, y, new Color(0.35f, 0.65f, 0.82f, 1f));
                }
            }
            // Распылитель на кончике
            for (int y = 92; y <= 104; y++)
            {
                for (int x = 106; x <= 118; x++)
                {
                    tex.SetPixel(x, y, new Color(0.85f, 0.92f, 0.98f, 1f));
                }
            }
            SaveTex(tex, path);
        }

        private static void CreateSeedSprite(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            Vector2 center = new Vector2(64, 58);
            for (int y = 20; y <= 100; y++)
            {
                for (int x = 20; x <= 108; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= 36f)
                    {
                        float alpha = Mathf.Clamp01(36f - dist + 0.5f);
                        tex.SetPixel(x, y, new Color(0.95f, 0.85f, 0.45f, alpha));
                    }
                }
            }
            SaveTex(tex, path);
        }

        private static void CreateArrowRightSprite(string path, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Контурный треугольник ▶ (вершины (12, 12), (12, h-12), (w-12, h/2))
            Vector2 p1 = new Vector2(10, 10);
            Vector2 p2 = new Vector2(10, h - 10);
            Vector2 p3 = new Vector2(w - 12, h * 0.5f);

            DrawLine(tex, p1, p2, 4f, Color.white);
            DrawLine(tex, p2, p3, 4f, Color.white);
            DrawLine(tex, p3, p1, 4f, Color.white);

            SaveTex(tex, path);
        }

        private static void CreateFlowerPotSprite(string path, int size, bool isGray)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            Color potColor = isGray ? new Color(0.48f, 0.48f, 0.48f, 1f) : new Color(0.85f, 0.42f, 0.18f, 1f);
            Color rimColor = isGray ? new Color(0.42f, 0.42f, 0.42f, 1f) : new Color(0.72f, 0.35f, 0.14f, 1f);
            Color stemColor = isGray ? new Color(0.55f, 0.55f, 0.55f, 1f) : new Color(0.30f, 0.68f, 0.35f, 1f);
            Color petalColor = isGray ? new Color(0.80f, 0.80f, 0.80f, 1f) : new Color(0.98f, 0.98f, 0.98f, 1f);
            Color centerColor = isGray ? new Color(0.65f, 0.65f, 0.65f, 1f) : new Color(0.98f, 0.82f, 0.22f, 1f);

            // 1. Стебель
            DrawLine(tex, new Vector2(64, 48), new Vector2(64, 84), 4f, stemColor);
            DrawLine(tex, new Vector2(64, 58), new Vector2(50, 66), 3f, stemColor);
            DrawLine(tex, new Vector2(64, 64), new Vector2(78, 72), 3f, stemColor);

            // 2. Горшок (трапеция внизу)
            for (int y = 14; y <= 46; y++)
            {
                float t = (float)(y - 14) / (46 - 14);
                float halfW = Mathf.Lerp(20f, 26f, t);
                int minX = Mathf.RoundToInt(64 - halfW);
                int maxX = Mathf.RoundToInt(64 + halfW);
                for (int x = minX; x <= maxX; x++)
                {
                    tex.SetPixel(x, y, potColor);
                }
            }

            // Ободок горшка
            for (int y = 46; y <= 54; y++)
            {
                for (int x = 36; x <= 92; x++)
                {
                    tex.SetPixel(x, y, rimColor);
                }
            }

            // 3. Лепестки цветка (5 штук вокруг (64, 94))
            Vector2 flowerCenter = new Vector2(64, 94);
            float petalDist = 12f;
            float petalRadius = 9f;
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f * Mathf.Deg2Rad;
                Vector2 pCenter = flowerCenter + new Vector2(Mathf.Sin(angle) * petalDist, Mathf.Cos(angle) * petalDist);
                FillCircle(tex, pCenter, petalRadius, petalColor);
            }

            // 4. Сердцевина цветка
            FillCircle(tex, flowerCenter, 7f, centerColor);

            SaveTex(tex, path);
        }

        private static void CreateTooltipPointerSprite(string path, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Заполненный треугольник вниз (вершины (0, h-1), (w-1, h-1), (w/2, 0))
            Vector2 topL = new Vector2(0, h - 1);
            Vector2 topR = new Vector2(w - 1, h - 1);
            Vector2 bottomTip = new Vector2(w * 0.5f, 0);

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1); // 0 at bottom, 1 at top
                float halfW = Mathf.Lerp(0f, (w - 1) * 0.5f, t);
                int minX = Mathf.RoundToInt(w * 0.5f - halfW);
                int maxX = Mathf.RoundToInt(w * 0.5f + halfW);
                for (int x = minX; x <= maxX; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }

            SaveTex(tex, path);
        }

        private static void CreateTooltipPanelSprite(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
            SaveTex(tex, path);
        }

        private static void CreateAvailableRingSprite(string path, int size, int thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float outerR = center - 2f;
            float innerR = outerR - thickness;
            Color ringColor = new Color(0.29f, 0.87f, 0.50f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float aOuter = Mathf.Clamp01(outerR - dist + 0.5f);
                    float aInner = Mathf.Clamp01(dist - innerR + 0.5f);
                    float alpha = Mathf.Min(aOuter, aInner);
                    tex.SetPixel(x, y, new Color(ringColor.r, ringColor.g, ringColor.b, ringColor.a * alpha));
                }
            }
            SaveTex(tex, path);
        }

        private static void FillCircle(Texture2D tex, Vector2 center, float radius, Color col)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius - 1));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(center.x + radius + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius - 1));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(center.y + radius + 1));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    if (alpha > 0f)
                    {
                        Color existing = tex.GetPixel(x, y);
                        tex.SetPixel(x, y, Color.Lerp(existing, col, alpha));
                    }
                }
            }
        }

        private static void CreateArrowUpSprite(string path, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Контурный треугольник ▲
            Vector2 p1 = new Vector2(10, 6);
            Vector2 p2 = new Vector2(w - 10, 6);
            Vector2 p3 = new Vector2(w * 0.5f, h - 6);

            DrawLine(tex, p1, p2, 3f, Color.white);
            DrawLine(tex, p2, p3, 3f, Color.white);
            DrawLine(tex, p3, p1, 3f, Color.white);

            SaveTex(tex, path);
        }

        private static void DrawLine(Texture2D tex, Vector2 a, Vector2 b, float thickness, Color col)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b) * 2f);
            float halfThick = thickness * 0.5f;

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 pt = Vector2.Lerp(a, b, t);
                for (int dy = -Mathf.CeilToInt(halfThick); dy <= Mathf.CeilToInt(halfThick); dy++)
                {
                    for (int dx = -Mathf.CeilToInt(halfThick); dx <= Mathf.CeilToInt(halfThick); dx++)
                    {
                        int px = Mathf.RoundToInt(pt.x + dx);
                        int py = Mathf.RoundToInt(pt.y + dy);
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        {
                            tex.SetPixel(px, py, col);
                        }
                    }
                }
            }
        }

        private static void SaveTex(Texture2D tex, string path)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(tex);
        }
    }
}
