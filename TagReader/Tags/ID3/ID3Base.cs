using System;
using System.Collections.Generic;
using System.Text;

namespace digitalBrink.TagReader.Tags.ID3
{
    public abstract class ID3Base : Tag
    {
        public string Title;
        public string Artist;
        public string Album;
        public string Year;
        public string Comment;
        public string Genre;
        public int Track;
        public int GenreID;

        //Faster Implementation Only - 4 Bytes
        public static int syncsafe(byte byt1, byte byt2, byte byt3, byte byt4)
        {
            return (int)((byt1 << 21) | (byt2 << 14) | (byt3 << 7) | byt4);
        }

        //Faster Implementation Only - 5 Bytes
        public static uint syncsafe(byte byt1, byte byt2, byte byt3, byte byt4, byte byt5)
        {
            return (uint)((byt1 << 28) | (byt2 << 21) | (byt3 << 14) | (byt4 << 7) | byt5);
        }

        //Generic Implementation - Any number of Params
        public static ulong syncsafe(params byte[] byt)
        {
            int bitShift = (byt.Length - 1) * 7; //Decrement by 7 (for syncsafe)
            ulong Total = 0;

            for (int i = 0; i < byt.Length; ++i)
            {
                Total += (ulong)(byt[i] << bitShift);
                bitShift -= 7;
            }

            return Total;
        }

        //Faster Implementation Only - 4 Bytes 
        public static uint expand(byte byt1, byte byt2, byte byt3, byte byt4)
        {
            return (uint)((byt1 << 24) | (byt2 << 16) | (byt3 << 8) | byt4);
        }

        //Generic Implementation - Any number of Params
        public static ulong expand(params byte[] byt)
        {
            int bitShift = (byt.Length - 1) * 8;
            ulong Total = 0;

            for (int i = 0; i < byt.Length; ++i)
            {
                Total += (ulong)(byt[i] << bitShift);
                bitShift -= 8;
            }

            return Total;
        }

        public static byte[] UnSync(byte[] bytArray)
        {
            List<byte> newArray = new List<byte>();

            for (int i = 0; i < bytArray.Length; ++i)
            {
                if (bytArray.Length - i >= 3)
                {
                    if (bytArray[i] == 0xFF && bytArray[i + 1] == 0x00 && bytArray[i + 2] == 0x00)
                    {
                        //Only Add 0xFF and 0x00 back to the array
                        newArray.Add(bytArray[i]);
                        newArray.Add(bytArray[i + 1]);
                        i = i + 2; //++i will add one more
                    }
                    else
                    {
                        newArray.Add(bytArray[i]);
                    }
                }
                else
                {
                    newArray.Add(bytArray[i]);
                }
            }

            byte[] t = newArray.ToArray();
            return t;
        }

