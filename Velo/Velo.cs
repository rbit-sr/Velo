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
using SDL2;
using System.Collections.Generic;

namespace Velo
{
    public class Velo
    {
        public static SpriteBatch SpriteBatch;
        public static CContentBundleManager ContentManager;
        public static Player MainPlayer = null;
        public static Player Ghost = null;
        public static ModuleSolo ModuleSolo = null;
        public static bool Ingame = false;
        public static bool IngamePrev = false;
        public static bool Online = false;
        public static float TimerPrev = 0.0f;
        public static float Timer = 0.0f;
        public static bool BoostaCokeModified = false;
        public static bool GhostLaserCollision = false;

        public static List<Action> OnMainPlayerReset = new List<Action>();
        public static List<Action> OnLapFinish = new List<Action>();

        private static readonly List<Action> onPreUpdate = new List<Action>();
        private static readonly List<Action> onPostUpdate = new List<Action>();
        private static readonly List<Action> onPreRender = new List<Action>();
        private static readonly List<Action> onPostRender = new List<Action>();

        // thread-safe
        private static readonly List<Action> onPreUpdateTS = new List<Action>();
        private static readonly List<Action> onPostUpdateTS = new List<Action>();
        private static readonly List<Action> onPreRenderTS = new List<Action>();
        private static readonly List<Action> onPostRenderTS = new List<Action>();

        public static Player GetMainPlayer()
        {
            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;

            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                ICActorController controller = actor.controller;
                if (controller is Player)
                {
                    Player player = controller as Player;
                    if (player.actor.localPlayer)
                        return player;
                }
            }

            return null;
        }

