using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace moeTL_VC3
{
    internal class FileEntry
    {
        public string Name { get; set; }
        public uint Offset { get; set; }
        public uint Size { get; set; }
        public uint UnpackedSize { get; set; }
        public byte[] Data { get; set; } = [];
        public bool IsPacked { get; set; }

        // Constructor with parameters
        public FileEntry(string name, uint offset, uint size, uint unpackedSize, byte[] sourceArray, bool isPacked) : this(name, offset, size, unpackedSize, isPacked)
        {
            Data = new byte[Size];
            Array.Copy(sourceArray, Offset, Data, 0, Size);
        }

        // Constructor without initializing data (for internal use)
        private FileEntry(string name, uint offset, uint size, uint unpackedSize, bool isPacked)
        {
            Name = name;
            Offset = offset;
            Size = size;
            UnpackedSize = unpackedSize;
            IsPacked = isPacked;
        }
    }

}
