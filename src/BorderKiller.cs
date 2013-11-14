using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;

namespace FFS {
	class FakeFullscreen {	
		/// <summary>
		/// Finds a window by process name.
		/// </summary>
		/// <param name="FindString">The name of the process to search for, minus extension</param>
		/// <param name="FoundWindow">A Window value representing the found Window, null if no window was found</param>
		/// <returns>True if a window is found, false if not</returns>
		public static bool TryFindWindowByProcess(string FindString, out Window FoundWindow) {
			Process[] ProcessList = Process.GetProcesses();

			foreach (Process proc in ProcessList) {
				if (FindString.ToLower() == proc.ProcessName.ToLower()) {
					FoundWindow = new Window(proc.MainWindowHandle);
					return true;
				}
			}

			FoundWindow = null; return false;
		}

		/// <summary>
		/// Finds a window by process name.
		/// </summary>
		/// <param name="FindString">The name of the process to search for, minus extension</param>
		public static Window FindWindowByProcess(string FindString) {
			Window OutWnd;

			if (TryFindWindowByProcess(FindString, out OutWnd)) {
				return OutWnd;
			}

			return new Window();
		}

		/// <summary>
		/// Finds a window by process ID
		/// </summary>
		/// <param name="PID">The PID to search for</param>
		/// <param name="FoundWindow">A Window value representing the found Window, null if no window was found</param>
		/// <returns>True if a window is found, false if not</returns>
		public static bool TryFindWindowByProcessID(int PID, out Window FoundWindow) {
			try {
				FoundWindow = new Window(Process.GetProcessById(PID).MainWindowHandle);
				return true;
			}
			catch (System.ArgumentException) {
				FoundWindow = null; return false;
			}
		}

		/// <summary>
		/// Finds a window by process ID
		/// </summary>
		/// <param name="PID">The PID to search for</param>
		public static Window FindWindowByProcessID(int PID) {
			Window OutWnd;

			if (TryFindWindowByProcessID(PID, out OutWnd)) {
				return OutWnd;
			}

			return new Window();
		}

		/// <summary>
		/// Finds a window by title, accepts wildcards
		/// </summary>
		/// <param name="FindString">The title of the window to search for, supports * as wildcard</param>
		/// <param name="FoundWindow">A Window value representing the found Window, null if no window was found</param>
		/// <returns>True if a window is found, false if not</returns>
		public static bool TryFindWindowByTitle(string FindString, out Window FoundWindow) {
			Process[] ProcessList = Process.GetProcesses();

			foreach (Process proc in ProcessList) {
				if (Like(proc.MainWindowTitle, FindString)) {
					FoundWindow = new Window(proc.MainWindowHandle);
					return true;
				}
			}

			FoundWindow = null; return false;
		}

		/// <summary>
		/// Finds a window by title, accepts wildcards
		/// </summary>
		/// <param name="FindString">The title of the window to search for, supports * as wildcard</param>
		public static Window FindWindowByTitle(string FindString) {
			Window OutWnd;

			if (TryFindWindowByTitle(FindString, out OutWnd)) {
				return OutWnd;
			}

			return new Window();
		}

		/// <summary>
		/// Runs an executable and returns a Window representing its main window
		/// </summary>
		/// <param name="RunPath">The executable to run</param>
		/// <param name="RunArgs">Arguments to pass to the executable</param>
		/// <returns></returns>
		public static Window RunExe(string RunPath, string RunArgs = "") {
			//Sets up the ProcessStartInfo for --run with arguments from --args
			ProcessStartInfo ProcSI = new ProcessStartInfo(RunPath, RunArgs);
			ProcSI.UseShellExecute = false;

			//Get the parent directory of the executable from --run and set the PSI's working dir to it
			string WDir = Directory.GetParent(RunPath).FullName;
			ProcSI.WorkingDirectory = WDir;

			//Then start the process
			Process PRunning = Process.Start(ProcSI);

			//Then wait a second to allow the process to actually GET a window handle
			System.Threading.Thread.Sleep(1000);

			//Before returning the new Window
			return new Window(PRunning.MainWindowHandle);
		}

		#region Helper functions
		/// <summary>
		/// Compares input to pattern, allowing wildcards in pattern
		/// </summary>
		/// <param name="input">The string to compare to pattern</param>
		/// <param name="pattern">The string to use as a pattern, supports wildcards with *</param>
		/// <returns></returns>
		private static bool Like(string input, string pattern) {
			string tmp = pattern.Replace("*", ".*");
			return System.Text.RegularExpressions.Regex.IsMatch(input, tmp, RegexOptions.IgnoreCase);
		}
		
		/// <summary>
		/// Converts a string in the format "WIDTHxHEIGHT" to a Size(WIDTH, HEIGHT) 
		/// </summary>
		/// <param name="input">The input string in WIDTHxHEIGHT format</param>
		/// <returns></returns>
		public static Size WxHToSize(string input = "") {
			if (String.IsNullOrEmpty(input)) {
				return new Size(Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height);
			}
			else {
				//Matches NUMBERxNUMBER, with support for negative values, and puts the left side of the x in group w and the right in group h
				Match SizeMatch = Regex.Match(input, @"(?<w>(?:-|)[0-9]*)x(?<h>(?:-|)[0-9]*)");
				return new Size(Convert.ToInt32(SizeMatch.Groups["w"].Value.ToString())
								, Convert.ToInt32(SizeMatch.Groups["h"].Value.ToString()));
			}
		}

