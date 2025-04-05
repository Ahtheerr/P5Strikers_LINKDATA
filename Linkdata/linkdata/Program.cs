using System;
using System.IO;
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

    private static void Main(string[] args)
    {
        Init();

        if (args.Length == 1 && File.Exists(args[0]))
        {
            new LINKDATA().Load(args[0]);
            Exit();
        }

        if (args.Length == 4 && args[0] == "inject" && File.Exists(args[1]) && File.Exists(args[3]))
        {
            int num = int.Parse(args[2]);
            Console.WriteLine($"Injecting {args[1]} into slot {num}...");
            new LINKDATA().InjectFile(args[1], num, args[3]);
            Exit();
        }

        // NOVO: injeta múltiplos arquivos de uma pasta automaticamente
        if (args.Length == 3 && args[0] == "injectfolder" && File.Exists(args[1]) && Directory.Exists(args[2]))
        {
            string binPath = args[1];
            string folderPath = args[2];

            string[] files = Directory.GetFiles(folderPath, "*.bin");
            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (int.TryParse(fileName, out int slot))
                {
                    Console.WriteLine($"Injecting {file} into slot {slot}...");
                    new LINKDATA().InjectFile(binPath, slot, file);
                }
                else
                {
                    Console.WriteLine($"Skipping file {file} (invalid name, not a number)");
                }
            }

            Exit();
        }

        if (args.Length == 1 && Directory.Exists(args[0]))
        {
            LINKDATA.Build(args[0], Path.GetDirectoryName(args[0]) + "\\" + Path.GetFileName(args[0]) + ".nxarc");
            Exit();
        }

        if (args.Length == 2 && Directory.Exists(args[0]))
        {
            LINKDATA.Build(args[0], args[1]);
            Exit();
        }

        Exit(showFinished: true);
    }
}
