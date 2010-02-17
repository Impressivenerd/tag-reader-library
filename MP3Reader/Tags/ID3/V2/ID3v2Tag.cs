using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace digitalBrink.MP3Reader.Tags.ID3.V2
{
    public class ID3v2Tag : ID3Base
    {
        public bool AllFramesUnsynced;
        public bool Compression; //ID3v2.2 ONLY

        /*****
         * Extended Header Options
         *****/
        public ID3v2ExtendedHeader ExtendedHeader;

        public bool Experimental;
        public bool FooterExists;
        public bool PaddingExists;

        public Dictionary<string, List<ID3v2Frame>> Frames;

        public ID3v2Tag()
        {
            setDefaultValues();
        }

        private void setDefaultValues()
        {
            Exists = false;
            TagSize = 0;

            MajorVersion = 0;
            MinorVersion = 0;

            AllFramesUnsynced = false;
            Compression = false;

            ExtendedHeader = new ID3v2ExtendedHeader();

            Experimental = false;
            FooterExists = false;
            PaddingExists = false;

            Frames = new Dictionary<string, List<ID3v2Frame>>();
        }

        private void ProcessFlags(byte bytFlags)
        {
            switch (MajorVersion)
            {
                case 2:
                    //ID3v2.2 Flag defined: ab000000
                    AllFramesUnsynced = ((bytFlags & 0x80) == 0x80);        //a - Unsynchronisation
                    Compression = ((bytFlags & 0x40) == 0x40);              //b - Compression (if set, skip the entire tag)
                    break;
                case 3:
                    //ID3v2.3 Flag defined: abc00000
                    AllFramesUnsynced = ((bytFlags & 0x80) == 0x80);        //a - Unsynchronisation
                    ExtendedHeader.Exists = ((bytFlags & 0x40) == 0x40);    //b - Extended Header
                    Experimental = ((bytFlags & 0x20) == 0x20);             //c - Experimental Indicator
                    break;
                case 4:
                    //ID3v2.4 Flag defined: abcd0000
                    AllFramesUnsynced = ((bytFlags & 0x80) == 0x80);        //a - Unsynchronisation
                    ExtendedHeader.Exists = ((bytFlags & 0x40) == 0x40);    //b - Extended Header
                    Experimental = ((bytFlags & 0x20) == 0x20);             //c - Experimental Indicator
                    FooterExists = ((bytFlags & 0x10) == 0x10);             //d - Footer Present
                    break;
                default:
                    //Unknown - All Flags False
                    break;
            }
        }

        public ID3v2Tag(FileStream fs) : this(fs, 0) { }

        public ID3v2Tag(FileStream fs, long lngStartOffset)
        {
            //Store FileStreams current position
            long fsOriginalPosition = fs.Position;

            setDefaultValues();

            byte[] bytHeaderID3 = new byte[10];
            fs.Position = lngStartOffset;
            fs.Read(bytHeaderID3, 0, 10);

            //ID3v2.X should start with "ID3"
            if ((char)bytHeaderID3[0] == 'I' && (char)bytHeaderID3[1] == 'D' && (char)bytHeaderID3[2] == '3')
            {
                Position = lngStartOffset;

                //Major and Minor Versions should NOT be 0xFF
                MajorVersion = (bytHeaderID3[3] != 0xFF) ? (int)bytHeaderID3[3] : 0;
                MinorVersion = (bytHeaderID3[4] != 0xFF) ? (int)bytHeaderID3[4] : 0;

                ProcessFlags(bytHeaderID3[5]);

                /*
                 * ID3v2 Uses synchsafe integers, which skip the largest bit (farthest left) of each byte.
                 * 
                 * See http://www.id3.org/id3v2.4.0-structure and http://id3lib.sourceforge.net/id3/id3v2.3.0.html#sec3.1
                 */
                //Console.WriteLine("ID3v2.{0:D}.{1:D} Exists", tag.MajorVersion, tag.MinorVersion);


                //Max size for tag size = 2^28 = 268435456 bytes = 256MB; int.MaxValue = 2147483647 bytes = 2048MB
                TagSize = (uint)ID3Base.syncsafe(bytHeaderID3[6], bytHeaderID3[7], bytHeaderID3[8], bytHeaderID3[9]); //(int)((bytHeaderID3[6] << 21) | (bytHeaderID3[7] << 14) | (bytHeaderID3[8] << 7) | bytHeaderID3[9]);

                //Console.WriteLine("Len (Bytes): {0:D}", (ulong)( (bytHeaderID3[6] << 21) | (bytHeaderID3[7] << 14) | (bytHeaderID3[8] << 7) | bytHeaderID3[9] ));
                //Console.WriteLine("NUMBER: {0:D}", (ulong)( (0x00<<21) | (0x00<<14) | (0x02<<7) | (0x01) ));
                //Console.WriteLine("OUTPUT: {0:X}, {1:X}, {2:X}, {3:X}", bytHeaderID3[6], bytHeaderID3[7], bytHeaderID3[8], bytHeaderID3[9]);

                Exists = true;

                //Experimental - Extended Header Implementation
                ExtendedHeader.setID3Version(MajorVersion);
                if (ExtendedHeader.Exists && MajorVersion > 2)
                {
                    byte[] EH_Size = new byte[4];
                    fs.Read(EH_Size, 0, 4);

                    if (MajorVersion == 3)
                    {
                        //Extended Header size, excluding itself; only 6 or 10 bytes (6 if no CRC Data, 10 if CRC Data [4 bytes for CRC])
                        ExtendedHeader.size = (int)ID3Base.expand(EH_Size[0], EH_Size[1], EH_Size[2], EH_Size[3]);
                        byte[] headerAndData = new byte[4 + ExtendedHeader.size];
                        fs.Position = lngStartOffset + 10;
                        fs.Read(headerAndData, 0, headerAndData.Length);
                        ExtendedHeader.setData(headerAndData);
                    }
                    else if (MajorVersion == 4)
                    {
                        //Whole Extended Header
                        ExtendedHeader.size = ID3Base.syncsafe(EH_Size[0], EH_Size[1], EH_Size[2], EH_Size[3]);
                        byte[] headerAndData = new byte[ExtendedHeader.size];
                        fs.Position = lngStartOffset + 10;
                        fs.Read(headerAndData, 0, headerAndData.Length);
                        ExtendedHeader.setData(headerAndData);
                    }
                }

                //ID3v2.2.X uses 3 letter identifiers versus the 4 letter identifiers in 2.3.X and 2.4.X
                if (!Compression)
                {
                    Console.WriteLine("ID3v2.{0:D} Detected...", MajorVersion);
                    int totalFrameSize = 0;

                    int AdditionalBytes = 10;
                    if (FooterExists)
                    {
                        AdditionalBytes = 20;
                    }
                    while (!PaddingExists && fs.Position < TagSize + AdditionalBytes) //(tag.TagSize + 10) == End Position (+10 for Original Header, +20 For Original Header and Footer [if present])
                    {
                        if (MajorVersion == 2)
                        {
                            byte[] TempHeader = new byte[6];
                            fs.Read(TempHeader, 0, 6);

                            if (TempHeader[0] == 0x00 && TempHeader[1] == 0x00 && TempHeader[2] == 0x00)
                            {
                                PaddingExists = true;
                            }
                            else
                            {
                                Console.WriteLine("HEADER[{0}{1}{2}]", (char)TempHeader[0], (char)TempHeader[1], (char)TempHeader[2]);

                                string FrameHeaderName = ((char)TempHeader[0]).ToString() +
                                    ((char)TempHeader[1]).ToString() +
                                    ((char)TempHeader[2]).ToString();

                                if (Frames.ContainsKey(FrameHeaderName))
                                {
                                    Frames[FrameHeaderName].Add(new ID3v2Frame(FrameHeaderName, MajorVersion));
                                }
                                else
                                {
                                    List<ID3v2Frame> FrameList = new List<ID3v2Frame>();
                                    FrameList.Add(new ID3v2Frame(FrameHeaderName, MajorVersion));
                                    Frames.Add(FrameHeaderName, FrameList);
                                }

                                int currentFrameIndex = Frames[FrameHeaderName].Count - 1;
                                ID3v2Frame currentFrame = Frames[FrameHeaderName][currentFrameIndex];

                                currentFrame.getFrameFlags((byte)0x00, (byte)0x00, AllFramesUnsynced);

                                //Frame Size
                                currentFrame.FrameSize = (int)ID3Base.expand(TempHeader[3], TempHeader[4], TempHeader[5]);//(int)((TempHeader[3] << 16) | (TempHeader[4] << 8) | (TempHeader[5]));

                                totalFrameSize += currentFrame.FrameSize + 6; //+6 for Frame Header

                                //Set FrameData
                                byte[] tempData = new byte[currentFrame.FrameSize];
                                fs.Read(tempData, 0, currentFrame.FrameSize);
                                currentFrame.getFrameData(tempData);
                            }
                        }
                        else
                        {

                            byte[] TempHeader = new byte[10];
                            fs.Read(TempHeader, 0, 10);

                            //All Tags must be 4 Characters (characters capital A-Z and 0-9; not null)
                            if (!FooterExists && (TempHeader[0] == 0x00 || TempHeader[1] == 0x00 || TempHeader[2] == 0x00 || TempHeader[3] == 0x00))
                            {
                                //Nothing Here but Padding; Skip to the end of the tag
                                //Footer and Padding are Mutually Exclusive
                                PaddingExists = true;
                            }
                            else
                            {
                                Console.WriteLine("HEADER[{0}{1}{2}{3}]", (char)TempHeader[0], (char)TempHeader[1], (char)TempHeader[2], (char)TempHeader[3]);

                                //Get the Frame Name
                                string FrameHeaderName = ((char)TempHeader[0]).ToString() +
                                    ((char)TempHeader[1]).ToString() +
                                    ((char)TempHeader[2]).ToString() +
                                    ((char)TempHeader[3]).ToString();

                                if (Frames.ContainsKey(FrameHeaderName))
                                {
                                    Frames[FrameHeaderName].Add(new ID3v2Frame(FrameHeaderName, MajorVersion));
                                }
                                else
                                {
                                    List<ID3v2Frame> FrameList = new List<ID3v2Frame>();
                                    FrameList.Add(new ID3v2Frame(FrameHeaderName, MajorVersion));
                                    Frames.Add(FrameHeaderName, FrameList);
                                }

                                //Keep Track of which count of the Frame we are working on (some frames may be in the tag more than once)
                                int currentFrameIndex = Frames[FrameHeaderName].Count - 1;
                                ID3v2Frame currentFrame = Frames[FrameHeaderName][currentFrameIndex];

                                currentFrame.getFrameFlags(TempHeader[8], TempHeader[9], AllFramesUnsynced);

                                //ID3v2.3 does not appear to use syncsafe numbers for frame sizes (2.4 does)
                                bool unsync = currentFrame.FF_Unsynchronisation;

                                if (MajorVersion > 3)
                                {
                                    currentFrame.FrameSize = ID3Base.syncsafe(TempHeader[4], TempHeader[5], TempHeader[6], TempHeader[7]);
                                }
                                else
                                {
                                    currentFrame.FrameSize = (int)ID3Base.expand(TempHeader[4], TempHeader[5], TempHeader[6], TempHeader[7]);//(int)((TempHeader[4] << 24) | (TempHeader[5] << 16) | (TempHeader[6] << 8) | (TempHeader[7]));
                                }

                                totalFrameSize += currentFrame.FrameSize + 10; //+10 for Frame Header

                                //Set FrameData
                                byte[] tempData = new byte[currentFrame.FrameSize];
                                fs.Read(tempData, 0, currentFrame.FrameSize);
                                currentFrame.getFrameData(tempData);
                            }
                        }
                    }
                    Console.WriteLine("Padding Exsists: {0}", PaddingExists.ToString());
                    Console.WriteLine("TagSize: {0:D}, TotalFrameSize: {1:D}", TagSize, totalFrameSize);
                    Console.WriteLine();
                }
            }

            ProcessFrames();

            fs.Position = fsOriginalPosition;
        }

        private void ProcessFrames()
        {
            string[] FrameNames = 
    {
        "TIT2", "TT2",  //Title
        "TPE1", "TP1",  //Artist
        "TALB", "TAL",  //Album
        "APIC", "PIC"   //Album Picture
    };

            foreach (string name in FrameNames)
            {
                if (Frames.ContainsKey(name))
                {
                    switch (name)
                    {
                        //Title
                        case "TIT2":
                        case "TT2":
                            if (Frames[name][0].Data.GetType() == typeof(string))
                            {
                                Title = (string)Frames[name][0].Data;
                            }
                            break;

                        //Artist
                        case "TPE1":
                        case "TP1":
                            if (Frames[name][0].Data.GetType() == typeof(string))
                            {
                                Artist = (string)Frames[name][0].Data;
                            }
                            break;

                        //Album
                        case "TALB":
                        case "TAL":
                            if (Frames[name][0].Data.GetType() == typeof(string))
                            {
                                Album = (string)Frames[name][0].Data;
                            }
                            break;

                        //Album Picture
                        case "APIC":
                        case "PIC":
                            if (Frames[name][0].Data.GetType() == typeof(ID3v2APICFrame))
                            {
                                //((ID3v2APICFrame)Frames[name][0].Data).Picture.Save("2_" + this.Title + ".jpg");
                            }
                            break;
                    }
                }
            }
        }

        public Image getImage(ID3v2APICImageType iType)
        {
            string FrameName = "";
            if (Frames.ContainsKey("APIC"))
            {
                FrameName = "APIC";
            }
            else if (Frames.ContainsKey("PIC"))
            {
                FrameName = "PIC";
            }

            if (FrameName != "")
            {
                foreach (ID3v2Frame t in Frames[FrameName])
                {
                    if ((ID3v2APICImageType)((ID3v2APICFrame)t.Data).ImageType == iType)
                    {
                        return ((ID3v2APICFrame)t.Data).Picture;
                    }
                }
            }

            return null;
        }

        public override byte[] ToByte()
        {
            //Setup Encoding Here For Write Method
            return new byte[0];
        }
    }
}
