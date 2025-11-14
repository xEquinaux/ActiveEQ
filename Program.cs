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
using SharpDX.DXGI;
using System.Security.Policy;
using tUserInterface.ModUI;
using ListBox = tUserInterface.ModUI.ListBox;

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

	static string saveFileName = "0";
	float max = 0;
	static int width = 800, height = 400;
	int buttonWidth = 80;
	int buttonHeight = 20;
	int left = 20;
	int yOff = 50;
	int xOff = 90;
	int yOff2 = 30;
	int size = 30;
	int scale => 9600 / width;
	static int monitorKeyPress;
	internal static int keyPress;
	static bool leftMouse;
	static bool showTitle = false;
	static bool mainMenu = true;
	static bool init = false;
	static bool init2 = false;
	static bool init3 = false;
	static bool initCapture = false;
	static bool initMonitor = false;
	bool[] hold = new bool[8];
	static bool livePlayBack = false;
	static bool isRecording = false;
	internal static EQ[] eq = new EQ[8];
	Pen pen = new Pen(Color.GreenYellow, 4f);
	internal static Point MouseScreen;
	BiQuadFilter[] filter = new BiQuadFilter[8];
	static WasapiCapture? capture;
	static WaveFileWriter? record;
	static BinaryWriter? write;
	static BinaryReader? read;
	static FileStream? file;
	static DialogBox? dialog;
	static DialogBox? eula;
	static DialogBox? devices;
	static BufferedWaveProvider? bufferGated;
	static WasapiOut? monitorAudio;
	static MMDevice? deviceCapture;
	static MMDevice? deviceRender;
	static Scroll? scrollOne;
	static Scroll? scrollTwo;
	static Button[]? selection;

	public override void LoadResources()
	{
	}

	public void ResetParameters()
	{
		eq[0].position.X = (int)(100 * 0.82M);
		eq[1].position.X = 150 / 1;
		eq[2].position.X = 400 / 2;
		eq[3].position.X = 800 / 3;
		eq[4].position.X = 1200 / 4;     // 280
		eq[5].position.X = 2400 / 6;     // 400
		eq[6].position.X = 4800 / 8;
		eq[7].position.X = 9600 / 12;    // 800
		for (int i = 0; i < eq.Length; i++)
		{
			eq[i].position.Y = height / 2;
		}
	}

	public override void Initialize()
	{
		selection = new Button[]
		{
			new Button(ButtonID.Record, "Record", new Rectangle(left, height - yOff2, buttonWidth, buttonHeight)),
			new Button(ButtonID.Capture, "Capture", new Rectangle(left + xOff, height - yOff2, buttonWidth, buttonHeight)),
			new Button(ButtonID.Monitor, "Monitor", new Rectangle(left + xOff * 2, height - yOff2, buttonWidth, buttonHeight)),
			new Button(ButtonID.Devices, "Devices", new Rectangle(left + xOff * 3, height - yOff2, buttonWidth, buttonHeight)),
			new Button(ButtonID.Reset, "Reset", new Rectangle(left + xOff * 4, height - yOff2, buttonWidth, buttonHeight)),
			new Button(ButtonID.SaveLoad, "Save/Load", new Rectangle(left + xOff * 5, height - yOff2, buttonWidth, buttonHeight)),
			new Button(ButtonID.Quit, "Quit", new Rectangle(left + xOff * 6, height - yOff2, buttonWidth, buttonHeight))
		};

		MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
		deviceRender = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications);
		deviceCapture = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);

		ResetParameters();

		if (!init2)
		{
			init2 = true;
			Instance = this;
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

		devices = DialogBox.CreateResource("Select Input and Output Devices",
			"Use the up and down arrow keys when the mouse is positions over the options to scroll\n" +
			"The first list is input (capture), and the second list is output (render):");
		devices.save.text = "Back";
		devices.load.active = false;
		devices.cancel.active = false;

		RecollectDeviceList();
	}

	private void RecollectDeviceList()
	{
		int left = 20;

		MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
		MMDeviceCollection input = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
		MMDeviceCollection output = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

		var list1 = new tUserInterface.ModUI.Button[input.Count];
		for (int i = 0; i < list1.Length; i++)
		{
			list1[i] = new tUserInterface.ModUI.Button(input[i].DeviceFriendlyName, new Rectangle(0, 0, 300, 14), Brushes.Black);
		}
		var list2 = new tUserInterface.ModUI.Button[output.Count];
		for (int i = 0; i < list2.Length; i++)
		{
			list2[i] = new tUserInterface.ModUI.Button(output[i].DeviceFriendlyName, new Rectangle(0, 0, 300, 14), Brushes.Black);
		}

		var _one = new Rectangle(left, 100, 300, 100);
		devices.inputDevices = new ListBox(_one = new Rectangle(left, 100, 300, 100), scrollOne = new Scroll(_one), list1);
		var _two = new Rectangle(left, 220, 300, 100);
		devices.outputDevices = new ListBox(_one = new Rectangle(left, 200, 300, 100), scrollTwo = new Scroll(_two), list2);
	}

	public override void Update()
	{
		if (!init)
		{
			init = true;
			WindowHandle = Utility.FindWindowByCaption(IntPtr.Zero, "Main Window");
			RegistryKey reg = Registry.CurrentUser.OpenSubKey("Software", writable: true);
			RegistryKey key = reg.CreateSubKey("Circle Prefect").CreateSubKey("seer");
			if (bool.TryParse(key.GetValue("register", false).ToString(), out var flag))
			{
				if (!flag)
				{
					eula?.Show();
				}
			}
			reg.Close();
		}
		Utility.RECT window = default;
		Utility.GetWindowRect(WindowHandle, ref window);
		var mouse = System.Windows.Forms.Control.MousePosition;
		Vector2 point = new Vector2(mouse.X, mouse.Y) - new Vector2(window.Left, window.Top) - new Vector2(5, 30f);
		MouseScreen = new Point((int)Math.Max(point.X - (float)Game.Position.X, 0f), (int)Math.Max(point.Y - (float)Game.Position.Y, 0f));//  -5 to X coord, -30 to Y coord due to WPF factor

		if (!showTitle)
		{
			if (Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_RETURN))
			{
				showTitle = true;
			}
		}


		if (selection != null)
		{ 
			foreach (var item in selection)
			{
				item.Update();
			}
		}
		if (monitorKeyPress > 0 && !LeftMouse())
		{
			monitorKeyPress = 0;
		}

		if (dialog != null && dialog.active &&
		   (Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_1) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_2) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_3) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_4) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_5) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_6) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_7) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_8) ||
			Keyboard.IsKeyPressed(keyPress = (int)VIRTUALKEY.VK_9))
		)
		{   // save
			saveFileName = "Stored_parameters_" + keyPress + ".sav";
		}

		eula?.Update();
		dialog?.Update();
		devices?.Update();

		//	Listbox not exactly working very well
		if (devices != null && devices.active)
		{
			foreach (var button in devices.inputDevices.item)
			{
				if (button.LeftClick(MouseScreen, LeftMouse()))
				{
					if (deviceCapture != null)
						deviceCapture.Dispose();
					deviceCapture = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).First(t => t.DeviceFriendlyName == button.text);
					devices.Close();
					Init();
				}
			}
			foreach (var button in devices.outputDevices.item)
			{
				if (button.LeftClick(MouseScreen, LeftMouse()))
				{
					if (deviceRender != null)
						deviceRender.Dispose();
					deviceRender = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).First(t => t.DeviceFriendlyName == button.text);
					devices.Close();
					Init();
				}
			}
		}

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
		if (capture != null)
		{
			capture.StopRecording();
			capture.Dispose();
		}
		if (monitorAudio != null)
		{
			monitorAudio.Stop();
			monitorAudio.Dispose();
		}
		if (bufferGated != null)
		{
			bufferGated.ClearBuffer();
		}
		capture = new WasapiCapture(deviceCapture, false);
		capture.ShareMode = AudioClientShareMode.Shared;
		capture.DataAvailable += Capture_DataAvailable;
		capture.RecordingStopped += Capture_RecordingStopped;
		bufferGated = new BufferedWaveProvider(capture.WaveFormat);
		bufferGated.DiscardOnBufferOverflow = true;
		monitorAudio = new WasapiOut(deviceRender, AudioClientShareMode.Shared, useEventSync: false, 0);
	}

	private void Capture_RecordingStopped(object? sender, StoppedEventArgs e)
	{
		// stop recording here
		record?.Close();
		initCapture = false;
	}

	private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
	{
		if (!initMonitor)
		{
			initMonitor = true;
			monitorAudio?.Init(bufferGated);
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
		if (bufferGated != null)
		{ 
			bufferGated.BufferLength = e.BytesRecorded;
			if (livePlayBack)
			{
				bufferGated.AddSamples(buffer, 0, e.BytesRecorded);
			}
			else
			{
				bufferGated.ClearBuffer();
			}
		}

		//DataHandler(read);

		// record here
		if (isRecording)
		{
			if (!initCapture)
			{
				initCapture = true;
				InitWriter();
			}
			record?.Write(buffer, 0, buffer.Length);
		}

		// interpret as 16 bit audio
		for (int index = 0; index < e.BytesRecorded; index += 2)
		{
			float sample = (float)((e.Buffer[index + 1] << 8) |
									e.Buffer[index + 0]);
			// to floating point
			float sample32 = sample / 32768f;
			// absolute value 
			if (sample32 < 0) sample32 = -sample32;
			// is this the max value?
			if (sample32 > max) max = (float)Math.Clamp(sample32 - 1d, 0d, 1d);
		}
	}

	private void DataHandler(float[] samples)
	{
		for (int i = 0; i < samples.Length; i += 2)
		{
			byte[] liveCopy = new byte[2]
			{
				(byte)((uint)(samples[i]     * 2.1474836E+09f) >> 16),
				(byte)((uint)(samples[i + 1] * 2.1474836E+09f) >> 24)
			};
			if (livePlayBack)
			{
				bufferGated.AddSamples(liveCopy, 0, liveCopy.Length);
				bufferGated.BufferLength = 2;
			}
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
						buffered.Graphics.FillRectangle(new System.Drawing.SolidBrush(Color.FromArgb(30, 30, 30)), new Rectangle(0, 0, width, height));
						for (int i = 0; i < 5; i++)
						{ 
							int offset = 0;
							int bottomY = -20;
							buffered.Graphics.DrawLine(new Pen(Color.FromArgb(60, 60, 60), 1f), new Point(0, i * (height / 4)), new Point(width, i * (height / 4)));
							switch (i)
							{
								case 0:
									buffered.Graphics.DrawString("10 db", new Font("Helvetica", 11f), Brushes.Gray, new Point(0, offset));
									break;
								case 1:
									buffered.Graphics.DrawString("5 db", new Font("Helvetica", 11f), Brushes.Gray, new Point(0, offset + (height / 4 * 1)));
									break;
								case 2:
									buffered.Graphics.DrawString("0 db", new Font("Helvetica", 11f), Brushes.Gray, new Point(0, offset + (height / 4 * 2)));
									break;
								case 3:
									buffered.Graphics.DrawString("-5 db", new Font("Helvetica", 11f), Brushes.Gray, new Point(0, offset + (height / 4 * 3)));
									break;
								case 4:
									buffered.Graphics.DrawString("-10 db", new Font("Helvetica", 11f), Brushes.Gray, new Point(0, bottomY + (height / 4 * 4)));
									break;
							}
						}
						buffered.Graphics.FillRectangle(Brushes.Green, new Rectangle(0, height - 24, (int)(width * max), 10));
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
						if (selection != null)
						{ 
							foreach (var item in selection)
							{
								item.Draw(buffered.Graphics);
							}
						}
						dialog?.DrawDialog(buffered.Graphics);
						eula?.DrawDialog(buffered.Graphics);
						devices?.DrawDialog(buffered.Graphics);
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
		public ListBox? inputDevices;
		public ListBox? outputDevices;
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
				if (load != null && load.active)
					load?.Update();
				if (save != null && save.active)
					save?.Update();
				if (cancel != null && cancel.active)
					cancel?.Update();
				if (inputDevices != null)
					inputDevices.Update(MouseScreen, LeftMouse());
				if (outputDevices != null)
					outputDevices.Update(MouseScreen, LeftMouse());
				if (this == dialog)
				{
					dialog.message = 
						"Here are the options for save/load.\n" +
						"You could manually delete the saved settings files, " +
						"\nor use this instead to access data.\n" +
						"Press 1-9 to change which file to save to.\n\n" +
						"" +
						"Save = save/overwrite the " + saveFileName + " file.\n" +
						"Load = load the " + saveFileName + " file.\n" +
						"Cancel = do nothing.";
				}
			}
		}
		public void DrawDialog(Graphics graphics)
		{
			if (active)
			{
				graphics.FillRectangle(Brushes.DarkGray, 0, 0, width, height);
				graphics.DrawString(heading, new Font("Helvetica", 14f), Brushes.White, new Point(left, top));
				graphics.DrawString(message, new Font("Helvetica", 12f), Brushes.White, new Point(left, top + padding));
				if (load != null && load.active)
					load?.Draw(graphics);
				if (save != null && save.active)
					save?.Draw(graphics);
				if (cancel != null && cancel.active)
					cancel?.Draw(graphics);

				if (inputDevices != null)
				{
					inputDevices.DrawItemsNoIcon(graphics, MouseScreen, new Font("Helvetica", 12f));
					inputDevices.scroll.Draw(graphics, Brushes.White);
				}
				if (outputDevices != null)
				{
					outputDevices.DrawItemsNoIcon(graphics, MouseScreen, new Font("Helvetica", 12f));
					outputDevices.scroll.Draw(graphics, Brushes.White);
				}
			}
		}
		public void Show()
		{
			active = true;
		}
		public void Close()
		{
			active = false;
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
		public bool enabled = false;
		public bool active = true;
		public short id;
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
		public Button(short id, string text, Rectangle hitbox)
		{
			this.text = text;
			this.hitbox = hitbox;
			this.id = id;
			top = hitbox.Top;
			right = hitbox.Right;
			bottom = hitbox.Bottom;
			left = hitbox.Left;
		}
		public void Update()
		{
			if ((parent != null && parent.active) || (id != 0 && active))
			{
				if (monitorKeyPress == 0 && LeftMouse() && hitbox.Contains(Game.MouseScreen))
				{
					monitorKeyPress++;
					switch (id)
					{
						default:
							break;
						case ButtonID.None:
							break;
						case ButtonID.Quit:
							Environment.Exit(1);
							return;
						case ButtonID.Monitor:
							if (monitorAudio?.PlaybackState == PlaybackState.Stopped)
							{
								livePlayBack = true;
								monitorAudio?.Play();
							}
							else if (monitorAudio?.PlaybackState == PlaybackState.Playing)
							{
								livePlayBack = false;
								monitorAudio?.Stop();
							}
							enabled = livePlayBack;
							break;
						case ButtonID.Capture:
							if (capture?.CaptureState == CaptureState.Capturing)
								capture?.StopRecording();
							else
							{ 
								capture?.StartRecording();
							}
							enabled = capture?.CaptureState == CaptureState.Starting;
							break;
						case ButtonID.Record:
							isRecording = !isRecording;
							if (capture?.CaptureState == CaptureState.Capturing)
							{
								initCapture = false;
							}
							enabled = isRecording;
							break;
						case ButtonID.Reset:
							Instance.ResetParameters();
							break;
						case ButtonID.Devices:
							Instance.RecollectDeviceList();
							devices?.Show();
							break;
						case ButtonID.SaveLoad:
							try
							{
								string name = "Stored_parameters_" + keyPress + ".sav";
								string message = "Here are the options for save/load.\nYou could manually delete the saved settings files, \nor use this instead to access data.\n\n" +
								"Save = save/overwrite the " + saveFileName + " file.\n" +
								"Load = load the " + saveFileName + " file.\n" +
								"Cancel = do nothing.";
								dialog?.Show("Save/load dialog", message);
							}
							catch (Exception e)
							{
								dialog?.Show("Exception thrown", e.Message);
								return;
							}
							break;
					}
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
							if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), saveFileName)))
							{
								file = new FileStream(saveFileName, FileMode.Open);
								read = new BinaryReader(file);
								for (int i = 0; i < eq.Length; i++)
								{
									eq[i].position = new Point(read.ReadInt32(), read.ReadInt32());
								}
								read.Dispose();
								file.Dispose();
							}
							goto default;
						case "Save":
							file = new FileStream(saveFileName, FileMode.Create);
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
						case "Back":
							if (devices != null)
								devices.active = false;
							break;
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
			else if (id != 0)
			{	 
				graphics.FillRectangle(enabled ? Brushes.LightBlue : Brushes.White, hitbox = new Rectangle(left + margin, top + margin - 32, hitbox.Width, hitbox.Height));
				graphics.DrawString(text, new Font("Helvetica", 11f), Brushes.Black, new Point(left + margin, top + margin - 32));
			}
		}
	}
	public static class ButtonID
	{
		public const short
			None = 0,
			Record = 1,
			Monitor = 2,
			Devices = 3,
			Quit = 4,
			SaveLoad = 5,
			Reset = 6,
			Capture = 7;
	}
}