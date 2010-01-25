using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;

/* ----------------------------------------------------------

original C++ code by:
                    Gustav "Grim Reaper" Munkby
                    http://floach.pimpin.net/grd/
                    grimreaperdesigns@gmx.net

modified and converted to C# by:
                    Robert A. Wlodarczyk
                    http://rob.wincereview.com:8080
                    rwlodarc@hotmail.com

modified some more by:
                    Jeff "Babbage" Patyk
                    http://www.digitalbrink.com/
                    babbage@digitalbrink.com
---------------------------------------------------------- */

//http://www.devhood.com/tutorials/tutorial_details.aspx?tutorial_id=79

public class MPEGAudioFrame
{
    private byte[] RawHeader;

    public int VersionIndex;       //B
    public int LayerIndex;         //C
    public bool ProtectionBit;     //D
    public int BitrateIndex;       //E
    public int FrequencyIndex;     //F
    public bool PaddingBit;        //G
    public bool PrivateBit;        //H
    public int ChannelModeIndex;   //I
    public int ModeExtensionIndex; //J     
    public bool CopyrightBit;      //K
    public bool OriginalBit;       //L
    public int EmphasisIndex;      //M

    public bool Valid;
    public int Size;
    

    public MPEGAudioFrame(byte[] Header)
    {
        RawHeader = Header;

        Valid = HeaderCheck();
        if (Valid)
        {
            ProcessInfo();
        }
    }

    private bool HeaderCheck()
    {
        //AAAAAAAA AAABBCCD EEEEFFGH IIJJKLMM

        return (
            (RawHeader[0] == 0xFF) && ((RawHeader[1] & 0xE0) == 0xE0) &&            //AAAAAAAA AAA = Frame Sync (Must be All 1's)
            ((RawHeader[1] & 0x18) != 0x08) &&                                      //BB = MPEG Audio Version (Cannot be 01 - Reserved)
            ((RawHeader[1] & 0x06) != 0x00) &&                                      //CC = Layer Description (Cannot be 00 - Reserved)
                                                                                    //D = Protection Bit (Nothing to Check)
            ((RawHeader[2] & 0xF0) != 0x00) && ((RawHeader[2] & 0xF0) != 0xF0) &&   //EEEE = Bitrate Index (Cannot be All 0's or All 1's
            ((RawHeader[2] & 0x0C) != 0x0C) &&                                      //FF - Frequency Index (Cannot be 11 - Reserved)
                                                                                    //G - Padding Bit (Nothing to Check)
                                                                                    //H - Private Bit (Nothing to Check)
                                                                                    //II - Channel Mode (Nothing to Check)
                                                                                    //JJ - Mode Extension (Nothing to Check)
                                                                                    //K - Copyright (Nothing to Check)
                                                                                    //L - Original (Nothing to Check)
            ((RawHeader[3] & 0x03) != 0x02)                                         //MM - Emphasis (Cannot be 10 - Reserved)
        );
    }

    private void ProcessInfo() 
    {
        //A: Frame Sync - Nothing to Store

        //B: MPEG Version [Index - Use getVersion() For MPEG Version]
        VersionIndex = (int)((RawHeader[1] >> 3) & 0x03);

        //C: Layer Description [Index]
        LayerIndex = (int)((RawHeader[1] >> 1) & 0x03);

        //D: Protection Bit
        ProtectionBit = ((RawHeader[1] & 0x01) == 0x01);

        //E: Bitrate Index
        BitrateIndex = (int)((RawHeader[2] >> 4) & 0x0F);

        //F: Frequency Index
        FrequencyIndex = (int)((RawHeader[2] >> 2) & 0x03);

        //G: Padding Bit
        PaddingBit = ((RawHeader[2] & 0x02) == 0x02);

        //H: Private Bit
        PrivateBit = ((RawHeader[2] & 0x01) == 0x01);

        //I: Channel Mode Index
        ChannelModeIndex = (int)((RawHeader[3] >> 6) & 0x03);

        //J: Mode Extension Index
        ModeExtensionIndex = (int)((RawHeader[3] >> 4) & 0x03);

        //K: Copyright Bit
        CopyrightBit = ((RawHeader[3] & 0x08) == 0x08);

        //L: Original Bit
        OriginalBit = ((RawHeader[3] & 0x04) == 0x04);

        //M: Emphasis Index
        EmphasisIndex = (int)(RawHeader[3] & 0x03);

        //Determine Frame Size
        int Padding = (PaddingBit) ? 1 : 0;
        if (getLayer() == 1)
        {
            Size = (12 * (getBitrate() * 1000) / getFrequency() + Padding) * 4;
        }
        else
        {
            Size = 144 * (getBitrate() * 1000) / getFrequency() + Padding;
        }
    }

    public double getVersion()
    {
        double[] VersionTable = { 2.5, 0.0, 2.0, 1.0 };
        return VersionTable[VersionIndex];
    }

