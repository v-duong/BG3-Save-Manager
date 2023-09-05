using LSLib.LS;
using LZ4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Printing;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using zlib;

namespace BG3_Save_Manager
{
    public class LarianReader
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct FileEntry18
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] Name;

            public UInt32 OffsetInFile1;
            public UInt16 OffsetInFile2;
            public Byte ArchivePart;
            public Byte Flags;
            public UInt32 SizeOnDisk;
            public UInt32 UncompressedSize;
        }

        public static byte[] VersionSignature = { 0x4C, 0x53, 0x50, 0x4B};
        public static byte[] LSFSignature = new byte[] { 0x4C, 0x53, 0x4F, 0x46 };

        public SaveMetadata ReadFile(string path)
        {
            FileStream fileStream = File.OpenRead(path);

            using (var reader = new BinaryReader(fileStream))
            {
                if (VersionSignature.SequenceEqual(reader.ReadBytes(4)))
                {
                    Int32 version = reader.ReadInt32();
                    if (version == 18)
                    {
                        var fileListOffset = reader.ReadUInt64();
                        var fileListSize = reader.ReadUInt32();
                        fileStream.Seek((long)fileListOffset, SeekOrigin.Begin);

                        int numFiles = reader.ReadInt32();
                        int compressedSize = reader.ReadInt32();
                        byte[] compressedFileList = reader.ReadBytes(compressedSize);

                        int fileBufferSize = Marshal.SizeOf(typeof(FileEntry18)) * numFiles;
                        var uncompressedList = new byte[fileBufferSize];

                        LZ4Codec.Decode(compressedFileList, 0, compressedFileList.Length, uncompressedList, 0, fileBufferSize);

                        MemoryStream metadataStream = new MemoryStream(GetMetadataFile(numFiles, uncompressedList, fileStream, reader));
                        var lsf = ResourceUtils.LoadResource(metadataStream, LSLib.LS.Enums.ResourceFormat.LSF);
                        //return ReadMetadata(metadataStream);
                    }
                }
            }

            return null;
        }

        private byte[] GetMetadataFile(int numFiles, byte[] uncompressedList, FileStream fileStream, BinaryReader reader)
        {
            var ms = new MemoryStream(uncompressedList);
            using var memReader = new BinaryReader(ms);

            for (int i = 0; i < numFiles; i++)
            {
                var entry = new FileEntry18
                {
                    Name = memReader.ReadBytes(256),
                    OffsetInFile1 = memReader.ReadUInt32(),
                    OffsetInFile2 = memReader.ReadUInt16(),
                    ArchivePart = memReader.ReadByte(),
                    Flags = memReader.ReadByte(),
                    SizeOnDisk = memReader.ReadUInt32(),
                    UncompressedSize = memReader.ReadUInt32()
                };

                int nameLen;
                for (nameLen = 0; nameLen < entry.Name.Length && entry.Name[nameLen] != 0; nameLen++)
                {
                }
                string name = Encoding.UTF8.GetString(entry.Name, 0, nameLen);
                if (name == "meta.lsf")
                {
                    ulong offset = entry.OffsetInFile1 | ((ulong)entry.OffsetInFile2 << 32);
                    fileStream.Seek((long)offset, SeekOrigin.Begin);
                    var compressedBytes = new byte[entry.SizeOnDisk];
                    reader.Read(compressedBytes, 0, (int)entry.SizeOnDisk);

                    return DecompressZLib(compressedBytes);
                }
            }

            return null;
        }

        private byte[] DecompressZLib(byte[] compressedBytes)
        {
            using (var decompressedStream = new MemoryStream())
            using (var compressedStream = new MemoryStream(compressedBytes))
            using (var zStream = new zlib.ZInputStream(compressedStream))
            {
                byte[] buf = new byte[0x10000];
                int length = 0;
                while ((length = zStream.read(buf, 0, buf.Length)) > 0)
                {
                    decompressedStream.Write(buf, 0, length);
                }
                return decompressedStream.ToArray();
            }
        }

        private byte[] DecompressLZ4(byte[] compressedBytes, int decompressedSize, bool chunked = false)
        {
            if (chunked)
            {
                return null;
            } else
            {
                var decompressed = new byte[decompressedSize];
                LZ4Codec.Decode(compressedBytes, 0, compressedBytes.Length, decompressed, 0, decompressedSize, true);
                return decompressed;
            }
        }

        private SaveMetadata ReadMetadata(Stream metadataStream)
        {
            using (var reader = new BinaryReader(metadataStream))
            {
                metadataStream.Seek(0, SeekOrigin.Begin);
                UInt32 signature = reader.ReadUInt32();
                if (signature != BitConverter.ToUInt32(LSFSignature, 0))
                {
                    Debug.WriteLine("mismatch");
                }

                var fileVersion = reader.ReadUInt32();

                if (fileVersion >= 6)
                {
                    Debug.WriteLine(fileVersion);
                }

                var gameVersion = reader.ReadInt64();

                var metadataRegions = new UInt32[10];
                for (int i = 0; i < 10; i++)
                {
                    metadataRegions[i] = reader.ReadUInt32();
                }

                var compression = reader.ReadByte();
                metadataStream.Seek(3, SeekOrigin.Current);
                var hasSiblingData = reader.ReadUInt32();
                Debug.WriteLine($"{hasSiblingData} sibling");

                var decompressedNames = DecompressLZ4(reader.ReadBytes((int)metadataRegions[1]), (int)metadataRegions[0]);
                var names = new List<List<string>>();
                using (var nameStream = new MemoryStream(decompressedNames))
                using (var nameReader = new BinaryReader(nameStream))
                {
                    var entryCount = nameReader.ReadUInt32();
                    for(int i = 0; i < entryCount; i++)
                    {
                        var stringCount = nameReader.ReadUInt16();

                        var sublist = new List<string>();
                        names.Add(sublist);

                        for (int j = 0; j <  stringCount; j++)
                        {
                            var nameLen = nameReader.ReadUInt16();
                            byte[] bytes = nameReader.ReadBytes(nameLen);
                            var name = Encoding.UTF8.GetString(bytes);
                            sublist.Add(name);
                        }
                    }
                }

   

            }
            return null;
        }
    }
}
