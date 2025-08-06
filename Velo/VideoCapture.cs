using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Velo
{
    public struct CaptureParams
    {
        public int Width;
        public int Height;
        public int CaptureRate;
        public int VideoRate;
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

        private int frame = 0;
        private int batch = 0;

        Task writeTask;

        public VideoCapturer(CaptureParams captureParams)
        {
            this.captureParams = captureParams;
            buffer = new byte[captureParams.Width * captureParams.Height * 4];
            bmp_stride = captureParams.Width * 3;
            bmp_stride = (bmp_stride + 3) / 4;
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
            //Velo.GraphicsDevice.SetRenderTarget(target);
        }

        public void PostPresent()
        {
            if (!capture)
                return;
            target.GetData(buffer);

            writeTask?.Wait();

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
                using (FileStream stream = File.Create("test.bmp"))
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
                    stream.Write((short)32);
                    stream.Write(0);
                    stream.Write(bmp_buffer.Length);
                    stream.Write(0);
                    stream.Write(0);
                    stream.Write(0);
                    stream.Write(0);

                    stream.WriteArrFixed(bmp_buffer, bmp_buffer.Length);
                }
            });

            frame++;
            capture = false;
            copies = 0;
            Velo.GraphicsDevice.SetRenderTarget(null);
            Velo.RenderTarget = null;
        }
    }
}
