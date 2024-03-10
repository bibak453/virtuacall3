using System.Text;
using DiscUtils.Iso9660;

namespace moeTL_VC3
{
    internal class Program
    {
        public static string AppPath { get; set; } = @"C:\temp";

        static int Main()
        {
            AppPath = AppDomain.CurrentDomain.BaseDirectory;
            
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            bool loop = true;

            while (loop)
            {
                string input;
                int output;
                int exec;

                do
                {
                    Console.WriteLine("Hello, Senpai! (Ă˘â€°Â§?Ă˘â€°Â¦) What would you like to do?");
                    Console.WriteLine("1. Extract MES.VFS");
                    Console.WriteLine("2. Convert folder to CSV");
                    Console.WriteLine("3. Convert folder to RPY");
                    Console.WriteLine("4. Convert CSV to folder");
                    Console.WriteLine("5. Pack folder to MES.VFS");
                    Console.WriteLine("6. Analyze Characters");
                    Console.WriteLine("7. Leave (?_?;)");

                    input = GetInput("Please choose an option (oĂ‚Â´Ă˘â€“Ëť`o) :");

                } while (!int.TryParse(input, out output) || output < 1 || output > 7);

                Console.Clear();

                switch (output)
                {
                    case 1:
                        exec = Extract();
                        CompletionMessage(exec == 0, 0);
                        break;
                    case 2:
                        exec = ConvertCSV();
                        CompletionMessage(exec == 0, 1);
                        break;

                    case 5:
                        exec = Pack();
                        CompletionMessage(exec == 0, 2);
                        break;

                    case 6:
                        exec = Analyze();
                        CompletionMessage(exec == 0, 3);
                        break;

                    case 7:
                        WriteMessage("Bye-bye, Senpai! Take care! (Ă‚Â´?ËťË?? Ä?? ??ËťË‡`) ?");
                        loop = false;
                        break;

                    default:
                        WriteMessage("Not implemented yet Senpai. (?_?;)");
                        break;
                }
                
                Thread.Sleep(1500);
                Console.Clear();
            }

            return 0;
        }
        
        static string GetInput(string prompt)
        {
            Console.Write($"\n{prompt}");
            string output = Console.ReadLine();

            if (string.IsNullOrEmpty(output))
                return "";

            return output;
        }

        static string GetFolder()
        {
            string[] SubDirectories = Directory.GetDirectories(AppPath);
            string TargetDirectory;

            if (SubDirectories.Length <= 0)
            {
                WriteMessage("There are no folders to work on Senpai :(");
                return "";
            }

            string input;
            int output;

            do
            {
                WriteMessage("Directories:");

                for (int i = 0; i < SubDirectories.Length; i++)
                {
                    Console.WriteLine($"[{i + 1}]\t{SubDirectories[i]}");
                }

                input = GetInput("Pick a folder: ");

                Console.Clear();

            } while (!int.TryParse(input, out output) || output < 1 || output > SubDirectories.Length);

            TargetDirectory = SubDirectories[output - 1];

            WriteMessage($"You picked:\t{TargetDirectory}");

            return TargetDirectory;
        }

        static string[] GetOriginalOrder()
        {
            try
            {
                string[] Files = File.ReadAllLines(Path.Combine(AppPath, "originalOrder.txt"));
                return Files;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading the file: " + ex.Message);
                return [];
            }
        }

        static void WriteMessage(string prompt)
        {
            Console.WriteLine($"\n{prompt}\n");
        }

        static void WriteIso()
        {
            string FolderPath = Path.Combine(AppPath, "MOD");
            string[] Files = Directory.GetFiles(FolderPath);

            CDBuilder ISO = new CDBuilder
            {
                UseJoliet = true,
                VolumeIdentifier = "MES"
            };

            foreach (string file in Files)
            {
                byte[] fileBytes = File.ReadAllBytes(file);
                string fileName = Path.GetFileName(file);
                ISO.AddFile(fileName, fileBytes);
            }

            ISO.Build(Path.Combine(AppPath, "MES.ISO"));
        }


