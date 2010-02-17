using System;
using System.Collections.Generic;
using System.Text;
using digitalBrink.MP3Reader.Tags.ID3.V1;
using digitalBrink.MP3Reader.Tags.ID3.V2;
using digitalBrink.MP3Reader.Tags.Lyrics3;
using digitalBrink.MP3Reader.Tags.APE;
using System.IO;

namespace digitalBrink.MP3Reader.MPEG
{
    public class MP3
    {
        //Max Range - 16384
        //Min Frame Size - 24 (MPEG2, LayerIII, 8kbps, 24kHz => Framesize = 24 Bytes)

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
        private int intVFrames
        {
            get
            {
                if (vbrFrameInfo != null)
                {
                    return vbrFrameInfo.intVFrames;
                }
                else
                {
                    return -1;
                }
            }
        }

        //Jeff
        private MPEGAudioFrame firstFrame;
        private MPEGAudioFrame lastFrame;
        private VBRHeader vbrFrameInfo;

        public bool Valid;

        //private bool id3v1; //Temporarily to determine if id3v1 is present
        //private bool id3v2; //Temporarily to determine if id3v2 is present

        public ID3v1Tag id3v1;
        public ID3v2Tag id3v2;
        public Lyrics3Tag lyrics3;
        public APEv2Tag apev2;

