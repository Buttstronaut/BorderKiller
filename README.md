BorderKiller
==============

BorderKiller, or boki for short, is a small command line program, written in C#, for simply removing window borders, resizing them and moving them. It's mainly used for making windowed video games run in "borderless fullscreen" mode.

A standard use case would be running a game in windowed mode, at 1920x1080, on a 1080p monitor, then using the --fs argument to fullscreen it.

Usage:
-----------
    boki.exe [OPTIONS]

boki uses NDesk.Options to allow support for GNU-style command line options, so things like -fd5 (fullscreen, delay of 5 seconds) are fine.

Examples:
---------------
    boki --pid=4822 -fd5 
Would wait 5 seconds then borderless and fullscreen the main window of the process with process ID 4822.

    boki --pname=notepad --pos --size=300x500
Would move the first found notepad.exe process's main window to 0x0 and resize it to 300x500 while removing borders.

    boki --ptitle=Unt*pad -fn
Would find the first window whose title matches "Unt*pad", for example "Untitled - Notepad", and fullscreen it without removing borders.

	boki --run="C:\Windows\Notepad.exe" --args="C:\textfile.txt" -fd5
Would run Notepad.exe, with the argument "C:\textfile.txt", then wait 5 seconds before fullscreening and removing borders.

Args:
-------
	--pid=VALUE
PID of the target process.

	--pname=VALUE
Name of the target process, excluding file extension, so for notepad.exe, "--pname=notepad".

	--ptitle=VALUE
Title of the target window, wildcards are allowed with *.

	--run=VALUE
Filename of an executable to run.

At least one of the above options should probably be specified if you want boki to DO anything.

	--pos[=VALUE]
X/Y coordinates to move the target window to.

	--size[=VALUE]
W/H to resize the target window to.

Both of the above options take a value like "500x300". --pos is XxY and --size is WidthxHeight. Optionally, if no value is specified, pos will use 0x0 and size will use the width/height of your primary monitor.

	-f, --fs, --fullscreen
Does the same as --pos --size, moves to 0x0 and resizes to the primary monitor's width/height.

	-n, --nothing
Causes boki to not do anything to the window borders.

	-r, --resize
Causes boki to remove all window styles EXCEPT the resize handles.

	-a, --args
A string containing arguments to pass to the executable in --run.

	-d, --delay
Adds a delay before doing anything to the window.

	-v, --ver, --version
Prints version and exits.

	-?, -h, --help
Prints help and exits.