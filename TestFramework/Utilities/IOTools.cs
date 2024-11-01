using System.Collections.Generic;
using System.IO;

namespace CleanIMP.Utilities;

// ReSharper disable once InconsistentNaming
/// <summary>
/// A class for custom utility functions facilitating the use of file system.
/// </summary>
public static class IOTools
{
    /* Additional enums for flags to be used in functions */

    public enum FileWriteMode
    {
        Create, Overwrite, Append
    }

    /// <summary>
    /// Reads the <paramref name="file"/> and return its every line as a lazy Enumerable collection.
    /// Warning: StreamReader is blocking the file until the enumerator is deconstructed.
    /// </summary>
    /// <param name="file">File to read</param>
    /// <returns>Stream of string from file. Blocks file until read to the end or interrupted.</returns>
    public static IEnumerable<string> EnumerateAllLines(string file)
    {
        using var sr = new StreamReader(new FileStream(file, FileMode.Open));
        while (!sr.EndOfStream)
        {
            string? line = sr.ReadLine();
            if (line != null) yield return line;
        }
    }

    /// <summary>
    /// Writes contents of a given Enumerable string <paramref name="collection"/> to file <paramref name="file"/> every element is written to a new line.
    /// </summary>
    /// <param name="file">File to write a collection into</param>
    /// <param name="collection">String collection containing lines to write into the file</param>
    /// <param name="writeMode">Enum indicating how the file should be written to. FileWriteMode.Create will fire an IOException if the file already exists. Mode is overwrite by default.</param>
    public static void FileWriteAllLines(string file, IEnumerable<string> collection, FileWriteMode writeMode = FileWriteMode.Overwrite)
    {
        if (File.Exists(file))
        {
            switch (writeMode)
            {
                case FileWriteMode.Overwrite:
                    File.Delete(file);
                    break;
                case FileWriteMode.Create:
                    throw new IOException("File already exists");
            }
        }

        using var writer = new StreamWriter(new FileStream(file, FileMode.OpenOrCreate));

        if (writeMode == FileWriteMode.Append)
        {
            writer.BaseStream.Seek(0, SeekOrigin.End);
        }

        foreach (string chunk in collection)
        {
            writer.WriteLine(chunk);
        }
    }

    /// <summary>
    /// Writes contents of a given Enumerable string <paramref name="collection"/> to file <paramref name="file"/> every element is written to a new line.
    /// </summary>
    /// <param name="file">File to write a collection into</param>
    /// <param name="collection">String collection containing lines to write into the file</param>
    /// <param name="writeMode">Enum indicating how the file should be written to. FileWriteMode.Create will fire an IOException if the file already exists. Mode is overwrite by default.</param>
    public static void FileWriteAllLines(this IEnumerable<string> collection, string file, FileWriteMode writeMode = FileWriteMode.Overwrite)
    {
        FileWriteAllLines(file, collection, writeMode);
    }

    /// <summary>
    /// Writes <paramref name="contents"/> of a string to file <paramref name="file"/>.
    /// </summary>
    /// <param name="file">File to write the contents into</param>
    /// <param name="contents">String with contents to write into the file</param>
    /// <param name="writeMode">Enum indicating how the file should be written to. FileWriteMode.Create will fire an IOException if the file already exists. Mode is overwrite by default.</param>
    public static void FileWriteAllText(string file, string contents, FileWriteMode writeMode = FileWriteMode.Overwrite)
    {
        if (File.Exists(file))
        {
            switch (writeMode)
            {
                case FileWriteMode.Overwrite:
                    File.Delete(file);
                    break;
                case FileWriteMode.Create:
                    throw new IOException("File already exists");
            }
        }

        using var writer = new StreamWriter(new FileStream(file, FileMode.OpenOrCreate));

        if (writeMode == FileWriteMode.Append)
        {
            writer.BaseStream.Seek(0, SeekOrigin.End);
        }

        writer.Write(contents);
    }
}