        public MP3(string FileName)
        {
            Console.WriteLine(FileName);
            FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
            Valid = false;

            // Set the filename not including the path information
            strFileName = @fs.Name;
            char[] chrSeparators = new char[] { '\\', '/' };
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
            id3v1 = new ID3v1Tag(fs);

            //Read if id3v2 exists
            id3v2 = new ID3v2Tag(fs);

            //Read if lyrics3 exists
            lyrics3 = new Lyrics3Tag(fs, id3v1.Exists);

            //Read if APEv2 exists
            apev2 = new APEv2Tag(fs, id3v1, lyrics3, true);

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

            fs.Position = intPos;
            fs.Read(bytHeader, 0, 4);
            MPEGAudioFrame temp = new MPEGAudioFrame(bytHeader, 0);
            if (temp.Valid == false)
            {
                if (Encoding.ASCII.GetString(bytHeader, 0, 3) == "ID3")
                {
                    //Another ID3v2 Tag?
                    Console.WriteLine("Another ID3v2 tag was found after the initial one");
                    Console.WriteLine();
                }
                else if (Encoding.ASCII.GetString(bytHeader, 0, 3) == "APE")
                {
                    //APETAGEX has appeared before audio data
                    Console.WriteLine("APEv2 Tag Found after ID3v2 tag");
                    Console.WriteLine();
                }
                else
                {
                    //Unknown - Somthing is here that is not supposed to be
                    Console.WriteLine("Garbage found after ID3v2 tag");
                    Console.WriteLine();
                }
            }

            //Search Backwards (Max Search 16384)
            int intPosPrev = intPos;
            int maxPos = intPos - 16384;
            if (maxPos < 0) maxPos = 0;
            do
            {
                fs.Position = intPos;
                fs.Read(bytHeader, 0, 4);

                firstFrame = new MPEGAudioFrame(bytHeader, intPos);
                if (firstFrame.Valid)
                {
                    nextFrame = intPos + firstFrame.FrameSize;
                    fs.Position = nextFrame;
                    bytHeader = new byte[4]; //Reset bytHeader array
                    fs.Read(bytHeader, 0, 4);
                    MPEGAudioFrame SecondHeader = new MPEGAudioFrame(bytHeader, nextFrame);
                    if (SecondHeader.Valid)
                    {
                        Valid = true;
                        break;
                    }
                    else
                    {
                        //The next frame did not appear valid - reset stream position
                        fs.Position = intPos;
                    }
                }

                intPos--;

            } while (!Valid && (fs.Position >= maxPos) && intPos >= 0);

            if (!Valid) intPos = intPosPrev;

            //Search Forwards
            do
            {
                fs.Position = intPos;
                fs.Read(bytHeader, 0, 4);

                firstFrame = new MPEGAudioFrame(bytHeader, intPos);
                if (firstFrame.Valid)
                {
                    nextFrame = intPos + firstFrame.FrameSize;
                    fs.Position = nextFrame;
                    bytHeader = new byte[4]; //Reset bytHeader array
                    fs.Read(bytHeader, 0, 4);
                    MPEGAudioFrame SecondHeader = new MPEGAudioFrame(bytHeader, nextFrame);
                    if (SecondHeader.Valid)
                    {
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
            if (Valid && (fs.Position != fs.Length))
            {
                intPos += 4; //Bypass the 4 byte header

                //The following is retrieved from XING SDK //http://www.mp3-tech.org/programmer/decoding.html
                if (firstFrame.getVersion() == 1.0)         //MPEG Version 1
                {
                    if (firstFrame.ChannelModeIndex == 3)   //Single Channel (Mono)
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
                    if (firstFrame.ChannelModeIndex == 3)   //Single Channel (Mono)
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
                //fs.Read(bytVBitRate,0,12);
                //bVBR = LoadVBRHeader(bytVBitRate);
                vbrFrameInfo = new VBRHeader(fs, (long)intPos);

                // Find the last Audio Frame of the MP3
                findLastFrame(fs);

                // Once the file's read in, then assign the properties of the file to the public variables
                intBitRate = getBitrate();
                intFrequency = firstFrame.getFrequency();
                strMode = firstFrame.getChannelMode();
                intLength = getLengthInSeconds();
                strLengthFormatted = getFormattedLength();

                fs.Close();
            }
        }

        private void findLastFrame(FileStream fs)
        {
            long lngOffset = lngFileSize;

            if (apev2.Exists && apev2.Appended)
            {
                lngOffset -= apev2.TagSize;
            }
            if (lyrics3.Exists)
            {
                lngOffset -= lyrics3.TagSize;
            }
            if (id3v1.Exists)
            {
                lngOffset -= id3v1.TagSize;
            }

            long originalPos = lngOffset;

            fs.Position = lngOffset;

            bool forward = true;
            bool valid = false;

            long nextFrame = 0;

            byte[] tempHeader = new byte[4];

            lngOffset = originalPos;

            MPEGAudioFrame FirstHeader;

            if (!valid)
            {
                while (!valid && (fs.Position >= 0) && (lngOffset - originalPos < 16384))
                {
                    fs.Position = lngOffset;
                    fs.Read(tempHeader, 0, 4);

                    FirstHeader = new MPEGAudioFrame(tempHeader, lngOffset);
                    if (FirstHeader.Valid)
                    {
                        nextFrame = lngOffset + FirstHeader.FrameSize;
                        fs.Position = nextFrame;
                        tempHeader = new Byte[4]; //Reset tempHeader
                        fs.Read(tempHeader, 0, 4);
                        MPEGAudioFrame SecondHeader = new MPEGAudioFrame(tempHeader, nextFrame);
                        if (SecondHeader.Valid)
                        {
                            //posFirstAudioFrame = intPos;
                            valid = true;
                            break;
                        }
                        else
                        {
                            lastFrame = FirstHeader;
                            valid = true;
                            break;
                        }
                    }

                    lngOffset--;
                }
            }

            /*while (!valid && (fs.Position <= fs.Length))
            {
                fs.Position = iPos;
                fs.Read(tempHeader, 0, 4);

                Header = new MPEGAudioFrame(tempHeader);
                if (Header.Valid)
                {
                    nextFrame = iPos + Header.Size;
                    fs.Position = nextFrame;
                    fs.Read(tempHeader, 0, 4);
                    MPEGAudioFrame SecondHeader = new MPEGAudioFrame(tempHeader);
                    if (SecondHeader.Valid)
                    {
                        //posFirstAudioFrame = intPos;
                        valid = true;
                        break;
                    }
                    else
                    {
                        //The next frame did not appear valid - reset stream position
                        fs.Position = iPos;
                    }
                }

                iPos++;
            }*/
        }

        /*private bool LoadVBRHeader(byte[] inputheader)
        {
            // If it's a variable bitrate MP3, the first 4 bytes will read 'Xing'
            // since they're the ones who added variable bitrate-edness to MP3s
            string HeaderType = Encoding.ASCII.GetString(inputheader, 0, 4);
            if(HeaderType == "Xing" || HeaderType == "Info") //(char)inputheader[0] == 'X' && (char)inputheader[1] == 'i' && (char)inputheader[2] == 'n' && (char)inputheader[3] == 'g')
            {
                int flags = (int)ID3.expand(inputheader[4], inputheader[5], inputheader[6], inputheader[7]);//(int)(((inputheader[4] & 255) << 24) | ((inputheader[5] & 255) << 16) | ((inputheader[6] & 255) <<  8) | ((inputheader[7] & 255)));
            
            
                //Flags:
                //Frames
                //Bytes
                //TOC
            
                bool framesFlag = ((flags & 0x01) == 0x01);     // total bit stream frames from Xing header data
                bool bytesFlag = ((flags & 0x02) == 0x02);      // total bit stream bytes from Xing header data
                bool tocFlag = ((flags & 0x04) == 0x04);
                bool vbrScaleFlag = ((flags & 0x08) == 0x08);   // encoded vbr scale from Xing header data
            
                if(framesFlag)
                {
                    intVFrames = (int)ID3.expand(inputheader[8], inputheader[9], inputheader[10], inputheader[11]);//(int)(((inputheader[8] & 255) << 24) | ((inputheader[9] & 255) << 16) | ((inputheader[10] & 255) <<  8) | ((inputheader[11] & 255)));
                }
                else
                {
                    intVFrames = -1;
                }

                if (HeaderType == "Xing")
                {
                    //VBR Indeed
                    return true;
                }
                else
                {
                    //Header is 'Info' which means CBR
                    return false;
                }
            }
            return false;
        } */

        //Jeff
        public bool IsVBR()
        {
            return vbrFrameInfo.bVBR;
        }

        private int getBitrate()
        {
            // If the file has a variable bitrate, then we return an integer average bitrate,
            // otherwise, we use a lookup table to return the bitrate
            if (IsVBR())
            {
                //double medFrameSize = (double)lngFileSize / (double)getNumberOfFrames();
                double medFrameSize = (double)getAudioFileSize() / (double)getNumberOfFrames();
                return (int)((medFrameSize * (double)firstFrame.getFrequency()) / (1000.0 * ((firstFrame.LayerIndex == 3) ? 12.0 : 144.0)));
            }
            else
            {
                return firstFrame.getBitrate();
            }
        }

        private int getLengthInSeconds()
        {
            // "intKilBitFileSize" made by dividing by 1000 in order to match the "Kilobits/second"
            long intKiloBitFileSize = (long)((8 * getAudioFileSize()) / 1000);
            return (int)Math.Round(intKiloBitFileSize / (float)getBitrate());
        }

        /// <summary>
        /// Retrieve the filesize (in bytes) without any of the tags.
        /// </summary>
        /// <returns></returns>
        private long getAudioFileSize()
        {
            if (firstFrame.Valid && lastFrame.Valid)
            {
                return (lastFrame.Position + lastFrame.FrameSize) - firstFrame.Position;
            }
            else
            {
                //Less accurate method
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

                if (lyrics3.Exists)
                {
                    curFileSize -= (long)lyrics3.TagSize;
                }

                if (apev2.Exists)
                {
                    curFileSize -= (long)apev2.TagSize;
                }

                return curFileSize;
            }
        }

        /// <summary>
        /// Output the length of the song in HH:MM:SS format.
        /// </summary>
        /// <returns>The formatted length (in HH:MM:SS).</returns>
        private string getFormattedLength()
        {
            // Complete number of seconds
            int s = getLengthInSeconds();

            // Seconds to display
            int ss = s % 60;

            // Complete number of minutes
            int m = (s - ss) / 60;

            // Minutes to display
            int mm = m % 60;

            // Complete number of hours
            int h = (m - mm) / 60;

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
            if (!IsVBR())
            {
                double medFrameSize = (double)(((firstFrame.LayerIndex == 3) ? 12 : 144) * ((1000.0 * (float)firstFrame.getBitrate()) / (float)firstFrame.getFrequency()));
                return (int)(getAudioFileSize() / medFrameSize);
                //return (int)(lngFileSize / medFrameSize);
            }
            else
            {
                return intVFrames;
            }
        }

        public void Save(string FileName)
        {
            DateTime start = DateTime.Now;

            FileStream file = File.OpenRead(this.strFileName);

            FileStream nFS = File.Create("TEST OUTPUT.mp3");

            file.Position = firstFrame.Position;

            int b;
            long lastPos = lastFrame.Position + lastFrame.FrameSize;
            long curPos = file.Position;
            /*while ((b = file.ReadByte()) > -1)
            {
                curPos++;
                nFS.WriteByte((byte)b);
                if (curPos >= lastPos)
                {
                    break;
                }
            }
            nFS.Flush();*/

            /*byte[] testread = new byte[(lastFrame.Position + lastFrame.FrameSize) - firstFrame.Position];
            file.Read(testread, 0, testread.Length);
            nFS.Write(testread, 0, testread.Length);
            nFS.Flush();*/

            /**
             * If filesize > MAX_ALLOWED_LENGTH then
             *   Copy byte by byte
             * else
             *   Copy entire MP3 into memory and write out
             **/

            if (id3v1.Exists)
            {
                nFS.Write(id3v1.ToByte(), 0, id3v1.ToByte().Length);
            }
            nFS.Flush();

            Console.WriteLine("[BEGIN WRITE]");
            Console.WriteLine("Writing ID3v2 Tag (if enabled).");
            Console.WriteLine("Writing MPEG Audio");
            Console.WriteLine("Writing APE Tag (if enabled).");
            Console.WriteLine("Writing Lyrics3 Tag (if enbaled & ID3v1 enabled).");
            Console.WriteLine("Writing ID3v1 Tag (if enabled).");
            Console.WriteLine("[END WRITE]");
            nFS.Close();

            TimeSpan tTime = DateTime.Now - start;
            Console.WriteLine("SAVING TOOK: " + tTime.TotalMilliseconds);
        }

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
                    String.Format("Size of the MP3: {0:##.00} MB [{1}]", (this.lngFileSize / 1024.0 / 1024.0), this.lngFileSize) + "\n" +
                    String.Format("Size of the MP3 w/o tags: {0:##.00} MB [{1}]", (this.getAudioFileSize() / 1024.0 / 1024.0), this.getAudioFileSize()) + "\n" +
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

}
