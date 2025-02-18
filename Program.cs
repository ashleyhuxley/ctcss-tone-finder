using System.Diagnostics;

namespace ElectricFox.CtcssDetector;

public static class Program
{
    static readonly double[] CtcssTones = 
    {
        67.0, 71.9, 77.0, 82.5, 88.5, 94.8, 100.0, 107.2, 114.8, 123.0, 131.8,
        141.3, 151.4, 162.2, 173.8, 186.2, 203.5, 218.1, 233.6, 250.3
    };

    static readonly double[] PmrChannelFrequencies =
    {
        446.00625, 446.01875, 446.03125, 446.04375, 446.05625, 446.06875, 446.08125, 446.09375,
        446.10625, 446.11875, 446.13125, 446.14375, 446.15625, 446.16875, 446.18125, 446.19375
    };

    const int SAMPLE_RATE = 12000;  // Matches rtl_fm

    const string RTL_FM_PATH = "rtl_fm";

    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Incorrect number of arguments - specify either channel (P1 - P16) or frequency in MHz");
            return;
        }

        double frequency;

        if (args[0].StartsWith('P'))
        {
            var channel = int.Parse(args[0].Substring(1));
            if (channel < 1 || channel > 16)
            {
                Console.WriteLine("Invalid channel number - must be between 1 and 16");
                return;
            }

            frequency = PmrChannelFrequencies[channel - 1];
        }
        else
        {
            if (!double.TryParse(args[0], out frequency))
            {
                Console.WriteLine("Invalid frequency - must be a number in MHz");
                return;
            }
        }

        string sfrequency = $"{frequency}M";

        ProcessStartInfo psi = new()
        {
            FileName = RTL_FM_PATH,
            Arguments = $"-f {sfrequency} -M fm -s {SAMPLE_RATE} -g 40",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new() { StartInfo = psi };
        process.Start();

        using Stream stream = process.StandardOutput.BaseStream;
        using BinaryReader reader = new(stream);

        //using FileStream fileStream = new("D:\\Temp\\sample_ctcss_114-8hz.raw", FileMode.Open);
        //using BinaryReader reader = new(fileStream);

        byte[] buffer = new byte[SAMPLE_RATE * 2]; // 1-second audio buffer

        while (true)
        {
            int bytesRead = reader.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                continue;
            }

            short[] samples = new short[bytesRead / 2];
            Buffer.BlockCopy(buffer, 0, samples, 0, bytesRead);

            var powerLevels = GetToneValues(samples);
            DisplayHistogram(powerLevels);
        }
    }

    public static void DisplayHistogram(Dictionary<double, double> ctcssTones)
    {
        Console.Clear();

        const int maxBarLength = 50; // Maximum length of the histogram bar

        double maxPower = ctcssTones.Max(t => t.Value);

        foreach (var tone in ctcssTones)
        {
            double frequency = tone.Key;
            double power = tone.Value;
            int barLength = (int)(power / maxPower * maxBarLength);

            Console.WriteLine($"{frequency,8:F2} Hz: {new string('#', barLength)}");
        }
    }

    static Dictionary<double, double> GetToneValues(short[] samples)
    {
        var result = new Dictionary<double, double>();

        foreach (var tone in CtcssTones)
        {
            double power = Goertzel(samples, tone, SAMPLE_RATE);
            result.Add(tone, power);
        }

        return result;
    }

    static double? DetectCTCSS(short[] samples)
    {
        double maxPower = 0;
        double? bestMatch = null;

        foreach (double freq in CtcssTones)
        {
            double power = Goertzel(samples, freq, SAMPLE_RATE);
            if (power > maxPower)
            {
                maxPower = power;
                bestMatch = freq;
            }
        }

        return maxPower > 1e6 ? bestMatch : null; // Adjust threshold as needed
    }

    static double Goertzel(short[] samples, double frequency, int sampleRate)
    {
        int k = (int)(0.5 + ((samples.Length * frequency) / sampleRate));
        double omega = (2.0 * Math.PI * k) / samples.Length;
        double coeff = 2.0 * Math.Cos(omega);

        double s1 = 0, s2 = 0;
        foreach (short sample in samples)
        {
            double s0 = sample + coeff * s1 - s2;
            s2 = s1;
            s1 = s0;
        }

        return s1 * s1 + s2 * s2 - coeff * s1 * s2;
    }
}