    public int getLayer()
    {
        int[] LayerTable = { 0, 3, 2, 1 };
        return LayerTable[LayerIndex];
    }

    public int getBitrate() 
    {
            int[,,] BitrateTable =
                {
                    { // MPEG 2 & 2.5
                        {0,  8, 16, 24, 32, 40, 48, 56, 64, 80, 96,112,128,144,160,0}, // Layer III
                        {0,  8, 16, 24, 32, 40, 48, 56, 64, 80, 96,112,128,144,160,0}, // Layer II
                        {0, 32, 48, 56, 64, 80, 96,112,128,144,160,176,192,224,256,0}  // Layer I
                    },

                    { // MPEG 1
                        {0, 32, 40, 48, 56, 64, 80, 96,112,128,160,192,224,256,320,0}, // Layer III
                        {0, 32, 48, 56, 64, 80, 96,112,128,160,192,224,256,320,384,0}, // Layer II
                        {0, 32, 64, 96,128,160,192,224,256,288,320,352,384,416,448,0}  // Layer I
                    }
                };

            return BitrateTable[VersionIndex & 1, LayerIndex - 1, BitrateIndex];
    }

    public int getFrequency()
    {
        int[,] FrequencyTable =
            {
                {32000, 16000,  8000}, // MPEG 2.5
                {    0,     0,     0}, // Reserved
                {22050, 24000, 16000}, // MPEG 2
                {44100, 48000, 32000}  // MPEG 1
            };

        return FrequencyTable[VersionIndex, FrequencyIndex];
    }

    public string getChannelMode()
    {
        switch (ChannelModeIndex)
        {
            default:
            case 0:
                return "Stereo";
            case 1:
                return "Joint Stereo";
            case 2:
                return "Dual Channel";
            case 3:
                return "Single Channel";
        }
    }

    public void getExtentionMode() 
    {
        //TODO: Figure out how to return the Extension Mode
    }
    
    public string getEmphasis() 
    {
        switch (EmphasisIndex)
        {
            default:
            case 0:
                return "none";
            case 1:
                return "50/15 ms";
            case 2:
                return "reserved";
            case 3:
                return "CCIT J.17";
        }
    }
}

public class MP3
{
    // Public variables for storing the information about the MP3
    public int intBitRate;
    public string strFileName;

    /// <summary>
    /// The size of the MP3 we are currently opening (in bytes).
    /// </summary>
    public long lngFileSize;
    public int intFrequency;

    /// <summary>
    /// The stereo mode of the song.
    /// </summary>
    public string strMode;

    /// <summary>
    /// Length of song (in seconds).
    /// </summary>
    public int intLength;

    /// <summary>
    /// Length of song (formatted in hh:mm:ss).
    /// </summary>
    public string strLengthFormatted;

    // Private variables used in the process of reading in the MP3 files
    private bool bVBR;
    private int intVFrames;
    
    //Jeff
    private MPEGAudioFrame Header;
    private long posFirstAudioFrame;
    private long posLastAudioFrame;
    private int totalAudioFrames;

    public bool Valid;

    //private bool id3v1; //Temporarily to determine if id3v1 is present
    //private bool id3v2; //Temporarily to determine if id3v2 is present

    public ID3v1 id3v1;
    public ID3v2 id3v2;

