# wmctrl-for-windows
Command line implementation of wmctrl for windows.
Only few features of the linux tool wmctrl are implemented so far, that is:

- List the window titles, process name and ID
- Switch the focus to a given window

The program is written in C-sharp.
This repository contains the latest binary and the source code (compatible Mono or Microsoft).

## Binaries
The latest binary is available [here](https://github.com/cbiasco/wmctrl-for-windows/raw/master/_bin/win_wmctrl.exe)

## Features/ usage
Typical example:
```bash
# Jump to google chrome window
win_wmctrl -a chrome
```
   
   
Usage:
```bash
win_wmctrl [options] [args]

options:
  -h                 : show this help
  -l <opt:PNAME>     : list processes, or windows if a process name is given
  -a <PNAME>         : switch to the window of the process name <PNAME>
  -ia <HWND>         : switch to the window of the window handle <HWND>
  -p <PNAME> <WNAME> : write the window handle of the process name and window name pair to stdout
```


## Compilation
Compile (using Mono or Miscrosoft Visual Studio tools).
You can use the Makefile provided in this repository. 
(You need csc.exe or mcs.exe in your system path)

## Installation
Put the exe file somewhere in your system path.
