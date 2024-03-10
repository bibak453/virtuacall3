using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace moeTL_VC3
{
    internal class FileHandler
    {
        public string ReadAndDeleteFirstLine(string filePath)
        {
            string firstLine = null;

            try
            {
                // Read the first line from the file
                using (StreamReader reader = new StreamReader(filePath, Encoding.GetEncoding("Shift-jis")))
                {
                    firstLine = reader.ReadLine();
                }

                // Remove the first line from the file
                RemoveFirstLineFromFile(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            return firstLine;
        }

        private void RemoveFirstLineFromFile(string filePath)
        {
            try
            {
                // Read all lines except the first one
                string[] lines = File.ReadAllLines(filePath, Encoding.GetEncoding("shift-jis"));

                // Overwrite the file with all lines except the first one
                using (StreamWriter writer = new StreamWriter(filePath,false,Encoding.GetEncoding("shift-jis")))
                {
                    for (int i = 1; i < lines.Length; i++)
                    {
                        writer.WriteLine(lines[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while removing the first line from the file: " + ex.Message);
            }
        }
    }
}
