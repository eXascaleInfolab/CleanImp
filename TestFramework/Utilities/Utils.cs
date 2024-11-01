using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CleanIMP.Utilities;

/// <summary>
/// A class for custom utility functions of general type.
/// </summary>
public static class Utils
{
    public const string PythonExec = "python3.9";
    
    public static void RunVoidProcess(string command, string cliArgs, string workingDir = "", bool silent = false, bool fullsilent = false)
    {
        silent |= fullsilent; // override silent if fullsilent is true
        Process proc = new()
        {
            StartInfo =
            {
                WorkingDirectory = workingDir,
                FileName = command,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = silent,
                RedirectStandardError = fullsilent,
                Arguments = cliArgs
            }
        };

        proc.Start();
        proc.WaitForExit();

        if (proc.ExitCode != 0 && !fullsilent)
        {
            string errText =
                $"[WARNING] Process {command} returned code {proc.ExitCode} on exit.{Environment.NewLine}" +
                $"CLI args: {proc.StartInfo.Arguments}";

            Console.WriteLine(errText);
        }
        else if (!silent)
        {
            Console.WriteLine($"Process {command} successfully exited with code 0");
        }
    }

    public static IEnumerable<string> RunOutputProcess(string command, string cliArgs, string workingDir = "", bool fullsilent = false)
    {
        Process proc = new()
        {
            StartInfo =
            {
                WorkingDirectory = workingDir,
                FileName = command,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = fullsilent,
                Arguments = cliArgs
            }
        };

        proc.Start();

        var sr = proc.StandardOutput;

        while (!sr.EndOfStream)
        {
            string? line = sr.ReadLine();
            if (line == null) break;
            yield return line;
        }
        proc.WaitForExit();

        if (proc.ExitCode != 0 && !fullsilent)
        {
            string errText =
                $"[WARNING] Process {command} returned code {proc.ExitCode} on exit.{Environment.NewLine}" +
                $"CLI args: {proc.StartInfo.Arguments}";

            Console.WriteLine(errText);
        }
        if (!fullsilent)
        {
            Console.WriteLine($"Process {command} successfully exited with code 0");
        }
    }
    
    public static string EnquoteEsc(this string str) => $@"\""{str}\""";
    
    // cpu stuff

    private static double CpuCountAdj()
    {
        // Those calculations break for CPUs without HT/SMT
        // Alder Lake also can have some SMT cores and others non-SMT, this is handled below
        
        // I honestly don't think you should be running this bench on a machine w/o SMT, most of which are 4/4
        // But if you do - creating ~/.ALDER_LAKE will allow you to regain some performance without modifying the code

        double cpuCount = Math.Round((Environment.ProcessorCount / 2.0) * 1.2) + 0.01;

        if (IsAlderLake)
        {
            // alder lake can have P:E ratio of 1:1, 2:1, 3:2 and even 3:4, can't determine this w/o physical core count
            // ... which you can't get from .NET
            
            // roughly, the "average" Alder Lake cpu is 4xP+3xE
            // which means physical core # = (x - x * 0.4) / 2 + x * 0.4 = 0.5 x - 0.2 x + 0.4 x = 0.7 x = x / 1.43
            // but rounds up to 1.5 because 1.2 is too much for SMT gain factor given some cores don't have SMT
            cpuCount = Math.Round((Environment.ProcessorCount / 1.5) * 1.2) + 0.01;
        }

        return cpuCount;
    }

    // do not create this file if you have 12600 (non-K) and lower, or any other CPU that doesn't have E-cores
    //     or if E-cores are disabled in BIOS
    // default setup works 100% fine
    private static readonly bool IsAlderLake =
        System.IO.File.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}.ALDER_LAKE");

    public static int ParallelExecutionNo(int instances = 0)
    {
        double cpus = CpuCountAdj();
        if (instances == 0)
        {
            // max capacity
            return (int)cpus;
        }
        
        // Base idea:
        // c = number of physical cores with a small factor to account for possible SMT gains
        // t = ceil(ticks / c) = minimum number of parallel thread full runs needed to complete the task
        // n = ceil(ticks / t) = minimal number of parallel threads needed to complete the task in a given number of runs
        return (int)Math.Round(instances / Math.Ceiling(instances / cpus));
    }
    
    public static int ParallelExecutionNo(int instances, int factor)
    {
        // for "partially parallelized" algorithms
        
        // factor = 0 - do not parallelize
        // factor = 1 - proceed as usual
        // factor > 1 - return max/factor

        if (factor == 0) return 1;
        
        int res = ParallelExecutionNo(factor == 1 ? instances : 0); // either adjust to instances or get full capacity
        
        return (int)Math.Round((double)res / factor); // f = 1 -> no change; f > 1 -> divide capacity by f
    }
}