using System; // For IntPtr
using System.Runtime.InteropServices; // DllImport
using System.Diagnostics; // Process
using System.Collections.Generic; // IEnumerable
using System.Text; // StringBuilder

public class wmctrl
{
    delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn,
                     IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public static extern bool SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessage(int hWnd, int Msg, int wparam, int lparam);

    [DllImport("user32.dll")]
    public static extern void SwitchToThisWindow(IntPtr hWnd);

    const int WM_GETTEXT = 0x000D;
    const int WM_GETTEXTLENGTH = 0x000E;

    // --------------------------------------------------------------------------------
    // --- Helper for enumerating window handles under a process
    // --------------------------------------------------------------------------------
    static IEnumerable<IntPtr> EnumerateProcessWindowHandles(Process proc)
    {
        var handles = new List<IntPtr>();

        foreach (ProcessThread thread in proc.Threads)
        {
            EnumThreadWindows(thread.Id,
                      (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);
        }

        return handles;
    }

    static IntPtr GetProcessWindowHandle(Process proc, string windowName)
    {
        IntPtr handle = IntPtr.Zero;
        StringBuilder title = new StringBuilder(1000);

        foreach (ProcessThread thread in proc.Threads)
        {
            if (handle == IntPtr.Zero)
            {
                EnumThreadWindows(thread.Id,
                          (hWnd, lParam) =>
                          {
                              SendMessage(hWnd, WM_GETTEXT, title.Capacity, title);
                              if (title.ToString().Contains(windowName))
                              {
                                  handle = hWnd;
                                  return false;
                              }
                              return true;
                          }, IntPtr.Zero);
            }
        }

        return handle;
    }

    // --------------------------------------------------------------------------------
    // --- Switch to Window
    // --------------------------------------------------------------------------------
    public static int SwitchToWindow(string procName)
    {
        // Getting window matching
        Process[] procs = Process.GetProcessesByName(procName);
        int nProcs = procs.Length;
        if (nProcs < 1)
        {
            Console.WriteLine("Error: No process found for name: {0}", procName);
            return -1;
        }
        else
        {
            // We'll use the first window we found
            Process proc = procs[0];
            if (nProcs > 1)
            {
                Console.WriteLine("{0} processes found with name: {1}", nProcs, procName);
                Console.WriteLine("Using first process:");
                Console.WriteLine("Process Name: {0} ID: {1} Title: {2}", proc.ProcessName, proc.Id, proc.MainWindowTitle);
            }

            // --- Switching to window using user32.dll function
            SwitchToThisWindow(proc.MainWindowHandle);
            return 0;
        }
    }

    // --------------------------------------------------------------------------------
    // --- List Processes info
    // --------------------------------------------------------------------------------
    public static int ListProcesses()
    {
        Process[] processlist = Process.GetProcesses();
        Console.WriteLine("ID: \t Name:\t Title:");
        Console.WriteLine("-------------------------------------------------");
        foreach (Process proc in processlist)
        {
            if (!String.IsNullOrEmpty(proc.MainWindowTitle))
            {
                Console.WriteLine("{0}\t {1}\t {2}", proc.Id, proc.ProcessName, proc.MainWindowTitle);
            }
        }
        return 0;
    }

    // --------------------------------------------------------------------------------
    // --- List Windows info
    // --------------------------------------------------------------------------------
    public static int ListWindows(string procName)
    {
        // Getting window matching
        Process[] procs = Process.GetProcessesByName(procName);
        int nProcs = procs.Length;
        if (nProcs < 1)
        {
            Console.WriteLine("Error: No process found for name: {0}", procName);
            return -1;
        }
        else
        {
            foreach (var proc in procs)
            {
                Console.WriteLine("Process Name: {0} ID: {1} Title: {2}", proc.ProcessName, proc.Id, proc.MainWindowTitle);

                foreach (var handle in EnumerateProcessWindowHandles(proc))
                {
                    StringBuilder message = new StringBuilder(1000);
                    SendMessage(handle, WM_GETTEXT, message.Capacity, message);
                    if (string.IsNullOrWhiteSpace(message.ToString()))
                    {
                        Console.WriteLine("---");
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                    Console.WriteLine("    {0}", handle);
                }

                Console.WriteLine("");
            }
            return 0;
        }
    }

    // --------------------------------------------------------------------------------
    // --- Get info for specific Window
    // --------------------------------------------------------------------------------
    public static IntPtr GetWindowHandle(string procName, string windowName)
    {
        // Getting window matching
        Process[] procs = Process.GetProcessesByName(procName);
        int nProcs = procs.Length;
        if (nProcs < 1)
        {
            Console.Write("-3");
            return IntPtr.Zero;
        }
        else
        {
            return GetProcessWindowHandle(procs[0], windowName);
        }
    }

    // --------------------------------------------------------------------------------
    // --- Print command usage
    // --------------------------------------------------------------------------------
    public static void print_usage()
    {
        Console.WriteLine("");
        Console.WriteLine("usage: win_wmctrl [options] [args]");
        Console.WriteLine("");
        Console.WriteLine("options:");
        Console.WriteLine("  -h                 : show this help");
        Console.WriteLine("  -l <opt:PNAME>     : list processes, or windows if a process name is given");
        Console.WriteLine("  -a <PNAME>         : switch to the window of the process name <PNAME>");
        Console.WriteLine("  -ia <HWND>         : switch to the window of the window handle <HWND>");
        Console.WriteLine("  -p <PNAME> <WNAME> : write the window handle of the process name and window name pair to stdout");
        Console.WriteLine("");

    }

    // --------------------------------------------------------------------------------
    // --- Main Program
    // --------------------------------------------------------------------------------
    public static int Main(string[] args)
    {
        int status = 0; // Return status for Main

        // --------------------------------------------------------------------------------
        // --- Parsing arguments
        // --------------------------------------------------------------------------------
        int nArgs = args.Length;
        if (nArgs == 0)
        {
            Console.WriteLine("Error: insufficient command line arguments");
            print_usage();
            return 0;
        }
        int i = 0;
        while (i < nArgs)
        {
            string s = args[i];
            switch (s)
            {
                case "-h": // Help
                    print_usage();
                    i = i + 1;
                    break;
                case "-a": // Switch to Window via name
                    if (i + 1 < nArgs)
                    {
                        status = SwitchToWindow(args[i + 1]);
                        i = i + 2;
                    }
                    else
                    {
                        Console.WriteLine("Error: command line option -a needs to be followed by a process name.");
                        status = -1;
                        i++;
                    }
                    break;
                case "-ia": // Switch to Window via window handle
                case "-ai":
                    if (i + 1 < nArgs)
                    {
                        Int64 hWnd;
                        bool isNumeric = Int64.TryParse(args[i + 1], out hWnd);
                        if (isNumeric)
                        {
                            SwitchToThisWindow(new IntPtr(hWnd));
                            status = 0;
                        }
                        else
                        {
                            Console.WriteLine("Error: command line option -i needs to be followed by a window handle number.");
                            status = -1;
                        }
                        i = i + 2;
                    }
                    else
                    {
                        Console.WriteLine("Error: command line option -i needs to be followed by a window handle number.");
                        status = -1;
                        i++;
                    }
                    break;
                case "-p":
                    status = 0; // The status will never be non-zero for -p, because the console output is expected to be used by the caller.
                    if (i + 2 < nArgs)
                    {
                        IntPtr handle = GetWindowHandle(args[i + 1], args[i + 2]);
                        if (handle == IntPtr.Zero)
                        {
                            Console.Write("-2");
                        }
                        else
                        {
                            Console.Write("{0}", handle);
                        }
                        i = i + 3;
                    }
                    else
                    {
                        Console.Write("-1");
                        i++;
                    }
                    break;
                case "-l": // List Processes/Windows
                    if (i + 1 < nArgs)
                    {
                        status = ListWindows(args[i + 1]);
                        i = i + 2;
                    }
                    else
                    {
                        status = ListProcesses();
                    }
                    i++;
                    break;
                default:
                    Console.WriteLine("\nSkipped argument: " + args[i]);
                    i++;
                    break;
            }
            if (status != 0)
            {
                // If an error occured, print usage and exit
                print_usage();
                return status;
            }
        }
        //
        return status;
    }



}
