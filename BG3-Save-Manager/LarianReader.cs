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
        public static byte[] VersionSignature = { 0x4C, 0x53, 0x50, 0x4B };
        public static byte[] LSFSignature = new byte[] { 0x4C, 0x53, 0x4F, 0x46 };

        public SaveMetadata? ReadFile(string path)
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

                        var metadataBytes = GetMetadataFile(numFiles, uncompressedList, fileStream, reader);
                        if (metadataBytes == null)
                            return null;

                        MemoryStream metadataStream = new MemoryStream(metadataBytes);
                        Resource lsf = ResourceUtils.LoadResource(metadataStream, LSLib.LS.Enums.ResourceFormat.LSF);

                        return ExtractMetadataFromResource(lsf);
                    }
                }
            }

            return null;
        }

        private SaveMetadata ExtractMetadataFromResource(Resource resource)
        {
            var metadataAttributes = resource.Regions["MetaData"].Children["MetaData"][0].Attributes;
            var versionAttributes = resource.Regions["MetaData"].Children["MetaData"][0].Children["GameVersions"][0].Children["GameVersion"];

            var metadata = new SaveMetadata(
                (string)metadataAttributes["GameSessionID"].Value,
                (string)metadataAttributes["LeaderName"].Value,
                (UInt64)metadataAttributes["SaveTime"].Value,
                (SaveMetadata.DifficultyType)Convert.ToInt32(metadataAttributes["Difficulty"].Value),
                (SaveMetadata.SaveType)Convert.ToInt32(metadataAttributes["SaveGameType"].Value),
                (UInt32)metadataAttributes["TimeStamp"].Value,
                (int)metadataAttributes["Seed"].Value,
                (string)versionAttributes.Last().Attributes["Object"].Value
            );

            return metadata;
        }
        
        private byte[]? GetMetadataFile(int numFiles, byte[] uncompressedList, FileStream fileStream, BinaryReader reader)
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



    }
       
}
