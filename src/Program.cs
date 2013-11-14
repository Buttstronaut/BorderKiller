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
				.Add("a=|args=", "Arguments to pass to --run", v => RunArgs = v)
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

			if (RunPath != "") {  //Run an executable
				Console.WriteLine("boki: Running {0} {1}", RunPath, RunArgs);
				
				try {
					UsingWindow = FakeFullscreen.RunExe(RunPath, RunArgs);
				}
				catch (System.ComponentModel.Win32Exception X) { //This should only happen with an invalid --run path or with inadequate permissions (I think!)
					Console.WriteLine("boki: Error running {0}: {1}", RunPath, X.Message);
					Environment.Exit(1);
				}
			}
			//Got a delay, so wait n seconds and do some fancy printing much like scrot -cd n
			WaitForDelay(Delay);
			
			//And finally, borderless, resize and move the given window
			if (UsingWindow.Handle != IntPtr.Zero) {
				Console.WriteLine("boki: Modifying window: " + UsingWindow.Title);

				if (!DoNothing) //Not doing nothing, so remove window borders
					UsingWindow.MakeBorderless(LeaveHandles);

				//Got a resize string or FullScreen arg, so resize
				if (!string.IsNullOrEmpty(SizeString) || FullScreen)
					UsingWindow.Resize(FakeFullscreen.WxHToSize(SizeString));
				//Same as above but move
				if (!string.IsNullOrEmpty(PosString) || FullScreen)
					UsingWindow.Move(FakeFullscreen.XxYToPoint(PosString));
			}
			else {
				Console.WriteLine("boki: No window found!");
			}
		}

		//Causes the thread to sleep for n seconds
		//Also prints a swanky countdown
		public static void WaitForDelay(int Delay) {
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
		}
		
		//Prints the version and exits
		public static void PrintVersion() {
			Console.WriteLine("BorderKiller v" + typeof(Program).Assembly.GetName().Version.ToString().Split('.').Take(3).Aggregate((a, b) => a + "." + b));
			Environment.Exit(1);
		}

		//Prints help and exits
		public static void PrintHelp(OptionSet opt) {
			string ExeName = AppDomain.CurrentDomain.FriendlyName;
			Console.WriteLine(@"BorderKiller v{0}
USAGE: {1} [OPTIONS]
Removes window captions and borders from windows as well as optionally resizing and moving them.

Arguments:",
			typeof(Program).Assembly.GetName().Version.ToString().Split('.').Take(3).Aggregate((a, b) => a + "." + b),
			ExeName);

  opt.WriteOptionDescriptions(Console.Out);
			Environment.Exit(1);
		}
	}
}