		/// <summary>
		/// Converts a string in the format "XxY" to a Point(X, Y)
		/// </summary>
		/// <param name="input">The input string in XxY format</param>
		/// <returns></returns>
		public static Point XxYToPoint(string input = "") {
			if (String.IsNullOrEmpty(input)) {
				return new Point(0, 0);
			}
			else {
				//Same as the regex above, but XxY and groups x/y
				Match SizeMatch = Regex.Match(input, @"(?<x>(?:-|)[0-9]*)x(?<y>(?:-|)[0-9]*)");
				return new Point(Convert.ToInt32(SizeMatch.Groups["x"].Value.ToString())
								, Convert.ToInt32(SizeMatch.Groups["y"].Value.ToString()));
			}
		}
		#endregion
	}


	class Window {
		private IntPtr WindowHandle = IntPtr.Zero;

		public Window() { }
		public Window(IntPtr HWND) {
			this.WindowHandle = HWND;
		}

		/// <summary>
		/// Removes a window's borders.
		/// </summary>
		/// <param name="LeaveResizeHandle">Leaves the window's resize handle intact, allowing it to still be resized</param>
		public void MakeBorderless(bool LeaveResizeHandle = false) {
			//Gets the window's style flags using the WinAPI GetWindowLong() function
			uint lstyle = Externs.GetWindowLong(this.WindowHandle, Externs.GWL_STYLE);

			//Removes the window's borders, leaving the resize handle if LeaveResizeHandle is true
			if (LeaveResizeHandle)
				lstyle &= ~((uint)Externs.WS.CAPTION);
			else
				lstyle &= ~((uint)Externs.WS.CAPTION | (uint)Externs.WS.SIZEFRAME | (uint)Externs.WS.MINIMIZE | (uint)Externs.WS.MAXIMIZE | (uint)Externs.WS.SYSMENU | (uint)Externs.WS.DLGFRAME);

			//Applies the new window style
			Externs.SetWindowLong(this.WindowHandle, Externs.GWL_STYLE, lstyle);
			//Updates the window's inner frame via a call to SetWindowPos(), which is used for moving and resizing forms, including windows.
			//Note the SWP_FRAMECHANGED flag, which sends WM_NCCALCSIZE to the window, which in turn causes the window to recalculate its client area
			//Normally this would be sent with SWP_NOMOVE AND SWP_NOSIZE, but I began having strange issues where if I didn't resize the window at all, WM_NCCALCSIZE did absolutely nothing for some reason, so the window's client area never updated
			//This is bad because, well, the entire point of this line is to update the window's client area.
			//SWP_NOSIZE/SWP_NOMOVE just cause SetWindowPos() to not actually do anything with the new window size/position it is passed. If you look at Resize() you'll see SWP_NOMOVE and Move() has SWP_NOSIZE.
			Externs.SetWindowPos(this.WindowHandle, IntPtr.Zero, 0, 0, CurrentSize.Width + 1, CurrentSize.Height + 1, Externs.SetWindowPosFlags.SWP_FRAMECHANGED | Externs.SetWindowPosFlags.SWP_NOMOVE | Externs.SetWindowPosFlags.SWP_NOZORDER | Externs.SetWindowPosFlags.SWP_NOOWNERZORDER);
		}

		/// <summary>
		/// Resizes the window.
		/// </summary>
		/// <param name="WindowSize">The size to resize the window to</param>
		public void Resize(Size NewSize) {
			Externs.SetWindowPos(this.WindowHandle, IntPtr.Zero, 0, 0, NewSize.Width, NewSize.Height, Externs.SetWindowPosFlags.SWP_NOMOVE | Externs.SetWindowPosFlags.SWP_NOZORDER | Externs.SetWindowPosFlags.SWP_NOOWNERZORDER);
		}

		/// <summary>
		/// Moves the window.
		/// </summary>
		/// <param name="WindowPosition">The X/Y position to move the window to</param>
		public void Move(Point NewPosition) {
			Externs.SetWindowPos(this.WindowHandle, IntPtr.Zero, NewPosition.X, NewPosition.Y, 0, 0, Externs.SetWindowPosFlags.SWP_FRAMECHANGED | Externs.SetWindowPosFlags.SWP_NOSIZE | Externs.SetWindowPosFlags.SWP_NOZORDER | Externs.SetWindowPosFlags.SWP_NOOWNERZORDER);
		}

		/// <summary>
		/// Returns the window handle as IntPtr.
		/// </summary>
		public IntPtr Handle {
			get { return WindowHandle; }
		}

		public string Title {
			get { return Externs.GetWindowTextRaw(this.WindowHandle); }
		}

