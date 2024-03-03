using System;
using System.IO;
using System.Text;
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
        public bool IsPacked { get; set; }
    }

    class Program
    {
        static string appPath;

        static int Main(string[] args)
        {
            appPath = AppDomain.CurrentDomain.BaseDirectory;

            while (true)
            {
                PrintBanner();

                Console.WriteLine("Hello, Senpai! (≧◡≦) What would you like to do?");
                Console.WriteLine("1. Extract MES.VFS");
                Console.WriteLine("2. Convert folder to CSV");
                Console.WriteLine("3. Convert folder to RPY");
                Console.WriteLine("4. Convert CSV to folder");
                Console.WriteLine("5. Pack folder to MES.VFS");
                Console.WriteLine("6. Leave (⊙_⊙;)");

                string input = GetInput("Please choose an option (o´▽`o) :");

                switch (input)
                {
                    case "1":
                        int exec = Extract();

                        if (exec == 0)
                            WriteMessage("Extraction completed successfully! ♥(*^-^*)♥");
                        else
                            WriteMessage("Oops! Something went wrong during extraction. (T_T)");

                        break;

                    case "6":
                        WriteMessage("Bye-bye, Senpai! Take care! (´｡• ω •｡`) ♡");
                        return 0;

                    default:
                        WriteMessage("Not implemented yet. (⊙_⊙;)");

                        break;
                }
                Thread.Sleep(1500);
                Console.Clear();
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

        static int Extract()
        {
            string fileName = GetInput("\n\nPlease enter the name of the file ♡\nDefault is \"MES.VFS\" (´｡• ω •｡`): ");

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

            if (!IsSaneCount(count))
            {
                WriteMessage("Invalid count. Please, double-check, Senpai! (⊙_⊙;)");
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

                Console.WriteLine($"Senpai! (≧◡≦) It's extracted: {entry.Name}");
            }

            return 0;
        }

        static string GetInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine();
        }
        static void WriteMessage(string msg)
        {
            Console.WriteLine($"\n{msg}\n");
        }

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
