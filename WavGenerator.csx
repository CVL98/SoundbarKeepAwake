using System;
using System.IO;
using System.Text;

string filename = Path.Combine(Environment.CurrentDirectory, "beep.wav");

int durationSeconds = 1;
int sampleRate = 44100;
short amplitude = 0; // Silence
int numSamples = durationSeconds * sampleRate;

using (var fs = new FileStream(filename, FileMode.Create))
using (var writer = new BinaryWriter(fs))
{
    writer.Write(Encoding.ASCII.GetBytes("RIFF"));
    writer.Write(36 + numSamples * 2);
    writer.Write(Encoding.ASCII.GetBytes("WAVE"));

    writer.Write(Encoding.ASCII.GetBytes("fmt "));
    writer.Write(16);
    writer.Write((short)1);
    writer.Write((short)1);
    writer.Write(sampleRate);
    writer.Write(sampleRate * 2);
    writer.Write((short)2);
    writer.Write((short)16);

    writer.Write(Encoding.ASCII.GetBytes("data"));
    writer.Write(numSamples * 2);

    for (int i = 0; i < numSamples; i++)
    {
        writer.Write(amplitude);
    }
}

Console.WriteLine($"Silent WAV saved to {filename}");