		/// <summary>
		/// Returns the window's Rectangle
		/// .Width is actually .Right and .Height is actually .Bottom
		/// </summary>
		private Rectangle WindowRect {
			get {
				Rectangle WRect;
				Externs.GetWindowRect(this.WindowHandle, out WRect);
				return WRect;
			}
		}
		/// <summary>
		/// Returns the window's current screen position.
		/// </summary>
		public Point CurrentPosition {
			get { return new Point(WindowRect.X, WindowRect.Y); }
		}

		/// <summary>
		/// Returns the window's current Width and Height.
		/// </summary>
		public Size CurrentSize {
			//You would expect this to just be .Width/.Height, but no, they are actually .Right and .Bottom due to converting from a winapi RECT to a .NET Rectangle.
			get { return new Size(WindowRect.Width - WindowRect.Left, WindowRect.Height - WindowRect.Top); }
		}
	}

	class Externs {
		//Window messages enum
		//For sending to windows with SendMessage
		public enum WM : uint {
			/// <summary>
			/// An application sends a WM_GETTEXT message to copy the text that corresponds to a window into a buffer provided by the caller.
			/// </summary>
			GETTEXT = 0x000D,
			/// <summary>
			/// An application sends a WM_GETTEXTLENGTH message to determine the length, in characters, of the text associated with a window.
			/// </summary>
			GETTEXTLENGTH = 0x000E
		}
		//Window styles enum
		//These define, mostly, a window's borders; whether it has a caption or not, whether it is resizable, etc.
		[Flags]
		public enum WS : uint {
			/// <summary>The window has a thin-line border.</summary>
			BORDER = 0x800000,
			/// <summary>The window has a title bar (includes the BORDER style).</summary>
			CAPTION = 0xc00000,
			/// <summary>The window has a border of a style typically used with dialog boxes. A window with this style cannot have a title bar.</summary>
			DLGFRAME = 0x400000,
			/// <summary>The window is initially maximized.</summary>
			MAXIMIZE = 0x1000000,
			/// <summary>The window has a maximize button. Cannot be combined with the EX_CONTEXTHELP style. The SYSMENU style must also be specified.</summary>
			MAXIMIZEBOX = 0x10000,
			/// <summary>The window is initially minimized.</summary>
			MINIMIZE = 0x20000000,
			/// <summary>The window has a minimize button. Cannot be combined with the EX_CONTEXTHELP style. The SYSMENU style must also be specified.</summary>
			MINIMIZEBOX = 0x20000,
			/// <summary>The window has a sizing border.</summary>
			SIZEFRAME = 0x40000,
			/// <summary>The window has a window menu on its title bar. The CAPTION style must also be specified.</summary>
			SYSMENU = 0x80000
		}
		//SWP flags for use with, surprise, SetWindowPos
		//They just change what SetWindowPos does, like SWP_NOMOVE will make it not attempt to move the window
		[Flags]
		public enum SetWindowPosFlags : uint {
			/// <summary>
			///     Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's size is being changed.
			/// </summary>
			SWP_FRAMECHANGED = 0x0020,
			/// <summary>
			///     Retains the current position (ignores X and Y parameters).
			/// </summary>
			SWP_NOMOVE = 0x0002,
			/// <summary>
			///     Does not change the owner window's position in the Z order.
			/// </summary>
			SWP_NOOWNERZORDER = 0x0200,
			/// <summary>
			///     Retains the current size (ignores the cx and cy parameters).
			/// </summary>
			SWP_NOSIZE = 0x0001,
			/// <summary>
			///     Retains the current Z order (ignores the hWndInsertAfter parameter).
			/// </summary>
			SWP_NOZORDER = 0x0004
		}

		//GetWindowLong flags for simply getting the window's style/exstyle
		public const int GWL_STYLE = -16;

		//Most of these externs are documented far better than I could ever document them on pinvoke.net and MSDN and a thousand other sites
		//So I won't go into too much detail

		//Set/Get WindowLong are used with GWL_STYLE to set/get the window's style
		[DllImport("user32.dll")]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

		//Sets the position, size and Z-order of a window depending on which flags (SetWindowPosFlags) are passed to it
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

		//Sends a window message to a given form handle, used with, say, WM_GETTEXT to get a form's text (like GetWindowText)
		//It can do a hell of a lot more than that and is a major part of windows in general but that's about all I'm using it for here
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, [Out] StringBuilder lParam);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowRect(IntPtr hwnd, out System.Drawing.Rectangle lpRect);

		//Uses SendMessage to get and return a window's title in a string
		//I only wrote this to avoid doing the below 4 lines or similar with GetWindowText a bunch of times
		public static string GetWindowTextRaw(IntPtr hwnd) {
			int length = (int)SendMessage(hwnd, (int)WM.GETTEXTLENGTH, IntPtr.Zero, null);
			StringBuilder sb = new StringBuilder(length + 1);
			SendMessage(hwnd, (int)WM.GETTEXT, (IntPtr)sb.Capacity, sb);
			return sb.ToString();
		}
	}	
}
