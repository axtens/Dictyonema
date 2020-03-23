using System;
using System.Collections.Generic;
using System.Management;
using System.Diagnostics;
using System.Text;
using System.ServiceProcess;

public class Infrastructure
{
    internal static Axtended.ApplicationLogger.ApplicationLogging AL = null;

    public static List<PropertyDataCollection> ChildProcessesOfParentProcess(uint pid)
    {
        AL?.Module("ChildProcessesOfParentProcess");

        string WQL = string.Format("SELECT * FROM Win32_Process WHERE ParentProcessId = {0}", pid);

        List<PropertyDataCollection> mol = new List<PropertyDataCollection>();

        ManagementObjectSearcher mgmtObjSearcher = new ManagementObjectSearcher(WQL);
        ManagementObjectCollection objCol = mgmtObjSearcher.Get();

        AL?.Inform(objCol.Count, "items found using", WQL);

        if (objCol.Count != 0)
        {
            foreach (ManagementObject Process in objCol)
            {
                mol.Add(Process.Properties);
            }
        }
        AL?.Module();
        return mol;

    }
    public static List<PropertyDataCollection> ChildProcessesOfCurrentProcess() =>
        ChildProcessesOfParentProcess((uint)Process.GetCurrentProcess().Id);

    public static List<PropertyDataCollection> ChildProcessesOfProcess(uint pid)
    {
        AL?.Module("ChildProcessesOfProcess");

        string WQL = string.Format("SELECT * FROM Win32_Process WHERE ProcessId = {0}", pid);
        List<PropertyDataCollection> mol = new List<PropertyDataCollection>();

        ManagementObjectSearcher mgmtObjSearcher = new ManagementObjectSearcher(WQL);
        ManagementObjectCollection objCol = mgmtObjSearcher.Get();
        AL?.Inform(objCol.Count, "items found using", WQL);
        if (objCol.Count != 0)
        {
            foreach (ManagementObject Process in objCol)
            {
                mol.Add(Process.Properties);
            }
        }

        AL?.Module();
        return mol;

    }
    public static List<PropertyDataCollection> ChildProcessesOfProcess() =>
        ChildProcessesOfProcess((uint)Process.GetCurrentProcess().Id);

    public static uint ParentProcessOfProcess(uint pid)
    {
        AL?.Module("ParentProcessOfProcess");

        uint result = UInt32.MinValue;
        string WQL = string.Format("SELECT * FROM Win32_Process WHERE ProcessId = {0}", pid);
        ManagementObjectSearcher mgmtObjSearcher = new ManagementObjectSearcher(WQL);
        ManagementObjectCollection objCol = mgmtObjSearcher.Get();
        AL?.Inform(objCol.Count, "items found using", WQL);
        if (objCol.Count != 0)
        {
            foreach (ManagementObject Process in objCol)
            {
                result = (uint)Process.Properties["ParentProcessId"].Value;
            }
        }

        AL?.Module();

        return result;
    }

    public static uint ParentProcessOfCurrentProcess() =>
        ParentProcessOfProcess((uint)Process.GetCurrentProcess().Id);

    // specific uses:
    //  with every chromedriver. 
    //      get parent.
    //      if parent is an instance of NormanEmailScraper
    //          get children of that instance of NES and see if it has a pid matching the pid of chromedriver.
    //          if pid is not matching, then 
    //              terminate the chromedriver and all its subprocesses.
    //      else
    //          terminte chromedriver process and subprocesses

    public static List<string> GetCommandLines(string processName)
    {
        AL?.Module("GetCommandLines");
        List<string> answer = new List<string>();
        Process[] procs = Process.GetProcessesByName(processName);
        foreach (Process proc in procs)
        {
            List<PropertyDataCollection> lPDC = ChildProcessesOfProcess((uint)proc.Id);
            foreach (PropertyDataCollection pdc in lPDC)
            {
                answer.Add(pdc["CommandLine"].Value.ToString());
            }
        }
        AL?.Module();
        return answer;
    }

