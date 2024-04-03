using System;
using Microsoft.Xna.Framework;
using CEngine.Graphics.Layer;
using Microsoft.Xna.Framework.Graphics;

namespace Velo
{
    public class TileUtil
    {
        public const int TILE_AIR = 0;
        public const int TILE_SQUARE = 1;
        public const int TILE_WALL_LEFT = 2;
        public const int TILE_GRAPPLE_CEIL = 3;
        public const int TILE_WALL_RIGHT = 4;
        public const int TILE_CHECKERED = 5;
        public const int TILE_SLOPE_FLOOR_LEFT = 6;
        public const int TILE_SLOPE_FLOOR_RIGHT = 7;
        public const int TILE_STAIRS_LEFT = 8;
        public const int TILE_STAIRS_RIGHT = 9;
        public const int TILE_CHECKERED_SLOPE_FLOOR_LEFT = 10;
        public const int TILE_CHECKERED_SLOPE_FLOOR_RIGHT = 11;
        public const int TILE_SLOPE_CEIL_LEFT = 12;
        public const int TILE_SLOPE_CEIL_RIGHT = 13;
        public const int TILE_CHECKERED_SLOPE_CEIL_LEFT = 14;
        public const int TILE_CHECKERED_SLOPE_CEIL_RIGHT = 15;

        public const int SHAPE_AIR = 0;
        public const int SHAPE_SQUARE = 1;
        public const int SHAPE_SLOPE_CEIL_RIGHT = 2;
        public const int SHAPE_SLOPE_CEIL_LEFT = 3;
        public const int SHAPE_SLOPE_FLOOR_RIGHT = 4;
        public const int SHAPE_SLOPE_FLOOR_LEFT = 5;

        public static int GetShape(int tile)
        {
            switch (tile)
            {
                case TILE_AIR:
                case TILE_CHECKERED:
                    return SHAPE_AIR;
                case TILE_SQUARE:
                case TILE_GRAPPLE_CEIL:
                case TILE_WALL_LEFT:
                case TILE_WALL_RIGHT:
                    return SHAPE_SQUARE;
                case TILE_SLOPE_FLOOR_RIGHT:
                case TILE_CHECKERED_SLOPE_FLOOR_RIGHT:
                case TILE_STAIRS_RIGHT:
                    return SHAPE_SLOPE_FLOOR_RIGHT;
                case TILE_SLOPE_FLOOR_LEFT:
                case TILE_CHECKERED_SLOPE_FLOOR_LEFT:
                case TILE_STAIRS_LEFT:
                    return SHAPE_SLOPE_FLOOR_LEFT;
                case TILE_SLOPE_CEIL_RIGHT:
                case TILE_CHECKERED_SLOPE_CEIL_RIGHT:
                    return SHAPE_SLOPE_CEIL_RIGHT;
                case TILE_SLOPE_CEIL_LEFT:
                case TILE_CHECKERED_SLOPE_CEIL_LEFT:
                    return SHAPE_SLOPE_CEIL_LEFT;
            }
            return SHAPE_AIR;
        }

        public static int GetShape(CBufferedTileMapLayer tilemap, int x, int y)
        {
            if (y < 0)
                return SHAPE_AIR;
            if (x < 0 || x >= tilemap.Width || y >= tilemap.Height)
                return SHAPE_SQUARE;
            return GetShape(tilemap.GetTile(x, y));
        }

        public static bool TopEdge(int shape)
        {
            return
                shape == SHAPE_SQUARE || shape == SHAPE_SLOPE_CEIL_RIGHT || shape == SHAPE_SLOPE_CEIL_LEFT;
        }

        public static bool BottomEdge(int shape)
        {
            return
                shape == SHAPE_SQUARE || shape == SHAPE_SLOPE_FLOOR_RIGHT || shape == SHAPE_SLOPE_FLOOR_LEFT;
        }

        public static bool LeftEdge(int shape)
        {
            return
                shape == SHAPE_SQUARE || shape == SHAPE_SLOPE_CEIL_RIGHT || shape == SHAPE_SLOPE_FLOOR_RIGHT;
        }

        public static bool RightEdge(int shape)
        {
            return
                shape == SHAPE_SQUARE || shape == SHAPE_SLOPE_CEIL_LEFT || shape == SHAPE_SLOPE_FLOOR_LEFT;
        }

