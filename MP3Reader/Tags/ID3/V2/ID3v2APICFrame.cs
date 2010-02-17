using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace digitalBrink.MP3Reader.Tags.ID3.V2
{
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
}