    public MP3(string FileName)
    {
        FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
        Valid = false;

        // Set the filename not including the path information
        strFileName = @fs.Name;
        char[] chrSeparators = new char[]{'\\','/'};
        string[] strSeparator = strFileName.Split(chrSeparators);
        int intUpper = strSeparator.GetUpperBound(0);
        strFileName = strSeparator[intUpper];
        
        // Replace ' with '' for the SQL INSERT statement
        strFileName = strFileName.Replace("'", "''");
        
        // Set the file size
        lngFileSize = fs.Length;

        byte[] bytHeader = new byte[4];
        byte[] bytVBitRate = new byte[12];
        int intPos = 0;
        
        // Keep reading 4 bytes from the header until we know for sure that in 
        // fact it's an MP3
        int nextFrame = 0;  //Jeff - Location in stream to next Frame (current position + framesize)

        //Read if id3v1 exists
        id3v1 = new ID3v1(fs);

        //Read if id3v2 exists
        id3v2 = new ID3v2(fs);

        //Pass up the ID3v2 tag (if it exists)
        if (id3v2.Exists)
        {
            if (!id3v2.FooterExists)
            {
                intPos = (int)id3v2.TagSize + 10; //+10 for header
            }
            else
            {
                intPos = (int)id3v2.TagSize + 20; //+20 for header and footer
            }
        }

        do
        {
            fs.Position = intPos;
            fs.Read(bytHeader, 0, 4);

            Header = new MPEGAudioFrame(bytHeader);
            if (Header.Valid)
            {
                nextFrame = intPos + Header.Size;
                fs.Position = nextFrame;
                fs.Read(bytHeader, 0, 4);
                MPEGAudioFrame SecondHeader = new MPEGAudioFrame(bytHeader);
                if (SecondHeader.Valid)
                {
                    posFirstAudioFrame = intPos;
                    Valid = true;
                    break;
                }
                else
                {
                    //The next frame did not appear valid - reset stream position
                    fs.Position = intPos;
                }
            }

            intPos++;
        }
        while (!Valid && (fs.Position != fs.Length));

        // If the current file stream position is equal to the length, 
        // that means that we've read the entire file and it's not a valid MP3 file
        if(Valid && (fs.Position != fs.Length))
        {
            intPos += 4; //Bypass the 4 byte header

            //The following is retrieved from XING SDK //http://www.mp3-tech.org/programmer/decoding.html
            if (Header.getVersion() == 1.0)         //MPEG Version 1
            {
                if (Header.ChannelModeIndex == 3)   //Single Channel (Mono)
                {
                    intPos += 17;
                }
                else
                {
                    intPos += 32;
                }
            }
            else                                    //MPEG Version 2.0 or 2.5
            {
                if (Header.ChannelModeIndex == 3)   //Single Channel (Mono)
                {
                    intPos += 9;
                }
                else
                {
                    intPos += 17;
                }
            }
            
            // Check to see if the MP3 has a variable bitrate
            fs.Position = intPos;
            fs.Read(bytVBitRate,0,12);
            bVBR = LoadVBRHeader(bytVBitRate);

            // Once the file's read in, then assign the properties of the file to the public variables
            intBitRate = getBitrate();
            intFrequency = Header.getFrequency();
            strMode = Header.getChannelMode();
            intLength = getLengthInSeconds();
            strLengthFormatted = getFormattedLength();

            fs.Close();
        }
    }

    private bool LoadVBRHeader(byte[] inputheader)
    {
        // If it's a variable bitrate MP3, the first 4 bytes will read 'Xing'
        // since they're the ones who added variable bitrate-edness to MP3s
        if((char)inputheader[0] == 'X' && (char)inputheader[1] == 'i' && (char)inputheader[2] == 'n' && (char)inputheader[3] == 'g')
        {
            int flags = (int)ID3.expand(inputheader[4], inputheader[5], inputheader[6], inputheader[7]);//(int)(((inputheader[4] & 255) << 24) | ((inputheader[5] & 255) << 16) | ((inputheader[6] & 255) <<  8) | ((inputheader[7] & 255)));
            
            /**
             * Flags:
             * Frames
             * Bytes
             * TOC
             */
            
            if((flags & 0x0001) == 1)
            {
                intVFrames = (int)(((inputheader[8] & 255) << 24) | ((inputheader[9] & 255) << 16) | ((inputheader[10] & 255) <<  8) | ((inputheader[11] & 255)));
                return true;
            }
            else
            {
                intVFrames = -1;
                return true;
            }
        }
        return false;
    }

    //Jeff
    public bool IsVBR()
    {
        return this.bVBR;
    }

    private int getBitrate() 
    {
        // If the file has a variable bitrate, then we return an integer average bitrate,
        // otherwise, we use a lookup table to return the bitrate
        if(bVBR)
        {
            double medFrameSize = (double)lngFileSize / (double)getNumberOfFrames();
            return (int)((medFrameSize * (double)Header.getFrequency()) / (1000.0 * ((Header.LayerIndex==3) ? 12.0 : 144.0)));
        }
        else
        {
            return Header.getBitrate();
        }
    }

    private int getLengthInSeconds() 
    {
        // "intKilBitFileSize" made by dividing by 1000 in order to match the "Kilobits/second"


        /* TODO: 
         * Should subtract the size of the ID3v2 Tag AND the ID3v1 Tag (if they 
         * exist), to get the most accurate size. Also, using Math.Round (which 
         * is not exactly necessary), will give us a more accurate length of the
         * song).
         * 
         * long curFileSize = lngFileSize;
         * curFileSize = curFileSize - 116376 - 128; //For ID3v1
         */

        long curFileSize = lngFileSize;

        if (id3v1.Exists)
        {
            //ID3v1 Tag size is 128 bytes
            curFileSize -= (long)id3v1.TagSize;
        }

        if (id3v2.Exists)
        {
            curFileSize -= (long)id3v2.TagSize;
        }

        long intKiloBitFileSize = (long)((8 * curFileSize) / 1000);
        return (int) Math.Round( intKiloBitFileSize / (float)getBitrate() );
    }

