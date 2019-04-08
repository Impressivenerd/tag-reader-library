using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using digitalBrink.TagReader.MPEG;
using digitalBrink.TagReader.Tags.ID3.V2;
using System.IO;

namespace TagTest
{
    public partial class MainForm : Form
    {
        public int iNumOfImages;
        public int iCurImage;
        MP3 curMP3;

        public MainForm()
        {
            InitializeComponent();
        }

        private void dragDropLabel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void dragDropLabel_DragDrop(object sender, DragEventArgs e)
        {
            string Filename = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            resetMP3Data();
            loadMP3(Filename);
        }

        private void resetMP3Data()
        {
            pictureBox1.Image = null;
            btnNextImage.Enabled = false;
            btnPrevImage.Enabled = false;
            curMP3 = null;
        }

        private void loadMP3(string Filename)
        {
            MP3 mp3 = new MP3(Filename);

            dragDropLabel.Text = mp3.ToString();

            //Try to find cover information, if embedded
            try
            {
                string FrameName;
                if (mp3.id3v2.MajorVersion == 2)
                {
                    FrameName = "PIC";
                }
                else
                {
                    FrameName = "APIC";
                }

                iNumOfImages = mp3.id3v2.Frames[FrameName].Count;
                if (iNumOfImages > 0)
                {
                    pictureBox1.Image = ((ID3v2APICFrame)mp3.id3v2.Frames[FrameName][0].Data).Picture;
                    lblImageText.Text = "Image 1 / " + iNumOfImages.ToString() + "\n";
                    lblImageText.Text += "Image Type: " + ((ID3v2APICFrame)mp3.id3v2.Frames[FrameName][0].Data).PictureType();
                    pictureBox1.Refresh();

                    btnPrevImage.Enabled = false;
                    if (iNumOfImages > 1)
                    {
                        btnNextImage.Enabled = true;
                    }
                    iCurImage = 0;
                }
            }
            catch (Exception ex)
            {
            }

            curMP3 = mp3;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            iNumOfImages = 0;
            string[] t = Environment.GetCommandLineArgs();
            if (t.Length > 1)
            {
                loadMP3(t[1]);
            }
        }

        private void btnNextImage_Click(object sender, EventArgs e)
        {
            string FrameName = "APIC";
            if (curMP3.id3v2.MajorVersion == 2)
            {
                FrameName = "PIC";
            }

            if (iCurImage != iNumOfImages - 1)
            {
                iCurImage++;
                pictureBox1.Image = ((ID3v2APICFrame)curMP3.id3v2.Frames[FrameName][iCurImage].Data).Picture;
                lblImageText.Text = "Image " + (iCurImage + 1) + " / " + iNumOfImages.ToString() + "\n";
                lblImageText.Text += "Image Type: " + ((ID3v2APICFrame)curMP3.id3v2.Frames[FrameName][iCurImage].Data).PictureType();
                pictureBox1.Refresh();

                if (iCurImage == iNumOfImages - 1)
                {
                    btnPrevImage.Enabled = true;
                    btnNextImage.Enabled = false;
                }
            }
            else
            {
                btnPrevImage.Enabled = true;
                btnNextImage.Enabled = false;
            }
        }

        private void btnPrevImage_Click(object sender, EventArgs e)
        {
            string FrameName = "APIC";
            if (curMP3.id3v2.MajorVersion == 2)
            {
                FrameName = "PIC";
            }

            if (iCurImage != 0)
            {
                iCurImage--;
                pictureBox1.Image = ((ID3v2APICFrame)curMP3.id3v2.Frames[FrameName][iCurImage].Data).Picture;
                lblImageText.Text = "Image " + (iCurImage + 1) + " / " + iNumOfImages.ToString() + "\n";
                lblImageText.Text += "Image Type: " + ((ID3v2APICFrame)curMP3.id3v2.Frames[FrameName][iCurImage].Data).PictureType();
                pictureBox1.Refresh();

                if (iCurImage == 0)
                {
                    btnPrevImage.Enabled = false;
                    btnNextImage.Enabled = true;
                }
            }
            else
            {
                btnPrevImage.Enabled = false;
                btnNextImage.Enabled = true;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainForm.ActiveForm.Close();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            string Filename = openFileDialog1.FileName;
            resetMP3Data();
            loadMP3(Filename);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                Stream m = openFileDialog1.OpenFile();
            }

        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "TagReader Sample Project\n\nVisit https://github.com/ImpressiveNerd/tag-reader-library for more information.", "About TagReader Sample Project", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}