using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace linkdata;

internal class Program
{
    private static void Init()
    {
        string text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        Console.WriteLine("Modified Dragon Quest Builders Tool" + text + "\n");
    }

    private static void Exit(bool showFinished = false)
    {
        if (showFinished)
        {
            Console.WriteLine();
            Console.WriteLine("Finished, please press any key to exit...");
            Console.ReadKey();
        }
        Environment.Exit(0);
    }

    private static byte[] EncryptData(byte[] data, uint id)
    {
        byte[] encryptedData = (byte[])data.Clone(); // Clone to preserve original
        var gen = new Mersenne(id + 0x7F6BA458);
        var size = encryptedData.Length;

        for (var i = 0; i < size; i++)
        {
            var shift = (size - i >= 2) && (gen.Next() & 1) != 0 ? 1 : 0;
            var r = gen.Next();

            encryptedData[i] ^= (byte)r;
            if (i + shift < size)
                encryptedData[i + shift] ^= (byte)(r >> 8);
            i += shift;
        }

        return encryptedData;
    }

    private static void Main(string[] args)
    {
        Init();

        bool encrypt = false;
        string[] filteredArgs = args;

        // Check for -Enc argument
        if (args.Any(arg => arg.ToUpper() == "-ENC"))
        {
            encrypt = true;
            filteredArgs = args.Where(arg => arg.ToUpper() != "-ENC").ToArray();
        }

        if (filteredArgs.Length == 1 && File.Exists(filteredArgs[0]))
        {
            new LINKDATA().Load(filteredArgs[0]);
            Exit();
        }

        if (filteredArgs.Length == 4 && filteredArgs[0] == "inject" && File.Exists(filteredArgs[1]) && File.Exists(filteredArgs[3]))
        {
            int num = int.Parse(filteredArgs[2]);
            string inputFile = filteredArgs[3];

            if (encrypt)
            {
                Console.WriteLine($"Encrypting data for {inputFile} with ID={num}...");
                byte[] data = File.ReadAllBytes(inputFile);
                data = EncryptData(data, (uint)num);
                string tempFile = Path.GetTempFileName();
                File.WriteAllBytes(tempFile, data);
                Console.WriteLine($"Injecting encrypted data into slot {num}...");
                new LINKDATA().InjectFile(filteredArgs[1], num, tempFile);
                File.Delete(tempFile); // Clean up
            }
            else
            {
                Console.WriteLine($"Injecting {inputFile} into slot {num}...");
                new LINKDATA().InjectFile(filteredArgs[1], num, inputFile);
            }

            Exit();
        }

        if (filteredArgs.Length == 3 && filteredArgs[0] == "injectfolder" && File.Exists(filteredArgs[1]) && Directory.Exists(filteredArgs[2]))
        {
            string binPath = filteredArgs[1];
            string folderPath = filteredArgs[2];

            string[] files = Directory.GetFiles(folderPath, "*.bin");
            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (int.TryParse(fileName, out int slot))
                {
                    if (encrypt)
                    {
                        Console.WriteLine($"Encrypting data for {file} with ID={slot}...");
                        byte[] data = File.ReadAllBytes(file);
                        data = EncryptData(data, (uint)slot);
                        string tempFile = Path.GetTempFileName();
                        File.WriteAllBytes(tempFile, data);
                        Console.WriteLine($"Injecting encrypted data into slot {slot}...");
                        new LINKDATA().InjectFile(binPath, slot, tempFile);
                        File.Delete(tempFile); // Clean up
                    }
                    else
                    {
                        Console.WriteLine($"Injecting {file} into slot {slot}...");
                        new LINKDATA().InjectFile(binPath, slot, file);
                    }
                }
                else
                {
                    Console.WriteLine($"Skipping file {file} (invalid name, not a number)");
                }
            }

            Exit();
        }

        if (filteredArgs.Length == 1 && Directory.Exists(filteredArgs[0]))
        {
            LINKDATA.Build(filteredArgs[0], Path.GetDirectoryName(filteredArgs[0]) + "\\" + Path.GetFileName(filteredArgs[0]) + ".nxarc");
            Exit();
        }

        if (filteredArgs.Length == 2 && Directory.Exists(filteredArgs[0]))
        {
            LINKDATA.Build(filteredArgs[0], filteredArgs[1]);
            Exit();
        }

        Exit(showFinished: true);
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