    /// <summary>
    /// Output the length of the song in HH:MM:SS format.
    /// </summary>
    /// <returns>The formatted length (in HH:MM:SS).</returns>
    private string getFormattedLength() 
    {
        // Complete number of seconds
        int s  = getLengthInSeconds();

        // Seconds to display
        int ss = s%60;

        // Complete number of minutes
        int m  = (s-ss)/60;

        // Minutes to display
        int mm = m%60;

        // Complete number of hours
        int h = (m-mm)/60;

        // Make "hh:mm:ss"
        return h.ToString("D2") + ":" + mm.ToString("D2") + ":" + ss.ToString("D2");
    }

    /// <summary>
    /// Retrieve the number of MPEG frames from the MP3.
    /// </summary>
    /// <returns>The number of MPEG frames.</returns>
    private int getNumberOfFrames() 
    {
        // Again, the number of MPEG frames is dependant on whether it's a variable bitrate MP3 or not
        if (!bVBR)
        {
            double medFrameSize = (double)(((Header.LayerIndex == 3) ? 12 : 144) * ((1000.0 * (float)Header.getBitrate()) / (float)Header.getFrequency()));
            return (int)(lngFileSize / medFrameSize);
        }
        else
        {
            return intVFrames;
        }
    }

    //Jeff
    public override string ToString()
    {
        if (Valid)
        {
            string info =
                "Reading file: " + this.strFileName + "\n" +
                "Frequency: " + this.intFrequency.ToString() + "\n" +
                "Bitrate: " + this.intBitRate.ToString() + "\n" +
                "Is this VBR Encoded? : " + this.IsVBR().ToString() + "\n" +
                "Length of the Song: " + (this.intLength / 60) + ":" + (this.intLength % 60) + "\n" +
                "Length of the Song (Formatted): " + this.strLengthFormatted + "\n" +
                String.Format("Size of the MP3: {0:##.00} MB", (this.lngFileSize / 1024.0 / 1024.0)) + "\n" +
                "Output Mode: " + this.strMode + "\n";

            info += "[ID3v1]\n";
            if (id3v1.Exists)
            {
                info += "ID3v1 Version: ID3v1." + id3v1.MajorVersion.ToString() + "\n";
                info += "Title: " + id3v1.Title + "\n";
                info += "Artist: " + id3v1.Artist + "\n";
                info += "Album: " + id3v1.Album + "\n";
            }
            else
            {
                info += "ID3v1 Does Not Exist\n";
            }

            info += "[ID3v2]\n";
            if (id3v2.Exists)
            {
                info += "ID3v2 Version: ID3v2." + id3v2.MajorVersion.ToString() + "." + id3v2.MinorVersion.ToString() + "\n";
                info += "Title: " + id3v2.Title + "\n";
                info += "Artist: " + id3v2.Artist + "\n";
                info += "Album: " + id3v2.Album + "\n";
            }
            else
            {
                info += "ID3v2 Does Not Exist\n";
            }

            return info;
        }
        else
        {
            return "Not a valid MP3.";
        }
    }

}

#region ID3 Tag Information

public class ID3
{
    public string Title;
    public string Artist;
    public string Album;
    public string Year;
    public string Comment;
    public string Genre;
    public int Track;
    public int GenreID;
    public bool Exists;
    public int TagSize;

    public int MajorVersion;
    public int MinorVersion;

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
        int bitShift = (byt.Length-1) * 7; //Decrement by 7 (for syncsafe)
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
        int bitShift = (byt.Length-1) * 8;
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

public class ID3v2ExtendedHeader
{
    public bool Exists;
    public bool TagIsUpdate;
    public bool CRCPresent;
    public bool TagRestrictions;
    public int MajorVersion;
    public int size;
    public bool ForceUnsync;

    public byte[] RawData;
    public UInt32 CRCData;
    public UInt32 PaddingSize;

    public ID3v2ExtendedHeader()
    {
        //Default Values
        setDefaults();
    }

    private void setDefaults()
    {
        MajorVersion = 0;

        Exists = false;
        TagIsUpdate = false;
        CRCPresent = false;
        TagRestrictions = false;
    }

    public void setID3Version(int ID3MajorVersion)
    {
        MajorVersion = ID3MajorVersion;
    }

    public void setData(byte[] data)
    {
        RawData = data;

        /**
         * MajorVersion == 3
         *   RawData[0] ... RawData[3]  == ExtendedHeader Size (Inclusive)
         *   RawData[4] ... RawData[5]  == Flags (Inclusive)
         *   RawData[6] ... RawData[9]  == Size of Padding (Inclusive)
         *   RawData[10] .. RawData[13] == CRCData (if CRCPresent == true)
         * 
         * MajorVersion == 4
         *   RawData[0] ... RawData[3] == ExtendedHeader Size (Inclusive)
         *   RawData[4] .............. == Number of Flag Bytes
         *   RawData[5] ... RawData[?] == Flags (Inclusive; Depended on RawData[4])
         *   RawData[?] ... RawData[?] == Data Corresponding to Flags
         **/

        processFlags();
    }

