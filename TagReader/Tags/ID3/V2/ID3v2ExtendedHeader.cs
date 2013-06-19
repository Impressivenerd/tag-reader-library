using System;
using System.Collections.Generic;
using System.Text;

namespace digitalBrink.TagReader.Tags.ID3.V2
{
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
                    PaddingSize = ID3Base.expand(RawData[6], RawData[7], RawData[8], RawData[9]);

                    if (CRCPresent)
                    {
                        CRCData = ID3Base.expand(RawData[10], RawData[11], RawData[12], RawData[13]);
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

                        CRCData = ID3Base.syncsafe(RawData[DataStartingPoint],
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
                        switch (t)
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
}
