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
using Microsoft.Xna.Framework.Input;

namespace Velo
{
    public class Velo
    {
        private static TimeSpan lastFrameTime = TimeSpan.Zero;

        public static TimeSpan Delta;
        public static CEngine.CEngine CEngineInst;
        public static SpriteBatch SpriteBatch;
        public static CContentBundleManager ContentManager;
        public static GraphicsDevice GraphicsDevice;
        public static GameTime GameTime;
        public static Player MainPlayer = null;
        public static Player Ghost = null;
        public static ModuleSolo ModuleSolo = null;
        public static bool Ingame = false;
        public static bool IngamePrev = false;
        public static bool Paused = false;
        public static bool PausedPrev = false;
        public static bool PauseMenu = false;
        public static bool PauseMenuPrev = false;
        public static bool Online = false;
        public static bool BoostaCokeModified = false;
        public static bool GhostLaserCollision = false;
        public static float LastUsedItemTime = float.PositiveInfinity;
        public static byte LastUsedItemId = (byte)EItem.NONE;
        private static byte itemIdPrev = (byte)EItem.NONE;
        public static float LastUsedDrillTime = float.PositiveInfinity;
        public static float LastUsedModTime = float.PositiveInfinity;
        public static float LastUsedSavestateTime = float.PositiveInfinity;

        public static List<Action> OnMainPlayerReset = new List<Action>();
        public static List<Action<float>> OnLapFinish = new List<Action<float>>();

        private static readonly List<Action> onPreUpdate = new List<Action>();

        // thread-safe
        private static readonly List<Action> onPreUpdateTS = new List<Action>();

        private static readonly HashSet<Module> cursorRequests = new HashSet<Module>();

        private static int ghostPollCounter = 0;

        

        public static Player GetMainPlayer()
        {
            CCollisionEngine collisionEngine = CEngineInst.World.CollisionEngine;

            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                ICActorController controller = actor.controller;
                if (controller is Player)
                {
                    Player player = controller as Player;
                    if (player.slot.LocalPlayer)
                        return player;
                }
            }

            return null;
        }

