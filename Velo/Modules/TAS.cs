using System;
using System.Windows.Forms;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Velo
{    
    public class TAS : Module
    {
        public IntSetting DeltaTime;
        public BoolSetting FixDeltaTime;
        public HotkeySetting FreezeKey;
        public HotkeySetting Step1Key;
        public HotkeySetting Step10Key;
        public HotkeySetting StartRecordingKey;
        public HotkeySetting StopRecordingKey;
        public HotkeySetting StartPlaybackKey;
        public HotkeySetting StopPlaybackKey;

        public ToggleSetting Test;

        private bool frozen = false;
        private int stepCount = 0;

        public Recording recCurrent = new Recording();
        public Recording recLast = new Recording();

        private Recorder recorder = new Recorder();
        private Playback playback = new Playback();

        private TAS() : base("TAS")
        {
            NewCategory("delta time");
            FixDeltaTime = AddBool("fix delta time", false);
            DeltaTime = AddInt("delta time", 33333, 0, 666666);

            CurrentCategory.Tooltip =
                "The delta time is the measured time difference between subsequent frames. " +
                "This value is used for physics calculations to determine for example how far " +
                "to move the player on the current frame. " +
                "As the delta times depends on measurements, " +
                "they are highly inconsistent and lead to indeterminism in the game's physics.";
            FixDeltaTime.Tooltip =
                "Fixes the delta time to a constant value, making the game's physics 100% deterministic " +
                "(excluding randomized events like item pickups and bot behavior).";

            NewCategory("hotkeys");
            FreezeKey = AddHotkey("freeze key", 0x97);
            Step1Key = AddHotkey("step 1 key", 0x97);
            Step10Key = AddHotkey("step 10 key", 0x97);
            StartRecordingKey = AddHotkey("start recording key", 0x97);
            StopRecordingKey = AddHotkey("stop recording key", 0x97);
            StartPlaybackKey = AddHotkey("start playback key", 0x97);
            StopPlaybackKey = AddHotkey("stop playback key", 0x97);
            Test = AddToggle("test", new Toggle());

            FreezeKey.Tooltip =
                "Toggles the game between a frozen and unfrozen state. " +
                "When frozen, the game stops doing any physics updates.";
            Step1Key.Tooltip =
                "Steps 1 frame forward. Automatically freezes the game.";
            Step10Key.Tooltip =
                "Steps 10 frames forward. Automatically freezes the game.";
            StartRecordingKey.Tooltip = "[Experimental]";
            StopRecordingKey.Tooltip = "[Experimental]";
            StartPlaybackKey.Tooltip = "[Experimental]";
            StopPlaybackKey.Tooltip = "[Experimental]";

            playback.Interpolate = true;
            playback.UseGhost = false;
        }

        public static TAS Instance = new TAS();

        public bool DtFixed 
        { 
            get
            {
                if (!Velo.Ingame || Velo.Online)
                    return false;

                return FixDeltaTime.Value || stepCount > 0 || frozen || (playback != null && playback.DtFixed); 
            }
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (!Velo.IngamePrev && Velo.Ingame)
            {
                recorder.Start(recCurrent);
            }
            if (Velo.IngamePrev && !Velo.Ingame)
            {
                recorder.Stop();
                playback.Stop();
            }
            if (Velo.LapFinish)
            {
                recLast = recCurrent;
                recLast.Info.PlayerId = Steamworks.SteamUser.GetSteamID().m_SteamID;
                recLast.Info.MapId = Map.GetCurrentMapId();
                recLast.Info.Category = (int)recLast.Rules.GetCategory();
                recLast.Info.RunTime = (int)(Velo.TimerPrev * 1000f);
                Leaderboard.Instance.OnRunFinished(recLast);

                recCurrent = new Recording();
                recorder.Start(recCurrent);
            }

            if (!Velo.Ingame || Velo.Online)
            {
                frozen = false;
                stepCount = 0;
                return;
            }

            if (Keyboard.Pressed[FreezeKey.Value])
            {
                if (!frozen)
                    frozen = true;
                else
                    frozen = false;
            }

            if (Keyboard.Pressed[Step1Key.Value])
            {
                stepCount = 1;
            }

            if (Keyboard.Pressed[Step10Key.Value])
            {
                stepCount = 10;
            }

            /*if (Keyboard.Pressed[StartRecording.Value])
            {
                playback.Stop();
                recorder.Start(recCurrent);
                Leaderboard.Instance.PushNotification("record start");
            }
            if (Keyboard.Pressed[StopRecording.Value])
            {
                recorder.Stop();
                Leaderboard.Instance.PushNotification("record stop");
            }*/
            if (Keyboard.Pressed[StartPlaybackKey.Value])
            {
                StartPlayback(recLast);
            }
            if (Keyboard.Pressed[StopPlaybackKey.Value])
            {
                StopPlayback();
            }
            if (Test.Modified())
            {
                //Velo.MainPlayer.gameInfo.setOption(2, Test.Value.Enabled);
                //Velo.MainPlayer.gameInfo.options[2] = false;
            }

            if (DtFixed)
            {
                CEngine.CEngine cengine = CEngine.CEngine.Instance;
                long delta;
                if (frozen && stepCount == 0)
                    delta = 0;
                else
                    delta = DeltaTime.Value;

                long time = cengine.gameTime.TotalGameTime.Ticks;
                if (!(cengine.isHoldingElapsedTime || cengine.isHoldingTotalTime || cengine.pausedLocal)) 
                    time += delta;
                cengine.gameTime = new GameTime(new TimeSpan(time), new TimeSpan(delta));
            }

            recorder.PreUpdate();
            playback.PreUpdate();

            if (Velo.Timer < Velo.TimerPrev)
                printed = false;

            if (!printed)
            {
                foreach (var reason in recCurrent.Rules.OneLapReasons)
                    Console.WriteLine(reason.Value);
                foreach (var reason in recCurrent.Rules.Violations)
                    Console.WriteLine(reason.Value);
            }
            if (recCurrent.Rules.OneLapReasons.Count > 0 || recCurrent.Rules.Violations.Count > 0)
            {
                printed = true;
            }
        }

        private static bool printed = false;
        
        public override void PostUpdate()
        {
            base.PostUpdate();

            if (!Velo.Ingame || Velo.Online)
                return;

            recorder.PostUpdate();
            playback.PostUpdate();

            if (stepCount > 0)
            {
                stepCount--;
                if (stepCount == 0)
                    frozen = true;
            }
        }

        public void StartPlayback(Recording recording)
        {
            recorder.Stop();
            playback.Start(recording);
            Notifications.Instance.PushNotification("playback start");
        }

        public void StopPlayback()
        {
            playback.Stop();
            recorder.Start(recCurrent);
            Notifications.Instance.PushNotification("playback stop");
        }

        public bool SetInputs()
        {
            return playback.SetInputs();
        }

        public bool SkipIfGhost()
        {
            return playback.SkipIfGhost();
        }
    }
}
