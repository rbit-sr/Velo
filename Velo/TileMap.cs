using System;
using Microsoft.Xna.Framework;
using CEngine.Graphics.Layer;
using Microsoft.Xna.Framework.Graphics;

namespace Velo
{
    public struct Vector2i
    {
        public int X, Y;

        public Vector2i(int x, int y)
        {
            X = x; 
            Y = y;
        }

        public Vector2i(Vector2 vec)
        {
            X = (int)vec.X;
            Y = (int)vec.Y;
        }

        public static Rectangle operator +(Rectangle rect, Vector2i vec)
        {
            return new Rectangle(rect.X + vec.X, rect.Y + vec.Y, rect.Width, rect.Height);
        }
    }

    public static class TileUtil
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
        public const int SHAPE_SLOPE_FLOOR_RIGHT = 3;
        public const int SHAPE_SLOPE_FLOOR_LEFT = 4;
        public const int SHAPE_SLOPE_CEIL_LEFT = 5;

        public static int GetShape(this int tile)
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

        public static int RotateCCW(this int shape, int rotation)
        {
            if (shape == SHAPE_AIR || shape == SHAPE_SQUARE)
                return shape;
            shape += rotation;
            while (shape > SHAPE_SLOPE_CEIL_LEFT)
                shape -= 4;
            return shape;
        }

        public static Rectangle RotateCCW(this Rectangle rect, int rotation)
        {
            if (rotation > 3 || rotation < 0)
                rotation %= 4;

            switch (rotation)
            {
                case 0:
                    return rect;
                case 1:
                    return new Rectangle(rect.Top, 16 - rect.Right, rect.Height, rect.Width);
                case 2:
                    return new Rectangle(16 - rect.Right, 16 - rect.Bottom, rect.Width, rect.Height);
                case 3:
                    return new Rectangle(16 - rect.Bottom, rect.Left, rect.Height, rect.Width);
                default:
                    return default(Rectangle);
            }
        }

        private static void RotateRight<T>(ref T t1, ref T t2, ref T t3, ref T t4)
        {
            T temp = t4;
            t4 = t3;
            t3 = t2;
            t2 = t1;
            t1 = temp;
        }

