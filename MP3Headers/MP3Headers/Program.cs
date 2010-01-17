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
            MP3Header mp3EHText = new MP3Header("TestEH.mp3");
            MP3Header mp3_id324 = new MP3Header("Sample ID3v2.4.mp3");
            MP3Header mp3Test = new MP3Header("Test.mp3");
            Console.WriteLine(mp3Test.ToString());
            MP3Header mp3_cbr = new MP3Header("Sample CBR.mp3");
            MP3Header mp3_vbr = new MP3Header("Sample VBR.mp3");

            Console.Write("MP3Headers Demo\n\n");

            Console.WriteLine(mp3_id324.ToString());
            Console.WriteLine();

            Console.WriteLine(mp3_cbr.ToString());
            Console.WriteLine();

            Console.WriteLine(mp3_vbr.ToString());

            Console.Write("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}
