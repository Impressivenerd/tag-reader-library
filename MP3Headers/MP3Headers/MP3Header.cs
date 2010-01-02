using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

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

public class MP3Header
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
    private ulong bithdr;
    private bool boolVBitRate;
    private int intVFrames;
    
    //Jeff
    private int padding;

    //private bool id3v1; //Temporarily to determine if id3v1 is present
    //private bool id3v2; //Temporarily to determine if id3v2 is present

    private ID3v1 id3v1;
    private ID3v2 id3v2;

    public bool ReadMP3Information(string FileName)
    {
        FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);

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
        bool valid = false; //Jeff - Determine if the MP3 is valid
        int frameSize = 0;  //Jeff - Size of the MPEG frame
        int nextFrame = 0;  //Jeff - Location in stream to next Frame (current position + framesize)

        //Read if id3v1 exists
        id3v1 = new ID3v1(fs);

        //Read if id3v2 exists
        id3v2 = new ID3v2(fs);

        //Pass up the ID3v2 tag (if it exists)
        if (id3v2.Exists)
        {
            intPos = (int)id3v2.TagSize;
        }

        do
        {
            fs.Position = intPos;
            fs.Read(bytHeader, 0, 4);

            if (quickHeaderCheck(bytHeader))
            {
                LoadMP3Header(bytHeader);
                if (IsValidHeader())
                {
                    //Get the current frame size to find where the next frame *should* be
                    frameSize = 144 * (getBitrate() * 1000) / getFrequency() + getPaddingQuick(bytHeader);
                    nextFrame = intPos + frameSize;
                    fs.Position = nextFrame;
                    fs.Read(bytHeader, 0, 4);
                    if (quickHeaderCheck(bytHeader))
                    {
                        LoadMP3Header(bytHeader);
                        padding = getPaddingQuick(bytHeader);
                        valid = true;
                    }
                    else
                    {
                        //The next frame did not appear valid - reset stream position
                        fs.Position = intPos;
                    }
                }
            }

            intPos++;
        }
        while (!valid && (fs.Position != fs.Length));
        //while(!IsValidHeader() && (fs.Position!=fs.Length));

        // If the current file stream position is equal to the length, 
        // that means that we've read the entire file and it's not a valid MP3 file
        if(fs.Position != fs.Length)
        {
            intPos += 3;

            if(getVersionIndex() == 3)    // MPEG Version 1
            {
                if(getModeIndex() == 3)    // Single Channel
                {
                    intPos += 17;
                }
                else
                {
                    intPos += 32;
                }
            }
            else                        // MPEG Version 2.0 or 2.5
            {
                if(getModeIndex() == 3)    // Single Channel
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
            boolVBitRate = LoadVBRHeader(bytVBitRate);

            // Once the file's read in, then assign the properties of the file to the public variables
            intBitRate = getBitrate();
            intFrequency = getFrequency();
            strMode = getMode();
            intLength = getLengthInSeconds();
            strLengthFormatted = getFormattedLength();
            fs.Close();
            return true;
        }
        return false;
    }

    //Jeff
    private bool quickHeaderCheck(byte[] c)
    {
        /**
         * Byte 0 = Frame Sync
         * Byte 1 (First 3 bits) = Frame Sync
         * Byte 2 (First 4 bits) = Bitrate
         **/

        //Make sure Frame Sync is 11111111 111 and bitrate IS NOT 1111
        if ((c[0] == 0xFF) &&
            ((c[1] & 0xE0) == 0xE0) &&
            ((c[2] & 0xF0) != 0xF0))
        {
            return headerChecks(c);
        }

        return false;
    }

    //Jeff
    private bool headerChecks(byte[] pHeader)
    {
        /**
         * Header is built like so:
         * AAAAAAAA AAABBCCD EEEEFFGH IIJJKLMM
         * BYTE 0   BYTE 1   BYTE 2   BYTE 3
         * Description of Letters: http://mpgedit.org/mpgedit/mpeg_format/mpeghdr.htm
         **/

        // get MPEG version [bit 11,12]
        if( ((pHeader[1] >> 3) & 0x03) == 1 )
            return false;

        // get MPEG Layer (Listed in reversed order - 00 = reserved, 11 = Layer 1)
        if ( (3 - ((pHeader[1] >> 1) & 0x03)) == 3)
            return false;

        // bitrate [bit 16..19]
        if ( ((pHeader[2] >> 4) & 0x0F) == 0x0F)		// all bits set is reserved
	        return false;

        // sampling rate [bit 20,21]
        if ( ((pHeader[2] >> 2) & 0x03) == 0x03)		// all bits set is reserved
            return false;

        // emphasis [bit 30,31]
        if ( ((pHeader[3]) & 0x03) == 2)                // bits set are reserved
	        return false;

        return true;
    }

    //Jeff
    private int getPaddingQuick(byte[] pHeader)
    {
        return (int)((pHeader[2] >> 1) & 1);
    }

    private void LoadMP3Header(byte[] c)
    {
        // this thing is quite interesting, it works like the following
        // c[0] = 00000011
        // c[1] = 00001100
        // c[2] = 00110000
        // c[3] = 11000000
        // the operator << means that we'll move the bits in that direction
        // 00000011 << 24 = 00000011000000000000000000000000
        // 00001100 << 16 =         000011000000000000000000
        // 00110000 << 24 =                 0011000000000000
        // 11000000       =                         11000000
        //                +_________________________________
        //                  00000011000011000011000011000000
        bithdr = (ulong)(((c[0] & 255) << 24) | ((c[1] & 255) << 16) | ((c[2] & 255) <<  8) | ((c[3] & 255)));
    }

    private bool LoadVBRHeader(byte[] inputheader)
    {
        // If it's a variable bitrate MP3, the first 4 bytes will read 'Xing'
        // since they're the ones who added variable bitrate-edness to MP3s
        if(inputheader[0] == 88 && inputheader[1] == 105 && 
            inputheader[2] == 110 && inputheader[3] == 103)
        {
            int flags = (int)(((inputheader[4] & 255) << 24) | ((inputheader[5] & 255) << 16) | ((inputheader[6] & 255) <<  8) | ((inputheader[7] & 255)));
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

    private bool IsValidHeader() 
    {
        return (((getFrameSync()      & 2047)==2047) &&
                ((getVersionIndex()   &    3)!=   1) &&
                ((getLayerIndex()     &    3)!=   0) && 
                ((getBitrateIndex()   &   15)!=   0) &&
                ((getBitrateIndex()   &   15)!=  15) &&
                ((getFrequencyIndex() &    3)!=   3) &&
                ((getEmphasisIndex()  &    3)!=   2)    );
    }

    //Jeff
    public bool IsVBR()
    {
        return this.boolVBitRate;
    }

    private int getFrameSync()     
    {
        return (int)((bithdr>>21) & 2047); 
    }

    private int getVersionIndex()  
    { 
        return (int)((bithdr>>19) & 3);  
    }

    private int getLayerIndex()    
    { 
        return (int)((bithdr>>17) & 3);  
    }

    private int getProtectionBit() 
    { 
        return (int)((bithdr>>16) & 1);  
    }

    private int getBitrateIndex()  
    { 
        return (int)((bithdr>>12) & 15); 
    }

    private int getFrequencyIndex()
    { 
        return (int)((bithdr>>10) & 3);  
    }

    private int getPaddingBit()    
    { 
        return (int)((bithdr>>9) & 1);  
    }

    private int getPrivateBit()    
    { 
        return (int)((bithdr>>8) & 1);  
    }

    private int getModeIndex()     
    { 
        return (int)((bithdr>>6) & 3);  
    }

    private int getModeExtIndex()  
    { 
        return (int)((bithdr>>4) & 3);  
    }

    private int getCoprightBit()   
    { 
        return (int)((bithdr>>3) & 1);  
    }

    private int getOrginalBit()    
    { 
        return (int)((bithdr>>2) & 1);  
    }
    
    private int getEmphasisIndex() 
    { 
        return (int)(bithdr & 3);  
    }

    private double getVersion() 
    {
        double[] table = {2.5, 0.0, 2.0, 1.0};
        return table[getVersionIndex()];
    }

    private int getLayer() 
    {
        return (int)(4 - getLayerIndex());
    }

    private int getBitrate() 
    {
        // If the file has a variable bitrate, then we return an integer average bitrate,
        // otherwise, we use a lookup table to return the bitrate
        if(boolVBitRate)
        {
            double medFrameSize = (double)lngFileSize / (double)getNumberOfFrames();
            return (int)((medFrameSize * (double)getFrequency()) / (1000.0 * ((getLayerIndex()==3) ? 12.0 : 144.0)));
        }
        else
        {
            int[,,] table =        {
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

            return table[getVersionIndex() & 1, getLayerIndex()-1, getBitrateIndex()];
        }
    }

    private int getFrequency() 
    {
        int[,] table =    {    
                            {32000, 16000,  8000}, // MPEG 2.5
                            {    0,     0,     0}, // reserved
                            {22050, 24000, 16000}, // MPEG 2
                            {44100, 48000, 32000}  // MPEG 1
                        };

        return table[getVersionIndex(), getFrequencyIndex()];
    }

    /// <summary>
    /// Determine which output mode the song will be played in (Stereo, Join Stereo, Dual Channel, Single Channel).
    /// </summary>
    /// <returns>string</returns>
    private string getMode() 
    {
        switch(getModeIndex()) 
        {
            default:
                return "Stereo";
            case 1:
                return "Joint Stereo";
            case 2:
                return "Dual Channel";
            case 3:
                return "Single Channel";
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
        if (!boolVBitRate) 
        {
            double medFrameSize = (double)(((getLayerIndex()==3) ? 12 : 144) *((1000.0 * (float)getBitrate())/(float)getFrequency()));
            return (int)(lngFileSize/medFrameSize);
        }
        else 
            return intVFrames;
    }

    //Jeff
    public override string ToString()
    {
        return "Reading file: " + this.strFileName + "\n" +
            "Frequency: " + this.intFrequency.ToString() + "\n" +
            "Bitrate: " + this.intBitRate.ToString() + "\n" + 
            "Is this VBR Encoded? : " + this.IsVBR().ToString() + "\n" +
            "Length of the Song: " + (this.intLength / 60) + ":" + (this.intLength % 60) + "\n" +
            "Length of the Song (Formatted): " + this.strLengthFormatted + "\n" +
            String.Format("Size of the MP3: {0:##.00} MB", (this.lngFileSize / 1024.0 / 1024.0)) + "\n" +
            "ID3v1: " + this.id3v1.Exists.ToString() + "\n" + 
            "ID3v2: " + this.id3v2.Exists.ToString() + "\n" +
            "Title: " + id3v2.Title + "\n" +
            "Output Mode: " + this.strMode;
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

    public static int syncsafe(byte byt1, byte byt2, byte byt3, byte byt4)
    {
        return (int)((byt1 << 21) | (byt2 << 14) | (byt3 << 7) | byt4);
    }

    public string getGenre(int genreID)
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

public class ID3v2 : ID3
{
    //Version Information
    public int MajorVersion;
    public int MinorVersion;

    public bool AllFramesUnsynced;

    /*****
     * Extended Header Options
     *****/
    public bool ExtendedHeaderExists;
    public bool EH_TagIsUpdate;
    public bool EH_CRCPresent;
    public bool EH_TagRestrictions;

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
        MajorVersion = 0;
        MinorVersion = 0;
        Exists = false;
        TagSize = 0;

        AllFramesUnsynced = false;

        ExtendedHeaderExists = false;
        EH_TagIsUpdate = false;
        EH_CRCPresent = false;
        EH_TagRestrictions = false;

        Experimental = false;
        PaddingExists = false;
        FooterExists = false;

        Frames = new Dictionary<string, List<ID3v2Frame>>();
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

            //Byte 5 should be binary like so: abcd0000
            AllFramesUnsynced = ((bytHeaderID3[5] & 0x80) == 0x80);
            ExtendedHeaderExists = ((bytHeaderID3[5] & 0x40) == 0x40);
            Experimental = ((bytHeaderID3[5] & 0x20) == 0x20);
            FooterExists = ((bytHeaderID3[5] & 0x10) == 0x10);

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

            //ID3v2.2.X uses 3 letter identifiers versus the 4 letter identifiers in 2.3.X and 2.4.X
            if (MajorVersion > 2)
            {
                Console.WriteLine("ID3v2.{0:D} Detected...", MajorVersion);
                fs.Position = 10;
                while (!PaddingExists && fs.Position <= TagSize + 10) //(tag.TagSize + 10) == End Position (+10 for Original Header, +20 For Original Header and Footer [if present])
                {
                    byte[] TempHeader = new byte[10];
                    fs.Read(TempHeader, 0, 10);

                    if (!FooterExists && TempHeader[0] == 0x00 || TempHeader[1] == 0x00 || TempHeader[2] == 0x00 || TempHeader[3] == 0x00)
                    {
                        //Nothing Here but Padding; Skip to the end of the tag
                        //Footer and Padding are Mutually Exclusive
                        PaddingExists = true;
                    }
                    else
                    {
                        Console.WriteLine("HEADER[{0}{1}{2}{3}]", (char)TempHeader[0], (char)TempHeader[1], (char)TempHeader[2], (char)TempHeader[3]);

                        string FrameHeaderName = ((char)TempHeader[0]).ToString() +
                            ((char)TempHeader[1]).ToString() +
                            ((char)TempHeader[2]).ToString() +
                            ((char)TempHeader[3]).ToString();

                        int currentWorkingIndex = 0;

                        if (Frames.ContainsKey(FrameHeaderName))
                        {
                            Frames[FrameHeaderName].Add(new ID3v2Frame(FrameHeaderName));
                        }
                        else
                        {
                            List<ID3v2Frame> FrameList = new List<ID3v2Frame>();
                            FrameList.Add(new ID3v2Frame(FrameHeaderName));
                            Frames.Add(FrameHeaderName, FrameList);
                        }

                        currentWorkingIndex = Frames[FrameHeaderName].Count - 1;

                        Frames[FrameHeaderName][currentWorkingIndex].getFrameFlags(TempHeader[8], TempHeader[9]);

                        //ID3v2.3 does not appear to use syncsafe numbers for frame sizes (2.4 does, doesn't if unsync flag is active)
                        bool unsync = false;

                        if (MajorVersion > 3)
                        {
                            unsync = ((TempHeader[9] & 0x02) == 0x02);
                        }

                        if (!unsync && MajorVersion > 3)
                        {
                            Frames[FrameHeaderName][currentWorkingIndex].FrameSize = ID3.syncsafe(TempHeader[4], TempHeader[5], TempHeader[6], TempHeader[7]);
                        }
                        else
                        {
                            Frames[FrameHeaderName][currentWorkingIndex].FrameSize = (int)((TempHeader[4] << 24) | (TempHeader[5] << 16) | (TempHeader[6] << 8) | (TempHeader[7]));
                        }

                        //Set FrameData
                        byte[] tempData = new byte[Frames[FrameHeaderName][currentWorkingIndex].FrameSize];
                        fs.Read(tempData, 0, Frames[FrameHeaderName][currentWorkingIndex].FrameSize);
                        Frames[FrameHeaderName][currentWorkingIndex].getFrameData(tempData);
                    }
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("ID3v2.2 Not Supported. Skipping...");
            }
        }

        if (Frames.ContainsKey("TIT2"))
        {
            Title = (string)Frames["TIT2"][0].Data;
        }

        fs.Position = fsOriginalPosition;
    }
}

public class ID3v2Frame
{
    public string FrameName;
    public int FrameSize;
    public byte[] FrameData;
    public object Data;

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

    public ID3v2Frame()
    {
        FrameName = "";
        FrameSize = 0;
    }

    public ID3v2Frame(string frameName)
    {
        FrameName = frameName;
        FrameSize = 0;
    }

    public void getFrameFlags(byte bytFrameFlag1, byte bytFrameFlag2)
    {
        //Frame Status, bytFrameFlag1, %0abc0000
        FS_TagAlterPreserve = ((bytFrameFlag1 & 0x40) == 0x40);
        FS_FileAlterPreserve = ((bytFrameFlag1 & 0x20) == 0x20);
        FS_ReadOnly = ((bytFrameFlag1 & 0x10) == 0x10);

        //Frame Format, bytFrameFlag2, %0h00kmnp
        FF_GroupingIdentity = ((bytFrameFlag2 & 0x40) == 0x40);
        FF_Compression = ((bytFrameFlag2 & 0x08) == 0x08);
        FF_Encryption = ((bytFrameFlag2 & 0x04) == 0x04);
        FF_Unsynchronisation = ((bytFrameFlag2 & 0x02) == 0x02);
        FF_DataLengthIndicator = ((bytFrameFlag2 & 0x01) == 0x01);
    }

    public void getFrameData(byte[] data)
    {
        if (this.FrameSize != 0)
        {
            FrameData = data;
            testGetData();
        }
    }

    public void testGetData()
    {
        //T-Type Frame (Text)
        if (FrameName.ToCharArray()[0] == 'T' && FrameName != "TXXX")
        {
            bool useBOM = false; //BOM = Unicode Byte Order Mark
            System.Text.Encoding enc  = Encoding.ASCII;
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

            if (FrameData.Length > 1)
            {
                if (!useBOM)
                {
                    Data = enc.GetString(FrameData, 1, FrameData.Length - 1);
                }
                else
                {
                    Encoding[] UnicodeEncodings = { Encoding.BigEndianUnicode, Encoding.Unicode, Encoding.UTF8 };
                    for (int i = 0; enc == Encoding.ASCII && i < UnicodeEncodings.Length; ++i)
                    {
                        bool PreambleEqual = true;
                        byte[] Preamble = UnicodeEncodings[i].GetPreamble();

                        for (int j = 0; PreambleEqual && j < Preamble.Length; ++j)
                        {
                            PreambleEqual = Preamble[j] == FrameData[j + 1];
                        }

                        if (PreambleEqual)
                        {
                            enc = UnicodeEncodings[i];
                        }
                    }

                    Data = enc.GetString(FrameData, 1 + enc.GetPreamble().Length, FrameData.Length - (1 + enc.GetPreamble().Length));
                }
            }
            else
            {
                //No Frame Data, so blank.
                Console.WriteLine("The Frame Above Had No Data");
                Data = "";
            }
        }
        else if (FrameName == "TXXX")
        {
            //TXXX Frame Goes Here (TODO)
        }

        //W-Type Frame (TODO: Needs more testing)
        if (FrameName.ToCharArray()[0] == 'W' && FrameName != "WXXX")
        {
            Encoding enc = Encoding.ASCII;
            Data = enc.GetString(FrameData, 0, FrameData.Length);
        }
        else if (FrameName == "WXXX")
        {
            //WXXX is always ISO-8859-1 Encoded
            Encoding enc = Encoding.GetEncoding("ISO-8859-1");
            Data = enc.GetString(FrameData, 0, FrameData.Length);
        }
    }
}

public struct ID3v2CustomFrame
{
    public string Description;
    public string Value;
}

public class ID3v1 : ID3
{
    public ID3v1()
    {
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
                Title += (char)bytHeaderTAG[i];
            }

            //Artist
            for (int i = 33; i <= 62; ++i)
            {
                Artist += (char)bytHeaderTAG[i];
            }

            //Album
            for (int i = 63; i <= 92; ++i)
            {
                Album += (char)bytHeaderTAG[i];
            }

            //Year
            for (int i = 93; i <= 96; ++i)
            {
                Year += (char)bytHeaderTAG[i];
            }

            //Comment
            for (int i = 97; i <= 125; ++i)
            {
                Comment += (char)bytHeaderTAG[i];
            }

            //Track or End of Comment
            if ((bytHeaderTAG[125] == 0x00 || bytHeaderTAG[125] == 0x20) && bytHeaderTAG[126] != 0x00)
            {
                //ID3v1.1
                Track = (int)bytHeaderTAG[126];
            }
            else
            {
                //ID3v1.0
                Comment += (char)bytHeaderTAG[126];
            }

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