        public static bool TopLeftCorner(int shape)
        {
            return
                shape == SHAPE_SQUARE || shape == SHAPE_SLOPE_CEIL_RIGHT;
        }

        public static bool BottomLeftCorner(int shape)
        {
            return
                shape == SHAPE_SQUARE || shape == SHAPE_SLOPE_FLOOR_RIGHT;
        }

        public static bool TopRightCorner(int shape)
        {
            return
                shape == SHAPE_SQUARE || shape == SHAPE_SLOPE_CEIL_LEFT;
        }

        public static bool BottomRightCorner(int shape)
        {
            return
                shape == SHAPE_SQUARE || shape == SHAPE_SLOPE_FLOOR_LEFT;
        }

        public static bool FromLineStyle(ELineStyle style, int pos)
        {
            switch (style)
            {
                case ELineStyle.SOLID:
                    return true;
                case ELineStyle.DASHED:
                    return pos < 8;
                case ELineStyle.DOTTED:
                    return pos / 4 % 2 == 0;
            }
            return true;
        }
    }

    public class TileMap : Module
    {
        public ColorTransitionSetting ColorMultiplier;
        public IntSetting OutlineWidth;
        public ColorSetting OutlineColor;
        public ColorSetting SlopeOutlineColor;
        public BoolSetting EnableVeloCustomTiles;
        public ColorSetting FillColor;
        public ColorSetting GrappleCeilingColor;
        public IntSetting GrappleCeilingWidth;
        public LineStyleSetting GrappleCeilingStyle;
        public ColorSetting WallColor;
        public IntSetting WallWidth;
        public LineStyleSetting WallStyle;
        public BoolSetting StairsReplaceWithSlopes;
        public BoolSetting RemoveCheckered;
        public BoolSetting SlopeAntiAliasing;

        private Texture2D pixel;
        private Texture2D[] tiles = new Texture2D[16];
        private Texture2D slopeCeilRightOutline;
        private Texture2D slopeCeilLeftOutline;
        private Texture2D slopeFloorRightOutline;
        private Texture2D slopeFloorLeftOutline;
        private Texture2D slopeCeilRightOutlineCornerBoth;
        private Texture2D slopeCeilRightOutlineCornerBelow;
        private Texture2D slopeCeilRightOutlineCornerRight;
        private Texture2D slopeCeilLeftOutlineCornerBoth;
        private Texture2D slopeCeilLeftOutlineCornerBelow;
        private Texture2D slopeCeilLeftOutlineCornerLeft;
        private Texture2D slopeFloorRightOutlineCornerBoth;
        private Texture2D slopeFloorRightOutlineCornerAbove;
        private Texture2D slopeFloorRightOutlineCornerRight;
        private Texture2D slopeFloorLeftOutlineCornerBoth;
        private Texture2D slopeFloorLeftOutlineCornerAbove;
        private Texture2D slopeFloorLeftOutlineCornerLeft;
        private Texture2D slopeCeilRightOutlineCornerSmall;
        private Texture2D slopeCeilLeftOutlineCornerSmall;
        private Texture2D slopeFloorRightOutlineCornerSmall;
        private Texture2D slopeFloorLeftOutlineCornerSmall;

        private bool texturesSetUp = false;

        private TileMap() : base("Tile Map")
        {
            ColorMultiplier = AddColorTransition("color multiplier", new ColorTransition(Color.White));
            NewCategory("outline");
            OutlineWidth = AddInt("outline width", 0, 0, 16);
            OutlineColor = AddColor("outline color", Color.White);
            SlopeOutlineColor = AddColor("slope outline color", Color.White);
            NewCategory("Velo custom tiles");
            EnableVeloCustomTiles = AddBool("enable", false);
            FillColor = AddColor("fill color", Color.Black);
            GrappleCeilingColor = AddColor("grapple ceiling color", Color.White);
            GrappleCeilingWidth = AddInt("grapple ceiling width", 3, 0, 16);
            GrappleCeilingStyle = AddLineStyle("grapple ceiling style", ELineStyle.SOLID);
            WallColor = AddColor("wall color", Color.White);
            WallWidth = AddInt("wall width", 3, 0, 16);
            WallStyle = AddLineStyle("wall style", ELineStyle.DOTTED);
            StairsReplaceWithSlopes = AddBool("stairs replace width slopes", false);
            RemoveCheckered = AddBool("remove checkered", false);
            SlopeAntiAliasing = AddBool("slope anti aliasing", false);
            EndCategory();
        }

