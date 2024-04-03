using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using CEngine.Content;
using CEngine.World.Collision;
using CEngine.Graphics.Camera;
using CEngine.World.Actor;
using CEngine.Graphics.Layer;
using Lidgren.Network;
using System.Windows.Forms;
using System.IO;

namespace Velo
{
    public class Velo
    {
        public static SpriteBatch SpriteBatch;
        public static CContentBundleManager ContentManager;
        public static Player MainPlayer = null;
        public static bool Ingame = false;
        public static bool Online = false;

        public static Player GetMainPlayer()
        {
            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;

            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                ICActorController controller = actor.controller;
                if (controller is Player && ((Player)controller).actor.localPlayer)
                {
                    return (Player)controller;
                }
            }

            return null;
        }

        public static bool IsOnline()
        {
            if (Main.game.stack.gameInfo == null)
                return false;

            foreach (var module in Main.game.stack.modules)
                if (module is ModuleSolo)
                    return false;

            Slot[] slots = Main.game.stack.gameInfo.slots;
            foreach (Slot slot in slots)
            {
                if (slot.Player != null && !slot.LocalPlayer)
                    return true;
            }

            return false;
        }

        // It follows a list of interface methods that the modded game client
        // can call in order to communicate with Velo.
        // These are all written in snake_case.

        // called once after the game's engine has finished setup
        public static void init()
        {
            SpriteBatch = CEngine.CEngine.Instance.SpriteBatch;
            ContentManager = CEngine.CEngine.Instance.ContentBundleManager;

            foreach (Type t in typeof(Module).Assembly.GetTypes())
            {
                if (typeof(Module).IsAssignableFrom(t)&& !t.IsAbstract)
                {
                    Module m = (Module)t.GetField("Instance").GetValue(null);
                    m.Init();
                }
            }

            Keyboard.Init();
        }

        // replaces the game's update call
        public static void game_update(GameTime gameTime)
        {
            // cache a few commonly needed values
            MainPlayer = GetMainPlayer();
            Ingame = MainPlayer != null;
            Online = IsOnline();

            if (Keyboard.Pressed[(ushort)Keys.F1])
            {
                string json = ModuleManager.Instance.ToJson(false).ToString(0);
                File.WriteAllText("test.txt", json);
            }

            if (!Util.IsFocused()) // we don't want inputs to be detected when game is unfocused
            {
                Keyboard.Held.Fill(false);
                Keyboard.Pressed.Fill(false);
            }
            
            foreach (Module module in ModuleManager.Instance.Modules)
                module.PreUpdate();

            Main.game.GameUpdate(gameTime); // the game's usual update procedure

            foreach (Module module in ModuleManager.Instance.Modules)
                module.PostUpdate();

            if (
                !(Performance.Instance.Enabled.Value.Enabled &&
                Performance.Instance.LimitFramerateAfterRender.Value) ||
                Util.IsMinimized() // call when minimized to prevent crashing
                )
            {
                Main.game.Delay(); // limits framerate
            }
        }

        public static void game_draw(GameTime gameTime)
        {
            foreach (Module module in ModuleManager.Instance.Modules)
                module.PreRender();

            Main.game.GameDraw(gameTime);

            foreach (Module module in ModuleManager.Instance.Modules)
                module.PostRender();

            if (
                Performance.Instance.Enabled.Value.Enabled &&
                Performance.Instance.LimitFramerateAfterRender.Value
                )
            {
                Main.game.Delay(); // limits framerate
            }

            Keyboard.Pressed.Fill(false);
        }

        // called in Main.game.Delay()
        // returns the targetted frame period in ticks
        public static long get_framelimit(long current)
        {
            int framelimit = LocalGameModifications.Instance.Framelimit.Value;
            if (framelimit == -1)
                return current;

            if (Online)
                return 10000000L / Math.Min(Math.Max(framelimit, 30), 300);

            return 10000000L / framelimit;
        }

        // called in CEngine's update method
        public static float get_time_scale()
        {
            if (!Online && Ingame)
            {
                if (Savestates.Instance.savestateLoadTime == 0 || Savestates.Instance.LoadHaltDuration.Value == 0)
                    return LocalGameModifications.Instance.TimeScale.Value;

                long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                float ratio = (float)Math.Min(milliseconds - Savestates.Instance.savestateLoadTime, Savestates.Instance.LoadHaltDuration.Value) / Savestates.Instance.savestateLoadTime;

                return LocalGameModifications.Instance.TimeScale.Value * ratio;
            }

            return 1.0f;
        }

