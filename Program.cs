using REWD.D2D;
using REWD.FoundationR;
using SharpDX;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Security.AccessControl;
using CotF_dev;
using SharpDX.Direct2D1;
using Font = System.Drawing.Font;
using System.Numerics;
using cotf.Base;
using ActiveEQ;
using NAudio.Dsp;
using SharpDX.Direct3D11;
using System.Text.RegularExpressions;
using DeviceContext = SharpDX.Direct2D1.DeviceContext;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System;
using System.Threading;
using Buffer = System.Buffer;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static REWD.FoundationR.Foundation;

namespace cotf;

internal class Program
{
	static void Main(string[] args)
	{
		new Game();
	}
}

public class Game : Direct2D
{
	public static Game Instance;
	static BufferedGraphicsContext context = BufferedGraphicsManager.Current;

	private static Point _position;
	private static Point _oldPosition;
	public static Point Position => _position;
	public static Camera CAMERA = new Camera();

	public IntPtr WindowHandle;

	public Game() : base(800, 400)
	{
		Instance = this;
	}

	float max = 0;
	int width = 800, height = 400;
	int size = 30;
	bool showTitle = false;
	bool mainMenu = true;
	bool init = false;
	bool initCapture = false;
	EQ[] eq = new EQ[8];
	Pen pen = new Pen(Color.GreenYellow, 4f);
	Point MouseScreen;
	BiQuadFilter[] filter = new BiQuadFilter[8];
	WasapiCapture capture;
	WaveFileWriter record;

	public override void LoadResources()
	{
	}

	public override void Initialize()
	{
		eq[0].position.X = 100;
		eq[1].position.X = 200;
		eq[2].position.X = 400;
		eq[3].position.X = 800;
		eq[4].position.X = 1200;
		eq[5].position.X = 2400;
		eq[6].position.X = 4800;
		eq[7].position.X = 9600;
		for (int i = 0; i < eq.Length; i++)
		{
			eq[i].position.Y = height / 2;
		}
		Init();
	}

	public override void Update()
	{
		if (!init)
		{
			init = true;
			WindowHandle = Utility.FindWindowByCaption(IntPtr.Zero, "SharpDX Render Window");
		}

		Utility.RECT window = default;
		Utility.GetWindowRect(WindowHandle, ref window);
		var mouse = System.Windows.Forms.Control.MousePosition;
		Vector2 point = new Vector2(mouse.X, mouse.Y) - new Vector2(window.Left, window.Top) - new Vector2(5, 30f);
		MouseScreen = new Point((int)Math.Max(point.X - (float)Game.Position.X, 0f), (int)Math.Max(point.Y - (float)Game.Position.Y, 0f));//  -5 to X coord, -30 to Y coord due to WPF factor

		if (!showTitle && CotF_dev.Keyboard.IsKeyPressed((int)VIRTUALKEY.VK_RETURN))
		{
			showTitle = true;
		}

		if (CotF_dev.Keyboard.IsKeyPressed((int)VIRTUALKEY.VK_R))
		{
			if (!initCapture && capture.CaptureState != CaptureState.Capturing && capture.CaptureState != CaptureState.Starting && capture.CaptureState != CaptureState.Stopping)
			{
				capture.StartRecording();
			}
		}
		else if (CotF_dev.Keyboard.IsKeyPressed((int)VIRTUALKEY.VK_S))
		{
			if (initCapture && capture.CaptureState == CaptureState.Capturing)
			{
				initCapture = false;
				capture.StopRecording();
			}
		}

		for (int i = 0; i < eq.Length; i++)
		{
			if (LeftMouse() && eq[i].hitbox(size).Contains(MouseScreen))
			{
				eq[i].position = Interface.RelativeMouse(eq[i].hitbox(size), MouseScreen);
				if (eq[0].position.X <= size)
				{
					eq[0].position.X = size;
				}
				if (eq[7].position.X >= width)
				{
					eq[7].position.X = width - size;
				}
				if (i == 0)
				{
					if (eq[0].position.X >= eq[1].position.X)
					{
						eq[0].position.X = eq[1].position.X - size;
					}
				}
				else if (i == eq.Length - 1)
				{
					if (eq[eq.Length - 1].position.X <= eq[eq.Length - 2].position.X)
					{
						eq[eq.Length - 1].position.X = eq[eq.Length - 2].position.X + size;
					}
				}
				else if (i > 0 && i < eq.Length - 1)
				{
					if (eq[i].position.X >= eq[i + 1].position.X)
					{
						eq[i].position.X = eq[i + 1].position.X - size;
					}
					if (eq[i].position.X <= eq[i - 1].position.X)
					{
						eq[i].position.X = eq[i - 1].position.X + size;
					}
				}
				break;
			}
		}

		var rate = capture.WaveFormat.SampleRate;
		for (int i = 0; i < eq.Length; i++)
		{
			if (filter[i] == null)
				filter[i] = BiQuadFilter.PeakingEQ(rate, eq[i].Frequency(), 0.8f, eq[i].Gain(height));
			else
				filter[i].SetPeakingEq(rate, eq[i].Frequency(), 0.8f, eq[i].Gain(height));
		}
	}

