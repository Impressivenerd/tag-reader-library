using System;
using System.Collections.Generic;
using System.Text;

namespace digitalBrink.MP3Reader.MPEG
{
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
        public int FrameSize;

        /// <summary>
        /// Position in the file.
        /// </summary>
        public long Position;

        public MPEGAudioFrame(byte[] Header, long Pos)
        {
            RawHeader = Header;
            Position = Pos;

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
                FrameSize = (12 * (getBitrate() * 1000) / getFrequency() + Padding) * 4;
            }
            else
            {
                FrameSize = 144 * (getBitrate() * 1000) / getFrequency() + Padding;
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
            int[, ,] BitrateTable =
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
}
