using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace digitalBrink.MP3Reader.Tags.ID3.V1
{
    public class ID3v1Tag : ID3Base
    {
        public ID3v1Tag()
        {
            MajorVersion = 0;
            MinorVersion = 0; //Never Changes for ID3v1

            Title = "";
            Artist = "";
            Album = "";
            Year = "";
            Comment = "";
            Genre = "";

            Track = 0;
            GenreID = 0;

            Exists = false;

            TagSize = 128;
        }

        public ID3v1Tag(FileStream fs)
        {
            /************
             * Defaults
             ************/
            Title = "";
            Artist = "";
            Album = "";
            Year = "";
            Comment = "";
            Genre = "";

            Track = 0;
            GenreID = 0;
            Exists = false;
            TagSize = 128;

            //Store FileStreams current position
            long fsOriginalPosition = fs.Position;

            //Start Parsing for ID3v1 Tag
            byte[] bytHeaderTAG = new byte[128];
            fs.Position = fs.Length - 128;
            fs.Read(bytHeaderTAG, 0, 128);

            //ID3v1.X Should begin with "TAG"
            if ((char)bytHeaderTAG[0] == 'T' && (char)bytHeaderTAG[1] == 'A' && (char)bytHeaderTAG[2] == 'G')
            {
                Position = fs.Position - 128;

                //Title
                for (int i = 3; i <= 32; ++i)
                {
                    //Ignore All Data After '\0'
                    if (bytHeaderTAG[i] == 0x00)
                    {
                        break;
                    }

                    Title += (char)bytHeaderTAG[i];
                }
                Title = Title.Trim('\0').Trim();   //Remove any Nulls and Spaces

                //Artist
                for (int i = 33; i <= 62; ++i)
                {
                    //Ignore All Data After '\0'
                    if (bytHeaderTAG[i] == 0x00)
                    {
                        break;
                    }
                    Artist += (char)bytHeaderTAG[i];
                }
                Artist = Artist.Trim('\0').Trim(); //Remove any Nulls and Spaces

                //Album
                for (int i = 63; i <= 92; ++i)
                {
                    //Ignore All Data After '\0'
                    if (bytHeaderTAG[i] == 0x00)
                    {
                        break;
                    }
                    Album += (char)bytHeaderTAG[i];
                }
                Album = Album.Trim('\0').Trim();   //Remove any Nulls and Spaces

                //Year
                for (int i = 93; i <= 96; ++i)
                {
                    Year += (char)bytHeaderTAG[i];
                }
                Year.Trim('\0').Trim();

                //Comment
                for (int i = 97; i <= 125; ++i)
                {
                    //Ignore All Data After '\0'
                    if (bytHeaderTAG[i] == 0x00)
                    {
                        break;
                    }
                    Comment += (char)bytHeaderTAG[i];
                }

                //Track or End of Comment
                if ((bytHeaderTAG[125] == 0x00 || bytHeaderTAG[125] == 0x20) && bytHeaderTAG[126] != 0x00)
                {
                    //ID3v1.1
                    Track = (int)bytHeaderTAG[126];
                    MajorVersion = 1;
                }
                else
                {
                    //ID3v1.0
                    Comment += (char)bytHeaderTAG[126];
                }
                Comment = Comment.Trim('\0').Trim();   //Remove any Nulls and Spaces

                //GenreID
                GenreID = (int)bytHeaderTAG[127];

                //Genre
                Genre = getGenre(GenreID);

                //Exists
                Exists = true;
            }

            fs.Position = fsOriginalPosition;
        }

        public override byte[] ToByte()
        {
            //Setup Encoding Here For Write Method
            byte[] id3tag = new byte[128];
            int ArrayOffset = 0;
            int ItemLen = 0; //Used to keep track of the current items length

            ASCIIEncoding ascii = new ASCIIEncoding();
            ascii.GetBytes("TAG").CopyTo(id3tag, 0);
            ArrayOffset += 3;

            //Title: Max Length = 30
            ItemLen = 0;
            ItemLen = (Title.Length <= 30) ? Title.Length : 30;
            ascii.GetBytes(Title.ToCharArray(0, ItemLen)).CopyTo(id3tag, ArrayOffset);
            ArrayOffset += ItemLen;

            //[PADDING] ArrayOffset should be at 33 by this point
            if (ArrayOffset < 33)
            {
                for (; ArrayOffset < 33; ++ArrayOffset)
                {
                    id3tag[ArrayOffset] = 0x00;
                }
            }

            //Artist: Max Length = 30
            ItemLen = 0;
            ItemLen = (Artist.Length <= 30) ? Artist.Length : 30;
            ascii.GetBytes(Artist.ToCharArray(0, ItemLen)).CopyTo(id3tag, ArrayOffset);
            ArrayOffset += ItemLen;

            if (ArrayOffset < 63)
            {
                for (; ArrayOffset < 63; ++ArrayOffset)
                {
                    id3tag[ArrayOffset] = 0x00;
                }
            }

            //Album: Max Length = 30
            ItemLen = 0;
            ItemLen = (Album.Length <= 30) ? Album.Length : 30;
            ascii.GetBytes(Album.ToCharArray(0, ItemLen)).CopyTo(id3tag, ArrayOffset);
            ArrayOffset += ItemLen;

            if (ArrayOffset < 93)
            {
                for (; ArrayOffset < 93; ++ArrayOffset)
                {
                    id3tag[ArrayOffset] = 0x00;
                }
            }

            //Year: Max Length = 4
            ItemLen = 0;
            ItemLen = (Year.Length <= 4) ? Year.Length : 4;
            ascii.GetBytes(Year.ToCharArray(0, ItemLen)).CopyTo(id3tag, ArrayOffset);
            ArrayOffset += ItemLen;

            if (ArrayOffset < 97)
            {
                for (; ArrayOffset < 97; ++ArrayOffset)
                {
                    id3tag[ArrayOffset] = 0x00;
                }
            }

            //Comment: Max Length = 30 (29 if v1.1)
            ItemLen = 0;
            ItemLen = (Comment.Length <= 30) ? Comment.Length : 30;
            ascii.GetBytes(Comment.ToCharArray(0, ItemLen)).CopyTo(id3tag, ArrayOffset);
            ArrayOffset += ItemLen;

            if (ArrayOffset < 127)
            {
                for (; ArrayOffset < 127; ++ArrayOffset)
                {
                    id3tag[ArrayOffset] = 0x00;
                }
            }

            //Track: Max Length = 1
            if (Track != 0)
            {
                id3tag[ArrayOffset - 1] = (byte)Track;
            }
            else
            {
                id3tag[ArrayOffset - 1] = 0x00;
            }

            //Genre: Max Length = 1
            id3tag[ArrayOffset] = (byte)GenreID;

            return id3tag;
        }
    }
}
