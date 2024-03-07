using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;

namespace ExtractVFS
{
    public class FileEntry
    {
        public string Name { get; set; }
        public uint Offset { get; set; }
        public uint Size { get; set; }
        public uint UnpackedSize { get; set; }
        public byte[] data { get; set; }
        public bool IsPacked { get; set; }
    }

    public class CharacterMap
    {
        public string A { get; set; }
        public string B { get; set; }

        public CharacterMap(string a, string b)
        {
            A = a;
            B = b;
        }
    }

    class Program
    {
        static string appPath;

        static int Main(string[] args)
        {
            appPath = AppDomain.CurrentDomain.BaseDirectory;
            bool loop = true;
            while (loop == true)
            {
                PrintBanner();

                Console.WriteLine("Hello, Senpai! (≧?≦) What would you like to do?");
                Console.WriteLine("1. Extract MES.VFS");
                Console.WriteLine("2. Convert folder to CSV");
                Console.WriteLine("3. Convert folder to RPY");
                Console.WriteLine("4. Convert CSV to folder");
                Console.WriteLine("5. Pack folder to MES.VFS");
                Console.WriteLine("6. Leave (?_?;)");

                string input = GetInput("Please choose an option (o´▽`o) :");
                int exec = 0;

                switch (input)
                {
                    case "1":
                        exec = Extract();

                        if (exec == 0)
                            WriteMessage("Extraction completed successfully! ?(*^-^*)?");
                        else
                            WriteMessage("Oops! Something went wrong during extraction. (T_T)");

                        break;
                    case "2":
                        exec = ConvertCSV();

                        if (exec == 0)
                            WriteMessage("Convertion completed successfully! ?(*^-^*)?");
                        else
                            WriteMessage("Oops! Something went wrong during conversion. (T_T)");

                        break;

                    case "5":
                        exec = Pack();

                        if (exec == 0)
                            WriteMessage("Packing was super fine Senpai! ?(*^-^*)?");
                        else
                            WriteMessage("Noooo! Something went wrong Senpai. (T_T)");

                        break;

                    case "6":
                        WriteMessage("Bye-bye, Senpai! Take care! (´｡? ω ?｡`) ?");
                        loop = false;
                        break;

                    default:
                        WriteMessage("Not implemented yet. (?_?;)");

                        break;
                }
                Thread.Sleep(1500);
                //Console.Clear();
            }

            return 0;
        }

        static void PrintBanner()
        {
            const string art = @"";
            const string title = @"";
            const string author = @"";

            Console.WriteLine(art);
            Console.WriteLine(title);
            Console.WriteLine(author);
        }

        static string GetEmoji(bool IsHappy = true)
        {
            List<string> Happy = new List<string> { "apple", "banana", "orange", "grape", "kiwi" };
            List<string> Sad = new List<string> { "apple", "banana", "orange", "grape", "kiwi" };

            Random random = new Random();

            if (IsHappy)
                return Happy[random.Next(0, Happy.Count)];
            else
                return Sad[random.Next(0, Sad.Count)];
        }

