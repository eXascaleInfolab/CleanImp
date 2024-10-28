using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Utilities.Interop;

/// <summary>
/// A class for external python algorithms that perform matrix imputation.
/// Communication is performed through stdin/stdout to avoid explicitly using the file system.
/// </summary>
public static class PythonPipeImpute
{
    private const string PyImputeLocation = "../external_code/impute/";
    
    /// <summary>
    /// Main function to run python imputation algorithms through pipes
    /// </summary>
    /// <remarks>
    /// See any imputation function in <see cref="PythonPipeImpute"/> and resp. python wrapper implementation in <see cref="PyImputeLocation"/> as an example of usage
    /// </remarks>
    /// <param name="command">Main executable command (should be with a python version, e.g. python3)</param>
    /// <param name="cliArgs">Arguments dictating how to import the function and parametrize the algorithm</param>
    /// <param name="inputMatrix">Input matrix in string form as a collection (rows) of space-separated values (columns)</param>
    /// <param name="workingDir">Working directory where to execute the command, by default <see cref="PyImputeLocation"/></param>
    /// <returns>Imputed matrix in the same string format as the input</returns>
    private static IEnumerable<string> RunPythonImpute(string command, string cliArgs, IEnumerable<string> inputMatrix, string workingDir = PyImputeLocation)
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
                RedirectStandardInput = true, // to send in the matrix
                RedirectStandardOutput = true, // to get the imputed output
                RedirectStandardError = false, // errors/warnings will be printed to the current terminal session
                Arguments = cliArgs
            }
        };

        // launch
        proc.Start();
    
        // write matrix to stdin
        StreamWriter sw = proc.StandardInput;
        foreach (string line in inputMatrix)
        {
            sw.WriteLine(line);
        }
        sw.Close(); // will send EOF so python stops waiting for further lines

        StreamReader sr = proc.StandardOutput;

        // read response from stdout
        while (!sr.EndOfStream)
        {
            string? line = sr.ReadLine();
            if (line == null) break;
            yield return line;
        }
        proc.WaitForExit();

        if (proc.ExitCode != 0)
        {
            string errText =
                $"[WARNING] Process {command} returned code {proc.ExitCode} on exit.{Environment.NewLine}" +
                $"CLI args: {proc.StartInfo.Arguments}";

            Console.WriteLine(errText);
        }
    }

    public static (long, Matrix<double>) PythonIIM(Matrix<double> matrix, int neighbors)
    {
        string cliParams = $"-c \"from iim import impute_piped_data; impute_piped_data({$"iim {neighbors}".EnquoteEsc()});\"";

        (string runtime, IEnumerable<string> res) = RunPythonImpute(Utils.PythonExec, cliParams, matrix.ExportMx()).HeadTail();

        return ((long)Double.Parse(runtime), Matrix<double>.Build.DenseOfRowArrays(MathX.Parse.ParseMatrix(res)));
    }
}