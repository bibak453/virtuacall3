using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

class Program
{
    static string appPath;

    static void Main(string[] args)
    {
        appPath = AppDomain.CurrentDomain.BaseDirectory;
        string directoryPath = GetFolder();
        string outputFile = "unique_characters.txt";

        // Read excluded characters from the blacklist file
        HashSet<char> excludedCharacters = ReadBlacklistCharacters("blacklist_characters.txt");

        // Create a HashSet to store unique characters
        HashSet<char> uniqueCharacters = new HashSet<char>();

        // Process files in the directory and write unique characters to the output file
        ProcessAndWriteFiles(directoryPath, outputFile, uniqueCharacters, excludedCharacters);

        Console.WriteLine("Unique characters have been written to " + outputFile);
    }

    static void ProcessAndWriteFiles(string directoryPath, string outputFile, HashSet<char> uniqueCharacters, HashSet<char> excludedCharacters)
    {
        try
        {
            // Check if the directory exists
            if (Directory.Exists(directoryPath))
            {
                // Get all files in the directory
                string[] files = Directory.GetFiles(directoryPath);

                /*
                string[] files = {
                    Path.Combine(directoryPath, "V_OP01.MES"),
                    Path.Combine(directoryPath, "V_OP02.MES"),
                    Path.Combine(directoryPath, "V_OP03.MES")
                };
                */
                
                // Process each file
                foreach (string file in files)
                {
                    // Read each character from the file using Shift-JIS encoding
                    string content = ReadFile(file, Encoding.GetEncoding("shift-jis"));
                    foreach (char c in content)
                    {
                        // Skip adding character if it's in the exclusion set
                        if (!excludedCharacters.Contains(c) & !uniqueCharacters.Contains(c))
                        {
                            uniqueCharacters.Add(c);
                        }
                    }

                    string[] lines = content.Split('\n');
                    List<string> lookLines = new List<string>();

                    foreach(string line in lines)
                    {
                        if (line.Contains("*"))
                        {
                            lookLines.Add(line);
                        }
                    }

                    using (StreamWriter writer = new StreamWriter(Path.Combine(appPath,"where.txt"), true, Encoding.GetEncoding("shift-jis")))
                    {
                        // Write each line from the array to the file
                        foreach (string line in lookLines)
                        {
                            writer.WriteLine(line);
                        }
                    }
                }

                List<char> sortedList = uniqueCharacters.ToList();
                sortedList.Sort();
                uniqueCharacters = new HashSet<char>(sortedList);

                // Write unique characters to the output file
                WriteToFile(outputFile, uniqueCharacters);
            }
            else
            {
                Console.WriteLine("Directory does not exist.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    static string ReadFile(string filePath, Encoding encoding)
    {
        try
        {
            // Read the file content using the specified encoding
            return File.ReadAllText(filePath, encoding);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
            return string.Empty;
        }
    }

    static void WriteToFile(string outputFile, HashSet<char> uniqueCharacters)
{
    try
    {
        // Write unique characters to the output file using Shift-JIS encoding
        using (StreamWriter writer = new StreamWriter(outputFile, false, Encoding.GetEncoding("shift-jis")))
        {
            foreach (char c in uniqueCharacters)
            {
                writer.Write(c);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error writing to file: " + ex.Message);
    }
}


    static HashSet<char> ReadBlacklistCharacters(string blacklistFilePath)
    {
        HashSet<char> excludedCharacters = new HashSet<char>();

        try
        {
            if (File.Exists(blacklistFilePath))
            {
                string blacklistContent = File.ReadAllText(blacklistFilePath, Encoding.GetEncoding("shift_jis"));
                foreach (char c in blacklistContent)
                {
                    // Add each character from the blacklist file to the HashSet
                    excludedCharacters.Add(c);
                }
            }
            else
            {
                Console.WriteLine("Blacklist file does not exist.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading blacklist file: " + ex.Message);
        }

        return excludedCharacters;
    }
}
