using System;
using System.IO;
using System.Text;

class TextExtractor
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Uso: TextExtractor.exe <arquivo.bin|arquivo.txt>");
            return;
        }

        string filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Erro: Arquivo '{filePath}' não encontrado.");
            return;
        }

        if (Path.GetExtension(filePath).ToLower() == ".txt")
        {
            ImportTexts(filePath);
        }
        else
        {
            ExtractTexts(filePath);
        }
    }

    static void ExtractTexts(string filePath)
    {
        try
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            if (fileBytes.Length < 12)
            {
                Console.WriteLine("Erro: Arquivo muito pequeno para conter uma header válida.");
                return;
            }

            uint offset1 = BitConverter.ToUInt32(fileBytes, 4);
            uint offset2 = BitConverter.ToUInt32(fileBytes, 8);
            uint textTableOffset = (offset1 * offset2) + 0x40;

            if (textTableOffset >= fileBytes.Length)
            {
                Console.WriteLine("Erro: Offset da tabela de textos fora dos limites do arquivo.");
                return;
            }

            string outputPath = Path.ChangeExtension(filePath, ".txt");
            using (StreamWriter writer = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                int currentOffset = (int)textTableOffset;
                MemoryStream textBuffer = new MemoryStream();

                while (currentOffset < fileBytes.Length)
                {
                    byte currentByte = fileBytes[currentOffset];
                    if (currentByte == 0x00)
                    {
                        if (textBuffer.Length > 0)
                        {
                            string text = ProcessTextBuffer(textBuffer.ToArray());
                            writer.WriteLine(text);
                            textBuffer.SetLength(0);
                        }
                        currentOffset++;
                        continue;
                    }
                    textBuffer.WriteByte(currentByte);
                    currentOffset++;
                }

                if (textBuffer.Length > 0)
                {
                    string text = ProcessTextBuffer(textBuffer.ToArray());
                    writer.WriteLine(text);
                }
            }
            Console.WriteLine($"Textos extraídos para: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
        }
    }

    static void ImportTexts(string textFilePath)
    {
        try
        {
            string binFilePath = Path.ChangeExtension(textFilePath, ".bin");
            if (!File.Exists(binFilePath))
            {
                Console.WriteLine($"Erro: Arquivo binário '{binFilePath}' não encontrado.");
                return;
            }

            byte[] binBytes = File.ReadAllBytes(binFilePath);
            uint idCount = BitConverter.ToUInt32(binBytes, 4);
            uint idSize = BitConverter.ToUInt32(binBytes, 8);
            uint textTableOffset = (idCount * idSize) + 0x40;

            string[] textLines = File.ReadAllLines(textFilePath, Encoding.UTF8);
            MemoryStream newTextTable = new MemoryStream();
            int[] originalEndOffsets = new int[textLines.Length];
            int[] newEndOffsets = new int[textLines.Length];
            int currentOriginalOffset = 0;
            int currentNewOffset = 0;

            // Calcula os offsets originais relativos ao início da tabela de textos
            int textOffset = (int)textTableOffset;
            for (int i = 0; i < textLines.Length && textOffset < binBytes.Length; i++)
            {
                int length = 0;
                while (textOffset + length < binBytes.Length && binBytes[textOffset + length] != 0x00)
                {
                    length++;
                }
                length++; // Inclui o 00
                originalEndOffsets[i] = currentOriginalOffset + length;
                currentOriginalOffset += length;
                textOffset += length;
            }

            // Constrói a nova tabela de textos e calcula os novos offsets
            for (int i = 0; i < textLines.Length; i++)
            {
                byte[] textBytes = Encoding.UTF8.GetBytes(textLines[i].Replace("[1b]", "\x1B").Replace("[0a]", "\n"));
                newTextTable.Write(textBytes, 0, textBytes.Length);
                newTextTable.WriteByte(0x00);
                newEndOffsets[i] = currentNewOffset + textBytes.Length + 1;
                currentNewOffset += textBytes.Length + 1;
            }

            // Inicializa newBinBytes com o tamanho do arquivo original
            byte[] newBinBytes = new byte[binBytes.Length];
            Array.Copy(binBytes, 0, newBinBytes, 0, binBytes.Length);

            // Salva os 4 bytes protegidos de cada ID
            byte[][] protectedBytes = new byte[idCount][];
            int[] protectedPositions = new int[idCount];
            for (int i = 0; i < idCount; i++)
            {
                int idOffset = 0x44 + (i * (int)idSize);
                int protectedOffset = idOffset + (int)idSize - 8;
                protectedBytes[i] = new byte[4];
                Array.Copy(binBytes, protectedOffset, protectedBytes[i], 0, 4);
                protectedPositions[i] = protectedOffset;
            }

            // Substitui todos os tamanhos originais pelos novos em todo o arquivo
            for (int i = 0; i < textLines.Length; i++)
            {
                byte[] originalSizeBytes = BitConverter.GetBytes(originalEndOffsets[i]);
                byte[] newSizeBytes = BitConverter.GetBytes(newEndOffsets[i]);

                for (int j = 0; j < newBinBytes.Length - 3; j += 4)
                {
                    if (newBinBytes[j] == originalSizeBytes[0] &&
                        newBinBytes[j + 1] == originalSizeBytes[1] &&
                        newBinBytes[j + 2] == originalSizeBytes[2] &&
                        newBinBytes[j + 3] == originalSizeBytes[3])
                    {
                        Array.Copy(newSizeBytes, 0, newBinBytes, j, 4);
                        Console.WriteLine($"Substituído {BitConverter.ToInt32(originalSizeBytes, 0):X8} por {BitConverter.ToInt32(newSizeBytes, 0):X8} na posição {j:X}");
                    }
                }
            }

            // Restaura os bytes protegidos
            for (int i = 0; i < idCount; i++)
            {
                Array.Copy(protectedBytes[i], 0, newBinBytes, protectedPositions[i], 4);
            }

            // Copia a nova tabela de textos e ajusta o tamanho do arquivo
            int finalSize = (int)textTableOffset + (int)newTextTable.Length;
            Array.Resize(ref newBinBytes, finalSize);
            newTextTable.Position = 0;
            newTextTable.Read(newBinBytes, (int)textTableOffset, (int)newTextTable.Length);

            File.WriteAllBytes(binFilePath, newBinBytes);
            Console.WriteLine($"Textos importados para: {binFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao importar textos: {ex.Message}");
        }
    }

    static string ProcessTextBuffer(byte[] buffer)
    {
        string text = Encoding.UTF8.GetString(buffer);
        return text.Replace("\x1B", "[1b]").Replace("\n", "[0a]");
    }
}
