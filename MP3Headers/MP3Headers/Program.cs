using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace MP3Headers
{
    class Program
    {
        static void Main(string[] args)
        {
            MP3 mp3EHText = new MP3("TestEH.mp3");
            MP3 mp3_id324 = new MP3("Sample ID3v2.4.mp3");
            MP3 mp3Test = new MP3("Test.mp3");
            MP3 mp3TestBad = new MP3("Test-Bad.mp3");
            MP3 mp3_cbr = new MP3("Sample CBR.mp3");
            MP3 mp3_vbr = new MP3("Sample VBR.mp3");
            MP3 lyrics3 = new MP3("Sample Lyrics3v2.mp3");

            Console.Write("MP3Headers Demo\n\n");

            Console.WriteLine(mp3EHText.ToString());
            Console.WriteLine();

            Console.WriteLine(mp3_id324.ToString());
            Console.WriteLine();

            Console.WriteLine(mp3Test.ToString());
            Console.WriteLine();

            Console.WriteLine(mp3TestBad.ToString());
            Console.WriteLine();

            Console.WriteLine(mp3_cbr.ToString());
            Console.WriteLine();

            Console.WriteLine(mp3_vbr.ToString());
            Console.WriteLine();

            Console.WriteLine(lyrics3.ToString());

            Console.Write("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}
