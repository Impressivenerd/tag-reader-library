using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace digitalBrink.MP3Reader.Tags.Lyrics3
{
    public class Lyrics3Tag : Tag
    {
        public bool Valid;

        public Lyrics3Tag(FileStream fs, bool ID3v1Exists)
        {
            Valid = false;
            Exists = false;
            MajorVersion = 0;

            int offsetModifier = 0;

            //ID3v1 does not have to exist for Lyrics3 to exist
            if (ID3v1Exists)
            {
                offsetModifier = 137;
            }
            else
            {
                offsetModifier = 9;
            }

            fs.Position = fs.Length - offsetModifier;

            byte[] bytLyricsEnd = new byte[9];
            fs.Read(bytLyricsEnd, 0, 9);

            string Version = Encoding.ASCII.GetString(bytLyricsEnd);

            switch (Version)
            {
                case "LYRICSEND": //Lyrics3v1
                    long lngPos = fs.Length - (5100 + offsetModifier);
                    fs.Position = lngPos;
                    while (!Valid && fs.Position <= (fs.Length - offsetModifier))
                    {
                        byte[] beginTagV1 = new byte[11];
                        fs.Read(beginTagV1, 0, 11);
                        if (Encoding.ASCII.GetString(beginTagV1) == "LYRICSBEGIN")
                        {
                            Valid = true;
                            Position = fs.Position - 11;
                        }
                        else
                        {
                            lngPos++;
                            fs.Position = lngPos;
                        }
                    }

                    MajorVersion = 1;
                    Exists = true;
                    break;
                case "LYRICS200": //Lyrics3v2

                    //Find Size of Tag (Excluding 6 bytes for size and 9 bytes for end string)
                    byte[] size = new byte[6];
                    fs.Position = fs.Length - (offsetModifier + 6); //128 [ID3v1] + 9 [LYRICS200] + 6 [SIZE]
                    fs.Read(size, 0, 6);

                    string strSize = Encoding.ASCII.GetString(size);
                    _ReportedTagSize = uint.Parse(strSize);
                    TagSize = _ReportedTagSize + 15; //6 Bytes for Size; 9 Bytes for LYRICS200

                    //Find Beginning of Tag
                    fs.Position = fs.Length - (_ReportedTagSize + (offsetModifier + 6));
                    byte[] beginning = new byte[11];
                    fs.Read(beginning, 0, 11);

                    if (Encoding.ASCII.GetString(beginning) == "LYRICSBEGIN")
                    {
                        //OK
                        Valid = true;
                        Position = fs.Position - 11;
                    }

                    MajorVersion = 2;
                    Exists = true;

                    break;
                default:
                    break;
            }
        }

        public override byte[] ToByte()
        {
            //Setup Encoding Here For Write Method
            return new byte[0];
        }
    }
}
