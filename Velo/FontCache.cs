using CEngine.Graphics.Library;
using System.Collections.Generic;

namespace Velo
{
    public static class FontCache
    {
        private static Dictionary<string, CFont> fonts = new Dictionary<string,CFont>();

        public static CFont Get(string name, int size)
        {
            string key = name + ":" + size;
            if (fonts.ContainsKey(key))
                return fonts[key];

            CFont newFont = new CFont(name, size);
            Velo.ContentManager.Load(newFont, false);
            fonts.Add(key, newFont);
            return newFont;
        }
    }
}
