using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using digitalBrink.TagReader.Tags.ID3;

namespace digitalBrink.TagReader.MPEG
{
    public class VBRHeader
    {
        //VBRHeader Properties
        public bool bVBR = false;
        public bool Exists = false;
        public string VBRType = "";

        //Flags
        bool framesFlag = false;
        bool bytesFlag = false;
        bool tocFlag = false;
        bool vbrScaleFlag = false;

        public byte[] TOC;
        public int file_bytes;

        public int intVFrames;

        public int HeaderSize;

        public long Position;

        public VBRHeader(FileStream fs, long startOffset)
        {
            fs.Position = startOffset;
            byte[] inputheader = new byte[8]; //4 for Xing or Info, 4 for flags
            fs.Read(inputheader, 0, 8);

            // If it's a variable bitrate MP3, the first 4 bytes will read 'Xing'
            // since they're the ones who added variable bitrate-edness to MP3s
            string HeaderType = Encoding.ASCII.GetString(inputheader, 0, 4);
            if (HeaderType == "Xing" || HeaderType == "Info") //(char)inputheader[0] == 'X' && (char)inputheader[1] == 'i' && (char)inputheader[2] == 'n' && (char)inputheader[3] == 'g')
            {
                VBRType = HeaderType;
                Exists = true;
                Position = startOffset;

                int flags = (int)ID3Base.expand(inputheader[4], inputheader[5], inputheader[6], inputheader[7]);//(int)(((inputheader[4] & 255) << 24) | ((inputheader[5] & 255) << 16) | ((inputheader[6] & 255) <<  8) | ((inputheader[7] & 255)));

                /**
                 * Flags:
                 * Frames
                 * Bytes
                 * TOC
                 * Scale
                 */

                framesFlag = ((flags & 0x01) == 0x01);     // total bit stream frames from Xing header data
                bytesFlag = ((flags & 0x02) == 0x02);      // total bit stream bytes from Xing header data
                tocFlag = ((flags & 0x04) == 0x04);
                vbrScaleFlag = ((flags & 0x08) == 0x08);   // encoded vbr scale from Xing header data

                int byteCount = 0;
                if (framesFlag) byteCount += 4;
                if (bytesFlag) byteCount += 4;
                if (tocFlag) byteCount += 100;
                if (vbrScaleFlag) byteCount += 4;

                byte[] inputheaderData = new byte[byteCount];
                fs.Read(inputheaderData, 0, byteCount);


                int pos = 0;
                if (framesFlag)
                {
                    intVFrames = (int)ID3Base.expand(inputheaderData[pos], inputheaderData[pos + 1], inputheaderData[pos + 2], inputheaderData[pos + 3]);//(int)(((inputheader[8] & 255) << 24) | ((inputheader[9] & 255) << 16) | ((inputheader[10] & 255) <<  8) | ((inputheader[11] & 255)));
                    pos += 4;
                }
                else
                {
                    intVFrames = -1;
                }

                if (bytesFlag)
                {
                    file_bytes = (int)ID3Base.expand(inputheaderData[pos], inputheaderData[pos + 1], inputheaderData[pos + 2], inputheaderData[pos + 3]);
                    pos += 4;
                }

                if (tocFlag)
                {
                    TOC = new byte[100];
                    for (int i = 0; i < 100; ++i)
                    {
                        TOC[i] = inputheaderData[pos];
                        pos++;
                    }
                }

                int vbrScale = -1;
                if (vbrScaleFlag)
                {
                    vbrScale = (int)ID3Base.expand(inputheaderData[pos], inputheaderData[pos + 1], inputheaderData[pos + 2], inputheaderData[pos + 3]);
                    pos += 4;
                }

                HeaderSize = pos + 8; //+8 for original 8 bytes ["Xing" + Flags]

                if (HeaderType == "Xing")
                {
                    //VBR Indeed
                    bVBR = true;

                    SeekPoint(100);
                }
                else
                {
                    //Header is 'Info' which means CBR
                    bVBR = false;
                }
            }
        }

        public long SeekPoint(double Percent)
        {
            // interpolate in TOC to get file seek point in bytes
            int iPercent;
            long seekPointInBytes;
            double fa, fb, fx; //interpolation variables


            if (Percent < 0.0) Percent = 0.0;
            if (Percent > 100.0) Percent = 100.0;

            iPercent = (int)Percent;

            if (iPercent > 99) iPercent = 99;

            fa = TOC[iPercent];
            if (iPercent < 99)
            {
                fb = TOC[iPercent + 1];
            }
            else
            {
                fb = 256.0;
            }

            //Interpolation Algorithm [Interpoloate Access Position]
            fx = fa + (fb - fa) * (Percent - iPercent);

            seekPointInBytes = (long)((1.0 / 256.0) * fx * file_bytes);


            return seekPointInBytes;
        }
    }
}
