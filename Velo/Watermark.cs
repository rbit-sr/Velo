using Microsoft.Xna.Framework;

namespace Velo
{
    public class Watermark : Module
    {
        private TextDraw textDraw;

        private Watermark() : base("Watermark") { }

        public static Watermark Instance = new Watermark();

        public override void PostRender()
        {
            base.PostRender();

            if (!Velo.Ingame || Velo.Online || (RecordingAndReplay.Instance.Recorder is TASRecorder) && !RecordingAndReplay.Instance.IsPlaybackRunning)
                return;

            string text = "";

            if (!RecordingAndReplay.Instance.IsPlaybackRunning)
            {
                if (Velo.get_time_scale() != 1f)
                    text += "\nx" + Velo.get_time_scale();

                if (OfflineGameMods.Instance.IsModded() || BlindrunSimulator.Instance.Enabled.Value.Enabled)
                    text += "\nmodded";

                if (OfflineGameMods.Instance.DtFixed)
                    text += "\nTAS";

                if ((Velo.RealTime - OfflineGameMods.Instance.SavestateLoadTime).TotalSeconds <= 0.5f)
                    text += "\nload";
            }
            if (
                RecordingAndReplay.Instance.IsPlaybackRunning &&
                OfflineGameMods.Instance.WatermarkType.Value == OfflineGameMods.EWatermarkType.TAS &&
                RecordingAndReplay.Instance.PlaybackRecording is Timeline
            )
            {
                text = "\nTAS";
            }
            /*else if (!OfflineGameMods.Instance.IsOwnPlaybackFromLeaderboard())
            {
                text = "\nreplay";
            }*/

            if (text.Length > 0)
            {
                if (textDraw == null)
                {
                    textDraw = new TextDraw
                    {
                        Color = Color.Red,
                        Offset = new Vector2(32, 32)
                    };
                    textDraw.SetFont("UI\\Font\\ariblk.ttf:24");
                }
                text = text.Remove(0, 1);
                
                textDraw.Text = text;
                textDraw.IsVisible = true;
                textDraw.HasDropShadow = true;
                textDraw.DropShadowColor = Color.Black;
                textDraw.DropShadowOffset = Vector2.One;
                textDraw.Scale = CEngine.CEngine.Instance.GraphicsDevice.Viewport.Height / 1080f * Vector2.One;
            
                textDraw.Draw();
            }
        }
    }
}
