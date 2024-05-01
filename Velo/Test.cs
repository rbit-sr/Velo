using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using CEngine.Graphics.Layer;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
/*
namespace Velo
{
    internal class Test
    {
        public static Dictionary<int, Bitmap> tileImgs = new Dictionary<int, Bitmap>();

        public static Bitmap get_tile_bitmap(int[] data)
        {
            Bitmap square = new Bitmap(8, 8);
            
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int color = data[y * 8 + x];
                    square.SetPixel(x, y, Color.FromArgb(255, color, color, color));
                }
            }

            return square;
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static void test()
        {
            tileImgs.Add(0, get_tile_bitmap(new int[]
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            }));

            tileImgs.Add(1, get_tile_bitmap(new int[]
            {
                255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255
            }));

            tileImgs.Add(5, get_tile_bitmap(new int[]
            {
                255, 255, 0, 0, 255, 255, 0, 0,
                255, 255, 0, 0, 255, 255, 0, 0,
                0, 0, 255, 255, 0, 0, 255, 255,
                0, 0, 255, 255, 0, 0, 255, 255,
                255, 255, 0, 0, 255, 255, 0, 0,
                255, 255, 0, 0, 255, 255, 0, 0,
                0, 0, 255, 255, 0, 0, 255, 255,
                0, 0, 255, 255, 0, 0, 255, 255,
            }));

            tileImgs.Add(6, get_tile_bitmap(new int[]
            {
                0, 0, 0, 0, 0, 0, 0, 127,
                0, 0, 0, 0, 0, 0, 127, 255,
                0, 0, 0, 0, 0, 127, 255, 255,
                0, 0, 0, 0, 127, 255, 255, 255,
                0, 0, 0, 127, 255, 255, 255, 255,
                0, 0, 127, 255, 255, 255, 255, 255,
                0, 127, 255, 255, 255, 255, 255, 255,
                127, 255, 255, 255, 255, 255, 255, 255
            }));

            tileImgs.Add(7, get_tile_bitmap(new int[]
            {
                127, 0, 0, 0, 0, 0, 0, 0,
                255, 127, 0, 0, 0, 0, 0, 0,
                255, 255, 127, 0, 0, 0, 0, 0,
                255, 255, 255, 127, 0, 0, 0, 0,
                255, 255, 255, 255, 127, 0, 0, 0,
                255, 255, 255, 255, 255, 127, 0, 0,
                255, 255, 255, 255, 255, 255, 127, 0,
                255, 255, 255, 255, 255, 255, 255, 127
            }));

            tileImgs.Add(8, get_tile_bitmap(new int[]
            {
                0, 0, 0, 0, 255, 255, 255, 255,
                0, 0, 0, 0, 195, 255, 255, 255,
                0, 0, 0, 0, 195, 255, 255, 255,
                0, 0, 0, 0, 195, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255,
                195, 255, 255, 255, 255, 255, 255, 255,
                195, 255, 255, 255, 255, 255, 255, 255,
                195, 255, 255, 255, 255, 255, 255, 255
            }));

            tileImgs.Add(9, get_tile_bitmap(new int[]
            {
                255, 255, 255, 255, 0, 0, 0, 0,
                255, 255, 255, 195, 0, 0, 0, 0,
                255, 255, 255, 195, 0, 0, 0, 0,
                255, 255, 255, 195, 0, 0, 0, 0,
                255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 195,
                255, 255, 255, 255, 255, 255, 255, 195,
                255, 255, 255, 255, 255, 255, 255, 195
            }));

            tileImgs.Add(10, get_tile_bitmap(new int[]
            {
                255, 255, 0, 0, 255, 255, 0, 127,
                255, 255, 0, 0, 255, 255, 127, 255,
                0, 0, 255, 255, 0, 127, 255, 255,
                0, 0, 255, 255, 127, 255, 255, 255,
                255, 255, 0, 127, 255, 255, 255, 255,
                255, 255, 127, 255, 255, 255, 255, 255,
                0, 127, 255, 255, 255, 255, 255, 255,
                127, 255, 255, 255, 255, 255, 255, 255
            }));

            tileImgs.Add(11, get_tile_bitmap(new int[]
            {
                255, 255, 0, 0, 255, 255, 0, 0,
                255, 255, 0, 0, 255, 255, 0, 0,
                255, 255, 255, 255, 0, 0, 255, 255,
                255, 255, 255, 255, 0, 0, 255, 255,
                255, 255, 255, 255, 255, 255, 0, 0,
                255, 255, 255, 255, 255, 255, 0, 0,
                255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255
            }));

            tileImgs.Add(12, get_tile_bitmap(new int[]
            {
                127, 255, 255, 255, 255, 255, 255, 255,
                0, 127, 255, 255, 255, 255, 255, 255,
                0, 0, 127, 255, 255, 255, 255, 255,
                0, 0, 0, 127, 255, 255, 255, 255,
                0, 0, 0, 0, 127, 255, 255, 255,
                0, 0, 0, 0, 0, 127, 255, 255,
                0, 0, 0, 0, 0, 0, 127, 255,
                0, 0, 0, 0, 0, 0, 0, 127
            }));

            tileImgs.Add(13, get_tile_bitmap(new int[]
            {
                255, 255, 255, 255, 255, 255, 255, 127,
                255, 255, 255, 255, 255, 255, 127, 0,
                255, 255, 255, 255, 255, 127, 0, 0,
                255, 255, 255, 255, 127, 0, 0, 0,
                255, 255, 255, 127, 0, 0, 0, 0,
                255, 255, 127, 0, 0, 0, 0, 0,
                255, 127, 0, 0, 0, 0, 0, 0,
                127, 0, 0, 0, 0, 0, 0, 0
            }));

            tileImgs.Add(14, get_tile_bitmap(new int[]
            {
                255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255,
                0, 0, 255, 255, 255, 255, 255, 255,
                0, 0, 255, 255, 255, 255, 255, 255,
                255, 255, 0, 0, 255, 255, 255, 255,
                255, 255, 0, 0, 255, 255, 255, 255,
                0, 0, 255, 255, 0, 0, 255, 255,
                0, 0, 255, 255, 0, 0, 255, 255
            }));

            tileImgs.Add(15, get_tile_bitmap(new int[]
            {
                255, 255, 255, 255, 255, 255, 255, 127,
                255, 255, 255, 255, 255, 255, 127, 0,
                255, 255, 255, 255, 255, 127, 255, 255,
                255, 255, 255, 255, 127, 0, 255, 255,
                255, 255, 255, 127, 255, 255, 0, 0,
                255, 255, 127, 0, 255, 255, 0, 0,
                255, 127, 255, 255, 0, 0, 255, 255,
                127, 0, 255, 255, 0, 0, 255, 255
            }));

            CBufferedTileMapLayer tiles = (CBufferedTileMapLayer)CEngine.CEngine.Instance.LayerManager.GetLayer("Collision");
 
            Image img = Image.FromFile("test.jpg");
            Bitmap bmp = new Bitmap(img);

            int imgW = img.Width;
            int imgH = img.Height;
            int tilesW = tiles.Width;
            int tilesH = tiles.Height;

            if (imgW * tilesH > tilesW * imgH)
            {
                tilesH = tilesW * imgH / imgW;
            }
            else
            {
                tilesW = tilesH * imgW / imgH;
            }

            bmp = ResizeImage(bmp, tilesW * 8, tilesH * 8);

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color oc = bmp.GetPixel(x, y);
                    int grayScale = (int)((oc.R * 0.3) + (oc.G * 0.59) + (oc.B * 0.11));
                    Color nc = Color.FromArgb(oc.A, grayScale, grayScale, grayScale);
                    bmp.SetPixel(x, y, nc);
                }
            }

            float[,] errs = new float[bmp.Width, bmp.Height];

            for (int y = 0; y < tilesH; y++)
            {
                for (int x = 0; x < tilesW; x++)
                {
                    Bitmap crop = bmp.Clone(new Rectangle(x * 8, y * 8, 8, 8), bmp.PixelFormat);

                    float minErr = int.MaxValue;
                    int minErrId = 0;

                    foreach (var entry in tileImgs)
                    {
                        float totDiff = 0;

                        for (int y1 = 0; y1 < 8; y1++)
                        {
                            for (int x1 = 0; x1 < 8; x1++)
                            {
                                Color pixel1 = entry.Value.GetPixel(x1, y1);
                                Color pixel2 = crop.GetPixel(x1, y1);

                                float diff = 255 - (int)pixel1.R - (int)pixel2.R + errs[x, y];
                                totDiff += diff;
                            }
                        }

                        float err = totDiff / 64f;

                        if (Math.Abs(err) < Math.Abs(minErr))
                        {
                            minErr = err;
                            minErrId = entry.Key;
                        }
                    }

                    tiles.SetTile(200 + x, y, minErrId);

                    if (x + 1 < tilesW)
                        errs[x + 1, y] += minErr * 7 / 16;
                    if (y + 1 < tilesH)
                    {
                        if (x - 1 >= 0)
                            errs[x - 1, y + 1] += minErr * 3 / 16;
                        errs[x, y + 1] += minErr * 5 / 16;
                        if (x + 1 < tilesW)
                            errs[x + 1, y + 1] += minErr * 1 / 16;
                    }
                }
            }
        }
    }
}
*/