    public void processFlags()
    {
        switch (MajorVersion)
        {
            default:
            case 2:
                //ID3v2.2 = ExtendedHeader Not Supported
                break;
            case 3:
                //ID3v2.3 = %x0000000 00000000 (x = CRCPresent)
                CRCPresent = ((RawData[4] & 0x80) == 0x80);
                PaddingSize = ID3.expand(RawData[6], RawData[7], RawData[8], RawData[9]);

                if (CRCPresent)
                {
                    CRCData = ID3.expand(RawData[10], RawData[11], RawData[12], RawData[13]);
                }
                break;
            case 4:
                //ID3v2.4 = %0bcd0000 (b = TagIsUpdate, c = CRCPresent, d = TagRestrictions)
                int numOfFlags = (int)RawData[4];

                for (int i = 0; i < numOfFlags; ++i)
                {
                    TagIsUpdate = ((RawData[5 + i] & 0x40) == 0x40);
                    CRCPresent = ((RawData[5 + i] & 0x20) == 0x20);
                    TagRestrictions = ((RawData[5 + i] & 0x10) == 0x10);
                }

                int DataStartingPoint = 5 + numOfFlags;

                if (TagIsUpdate)
                {
                    //Flag Data Length is $00
                    DataStartingPoint++;
                }
                if (CRCPresent)
                {
                    //Flag Data Length is $05
                    DataStartingPoint++;

                    CRCData = ID3.syncsafe(RawData[DataStartingPoint],
                        RawData[DataStartingPoint + 1],
                        RawData[DataStartingPoint + 2],
                        RawData[DataStartingPoint + 3],
                        RawData[DataStartingPoint + 4]);

                    DataStartingPoint += 5;
                }
                if (TagRestrictions)
                {
                    //Flag data length is $01
                    DataStartingPoint++;

                    //Restrictions %ppqr rstt
                    //p - Tag Size Restrictions
                    byte p = (byte)(RawData[DataStartingPoint] & 0xC0);
                    switch (p)
                    {
                        case 0x00: //0000 0000 [00] - No more than 128 frames and 1 MB total tag size.
                            break;
                        case 0x40: //0100 0000 [01] - No more than 64 frames and 128 KB total tag size.
                            break;
                        case 0x80: //1000 0000 [10] - No more than 32 frames and 40 KB total tag size.
                            break;
                        case 0xC0: //1100 0000 [11] - No more than 32 frames and 4 KB total tag size.
                            break;
                    }

                    //q - Text encoding restrictions
                    byte q = (byte)(RawData[DataStartingPoint] & 0x20);
                    switch (q)
                    {
                        case 0x00: //0000 0000 [0] - No Restrictions
                            break;
                        case 0x20: //0010 0000 [1] - Strings are only encoded with ISO-8859-1 [ISO-8859-1] or UTF-8 [UTF-8].
                            break;
                    }

                    //r - Text fields size restrictions
                    byte r = (byte)(RawData[DataStartingPoint] & 0x18);
                    switch (r)
                    {
                        case 0x00: //0000 0000 [00] - No restrictions
                            break;
                        case 0x08: //0000 1000 [01] - No string is longer than 1024 characters.
                            break;
                        case 0x10: //0001 0000 [10] - No string is longer than 128 characters.
                            break;
                        case 0x18: //0001 1000 [11] - No string is longer than 30 characters.
                            break;
                    }

                    //s - Image encoding restrictions
                    byte s = (byte)(RawData[DataStartingPoint] & 0x04);
                    switch (s)
                    {
                        case 0x00: //0000 0000 [0] - No restrictions
                            break;
                        case 0x04: //0000 0100 [1] - Images are encoded only with PNG [PNG] or JPEG [JFIF].
                            break;
                    }

                    //t - Image size restrictions
                    byte t = (byte)(RawData[DataStartingPoint] & 0x03);
                    switch(t)
                    {
                        case 0x00: //0000 0000 [00] - No restrictions
                            break;
                        case 0x01: //0000 0001 [01] - All images are 256x256 pixels or smaller.
                            break;
                        case 0x02: //0000 0010 [10] - All images are 64x64 pixels or smaller.
                            break;
                        case 0x03: //0000 0011 [11] - All images are exactly 64x64 pixels, unless required otherwise.
                            break;
                    }
                }
                break;
        }
    }
}

public class ID3v2 : ID3
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

    public ID3v2()
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