        public static float get_camera_max_speed(ICCameraModifier camera)
        {
            if (Online)
                return LocalGameModifications.Instance.CameraMaxSpeed.DefaultValue;

            return LocalGameModifications.Instance.CameraMaxSpeed.Value;
        }

        public static float get_max_speed()
        {
            if (Online)
                return LocalGameModifications.Instance.MaxSpeed.DefaultValue;

            return LocalGameModifications.Instance.MaxSpeed.Value;
        }

        public static Vector2 get_gravity()
        {
            if (Online)
                return LocalGameModifications.Instance.Gravity.DefaultValue;

            return LocalGameModifications.Instance.Gravity.Value;
        }

        public static float get_grapple_hook_speed()
        {
            if (Online)
                return LocalGameModifications.Instance.GrappleHookSpeed.DefaultValue;

            return LocalGameModifications.Instance.GrappleHookSpeed.Value;
        }

        public static float get_grapple_cooldown()
        {
            if (Online)
                return LocalGameModifications.Instance.GrappleCooldown.DefaultValue;

            return LocalGameModifications.Instance.GrappleCooldown.Value;
        }

        public static float get_slide_cooldown()
        {
            if (Online)
                return LocalGameModifications.Instance.SlideCooldown.DefaultValue;

            return LocalGameModifications.Instance.SlideCooldown.Value;
        }

        public static bool get_fix_grapple_glitches()
        {
            if (Online)
                return LocalGameModifications.Instance.FixGrappleGlitches.DefaultValue.Enabled;

            return LocalGameModifications.Instance.FixGrappleGlitches.Value.Enabled;
        }

        public static bool get_enable_old_moonwalk()
        {
            if (Online)
                return LocalGameModifications.Instance.EnableOldMoonwalk.DefaultValue;

            return LocalGameModifications.Instance.EnableOldMoonwalk.Value;
        }

        // called in CEngine
        public static bool dt_fixed()
        {
            return TAS.Instance.dtFixed;
        }

        // called in CEngine
        public static long delta()
        {
            return TAS.Instance.delta;
        }

        // called in CEngine
        public static void set_delta(long delta)
        {
            TAS.Instance.delta = delta;
        }

        // called in CEngine
        public static long time()
        {
            return TAS.Instance.time;
        }

        // called in CEngine
        public static void set_time(long time)
        {
            TAS.Instance.time = time;
        }

        public static void update_camera(ICCameraModifier cameraMod)
        {
            LocalGameModifications.Instance.UpdateCamera(cameraMod);
        }

        public static void update_cam_pos(ICCameraModifier cameraMod)
        {
            BlindrunSimulator.Instance.Update(cameraMod);
        }

        public static int event_id()
        {
            return EventBypass.Instance.EventId.Value;
        }

        public static bool bypass_pumpkin_cosmo()
        {
            return EventBypass.Instance.BypassPumpkinCosmo.Value;
        }

        public static bool bypass_xl()
        {
            return EventBypass.Instance.BypassXl.Value;
        }

        public static bool bypass_excel()
        {
            return EventBypass.Instance.BypassExcel.Value;
        }

        public static bool reloading_contents()
        {
            return HotReload.Instance.contentsReloaded;
        }

        public static void update_popup(Player player)
        {
            ColorsAndAppearance.Instance.UpdatePopup(player);
        }

        public static float popup_opacity()
        {
            return ColorsAndAppearance.Instance.PopupOpacity.Value;
        }

        public static float popup_scale()
        {
            return ColorsAndAppearance.Instance.PopupScale.Value;
        }

        public static void update_grapple_color(Grapple grapple)
        {
            ColorsAndAppearance.Instance.UpdateGrappleColor(grapple);
        }

        public static void update_golden_hook_color(GoldenHook goldenHook)
        {
            ColorsAndAppearance.Instance.UpdateGoldenHookColor(goldenHook);
        }

        public static void update_rope_color(Rope rope)
        {
            ColorsAndAppearance.Instance.UpdateRopeColor(rope);
        }

        public static Color get_popup_color()
        {
            return ColorsAndAppearance.Instance.PopupColor.Value.Get();
        }

        public static Color get_player_color()
        {
            return ColorsAndAppearance.Instance.PlayerColor.Value.Get();
        }

        public static Color get_win_star_color()
        {
            return ColorsAndAppearance.Instance.WinStarColor.Value.Get();
        }

