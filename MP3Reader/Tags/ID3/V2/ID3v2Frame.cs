using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace digitalBrink.MP3Reader.Tags.ID3.V2
{
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
                        PreambleEqual = Preamble[j] == FrameData[j + start]; //+start position = Skip Text Encoding Byte
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
                NewFrameData = ID3Base.UnSync(FrameData);
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
                        apic.Description = enc.GetString(NewFrameData, BeginDescription + enc.GetPreamble().Length, (DataPosition - (BeginDescription + enc.GetPreamble().Length)));
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
}