    public ID3v2(FileStream fs)
    {
        //Store FileStreams current position
        long fsOriginalPosition = fs.Position;

        setDefaultValues();

        byte[] bytHeaderID3 = new byte[10];
        fs.Position = 0;
        fs.Read(bytHeaderID3, 0, 10);

        //ID3v2.X should start with "ID3"
        if ((char)bytHeaderID3[0] == 'I' && (char)bytHeaderID3[1] == 'D' && (char)bytHeaderID3[2] == '3')
        {
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
            TagSize = ID3.syncsafe(bytHeaderID3[6], bytHeaderID3[7], bytHeaderID3[8], bytHeaderID3[9]); //(int)((bytHeaderID3[6] << 21) | (bytHeaderID3[7] << 14) | (bytHeaderID3[8] << 7) | bytHeaderID3[9]);
            
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
                    ExtendedHeader.size = (int)ID3.expand(EH_Size[0], EH_Size[1], EH_Size[2], EH_Size[3]);
                    byte[] headerAndData = new byte[4 + ExtendedHeader.size];
                    fs.Position = 10;
                    fs.Read(headerAndData, 0, headerAndData.Length);
                    ExtendedHeader.setData(headerAndData);
                }
                else if (MajorVersion == 4)
                {
                    //Whole Extended Header
                    ExtendedHeader.size = ID3.syncsafe(EH_Size[0], EH_Size[1], EH_Size[2], EH_Size[3]);
                    byte[] headerAndData = new byte[ExtendedHeader.size];
                    fs.Position = 10;
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
                            currentFrame.FrameSize = (int)ID3.expand(TempHeader[3], TempHeader[4], TempHeader[5]);//(int)((TempHeader[3] << 16) | (TempHeader[4] << 8) | (TempHeader[5]));

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
                                currentFrame.FrameSize = ID3.syncsafe(TempHeader[4], TempHeader[5], TempHeader[6], TempHeader[7]);
                            }
                            else
                            {
                                currentFrame.FrameSize = (int)ID3.expand(TempHeader[4], TempHeader[5], TempHeader[6], TempHeader[7]);//(int)((TempHeader[4] << 24) | (TempHeader[5] << 16) | (TempHeader[6] << 8) | (TempHeader[7]));
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

        foreach(string name in FrameNames)
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
                if ( (ID3v2APICImageType)((ID3v2APICFrame)t.Data).ImageType == iType)
                {
                    return ((ID3v2APICFrame)t.Data).Picture;
                }
            }
        }

        return null;
    }
}

public class ID3v2Frame
{
    public string FrameName;
    public int FrameSize;
    public byte[] FrameData;
    public object Data;
    public int MajorVersion;

    //Frame Status Flags [Byte 9/10]
    public bool FS_TagAlterPreserve; //a
    public bool FS_FileAlterPreserve; //b
    public bool FS_ReadOnly; //c

    //Frame Format Flags [Byte 10/10]
    public bool FF_GroupingIdentity; //h
    public bool FF_Compression; //k
    public bool FF_Encryption; //m
    public bool FF_Unsynchronisation; //n
    public bool FF_DataLengthIndicator; //p

    public ID3v2Frame(int ID3MajorVersion)
    {
        MajorVersion = ID3MajorVersion;
        FrameName = "";
        FrameSize = 0;
    }

    public ID3v2Frame(string frameName, int ID3MajorVersion)
    {
        MajorVersion = ID3MajorVersion;
        FrameName = frameName;
        FrameSize = 0;
    }

    public void getFrameFlags(byte bytFrameFlag1, byte bytFrameFlag2)
    {
        getFrameFlags(bytFrameFlag1, bytFrameFlag2, false);
    }

    public void getFrameFlags(byte bytFrameFlag1, byte bytFrameFlag2, bool ForceUnsync)
    {
        if (MajorVersion == 2)
        {
            //Everything is False (Except Possibly FF_Unsyncronisation)
            FS_TagAlterPreserve = false;
            FS_FileAlterPreserve = false;
            FS_ReadOnly = false;
            FF_Compression = false;
            FF_Encryption = false;
            FF_GroupingIdentity = false;
            FF_DataLengthIndicator = false;

            //Could possibly be true if ID3 Header says so
            FF_Unsynchronisation = ForceUnsync;
        }
        else if (MajorVersion == 3)
        {
            //Frame Status, bytFrameFlag1, %abc00000
            FS_TagAlterPreserve = ((bytFrameFlag1 & 0x80) == 0x80);     //a
            FS_FileAlterPreserve = ((bytFrameFlag1 & 0x40) == 0x40);    //b
            FS_ReadOnly = ((bytFrameFlag1 & 0x20) == 0x20);             //c

            //Frame Format, bytFrameFlag2, %ijk00000
            FF_Compression = ((bytFrameFlag2 & 0x80) == 0x80);          //i
            FF_Encryption = ((bytFrameFlag2 & 0x40) == 0x40);           //j
            FF_GroupingIdentity = ((bytFrameFlag2 & 0x20) == 0x20);     //k

            //Unused
            FF_Unsynchronisation = ForceUnsync;  //Will be false if ForceUnsync is false
            FF_DataLengthIndicator = false;
        }
        else if (MajorVersion == 4)
        {
            //Frame Status, bytFrameFlag1, %0abc0000
            FS_TagAlterPreserve = ((bytFrameFlag1 & 0x40) == 0x40);     //a
            FS_FileAlterPreserve = ((bytFrameFlag1 & 0x20) == 0x20);    //b
            FS_ReadOnly = ((bytFrameFlag1 & 0x10) == 0x10);             //c

            //Frame Format, bytFrameFlag2, %0h00kmnp
            FF_GroupingIdentity = ((bytFrameFlag2 & 0x40) == 0x40);     //h
            FF_Compression = ((bytFrameFlag2 & 0x08) == 0x08);          //k
            FF_Encryption = ((bytFrameFlag2 & 0x04) == 0x04);           //m
            FF_Unsynchronisation = ((bytFrameFlag2 & 0x02) == 0x02);    //n
            FF_DataLengthIndicator = ((bytFrameFlag2 & 0x01) == 0x01);  //p

            //If ForceUnsyc was set
            if (ForceUnsync)
            {
                FF_Unsynchronisation = true;
            }
        }
    }