        public static Player GetGhost()
        {
            if (ModuleSolo == null)
                return null;
            CCollisionEngine collisionEngine = CEngineInst.World.CollisionEngine;

            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                ICActorController controller = actor.controller;
                if (controller is Player)
                {
                    Player player = controller as Player;
                    if (!player.slot.LocalPlayer)
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
                if (module is ModuleSolo moduleSolo)
                    return moduleSolo;

            return default;
        }

        public static void EnableCursor(Module requester)
        {
            cursorRequests.Add(requester);
            if (cursorRequests.Count >= 1)
                CEngineInst.Game.IsMouseVisible = true;
        }

        public static void DisableCursor(Module requester)
        {
            cursorRequests.Remove(requester);
            if (cursorRequests.Count == 0)
                CEngineInst.Game.IsMouseVisible = false;
        }

        private static void ClearLastUsedTimes()
        {
            LastUsedItemTime = float.PositiveInfinity;
            LastUsedDrillTime = float.PositiveInfinity;
            LastUsedModTime = float.PositiveInfinity;
            LastUsedSavestateTime = float.PositiveInfinity;
            UpdateLastUsed();
        }

        private static void UpdateLastUsed()
        {
            float delta = (float)CEngineInst.gameTime.ElapsedGameTime.TotalSeconds;
            LastUsedItemTime += delta;
            LastUsedDrillTime += delta;
            LastUsedModTime += delta;
            LastUsedSavestateTime += delta;

            if (MainPlayer != null && itemIdPrev != (int)EItem.NONE && itemIdPrev != MainPlayer.item_id)
            {
                LastUsedItemTime = Math.Min(0f, LastUsedItemTime);
                if (itemIdPrev == (int)EItem.BOMB || itemIdPrev == (int)EItem.TRIGGER)
                    LastUsedItemTime = float.NegativeInfinity;
                LastUsedItemId = itemIdPrev;
            }
            if (MainPlayer != null && MainPlayer.using_drill)
                LastUsedDrillTime = 0f;

            if (LocalGameMods.Instance.IsPlaybackRunning())
                LastUsedModTime = Math.Min(0f, LastUsedModTime);
            else if (
                LocalGameMods.Instance.IsModded() ||
                LocalGameMods.Instance.DtFixed || 
                BlindrunSimulator.Instance.Enabled.Value.Enabled
                )
                LastUsedModTime = float.NegativeInfinity;

            if (MainPlayer != null)
                itemIdPrev = MainPlayer.item_id;
        }

#pragma warning disable IDE1006
#pragma warning disable IDE0060
        // It follows a list of interface methods that the modded game client
        // can call in order to communicate with Velo.
        // These are all written in snake_case.

        // called once after the game's engine has finished setup
        public static void init()
        {
            CEngineInst = CEngine.CEngine.Instance;
            SpriteBatch = CEngineInst.SpriteBatch;
            ContentManager = CEngineInst.ContentBundleManager;
            GraphicsDevice = CEngineInst.GraphicsDevice;
            GameTime = CEngineInst.gameTime;

            ModuleManager.Instance.Init();
            Storage.Instance.Load();
            SettingsUI.Instance.SendUpdates = false;
            SettingsUI.Instance.Enabled.Disable();
            SettingsUI.Instance.SendUpdates = true;
            LocalGameMods.Instance.Freeze.Disable();
            Leaderboard.Instance.Enabled.Disable();
        }

        // replaces the game's update call
        public static void game_update(GameTime gameTime)
        {
            TimeSpan now = new TimeSpan(DateTime.Now.Ticks);
            if (lastFrameTime != new TimeSpan())
                Delta = now - lastFrameTime;
            lastFrameTime = now;

            Input.Update();
           
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
            PauseMenuPrev = PauseMenu;
            PauseMenu = Main.game.stack.baseModule.IsPaused;
            PausedPrev = Paused;
            Paused = CEngineInst.IsPaused && !PauseMenu;
            Online = IsOnline();

            UpdateLastUsed();
            
            if (!IngamePrev && Ingame)
                ClearLastUsedTimes();
           
            ModuleManager.Instance.PostUpdate();

            if (
                !(Performance.Instance.Enabled.Value &&
                Performance.Instance.LimitFramerateAfterRender.Value)
                )
            {
                Main.game.Delay(); // limits framerate
            }

            ghostPollCounter = 0;
        }

        public static void game_draw(GameTime gameTime)
        {
            ModuleManager.Instance.PreRender();

            Main.game.GameDraw(gameTime);

            ModuleManager.Instance.PostRender();

            if (
                Performance.Instance.Enabled.Value &&
                Performance.Instance.LimitFramerateAfterRender.Value
                )
            {
                Main.game.Delay(); // limits framerate
            }
        }

        public static void AddOnPreUpdate(Action action)
        {
            onPreUpdate.Add(action);
        }

        public static void AddOnPreUpdateTS(Action action)
        {
            lock (onPreUpdateTS)
            {
                onPreUpdateTS.Add(action);
            }
        }

        // called in Main.game.Delay()
        // returns the targetted frame period in ticks
        public static long get_framelimit(long current)
        {
            return Performance.Instance.GetFramelimit(current);
        }

        // called in Main.game.Delay()
        public static int get_framelimit_method(int current)
        {
            return Performance.Instance.GetFramelimitMethod(current);
        }

        // called in CEngine's update method
        public static float get_time_scale()
        {
            return LocalGameMods.Instance.TimeScaleVal;
        }

        // skip all key inputs on true
        public static bool disable_key_input()
        {
            return 
                (SettingsUI.Instance.Enabled.Value.Enabled && 
                SettingsUI.Instance.DisableKeyInput.Value) ||
                AutoUpdate.Instance.Enabled;
        }

        // skip all mouse inputs on true
        public static bool disable_mouse_input()
        {
            return
                SettingsUI.Instance.Enabled.Value.Enabled ||
                Leaderboard.Instance.Enabled.Value.Enabled ||
                AutoUpdate.Instance.Enabled;
        }

        // hooked into FNA.dll's event loop
        public static void sdl_poll(ref SDL.SDL_Event sdl_event)
        {
            SettingsUI.Instance.SdlPoll(ref sdl_event);
        }

        // called in Player.Reset()
        public static void player_reset(Player player)
        {
            if (player == MainPlayer)
            {
                onPreUpdate.Add(ClearLastUsedTimes);

                foreach (Action action in OnMainPlayerReset)
                    onPreUpdate.Add(action);
            }
        }

        // called on lap finish
        public static void lap_finish(float time)
        {
            foreach (Action<float> action in OnLapFinish)
                onPreUpdate.Add(() => action(time));
        }

        public static void boostacoke_add()
        {
            BoostaCokeModified = true;
        }

        public static void boostacoke_remove()
        {
            BoostaCokeModified = true;
        }

        // "fixing" a weird bug where a ghost replay will freeze
        // the game by forever generating new packets in a single update
        public static bool ghost_poll()
        {
            if (Delta.TotalSeconds > 0.5 && ModuleSolo != null && ghostPollCounter++ > 100)
            {
                ModuleSolo.removeGhost(GameTime);
                Notifications.Instance.PushNotification("Warning: Game freezing ghost detected and removed!");
                return false;
            }
            return true;
        }

        public static bool disable_ghost_poll()
        {
            return LocalGameMods.Instance.IsGhostPlaybackRunning();
        }

        public static bool disable_grapple_sound(Player player)
        {
            return Miscellaneous.Instance.DisableGrappleSound(player);
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
                !Input.Held(LocalGameMods.Instance.FixGrappleGlitches.Value.Hotkey);
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

        // called when game detects a player laser collision
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

        // called when player inputs get polled (in the Player.Update() method)
        // on true the game skips the input polls
        public static bool set_inputs(Player player)
        {
            return LocalGameMods.Instance.SetInputs(player);
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
            return Miscellaneous.Instance.Event.Value.Id();
        }

        public static bool bypass_pumpkin_cosmo()
        {
            return Miscellaneous.Instance.BypassPumpkinCosmo.Value;
        }

        public static bool bypass_xl()
        {
            return Miscellaneous.Instance.BypassXl.Value;
        }

        public static bool bypass_excel()
        {
            return Miscellaneous.Instance.BypassExcel.Value;
        }

        public static bool reloading_contents()
        {
            return Miscellaneous.Instance.contentsReloaded;
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

        public static bool enable_popup_with_ghost()
        {
            return Appearance.Instance.EnablePopupWithGhost.Value;
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

        public static void update_gate_color(SwitchBlock switchBlock)
        {
            Appearance.Instance.UpdateGateColor(switchBlock);
        }

        public static Color get_popup_color()
        {
            return Appearance.Instance.PopupColor.Value.Get();
        }

        public static Color get_player_color(Player player)
        {
            if (player == MainPlayer)
                return Appearance.Instance.LocalColor.Value.Get();
            else if (player == Ghost)
                return Appearance.Instance.GhostColor.Value.Get();
            else
                return Appearance.Instance.RemoteColor.Value.Get();
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
            if (layer.Id == "BackgroundLayer0")
                return new Color(color.ToVector4() * Appearance.Instance.Background0Color.Value.Get().ToVector4());
            if (layer.Id == "BackgroundLayer1")
                return new Color(color.ToVector4() * Appearance.Instance.Background1Color.Value.Get().ToVector4());
            if (layer.Id == "BackgroundLayer2")
                return new Color(color.ToVector4() * Appearance.Instance.Background2Color.Value.Get().ToVector4());
            if (layer.Id == "BackgroundLayer3")
                return new Color(color.ToVector4() * Appearance.Instance.Background3Color.Value.Get().ToVector4());

            return color;
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

        public static string timespan_to_string(TimeSpan timespan, string format)
        {
            if (!Leaderboard.Instance.PreciseTimer.Value)
                return timespan.ToString(format);
            return Util.FormatTime(timespan.Ticks / TimeSpan.TicksPerMillisecond, ETimeFormat.DOT_COLON);
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
