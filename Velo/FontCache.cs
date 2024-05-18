using CEngine.Graphics.Library;
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

        public static void Get(ref CachedFont font, string name, int size)
        {
            string key = name + ":" + size;
            if (font != null && font.Key == key)
                return;

            if (font != null)
                font.Release();

            if (fonts.TryGetValue(key, out font) && font != null)
            {
                font.Count++;
                return;
            }

            CFont newFont = new CFont(name, size);
            Velo.ContentManager.Load(newFont, false);
            font = new CachedFont { Key = key, Font = newFont, Count = 1 };
            fonts.Add(key, font);
        }

        public static void Release(ref CachedFont font)
        {
            if (font == null)
                return;
            font.Release();
        }
    }
}