    public void getFrameData(byte[] data)
    {
        if (this.FrameSize != 0)
        {
            FrameData = data;
            ProcessFrame();
        }
    }

    //Assigns enc through reference, and returns a bool if we are using Byte Order Marks
    private bool GetEncodingType(ref Encoding enc, int start)
    {
        enc = null;          //Reset Encoder to Null to begin
        bool useBOM = false; //BOM = Unicode Byte Order Mark
        
        //System.Text.Encoding enc = null;
        switch (FrameData[0])
        {
            case 0x00:
                //ISO-8859-1 [ISO-8859-1]. Terminated with $00.
                enc = Encoding.GetEncoding("ISO-8859-1");
                break;
            case 0x01:
                //UTF-16 [UTF-16] encoded Unicode [UNICODE] with BOM. All
                //strings in the same frame SHALL have the same byteorder.
                //Terminated with $00 00.
                //enc = Encoding.GetEncoding("UTF-16");
                useBOM = true;
                break;
            case 0x02:
                //UTF-16BE [UTF-16] encoded Unicode [UNICODE] without BOM.
                //Terminated with $00 00.
                enc = Encoding.GetEncoding("UTF-16BE");
                break;
            case 0x03:
                //UTF-8 [UTF-8] encoded Unicode [UNICODE]. Terminated with $00.
                enc = Encoding.GetEncoding("UTF-8");
                break;
        }

        if (useBOM)
        {
            Encoding[] UnicodeEncodings = { Encoding.BigEndianUnicode, Encoding.Unicode, Encoding.UTF8 };

            for (int i = 0; enc == null && i < UnicodeEncodings.Length; ++i)
            {
                bool PreambleEqual = true;
                byte[] Preamble = UnicodeEncodings[i].GetPreamble();

                for (int j = 0; PreambleEqual && j < Preamble.Length; ++j)
                {
                    PreambleEqual = Preamble[j] == FrameData[j+start]; //+start position = Skip Text Encoding Byte
                }

                if (PreambleEqual)
                {
                    enc = UnicodeEncodings[i];
                }
            }
        }



        return useBOM;
    }

