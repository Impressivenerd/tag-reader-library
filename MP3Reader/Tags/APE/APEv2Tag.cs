using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using digitalBrink.MP3Reader.Tags.ID3.V1;
using digitalBrink.MP3Reader.Tags.Lyrics3;
using digitalBrink.MP3Reader.Tags.ID3;

namespace digitalBrink.MP3Reader.Tags.APE
{
    public class APEv2Tag : Tag
    {
        /// <summary>
        /// Tag exists AFTER the audio data.
        /// </summary>
        public bool Appended;

        //Flags
        public bool ReadOnly;
        public int InformationTypeIndex;

        private byte[] RawHeader;
        private byte[] RawFooter;

        public APEv2Tag(FileStream fs, ID3v1Tag id3v1, Lyrics3Tag lyrics3, bool afterAudioData)
        {
            Appended = false;
            Exists = false;
            Valid = false;

            long lngOffset = 32; //Size of footer tag of APEv2
            if (id3v1.Exists)
            {
                lngOffset += id3v1.TagSize; //128 Bytes for ID3v1
            }
            if (lyrics3.Exists)
            {
                lngOffset += lyrics3.TagSize;
            }

            RawFooter = new byte[32];
            fs.Position = fs.Length - lngOffset;
            fs.Read(RawFooter, 0, 32);

            if (Encoding.ASCII.GetString(RawFooter, 0, 8) == "APETAGEX")
            {
                Exists = true;
                Appended = true;

                //Version
                uint v = ID3Base.expand(RawFooter[11], RawFooter[10], RawFooter[9], RawFooter[8]);

                TagSize = ID3Base.expand(RawFooter[15], RawFooter[14], RawFooter[13], RawFooter[12]);

                uint numOfItems = ID3Base.expand(RawFooter[19], RawFooter[18], RawFooter[17], RawFooter[16]);

                //Detect Header
                fs.Position = fs.Length - (lngOffset + TagSize);
                RawHeader = new byte[32];
                fs.Read(RawHeader, 0, 32);
                if (Encoding.ASCII.GetString(RawHeader, 0, 8) == "APETAGEX")
                {
                    //WERE GOOD
                    ProcessFlags();
                }
            }
        }

        private void ProcessFlags()
        {
            //Bit 0: 1 = Read Only, 0 = Read/Write
            ReadOnly = (RawHeader[31] & 0x01) == 0x01;

            /**
             * Bit 1-2: 
             * 0: Item contains text information coded in UTF-8
             * 1: Item contains binary information*
             * 2: Item is a locator of external stored information**
             * 3: reserved 
             **/

            /**
             *  * Binary information: Information which should not be edited by a text editor, because
             * * Information is not a text.
             * * Contains control characters 
             * * Contains internal restrictions which can't be handled by a normal text editor
             * * Can't be easily interpreted by humans. 
             *  ** Allowed formats:
             * * http://host/directory/filename.ext
             * * ftp://host/directory/filename.ext
             * * filename.ext
             * * /directory/filename.ext
             * * DRIVE:/directory/filename.ext 
             * Note: Locators are also UTF-8 encoded. This can especially occur when filenames are encoded. 
             **/
            InformationTypeIndex = (int)((RawHeader[31] >> 1) & 0x03);

            /**
             * Bit 3-28: Undefined, must be zero 
             **/

            /**
             * Bit 29: 0 = This is the footer, not the header, 1 = This is the header, not the footer
             **/

            /**
             * Bit 30: 0 = Tag contains a footer, 1= Tag contains no footer 
             **/

            /**
             * Bit 31: 0 = Tag contains no header, 1 = Tag contains a header 
             **/
        }

        public override byte[] ToByte()
        {
            //Setup Encoding Here For Write Method
            return new byte[0];
        }
    }
}
