using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using FFS;
using NDesk.Options;

//TODO
// ADD A LOT OF COMMENTS god dammit

namespace BorderKiller {
	class Program {
		static void Main(string[] args) {
			bool  ShowHelp = (args.Length == 0)
				, DoNothing = false
				, LeaveHandles = false
				, FullScreen = false
			;

			string RunArgs = ""
				 , RunPath = ""
				 , PosString = ""
				 , SizeString = ""
			;
		
			int Delay = 0;

			Window UsingWindow = null;
			
			//Set up the various command line arguments in the wonderful NDesk.Options
			var ArgParser = new OptionSet()
				.Add("pid=", "PID of the target process", (int v) => UsingWindow = FakeFullscreen.FindWindowByProcessID(v))
				.Add("pname=", "Target process name\n - Not including file extension!", v => UsingWindow = FakeFullscreen.FindWindowByProcess(v))
				.Add("ptitle=", "Window title of the target window\n - Wildcards are allowed with *", v => UsingWindow = FakeFullscreen.FindWindowByTitle(v))
				.Add("run=", "An executable to run\n\n-At least one of the above args must be used-\n", v => RunPath = v)
				.Add("pos:", "X/Y coords to move the window to", v => PosString = v)
				.Add("size:", "W/H to resize the window to\n - Both of the above use XxY format\n   500x200, for example\n"+
								" - Negative values are allowed for --pos"+
								"\n - Both can also be null"+
								"\n   pos will move to 0x0"+
								"\n   size will use primary screen res\n", v => SizeString = v)
				.Add("f|fs|fullscreen", "The same as --pos --size", v => FullScreen = v != null)
				.Add("n|nothing", "Do nothing to window borders", v => DoNothing = v != null)
				.Add("r|resize", "Leave window resize handles intact", v => LeaveHandles = v != null)
				.Add("a:|args:", "Arguments to pass to --run", v => RunArgs = v)
				.Add("d=|delay=", "Adds a delay before borderlessing\n This is very useful when used with --run", (int v) => Delay = v)
				.Add("v|ver|version", "Print version and exit", v => PrintVersion())
				.Add("?|h|help", "Print this help and exit", v => ShowHelp = v != null)
			;

			//Parse the args, again with NDesk.Options and throw an OptionException if there are any unknown args
			try {
				var unrec = ArgParser.Parse(args);
				if (unrec.Any()) {
					throw new OptionException("Unknown option(s): " + unrec.Aggregate((a, b) => a + " " + b), "Default");
				}
			}
			catch (OptionException X) { //Then handle any OptionExceptions that may show up due to unknown or incorrect arguments
				Console.WriteLine("boki: " + X.Message);
				Console.WriteLine("Try '{0} --help' for more information.", AppDomain.CurrentDomain.FriendlyName);
				Environment.Exit(1);
			}

			//If we've broken something or requested help, show help before anything actually happens
			if (ShowHelp)
				PrintHelp(ArgParser);
			
			if (RunPath != "") {
				try {
					//Sets up the ProcessStartInfo for --run with arguments from --args
					ProcessStartInfo ProcSI = new ProcessStartInfo(RunPath, RunArgs);
					ProcSI.UseShellExecute = false;

					//Get the parent directory of the executable from --run and set the PSI's working dir to it
					string WDir = Directory.GetParent(RunPath).FullName;
					ProcSI.WorkingDirectory = WDir;

					Console.WriteLine("boki: Running {0} {1}", RunPath, RunArgs);

					//Then start the process
					Process PRunning = Process.Start(ProcSI);

					//Then wait a second to allow the process to actually GET a window handle
					System.Threading.Thread.Sleep(1000);

					//Before assigning it to UsingWindow
					UsingWindow = new Window(PRunning.MainWindowHandle);
				}
				catch (System.ComponentModel.Win32Exception X) { //This should only happen with an invalid --run path or with inadequate permissions (I hope!)
					Console.WriteLine("boki: Error running {0}: {1}", RunPath, X.Message);
				}
			}

			//Got a delay, so wait n seconds and do some fancy printing much like scrot -cd n
			if (Delay > 0) {
				int Waiting = Delay - 1;

				Console.Write("boki: Waiting for {0} seconds... {0}... ", Delay);
				System.Threading.Thread.Sleep(1000);
				do {
					Console.Write("{0}... ", Waiting);
					Waiting--;

					System.Threading.Thread.Sleep(1000);
				} while (Waiting > 0);

				Console.WriteLine();
			}
			
			//And finally, borderless, resize and move the given window
			if (UsingWindow.Handle != IntPtr.Zero) {
				Console.WriteLine("boki: Modifying window: " + UsingWindow.Title);
				if (!DoNothing)
					UsingWindow.MakeBorderless(LeaveHandles);

				if (!string.IsNullOrEmpty(SizeString) || FullScreen)
					UsingWindow.Resize(WxHToSize(SizeString));

				if (!string.IsNullOrEmpty(PosString) || FullScreen)
					UsingWindow.Move(XxYToPoint(PosString));
			}
			else {
				Console.WriteLine("boki: No window found!");
			}
		}

		public static Size WxHToSize(string input = null) {
			if (String.IsNullOrEmpty(input)) {
				return new Size(Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height);
			}
			else {
				Match SizeMatch = Regex.Match(input, @"(?<w>(?:-|)[0-9]*)x(?<h>(?:-|)[0-9]*)");
				return new Size(Convert.ToInt32(SizeMatch.Groups["w"].Value.ToString())
								, Convert.ToInt32(SizeMatch.Groups["h"].Value.ToString()));
			}
		}

		public static Point XxYToPoint(string input = null) {
			if (String.IsNullOrEmpty(input)) {
				return new Point(0, 0);
			}
			else {
				Match SizeMatch = Regex.Match(input, @"(?<x>(?:-|)[0-9]*)x(?<y>(?:-|)[0-9]*)");
				return new Point(Convert.ToInt32(SizeMatch.Groups["x"].Value.ToString())
								, Convert.ToInt32(SizeMatch.Groups["y"].Value.ToString()));
			}
		}
		
		public static void PrintVersion() {
			Console.WriteLine("BorderKiller v" + typeof(Program).Assembly.GetName().Version.ToString().Split('.').Take(3).Aggregate((a, b) => a + "." + b));
			Environment.Exit(1);
		}
		public static void PrintHelp(OptionSet opt) {
			string ExeName = AppDomain.CurrentDomain.FriendlyName;
			Console.WriteLine(@"BorderKiller v{0}
USAGE: {1} [OPTIONS]
Removes window captions and borders from windows as well as optionally resizing and moving them.
",
 typeof(Program).Assembly.GetName().Version.ToString().Split('.').Take(3).Aggregate((a, b) => a + "." + b),
 ExeName);

  opt.WriteOptionDescriptions(Console.Out);
			Environment.Exit(1);
		}
	}
}