        public static TileMap Instance = new TileMap();

        public override void PostRender()
        {
            base.PostRender();

            ICLayer layer = CEngine.CEngine.Instance.LayerManager.GetLayer("Collision");
            if (layer == null)
                return;

            if (
                OutlineWidth.Modified() ||
                OutlineColor.Modified() ||
                SlopeOutlineColor.Modified() ||
                EnableVeloCustomTiles.Modified() ||
                FillColor.Modified() ||
                GrappleCeilingColor.Modified() ||
                GrappleCeilingWidth.Modified() ||
                GrappleCeilingStyle.Modified() ||
                WallColor.Modified() ||
                WallWidth.Modified() ||
                WallStyle.Modified() ||
                StairsReplaceWithSlopes.Modified() ||
                RemoveCheckered.Modified() ||
                SlopeAntiAliasing.Modified()
                )
            {
                texturesSetUp = false;
            }

            if (!texturesSetUp)
            {
                setUpTextures();
                ((CBufferedTileMapLayer)layer).refresh();
            }
        }

        private void setUpTextures()
        {
            if (pixel != null)
                pixel.Dispose();
            for (int i = 1; i < 16; i++)
            {
                if (tiles[i] != null)
                    tiles[i].Dispose();
            }
            if (slopeCeilRightOutline != null)
                slopeCeilRightOutline.Dispose();
            if (slopeCeilLeftOutline != null)
                slopeCeilLeftOutline.Dispose();
            if (slopeFloorRightOutline != null)
                slopeFloorRightOutline.Dispose();
            if (slopeFloorLeftOutline != null)
                slopeFloorLeftOutline.Dispose();
            if (slopeCeilRightOutlineCornerBoth != null)
                slopeCeilRightOutlineCornerBoth.Dispose();
            if (slopeCeilRightOutlineCornerBelow != null)
                slopeCeilRightOutlineCornerBelow.Dispose();
            if (slopeCeilRightOutlineCornerRight != null)
                slopeCeilRightOutlineCornerRight.Dispose();
            if (slopeCeilLeftOutlineCornerBoth != null)
                slopeCeilLeftOutlineCornerBoth.Dispose();
            if (slopeCeilLeftOutlineCornerBelow != null)
                slopeCeilLeftOutlineCornerBelow.Dispose();
            if (slopeCeilLeftOutlineCornerLeft != null)
                slopeCeilLeftOutlineCornerLeft.Dispose();
            if (slopeFloorRightOutlineCornerBoth != null)
                slopeFloorRightOutlineCornerBoth.Dispose();
            if (slopeFloorRightOutlineCornerAbove != null)
                slopeFloorRightOutlineCornerAbove.Dispose();
            if (slopeFloorRightOutlineCornerRight != null)
                slopeFloorRightOutlineCornerRight.Dispose();
            if (slopeFloorLeftOutlineCornerBoth != null)
                slopeFloorLeftOutlineCornerBoth.Dispose();
            if (slopeFloorLeftOutlineCornerAbove != null)
                slopeFloorLeftOutlineCornerAbove.Dispose();
            if (slopeFloorLeftOutlineCornerLeft != null)
                slopeFloorLeftOutlineCornerLeft.Dispose();

            int outline_width = OutlineWidth.Value;
            int slopeOutline_width = (int)(outline_width * Math.Sqrt(2.0) + 0.5f);

            pixel = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 1, 1);
            pixel.SetData(new Color[]
                {
                Color.White
                }, 0, 1);

            Color[] data = new Color[16 * 16];

