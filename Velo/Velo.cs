using CEngine.Content;
using CEngine.Graphics.Camera;
using CEngine.Graphics.Component;
using CEngine.Graphics.Layer;
using CEngine.Graphics.Library;
using CEngine.Util.Input;
using CEngine.World.Actor;
using CEngine.World.Collision;
using CSpeedRunner.Library.Bundle;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Velo
{
    public class Velo
    {
        public static bool Exiting = false;

        private static TimeSpan lastFrameTime = TimeSpan.Zero;
        public static TimeSpan RealTime;
        public static TimeSpan RealDelta;
        public static TimeSpan GameTime => CEngine.CEngine.Instance.gameTime.TotalGameTime;
        public static TimeSpan GameDelta => CEngine.CEngine.Instance.gameTime.ElapsedGameTime;
        public static TimeSpan GameDeltaPreFreeze = TimeSpan.Zero;
        public static CEngine.CEngine CEngineInst;
        public static SpriteBatch SpriteBatch;
        public static CContentBundleManager ContentManager;
        public static GraphicsDevice GraphicsDevice;
        public static Player MainPlayer = null;
        public static ModuleSolo ModuleSolo = null;
        public static ModuleMP ModuleMP = null;
        public static ModuleLevelEditor ModuleLevelEditor = null; 
        public static bool Ingame = false;
        public static bool IngamePrev = false;
        public static bool Paused = false;
        public static bool PausedPrev = false;
        public static bool PauseMenu = false;
        public static bool PauseMenuPrev = false;
        public static bool Online = false;
        public static byte ItemId = (byte)EItem.NONE;
        public static byte ItemIdPrev = (byte)EItem.NONE;
        public static bool BoostaCokeModified = false;
        public static bool GhostLaserCollision = false;
        public static bool GhostFallTileCollision = false;
        public static bool GhostFocused = false;
        public static List<Vector2> VelocityPrev = new List<Vector2>();

        public static int MainThreadId;

        private static List<Action> onPreUpdate = new List<Action>();
        private static List<Action> onPreUpdateTemp = new List<Action>();
        private static readonly List<Action> onPreUpdateTS = new List<Action>();
        private static readonly List<Action> onExit = new List<Action>();
        public static List<Action> onMainPlayerReset = new List<Action>();
        public static List<Action<float>> onLapFinish = new List<Action<float>>();

        private static bool ignorePlayerReset = false;

        private static int ghostPollCounter = 0;

        private static bool originsLoaded = false;
        private static bool timerTriggered = false;

        private static ICInputMap moduleSoloInputMap = null;

        public static bool WindowMoved = false;

        public static bool DisableFramelimit = false;

        // The game becomes "poisoned" if a "set" command was executed or a poisoned savestate was loaded
        public static bool Poisoned = false;

        public static RenderTarget2D RenderTarget;

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

        public static void AddOnExit(Action action)
        {
            onExit.Add(action);
        }

        public static void AddOnMainPlayerReset(Action action)
        {
            onMainPlayerReset.Add(action);
        }

        public static void AddOnLapFinish(Action<float> action)
        {
            onLapFinish.Add(action);
        }

        public static Player GetMainPlayer()
        {
            if (Main.game.stack.gameInfo == null)
                return null;

            Slot[] slots = Main.game.stack.gameInfo.slots;

            for (int i = 0; i < 4; i++)
            {
                if (slots[i].Player != null && slots[i].LocalPlayer)
                    return slots[i].Player;
            }
            return null;
        }

        public static bool IsOnline()
        {
            if (Main.game.stack.gameInfo == null)
                return false;

            if (ModuleSolo != null)
                return false;

            Slot[] slots = Main.game.stack.gameInfo.slots;
            for (int i = 0; i < 4; i++)
            {
                if (slots[i].Player != null && !slots[i].LocalPlayer)
                    return true;
            }
            return false;
        }

        public static ModuleSolo GetModuleSolo()
        {
            if (Main.game.stack.gameInfo == null)
                return null;

            List<IModule> modules = Main.game.stack.modules;
            int count = modules.Count;
            for (int i = 0; i < count; i++)
            {
                if (modules[i] is ModuleSolo moduleSolo)
                    return moduleSolo;
            }
            return default;
        }

        public static ModuleMP GetModuleMP()
        {
            if (Main.game.stack.gameInfo == null)
                return null;

            List<IModule> modules = Main.game.stack.modules;
            int count = modules.Count;
            for (int i = 0; i < count; i++)
            {
                if (modules[i] is ModuleMP moduleMP)
                    return moduleMP;
            }
            return default;
        }

        public static ModuleLevelEditor GetModuleLevelEditor()
        {
            if (Main.game.stack.gameInfo == null)
                return null;

            List<IModule> modules = Main.game.stack.modules;
            int count = modules.Count;
            for (int i = 0; i < count; i++)
            {
                if (modules[i] is ModuleLevelEditor moduleLevelEditor)
                    return moduleLevelEditor;
            }
            return default;
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
            RealTime = new TimeSpan(DateTime.Now.Ticks);
            MainThreadId = Thread.CurrentThread.ManagedThreadId;

            ModuleManager.Instance.Init();
            Storage.Instance.Load();
            SettingsUI.Instance.SendUpdates = false;
            ModuleManager.Instance.InitModules();
            SettingsUI.Instance.Enabled.Disable();
            OfflineGameMods.Instance.Freeze.Disable();
            Leaderboard.Instance.Enabled.Disable();
            Origins.Instance.Enabled.Disable();
            SettingsUI.Instance.SendUpdates = true;
            Input.Init();

            if (!Verify.VerifyFiles(out Dictionary<string, bool> result))
            {
                string message = "WARNING: Client modification detected! Please make sure that the following\nfiles are all in their original state before proceeding to submit runs:\n";
                foreach (var pair in result)
                {
                    if (!pair.Value)
                        message += "-" + pair.Key + "\n";
                }
                message = message.Substring(0, message.Length - 1);
                Notifications.Instance.PushNotification(message, Color.Red, TimeSpan.FromSeconds(15));
            }

            if (Directory.Exists("Velo\\update"))
                Directory.Delete("Velo\\update", true);
            if (File.Exists("VeloUpdater.exe")) // old version
                File.Delete("VeloUpdater.exe");
            if (File.Exists("VeloVerifier.exe")) // old version
                File.Delete("VeloVerifier.exe");
            if (Directory.Exists("Velo\\recording") && !Directory.Exists("Velo\\recordings"))
                Directory.Move("Velo\\recording", "Velo\\recordings");
            if (Directory.Exists("Velo\\savestate") && !Directory.Exists("Velo\\savestates"))
                Directory.Move("Velo\\savestate", "Velo\\savestates");
            if (!File.Exists("Velo\\_statsMessage.txt"))
            {
                File.WriteAllText("Velo\\_statsMessage.txt",
@"${Velo.frame}:
vx: ${Player.actor.velocity.x 2 8}, vy: ${Player.actor.velocity.y 2 8}, va: ${Player.actor.velocity._a 2 7},
px: ${Player.actor.position.x 2 8}, py: ${Player.actor.position.y 2 8}, ax: ${Player.actor._acceleration.x 2 7},
j: ${Player.jumpVelocity.y 2 7}, b: ${Player.boost 3 5}, ib: ${Player.insideBoostSection 0 5}, sf: ${Player._surfCooldown 6 8},
gcd: ${Player._grappleCooldown 6 8}, bcd: ${Player.boostCooldown 6 9}, scd: ${Player._slideCooldown 6 8}");
            }
        }

        [DllImport("Velo_UI.dll", EntryPoint = "Capture")]
        private static extern void Capture();

        // replaces the game's update call
        public static void game_update(GameTime gameTime)
        {
            measure("Velo");

            RealTime = new TimeSpan(DateTime.Now.Ticks);
            if (lastFrameTime != new TimeSpan())
                RealDelta = RealTime - lastFrameTime;
            lastFrameTime = RealTime;

            Input.Update();

            onPreUpdateTemp = onPreUpdate;
            onPreUpdate = new List<Action>();
            lock (onPreUpdateTS)
            {
                onPreUpdateTemp.AddRange(onPreUpdateTS);
                onPreUpdateTS.Clear();
            }
            int count = onPreUpdateTemp.Count;
            for (int i = 0; i < count; i++)
                onPreUpdateTemp[i]();
            onPreUpdateTemp.Clear();
            
            ModuleManager.Instance.PreUpdate();
            BoostaCokeModified = false;
            GhostLaserCollision = false;
            GhostFallTileCollision = false;
            GhostFocused = false;

            if (GameDelta != TimeSpan.Zero && !PauseMenu)
            {
                count = CEngineInst.World.CollisionEngine.actorsById.Count;
                for (int i = 0; i < count; i++)
                {
                    if (VelocityPrev.Count <= i)
                        VelocityPrev.Add(Vector2.Zero);
                    CActor actor = CEngineInst.World.CollisionEngine.actorsById[i];
                    if (actor != null)
                        VelocityPrev[i] = actor.Velocity;
                    else
                        VelocityPrev[i] = Vector2.Zero;
                }
            }

            measure("other");
            Main.game.GameUpdate(gameTime); // the game's usual update procedure
            measure("Velo");

            if (GameDelta != TimeSpan.Zero && !PauseMenu)
            {
                GameDeltaPreFreeze = GameDelta;
            }

            // cache a few commonly needed values
            ModuleSolo = GetModuleSolo();
            ModuleMP = GetModuleMP();
            ModuleLevelEditor = GetModuleLevelEditor();
            Online = IsOnline(); 
            MainPlayer = GetMainPlayer();
            IngamePrev = Ingame;
            Ingame = MainPlayer != null;
            PauseMenuPrev = PauseMenu;
            PauseMenu = Main.game.stack.baseModule.IsPaused; 
            PausedPrev = Paused;
            Paused = CEngineInst.IsPaused && !PauseMenu;
            ItemIdPrev = ItemId;
            ItemId = MainPlayer != null ? MainPlayer.itemId : (byte)0;

            if (ModuleLevelEditor == null)
                moduleSoloInputMap = null;

            if (ModuleSolo == null)
                Poisoned = false;
           
            if (is_origins() && Ingame && !Paused && PausedPrev && !timerTriggered)
            {
                ModuleSolo.hud.Trigger();
                ModuleSolo.timer.Trigger();
                timerTriggered = true;
            }

            if (!Ingame)
                timerTriggered = false;

            ModuleManager.Instance.PostUpdate();

            CEngineInst.Game.IsMouseVisible = Util.CursorEnabled();

            if (!DisableFramelimit)
            {
                measure("idle");
                Main.game.Delay(); // limits framerate
                measure("Velo");
            }

            ignorePlayerReset = false;

            ghostPollCounter = 0;

            WindowMoved = false;

            if (!originsLoaded)
            {
                ContentManager.LoadBundle("Boss01", false);
                ContentManager.AddBundle("Boss02", Boss02Bundle.Content);
                ContentManager.LoadBundle("Boss02", false);
                ContentManager.LoadBundle("Boss03", false);
                originsLoaded = true;
            }

            measure("other");
        }

        public static RenderTarget2D set_render_target(RenderTarget2D target)
        {
            if (RenderTarget != null && target == null)
            {
                return RenderTarget;
            }
            return target;
        }

        public static void game_draw(GameTime gameTime)
        {
            measure("Velo");
            ModuleManager.Instance.PreRender();

            measure("render");
            Main.game.GameDraw(gameTime);

            measure("Velo");
            ModuleManager.Instance.PostRender();

            RecordingAndReplay.Instance.PlaybackPostRender();

            measure("render");
        }

        public static void post_present()
        {
            measure("Velo");

            ModuleManager.Instance.PostPresent();

            measure("other");
        }

        public static void on_exit()
        {
            Exiting = true;
            onExit.ForEach(a => a());
            if (RecordingAndReplay.Instance.Recorder is TASRecorder tasRecorder)
            {
                if (tasRecorder.NeedsSave)
                    tasRecorder.Save(false, recover: true);
            }
        }

        public static void pre_sdl_poll()
        {
            ModuleManager.Instance.PreSDLPoll();
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
            return OfflineGameMods.Instance.TimeScaleVal;
        }

        // skip all key inputs on true
        public static bool disable_key_input()
        {
            return Util.KeyInputsDisabled();
        }

        // skip all mouse inputs on true
        public static bool disable_mouse_input()
        {
            return Util.MouseInputsDisabled();
        }

        // hooked into FNA.dll's event loop
        public static void sdl_poll(ref SDL.SDL_Event sdl_event)
        {
            SettingsUI.Instance.SdlPoll(ref sdl_event);
        }

        // called in Player.Reset()
        // gets called twice on pressing reset for some reason
        public static void player_reset(Player player)
        {
            if (player == MainPlayer && !ignorePlayerReset)
            {
                onMainPlayerReset.ForEach(a => a());

                ignorePlayerReset = true;
            }
        }

        // called on lap finish
        public static void lap_finish(float time)
        {
            onLapFinish.ForEach(a => a(time));
        }

        public static void spawn_ghost(Player ghost)
        {
            Ghosts.Instance.MainGhostSpawned(ghost);
        }

        // bypasses the check whether a ghost is spawned in the ghost removal routine
        public static bool remove_ghost_bypass = false;

        public static void remove_ghost()
        {
            if (Ghosts.Instance.Get(0) != null)
                remove_ghost_bypass = true;
            Ghosts.Instance.RemoveAll(destroy: true);
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
            ghostPollCounter++;
            if (
                (RealDelta.TotalSeconds > 0.5 && ModuleSolo != null && ghostPollCounter > 100) ||
                (ModuleSolo != null && ghostPollCounter > 10000)
                )
            {
                ModuleSolo.removeGhost(CEngineInst.gameTime);
                Notifications.Instance.PushNotification("Warning: Game freezing ghost detected and removed!");
                return false;
            }
            return true;
        }

        public static bool disable_ghost_poll()
        {
            return RecordingAndReplay.Instance.GhostPlaybackCount > 0;
        }

        public static bool disable_grapple_sound(Player player)
        {
            return Miscellaneous.Instance.DisableGrappleSound(player);
        }

        public static float get_camera_max_speed(ICCameraModifier camera)
        {
            if (Online)
                return OfflineGameMods.Instance.CameraMaxSpeed.DefaultValue;

            return OfflineGameMods.Instance.CameraMaxSpeed.Value;
        }

        public static float get_max_speed()
        {
            if (Online)
                return OfflineGameMods.Instance.MaxSpeed.DefaultValue;

            return OfflineGameMods.Instance.MaxSpeed.Value;
        }

        public static Vector2 get_gravity()
        {
            if (Online)
                return OfflineGameMods.Instance.Gravity.DefaultValue;

            return OfflineGameMods.Instance.Gravity.Value;
        }

        public static float get_jump_strength()
        {
            if (Online)
                return OfflineGameMods.Instance.JumpStrength.DefaultValue;

            return OfflineGameMods.Instance.JumpStrength.Value;
        }

        public static float get_wall_jump_strength(float jumpStrength, Player player)
        {
            if (player is PlayerBot || player.gameInfo.isOption((int)EGameOptions.SUPER_SPEED_RUNNERS))
                return jumpStrength;

            if (Online)
                return OfflineGameMods.Instance.WallJumpStrength.DefaultValue;

            return OfflineGameMods.Instance.WallJumpStrength.Value;
        }

        public static float get_jump_duration()
        {
            if (Online)
                return OfflineGameMods.Instance.JumpDuration.DefaultValue;

            return OfflineGameMods.Instance.JumpDuration.Value;
        }

        public static float get_grapple_hook_speed()
        {
            if (Online)
                return OfflineGameMods.Instance.GrappleHookSpeed.DefaultValue;

            return OfflineGameMods.Instance.GrappleHookSpeed.Value;
        }

        public static float get_grapple_cooldown()
        {
            return OfflineGameMods.Instance.GetGrappleCooldown();
        }

        public static float get_slide_cooldown()
        {
            if (Online)
                return OfflineGameMods.Instance.SlideCooldown.DefaultValue;

            return OfflineGameMods.Instance.SlideCooldown.Value;
        }

        public static bool get_fix_grapple_glitches()
        {
            if (Online)
                return OfflineGameMods.Instance.FixGrappleGlitches.DefaultValue.Enabled;

            return
                OfflineGameMods.Instance.FixGrappleGlitches.Value.Enabled &&
                !Input.IsDown(OfflineGameMods.Instance.FixGrappleGlitches.Value.Hotkey);
        }

        public static bool get_fix_bounce_glitch()
        {
            return OfflineGameMods.Instance.GetFixBounceGlitch();
        }

        public static bool get_enable_old_moonwalk()
        {
            if (Online)
                return OfflineGameMods.Instance.EnableOldMoonwalk.DefaultValue;

            return OfflineGameMods.Instance.EnableOldMoonwalk.Value;
        }

        public static void update_buffer_grapples(Player player)
        {
            OfflineGameMods.Instance.UpdateBufferGrapples(player);
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
            if ((collidable as CActor).Controller == Ghosts.Instance.Get(0))
                return !OfflineGameMods.Instance.DisableGhostLaserInteraction.Value;
            return true;
        }

        // called when game detects a player laser collision
        public static void player_laser_collide(ICCollidable collidable)
        {
            if ((collidable as CActor).Controller == Ghosts.Instance.Get(0))
            {
                GhostLaserCollision = true;
            }
        }

        public static bool skip_fall_tile_collision(Player player)
        {
            if (ModuleSolo == null)
                return false;
            if (OfflineGameMods.Instance.DisableGhostFallTileInteraction.Value)
                return player != MainPlayer;
            if (player != MainPlayer)
                GhostFallTileCollision = true;
            return false;
        }

        // called in CEngine
        public static bool dt_fixed()
        {
            return OfflineGameMods.Instance.DtFixed;
        }

        // called when player inputs get polled (in the Player.Update() method)
        // on true the game skips the input polls
        public static bool set_inputs(Player player)
        {
            measure("Velo");
            bool result = RecordingAndReplay.Instance.SetInputs(player);
            measure("physics");
            return result;
        }

        public static bool skip_update_sprite(Player player)
        {
            return RecordingAndReplay.Instance.SkipUpdateSprite(player);
        }

        public static void update_camera(ICCameraModifier cameraMod)
        {
            OfflineGameMods.Instance.UpdateCamera(cameraMod);
        }

        public static void update_cam_pos(ICCameraModifier cameraMod)
        {
            measure("Velo");
            BlindrunSimulator.Instance.Update(cameraMod);
            measure("physics");
        }

        public static int event_id(int id)
        {
            if (Miscellaneous.Instance.Event.Value == EEvent.DEFAULT)
                return id;
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
            if (player.slot.LocalPlayer && !player.slot.IsBot)
                return Appearance.Instance.LocalColor.Value.Get();
            else if (!Online)
                return Appearance.Instance.GhostColor.Value.Get();
            else
                return Appearance.Instance.RemoteColor.Value.Get();
        }

        public static Vector2 get_afterimage_position(Player player)
        {
            Vector2 dir = player.actor.Velocity;
            dir.Normalize();
            return player.actor.Position - Appearance.Instance.AfterimagesOffset.Value * dir;
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
            return Appearance.Instance.GetBackgroundColor(layer, color);
        }

        public static Color get_hovered_color()
        {
            return Miscellaneous.Instance.HoveredColor.Value;
        }

        public static Color get_resizing_color()
        {
            return Miscellaneous.Instance.ResizingColor.Value;
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

        public static void actor_spawned(CActor actor)
        {
            Appearance.Instance.ActorSpawned(actor);
        }

        public static bool trail_add_point(int quality, double time, Player player)
        {
            return Performance.Instance.TrailAddPoint(quality, time, player);
        }

        public static bool ctrail_update_last()
        {
            return Performance.Instance.Enabled.Value && Performance.Instance.TrailResolution.Value != 0;
        }

        public static bool disable_bubbles()
        {
            return
                Performance.Instance.Enabled.Value &&
                Performance.Instance.DisableBubbles.Value;
        }

        public static void particle_engine_update()
        {
            Performance.Instance.ParticleEngineUpdate();
        }

        public static long get_time(long time)
        {
            if (Performance.Instance.Enabled.Value && Performance.Instance.PreciseTime.Value)
                return Util.UtcNow;
            else
                return time;
        }

        private static long prev_time = Util.UtcNow;

        public static long get_elapsed_time(long time)
        {
            if (Performance.Instance.Enabled.Value && Performance.Instance.PreciseTime.Value)
            {
                long now = Util.UtcNow;
                long elapsed = now - prev_time;
                prev_time = now;
                return elapsed;
            }
            else
                return time;
        }

        public static bool fix_input_delay()
        {
            return
                Performance.Instance.Enabled.Value &&
                Performance.Instance.FixInputDelay.Value;
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

        public static void poll_key_inputs()
        {
            if (Performance.Instance.FixInputDelay.Value && Performance.Instance.Enabled.Value)
                Input.PollLLHooks();
        }

        private static bool enabled_reduced_input_delay = false;

        public static void enable_reduced_input_delay(bool enable)
        {
            enabled_reduced_input_delay = enable;
        }

        public static bool is_down(bool isDown, Microsoft.Xna.Framework.Input.Keys key)
        {
            if (Util.KeyInputsDisabled())
                return false;
            if (!Performance.Instance.FixInputDelay.Value || !Performance.Instance.Enabled.Value || !enabled_reduced_input_delay)
                return isDown;
            return Input.IsKeyDown((byte)key);
        }

        public static bool is_up(bool isUp, Microsoft.Xna.Framework.Input.Keys key)
        {
            if (Util.KeyInputsDisabled())
                return true;
            if (!Performance.Instance.FixInputDelay.Value || !Performance.Instance.Enabled.Value)
                return isUp;
            return !Input.IsKeyDown((byte)key);
        }

        public static void set_mouse_inputs_prepare(Player player)
        {
            Miscellaneous.Instance.SetMouseInputsPrepare(player);
        }

        public static void set_mouse_inputs(Player player)
        {
            Miscellaneous.Instance.SetMouseInputs(player);
        }

        public static bool is_jump_pressed(Player player, bool isPressed)
        {
            return Miscellaneous.Instance.IsJumpPressed(player, isPressed);
        }

        public static bool is_slide_pressed(Player player, bool isPressed)
        {
            return Miscellaneous.Instance.IsSlidePressed(player, isPressed);
        }

        public static bool is_boost_pressed(Player player, bool isPressed)
        {
            return Miscellaneous.Instance.IsBoostPressed(player, isPressed);
        }

        public static bool level_editor_reset_released()
        {
            if (!Miscellaneous.Instance.UseResetBind.Value)
                return ModuleLevelEditor.inputMap.IsReleased("R");

            if (moduleSoloInputMap == null)
                moduleSoloInputMap = CEngineInst.GetInputMap(InputMapProvider.GetModuleSoloInputMap(Main.game.stack));
            
            return moduleSoloInputMap.IsReleased("ps_reset_practice_lap");
        }

        public static bool level_editor_select_frontmost_object()
        {
            return Miscellaneous.Instance.SelectFrontmostObject.Value;
        }

        public static bool level_editor_select_frontmost_graphic()
        {
            return Miscellaneous.Instance.SelectFrontmostGraphic.Value;
        }

        private class ColComparer : IComparer<CCollisionPair>
        {
            public Func<CCollisionPair, CCollisionPair, int> Func;

            public int Compare(CCollisionPair x, CCollisionPair y)
            {
                return Func(x, y);
            }
        }

        public static void sort_collisions_by_target(CCollisionPair[] collisions, int count)
        {
            Dictionary<CActor, int> actorIndices = new Dictionary<CActor, int>();
            for (int i = 0; i < CEngineInst.World.CollisionEngine.ActorCount; i++)
            {
                actorIndices.Add(CEngineInst.World.CollisionEngine.actors[i], i);
            }

            Array.Sort(collisions, 0, count, new ColComparer
            {
                Func = (c1, c2) =>
                {
                    if (
                    c1 != null && c2 != null &&
                    c1.target is CActor a1 && c2.target is CActor a2 &&
                    actorIndices.TryGetValue(a1, out int id1) && actorIndices.TryGetValue(a2, out int id2)
                    )
                    {
                        return id1.CompareTo(id2);
                    }
                    return -1;
                }
            });
        }

        public static int save_actors_count(int count)
        {
            return count;
        }

        public static void save_additional_actors(BinaryWriter writer)
        {
            
        }

        public static void measure(string label)
        {
            Performance.Instance.Measure(label);
        }

        public static void measure_previous()
        {
            Performance.Instance.MeasurePrevious();
        }

        public static string timespan_to_string(TimeSpan timespan, string format)
        {
            if (!Leaderboard.Instance.PreciseTimer.Value)
                return timespan.ToString(format);
            return Util.FormatTime(timespan.Ticks / TimeSpan.TicksPerMillisecond, ETimeFormat.DOT_COLON);
        }

        public static bool is_origins()
        {
            return Origins.Instance.IsOrigins();
        }

        public static bool enter_origins()
        {
            return Origins.Instance.EnterOrigins();
        }

        public static string origins_path()
        {
            return Origins.Instance.OriginsPath();
        }

        public static void touch_finish_bomb(Player player)
        {
            Origins.Instance.TouchFinishBomb(player);
        }

        public static bool check_collision_pairs_for_player(ref CCollisionPair[] collision_pairs)
        {
            foreach (CCollisionPair pair in collision_pairs)
            {
                if (pair == null)
                    continue;
                if (pair.source == MainPlayer.actor || pair.target == MainPlayer.actor)
                    return true;
            }
            return false;
        }

        public static Vector2 dove_offset()
        {
            if (!Origins.Instance.IsOrigins())
                return Vector2.Zero;
            return new Vector2(-100f, -100f);
        }

        public static Vector2 get_origin(ICharacter character, int variant)
        {
            if (Miscellaneous.Instance.SkinConstantsLookup.TryGetValue(new KeyValuePair<ICharacter, int>(character, variant), out var v))
                return v.Origin.Value;
            return character.Origin;
        }

        public static Vector2 get_position(ICharacter character, int variant)
        {
            if (Miscellaneous.Instance.SkinConstantsLookup.TryGetValue(new KeyValuePair<ICharacter, int>(character, variant), out var v))
                return v.Position.Value;
            return character.Position;
        }

        public static Vector2 get_rope_offset(ICharacter character, int variant)
        {
            if (Miscellaneous.Instance.SkinConstantsLookup.TryGetValue(new KeyValuePair<ICharacter, int>(character, variant), out var v))
                return v.RopeOffset.Value;
            return character.RopeOffset;
        }

        public static Vector2 get_swing_origin(ICharacter character, int variant)
        {
            if (Miscellaneous.Instance.SkinConstantsLookup.TryGetValue(new KeyValuePair<ICharacter, int>(character, variant), out var v))
                return v.SwingOrigin.Value;
            return character.SwingOrigin;
        }

        public static Vector2 get_swing_position(ICharacter character, int variant)
        {
            if (Miscellaneous.Instance.SkinConstantsLookup.TryGetValue(new KeyValuePair<ICharacter, int>(character, variant), out var v))
                return v.SwingPosition.Value;
            return character.SwingPosition;
        }

        public static Vector2 get_swing_offset(ICharacter character, int variant)
        {
            if (Miscellaneous.Instance.SkinConstantsLookup.TryGetValue(new KeyValuePair<ICharacter, int>(character, variant), out var v))
                return v.SwingOffset.Value;
            return character.SwingOffset;
        }

        public static Vector2 get_taunt_origin(ICharacter character, int variant)
        {
            if (Miscellaneous.Instance.SkinConstantsLookup.TryGetValue(new KeyValuePair<ICharacter, int>(character, variant), out var v))
                return v.TauntOrigin.Value;
            return character.TauntOrigin;
        }

        public static Vector2 get_taunt_position(ICharacter character, int variant)
        {
            if (Miscellaneous.Instance.SkinConstantsLookup.TryGetValue(new KeyValuePair<ICharacter, int>(character, variant), out var v))
                return v.TauntPosition.Value;
            return character.TauntPosition;
        }

        public static Vector2 get_climb_origin(ICharacter character, int variant)
        {
            if (Miscellaneous.Instance.SkinConstantsLookup.TryGetValue(new KeyValuePair<ICharacter, int>(character, variant), out var v))
                return v.ClimbOrigin.Value;
            return character.ClimbOrigin;
        }

        public static Vector2 get_climb_position(ICharacter character, int variant)
        {
            if (Miscellaneous.Instance.SkinConstantsLookup.TryGetValue(new KeyValuePair<ICharacter, int>(character, variant), out var v))
                return v.ClimbPosition.Value;
            return character.ClimbPosition;
        }

        public static Vector2 get_scale(ICharacter character, int variant)
        {
            if (Miscellaneous.Instance.SkinConstantsLookup.TryGetValue(new KeyValuePair<ICharacter, int>(character, variant), out var v))
                return v.Scale.Value;
            return character.Scale;
        }

        public static Color get_character_select_background_color(ICharacter character)
        {
            if (Miscellaneous.Instance.SkinConstantsLookup.TryGetValue(new KeyValuePair<ICharacter, int>(character, 0), out var v))
                return v.CharacterSelectBackgroundColor.Value.Get();
            return character.CharacterSelectBackgroundColor;
        }

        public static int get_player_count()
        {
            return 4;
        }

        public static bool is_16_players()
        {
            return false;
        }

        public static int version_major()
        {
            return 65;
        }

        public static int version_minor()
        {
            return 11;
        }

        public static int version_protocol()
        {
            return 108;
        }

        public static void reset_all_slots_as_bots()
        {
            Main.game.stack.gameInfo.GetSlot(0).ResetAsBot(0, 0);
            Main.game.stack.gameInfo.GetSlot(1).ResetAsBot(1, 0);
            Main.game.stack.gameInfo.GetSlot(2).ResetAsBot(5, 0);
            Main.game.stack.gameInfo.GetSlot(3).ResetAsBot(8, 0);
            if (is_16_players())
            {
                Main.game.stack.gameInfo.GetSlot(4).ResetAsBot(2, 0);
                Main.game.stack.gameInfo.GetSlot(5).ResetAsBot(3, 0);
                Main.game.stack.gameInfo.GetSlot(6).ResetAsBot(4, 0);
                Main.game.stack.gameInfo.GetSlot(7).ResetAsBot(6, 0);
                Main.game.stack.gameInfo.GetSlot(8).ResetAsBot(7, 0);
                Main.game.stack.gameInfo.GetSlot(9).ResetAsBot(9, 0);
                Main.game.stack.gameInfo.GetSlot(10).ResetAsBot(10, 0);
                Main.game.stack.gameInfo.GetSlot(11).ResetAsBot(11, 0);
                Main.game.stack.gameInfo.GetSlot(12).ResetAsBot(12, 0);
                Main.game.stack.gameInfo.GetSlot(13).ResetAsBot(13, 0);
                Main.game.stack.gameInfo.GetSlot(14).ResetAsBot(14, 0);
                Main.game.stack.gameInfo.GetSlot(15).ResetAsBot(15, 0);
            }
        }

        public static void reset_all_slots()
        {
            for (byte i = 1; i < get_player_count(); i++)
                Main.game.stack.gameInfo.GetSlot(i).SetStateAndCharacter(1, byte.MaxValue, 0, -1, null);
        }

        public static void close_all_slots()
        {
            for (byte i = 1; i < get_player_count(); i++)
                Main.game.stack.gameInfo.GetSlot(i).SetConnectedClosed();
        }

        public static int[] create_minus_one_array()
        {
            return Enumerable.Repeat(-1, get_player_count()).ToArray();
        }

        public static byte[] create_range_to_player_count()
        {
            return Enumerable.Range(0, get_player_count()).Select(i => (byte)i).ToArray();
        }

        public static void ghost_focused()
        {
            GhostFocused = true;
        }

        public static string get_frame_key(Deco deco, string frameKey)
        {
            if (deco.bundleId.Value == "StageMetro")
            {
                return ((CMultiSpriteAtlas)deco.graphic).regions[deco.frame.Value].region.Key;
            }
            return frameKey;
        }

        public static bool disable_quickchat()
        {
            if (ModuleSolo != null)
                return Miscellaneous.Instance.DisableSoloQuickchat.Value;
            return false;
        }

        public static void load_content(string name, object content)
        {
            /*if (content is Texture2D texture)
            {
                if (
                    !name.StartsWith("Backgrounds") &&
                    !name.StartsWith("Sprites\\Deco")
                    )
                    return;
                if (texture.Format == SurfaceFormat.Dxt1 || texture.Format == SurfaceFormat.Dxt3 || texture.Format == SurfaceFormat.Dxt5)
                    return;

                List<int> stack = new List<int>();

                int size = texture.Width * texture.Height;
                int w = texture.Width;
                Color[] data = new Color[size];
                texture.GetData(data);

                byte[] visited = new byte[size];
                for (int i = 0; i < size; i++)
                {
                    if (visited[i] != 0 || data[i].A == 0)
                        continue;

                    stack.Add(i);
                    
                    int rSum = 0;
                    int gSum = 0;
                    int bSum = 0;
                    int count = 0;

                    while (stack.Count > 0)
                    {
                        int j = stack.Last();
                        stack.RemoveAt(stack.Count - 1);

                        if (visited[j] != 0 || data[j].A == 0)
                            continue;

                        rSum += data[j].R;
                        gSum += data[j].G;
                        bSum += data[j].B;
                        count++;
                        visited[j] = 1;

                        if (j >= 1)
                            stack.Add(j - 1);
                        if (j < size - 1)
                            stack.Add(j + 1);
                        if (j >= w)
                            stack.Add(j - w);
                        if (j < size - w)
                            stack.Add(j + w);
                    }

                    stack.Add(i);
                    byte r = (byte)(rSum / (double)count);
                    byte g = (byte)(gSum / (double)count);
                    byte b = (byte)(bSum / (double)count);

                    while (stack.Count > 0)
                    {
                        int j = stack.Last();
                        stack.RemoveAt(stack.Count - 1);

                        if (visited[j] != 1 || data[j].A == 0)
                            continue;

                        data[j].R = (byte)(r * data[j].A / 255d);
                        data[j].G = (byte)(g * data[j].A / 255d);
                        data[j].B = (byte)(b * data[j].A / 255d);
                        visited[j] = 2;

                        if (j >= 1)
                            stack.Add(j - 1);
                        if (j < size - 1)
                            stack.Add(j + 1);
                        if (j >= w)
                            stack.Add(j - w);
                        if (j < size - w)
                            stack.Add(j + w);
                    }
                }

                texture.SetData(data);
            }*/
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