        public static Color get_bubble_color()
        {
            return ColorsAndAppearance.Instance.BubbleColor.Value.Get();
        }

        public static Color get_saw_color()
        {
            return ColorsAndAppearance.Instance.SawColor.Value.Get();
        }

        public static Color get_laser_lethal_inner_color()
        {
            return ColorsAndAppearance.Instance.LaserLethalInnerColor.Value.Get();
        }

        public static Color get_laser_lethal_outer_color()
        {
            return ColorsAndAppearance.Instance.LaserLethalOuterColor.Value.Get();
        }

        public static Color get_laser_lethal_particle_color()
        {
            return ColorsAndAppearance.Instance.LaserLethalParticleColor.Value.Get();
        }

        public static Color get_laser_lethal_smoke_color()
        {
            return ColorsAndAppearance.Instance.LaserLethalSmokeColor.Value.Get();
        }

        public static Color get_laser_non_lethal_inner_color()
        {
            return ColorsAndAppearance.Instance.LaserNonLethalInnerColor.Value.Get();
        }

        public static Color get_laser_non_lethal_outer_color()
        {
            return ColorsAndAppearance.Instance.LaserNonLethalOuterColor.Value.Get();
        }

        public static Color get_laser_non_lethal_particle_color()
        {
            return ColorsAndAppearance.Instance.LaserNonLethalParticleColor.Value.Get();
        }

        public static Color get_laser_non_lethal_smoke_color()
        {
            return ColorsAndAppearance.Instance.LaserNonLethalSmokeColor.Value.Get();
        }

        public static Color get_tile_map_color(ICLayer layer)
        {
            if (layer.Id != "Collision")
                return Color.White;
            return TileMap.Instance.ColorMultiplier.Value.Get();
        }

        public static bool draw_chunk(CBufferedTileMapLayer tilemap, Vector2 pos, int x, int y)
        {
            return TileMap.Instance.Draw(tilemap, pos, x, y);
        }

        public static void text_color_updated(CTextDrawComponent text)
        {
            ColorsAndAppearance.Instance.TextColorUpdated(text);
        }

        public static void text_shadow_color_updated(CTextDrawComponent text)
        {
            ColorsAndAppearance.Instance.TextShadowColorUpdated(text);
        }

        public static void image_color_updated(CImageDrawComponent image)
        {
            ColorsAndAppearance.Instance.ImageColorUpdated(image);
        }

        public static void sprite_color_updated(CSpriteDrawComponent sprite)
        {
            ColorsAndAppearance.Instance.SpriteColorUpdated(sprite);
        }

        public static void update_text_color(CTextDrawComponent text)
        {
            ColorsAndAppearance.Instance.UpdateTextColor(text);
        }

        public static void update_image_color(CImageDrawComponent image)
        {
            ColorsAndAppearance.Instance.UpdateImageColor(image);
        }

        public static void update_sprite_color(CSpriteDrawComponent sprite)
        {
            ColorsAndAppearance.Instance.UpdateSpriteColor(sprite);
        }

        public static void add_chat_comp(object obj, string type)
        {
            ColorsAndAppearance.Instance.AddChatComp(obj, type);
        }

        public static void update_chat_color(object obj)
        {
            ColorsAndAppearance.Instance.UpdateChatColor(obj);
        }

        public static bool disable_bubbles()
        {
            return
                Performance.Instance.Enabled.Value.Enabled &&
                Performance.Instance.DisableBubbles.Value;
        }

        public static bool disable_steam_input_api()
        {
            return
                Performance.Instance.Enabled.Value.Enabled &&
                Performance.Instance.DisableSteamInputApi.Value;
        }

        public static bool skip_input(int controller_id)
        {
            return
                Performance.Instance.Enabled.Value.Enabled &&
                Performance.Instance.EnableControllerId.Value != -1 &&
                Performance.Instance.EnableControllerId.Value != controller_id;
        }

        public class Message
        {
            public NetIncomingMessage message;
            public Steamworks.CSteamID identifier;

            public Message(NetIncomingMessage message, Steamworks.CSteamID identifier)
            {
                this.message = message;
                this.identifier = identifier;
            }
        }

        public static Message poll_packet(MessagePools pools, int channel)
        {
            return Performance.Instance.PollPacket(pools, channel);
        }

        public static void recycle_packet(NetIncomingMessage message, MessagePools pools)
        {
            Performance.Instance.RecyclePacket(message, pools);
        }
    }
}