        // Extracts the MES.VFS contents
        static int Extract()
        {
            string fileName = GetInput("\n\nPlease enter the name of the file ?\nDefault is \"MES.VFS\" (´｡? ω ?｡`): ");

            if (fileName == "")
            {
                fileName = "MES.VFS";
            }

            string packPath = Path.Combine(appPath, fileName);

            byte[] data;
            int maxOffset;

            if (!File.Exists(packPath))
            {
                WriteMessage("The file could not be found, Senpai. (T_T)");
                return 1;
            }

            using (FileStream fileStream = new FileStream(packPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    maxOffset = (int)fileStream.Length;
                    data = reader.ReadBytes(maxOffset);
                }
            }

            int signature = ReadInt16(data, 0);
            int version = ReadInt16(data, 2);
            int count = ReadInt16(data, 4);
            int entrySize = ReadInt16(data, 6);
            int indexSize = ReadInt32(data, 8);

            if (0x4656 != signature && 0x4C56 != signature)
            {
                WriteMessage("Invalid file signature, Senpai. (>.<)");
                return 1;
            }

            if (version >= 0x0200)
                WriteMessage("Senpai, The version is higher than expected. Need to use other method (not implemented) (>.<)");

            if (!IsSaneCount(count))
            {
                WriteMessage("Invalid count. Please, double-check, Senpai! (?_?;)");
                return 1;
            }

            if (entrySize <= 0 || indexSize <= 0 || maxOffset != ReadUInt32(data, 0xC))
            {
                WriteMessage("Invalid file size or entry size, Senpai. (T_T)");
                return 1;
            }

            string extractDir = packPath + "~";

            if (Directory.Exists(extractDir))
            {
                int folderNumber = 1;
                string folderName = "";

                do
                {
                    folderName = $"{extractDir}{folderNumber:D3}";
                    folderNumber++;
                } while (Directory.Exists(folderName));

                extractDir = folderName;
            }

            Directory.CreateDirectory(extractDir);

            List<FileEntry> fileList = new List<FileEntry>();
            int indexOffset = 0x10;

            for (int i = 0; i < count; i++)
            {
                if (ReadUInt32(data, indexOffset + 0x13) > 0)
                {
                    var entry = new FileEntry();

                    entry.Name = ReadString(data, indexOffset, 0x13, i);
                    entry.Offset = ReadUInt32(data, indexOffset + 0x13);
                    entry.Size = ReadUInt32(data, indexOffset + 0x17);
                    entry.UnpackedSize = ReadUInt32(data, indexOffset + 0x1B);
                    entry.IsPacked = 0 != ReadByte(data, indexOffset + 0x1F);

                    fileList.Add(entry);
                }

                indexOffset += entrySize;
            }

            foreach (var entry in fileList)
            {
                string filePath = Path.Combine(extractDir, entry.Name);
                byte[] extractedData = new byte[entry.Size];

                Array.Copy(data, entry.Offset, extractedData, 0, entry.Size);

                try
                {
                    using (FileStream fs = File.Create(filePath))
                    {
                        fs.Write(extractedData, 0, extractedData.Length);
                    }
                }
                catch (Exception ex)
                {
                    WriteMessage($"Failed to write the file '{entry.Name}': {ex.Message}, Senpai. (T_T)");
                }

                Console.WriteLine($"Senpai! (≧?≦) It's extracted: {entry.Name}");
            }

            return 0;
        }