	public void Init()
	{
		var input = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
		capture = new WasapiCapture(input, false);
		capture.ShareMode = AudioClientShareMode.Shared;
		capture.DataAvailable += Capture_DataAvailable;
		capture.RecordingStopped += Capture_RecordingStopped;
	}

	private void Capture_RecordingStopped(object? sender, StoppedEventArgs e)
	{
		// stop recording here
		record.Close();
	}

	private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
	{
		if (!initCapture)
		{
			initCapture = true;
			var date = DateTime.Now;
			record = new WaveFileWriter($"{date.ToString().Replace("\\", "").Replace("/", "")}", capture.WaveFormat);
		}
		float[] read = new float[e.BytesRecorded];
        Buffer.BlockCopy(e.Buffer, 0, read, 0, e.BytesRecorded);
        for (int j = 0; j < read.Length; j++)
        {
            for (int n = 0; n < filter.Length; n++)
            {
                if (filter[n] != null)
                {
                    read[j] = filter[n].Transform(read[j]);
                }
            }
        }
        byte[] buffer = new byte[e.BytesRecorded];
        Buffer.BlockCopy(read, 0, buffer, 0, e.BytesRecorded);
		// record here
		record.Write(buffer, 0, buffer.Length);

		// interpret as 16 bit audio
		for (int index = 0; index < e.BytesRecorded; index += 2)
		{
			short sample = (short)((e.Buffer[index + 1] << 8) |
									e.Buffer[index + 0]);
			// to floating point
			var sample32 = sample/32768f;
			// absolute value 
			if (sample32 < 0) sample32 = -sample32;
			// is this the max value?
			if (sample32 > max) max = sample32;
		}
	}

