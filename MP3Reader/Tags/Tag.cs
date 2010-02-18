using System;
using System.Collections.Generic;
using System.Text;

namespace digitalBrink.MP3Reader.Tags
{
    public abstract class Tag
    {
        /// <summary>
        /// Fully Inclusive Tag Size
        /// </summary>
        public uint TagSize = 0;
        /// <summary>
        /// Internally Reported TagSize
        /// </summary>
        protected uint _ReportedTagSize = 0;

        public int MajorVersion = 0;
        public int MinorVersion = 0;
        public long Position = 0;
        public bool Exists = false;
        public bool Valid = true;

        /// <summary>
        /// Is this tag enabled? (used in MP3 Write() method)
        /// </summary>
        public bool Enabled = true;

        public abstract byte[] ToByte();
    }
}
