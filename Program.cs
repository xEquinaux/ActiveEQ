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
using static cotf.Game;
using System.IO;
using SharpDX.Win32;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic.ApplicationServices;
using System.ComponentModel.Design;
using System.Security.Claims;
using Microsoft.Win32;

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
	static int width = 800, height = 400;
	int size = 30;
	int scale => 9600 / width;
	internal static int keyPress;
	bool showTitle = false;
	bool mainMenu = true;
	bool init = false;
	bool init2 = false;
	bool init3 = false;
	bool initCapture = false;
	bool[] hold = new bool[8];
	internal static EQ[] eq = new EQ[8];
	Pen pen = new Pen(Color.GreenYellow, 4f);
	internal static Point MouseScreen;
	BiQuadFilter[] filter = new BiQuadFilter[8];
	WasapiCapture capture;
	WaveFileWriter record;
	BinaryWriter write;
	BinaryReader read;
	FileStream file;
	DialogBox dialog;
	DialogBox eula;

	public override void LoadResources()
	{
	}

	public override void Initialize()
	{
		eq[0].position.X = (int)(100 * 0.82M);
		eq[1].position.X = 150  / 1;
		eq[2].position.X = 400  / 2;
		eq[3].position.X = 800  / 3;
		eq[4].position.X = 1200 / 4;     // 280
		eq[5].position.X = 2400 / 6;	 // 400
		eq[6].position.X = 4800 / 8;
		eq[7].position.X = 9600 / 12;    // 800
		for (int i = 0; i < eq.Length; i++)
		{
			eq[i].position.Y = height / 2;
		}
		if (!init2)
		{
			init2 = true;
			Init();
		}
		eula = DialogBox.CreateResource("End-user License Agreement (EULA)", 
			"The end-user holds no responsiblity over the party this software was purchased from nor the party\n" +
			"who designed this sofware regarding the functions of this program such as faults that can be claimed\n" +
			"to damage other software on the user's hardware, the OS (operating system) of the user or the\n" +
			"hardware this software runs on.\n\n" +
			"" +
			"If this software was activated via a product key, the user also agrees not to proliferate the software\n" +
			"key that was granted upon purchase of this software to other individuals. The key, if delivered upon\n" +
			"purchase of this software, may be used only by the individual -- the solitary individual that purchased\n" +
			"this software -- indefinitely and as many times, and on as many machines, as the user owns.\n\n" +
			"" +
			"By clicking \"Yes\", you agree to this EULA.");
		eula.save.text = "Yes";
		eula.load.text = "No";
		eula.cancel.active = false;
		dialog = DialogBox.CreateResource();
	}
	public override void Update()
	{
		if (!init)
		{
			init = true;
			WindowHandle = Utility.FindWindowByCaption(IntPtr.Zero, "SharpDX Render Window");
			eula.Show();
		}
		Utility.RECT window = default;
		Utility.GetWindowRect(WindowHandle, ref window);
		var mouse = System.Windows.Forms.Control.MousePosition;
		Vector2 point = new Vector2(mouse.X, mouse.Y) - new Vector2(window.Left, window.Top) - new Vector2(5, 30f);
		MouseScreen = new Point((int)Math.Max(point.X - (float)Game.Position.X, 0f), (int)Math.Max(point.Y - (float)Game.Position.Y, 0f));//  -5 to X coord, -30 to Y coord due to WPF factor

		int keyPress = 0;
		if (!showTitle && CotF_dev.Keyboard.IsKeyPressed((int)VIRTUALKEY.VK_RETURN))
		{
			showTitle = true;
		}
		if (CotF_dev.Keyboard.IsKeyPressed((int)VIRTUALKEY.VK_ESCAPE))
		{
			Environment.Exit(1);
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
		else if (CotF_dev.Keyboard.IsKeyPressed((int)VIRTUALKEY.VK_X))
		{
			Initialize();
		}
		else if (
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_1) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_2) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_3) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_4) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_5) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_6) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_7) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_8) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_9)
		){   // save
			try
			{
				string name = "Stored_parameters_" + keyPress + ".sav";
				string message = "Here are the options for save/load.\nYou could manually delete the saved settings files, \nor use this instead to access data.\n\n" +
				"Save = save/overwrite the " + name + " file.\n" +
				"Load = load the " + name + "file.\n" +
				"Cancel = do nothing.";
				dialog.Show("Save/load dialog", message);
			}
			catch (Exception e)
			{
				dialog.Show("Exception thrown", e.Message);
				return;
			}
		}
		eula.Update();
		dialog.Update();
		for (int i = 0; i < eq.Length; i++)
		{
			if (hold.All(t => !t) && LeftMouse() && eq[i].hitbox(size).Contains(MouseScreen))
			{
				hold[i] = true;
			}
			if (!LeftMouse())
			{
				hold[i] = false;
			}
			if (hold[i])
			{ 
				var rec = Interface.Drag(eq[i].hitbox(size), MouseScreen, LeftMouse());
				eq[i].position = new Point(rec.X, rec.Y);
				if (eq[0].position.X <= size / 2)
				{
					eq[0].position.X = size / 2;
				}
				if (eq[7].position.X >= width)
				{
					eq[7].position.X = width;
				}
				if (eq[i].position.Y >= height)
				{
					eq[i].position.Y = height;
				}
				if (eq[i].position.Y <= 0)
				{
					eq[i].position.Y = 0;
				}
				/*	Movement limit
				if (i == 0)
				{
					if (eq[0].position.X >= eq[1].position.X)
					{
						eq[0].position.X = eq[1].position.X;
					}
				}
				else if (i == eq.Length - 1)
				{
					if (eq[eq.Length - 1].position.X <= eq[eq.Length - 2].position.X)
					{
						eq[eq.Length - 1].position.X = eq[eq.Length - 2].position.X;
					}
				}
				else if (i > 0 && i < eq.Length - 1)
				{
					if (eq[i].position.X >= eq[i + 1].position.X - size / 2)
					{
						eq[i].position.X = eq[i + 1].position.X - size / 2;
					}
					if (eq[i].position.X <= eq[i - 1].position.X + size / 2)
					{
						eq[i].position.X = eq[i - 1].position.X + size / 2;
					}
				} */
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
		initCapture = false;
	}

	private void Capture_DataAvailable(object sender, WaveInEventArgs e)
	{
		if (!initCapture)
		{
			initCapture = true;
			InitWriter();
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
			float sample = (float)((e.Buffer[index + 1] << 8) |
									e.Buffer[index + 0]);
			// to floating point
			float sample32 = sample/32768f;
			// absolute value 
			if (sample32 < 0) sample32 = -sample32;
			// is this the max value?
			if (sample32 > max) max = (float)Math.Clamp(sample32 - 1d, 0d, 1d);
		}
	}


	private void InitWriter()
	{
		DateTime now = DateTime.Now;
		string name = "Recorded-on_" + $"{now.ToString().Replace('/', '-').Replace(':', '-')}" + ".wav";
		record = new WaveFileWriter(name, capture.WaveFormat);
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
						buffered.Graphics.FillRectangle(Brushes.Black, new Rectangle(0, 0, width, height));
						buffered.Graphics.DrawRectangle(Pens.White, 0, 0, 1, 1);
						buffered.Graphics.FillRectangle(Brushes.Green, new Rectangle(0, height - 24, (int)(width * max), 24));
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
						buffered.Graphics.DrawCurve(pen, point.ToList().OrderBy(t => t.X).ToArray());
						for (int i = 0; i < eq.Length; i++)
						{
							buffered.Graphics.FillRoundedRectangle(Brushes.White, eq[i].hitbox(size), new Size(5, 5));
							buffered.Graphics.DrawString((i + 1).ToString(), new Font("Helvetica", 12f), Brushes.Gray, eq[i].hitbox(size).Left, eq[i].hitbox(size).Top);
							buffered.Graphics.DrawString($"{eq[i].Frequency()}, {Math.Round(eq[i].Gain(height), 2)}", new Font("Helvetica", 12f), Brushes.Gray, eq[i].hitbox(size).Left, eq[i].hitbox(size).Bottom);
						}
						if (initCapture)
						{ 
							buffered.Graphics.DrawString("Recording on", new Font("Helvetica", 18f), Brushes.White, new Point(0, height - 50));
						}
						else
						{
							buffered.Graphics.DrawString("Recording off", new Font("Helvetica", 18f), Brushes.Gray, new Point(0, height - 50));
						}
						buffered.Graphics.DrawString("Commands: R to start recording, S to stop recording, X to reset, 1-9 to save, ESC to close", new Font("Helvetica", 12f), Brushes.Gray, new Point(0, height - 24));
						dialog.DrawDialog(buffered.Graphics);
						eula.DrawDialog(buffered.Graphics);
					}
					else this.TitleScreen(buffered.Graphics);
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
		public int Frequency(int width = 800, int maxFreq = 9600) => (int)(X * (maxFreq / width) * ((float)X / width));
		public float Gain(float height = 400) => (float)(position.Y - height / 2f) / height * -2f * 10f;
		public int X => position.X;
		public int Y => position.Y;
		public Point position;
		public Rectangle hitbox(int size = 30) => new Rectangle(position.X - size / 2, position.Y - size / 2, size, size);
	}

	public class DialogBox
	{
		public bool active; 
		public bool closed;
		public int width, height, left, top, padding;
		public string? heading;
		public string? message;
		public Button? load, save, cancel;
		public static DialogBox CreateResource()
		{
			var box = new DialogBox();
			box.left = 20;
			box.width = Game.width;
			box.height = Game.height;
			box.padding = 40;
			box.load = new Button("Save", new Rectangle(box.left, Game.height, 80, 24)) { parent = box };
			box.save = new Button("Load", new Rectangle(box.left + 84, Game.height, 80, 24)) { parent = box };
			box.cancel = new Button("Cancel", new Rectangle(box.left + 168, Game.height, 80, 24)) { parent = box };
			return box;
		}
		public static DialogBox CreateResource(string heading, string message)
		{
			var box = new DialogBox();
			box.heading = heading;
			box.message = message;
			box.left = 20;
			box.width = Game.width;
			box.height = Game.height;
			box.padding = 40;
			box.load = new Button("Save", new Rectangle(box.left, Game.height - 32, 80, 24)) { parent = box };
			box.save = new Button("Load", new Rectangle(box.left + 84, Game.height - 32, 80, 24)) { parent = box };
			box.cancel = new Button("Cancel", new Rectangle(box.left + 168, Game.height - 32, 80, 24)) { parent = box };
			return box;
		}
		public void Update()
		{
			if (active)
			{
				load?.Update();
				save?.Update();
				if (cancel.active)
				cancel?.Update();
			}
		}
		public void DrawDialog(Graphics graphics)
		{
			if (active)
			{
				graphics.FillRectangle(Brushes.DarkGray, 0, 0, width, height);
				graphics.DrawString(heading, new Font("Helvetica", 14f), Brushes.White, new Point(left, top));
				graphics.DrawString(message, new Font("Helvetica", 12f), Brushes.White, new Point(left, top + padding));
				load?.Draw(graphics);
				save?.Draw(graphics);
				if (cancel.active)
				cancel?.Draw(graphics);
			}
		}
		public void Show()
		{
			active = true;
		}
		public void Show(string heading, string message)
		{
			this.heading = heading;
			this.message = message;
			active = true;
		}
	}
	public class Button
	{
		public bool active = true;
		public int margin = 2;
		int top, right, bottom, left;
		public string? text;
		public Rectangle hitbox;
		public DialogBox? parent;
		BinaryWriter? write;
		BinaryReader? read;
		FileStream? file;
		public Button(string text, Rectangle hitbox)
		{
			this.text = text;
			this.hitbox = hitbox;
			top = hitbox.Top;
			right = hitbox.Right;
			bottom = hitbox.Bottom;
			left = hitbox.Left;
		}
		public void Update()
		{
			if (parent != null && parent.active)
			{ 
				if (LeftMouse() && hitbox.Contains(Game.MouseScreen))
				{
					switch (text)
					{
						case "Yes":
							RegistryKey? reg = Registry.CurrentUser?.OpenSubKey("Software", writable: true);
							RegistryKey? key = reg?.CreateSubKey("Circle Prefect").CreateSubKey("seer");
							if (bool.TryParse(key?.GetValue("register", false).ToString(), out var flag))
							{
								key.SetValue("register", true);
							}
							reg?.Close();
							goto default;
						case "No":
							Environment.Exit(1);
							break;
						case "Load":
							file = new FileStream("Stored_parameters_" + keyPress.ToString() + ".sav", FileMode.Open);
							read = new BinaryReader(file);
							for (int i = 0; i < eq.Length; i++)
							{
								eq[i].position = new Point(read.ReadInt32(), read.ReadInt32());
							}
							read.Dispose();
							file.Dispose();
							goto default;
						case "Save":
							file = new FileStream("Stored_parameters_" + keyPress.ToString() + ".sav", FileMode.Create);
							write = new BinaryWriter(file);
							for (int i = 0; i < eq.Length; i++)
							{
								write.Write(eq[i].X);
								write.Write(eq[i].Y);
							}
							write.Dispose();
							file.Dispose();
							goto default;
						case "Cancel":
							goto default;
						default:
							if (parent != null)
							parent.active = false;
							break;
					}
				}
			}
		}
		public void Draw(Graphics graphics)
		{		   
			if (parent != null && parent.active)
			{ 
				graphics.FillRectangle(Brushes.White, hitbox = new Rectangle(left + margin, top + margin - 32, hitbox.Width, hitbox.Height));
				graphics.DrawString(text, new Font("Helvetica", 11f), Brushes.Black, new Point(left + margin, top + margin - 32));
			}
		}
	}
}