using System;
using System.Collections.Generic;
using System.Text;

namespace MP3Headers
{
    class Program
    {
        static void Main(string[] args)
        {
            MP3Header mp3 = new MP3Header();
            mp3.ReadMP3Information("Sample.mp3");

            Console.Write("MP3Headers Demo\n\n");

            Console.WriteLine("Reading file: " + mp3.strFileName);
            Console.WriteLine("Frequency: " + mp3.intFrequency.ToString());
            Console.WriteLine("Bitrate: " + mp3.intBitRate.ToString());

            Console.Write("\n\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}