    static void CompletionMessage(bool positive, int action)
        {
            string[] actions = [
                "Extraction",
                "Converting",
                "Packing",
                "Analyzing"
            ];

            if (positive)
                WriteMessage($"{actions[action]} was super fine Senpai! ?(*^-^*)?");
            else
                WriteMessage($"Noooo! Something went wrong with {actions[action].ToLower()} Senpai. (T_T)");
        }

        static int Extract()
        {
            string File = GetInput("\n\nPlease enter the name of the file ?\nDefault is \"MES.VFS\": ");
            
            if (File == "")
                File = "MES.VFS";

            string FilePath = Path.Combine(AppPath, File);

            if (!System.IO.File.Exists(FilePath))
            {
                WriteMessage("The file could not be found, Senpai. (T_T)");
                return 1;
            }

            byte[] Data = System.IO.File.ReadAllBytes(FilePath);
            int MaxOffset = Data.Length;

            int Signature   =   ReadInt16(Data, 0);
            int Version     =   ReadInt16(Data, 2);
            int FileCount   =   ReadInt16(Data, 4);
            int EntrySize   =   ReadInt16(Data, 6);
            int IndexSize   =   ReadInt32(Data, 8);

            if (0x4656 != Signature && 0x4C56 != Signature)
            {
                WriteMessage("Invalid file signature, Senpai. (>.<)");
                return 1;
            }

            if (Version >= 0x0200)
            {
                WriteMessage("Senpai, The version is higher than expected. Need to use other method (not implemented) (>.<)");
                return 1;
            }

            if (!IsSaneCount(FileCount))
            {
                WriteMessage("Invalid count. Please, double-check, Senpai! (?_?;)");
                return 1;
            }

            if (EntrySize <= 0 || IndexSize <= 0 || MaxOffset != ReadUInt32(Data, 0xC))
            {
                WriteMessage("Invalid file size or entry size, Senpai. (T_T)");
                return 1;
            }

            string TargetPath = Path.Combine(AppPath, Path.GetFileName(FilePath) + "~");

            if (Directory.Exists(TargetPath))
            {
                int iter = 1;
                string tempName;

                do
                {
                    tempName = $"{TargetPath}{iter:D3}";
                    iter++;
                } while (Directory.Exists(tempName));

                TargetPath = tempName;
            }

            Directory.CreateDirectory(TargetPath);

            List<FileEntry> fileList = new List<FileEntry>();
            int IndexOffset = 0x10;

            for (int i = 0; i < FileCount; i++)
            {
                if (ReadUInt32(Data, IndexOffset + 0x13) > 0)
                {
                    var entry = new FileEntry(
                        ReadString(Data, IndexOffset, 0x13, i),
                        ReadUInt32(Data, IndexOffset + 0x13),
                        ReadUInt32(Data, IndexOffset + 0x17),
                        ReadUInt32(Data, IndexOffset + 0x1B),
                        Data,
                        0x01 != ReadByte(Data, IndexOffset + 0x1F)
                    );

                    fileList.Add(entry);
                }

                IndexOffset += EntrySize;
            }

            foreach (var entry in fileList)
            {
                string filePath = Path.Combine(TargetPath, entry.Name);

                try
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Create))
                    {
                        fs.Write(entry.Data, 0, entry.Data.Length);
                    }
                }
                catch (Exception ex)
                {
                    WriteMessage($"Failed to write the file '{entry.Name}': {ex.Message}, Senpai. (T_T)");
                }

