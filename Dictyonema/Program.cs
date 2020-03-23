using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Dictyonema
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // get process name from command line
            if (args.Length == 0)
            {
                Console.WriteLine("Syntax:\n\tDictyonema processname");
                Environment.Exit(1);
            }
            var processName = args[0];
            Console.WriteLine($"Examining processes named {processName}");

            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                Console.WriteLine($"No processes found matching {processName}");
                Environment.Exit(2);
            }

            // foreach process in processes
            var i = 1;
            foreach (Process process in processes)
            {
                Console.WriteLine($"\t{i}: {process.ProcessName}");
                i++;

                //      get parent of process
                var parentId = Infrastructure.ParentProcessOfProcess((uint)process.Id);

                try
                {
                    var parentProcess = Process.GetProcessById((int)parentId);
                    var cmdLine = Infrastructure.GetCommandLine(parentProcess);
                    Console.WriteLine($"\t\tParent: {cmdLine}");
                }
                catch (Exception E)
                {
                    Console.WriteLine($"\t\tParent: {E.Message}");
                    Console.WriteLine($"\t\t\tProcess {process.Id} {process.ProcessName} has been orphaned.");
                    continue;
                }

                //      get children of parent
                var children = Infrastructure.ChildProcessesOfParentProcess((uint)parentId);

                //      foreach child in children
                var parentKnowsChild = false;
                foreach (PropertyDataCollection child in children)
                {
                    var childId = (uint)child["ProcessId"].Value;
                    //          if process.id of child == process.id of process
                    if (childId == process.Id)
                    {
                        //              ownership is ok
                        parentKnowsChild = true;
                        Console.WriteLine($"\t\t\tChild: {child["CommandLine"].Value}");
                        break;
                    }
                }
                //      if ownership is not ok
                if (!parentKnowsChild) {
                    //          say so
                    Console.WriteLine($"\t\t\t\t{process.Id} {process.ProcessName} appears to be orphaned.");
                }
                else
                {
                    Console.WriteLine($"\t\t\t\t{process.Id} {process.ProcessName} has a real parent.");
                }
            }
        }
    }
}