    public void ProcessFrame()
    {
        byte[] NewFrameData;

        if (FF_Unsynchronisation)
        {
            NewFrameData = ID3.UnSync(FrameData);
        }
        else
        {
            NewFrameData = FrameData;
        }

        //T-Type Frame (Text)
        if (FrameName.ToCharArray()[0] == 'T' && FrameName != "TXXX" && FrameName != "TXX")
        {
            if (NewFrameData.Length > 1)
            {
                System.Text.Encoding enc = null;
                bool useBOM = GetEncodingType(ref enc, 1);

                if (useBOM)
                {
                    Data = enc.GetString(NewFrameData, 1 + enc.GetPreamble().Length, NewFrameData.Length - (1 + enc.GetPreamble().Length));
                }
                else
                {
                    Data = enc.GetString(NewFrameData, 1, NewFrameData.Length - 1);
                }

                Data = ((string)Data).Trim('\0').Trim();
            }
            else
            {
                //No Frame Data, so blank.
                Console.WriteLine("The Frame Above Had No Data");
                Data = "";
            }
        }
        else if (FrameName == "TXXX" || FrameName == "TXX")
        {
            //TXXX, TXX Frame Goes Here (TODO)
        }

        //W-Type Frame (TODO: Needs more testing)
        if (FrameName.ToCharArray()[0] == 'W' && FrameName != "WXXX" && FrameName != "WXX")
        {
            Encoding enc = Encoding.ASCII;
            Data = enc.GetString(NewFrameData, 0, NewFrameData.Length);
        }
        else if (FrameName == "WXXX" || FrameName == "WXX")
        {
            //WXXX, WXX is always ISO-8859-1 Encoded
            Encoding enc = Encoding.GetEncoding("ISO-8859-1");
            Data = enc.GetString(NewFrameData, 0, NewFrameData.Length);
        }

        if (FrameName == "APIC" || FrameName == "PIC")
        {
            if (NewFrameData.Length > 1)
            {
                int DataPosition = 0;
                ID3v2APICFrame apic = new ID3v2APICFrame();

                //Skip just Text Encoding
                DataPosition++;

                //Get MimeType (as Generic Text)
                Encoding enc = Encoding.ASCII;
                if (MajorVersion > 2)
                {
                    int BeginMimeType = DataPosition;
                    while (NewFrameData[DataPosition] != 0x00)
                    {
                        DataPosition++;
                    }
                    apic.MIMEType = enc.GetString(NewFrameData, BeginMimeType, DataPosition - BeginMimeType);
                }
                else
                {
                    apic.MIMEType = enc.GetString(NewFrameData, DataPosition, 3);
                    DataPosition += 2; //Should be Increment by 3, but next instruction is to increment by 1
                }

                //Get ImageType
                DataPosition++;
                apic.ImageType = NewFrameData[DataPosition];

                //Get Description
                DataPosition++;
                bool useBOM = GetEncodingType(ref enc, DataPosition); //Determine what encoding style we need

                int BeginDescription = DataPosition;
                if (DataPosition + 1 < NewFrameData.Length && enc == Encoding.Unicode)  //Little Endian, Every Two Bytes (16bits)
                {
                    while (!(NewFrameData[DataPosition] == 0x00 && NewFrameData[DataPosition + 1] == 0x00))
                    {
                        DataPosition += 2;
                    }
                    
                    //Skip Past $00 00 at End
                    DataPosition += 2;
                }
                else
                {
                    while (NewFrameData[DataPosition] != 0x00)
                    {
                        DataPosition++;
                    }
                    
                    //Skip Past $00 at End
                    DataPosition++;
                }

                if (!useBOM)
                {
                    apic.Description = enc.GetString(NewFrameData, BeginDescription, DataPosition - BeginDescription);
                }
                else
                {
                    apic.Description = enc.GetString(NewFrameData, BeginDescription + enc.GetPreamble().Length, (DataPosition - (BeginDescription + enc.GetPreamble().Length)) );
                }
                apic.Description = apic.Description.Trim('\0').Trim();

                //Get Binary Data
                MemoryStream ms = new MemoryStream(NewFrameData, DataPosition, NewFrameData.Length - DataPosition);
                try
                {
                    apic.Picture = Image.FromStream(ms);
                }
                catch (System.ArgumentException ex)
                {
                    apic.Picture = null;
                    Console.WriteLine(ex.Message);
                }
                
                Data = apic;
            }
        }
    }
}

public struct ID3v2UserFrame
{
    public string Description;
    public string Value;
}

public enum ID3v2APICImageType
{
    Other = 0,
    FileIcon,
    OtherIcon,
    CoverFront,
    CoverBack,
    LeafletPage,
    Media,
    LeadArtist,
    Artist,
    Conductor,
    Band,
    Composer,
    Lyricist,
    RecordingLocation,
    DuringRecording,
    MovieScreenCapture,
    BrightFish,
    Illustration,
    BandLogo,
    PublisherLogo
}

public class ID3v2APICFrame
{
    public string MIMEType;
    public string Description;
    public byte ImageType;
    public Image Picture;

    public bool IsType(ID3v2APICImageType iType)
    {
        if ((ID3v2APICImageType)ImageType == iType)
            return true;
        else
            return false;
    }

    public string PictureType()
    {
        switch (ImageType)
        {
            default:
            case 0x00:
                return "Other";
            case 0x01:
                return "32x32 pixels 'file icon' (PNG only)";
            case 0x02:
                return "Other file icon";
            case 0x03:
                return "Cover (front)";
            case 0x04:
                return "Cover (back)";
            case 0x05:
                return "Leaflet page";
            case 0x06:
                return "Media (e.g. label side of CD)";
            case 0x07:
                return "Lead artist/lead performer/soloist";
            case 0x08:
                return "Artist/performer";
            case 0x09:
                return "Conductor";
            case 0x0A:
                return "Band/Orchestra";
            case 0x0B:
                return "Composer";
            case 0x0C:
                return "Lyricist/text writer";
            case 0x0D:
                return "Recording Location";
            case 0x0E:
                return "During recording";
            case 0x0F:
                return "During performance";
            case 0x10:
                return "Movie/video screen capture";
            case 0x11:
                return "A bright coloured fish";
            case 0x12:
                return "Illustration";
            case 0x13:
                return "Band/artist logotype";
            case 0x14:
                return "Publisher/Studio logotype";
        }
    }
}

public class ID3v1 : ID3
{
    public ID3v1()
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

    public ID3v1(FileStream fs)
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
}

#endregion