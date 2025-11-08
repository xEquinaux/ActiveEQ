using System;
using System.Runtime.InteropServices;

namespace CotF_dev;

public class KeyBoard
{
	private static IntPtr _hookID = IntPtr.Zero;

	/*static void Main(string[] args)
	{
		_hookID = SetHook(HookCallback);
		Console.WriteLine("Press any key to exit...");
		Console.ReadKey();
		UnhookWindowsHookEx(_hookID);
	}*/

	public static void Init()
	{
		_hookID = SetHook(HookCallback);
		
	}
	public static void Dispose()
	{
		UnhookWindowsHookEx(_hookID);
	}

	private static IntPtr SetHook(LowLevelKeyboardProc proc)
	{
		using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
		using (var curModule = curProcess.MainModule)
		{
			return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
				GetModuleHandle(curModule.ModuleName), 0);
		}
	}

	private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

	private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode >= 0)
		{
			Console.WriteLine("Key pressed!");
		}
		return CallNextHookEx(_hookID, nCode, wParam, lParam);
	}

	private const int WH_KEYBOARD_LL = 13;

	[DllImport("user32.dll")]
	private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

	[DllImport("user32.dll")]
	private static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("user32.dll")]
	private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr GetModuleHandle(string lpModuleName);
}
