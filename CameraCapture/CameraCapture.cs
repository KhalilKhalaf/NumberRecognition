using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace CameraCapture
{
    public partial class CameraCapture : Form
    {
        // Declaring global variables
        private Capture capture;  // Takes images from camera as image frames
        private bool captureInProgress;
        int h_min = 0, h_max = 0;
        int s_min = 0, s_max = 0;
        int v_min = 0, v_max = 0;
        List<int[]> numbers;

        //For Console Display Test
        [DllImport("kernel32", SetLastError = true)]
        static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32", SetLastError = true)]
        static extern bool FreeConsole();
        [DllImport("kernel32", SetLastError = true)]
        static extern bool AllocConsole();

        public CameraCapture()
        {
            InitializeComponent();
        }

        
        private void ProcessFrame(object sender, EventArgs arg)
        {
            Image<Bgr, Byte> frame = capture.QueryFrame().Resize(CamImageBox.Width, CamImageBox.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            // ImageFrame = ImageFrame.ThresholdBinary(new Gray(150), new Gray(255));
            
            // Convert the image to hsv
            Image<Hsv, Byte> hsvFrame = frame.Convert<Hsv, Byte>(); 

            Image<Gray, Byte>[] channels = hsvFrame.Split(); // split the hsv into components
            Image<Gray, Byte> imgHue = channels[0];
            Image<Gray, Byte> imgSat = channels[1];
            Image<Gray, Byte> imgVal = channels[2];

            // Link to trackbars and filter
            Image<Gray, Byte> hueFilter = imgHue.InRange(new Gray(h_min), new Gray(h_max));
            Image<Gray, Byte> satFilter = imgSat.InRange(new Gray(s_min), new Gray(s_max));
            Image<Gray, Byte> valFilter = imgVal.InRange(new Gray(v_min), new Gray(v_max));

            // Sum the images into one image
            Image<Gray, Byte> combined = hueFilter.And(satFilter).And(valFilter).SmoothMedian(5);

            // Display
            CamImageBox.Image = frame;
            imageBox3.Image = hueFilter.Resize(imageBox3.Width, imageBox3.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            imageBox4.Image = satFilter.Resize(imageBox4.Width, imageBox4.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            imageBox5.Image = valFilter.Resize(imageBox5.Width, imageBox4.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            imageBox2.Image = combined.Resize(imageBox2.Width, imageBox2.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            // Find the contour with the largest area
            double max_area = 0;
            Rectangle max_bounding_box = new Rectangle();
            for(Contour<Point> contours = combined.FindContours(); contours != null; contours = contours.HNext)
            {
                if (contours.Area > max_area)
                {
                    max_area = contours.Area;
                    max_bounding_box = contours.BoundingRectangle;
                }
            }

            // Draw a bounding box on it
            frame.Draw(max_bounding_box, new Bgr(Color.Red), 4);
            combined.ROI = max_bounding_box;

            // Display
            imageBox1.Image = combined.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            //imageBox1.Image = frame.Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            Image<Gray, Byte> TempImg; // temp image to get the black pixels
            TempImg = combined; // assign the captured image
            TempImg = TempImg.Resize(3, 5, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            // Recognize digit
            List<int> MyList;
            MyList = new List<int>();
            int k = 0;
            
            // Get light pixels
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (TempImg.Data[i, j, 0] == 255)
                    {
                        MyList.Add(k);
                    }
                    k++;
                }
            }

            // Convert to an array of integers 
            int[] results = MyList.ToArray<int>();

            // Display Test
            //AllocConsole();
            //for (int i = 0; i < results.Length; i++)
            //    Console.Write(results[i] + " ");
            //Console.WriteLine();

            // Check match sequence in List and get index if match (index is the digit 0->9)
            k = numbers.FindIndex(collection => collection.SequenceEqual(results));
            
            // Display recognized number
            this.label1.Text = k.ToString();
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            #region if capture is not created, create it now
            if (capture == null)
            {
                try
                {
                    capture = new Capture();
                }
                catch (NullReferenceException excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }
            #endregion

            if (capture != null)
            {
                if (captureInProgress)
                {  //if camera is getting frames then stop the capture and set button Text
                    // "Start" for resuming capture
                    btnStart.Text = "Start!"; //
                    Application.Idle -= ProcessFrame;
                }
                else
                {
                    //if camera is NOT getting frames then start the capture and set button
                    // Text to "Stop" for pausing capture
                    btnStart.Text = "Stop";
                    Application.Idle += ProcessFrame;
                }

                captureInProgress = !captureInProgress;
            }
        }


        private void ReleaseData()
        {
            if (capture != null)
                capture.Dispose();
        }

        private void CameraCapture_Load(object sender, EventArgs e)
        {
            // initialize list of pixel sequence for detecting integers
            numbers = new List<int[]>();
            numbers.Add(new int[] { 0, 1, 2, 3, 5, 6, 8, 9, 11, 12, 13, 14 });      // 0
            numbers.Add(new int[] { 0, 1, 4, 7, 10, 13 });                          // 1
            numbers.Add(new int[] { 0, 1, 2, 5, 6, 7, 8, 9, 12, 13, 14 });          // 2
            numbers.Add(new int[] { 0, 1, 2, 5, 6, 7, 8, 11, 12, 13, 14 });         // 3
            numbers.Add(new int[] { 0, 2, 3, 5, 6, 7,8, 11, 14 });                // 4
            numbers.Add(new int[] { 0, 1, 2, 3, 6, 7, 8, 11, 12, 13, 14 });         // 5
            numbers.Add(new int[] { 0, 1, 2, 3, 6, 7, 8, 9, 11, 12, 13, 14 });      // 6
            numbers.Add(new int[] { 0, 1, 2, 5, 8, 11, 14 });                       // 7
            numbers.Add(new int[] { 0, 1, 2, 3, 5, 6, 7, 8, 9, 11, 12, 13, 14 });   // 8
            numbers.Add(new int[] { 0, 1, 2, 3, 5, 6, 7, 8, 11, 14 });              // 9
        }
        private void CamImageBox_Click(object sender, EventArgs e)
        {

        }

        private void trackBar1_Scroll(object sender, EventArgs e) // Hue max
        {
            if (trackBar2.Value > trackBar1.Value)
            {
                h_min = Math.Min(179, trackBar1.Value - 1);
                trackBar2.Value = h_min;
                h_min_lbl.Text = h_min.ToString();
            }
            h_max = trackBar1.Value;
            h_max_lbl.Text = h_max.ToString();
        }
        private void trackBar2_Scroll(object sender, EventArgs e) // Hue min
        {
            if (trackBar2.Value > trackBar1.Value)
            {
                h_max = Math.Min(179, trackBar2.Value + 1);
                trackBar1.Value = h_max;
                h_max_lbl.Text = h_max.ToString();
            }
            h_min = trackBar2.Value;
            h_min_lbl.Text = h_min.ToString();
        }
        private void trackBar3_Scroll(object sender, EventArgs e) // Sat max
        {
            if (trackBar4.Value > trackBar3.Value)
            {
                s_min = Math.Min(254, trackBar3.Value - 1);
                trackBar4.Value = s_min;
                s_min_lbl.Text = s_min.ToString();
            }
            s_max = trackBar3.Value;
            s_max_lbl.Text = s_max.ToString();
        }
        private void trackBar4_Scroll(object sender, EventArgs e) // Sat min
        {
            if (trackBar4.Value > trackBar3.Value)
            {
                s_max = Math.Min(254, trackBar4.Value + 1);
                trackBar3.Value = s_max;
                s_max_lbl.Text = s_max.ToString();
            }
            s_min = trackBar4.Value;
            s_min_lbl.Text = s_min.ToString();
        }
        private void trackBar5_Scroll(object sender, EventArgs e) // Val max
        {
            if (trackBar6.Value > trackBar5.Value)
            {
                v_min = Math.Min(254, trackBar5.Value - 1);
                trackBar6.Value = v_min;
                v_min_lbl.Text = v_min.ToString();
            }
            v_max = trackBar5.Value;
            v_max_lbl.Text = v_max.ToString();
        }
        private void trackBar6_Scroll(object sender, EventArgs e) // Val min
        {
            if (trackBar6.Value > trackBar5.Value)
            {
                v_max = Math.Min(254, trackBar6.Value + 1);
                trackBar5.Value = v_max;
                v_max_lbl.Text = v_max.ToString();
            }
            v_min = trackBar6.Value;
            v_min_lbl.Text = v_min.ToString();
        }
    }
}