            // square
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    data[i] = FillColor.Value;
                    i++;
                }
            }
            tiles[TileUtil.TILE_SQUARE] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_SQUARE].SetData(data, 0, 16 * 16);

            // wall right
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x >= 16 - WallWidth.Value && TileUtil.FromLineStyle(WallStyle.Value, y))
                        data[i] = WallColor.Value;
                    else
                        data[i] = FillColor.Value;
                    i++;
                }
            }
            tiles[TileUtil.TILE_WALL_RIGHT] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_WALL_RIGHT].SetData(data, 0, 16 * 16);

            // wall left
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x < WallWidth.Value && TileUtil.FromLineStyle(WallStyle.Value, y))
                        data[i] = WallColor.Value;
                    else
                        data[i] = FillColor.Value;
                    i++;
                }
            }
            tiles[TileUtil.TILE_WALL_LEFT] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_WALL_LEFT].SetData(data, 0, 16 * 16);

            // grapple ceiling
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (y >= 16 - GrappleCeilingWidth.Value && TileUtil.FromLineStyle(GrappleCeilingStyle.Value, x))
                        data[i] = GrappleCeilingColor.Value;
                    else
                        data[i] = FillColor.Value;
                    i++;
                }
            }
            tiles[TileUtil.TILE_GRAPPLE_CEIL] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_GRAPPLE_CEIL].SetData(data, 0, 16 * 16);

            // checkered
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if ((x / 4 % 2 == 0) == (y / 4 % 2 == 0))
                        data[i] = FillColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_CHECKERED] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_CHECKERED].SetData(data, 0, 16 * 16);

            // slope ceiling right
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x <= 15 - y)
                    {
                        data[i] = FillColor.Value;
                        if (SlopeAntiAliasing.Value && x == 15 - y)
                            data[i].A = 127;
                    }
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_SLOPE_CEIL_RIGHT] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_SLOPE_CEIL_RIGHT].SetData(data, 0, 16 * 16);

            // slope ceiling left
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x >= y)
                    {
                        data[i] = FillColor.Value;
                        if (SlopeAntiAliasing.Value && x == y)
                            data[i].A = 127;
                    }
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_SLOPE_CEIL_LEFT] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_SLOPE_CEIL_LEFT].SetData(data, 0, 16 * 16);

            // slope floor right
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x <= y)
                    {
                        data[i] = FillColor.Value;
                        if (SlopeAntiAliasing.Value && x == y)
                            data[i].A = 127;
                    }
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_SLOPE_FLOOR_RIGHT] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_SLOPE_FLOOR_RIGHT].SetData(data, 0, 16 * 16);

            // slope floor left
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x >= 15 - y)
                    {
                        data[i] = FillColor.Value;
                        if (SlopeAntiAliasing.Value && x == 15 - y)
                            data[i].A = 127;
                    }
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_SLOPE_FLOOR_LEFT] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_SLOPE_FLOOR_LEFT].SetData(data, 0, 16 * 16);

            int[] stairs_profile = { 8, 8, 9, 9, 9, 9, 9, 9, 0, 0, 1, 1, 1, 1, 1, 1 };

            // stairs right
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (16 - x > stairs_profile[y])

                        data[i] = FillColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_STAIRS_RIGHT] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_STAIRS_RIGHT].SetData(data, 0, 16 * 16);

            // stairs left
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x >= stairs_profile[y])
                        data[i] = FillColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_STAIRS_LEFT] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_STAIRS_LEFT].SetData(data, 0, 16 * 16);

            // checkered slope ceiling right
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x <= 15 - y || (x / 4 % 2 == 0) == (y / 4 % 2 == 0))
                    {
                        data[i] = FillColor.Value;
                        if (SlopeAntiAliasing.Value && x == 15 - y)
                            data[i].A = 127;
                    }
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_CHECKERED_SLOPE_CEIL_RIGHT] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_CHECKERED_SLOPE_CEIL_RIGHT].SetData(data, 0, 16 * 16);

            // checkered slope ceiling left
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x >= y || (x / 4 % 2 == 0) == (y / 4 % 2 == 0))
                    {
                        data[i] = FillColor.Value;
                        if (SlopeAntiAliasing.Value && x == y)
                            data[i].A = 127;
                    }
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_CHECKERED_SLOPE_CEIL_LEFT] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_CHECKERED_SLOPE_CEIL_LEFT].SetData(data, 0, 16 * 16);

            // checkered slope floor right
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x <= y || (x / 4 % 2 == 0) == (y / 4 % 2 == 0))
                    {
                        data[i] = FillColor.Value;
                        if (SlopeAntiAliasing.Value && x == y)
                            data[i].A = 127;
                    }
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_CHECKERED_SLOPE_FLOOR_RIGHT] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_CHECKERED_SLOPE_FLOOR_RIGHT].SetData(data, 0, 16 * 16);

            // checkered slope floor left
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x >= 15 - y || (x / 4 % 2 == 0) == (y / 4 % 2 == 0))
                    {
                        data[i] = FillColor.Value;
                        if (SlopeAntiAliasing.Value && x == 15 - y)
                            data[i].A = 127;
                    }
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_CHECKERED_SLOPE_FLOOR_LEFT] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            tiles[TileUtil.TILE_CHECKERED_SLOPE_FLOOR_LEFT].SetData(data, 0, 16 * 16);

            // slope ceiling right outline
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x <= 15 - y && x > 15 - y - slopeOutline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeCeilRightOutline = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeCeilRightOutline.SetData(data, 0, 16 * 16);

            // slope ceiling left outline
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x >= y && x < y + slopeOutline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeCeilLeftOutline = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeCeilLeftOutline.SetData(data, 0, 16 * 16);

            // slope floor right outline
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x <= y && x > y - slopeOutline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeFloorRightOutline = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeFloorRightOutline.SetData(data, 0, 16 * 16);

            // slope floor left outline
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x >= 15 - y && x < 15 - y + slopeOutline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeFloorLeftOutline = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeFloorLeftOutline.SetData(data, 0, 16 * 16);

            // slope ceiling right outline corner with both neightbors slopes
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x + y >= 32 - slopeOutline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeCeilRightOutlineCornerBoth = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeCeilRightOutlineCornerBoth.SetData(data, 0, 16 * 16);

            // slope ceiling right outline corner with below neightbor slope
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x + y >= 32 - slopeOutline_width && y >= 16 - outline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeCeilRightOutlineCornerBelow = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeCeilRightOutlineCornerBelow.SetData(data, 0, 16 * 16);

            // slope ceiling right outline corner with right neightbor slope
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x + y >= 32 - slopeOutline_width && x >= 16 - outline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeCeilRightOutlineCornerRight = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeCeilRightOutlineCornerRight.SetData(data, 0, 16 * 16);

            // slope ceiling left outline corner with both neightbors slopes
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (15 - x + y >= 32 - slopeOutline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeCeilLeftOutlineCornerBoth = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeCeilLeftOutlineCornerBoth.SetData(data, 0, 16 * 16);

            // slope ceiling left outline corner with below neightbor slope
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (15 - x + y >= 32 - slopeOutline_width && y >= 16 - outline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeCeilLeftOutlineCornerBelow = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeCeilLeftOutlineCornerBelow.SetData(data, 0, 16 * 16);

            // slope ceiling left outline corner with left neightbor slope
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (15 - x + y >= 32 - slopeOutline_width && x < outline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeCeilLeftOutlineCornerLeft = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeCeilLeftOutlineCornerLeft.SetData(data, 0, 16 * 16);

            // slope floor right outline corner with both neightbors slopes
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x + 15 - y >= 32 - slopeOutline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeFloorRightOutlineCornerBoth = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeFloorRightOutlineCornerBoth.SetData(data, 0, 16 * 16);

            // slope floor right outline corner with above neightbor slope
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x + 15 - y >= 32 - slopeOutline_width && y < outline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeFloorRightOutlineCornerAbove = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeFloorRightOutlineCornerAbove.SetData(data, 0, 16 * 16);

            // slope floor right outline corner with right neightbor slope
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x + 15 - y >= 32 - slopeOutline_width && x >= 16 - outline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeFloorRightOutlineCornerRight = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeFloorRightOutlineCornerRight.SetData(data, 0, 16 * 16);

            // slope floor left outline corner with both neightbors slopes
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (15 - x + 15 - y >= 32 - slopeOutline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeFloorLeftOutlineCornerBoth = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeFloorLeftOutlineCornerBoth.SetData(data, 0, 16 * 16);

            // slope floor left outline corner with above neightbor slope
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (15 - x + 15 - y >= 32 - slopeOutline_width && y < outline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeFloorLeftOutlineCornerAbove = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeFloorLeftOutlineCornerAbove.SetData(data, 0, 16 * 16);

            // slope floor left outline corner with left neightbor slope
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (15 - x + 15 - y >= 32 - slopeOutline_width && x < outline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeFloorLeftOutlineCornerLeft = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeFloorLeftOutlineCornerLeft.SetData(data, 0, 16 * 16);

            // slope ceiling right outline small corner
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x + y >= 48 - slopeOutline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeCeilRightOutlineCornerSmall = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeCeilRightOutlineCornerSmall.SetData(data, 0, 16 * 16);

            // slope ceiling left outline small corner
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (15 - x + y >= 48 - slopeOutline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeCeilLeftOutlineCornerSmall = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeCeilLeftOutlineCornerSmall.SetData(data, 0, 16 * 16);

            // slope floor right outline small corner
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x + 15 - y >= 48 - slopeOutline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeFloorRightOutlineCornerSmall = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeFloorRightOutlineCornerSmall.SetData(data, 0, 16 * 16);

            // slope floor left outline small corner
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (15 - x + 15 - y >= 48 - slopeOutline_width)
                        data[i] = SlopeOutlineColor.Value;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            slopeFloorLeftOutlineCornerSmall = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            slopeFloorLeftOutlineCornerSmall.SetData(data, 0, 16 * 16);

            texturesSetUp = true;
        }

        public bool Draw(CBufferedTileMapLayer tilemap, Vector2 pos, int x, int y)
        {
            if (!EnableVeloCustomTiles.Value && OutlineWidth.Value == 0 || tilemap.Id != "Collision")
                return false;

            if (!texturesSetUp)
                return false;
            
            int tileId = tilemap.GetTile(x, y);
            if (StairsReplaceWithSlopes.Value)
            {
                if (tileId == TileUtil.TILE_STAIRS_RIGHT)
                    tileId = TileUtil.TILE_SLOPE_FLOOR_RIGHT;
                if (tileId == TileUtil.TILE_STAIRS_LEFT)
                    tileId = TileUtil.TILE_SLOPE_FLOOR_LEFT;
            }
            if (RemoveCheckered.Value)
            {
                if (tileId == TileUtil.TILE_CHECKERED)
                    return true;
                if (tileId == TileUtil.TILE_CHECKERED_SLOPE_CEIL_RIGHT)
                    tileId = TileUtil.TILE_SLOPE_CEIL_RIGHT;
                if (tileId == TileUtil.TILE_CHECKERED_SLOPE_CEIL_LEFT)
                    tileId = TileUtil.TILE_SLOPE_CEIL_LEFT;
                if (tileId == TileUtil.TILE_CHECKERED_SLOPE_FLOOR_RIGHT)
                    tileId = TileUtil.TILE_SLOPE_FLOOR_RIGHT;
                if (tileId == TileUtil.TILE_CHECKERED_SLOPE_FLOOR_LEFT)
                    tileId = TileUtil.TILE_SLOPE_FLOOR_LEFT;
            }

            if (EnableVeloCustomTiles.Value)
                Velo.SpriteBatch.Draw(tiles[tileId], new Rectangle((int)pos.X, (int)pos.Y, 16, 16), Color.White);
            else
                Velo.SpriteBatch.Draw(tilemap.tileImage.GetTileImage(tileId).Image, new Rectangle((int)pos.X, (int)pos.Y, 16, 16), Color.White);

            if (OutlineWidth.Value == 0)
                return true;

            Color outlineCol = OutlineColor.Value;
            int outlineWidth = OutlineWidth.Value;

            int here = TileUtil.GetShape(tilemap, x, y);
            int above = TileUtil.GetShape(tilemap, x, y - 1);
            int below = TileUtil.GetShape(tilemap, x, y + 1);
            int left = TileUtil.GetShape(tilemap, x - 1, y);
            int right = TileUtil.GetShape(tilemap, x + 1, y);
            int aboveLeft = TileUtil.GetShape(tilemap, x - 1, y - 1);
            int belowLeft = TileUtil.GetShape(tilemap, x - 1, y + 1);
            int aboveRight = TileUtil.GetShape(tilemap, x + 1, y - 1);
            int belowRight = TileUtil.GetShape(tilemap, x + 1, y + 1);

            if (here == TileUtil.SHAPE_AIR)
                return true;

            if (outlineWidth >= 16 && (tileId == TileUtil.TILE_GRAPPLE_CEIL || tileId == TileUtil.TILE_WALL_LEFT || tileId == TileUtil.TILE_WALL_RIGHT))
                return true;

            if (here == TileUtil.SHAPE_SQUARE && !TileUtil.BottomEdge(above))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, outlineWidth),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SQUARE && !TileUtil.TopEdge(below) && tileId != TileUtil.TILE_GRAPPLE_CEIL)
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X, (int)pos.Y + 16 - outlineWidth, 16, outlineWidth),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SQUARE && !TileUtil.RightEdge(left) && tileId != TileUtil.TILE_WALL_LEFT)
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X, (int)pos.Y, outlineWidth, 16),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SQUARE && !TileUtil.LeftEdge(right) && tileId != TileUtil.TILE_WALL_RIGHT)
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X + 16 - outlineWidth, (int)pos.Y, outlineWidth, 16),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SLOPE_CEIL_RIGHT)
            {
                Velo.SpriteBatch.Draw(
                    slopeCeilRightOutline,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (here == TileUtil.SHAPE_SLOPE_CEIL_LEFT)
            {
                Velo.SpriteBatch.Draw(
                    slopeCeilLeftOutline,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (here == TileUtil.SHAPE_SLOPE_FLOOR_RIGHT)
            {
                Velo.SpriteBatch.Draw(
                    slopeFloorRightOutline,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (here == TileUtil.SHAPE_SLOPE_FLOOR_LEFT)
            {
                Velo.SpriteBatch.Draw(
                    slopeFloorLeftOutline,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (TileUtil.BottomRightCorner(here) && below == TileUtil.SHAPE_SLOPE_CEIL_RIGHT && right == TileUtil.SHAPE_SLOPE_CEIL_RIGHT && (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
            {
                Velo.SpriteBatch.Draw(
                    slopeCeilRightOutlineCornerBoth,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (TileUtil.BottomRightCorner(here) && below == TileUtil.SHAPE_SLOPE_CEIL_RIGHT && right != TileUtil.SHAPE_SLOPE_CEIL_RIGHT && (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
            {
                Velo.SpriteBatch.Draw(
                    slopeCeilRightOutlineCornerBelow,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (TileUtil.BottomRightCorner(here) && below != TileUtil.SHAPE_SLOPE_CEIL_RIGHT && right == TileUtil.SHAPE_SLOPE_CEIL_RIGHT && (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
            {
                Velo.SpriteBatch.Draw(
                    slopeCeilRightOutlineCornerRight,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (TileUtil.BottomLeftCorner(here) && below == TileUtil.SHAPE_SLOPE_CEIL_LEFT && left == TileUtil.SHAPE_SLOPE_CEIL_LEFT && (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
            {
                Velo.SpriteBatch.Draw(
                    slopeCeilLeftOutlineCornerBoth,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (TileUtil.BottomLeftCorner(here) && below == TileUtil.SHAPE_SLOPE_CEIL_LEFT && left != TileUtil.SHAPE_SLOPE_CEIL_LEFT && (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
            {
                Velo.SpriteBatch.Draw(
                    slopeCeilLeftOutlineCornerBelow,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (TileUtil.BottomLeftCorner(here) && below != TileUtil.SHAPE_SLOPE_CEIL_LEFT && left == TileUtil.SHAPE_SLOPE_CEIL_LEFT && (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
            {
                Velo.SpriteBatch.Draw(
                    slopeCeilLeftOutlineCornerLeft,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (TileUtil.TopRightCorner(here) && above == TileUtil.SHAPE_SLOPE_FLOOR_RIGHT && right == TileUtil.SHAPE_SLOPE_FLOOR_RIGHT && (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
            {
                Velo.SpriteBatch.Draw(
                    slopeFloorRightOutlineCornerBoth,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (TileUtil.TopRightCorner(here) && above == TileUtil.SHAPE_SLOPE_FLOOR_RIGHT && right != TileUtil.SHAPE_SLOPE_FLOOR_RIGHT && (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
            {
                Velo.SpriteBatch.Draw(
                    slopeFloorRightOutlineCornerAbove,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (TileUtil.TopRightCorner(here) && above != TileUtil.SHAPE_SLOPE_FLOOR_RIGHT && right == TileUtil.SHAPE_SLOPE_FLOOR_RIGHT && (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
            {
                Velo.SpriteBatch.Draw(
                    slopeFloorRightOutlineCornerRight,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (TileUtil.TopLeftCorner(here) && above == TileUtil.SHAPE_SLOPE_FLOOR_LEFT && left == TileUtil.SHAPE_SLOPE_FLOOR_LEFT && (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
            {
                Velo.SpriteBatch.Draw(
                    slopeFloorLeftOutlineCornerBoth,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (TileUtil.TopLeftCorner(here) && above == TileUtil.SHAPE_SLOPE_FLOOR_LEFT && left != TileUtil.SHAPE_SLOPE_FLOOR_LEFT && (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
            {
                Velo.SpriteBatch.Draw(
                    slopeFloorLeftOutlineCornerAbove,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            if (TileUtil.TopLeftCorner(here) && above != TileUtil.SHAPE_SLOPE_FLOOR_LEFT && left == TileUtil.SHAPE_SLOPE_FLOOR_LEFT && (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
            {
                Velo.SpriteBatch.Draw(
                    slopeFloorLeftOutlineCornerLeft,
                    new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                    Color.White);
            }
            int corner_width = here == TileUtil.SHAPE_SQUARE ? outlineWidth : Math.Min(outlineWidth, 8);
            if (TileUtil.TopLeftCorner(here) && TileUtil.TopRightCorner(left) && TileUtil.BottomLeftCorner(above) && !TileUtil.BottomRightCorner(aboveLeft))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X, (int)pos.Y, corner_width, corner_width),
                    outlineCol);
            }
            if (TileUtil.BottomLeftCorner(here) && TileUtil.BottomRightCorner(left) && TileUtil.TopLeftCorner(below) && !TileUtil.TopRightCorner(belowLeft))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X, (int)pos.Y + 16 - corner_width, corner_width, corner_width),
                    outlineCol);
            }
            if (TileUtil.TopRightCorner(here) && TileUtil.TopLeftCorner(right) && TileUtil.BottomRightCorner(above) && !TileUtil.BottomLeftCorner(aboveRight))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X + 16 - corner_width, (int)pos.Y, corner_width, corner_width),
                    outlineCol);
            }
            if (TileUtil.BottomRightCorner(here) && TileUtil.BottomLeftCorner(right) && TileUtil.TopRightCorner(below) && !TileUtil.TopLeftCorner(belowRight))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X + 16 - corner_width, (int)pos.Y + 16 - corner_width, corner_width, corner_width),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SLOPE_CEIL_RIGHT && !TileUtil.BottomEdge(above))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X, (int)pos.Y, 16 - outlineWidth, outlineWidth),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SLOPE_CEIL_RIGHT && !TileUtil.RightEdge(left))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X, (int)pos.Y, outlineWidth, 16 - outlineWidth),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SLOPE_CEIL_LEFT && !TileUtil.BottomEdge(above))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X + outlineWidth, (int)pos.Y, 16 - outlineWidth, outlineWidth),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SLOPE_CEIL_LEFT && !TileUtil.LeftEdge(right))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X + 16 - outlineWidth, (int)pos.Y, outlineWidth, 16 - outlineWidth),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SLOPE_FLOOR_RIGHT && !TileUtil.TopEdge(below))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X, (int)pos.Y + 16 - outlineWidth, 16 - outlineWidth, outlineWidth),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SLOPE_FLOOR_RIGHT && !TileUtil.RightEdge(left))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X, (int)pos.Y + outlineWidth, outlineWidth, 16 - outlineWidth),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SLOPE_FLOOR_LEFT && !TileUtil.TopEdge(below))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X + outlineWidth, (int)pos.Y + 16 - outlineWidth, 16 - outlineWidth, outlineWidth),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SLOPE_FLOOR_LEFT && !TileUtil.LeftEdge(right))
            {
                Velo.SpriteBatch.Draw(
                    pixel,
                    new Rectangle((int)pos.X + 16 - outlineWidth, (int)pos.Y + outlineWidth, outlineWidth, 16 - outlineWidth),
                    outlineCol);
            }
            if (here == TileUtil.SHAPE_SQUARE && belowRight == TileUtil.SHAPE_SLOPE_CEIL_RIGHT)
            {
                Velo.SpriteBatch.Draw(
                   slopeCeilRightOutlineCornerSmall,
                   new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                   Color.White);
            }
            if (here == TileUtil.SHAPE_SQUARE && belowLeft == TileUtil.SHAPE_SLOPE_CEIL_LEFT)
            {
                Velo.SpriteBatch.Draw(
                   slopeCeilLeftOutlineCornerSmall,
                   new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                   Color.White);
            }
            if (here == TileUtil.SHAPE_SQUARE && aboveRight == TileUtil.SHAPE_SLOPE_FLOOR_RIGHT)
            {
                Velo.SpriteBatch.Draw(
                   slopeFloorRightOutlineCornerSmall,
                   new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                   Color.White);
            }
            if (here == TileUtil.SHAPE_SQUARE && aboveLeft == TileUtil.SHAPE_SLOPE_FLOOR_LEFT)
            {
                Velo.SpriteBatch.Draw(
                   slopeFloorLeftOutlineCornerSmall,
                   new Rectangle((int)pos.X, (int)pos.Y, 16, 16),
                   Color.White);
            }

            return true;
        }
    }
}