        protected string getGenre(int genreID)
        {
            switch (genreID)
            {
                case 0:
                    return "Blues";
                case 1:
                    return "Classic Rock";
                case 2:
                    return "Country";
                case 3:
                    return "Dance";
                case 4:
                    return "Disco";
                case 5:
                    return "Funk";
                case 6:
                    return "Grunge";
                case 7:
                    return "Hip-Hop";
                case 8:
                    return "Jazz";
                case 9:
                    return "Metal";
                case 10:
                    return "New Age";
                case 11:
                    return "Oldies";
                case 12:
                    return "Other";
                case 13:
                    return "Pop";
                case 14:
                    return "R&B";
                case 15:
                    return "Rap";
                case 16:
                    return "Reggae";
                case 17:
                    return "Rock";
                case 18:
                    return "Techno";
                case 19:
                    return "Industrial";
                case 20:
                    return "Alternative";
                case 21:
                    return "Ska";
                case 22:
                    return "Death Metal";
                case 23:
                    return "Pranks";
                case 24:
                    return "Soundtrack";
                case 25:
                    return "Euro-Techno";
                case 26:
                    return "Ambient";
                case 27:
                    return "Trip-Hop";
                case 28:
                    return "Vocal";
                case 29:
                    return "Jazz+Funk";
                case 30:
                    return "Fusion";
                case 31:
                    return "Trance";
                case 32:
                    return "Classical";
                case 33:
                    return "Instrumental";
                case 34:
                    return "Acid";
                case 35:
                    return "House";
                case 36:
                    return "Game";
                case 37:
                    return "Sound Clip";
                case 38:
                    return "Gospel";
                case 39:
                    return "Noise";
                case 40:
                    return "AlternRock";
                case 41:
                    return "Bass";
                case 42:
                    return "Soul";
                case 43:
                    return "Punk";
                case 44:
                    return "Space";
                case 45:
                    return "Meditative";
                case 46:
                    return "Instrumental Pop";
                case 47:
                    return "Instrumental Rock";
                case 48:
                    return "Ethnic";
                case 49:
                    return "Gothic";
                case 50:
                    return "Darkwave";
                case 51:
                    return "Techno-Industrial";
                case 52:
                    return "Electronic";
                case 53:
                    return "Pop-Folk";
                case 54:
                    return "Eurodance";
                case 55:
                    return "Dream";
                case 56:
                    return "Southern Rock";
                case 57:
                    return "Comedy";
                case 58:
                    return "Cult";
                case 59:
                    return "Gangsta";
                case 60:
                    return "Top 40";
                case 61:
                    return "Christian Rap";
                case 62:
                    return "Pop/Funk";
                case 63:
                    return "Jungle";
                case 64:
                    return "Native American";
                case 65:
                    return "Cabaret";
                case 66:
                    return "New Wave";
                case 67:
                    return "Psychadelic";
                case 68:
                    return "Rave";
                case 69:
                    return "Showtunes";
                case 70:
                    return "Trailer";
                case 71:
                    return "Lo-Fi";
                case 72:
                    return "Tribal";
                case 73:
                    return "Acid Punk";
                case 74:
                    return "Acid Jazz";
                case 75:
                    return "Polka";
                case 76:
                    return "Retro";
                case 77:
                    return "Musical";
                case 78:
                    return "Rock & Roll";
                case 79:
                    return "Hard Rock";

                //Begin WinAmp expanded codes
                case 80:
                    return "Folk";
                case 81:
                    return "Folk-Rock";
                case 82:
                    return "National Folk";
                case 83:
                    return "Swing";
                case 84:
                    return "Fast Fusion";
                case 85:
                    return "Bebob";
                case 86:
                    return "Latin";
                case 87:
                    return "Revival";
                case 88:
                    return "Celtic";
                case 89:
                    return "Bluegrass";
                case 90:
                    return "Avantgarde";
                case 91:
                    return "Gothic Rock";
                case 92:
                    return "Progressive Rock";
                case 93:
                    return "Psychedelic Rock";
                case 94:
                    return "Symphonic Rock";
                case 95:
                    return "Slow Rock";
                case 96:
                    return "Big Band";
                case 97:
                    return "Chorus";
                case 98:
                    return "Easy Listening";
                case 99:
                    return "Acoustic";
                case 100:
                    return "Humour";
                case 101:
                    return "Speech";
                case 102:
                    return "Chanson";
                case 103:
                    return "Opera";
                case 104:
                    return "Chamber Music";
                case 105:
                    return "Sonata";
                case 106:
                    return "Symphony";
                case 107:
                    return "Booty Brass";
                case 108:
                    return "Primus";
                case 109:
                    return "Porn Groove";
                case 110:
                    return "Satire";
                case 111:
                    return "Slow Jam";
                case 112:
                    return "Club";
                case 113:
                    return "Tango";
                case 114:
                    return "Samba";
                case 115:
                    return "Folklore";
                case 116:
                    return "Ballad";
                case 117:
                    return "Poweer Ballad";
                case 118:
                    return "Rhytmic Soul";
                case 119:
                    return "Freestyle";
                case 120:
                    return "Duet";
                case 121:
                    return "Punk Rock";
                case 122:
                    return "Drum Solo";
                case 123:
                    return "A Capela";
                case 124:
                    return "Euro-House";
                case 125:
                    return "Dance Hall";

                default:
                    return "Unknown";
            }
        }
    }
}