	public override void Draw(DeviceContext rt)
	{
		using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height))
		{
			using (Graphics graphics = Graphics.FromImage(bmp))
			using (BufferedGraphics buffered = context.Allocate(graphics, new Rectangle(0, 0, width, height)))
			{
				SetQuality(buffered.Graphics, new System.Drawing.Rectangle(0, 0, width, height));
				graphics.Clear(System.Drawing.Color.CornflowerBlue);
				{
					if (!mainMenu)
					{
						graphics.FillRectangle(Brushes.Black, new Rectangle(0, 0, width, height));
						graphics.FillRectangle(Brushes.Green, new Rectangle(0, height - 30, (int)(width * max), 30));
						Point[] point = new Point[]
						{
							new Point(0, height / 2),
							eq[0].position,
							eq[1].position,
							eq[2].position,
							eq[3].position,
							eq[4].position,
							eq[5].position,
							eq[6].position,
							eq[7].position,
							new Point(width, height / 2)
						};
						graphics.DrawCurve(pen, point);
						for (int i = 0; i < eq.Length; i++)
						{
							graphics.FillRoundedRectangle(Brushes.White, eq[i].hitbox(size), new Size(5, 5));
							graphics.DrawString((i + 1).ToString(), new Font("Helvetica", 12f), Brushes.Gray, eq[i].hitbox(size).Left, eq[i].hitbox(size).Top);
							graphics.DrawString($"{eq[i].Frequency}, {eq[i].Gain(height)}", new Font("Helvetica", 12f), Brushes.Gray, eq[i].hitbox(size).Left, eq[i].hitbox(size).Bottom);
						}
						if (initCapture)
						{ 
							graphics.DrawString("Recording on", new Font("Helvetica", 18f), Brushes.White, new Point(0, height - 42));
						}
						else
						{
							graphics.DrawString("Recording off", new Font("Helvetica", 18f), Brushes.Gray, new Point(0, height - 42));
						}
						graphics.DrawString("Commands: R to start recording, S to stop recording", new Font("Helvetica", 14f), Brushes.Gray, new Point(0, height - 24));
					}
					else this.TitleScreen(graphics);
				}
				buffered.Render();
			}
			var surface = ConvertBitmap(bmp, deviceContext);
			rt.DrawBitmap(surface, 1f, BitmapInterpolationMode.NearestNeighbor);
			surface.Dispose();
		}
	}

	private SharpDX.Direct2D1.Bitmap ConvertBitmap(System.Drawing.Bitmap bitmap, SharpDX.Direct2D1.DeviceContext deviceContext)
	{
		var bitmapProperties = new SharpDX.Direct2D1.BitmapProperties(
			new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied));

		var bitmapData = bitmap.LockBits(
			new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
			System.Drawing.Imaging.ImageLockMode.ReadOnly,
			System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

		using (var dataStream = new SharpDX.DataStream(bitmapData.Scan0, bitmapData.Stride * bitmap.Height, true, false))
		{
			var sharpDxBitmap = new SharpDX.Direct2D1.Bitmap(deviceContext, new Size2(bitmap.Width, bitmap.Height), dataStream, bitmapData.Stride, bitmapProperties);
			bitmap.UnlockBits(bitmapData);
			return sharpDxBitmap;
		}
	}

	void ResizeImageToWidth(int targetWidth, ref int srcWidth, ref int srcHeight)
	{
		float aspectRatio = (float)srcHeight / srcWidth;
		int calculatedHeight = (int)(targetWidth * aspectRatio);

		srcWidth = targetWidth;
		srcHeight = calculatedHeight;
	}
	private void TitleScreen(Graphics graphics)
	{
		if (!showTitle)
		{
			int w = width;
			int h = height;
			ResizeImageToWidth(width, ref w, ref h);
			int centerHeight = (width / 2 - h / 2) / 2;
			graphics.DrawString("ActiveEQ", new Font("Helvetica", 16f), Brushes.Purple, new Point(0, centerHeight / 2 - 8));
			graphics.DrawString("Press Enter", new Font("Helvetica", 12f), Brushes.Gray, new Point(0, height - centerHeight / 2 - 6));
			//graphics.DrawImage(titleScreen, new Rectangle(0, centerHeight, w, h));
		}
		else mainMenu = false;
	}
	#region quality settings
	public CompositingQuality compositingQuality = CompositingQuality.HighQuality;
	public CompositingMode compositingMode = CompositingMode.SourceOver;
	public System.Drawing.Drawing2D.InterpolationMode interpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
	public TextRenderingHint textRenderHint = TextRenderingHint.ClearTypeGridFit;
	public GraphicsUnit graphicsUnit = GraphicsUnit.Pixel;
	public SmoothingMode smoothingMode = SmoothingMode.AntiAlias;
	private void SetQuality(Graphics graphics, System.Drawing.Rectangle bounds)
	{
		graphics.CompositingQuality = compositingQuality;
		graphics.CompositingMode = compositingMode;
		graphics.InterpolationMode = interpolationMode;
		graphics.TextRenderingHint = textRenderHint;
		//graphics.RenderingOrigin = new Point(bounds.X, bounds.Y);
		//graphics.Clip = new System.Drawing.Region(bounds);
		graphics.PageUnit = graphicsUnit;
		graphics.SmoothingMode = smoothingMode;
	}
	#endregion

	public static bool LeftMouse()
	{
		return Input.IsLeftPressed();
	}
	public static bool RightMouse()
	{
		return Input.IsRightPressed();
	}

	public struct EQ
	{
		public int Frequency(int width = 800, int maxFreq = 9600) => (X + maxFreq) / width;
		public float Gain(float height = 400) => (position.Y - height / 2) / height * -2;
		public int X => position.X;
		public int Y => position.Y;
		public Point position;
		public Rectangle hitbox(int size = 30) => new Rectangle(position.X - size / 2, position.Y - size / 2, size, size);
	}
}