                Console.WriteLine($"Senpai! (ç«•ď˝§?ç«•ď˝¦) It's extracted: {entry.Name}");
            }

            try
            {
                using (FileStream fs = System.IO.File.Create(Path.Combine(AppPath, "originalOrder.txt")))
                {
                    foreach (var entry in fileList)
                    {
                        fs.Write(Encoding.GetEncoding("shift-jis").GetBytes($"{entry.Name}\n"));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteMessage($"Failed to write the file '': {ex.Message}, Senpai. (T_T)");
            }
            
            return 0;
        }
        
        static int Pack()
        {
            string TargetFolder = GetFolder();

            if (TargetFolder == "")
            { 
                WriteMessage("Senpai, you didn't pick anythin :(");
                return 1;
            }

            List<FileEntry> fileList = new List<FileEntry>();
            string[] files = GetOriginalOrder();

            byte[] header = File.ReadAllBytes(Path.Combine(AppPath, "header.BIN"));
            byte[] scenarioData = [];
            
            uint fileOffset = 0;

            foreach (string file in files)
            {
                byte[] fileData = File.ReadAllBytes(Path.Combine(TargetFolder, file));

                if (file == "V_OP01.MES")
                {
                    FileHandler fileHandler = new FileHandler();
                    string insertLine = fileHandler.ReadAndDeleteFirstLine(Path.Combine(AppPath, "tempLines.txt"));

                    string original = Encoding.GetEncoding("shift-jis").GetString(fileData);

                    original.Replace("XOXOXOXO", insertLine);

                    fileData = Encoding.GetEncoding("shift-jis").GetBytes(original);

                }

                FileEntry entry = new FileEntry(
                    Path.GetFileName(file),
                    0,
                    (uint)fileData.Length,
                    (uint)fileData.Length,
                    fileData,
                    false
                );
                entry.Offset = fileOffset;

                fileList.Add(entry);

                fileOffset += (uint)entry.Data.Length;
                scenarioData = AddBytes(scenarioData, fileData);
            }

            int entrySize = 32;
            int entryOffset = 0x10;
            int indexSize = entrySize * fileList.Count;

            using (FileStream destStream = new FileStream(Path.Combine(AppPath, "MOD", $"MES_{DateTime.Now.ToString("HHmmss")}.VFS"), FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(destStream))
                {
                    // Write header
                    writer.Write((short)0x4656);
                    writer.Write((short)0x100);
                    writer.Write((short)fileList.Count);
                    writer.Write((short)entrySize);
                    writer.Write(indexSize);

                    writer.Write((byte)0x31);
                    writer.Write((byte)0xB6);
                    writer.Write((byte)0x3B);
                    writer.Write((byte)0x00);

                    // Write list
                    writer.Write(header);

                    // Get current position
                    long scenarioPosition = writer.BaseStream.Position;

                    //Write scenario
                    writer.Write(scenarioData);
                    writer.Seek(entryOffset, SeekOrigin.Begin);

                    // Modify the List data
                    foreach (FileEntry entry in fileList)
                    {
                        entry.Offset += (uint)scenarioPosition;

                        writer.Seek(entryOffset + 0x13, SeekOrigin.Begin);

                        writer.Write(entry.Offset);
                        writer.Write(entry.Size);

                        entryOffset += entrySize;
                    }
                    long totalFileSize = destStream.Length;

                    // Seek to position 0xC from the beginning of the file
                    destStream.Seek(0xC, SeekOrigin.Begin);

                    // Write the total file size as a uint32 value
                    writer.Write((uint)totalFileSize);
                }
            }

            WriteIso();

            return 0;
        }

        static int Analyze()
        {
            string TargetPath = GetFolder();

            if (string.IsNullOrEmpty(TargetPath))
            {
                return 1;
            }

            HashSet<char> uniqueCharacters = new HashSet<char>();
            HashSet<char> excludedCharacters = new HashSet<char>();

            string Done = File.ReadAllText(Path.Combine(AppPath, "DoneCharacters.txt"), Encoding.GetEncoding("shift-jis"));
            Console.WriteLine(Done);
            foreach (char character in Done)
            {
                if (!excludedCharacters.Contains(character))
                {
                    excludedCharacters.Add(character);
                }
            }

            try
            {
                if (Directory.Exists(TargetPath))
                {
                    string[] files = Directory.GetFiles(TargetPath);

                    foreach (string file in files)
                    {
                        string content = File.ReadAllText(file, Encoding.GetEncoding("shift-jis"));

                        foreach (char c in content)
                        {
                            if (!excludedCharacters.Contains(c) && !uniqueCharacters.Contains(c))
                            {
                                uniqueCharacters.Add(c);
                            }
                        }
                    }

                    List<char> sortedList = uniqueCharacters.ToList();

                    sortedList.Sort();

                    string output = "";
                    int counter = 0;
                    foreach(char c in sortedList)
                    {

                        output += c;
                        output += "　";
                        if (counter > 56)
                        {
                            output += "ｺ\n";
                            counter = 0;
                            output +="(#";
                        }
                        counter++;
                    }

                    File.WriteAllBytes(Path.Combine(AppPath, "UniqueCharacters.txt"), Encoding.GetEncoding("shift-jis").GetBytes(output));
                }
                else
                {
                    WriteMessage("Senpai! Did you delete it on purpose? :(");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return 0;
        }

        static int ConvertCSV()
        {
            string TargetDirectory = GetFolder();

            if (TargetDirectory == "")
                return 1;

            try
            {
                string newTargetDirectory = Path.Combine(AppPath, Path.GetDirectoryName(TargetDirectory), "_regex");

                Directory.CreateDirectory(newTargetDirectory);

                string[] Files = Directory.GetFiles(TargetDirectory);

                foreach (string File in Files)
                {
                    RewriteFile(Path.GetFileName(File), Path.Combine(newTargetDirectory, Path.GetFileName(File)));
                }

                Console.WriteLine("Regex matches extracted and saved to files in the new folder.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return 0;
        }
        
        static void RewriteFile(string InputFile, string OutputFile)
        {
            try
            {
                string ScenarioFile = File.ReadAllText(InputFile, Encoding.GetEncoding("shift-jis"));
                
                ScenarioFile = ScenarioFile.Replace("\n", "");
                ScenarioFile = ScenarioFile.Replace("(", "\n(");
                //Text = Decrypt(Text);

                File.WriteAllText(OutputFile, ScenarioFile, Encoding.GetEncoding("shift_jis"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }

        static string Decrypt(string input)
        {
            List<CharacterMap> ReplaceMap =
            [
                new CharacterMap("Z", "莠ｺ"),
            ];

            string decrypted = input;
            // TODO: Add a check so it will convert only dialogue
            foreach (CharacterMap ch in ReplaceMap)
            {
                decrypted = decrypted.Replace(ch.A, ch.B);
            }

            return decrypted;
        }

        public static bool IsSaneCount(int count)
        {
            return count > 0 && count < 0x40000;
        }
        
        static string ReadString(byte[] byteArray, int offset, int size, int count)
        {
            try
            {
                string decodedString = Encoding.GetEncoding("shift-jis").GetString(byteArray, offset, size);

                int index = decodedString.IndexOf(".MES");

                if (index != -1)
                {
                    return decodedString[..(index + 4)].Trim();
                }
            }
            catch (Exception ex)
            {
                WriteMessage("Failed to decode the filename, Senpai. (T_T)");
                WriteMessage(ex.Message);
            }

            return $"{count:D3}.MES";
        }

        static byte[] AddBytes(byte[] originalArray, byte[] bytesToAdd)
        {
            int originalLength = originalArray.Length;
            int bytesToAddLength = bytesToAdd.Length;
            int newSize = originalLength + bytesToAddLength;

            byte[] newArray = new byte[newSize];

            Array.Copy(originalArray, newArray, originalLength);
            Array.Copy(bytesToAdd, 0, newArray, originalLength, bytesToAddLength);

            return newArray;
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
    }
}
