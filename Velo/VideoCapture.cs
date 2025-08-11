using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Velo
{
    public struct CaptureParams
    {
        public int Width;
        public int Height;
        public int CaptureRate;
        public int VideoRate;
        public string PixelFormat;
        public string Preset;
        public int Crf;
        public string Filename;
    }

    public class VideoCapturer
    {
        private CaptureParams captureParams;
        public CaptureParams CaptureParams => captureParams;

        private readonly byte[] buffer;
        private readonly byte[] bmp_buffer;
        private readonly int bmp_stride;
        private readonly RenderTarget2D target;
        private readonly Texture2D texture;

        private bool capture = false;
        private int copies = 0;
        private int copiesLeft = 0;

        private int frame = 0;
        private int batch = 0;

        Task writeTask;
        Process ffmpeg;

        public VideoCapturer(CaptureParams captureParams)
        {
            this.captureParams = captureParams;
            buffer = new byte[captureParams.Width * captureParams.Height * 4];
            bmp_stride = captureParams.Width * 3;
            bmp_stride = (bmp_stride + 3) / 4 * 4;
            bmp_buffer = new byte[bmp_stride * captureParams.Height];
            target = new RenderTarget2D(Velo.GraphicsDevice, captureParams.Width, captureParams.Height);
            texture = new Texture2D(Velo.GraphicsDevice, captureParams.Width, captureParams.Height);
        }

        public void Capture()
        {
            capture = true;
            copies++;
            Velo.GraphicsDevice.SetRenderTarget(target);
            Velo.RenderTarget = target;
        }

        public void PostRender()
        {
            Velo.GraphicsDevice.SetRenderTarget(null);
            Velo.SpriteBatch.Begin();
            texture.SetData(buffer);
            Velo.SpriteBatch.Draw(texture, Vector2.Zero, Color.White);
            Velo.SpriteBatch.End();
        }

        private void WriteNextBmp()
        {
            if (!Directory.Exists("Velo\\temp_"))
                Directory.CreateDirectory("Velo\\temp_");

            using (FileStream stream = File.Create($"Velo\\temp_\\f{(batch % 2 == 0 ? frame : frame + 50)}.bmp"))
            {
                stream.Write((byte)'B');
                stream.Write((byte)'M');
                stream.Write(14 + 40 + bmp_buffer.Length);
                stream.Write((short)0);
                stream.Write((short)0);
                stream.Write(14 + 40);

                stream.Write(40);
                stream.Write(target.Width);
                stream.Write(-target.Height);
                stream.Write((short)1);
                stream.Write((short)24);
                stream.Write(0);
                stream.Write(bmp_buffer.Length);
                stream.Write(0);
                stream.Write(0);
                stream.Write(0);
                stream.Write(0);

                stream.WriteArrFixed(bmp_buffer, bmp_buffer.Length);
            }

            frame++;
        }

        private void FinishCurrentBatch()
        {
            if (frame == 0)
                return;

            ffmpeg?.WaitForExit();

            ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "cmd.exe";
            ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ffmpeg.StartInfo.CreateNoWindow = true;
            if (captureParams.PixelFormat != "rgb24")
                ffmpeg.StartInfo.Arguments = $"/c ffmpeg.exe -y -r {captureParams.VideoRate} -start_number {(batch % 2 == 0 ? "0" : "50")} -i Velo/temp_/f%d.bmp -frames:v {frame} -vf scale=out_color_matrix=bt709:flags=full_chroma_int+accurate_rnd,format={captureParams.PixelFormat},fps={captureParams.VideoRate} -c:v libx264 -preset {captureParams.Preset} -crf {captureParams.Crf} -tune animation Velo/temp_/b{batch}.mp4";
            else
                ffmpeg.StartInfo.Arguments = $"/c ffmpeg.exe -y -r {captureParams.VideoRate} -start_number {(batch % 2 == 0 ? "0" : "50")} -i Velo/temp_/f%d.bmp -frames:v {frame} -vf format=rgb24,fps={captureParams.VideoRate} -c:v libx264rgb -preset {captureParams.Preset} -crf {captureParams.Crf} -tune animation Velo/temp_/b{batch}.mp4";
            ffmpeg.Start();

            batch++;
            frame = 0;
        }

        public void PostPresent()
        {
            if (!capture)
                return;
            target.GetData(buffer);

            writeTask?.Wait();

            copiesLeft = copies;
            copies = 0;

            for (int i = 0, j = 0, s = 0; i < buffer.Length; i += 4, j += 3, s += 3)
            {
                if (s == captureParams.Width * 3)
                {
                    j += bmp_stride - s;
                    s = 0;
                }

                bmp_buffer[j]     = buffer[i + 2];
                bmp_buffer[j + 1] = buffer[i + 1];
                bmp_buffer[j + 2] = buffer[i];
            }

            writeTask = Task.Run(() =>
            {
                for (; copiesLeft > 0; copiesLeft--)
                {
                    WriteNextBmp();

                    if (frame == 50)
                    {
                        FinishCurrentBatch();
                    }
                }
            });

            capture = false;
            Velo.GraphicsDevice.SetRenderTarget(null);
            Velo.RenderTarget = null;
        }

        public void Finish()
        {
            ConsoleM.Instance.Enabled.Enable();
            ConsoleM.Instance.AppendLine("Finishing...");

            Task.Run(() =>
            {
                writeTask?.Wait();
                FinishCurrentBatch();
                ffmpeg?.WaitForExit();

                using (FileStream stream = File.Create("Velo\\temp_\\blist"))
                {
                    for (int i = 0; i < batch; i++)
                    {
                        byte[] bytes = Encoding.ASCII.GetBytes($"file 'b{i}.mp4'\n");
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }

                if (!Directory.Exists("Velo\\videos"))
                    Directory.CreateDirectory("Velo\\videos");

                ffmpeg = new Process();
                ffmpeg.StartInfo.FileName = "cmd.exe";
                ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                ffmpeg.StartInfo.CreateNoWindow = true;
                ffmpeg.StartInfo.Arguments = $"/c ffmpeg.exe -y -f concat -safe 0 -i Velo/temp_/blist -c copy \"Velo/videos/{captureParams.Filename}\"";
                ffmpeg.Start();
                ffmpeg.WaitForExit();

                Directory.Delete("Velo\\temp_", true);

                Velo.AddOnPreUpdateTS(() => ConsoleM.Instance.AppendLine("Finished capturing!"));
            });

            Velo.RenderTarget = null;
            Velo.GraphicsDevice.SetRenderTarget(null);

            target.Dispose();
            texture.Dispose();
        }
    }
}
