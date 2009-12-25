using System;
using System.Collections.Generic;
using System.Text;

namespace MP3Headers
{
    class Program
    {
        static void Main(string[] args)
        {
            MP3Header mp3_id324 = new MP3Header();
            mp3_id324.ReadMP3Information("Sample ID3v2.4.mp3");

            MP3Header mp3_cbr = new MP3Header();
            mp3_cbr.ReadMP3Information("Sample CBR.mp3");

            MP3Header mp3_vbr = new MP3Header();
            mp3_vbr.ReadMP3Information("Sample VBR.mp3");

            Console.Write("MP3Headers Demo\n\n");

            Console.WriteLine(mp3_cbr.ToString());

            Console.WriteLine();

            Console.WriteLine(mp3_vbr.ToString());

            Console.Write("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}
