using CEngine.World.Collision.Shape;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Velo
{
    public class Performance : Module
    {
        public BoolSetting Enabled;
        public BoolSetting ShowStats;

        public IntSetting Framelimit;
        public IntSetting FramelimitMethod;
        public BoolSetting PreciseTime;
        public BoolSetting MaximumTimerResolution;

        public IntSetting TrailResolution;
        public BoolSetting DisableBubbles;
        public BoolSetting DisableRemotePlayersAfterimages;
        public BoolSetting DisableBrakeParticles;
        public BoolSetting DisableWallParticles;
        public BoolSetting DisablePlusParticles;
        public BoolSetting DisableGlitterParticles;
        public BoolSetting DisableDrillSparkParticles;
        public BoolSetting DisableRemotePlayersGoldenTrail;

        public BoolSetting FixInputDelay;
        public BoolSetting DisableSteamInputApi;
        public IntSetting EnableControllerId;

        public BoolSetting MultithreadedNetwork;
        public BoolSetting DisableOfflineNetwork;

        private TimeSpan measurementStart;
        private string currentMeasurement = "";
        private string previousMeasurement = "";
        private readonly List<KeyValuePair<string, TimeSpan>> measurements = new List<KeyValuePair<string, TimeSpan>>();
        private readonly List<KeyValuePair<string, TimeSpan>> measurementsAvg = new List<KeyValuePair<string, TimeSpan>>();
        private TimeSpan measurementTotal = TimeSpan.Zero;
        private int measurementCount = 0;
        private TimeSpan lastMeasurementUpdate = TimeSpan.Zero;
        private TextDraw statsTextDraw;

        private Performance() : base("Performance")
        {
            Enabled = AddBool("enabled", true);
            ShowStats = AddBool("show stats", false);

            ShowStats.Tooltip =
                "Shows measures of the exact durations of each step in the game's update cycle in milliseconds." +
                "The duration it takes to render these stats is excluded for better comparability.";

            NewCategory("framerate");
            Framelimit = AddInt("framelimit", -1, -1, 2000);
            FramelimitMethod = AddInt("framelimit method", -1, -1, 3);
            PreciseTime = AddBool("precise time", false);
            MaximumTimerResolution = AddBool("maximum timer resolution", false);

            Framelimit.Tooltip =
                "framelimit, as in the +framelimit launch argument (-1 for default, below 30 and above 300 cannot be used in multiplayer.)";
            FramelimitMethod.Tooltip =
                "framelimit method, as in the +framelimitmethod launch argument:\n" +
                "-1: default\n" +
                "0: calls Thread.Yield() repeatedly\n" +
                "1: calls Thread.Sleep(0) repeatedly\n" +
                "2: calls Thread.Sleep(1) repeatedly\n" +
                "3: calls nothing, basically a spin wait";
            PreciseTime.Tooltip =
                "Use a more precise system clock for smoother framerates (requires Windows 8 or higher).";
            MaximumTimerResolution.Tooltip =
                "Increases the system-wide timer resolution from 1ms to 0.5ms (if supported). " +
                "Should lead to less input lag and smoother framerates.";

            NewCategory("particles");
            TrailResolution = AddInt("trail resolution", 0, 0, 50);
            DisableBubbles = AddBool("disable bubbles", false);
            DisableRemotePlayersAfterimages = AddBool("disable remote players afterimages", false);
            DisableRemotePlayersGoldenTrail = AddBool("disable remote players golden trail", false);
            DisableBrakeParticles = AddBool("disable brake particles", false);
            DisableWallParticles = AddBool("disable wall particles", false);
            DisablePlusParticles = AddBool("disable plus particles", false);
            DisableGlitterParticles = AddBool("disable glitter particles", false);
            DisableDrillSparkParticles = AddBool("disable drill spark particles", false);

            TrailResolution.Tooltip =
                "Time interval in milliseconds for new trail segments to appear. " +
                "\"Quality: Low\" uses 150ms.";
            DisableBubbles.Tooltip =
                "Disabling bubbles gives a good performance boost on maps like SpeedCity Nights. " +
                "The game does a collision detection for each bubble on every frame.";
            DisableRemotePlayersAfterimages.Tooltip =
                "Disables afterimages for remote players and ghosts.";
            DisableRemotePlayersGoldenTrail.Tooltip =
                "Disables the white trail for remote players and ghosts using a golden character."; 
            DisableBrakeParticles.Tooltip =
                "Disables small black particles that appear when braking/jumping/landing on ground.";
            DisableWallParticles.Tooltip =
                "Disables small white particles that appear when climbing on walls.";
            DisablePlusParticles.Tooltip =
                "Disables plus particles that appear when grabbing boost.";
            DisableGlitterParticles.Tooltip =
                "Disables glitter particles that appear when playing a golden character.";
            DisableDrillSparkParticles.Tooltip =
                "Disables drill spark particles that appear when using a drill.";
            
            NewCategory("input");
            FixInputDelay = AddBool("fix input delay", false);
            DisableSteamInputApi = AddBool("disable Steam input API", false);
            EnableControllerId = AddInt("enable controller ID", -1, -2, 16);

            FixInputDelay.Tooltip =
                "There is a consistent input delay of at least 1 frame. " +
                "This setting might break keyboard inputs entirely in some rare occasions, just open and close the settings UI or press ESC in these cases to fix.";
            DisableSteamInputApi.Tooltip =
                "Disables the Steam input API. Might break controller inputs.";
            EnableControllerId.Tooltip =
                "Enables only the specified controller ID. " +
                "Put -1 to enable all and -2 to enable none (-2 for best performance). " +
                "Might break controller inputs.";

            NewCategory("network");
            MultithreadedNetwork = AddBool("multithreaded network", false);
            DisableOfflineNetwork = AddBool("disable offline network", false);

            MultithreadedNetwork.Tooltip =
                "[WARNING: Experimental] Starts a new thread to poll network packets. " +
                "If you experience more crashes than usual, then disable this setting again.";
            DisableOfflineNetwork.Tooltip =
                "Disables unnecessary networking in offline games.";
        }

        public static Performance Instance = new Performance();

        [DllImport("Velo_UI.dll", EntryPoint = "SetMaximumTimerResolution")]
        private static extern void SetMaximumTimerResolution();

        [DllImport("Velo_UI.dll", EntryPoint = "IsTimerResolutionSet")]
        private static extern bool IsTimerResolutionSet();

        [DllImport("Velo_UI.dll", EntryPoint = "ResetTimerResolution")]
        private static extern void ResetTimerResolution();

        private bool resolutionSet = false;
        private TimeSpan lastTimeResolutionSet = TimeSpan.Zero;

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Enabled.Value && MaximumTimerResolution.Value && (!resolutionSet || Velo.RealTime - lastTimeResolutionSet > TimeSpan.FromSeconds(10)))
            {
                SetMaximumTimerResolution();
                lastTimeResolutionSet = Velo.RealTime;
                resolutionSet = true;
            }
            if (!(Enabled.Value && MaximumTimerResolution.Value) && resolutionSet)
            {
                ResetTimerResolution();
                lastTimeResolutionSet = Velo.RealTime;
                resolutionSet = false;
            }

            if (!ShowStats.Value)
                return;

            if (measurementCount >= 1 && (Velo.RealTime - lastMeasurementUpdate >= TimeSpan.FromSeconds(1d)))
            {
                measurementsAvg.Clear();
                foreach (var measurement in measurements)
                {
                    measurementsAvg.Add(new KeyValuePair<string, TimeSpan>(measurement.Key, new TimeSpan(measurement.Value.Ticks / measurementCount)));
                }
                measurements.Clear();
                measurementTotal = measurementsAvg.Select(pair => pair.Value).Aggregate(TimeSpan.Zero, (t1, t2) => t1 + t2);
                measurementCount = 0;
                lastMeasurementUpdate = Velo.RealTime;
            }
            else
                measurementCount++;
        }

        public override void PostRender()
        {
            base.PostRender();

            if (!ShowStats.Value)
                return;

            if (statsTextDraw == null)
            {
                statsTextDraw = new TextDraw()
                {
                    Color = Color.LightGray,
                    HasDropShadow = true,
                    DropShadowColor = Color.Black,
                    DropShadowOffset = Vector2.One,
                    Align = new Vector2(0f, 0f),
                    IsVisible = true
                };
                statsTextDraw.SetFont("UI\\Font\\NotoSans-Regular.ttf:14");
            }

            Measure("");

            statsTextDraw.Scale = CEngine.CEngine.Instance.GraphicsDevice.Viewport.Height / 1080f * Vector2.One;

            RoundingMultiplier roundingMS = new RoundingMultiplier("0.00001");
            RoundingMultiplier roundingFPS = new RoundingMultiplier("0.01");

            string text = "";

            foreach (var measurement in measurementsAvg)
            {
                if (measurement.Key == "idle")
                    continue;
                text += measurement.Key + ": " + roundingMS.ToStringRounded(measurement.Value.TotalMilliseconds) + "\n";
            }
            TimeSpan idleTime = measurementsAvg.Find(pair => pair.Key == "idle").Value;
            text += "total: " + roundingMS.ToStringRounded((measurementTotal - idleTime).TotalMilliseconds) + "\n";
            text += "idle: " + roundingMS.ToStringRounded(idleTime.TotalMilliseconds) + "\n";
            text += "max FPS: " + roundingFPS.ToStringRounded(((measurementTotal - idleTime).Ticks != 0 ? 1d / (measurementTotal - idleTime).TotalSeconds : -1d));

            statsTextDraw.Text = text;

            CAABB bounds = statsTextDraw.Bounds;

            float screenWidth = Velo.GraphicsDevice.Viewport.Width;
            float screenHeight = Velo.GraphicsDevice.Viewport.Height;

            float width = bounds.Width;
            float height = bounds.Height;

            statsTextDraw.Position = EOrientation.BOTTOM_LEFT.GetOrigin(width, height, screenWidth, screenHeight, Vector2.Zero) + new Vector2(16f, -16f);

            statsTextDraw.Draw();

            Measure("Velo");
        }

        public void Measure(string label)
        {
            if (!ShowStats.Value)
                return;

            if (Velo.MainThreadId != Thread.CurrentThread.ManagedThreadId)
                return;

            TimeSpan now = new TimeSpan(Util.UtcNow);

            if (currentMeasurement != "")
            {
                int i = measurements.FindIndex(pair => pair.Key == currentMeasurement);

                if (i != -1)
                {
                    measurements[i] = new KeyValuePair<string, TimeSpan>(currentMeasurement, measurements[i].Value + now - measurementStart);
                }
                else
                {
                    measurements.Add(new KeyValuePair<string, TimeSpan>(currentMeasurement, now - measurementStart));
                }
            }

            previousMeasurement = currentMeasurement;
            currentMeasurement = label;
            measurementStart = now;
        }

        public void MeasurePrevious()
        {
            Measure(previousMeasurement);
        }

        public void ParticleEngineUpdate()
        {
            if (!Velo.Ingame)
                return;

            IEnumerable<Player> players = Main.game.stack.gameInfo.slots.Select(slot => slot.Player).Concat(Ghosts.Instance.All().Skip(1));
            foreach (Player player in players)
            {
                if (player != null)
                {
                    if (Enabled.Value && DisableRemotePlayersAfterimages.Value && !player.slot.LocalPlayer)
                        player.afterImagesParticleEmitterProvider.Active = false;
                    if (Enabled.Value && DisableRemotePlayersGoldenTrail.Value && !player.slot.LocalPlayer)
                        player.trail?.trails.ForEach(trail => trail.Clear());
                    if (Enabled.Value && DisableBrakeParticles.Value)
                    {
                        player.brakeParticleEmitter.Active = false;
                        player.brakeParticleEmitter2.time = 0f;
                        player.brakeParticleEmitter2.Reset();
                        player.brakeParticleEmitter3.Active = false;
                    }
                    if (Enabled.Value && DisableWallParticles.Value)
                        player.wallParticleEmitter.Active = false;
                    if (Enabled.Value && DisablePlusParticles.Value)
                        player.plusParticleEmitter.Active = false;
                    if (Enabled.Value && DisableGlitterParticles.Value)
                    {
                        player.glitterParticleEmitter.Active = false;
                        player.glitterParticleEmitter.Unknown = false;
                    }
                    if (Enabled.Value && DisableDrillSparkParticles.Value)
                        player.drillSparkParticleEmitter.Active = false;
                }
            }
        }

        public long GetFramelimit(long current)
        {
            int framelimit = Framelimit.Value;
            if (framelimit == -1)
                return current;

            if (Velo.Online)
                return 10000000L / Math.Min(Math.Max(framelimit, 30), 300);

            if (framelimit < 10)
                framelimit = 10;
            return 10000000L / framelimit;
        }

        public int GetFramelimitMethod(int current)
        {
            if (FramelimitMethod.Value < 0 || FramelimitMethod.Value > 3)
            {
                if (Enabled.Value && PreciseTime.Value && current == 2)
                    return 3;
                return current;
            }

            if (Enabled.Value && PreciseTime.Value && FramelimitMethod.Value == 2)
                return 3;

            return FramelimitMethod.Value;
        }

        public MessagePools pools = new MessagePools(true, 64);

        public class MessagePoller
        {
            public int channel;
            public List<Velo.Message> messages;

            public MessagePoller(int channel)
            {
                this.channel = channel;
                this.messages = new List<Velo.Message>();
            }
        }

        public HashSet<NetIncomingMessage> messagesVelo = new HashSet<NetIncomingMessage>();

        public List<MessagePoller> pollers = new List<MessagePoller>();

        public Velo.Message PollPacket(MessagePools pools, int channel)
        {
            if (
                !(Enabled.Value &&
                MultithreadedNetwork.Value)
                )
            {
                if (pollers.Count > 0)
                {
                    lock (pollers)
                    {
                        pollers.Clear();
                    }
                }
                if (DisableOfflineNetwork.Value && Enabled.Value && Velo.Ingame && !Velo.Online)
                    return null;
                if (Steamworks.SteamNetworking.IsP2PPacketAvailable(out uint num, channel))
                {
                    NetIncomingMessage msg = pools.CreateIncomingMessage(NetIncomingMessageType.Data, (int)num);
                    Steamworks.SteamNetworking.ReadP2PPacket(msg.PeekDataBuffer(), num, out num, out Steamworks.CSteamID identifier, channel);
                    msg.LengthBits = (int)(num * 8u);
                    return new Velo.Message(msg, identifier);
                }
                else
                    return null;
            }

            foreach (MessagePoller poller in pollers)
            {
                if (poller.channel == channel)
                {
                    lock (poller.messages)
                    {
                        if (poller.messages.Count >= 1)
                        {
                            Velo.Message msg = poller.messages[0];
                            poller.messages.RemoveAt(0);
                            return msg;
                        }
                        else
                            return null;
                    }
                }
            }

            // couldn't find a poller for specified channel, create a new one
            MessagePoller newPoller = new MessagePoller(channel);
            lock (pollers)
            {
                pollers.Add(newPoller);
            }
            Task.Run(() => PollPacketsLoop(newPoller));
            return null;
        }

        public void RecyclePacket(NetIncomingMessage message, MessagePools pools)
        {
            if (!(Enabled.Value &&
                MultithreadedNetwork.Value)
                )
            {
                pools.Recycle(message);
                return;
            }

            if (!messagesVelo.Contains(message))
                return;

            lock (this.pools)
            {
                this.pools.Recycle(message);
            }
        }

        public void PollPacketsLoop(object pollerObj)
        {
            MessagePoller poller = (MessagePoller)pollerObj;
            while (
                Enabled.Value &&
                MultithreadedNetwork.Value
                )
            {
                try
                {
                    while (Steamworks.SteamNetworking.IsP2PPacketAvailable(out uint num, poller.channel))
                    {
                        NetIncomingMessage msg;
                        lock (pools)
                        {
                            msg = pools.CreateIncomingMessage(NetIncomingMessageType.Data, (int)num);
                        }
                        Steamworks.SteamNetworking.ReadP2PPacket(msg.PeekDataBuffer(), num, out num, out Steamworks.CSteamID identifier, poller.channel);
                        msg.LengthBits = (int)(num * 8u);
                        lock (poller.messages)
                        {
                            poller.messages.Add(new Velo.Message(msg, identifier));
                        }
                    }

                    Task.Delay(1).Wait();
                }
                catch (Exception) { }
            }
        }
    }
}
