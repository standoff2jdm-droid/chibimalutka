using System;
using System.IO;

namespace SoundGen
{
    class Program
    {
        static void WriteWav(string path, byte[] samples, int sampleRate)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int dataSize = samples.Length;
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataSize);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16); // subchunk1 size
                bw.Write((short)1); // PCM
                bw.Write((short)1); // mono
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2); // byte rate (16-bit mono)
                bw.Write((short)2); // block align
                bw.Write((short)16); // bits per sample
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);
                bw.Write(samples);
            }
        }

        static byte[] GenerateTone(int sampleRate, double durationSec, Func<double, double> freqFunc, Func<double, double> envFunc)
        {
            int totalSamples = (int)(sampleRate * durationSec);
            byte[] data = new byte[totalSamples * 2];
            double phase = 0.0;

            for (int i = 0; i < totalSamples; i++)
            {
                double t = (double)i / sampleRate;
                double freq = freqFunc(t);
                phase += 2.0 * Math.PI * freq / sampleRate;

                // Soft sine wave with a little 2nd harmonic warmth
                double sample = Math.Sin(phase) * 0.75 + Math.Sin(phase * 2.0) * 0.25;
                double env = envFunc(t);
                sample *= env;

                short s16 = (short)(Math.Max(-1.0, Math.Min(1.0, sample)) * 26000);
                data[i * 2] = (byte)(s16 & 0xFF);
                data[i * 2 + 1] = (byte)((s16 >> 8) & 0xFF);
            }
            return data;
        }

        static void Main(string[] args)
        {
            string outDir = args.Length > 0 ? args[0] : "assets/sounds";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            int sr = 44100;

            // 1. Cute Nya chirp (rising cute glissando 650Hz -> 1150Hz -> 900Hz)
            var nya = GenerateTone(sr, 0.22, t =>
            {
                if (t < 0.12) return 600 + (t / 0.12) * 550;
                return 1150 - ((t - 0.12) / 0.10) * 250;
            }, t =>
            {
                double attack = Math.Min(1.0, t / 0.02);
                double decay = Math.Max(0.0, 1.0 - (t / 0.22));
                return attack * decay;
            });
            WriteWav(Path.Combine(outDir, "nya.wav"), nya, sr);

            // 2. High squeak / Kyaa (playful high chirp with vibrato 1200Hz - 1600Hz)
            var squeak = GenerateTone(sr, 0.18, t =>
            {
                double baseF = 1100 + (t / 0.18) * 500;
                return baseF + Math.Sin(t * 80.0) * 40.0;
            }, t =>
            {
                double attack = Math.Min(1.0, t / 0.015);
                double decay = Math.Max(0.0, 1.0 - Math.Pow(t / 0.18, 1.5));
                return attack * decay * 0.85;
            });
            WriteWav(Path.Combine(outDir, "squeak.wav"), squeak, sr);

            // 3. Gentle purr / soft hum (harmonic warmth with flutter)
            var purr = GenerateTone(sr, 0.35, t =>
            {
                return 480 + Math.Sin(t * 45.0) * 60.0;
            }, t =>
            {
                double attack = Math.Min(1.0, t / 0.05);
                double decay = Math.Max(0.0, 1.0 - (t / 0.35));
                return attack * decay * 0.7;
            });
            WriteWav(Path.Combine(outDir, "purr.wav"), purr, sr);

            // 4. Petting chime (two sweet harmony bells)
            var pet = GenerateTone(sr, 0.25, t =>
            {
                return 880 + (t / 0.25) * 440; // A5 -> A6
            }, t =>
            {
                double attack = Math.Min(1.0, t / 0.01);
                double decay = Math.Max(0.0, Math.Exp(-t * 12.0));
                return attack * decay;
            });
            WriteWav(Path.Combine(outDir, "pet1.wav"), pet, sr);

            // 5. Click bounce squeak
            var click = GenerateTone(sr, 0.14, t =>
            {
                return 750 + Math.Sin(t * 60.0) * 150.0;
            }, t =>
            {
                return Math.Min(1.0, t / 0.01) * Math.Max(0.0, 1.0 - t / 0.14);
            });
            WriteWav(Path.Combine(outDir, "click.wav"), click, sr);

            Console.WriteLine("Cute sounds generated successfully in: " + outDir);
        }
    }
}
