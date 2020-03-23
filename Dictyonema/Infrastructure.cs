using System;
using System.Collections.Generic;
using System.Management;
using System.Diagnostics;
using System.Linq;

public static class Infrastructure
{
    public static List<PropertyDataCollection> ChildProcessesOfParentProcess(uint pid)
    {
        string WQL = string.Format("SELECT * FROM Win32_Process WHERE ParentProcessId = {0}", pid);

        List<PropertyDataCollection> mol = new List<PropertyDataCollection>();

        using (ManagementObjectSearcher mgmtObjSearcher = new ManagementObjectSearcher(WQL))
        {
            using (ManagementObjectCollection objCol = mgmtObjSearcher.Get())
            {
                if (objCol.Count != 0)
                {
                    mol.AddRange(from ManagementObject Process in objCol
                                 select Process.Properties);
                }
            }
        }

        return mol;
    }

    public static List<PropertyDataCollection> ChildProcessesOfCurrentProcess() =>
        ChildProcessesOfParentProcess((uint)Process.GetCurrentProcess().Id);

    public static List<PropertyDataCollection> ChildProcessesOfProcess(uint pid)
    {
        string WQL = string.Format("SELECT * FROM Win32_Process WHERE ProcessId = {0}", pid);
        List<PropertyDataCollection> mol = new List<PropertyDataCollection>();

        using (ManagementObjectSearcher mgmtObjSearcher = new ManagementObjectSearcher(WQL))
        {
            using (ManagementObjectCollection objCol = mgmtObjSearcher.Get())
            {
                if (objCol.Count != 0)
                {
                    mol.AddRange(from ManagementObject Process in objCol
                                 select Process.Properties);
                }
            }
        }

        return mol;
    }

    public static List<PropertyDataCollection> ChildProcessesOfProcess() =>
        ChildProcessesOfProcess((uint)Process.GetCurrentProcess().Id);

    public static uint ParentProcessOfProcess(uint pid)
    {
        uint result = UInt32.MinValue;
        string WQL = string.Format("SELECT * FROM Win32_Process WHERE ProcessId = {0}", pid);
        using (ManagementObjectSearcher mgmtObjSearcher = new ManagementObjectSearcher(WQL))
        {
            using (ManagementObjectCollection objCol = mgmtObjSearcher.Get())
            {
                if (objCol.Count != 0)
                {
                    foreach (ManagementObject Process in objCol)
                    {
                        result = (uint)Process.Properties["ParentProcessId"].Value;
                    }
                }
            }
        }

        return result;
    }

    public static uint ParentProcessOfCurrentProcess() =>
        ParentProcessOfProcess((uint)Process.GetCurrentProcess().Id);

    public static List<string> GetCommandLines(string processName)
    {
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

        return answer;
    }

    public static string GetCommandLine(Process proc)
    {
        using (ManagementObjectSearcher commandLineSearcher = new ManagementObjectSearcher(
            "SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + proc.Id))
        {
            string commandLine = "";
            foreach (ManagementObject commandLineObject in commandLineSearcher.Get())
            {
                commandLine += (string)commandLineObject["CommandLine"];
            }
            return commandLine;
        }
    }
}