        // Converts files 
        static int ConvertCSV()
        {
            string folderPath = GetFolder();

            if (folderPath == "")
                return 1;

            try
            {
                // Create a new folder name by adding "_regex" suffix
                string newFolderPath = Path.Combine(Path.GetDirectoryName(folderPath), Path.GetFileName(folderPath) + "_regex");
                Directory.CreateDirectory(newFolderPath);

                // Process each file in the folder
                string[] files = Directory.GetFiles(folderPath);
                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    string newFilePath = Path.Combine(newFolderPath, fileName);
                    RewriteFile(filePath,newFilePath);
                }

                Console.WriteLine("Regex matches extracted and saved to files in the new folder.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }


            return 0;
        }

        static void RewriteFile(string inputFilePath, string outputFilePath)
        {
            try
            {
                // Read the Shift-JIS encoded file
                string content;
                using (StreamReader reader = new StreamReader(inputFilePath, Encoding.GetEncoding("shift_jis")))
                {
                    content = reader.ReadToEnd();
                }
                

                // Replace original new lines with spaces
                content = content.Replace("\n", " ");

                // Insert new line before "("
                content = content.Replace("(", "\n(");

                content = Decrypt(content);

                // Write the modified content to the new file
                using (StreamWriter writer = new StreamWriter(outputFilePath, false, Encoding.GetEncoding("shift_jis")))
                {
                    writer.Write(content);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }

        // Returns a full japanese string based on the missing characters
        static string Decrypt(string input)
        {
            List<CharacterMap> ReplaceMap = new List<CharacterMap>
            {
                new CharacterMap("Z", "人"),
            };

            string decrypted = input;

            foreach(CharacterMap ch in ReplaceMap)
            {
                decrypted = decrypted.Replace(ch.A, ch.B);
            }

            return decrypted;
        }

        // Packs multiple files back to a MES.VFS archive
        static int Pack()
        {
            string WorkingDirectory = GetFolder();

            if (WorkingDirectory == "")
                return 1;

            string[] files = Directory.GetFiles(WorkingDirectory);

            List<FileEntry> fileList = new List<FileEntry>();

            foreach (string file in files)
            {
                byte[] fileData = File.ReadAllBytes(file);

                var entry = new FileEntry
                {
                    Name = Path.GetFileName(file),
                    Offset = 0,
                    Size = (uint)fileData.Length,
                    UnpackedSize = (uint)fileData.Length,
                    IsPacked = false,
                    data = fileData
                };

                fileList.Add(entry);
            }

            fileList = fileList.OrderBy(fe => fe.Name).ToList();

            int entrySize = 32;
            int indexSize = entrySize * fileList.Count;

            using (FileStream destStream = new FileStream("MES_MOD.VFS", FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(destStream))
                {
                    writer.Write((short)0x4656);
                    writer.Write((short)1);
                    writer.Write((short)fileList.Count);
                    writer.Write((short)entrySize);
                    writer.Write(indexSize);

                    foreach (FileEntry entry in fileList)
                    {
                        entry.Offset = (uint)destStream.Position;
                        writer.Write(Encoding.GetEncoding("shift-jis").GetBytes(entry.Name.PadRight(19, '\0')));
                        writer.Write(entry.Offset);
                        writer.Write(entry.Size);
                        writer.Write(entry.UnpackedSize);
                        writer.Write(entry.IsPacked ? (byte)1 : (byte)0);
                    }
                    foreach (FileEntry entry in fileList)
                    {
                        writer.Write(entry.data);
                    }
                }
            }

            return 0;
        }

        static string GetFolder()
        {
            string WorkingDirectory = "";
            string[] subDirectories = Directory.GetDirectories(appPath);

            if (subDirectories.Length <= 0)
            {
                WriteMessage("There are no folders to work on Senpai :(");
                return "";
            }

            WriteMessage("Directories:");

            for (int i = 0; i < subDirectories.Length; i++)
            {
                Console.WriteLine($"[{i + 1}]\t{subDirectories[i]}");
            }

            while (true)
            {
                string console_input = GetInput("Pick a folder:");
                int input = -1;

                if (int.TryParse(console_input, out input))
                {
                    if (input >= 1 && input <= subDirectories.Length)
                    {
                        WorkingDirectory = subDirectories[input - 1];
                        break;
                    }
                    else
                        WriteMessage($"Senpai please enter a number between 1 and {subDirectories.Length} ^.^");
                }
                else
                    WriteMessage("Please, try again Senpai :(");
            }

            WriteMessage($"You picked:\t{WorkingDirectory}");

            return WorkingDirectory;
        }

        // A helper function to get and diplay question and terminal input
        static string GetInput(string prompt)
        {
            Console.Write($"\n{prompt}");
            return Console.ReadLine();
        }

        // It writes a message to the terminal with padding (\n on the start and end)
        static void WriteMessage(string msg)
        {
            Console.WriteLine($"\n{msg}\n");
        }

        // Byte functions
        public static bool IsSaneCount(int count)
        {
            return count > 0 && count < 0x40000;
        }

        public static byte ReadByte(byte[] data, int offset)
        {
            return data[offset];
        }

        public static short ReadInt16(byte[] data, int offset)
        {
            return BitConverter.ToInt16(data, offset);
        }

        public static uint ReadUInt32(byte[] data, int offset)
        {
            return BitConverter.ToUInt32(data, offset);
        }

        public static int ReadInt32(byte[] data, int offset)
        {
            return BitConverter.ToInt32(data, offset);
        }

        static string ReadString(byte[] byteArray, int offset, int size, int count)
        {
            try
            {
                string decodedString = Encoding.GetEncoding("shift_jis").GetString(byteArray, offset, size);

                int index = decodedString.IndexOf(".MES");

                if (index != -1)
                {
                    return decodedString.Substring(0, index + 4).Trim();
                }
            }
            catch (Exception ex)
            {
                WriteMessage("Failed to decode the filename, Senpai. (T_T)");
                WriteMessage(ex.Message);
            }

            return $"{count:D3}.MES";
        }
    }
}
