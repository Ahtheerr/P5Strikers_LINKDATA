using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

class Program
{
    const int ExtractSize = 0x71080;

    [STAThread]
    static void Main()
    {
        // 1. Selecionar o executável
        string exePath = AbrirDialogo("Select game.exe", "Executable (*.exe)|*.exe");
        if (exePath == null) return;

        byte[] exeBytes = File.ReadAllBytes(exePath);

        // 2. Encontrar LINKDATA.BIN
        int linkdataIndex = EncontrarPadrao(exeBytes, Encoding.ASCII.GetBytes("LINKDATA.BIN"));
        if (linkdataIndex == -1)
        {
            Console.WriteLine("Not found.");
            return;
        }

        // 3. Encontrar o PARAM após o LINKDATA.BIN
        int paramIndex = EncontrarPadrao(exeBytes, Encoding.ASCII.GetBytes("PARAM"), linkdataIndex);
        if (paramIndex == -1)
        {
            Console.WriteLine("Not found.");
            return;
        }

        // 4. Calcular início da extração/injeção
        int startIndex = (paramIndex + 0x10) & ~0xF;

        // 5. Selecionar .IDX ou cancelar para extrair
        string idxPath = AbrirDialogo("Select the .IDX file (or cancel to extract)", "IDX (*.idx)|*.idx|Todos (*.*)|*.*", true);

        if (idxPath != null)
        {
            // REINJEÇÃO
            byte[] idxData = File.ReadAllBytes(idxPath);
            if (idxData.Length > ExtractSize)
            {
                Console.WriteLine("Error: The .IDX is bigger than 0x71080 bytes.");
                return;
            }

            using FileStream fs = new FileStream(exePath, FileMode.Open, FileAccess.Write);
            fs.Seek(startIndex, SeekOrigin.Begin);
            fs.Write(idxData, 0, idxData.Length);

            // Preencher com 00 se o arquivo for menor que 0x71080
            if (idxData.Length < ExtractSize)
            {
                byte[] zeros = new byte[ExtractSize - idxData.Length];
                fs.Write(zeros, 0, zeros.Length);
            }

            Console.WriteLine("Success.");
        }
        else
        {
            // EXTRAÇÃO
            if (startIndex + ExtractSize > exeBytes.Length)
            {
                Console.WriteLine("Error: Insufficient data.");
                return;
            }

            byte[] extracted = new byte[ExtractSize];
            Array.Copy(exeBytes, startIndex, extracted, 0, ExtractSize);

            string outputPath = Path.Combine(Path.GetDirectoryName(exePath), "LINKDATA.IDX");
            File.WriteAllBytes(outputPath, extracted);

            Console.WriteLine("Extracted.");
            Console.WriteLine("File saved on: " + outputPath);
        }
    }

    static string AbrirDialogo(string titulo, string filtro, bool podeCancelar = false)
    {
        using OpenFileDialog dialog = new OpenFileDialog
        {
            Title = titulo,
            Filter = filtro
        };
        var result = dialog.ShowDialog();
        return (result == DialogResult.OK) ? dialog.FileName : (podeCancelar ? null : throw new Exception("Operation canceled"));
    }

    static int EncontrarPadrao(byte[] data, byte[] padrao, int inicio = 0)
    {
        for (int i = inicio; i <= data.Length - padrao.Length; i++)
        {
            bool achou = true;
            for (int j = 0; j < padrao.Length; j++)
            {
                if (data[i + j] != padrao[j])
                {
                    achou = false;
                    break;
                }
            }
            if (achou) return i;
        }
        return -1;
    }
}
