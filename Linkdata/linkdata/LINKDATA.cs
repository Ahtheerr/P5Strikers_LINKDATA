using System;
using System.Collections.Generic;
using System.IO;
using LibBuilders;

namespace linkdata;

public class LINKDATA
{
	public struct IDX_ENTRY
	{
		public long Offset;

		public long USize;

		public long CSize;

		public long IsCompressed;

		public void Load(BinaryReader br)
		{
			Offset = br.ReadInt64();
			USize = br.ReadInt64();
			CSize = br.ReadInt64();
			IsCompressed = br.ReadInt64();
		}
	}

	public struct BIN_FILE
	{
		public uint unk1;

		public int ChunkCount;

		public int FullSize;

		public List<int> ChunkPayloadSize;

		public byte[] PayloadData;

		public void Load(BinaryReader br)
		{
			ChunkPayloadSize = new List<int>();
			unk1 = br.ReadUInt32();
			ChunkCount = br.ReadInt32();
			FullSize = br.ReadInt32();
			for (int i = 0; i < ChunkCount; i++)
			{
				ChunkPayloadSize.Add(br.ReadInt32());
			}
			int num = 12 + ChunkCount * 4;
			long num2 = Util.AlignUp(num, 128) - num;
			br.ReadBytes((int)num2);
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
				for (int j = 0; j < ChunkCount; j++)
				{
					num = br.ReadInt32();
					byte[] buffer;
					if (ChunkPayloadSize[j] - 4 == 0)
					{
						buffer = new byte[4];
					}
					else if (num == ChunkPayloadSize[j] - 4)
					{
						buffer = UtilZlib.Decompress(br.ReadBytes(num), FullSize);
					}
					else
					{
						br.SeekTo(br.Position() - 4);
						buffer = br.ReadBytes(ChunkPayloadSize[j]);
					}
					num2 = Util.AlignUp(ChunkPayloadSize[j], 128) - ChunkPayloadSize[j];
					if (num2 > 0)
					{
						br.ReadBytes((int)num2);
					}
					binaryWriter.Write(buffer);
				}
				PayloadData = memoryStream.ToArray();
			}
			if (PayloadData.Length != FullSize)
			{
				Console.WriteLine($"Error invalid size after decompressing chunks. ({br.Position()})");
				Console.ReadKey();
			}
		}
	}

	public const int DATA_PADDING = 128;

	public const int IDX_ENTRY_SIZE = 32;

	private List<IDX_ENTRY> Entries;

	public void Load(string sPath)
	{
		string text = Path.GetDirectoryName(sPath) + "\\" + Path.GetFileNameWithoutExtension(sPath);
		string path = text + ".BIN";
		string path2 = text + ".IDX";
		if (!File.Exists(path) || !File.Exists(path2))
		{
			return;
		}
		Util.CreateDir(text);
		using BinaryReader binaryReader2 = new BinaryReader(File.OpenRead(path));
		using BinaryReader binaryReader = new BinaryReader(File.OpenRead(path2));
		int num = (int)binaryReader.BaseStream.Length / 32;
		Entries = new List<IDX_ENTRY>();
		for (int i = 0; i < num; i++)
		{
			IDX_ENTRY item = default(IDX_ENTRY);
			item.Load(binaryReader);
			Entries.Add(item);
		}
		for (int j = 0; j < num; j++)
		{
			string text2 = text + $"\\FILE_{j:D5}.dat";
			if (File.Exists(text2))
			{
				continue;
			}
			Console.Write("Writing " + text2 + "... ");
			binaryReader2.SeekTo(Entries[j].Offset);
			if (Entries[j].IsCompressed == 1)
			{
				if (Entries[j].CSize > 0)
				{
					BIN_FILE bIN_FILE = default(BIN_FILE);
					bIN_FILE.Load(binaryReader2);
					File.WriteAllBytes(text2, bIN_FILE.PayloadData);
				}
			}
			else if (Entries[j].USize > 0)
			{
				byte[] bytes = binaryReader2.ReadBytes((int)Entries[j].USize);
				File.WriteAllBytes(text2, bytes);
			}
			Console.WriteLine("Done!");
		}
	}

    public void InjectFile(string sPath, int id, string sFilePath)
    {
        FileInfo fileInfo = new FileInfo(sFilePath);
        if (fileInfo.Exists)
        {
            string basePath = Path.GetDirectoryName(sPath) + "\\" + Path.GetFileNameWithoutExtension(sPath);
            string text2 = basePath + ".BIN";
            string path = basePath + ".IDX";
            if (File.Exists(text2) && File.Exists(path))
            {
                byte[] array = File.ReadAllBytes(sFilePath);
                int num = 0;
                long fileLength = Util.GetFileLength(text2);
                if (!Util.IsAligned(array.Length, 128))
                {
                    num = (int)(Util.AlignUp(array.Length, 128) - array.Length);
                }
                using BinaryWriter binaryWriter2 = new BinaryWriter(File.OpenWrite(text2));
                using BinaryWriter binaryWriter = new BinaryWriter(File.OpenWrite(path));
                binaryWriter.SeekTo(id * 32);
                binaryWriter.Write(fileLength);
                binaryWriter.Write((long)array.Length);
                binaryWriter.Write((long)array.Length);
                binaryWriter.Write(0L);
                binaryWriter2.SeekTo(fileLength);
                binaryWriter2.Write(array);
                if (num > 0)
                {
                    Util.WritePadding(binaryWriter2, num);
                }
                return;
            }
            Console.WriteLine("Can't find IDX or BIN file!");
        }
        else
        {
            Console.WriteLine("Can't find Big file!");
        }
    }


    public static void Build(string sInPath, string sOutFile)
	{
	}
}
