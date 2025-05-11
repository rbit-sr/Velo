using CEngine.Graphics.Library;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Velo
{
    public class CachedFont
    {
        public string Key;
        public CFont Font;
        public int Count;

        public void Release()
        {
            Count--;
            if (Count == 0)
                Velo.ContentManager.Release(Font);
        }
    }

    public static class FontCache
    {
        private static readonly ConditionalWeakTable<string, CachedFont> fonts = new ConditionalWeakTable<string, CachedFont>();

        public static void Get(ref CachedFont font, string name)
        {
            string key = name;
            if (font != null && font.Key == key)
                return;

            font?.Release();

            if (fonts.TryGetValue(key, out font) && font != null)
            {
                font.Count++;
                return;
            }

            string[] split = name.Split(':');

            if (File.Exists("Content\\" + split[0]))
            {
                CFont newFont = new CFont(name);
                Velo.ContentManager.Load(newFont, false);
                font = new CachedFont { Key = key, Font = newFont, Count = 1 };
                fonts.Add(key, font);
            }
            else
            {
                int fontSize = 24;
                if (split.Length >= 2)
                {
                    fontSize = int.Parse(split[1]);
                }
                Get(ref font, "UI\\Font\\ariblk.ttf:" + fontSize);
            }
        }

        public static void Release(ref CachedFont font)
        {
            if (font == null)
                return;
            font.Release();
        }
    }
}
