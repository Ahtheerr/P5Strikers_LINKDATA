using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

public class LinkDataExtractor
{
    private byte[] idxData;
    private byte[] binData;
    private bool isBigEndian;
    private bool useDecryption;
    private bool useEntryFilter;

    public LinkDataExtractor(string idxPath, string binPath, bool useDecryption, bool useEntryFilter)
    {
        idxData = File.ReadAllBytes(idxPath);
        binData = File.ReadAllBytes(binPath);
        this.useDecryption = useDecryption;
        this.useEntryFilter = useEntryFilter;
        DetermineEndianness();
    }

    private void DetermineEndianness()
    {
        if (idxData.Length < 16)
            throw new Exception("IDX file too small");
        byte[] tmpBytes = new byte[4];
        Array.Copy(idxData, 0x0C, tmpBytes, 0, 4);
        uint tmp = BitConverter.ToUInt32(tmpBytes, 0);
        isBigEndian = (tmp != 0);
    }

    private long ReadLong(byte[] data, int offset)
    {
        byte[] bytes = new byte[8];
        Array.Copy(data, offset, bytes, 0, 8);
        if (isBigEndian)
            Array.Reverse(bytes);
        return BitConverter.ToInt64(bytes, 0);
    }

    private int ReadInt(byte[] data, int offset)
    {
        byte[] bytes = new byte[4];
        Array.Copy(data, offset, bytes, 0, 4);
        if (isBigEndian)
            Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    private bool ShouldExtract(int index)
    {
        if (index >= 5912 && index != 8158 && index != 8168 && index != 8178)
            return false;

        if (!useEntryFilter)
            return true;

        return index == 0 || (index % 8 == 0) || index == 8158 || index == 8168 || index == 8178;
    }

    public void Extract(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        int entrySize = 32;
        int numEntries = idxData.Length / entrySize;
        for (int i = 0; i < numEntries; i++)
        {
            if (!ShouldExtract(i))
                continue;

            int offset = i * entrySize;
            long OFFSET = ReadLong(idxData, offset);
            long SIZE = ReadLong(idxData, offset + 8);
            long ZSIZE = ReadLong(idxData, offset + 16);
            long ZIP = ReadLong(idxData, offset + 24);
            if (SIZE == 0)
                continue;

            string outputPath = Path.Combine(outputDir, $"{i}.bin");
            Console.WriteLine($"Processando entrada {i}: OFFSET={OFFSET}, SIZE={SIZE}, ZSIZE={ZSIZE}, ZIP={ZIP}");

            byte[] data;
            if (ZIP == 0)
            {
                if (OFFSET + SIZE > binData.Length)
                {
                    Console.WriteLine($"Erro: OFFSET + SIZE excede o tamanho do BIN para entrada {i}");
                    continue;
                }
                data = new byte[SIZE];
                Array.Copy(binData, OFFSET, data, 0, SIZE);
            }
            else
            {
                int pos = (int)OFFSET;
                if (pos + 12 > binData.Length)
                {
                    Console.WriteLine($"Erro: Offset inicial excede o tamanho do BIN para entrada {i}");
                    continue;
                }
                int CHUNK_SIZE = ReadInt(binData, pos);
                pos += 4;
                int CHUNKS = ReadInt(binData, pos);
                pos += 4;
                int totalSize = ReadInt(binData, pos);
                pos += 4;

                List<int> chunkZSizes = new List<int>();
                for (int c = 0; c < CHUNKS; c++)
                {
                    if (pos + 4 > binData.Length)
                    {
                        Console.WriteLine($"Erro: Não há espaço para ler chunkZSize para entrada {i}, chunk {c}");
                        break;
                    }
                    int chunkZSize = ReadInt(binData, pos);
                    chunkZSizes.Add(chunkZSize);
                    pos += 4;
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    int currentSize = 0;
                    for (int c = 0; c < chunkZSizes.Count; c++)
                    {
                        pos = ((pos + 127) / 128) * 128;
                        int chunkZSize = chunkZSizes[c];
                        if (pos + chunkZSize > binData.Length)
                        {
                            Console.WriteLine($"Erro: Chunk {c} excede o tamanho do BIN para entrada {i}");
                            break;
                        }

                        if (currentSize + chunkZSize == totalSize)
                        {
                            byte[] chunkData = new byte[chunkZSize];
                            Array.Copy(binData, pos, chunkData, 0, chunkZSize);
                            ms.Write(chunkData, 0, chunkData.Length);
                            currentSize += chunkData.Length;
                        }
                        else
                        {
                            byte[] compressedChunk = new byte[chunkZSize];
                            Array.Copy(binData, pos, compressedChunk, 0, chunkZSize);
                            try
                            {
                                Inflater inflater = new Inflater(CHUNK_SIZE == -1);
                                using (MemoryStream compressedStream = new MemoryStream(compressedChunk))
                                using (InflaterInputStream zlibStream = new InflaterInputStream(compressedStream, inflater))
                                {
                                    byte[] buffer = new byte[4096];
                                    int bytesRead;
                                    while ((bytesRead = zlibStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        ms.Write(buffer, 0, bytesRead);
                                        currentSize += bytesRead;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Falha na descompressão do chunk {c} da entrada {i}: {ex.Message}");
                                try
                                {
                                    Inflater inflater = new Inflater(true);
                                    using (MemoryStream compressedStream = new MemoryStream(compressedChunk))
                                    using (InflaterInputStream zlibStream = new InflaterInputStream(compressedStream, inflater))
                                    {
                                        byte[] buffer = new byte[4096];
                                        int bytesRead;
                                        while ((bytesRead = zlibStream.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            ms.Write(buffer, 0, bytesRead);
                                            currentSize += bytesRead;
                                        }
                                    }
                                }
                                catch (Exception ex2)
                                {
                                    Console.WriteLine($"Falha no fallback para deflate puro no chunk {c} da entrada {i}: {ex2.Message}");
                                    break;
                                }
                            }
                        }
                        pos += chunkZSize;
                    }
                    data = ms.ToArray();
                    if (data.Length != totalSize)
                        Console.WriteLine($"Aviso: Tamanho descomprimido não corresponde para entrada {i}: esperado {totalSize}, obtido {data.Length}");
                }
            }

            if (useDecryption)
            {
                Console.WriteLine($"Descriptografando entrada {i} com ID={i}");
                LinkEncryption.Decrypt(data.AsSpan(), (uint)i);
            }

            File.WriteAllBytes(outputPath, data);
            Console.WriteLine($"Extraído: {outputPath}");
        }
    }
}

public static class LinkEncryption
{
    public static void Encrypt(Span<byte> data, uint id)
        => Decrypt(data, id);

    public static void Decrypt(Span<byte> data, uint id)
    {
        var gen = new Mersenne(id + 0x7F6BA458);
        var size = data.Length;

        for (var i = 0; i < size; i++)
        {
            var shift = (size - i >= 2) && (gen.Next() & 1) != 0 ? 1 : 0;
            var r = gen.Next();

            data[i] ^= (byte)r;
            if (i + shift < size)
                data[i + shift] ^= (byte)(r >> 8);
            i += shift;
        }
    }

    public static void EncryptFolder(string inputDir)
    {
        string outputDir = inputDir + "_enc";
        Directory.CreateDirectory(outputDir);

        var files = Directory.GetFiles(inputDir, "*.bin").Select(f => new
        {
            Index = int.Parse(Path.GetFileNameWithoutExtension(f)),
            Data = File.ReadAllBytes(f)
        }).OrderBy(f => f.Index).ToList();

        foreach (var file in files)
        {
            byte[] data = (byte[])file.Data.Clone(); // Clona para não modificar o original
            Console.WriteLine($"Criptografando {file.Index}.bin com ID={file.Index}");
            Encrypt(data.AsSpan(), (uint)file.Index);

            string outputPath = Path.Combine(outputDir, $"{file.Index}.bin");
            File.WriteAllBytes(outputPath, data);
            Console.WriteLine($"Criptografado: {outputPath}");
        }

        Console.WriteLine($"Todos os arquivos foram criptografados em {outputDir}");
    }
}

public class Mersenne
{
    readonly uint[] State = new uint[4];

    public Mersenne(uint seed)
    {
        Init(seed);
    }

    public void Init(uint seed)
    {
        State[0] = 0x6C078965 * (seed ^ (seed >> 30));

        for (int i = 1; i < 4; i++)
            State[i] = (uint)(0x6C078965 * (State[i - 1] ^ (State[i - 1] >> 30)) + i);
    }

    public uint Next()
    {
        var temp = State[0] ^ (State[0] << 11);
        State[0] = State[1];
        State[1] = State[2];
        State[2] = State[3];
        State[3] ^= temp ^ ((temp ^ (State[3] >> 11)) >> 8);

        return State[3];
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Uso para extração: LinkDataExtractor <idx_path> <bin_path> <output_dir> [-PC] [-En]");
            Console.WriteLine("Uso para criptografia: LinkDataExtractor Enc <input_dir>");
            return;
        }

        string firstArg = args[0];

        if (firstArg.ToUpper() == "ENC")
        {
            // Modo de criptografia
            if (args.Length != 2 || !Directory.Exists(args[1]))
            {
                Console.WriteLine("Erro: Especifique uma pasta válida para criptografia. Uso: LinkDataExtractor Enc <input_dir>");
                return;
            }

            string inputDir = args[1];
            try
            {
                LinkEncryption.EncryptFolder(inputDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro geral na criptografia: {ex.Message}");
            }
        }
        else
        {
            // Modo de extração
            if (args.Length < 3)
            {
                Console.WriteLine("Uso para extração: LinkDataExtractor <idx_path> <bin_path> <output_dir> [-PC] [-En]");
                return;
            }

            string idxPath = args[0];
            string binPath = args[1];
            string outputDir = args[2];
            bool useDecryption = false;
            bool useEntryFilter = false;

            for (int i = 3; i < args.Length; i++)
            {
                if (args[i].ToUpper() == "-PC")
                    useDecryption = true;
                else if (args[i].ToUpper() == "-EN")
                    useEntryFilter = true;
            }

            try
            {
                LinkDataExtractor extractor = new LinkDataExtractor(idxPath, binPath, useDecryption, useEntryFilter);
                extractor.Extract(outputDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro geral na extração: {ex.Message}");
            }
        }
    }
}