        public static Player GetGhost()
        {
            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;

            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                ICActorController controller = actor.controller;
                if (controller is Player)
                {
                    Player player = controller as Player;
                    if (!player.actor.localPlayer)
                        return player;
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

        public static ModuleSolo GetModuleSolo()
        {
            if (Main.game.stack.gameInfo == null)
                return null;

            foreach (var module in Main.game.stack.modules)
                if (module is ModuleSolo)
                    return module as ModuleSolo;

            return default(ModuleSolo);
        }

#if !VELO_OLD
#pragma warning disable IDE1006
#pragma warning disable IDE0060
#endif
        // It follows a list of interface methods that the modded game client
        // can call in order to communicate with Velo.
        // These are all written in snake_case.

        // called once after the game's engine has finished setup
        public static void init()
        {
            SpriteBatch = CEngine.CEngine.Instance.SpriteBatch;
            ContentManager = CEngine.CEngine.Instance.ContentBundleManager;

            ModuleManager.Instance.Init();
            Storage.Instance.Load();
            SettingsUI.Instance.SendUpdates = false;
            SettingsUI.Instance.Enabled.Disable();
            SettingsUI.Instance.SendUpdates = true;
            Keyboard.Init();
        }

        // replaces the game's update call
        public static void game_update(GameTime gameTime)
        {
            if (!Util.IsFocused()) // we don't want inputs to be detected when game is unfocused
            {
                Keyboard.Held.Fill(false);
                Keyboard.Pressed.Fill(false);
            }

            foreach (Action action in onPreUpdate)
                action();
            onPreUpdate.Clear();
            lock (onPreUpdateTS)
            {
                foreach (Action action in onPreUpdateTS)
                    action();
                onPreUpdateTS.Clear();
            }
            ModuleManager.Instance.PreUpdate();
            BoostaCokeModified = false;
            GhostLaserCollision = false;

            Main.game.GameUpdate(gameTime); // the game's usual update procedure
           
            // cache a few commonly needed values
            MainPlayer = GetMainPlayer();
            Ghost = GetGhost();
            ModuleSolo = GetModuleSolo();
            IngamePrev = Ingame;
            Ingame = MainPlayer != null;
            Online = IsOnline();
            TimerPrev = Timer;
            if (Ingame)
            {
                ICActorController timerAct = CEngine.CEngine.Instance.World.CollisionEngine.FindActorOfType(typeof(Timer));
                if (timerAct != null)
                    Timer = (timerAct as Timer).current;
            }

            foreach (Action action in onPostUpdate)
                action();
            onPostUpdate.Clear();
            lock (onPostUpdateTS)
            {
                foreach (Action action in onPostUpdateTS)
                    action();
                onPostUpdateTS.Clear();
            }
            ModuleManager.Instance.PostUpdate();

            if (
                !(Performance.Instance.Enabled.Value &&
                Performance.Instance.LimitFramerateAfterRender.Value) ||
                Util.IsMinimized() // call when minimized to prevent crashing
                )
            {
                Main.game.Delay(); // limits framerate
            }
        }

        public static void game_draw(GameTime gameTime)
        {
            foreach (Action action in onPreRender)
                action();
            onPreRender.Clear();
            lock (onPreRenderTS)
            {
                foreach (Action action in onPreRenderTS)
                    action();
                onPreRenderTS.Clear();
            }
            ModuleManager.Instance.PreRender();

            Main.game.GameDraw(gameTime);

            foreach (Action action in onPostRender)
                action();
            onPostRender.Clear();
            lock (onPostRenderTS)
            {
                foreach (Action action in onPostRenderTS)
                    action();
                onPostRenderTS.Clear();
            }
            ModuleManager.Instance.PostRender();

            if (
                Performance.Instance.Enabled.Value &&
                Performance.Instance.LimitFramerateAfterRender.Value
                )
            {
                Main.game.Delay(); // limits framerate
            }

            Keyboard.Pressed.Fill(false);
        }

        public static void AddOnPreUpdate(Action action)
        {
            lock (onPreUpdateTS)
            {
                onPreUpdateTS.Add(action);
            }
        }

        public static void AddOnPostUpdate(Action action)
        {
            lock (onPostUpdateTS)
            {
                onPostUpdateTS.Add(action);
            }
        }

        public static void AddOnPreRender(Action action)
        {
            lock (onPreRenderTS)
            {
                onPreRenderTS.Add(action);
            }
        }

        public static void AddOnPostRender(Action action)
        {
            lock (onPostRenderTS)
            {
                onPostRenderTS.Add(action);
            }
        }

        // called in Main.game.Delay()
        // returns the targetted frame period in ticks
        public static long get_framelimit(long current)
        {
            int framelimit = Performance.Instance.Framelimit.Value;
            if (framelimit == -1)
                return current;

            if (Online)
                return 10000000L / Math.Min(Math.Max(framelimit, 30), 300);

            if (framelimit < 10)
                framelimit = 10;
            return 10000000L / framelimit;
        }

        // called in Main.game.Delay()
        public static int get_framelimit_method(int current)
        {
            if (Performance.Instance.FramelimitMethod.Value < 0 || Performance.Instance.FramelimitMethod.Value > 3)
                return current;

            return Performance.Instance.FramelimitMethod.Value;
        }

        // called in CEngine's update method
        public static float get_time_scale()
        {
            return LocalGameMods.Instance.TimeScaleVal;
        }

#if !VELO_OLD
        public static void sdl_poll(ref SDL.SDL_Event sdl_event)
        {
            SettingsUI.Instance.SdlPoll(ref sdl_event);
        }
#endif

        public static void player_reset(Player player)
        {
            if (player == MainPlayer)
            {
                foreach (Action action in OnMainPlayerReset)
                    onPreUpdate.Add(action);
            }
        }

        public static void lap_finish()
        {
            foreach (Action action in OnLapFinish)
                onPreUpdate.Add(action);
        }

        public static void boostacoke_add()
        {
            BoostaCokeModified = true;
        }

        public static void boostacoke_remove()
        {
            BoostaCokeModified = true;
        }

        public static float get_camera_max_speed(ICCameraModifier camera)
        {
            if (Online)
                return LocalGameMods.Instance.CameraMaxSpeed.DefaultValue;

            return LocalGameMods.Instance.CameraMaxSpeed.Value;
        }

        public static float get_max_speed()
        {
            if (Online)
                return LocalGameMods.Instance.MaxSpeed.DefaultValue;

            return LocalGameMods.Instance.MaxSpeed.Value;
        }

        public static Vector2 get_gravity()
        {
            if (Online)
                return LocalGameMods.Instance.Gravity.DefaultValue;

            return LocalGameMods.Instance.Gravity.Value;
        }

        public static float get_grapple_hook_speed()
        {
            if (Online)
                return LocalGameMods.Instance.GrappleHookSpeed.DefaultValue;

            return LocalGameMods.Instance.GrappleHookSpeed.Value;
        }

        public static float get_grapple_cooldown()
        {
            if (Online)
                return LocalGameMods.Instance.GrappleCooldown.DefaultValue;

            return LocalGameMods.Instance.GrappleCooldown.Value;
        }

        public static float get_slide_cooldown()
        {
            if (Online)
                return LocalGameMods.Instance.SlideCooldown.DefaultValue;

            return LocalGameMods.Instance.SlideCooldown.Value;
        }

        public static bool get_fix_grapple_glitches()
        {
            if (Online)
                return LocalGameMods.Instance.FixGrappleGlitches.DefaultValue.Enabled;

            return
                LocalGameMods.Instance.FixGrappleGlitches.Value.Enabled &&
                !Keyboard.Held[LocalGameMods.Instance.FixGrappleGlitches.Value.Hotkey];
        }

        public static bool get_enable_old_moonwalk()
        {
            if (Online)
                return LocalGameMods.Instance.EnableOldMoonwalk.DefaultValue;

            return LocalGameMods.Instance.EnableOldMoonwalk.Value;
        }

        private static bool isLaserTraceLine = false;

        public static void set_is_laser_trace_line(bool value)
        {
            isLaserTraceLine = value;
        }

        public static bool enable_trace_line(ICCollidable collidable)
        {
            if (!isLaserTraceLine)
                return true;
            if (collidable == null || !(collidable is CActor))
                return true;
            if (Online)
                return true;
            if ((collidable as CActor).Controller == Ghost)
                return !LocalGameMods.Instance.DisableGhostLaserInteraction.Value;
            return true;
        }

        public static void player_laser_collide(ICCollidable collidable)
        {
            if ((collidable as CActor).Controller == Ghost)
            {
                GhostLaserCollision = true;
            }
        }

        // called in CEngine
        public static bool dt_fixed()
        {
            return LocalGameMods.Instance.DtFixed;
        }

        public static bool set_inputs()
        {
            return LocalGameMods.Instance.SetInputs();
        }

        public static bool skip_if_ghost()
        {
            return LocalGameMods.Instance.SkipIfGhost();
        }

        public static bool skip_if_ghost(Player player)
        {
            return LocalGameMods.Instance.SkipIfGhost() && player == Ghost;
        }

        public static bool skip_update_sprite(Player player)
        {
            return LocalGameMods.Instance.SkipUpdateSprite(player);
        }

        public static void update_camera(ICCameraModifier cameraMod)
        {
            LocalGameMods.Instance.UpdateCamera(cameraMod);
        }

        public static void update_cam_pos(ICCameraModifier cameraMod)
        {
            BlindrunSimulator.Instance.Update(cameraMod);
        }

        public static int event_id()
        {
            return EventBypass.Instance.Event.Value.Id();
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
            Appearance.Instance.UpdatePopup(player);
        }

        public static float popup_opacity()
        {
            return Appearance.Instance.PopupColor.Value.Get().A / 255f * Appearance.Instance.PopupOpacity.Value;
        }

        public static float popup_scale()
        {
            return Appearance.Instance.PopupScale.Value;
        }

        public static void update_grapple_color(Grapple grapple)
        {
            Appearance.Instance.UpdateGrappleColor(grapple);
        }

        public static void update_golden_hook_color(GoldenHook goldenHook)
        {
            Appearance.Instance.UpdateGoldenHookColor(goldenHook);
        }

        public static void update_rope_color(Rope rope)
        {
            Appearance.Instance.UpdateRopeColor(rope);
        }

        public static Color get_popup_color()
        {
            return Appearance.Instance.PopupColor.Value.Get();
        }

        public static Color get_player_color()
        {
            return Appearance.Instance.PlayerColor.Value.Get();
        }

        public static Color get_win_star_color()
        {
            return Appearance.Instance.WinStarColor.Value.Get();
        }

        public static Color get_bubble_color()
        {
            return Appearance.Instance.BubbleColor.Value.Get();
        }

        public static Color get_saw_color()
        {
            return Appearance.Instance.SawColor.Value.Get();
        }

        public static Color get_laser_lethal_inner_color()
        {
            return Appearance.Instance.LaserLethalInnerColor.Value.Get();
        }

        public static Color get_laser_lethal_outer_color()
        {
            return Appearance.Instance.LaserLethalOuterColor.Value.Get();
        }

        public static Color get_laser_lethal_particle_color()
        {
            return Appearance.Instance.LaserLethalParticleColor.Value.Get();
        }

        public static Color get_laser_lethal_smoke_color()
        {
            return Appearance.Instance.LaserLethalSmokeColor.Value.Get();
        }

        public static Color get_laser_non_lethal_inner_color()
        {
            return Appearance.Instance.LaserNonLethalInnerColor.Value.Get();
        }

        public static Color get_laser_non_lethal_outer_color()
        {
            return Appearance.Instance.LaserNonLethalOuterColor.Value.Get();
        }

        public static Color get_laser_non_lethal_particle_color()
        {
            return Appearance.Instance.LaserNonLethalParticleColor.Value.Get();
        }

        public static Color get_laser_non_lethal_smoke_color()
        {
            return Appearance.Instance.LaserNonLethalSmokeColor.Value.Get();
        }

        public static Color get_tile_map_color(ICLayer layer)
        {
            if (layer.Id != "Collision")
                return Color.White;
            return TileMap.Instance.ColorMultiplier.Value.Get();
        }

        public static Color get_background_color(ICLayer layer, Color color)
        {
            if (layer.Id != "BackgroundLayer0")
                return color;
            return new Color(color.ToVector4() * Appearance.Instance.BackgroundColor.Value.Get().ToVector4());
        }

        public static bool draw_chunk(CBufferedTileMapLayer tilemap, Vector2 pos, int x, int y)
        {
            return TileMap.Instance.Draw(tilemap, pos, x, y);
        }

        public static void text_color_updated(CTextDrawComponent text)
        {
            Appearance.Instance.TextColorUpdated(text);
        }

        public static void text_shadow_color_updated(CTextDrawComponent text)
        {
            Appearance.Instance.TextShadowColorUpdated(text);
        }

        public static void image_color_updated(CImageDrawComponent image)
        {
            Appearance.Instance.ImageColorUpdated(image);
        }

        public static void sprite_color_updated(CSpriteDrawComponent sprite)
        {
            Appearance.Instance.SpriteColorUpdated(sprite);
        }

        public static void update_text_color(CTextDrawComponent text)
        {
            Appearance.Instance.UpdateTextColor(text);
        }

        public static void update_image_color(CImageDrawComponent image)
        {
            Appearance.Instance.UpdateImageColor(image);
        }

        public static void update_sprite_color(CSpriteDrawComponent sprite)
        {
            Appearance.Instance.UpdateSpriteColor(sprite);
        }

        public static void add_chat_comp(object obj, string type)
        {
            Appearance.Instance.AddChatComp(obj, type);
        }

        public static void update_chat_color(object obj)
        {
            Appearance.Instance.UpdateChatColor(obj);
        }

        public static bool disable_bubbles()
        {
            return
                Performance.Instance.Enabled.Value &&
                Performance.Instance.DisableBubbles.Value;
        }

        public static bool disable_steam_input_api()
        {
            return
                Performance.Instance.Enabled.Value &&
                Performance.Instance.DisableSteamInputApi.Value;
        }

        public static bool skip_input(int controller_id)
        {
            return
                Performance.Instance.Enabled.Value &&
                Performance.Instance.EnableControllerId.Value != -1 &&
                Performance.Instance.EnableControllerId.Value != controller_id;
        }

#if VELO_OLD
        public static bool skip_all_key_input()
        {
            return
                SettingsUI.Instance.Enabled.Value.Enabled &&
                SettingsUI.Instance.DisableKeyInput.Value;
        }
#endif

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