    public static bool TerminateUnownedChromeDriverInstances()
    {
        AL?.Module("TerminateUnownedChromeDriverInstances");

        bool status = false;
        Process[] chromedrivers = Process.GetProcessesByName("chromedriver");
        Process parentProcess = null;
        foreach (Process chromedriver in chromedrivers)
        {
            uint parentId = ParentProcessOfProcess((uint)chromedriver.Id);
            AL?.Inform("Want to kill", chromedriver.Id, "and subprocesses.");
            try
            {
                parentProcess = Process.GetProcessById((int)parentId);
            }
            catch (Exception E)
            {
                if (null != AL)
                {
                    AL.Warn("Did not find parent", parentId, "of", chromedriver.Id);
                    AL.Warn(E.Message);
                }
                TerminateBottomUp((uint)chromedriver.Id);
                status = true;
                continue;
            }

            //if (parentProcess.ProcessName.ToLower() == "normanemailscraper")
            //{
            AL?.Inform(parentProcess.ProcessName, "found");
            List<PropertyDataCollection> cpList = ChildProcessesOfParentProcess((uint)parentId);
            bool chromeDriverIdsMatch = false;
            foreach (PropertyDataCollection pdc in cpList)
            {
                AL?.Inform("Testing", pdc["ProcessId"].Value, chromedriver.Id);

                if ((uint)pdc["ProcessId"].Value == (uint)chromedriver.Id)
                {
                    chromeDriverIdsMatch = true;
                    AL?.Inform("Matched!");
                }
            }
            if (!chromeDriverIdsMatch)
            {
                AL?.Warn(parentProcess.ProcessName, "was not the owner of the chromedriver");
                TerminateBottomUp((uint)chromedriver.Id);
                status = true;
            }
            //}
            //else
            //{
            //    AL?.Warn(parentProcess.ProcessName, "was not an instance of NormanEmailScraper.");
            //    TerminateBottomUp((uint)chrome.Id);
            //    status = true;
            //}
        }

        AL?.Module();

        return status;
    }

    private static void StackChildren(ref Stack<uint> pidStack, uint id)
    {
        AL?.Module("StackChildren");
        List<PropertyDataCollection> children = Axtended.Infrastructure.WMI.ChildProcessesOfParentProcess(id);
        foreach (PropertyDataCollection child in children)
        {
            uint childId = (uint)child["ProcessId"].Value;
            pidStack.Push(childId);
            StackChildren(ref pidStack, childId);
        }
        AL?.Module();
    }

    private static void TerminateBottomUp(uint id)
    {
        AL?.Module("TerminateBottomUp");
        Stack<uint> pidStack = new Stack<uint>();
        pidStack.Push(id);
        StackChildren(ref pidStack, id);
        while (pidStack.Count > 0)
        {
            uint pid = pidStack.Pop();
            AL?.Inform("Killing", pid);
            Process p = Process.GetProcessById((int)pid);
            try
            {
                p.Kill();
            }
            catch (Exception E)
            {
                AL?.Fail("Could not kill", pid, E.Message);
            }
        }
    }

    public static void RegisterApplicationLogging(ref ApplicationLogger.ApplicationLogging ptr)
    {
        AL?.Module("RegisterApplicationLogging");
        AL = ptr;
        AL?.Inform("RegisterApplicationLogging");
        AL?.Module();
    }

    public static string getCommandLine(Process proc)
    {
        AL?.Module("getCommandLine");
        ManagementObjectSearcher commandLineSearcher = new ManagementObjectSearcher(
            "SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + proc.Id);
        String commandLine = "";
        foreach (ManagementObject commandLineObject in commandLineSearcher.Get())
        {
            commandLine += (String)commandLineObject["CommandLine"];
        }
        AL?.Inform(commandLine);
        AL?.Module();
        return commandLine;
    }

    public static string WhatsRunning()
    {
        StringBuilder answer = new StringBuilder();
        Process[] procs = Process.GetProcesses();
        foreach (Process proc in procs)
        {
            string procCmd = getCommandLine(proc);
            answer.Append(procCmd + "\r\n");
        }
        return answer.ToString();
    }

    public static void ExecuteCommand(string command)
    {
        AL?.Module("ExecuteCommand");
        var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
        processInfo.CreateNoWindow = true;
        processInfo.UseShellExecute = false;
        var process = Process.Start(processInfo);
        System.Threading.Thread.Sleep(10000);
        AL?.Inform(processInfo.Arguments);
        process.Close();
        AL?.Module();
    }


    public static bool RestartService(string serviceName, int timeoutMilliseconds)
    {
        AL?.Module("RestartService");
        ServiceController service = new ServiceController(serviceName);
        try
        {
            int millisec1 = Environment.TickCount;
            TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

            // count the rest of the timeout
            int millisec2 = Environment.TickCount;
            timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));

            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            AL?.Module();
            return true;
        }
        catch (Exception exc)
        {
            // ...
            AL?.Warn("RestartService", exc.Message, exc.Source, exc.StackTrace);
            AL?.Module();
            return false;
        }
    }

    //public static int KillAll(string procName)
    //{
    //    //Axtension.Processes px = new Axtension.Processes();
    //    int answer = 0;
    //    Axtension.Processes P = new Axtension.Processes();
    //    Process[] procs = Process.GetProcessesByName(procName);
    //    foreach (Process proc in procs)
    //    {
    //        P.KillProcessAndChildren(proc.Id);
    //    }
    //    return answer;
    //
    //}
}