        public static void RotateCCW1(this Color[] colors)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    RotateRight(
                        ref colors[x + 16 * y],
                        ref colors[y + 16 * (15 - x)],
                        ref colors[15 - x + 16 * (15 - y)],
                        ref colors[15 - y + 16 * x]
                        );
                }
            }
        }

        public static void Overdraw(this Color[] dst, Color[] src1, Color[] src2)
        {
            for (int i = 0; i < 16 * 16; i++)
            {
                if (src2[i] == Color.Transparent)
                    dst[i] = src1[i];
                else
                    dst[i] = src2[i];
            }
        }

        public static bool Edge(this int shape, int orientation)
        {
            shape = shape.RotateCCW(4 - orientation);

            return 
                shape == SHAPE_SQUARE || shape == SHAPE_SLOPE_CEIL_RIGHT || shape == SHAPE_SLOPE_CEIL_LEFT;
        }

        public static bool Corner(this int shape, int orientation)
        {
            shape = shape.RotateCCW(4 - orientation);

            return
                shape == SHAPE_SQUARE || shape == SHAPE_SLOPE_CEIL_RIGHT;
        }

        public static bool Slope(this int shape, int orientation)
        {
            shape = shape.RotateCCW(4 - orientation);

            return
                shape == SHAPE_SLOPE_CEIL_RIGHT;
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

    public enum ELineStyle
    {
        SOLID, DASHED, DOTTED
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
        public EnumSetting<ELineStyle> GrappleCeilingStyle;
        public ColorSetting WallColor;
        public IntSetting WallWidth;
        public EnumSetting<ELineStyle> WallStyle;
        public BoolSetting StairsReplaceWithSlopes;
        public BoolSetting RemoveCheckered;
        public BoolSetting SlopeAntiAliasing;

        public static string[] LineStyleLabels = new[] { "solid", "dashed", "dotted" };

        private Texture2D pixel;
        private readonly Texture2D[] tiles = new Texture2D[16];
        private readonly Texture2D[] slopeOutline = new Texture2D[4];
        private readonly Texture2D[] slopeOutlineCornerBoth = new Texture2D[4];
        private readonly Texture2D[] slopeOutlineCornerCCW = new Texture2D[4];
        private readonly Texture2D[] slopeOutlineCornerCW = new Texture2D[4];
        private readonly Texture2D[] slopeOutlineCornerSmall = new Texture2D[4];

        private bool texturesSetUp = false;

        private TileMap() : base("Tile Map")
        {
            NewCategory("general");
            ColorMultiplier = AddColorTransition("color multiplier", new ColorTransition(Color.White));

            ColorMultiplier.Tooltip =
                "color multiplier for the tile map";

            NewCategory("outline");
            OutlineWidth = AddInt("width", 0, 0, 16);
            OutlineColor = AddColor("color", Color.White);
            SlopeOutlineColor = AddColor("slope color", Color.White);

            CurrentCategory.Tooltip =
                "Renders an outline around the tile map";

            NewCategory("Velo custom tiles");
            EnableVeloCustomTiles = AddBool("enable", false);
            FillColor = AddColor("fill color", Color.Black);
            GrappleCeilingColor = AddColor("grapple ceil color", Color.White);
            GrappleCeilingWidth = AddInt("grapple ceil width", 3, 0, 16);
            GrappleCeilingStyle = AddEnum("grapple ceil style", ELineStyle.SOLID, LineStyleLabels);
            WallColor = AddColor("wall color", Color.White);
            WallWidth = AddInt("wall width", 3, 0, 16);
            WallStyle = AddEnum("wall style", ELineStyle.DOTTED, LineStyleLabels);
            StairsReplaceWithSlopes = AddBool("stairs replace with slopes", false);
            RemoveCheckered = AddBool("remove checkered", false);
            SlopeAntiAliasing = AddBool("slope anti aliasing", false);

            CurrentCategory.Tooltip =
                "Allows for quickly editing tiles.";
            EnableVeloCustomTiles.Tooltip =
                "Enables Velo custom tiles. This overshadows \"tiles_black_editor.xnb\"";
            StairsReplaceWithSlopes.Tooltip =
                "Replaces all stair tiles with regular slope tiles.";
            SlopeAntiAliasing.Tooltip =
                "Adds anti-aliasing to slope sprites. Can make them look a bit blurry." +
                "Does not work for outlines.";
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
                SetUpTextures();
                ((CBufferedTileMapLayer)layer).refresh();
            }
        }

        private void SetUpTextures()
        {
            void Dispose(Texture2D texture) => texture.Dispose();

            pixel.NullCond(Dispose);
            for (int i = 1; i < 16; i++)
            {
                tiles[i].NullCond(Dispose);
                tiles[i] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            }
            for (int i = 0; i < 4; i++)
            {
                slopeOutline[i].NullCond(Dispose);
                slopeOutline[i] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            }
            for (int i = 0; i < 4; i++)
            {
                slopeOutlineCornerBoth[i].NullCond(Dispose);
                slopeOutlineCornerBoth[i] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            }
            for (int i = 0; i < 4; i++)
            {
                slopeOutlineCornerCCW[i].NullCond(Dispose);
                slopeOutlineCornerCCW[i] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            }
            for (int i = 0; i < 4; i++)
            {
                slopeOutlineCornerCW[i].NullCond(Dispose);
                slopeOutlineCornerCW[i] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            }
            for (int i = 0; i < 4; i++)
            {
                slopeOutlineCornerSmall[i].NullCond(Dispose);
                slopeOutlineCornerSmall[i] = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 16, 16);
            }

            int outline_width = OutlineWidth.Value;
            int slopeOutline_width = (int)(outline_width * Math.Sqrt(2.0) + 0.5f);

            Color fillColor = Util.ApplyAlpha(FillColor.Value);
            Color wallColor = Util.ApplyAlpha(WallColor.Value);
            Color grappleCeilColor = Util.ApplyAlpha(GrappleCeilingColor.Value);

            pixel = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, 1, 1);
            pixel.SetData(new Color[]
                {
                Color.White
                }, 0, 1);

            Color[] data = new Color[16 * 16];
            Color[] checkered = new Color[16 * 16];
            Color[] slope = new Color[16 * 16];

            // square
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    data[i] = fillColor;
                    i++;
                }
            }
            tiles[TileUtil.TILE_SQUARE].SetData(data, 0, 16 * 16);

            // wall right
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x >= 16 - WallWidth.Value && TileUtil.FromLineStyle(WallStyle.Value, y))
                        data[i] = wallColor;
                    else
                        data[i] = fillColor;
                    i++;
                }
            }
            tiles[TileUtil.TILE_WALL_RIGHT].SetData(data, 0, 16 * 16);

            // wall left
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x < WallWidth.Value && TileUtil.FromLineStyle(WallStyle.Value, y))
                        data[i] = wallColor;
                    else
                        data[i] = fillColor;
                    i++;
                }
            }
            tiles[TileUtil.TILE_WALL_LEFT].SetData(data, 0, 16 * 16);

            // grapple ceiling
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (y >= 16 - GrappleCeilingWidth.Value && TileUtil.FromLineStyle((ELineStyle)GrappleCeilingStyle.Value, x))
                        data[i] = grappleCeilColor;
                    else
                        data[i] = fillColor;
                    i++;
                }
            }
            tiles[TileUtil.TILE_GRAPPLE_CEIL].SetData(data, 0, 16 * 16);

            // checkered
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if ((x / 4 % 2 == 0) == (y / 4 % 2 == 0))
                        checkered[i] = fillColor;
                    else
                        checkered[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_CHECKERED].SetData(checkered, 0, 16 * 16);

            // slope ceiling right
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x <= 15 - y)
                    {
                        slope[i] = fillColor;
                        if (SlopeAntiAliasing.Value && x == 15 - y)
                            slope[i] *= 0.5f;
                    }
                    else
                        slope[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_SLOPE_CEIL_RIGHT].SetData(slope, 0, 16 * 16);

            slope.RotateCCW1();
            tiles[TileUtil.TILE_SLOPE_FLOOR_RIGHT].SetData(slope, 0, 16 * 16);

            slope.RotateCCW1();
            tiles[TileUtil.TILE_SLOPE_FLOOR_LEFT].SetData(slope, 0, 16 * 16);

            slope.RotateCCW1();
            tiles[TileUtil.TILE_SLOPE_CEIL_LEFT].SetData(slope, 0, 16 * 16);

            slope.RotateCCW1();

            int[] stairs_profile = { 8, 8, 9, 9, 9, 9, 9, 9, 0, 0, 1, 1, 1, 1, 1, 1 };

            // stairs right
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (16 - x > stairs_profile[y])

                        data[i] = fillColor;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_STAIRS_RIGHT].SetData(data, 0, 16 * 16);

            // stairs left
            for (int y = 0, i = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x >= stairs_profile[y])
                        data[i] = fillColor;
                    else
                        data[i] = Color.Transparent;
                    i++;
                }
            }
            tiles[TileUtil.TILE_STAIRS_LEFT].SetData(data, 0, 16 * 16);

            data.Overdraw(slope, checkered);
            tiles[TileUtil.TILE_CHECKERED_SLOPE_CEIL_RIGHT].SetData(data, 0, 16 * 16);

            slope.RotateCCW1();
            data.Overdraw(slope, checkered);
            tiles[TileUtil.TILE_CHECKERED_SLOPE_FLOOR_RIGHT].SetData(data, 0, 16 * 16);

            slope.RotateCCW1();
            data.Overdraw(slope, checkered);
            tiles[TileUtil.TILE_CHECKERED_SLOPE_FLOOR_LEFT].SetData(data, 0, 16 * 16);

            slope.RotateCCW1();
            data.Overdraw(slope, checkered);
            tiles[TileUtil.TILE_CHECKERED_SLOPE_CEIL_LEFT].SetData(data, 0, 16 * 16);

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
            slopeOutline[0].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutline[1].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutline[2].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutline[3].SetData(data, 0, 16 * 16);

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
            slopeOutlineCornerBoth[0].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutlineCornerBoth[1].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutlineCornerBoth[2].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutlineCornerBoth[3].SetData(data, 0, 16 * 16);

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
            slopeOutlineCornerCCW[0].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutlineCornerCCW[1].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutlineCornerCCW[2].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutlineCornerCCW[3].SetData(data, 0, 16 * 16);

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
            slopeOutlineCornerCW[0].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutlineCornerCW[1].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutlineCornerCW[2].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutlineCornerCW[3].SetData(data, 0, 16 * 16);

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
            slopeOutlineCornerSmall[0].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutlineCornerSmall[1].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutlineCornerSmall[2].SetData(data, 0, 16 * 16);

            data.RotateCCW1();
            slopeOutlineCornerSmall[3].SetData(data, 0, 16 * 16);

            texturesSetUp = true;
        }

        public bool Draw(CBufferedTileMapLayer tilemap, Vector2 pos, int x, int y)
        {
            if (!EnableVeloCustomTiles.Value && OutlineWidth.Value == 0 || tilemap.Id != "Collision")
                return false;

            if (!texturesSetUp)
                return false;

            Vector2i posi = new Vector2i(pos);
            
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
            int[] neigh = new int[4];
            int[] diagNeigh = new int[4];
            neigh[0] = TileUtil.GetShape(tilemap, x, y - 1);
            neigh[1] = TileUtil.GetShape(tilemap, x - 1, y);
            neigh[2] = TileUtil.GetShape(tilemap, x, y + 1);
            neigh[3] = TileUtil.GetShape(tilemap, x + 1, y);
            diagNeigh[0] = TileUtil.GetShape(tilemap, x - 1, y - 1);
            diagNeigh[1] = TileUtil.GetShape(tilemap, x - 1, y + 1);
            diagNeigh[2] = TileUtil.GetShape(tilemap, x + 1, y + 1);
            diagNeigh[3] = TileUtil.GetShape(tilemap, x + 1, y - 1);

            if (here == TileUtil.SHAPE_AIR)
                return true;

            if (outlineWidth >= 16 && (tileId == TileUtil.TILE_GRAPPLE_CEIL || tileId == TileUtil.TILE_WALL_LEFT || tileId == TileUtil.TILE_WALL_RIGHT))
                return true;

            int cornerWidth = here == TileUtil.SHAPE_SQUARE ? outlineWidth : Math.Min(outlineWidth, 8);
            for (int i = 0; i < 4; i++)
            {
                if (
                    here == TileUtil.SHAPE_SQUARE && !TileUtil.Edge(neigh[i], i + 2) &&
                    (i != 2 || tileId != TileUtil.TILE_GRAPPLE_CEIL) &&
                    (i != 1 || tileId != TileUtil.TILE_WALL_LEFT) &&
                    (i != 3 || tileId != TileUtil.TILE_WALL_RIGHT))
                {
                    Velo.SpriteBatch.Draw(
                        pixel,
                        new Rectangle(0, 0, 16, outlineWidth).RotateCCW(i) + posi,
                        outlineCol);
                }

                if (
                    here.Corner(i) && !diagNeigh[i].Corner(i + 2) &&
                    neigh[i].Corner(i + 1) && neigh[(i + 1) % 4].Corner(i + 3))
                {
                    Velo.SpriteBatch.Draw(
                        pixel,
                        new Rectangle(0, 0, cornerWidth, outlineWidth).RotateCCW(i) + posi,
                        outlineCol);
                }

                if (here.Slope(i) && !neigh[i].Edge(i + 2))
                {
                    Velo.SpriteBatch.Draw(
                        pixel,
                        new Rectangle(0, 0, 16 - cornerWidth, outlineWidth).RotateCCW(i) + posi,
                        outlineCol);
                }

                if (here.Slope(i) && !neigh[(i + 1) % 4].Edge(i + 3))
                {
                    Velo.SpriteBatch.Draw(
                        pixel,
                        new Rectangle(0, 0, outlineWidth, 16 - cornerWidth).RotateCCW(i) + posi,
                        outlineCol);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                if (here.Slope(i))
                {
                    Velo.SpriteBatch.Draw(
                        slopeOutline[i],
                        new Rectangle(0, 0, 16, 16) + posi,
                        Color.White);
                }

                if (
                    here.Corner(i + 2) && 
                    neigh[(i + 2) % 4].Slope(i) && neigh[(i + 3) % 4].Slope(i) &&
                    (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
                {
                    Velo.SpriteBatch.Draw(
                        slopeOutlineCornerBoth[i],
                        new Rectangle(0, 0, 16, 16) + posi,
                        Color.White);
                }

                if (
                    here.Corner(i + 2) &&
                    neigh[(i + 2) % 4].Slope(i) && !neigh[(i + 3) % 4].Slope(i) &&
                    (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
                {
                    Velo.SpriteBatch.Draw(
                        slopeOutlineCornerCCW[i],
                        new Rectangle(0, 0, 16, 16) + posi,
                        Color.White);
                }

                if (
                    here.Corner(i + 2) &&
                    !neigh[(i + 2) % 4].Slope(i) && neigh[(i + 3) % 4].Slope(i) &&
                    (here == TileUtil.SHAPE_SQUARE || outlineWidth <= 12))
                {
                    Velo.SpriteBatch.Draw(
                        slopeOutlineCornerCW[i],
                        new Rectangle(0, 0, 16, 16) + posi,
                        Color.White);
                }

                if (here == TileUtil.SHAPE_SQUARE && diagNeigh[(i + 2) % 4].Slope(i))
                {
                    Velo.SpriteBatch.Draw(
                        slopeOutlineCornerSmall[i],
                        new Rectangle(0, 0, 16, 16) + posi,
                        Color.White);
                }
            }

            return true;
        }
    }
}
