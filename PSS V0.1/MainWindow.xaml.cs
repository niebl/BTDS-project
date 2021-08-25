using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Ink;
using System.Drawing;
//Project Libraries
using Microsoft.Kinect;
using System.Windows.Forms;

namespace PSS_V0._1
{
    public enum DisplayFrameType
    {
        Infrared,
        Color,
        Depth
    }


    public partial class MainWindow : Window
    {
        // Setup interface option selection
        private const DisplayFrameType DEFAULT_DISPLAYFRAMETYPE = DisplayFrameType.Depth;

        /// Kinect Sensor
        private KinectSensor kinectSensor = null;

        // Object to write the image to show on the interface
        private WriteableBitmap bitmap = null;
        private FrameDescription currentFrameDescription;
        private DisplayFrameType currentDisplayFrameType;
        // Reader to receive the information from the camera
        private MultiSourceFrameReader multiSourceFrameReader = null;



        /** Scale the values from 0 to 255 **/

        /// Setup the limits (post processing)of the infrared data that we will render.
        /// Increasing or decreasing this value sets a brightness "wall" either closer or further away.
        private const float InfraredOutputValueMinimum = 0.01f;
        private const float InfraredOutputValueMaximum = 1.0f;
        private const float InfraredSourceValueMaximum = ushort.MaxValue;
        /// Scale of Infrared source data
        private const float InfraredSourceScale = 0.75f;


        /** Scale values for depth input**/
        /// Map depth range to byte range
        private const int MapDepthToByte = 8000 / 256;
        /// Intermediate storage for frame data converted to color
        private byte[] depthPixels = null;

        public StrokeCollection strokes;

        private CoordinateMapper coordinateMapper = null;

        public Boolean wasClicked = false;
        public Boolean strokeSaved = false;

        public double[,] savedStroke { get; set; }

        public MainWindow()
        {

            // Initialize the sensor
            this.kinectSensor = KinectSensor.GetDefault();
            // open the reader for the  frames
            this.multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Infrared | FrameSourceTypes.Color | FrameSourceTypes.Depth);
            // wire handler for frame arrival - This is a defined method
            this.multiSourceFrameReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            // Set up display frame types:
            // get FrameDescription
            // create the bitmap to display
            SetupCurrentDisplay(DEFAULT_DISPLAYFRAMETYPE);



            // get the depth (display) extents
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // open the sensor
            this.kinectSensor.Open();
            InitializeComponent();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public ImageSource ImageSource
        {
            get
            {
                return this.bitmap;
            }
        }

        public FrameDescription CurrentFrameDescription
        {
            get { return this.currentFrameDescription; }
            set
            {
                if (this.currentFrameDescription != value)
                {
                    this.currentFrameDescription = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentFrameDescription"));
                    }
                }
            }
        }

        private void SetupCurrentDisplay(DisplayFrameType newDisplayFrameType)
        {
            currentDisplayFrameType = newDisplayFrameType;
            Console.WriteLine(currentDisplayFrameType);
            switch (currentDisplayFrameType)

            {
                case DisplayFrameType.Infrared:
                    FrameDescription infraredFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;
                    this.CurrentFrameDescription = infraredFrameDescription;
                    // allocate space to put the pixels being  received and converted
                    this.bitmap = new WriteableBitmap(infraredFrameDescription.Width, infraredFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray32Float, null);
                    break;

                case DisplayFrameType.Depth:
                    FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
                    this.CurrentFrameDescription = depthFrameDescription;
                    // allocate space to put the pixels being received and converted
                    this.depthPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height];
                    // create the bitmap to display
                    this.bitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
                    break;

                default:
                    break;
            }
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            switch (currentDisplayFrameType)
            {
                case DisplayFrameType.Infrared:
                    using (InfraredFrame infraredFrame = multiSourceFrame.InfraredFrameReference.AcquireFrame())
                    {
                        ShowInfraredFrame(infraredFrame);
                    }
                    break;

                case DisplayFrameType.Depth:
                    if (wasClicked == false) { 
                        using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                        {
                            ShowDepthFrame(0,0,depthFrame);
                        }
                    }
                    else
                    {
                        using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                        {
                            strokes = ic.Strokes.Clone();
                            
                            // i counts the stylusPoints in the saved Stroke
                            int i = 0;
                            foreach (StylusPoint p in strokes[0].StylusPoints)
                                {
                                    i++;
                                }

                            double[,] q = new double[i, 3];
                            double[,] t = new double[2, 3];
                            int k = 0;
                            foreach (StylusPoint p in strokes[0].StylusPoints)
                                {
                                    q[k, 0] = GetX(p, depthFrame);
                                    q[k, 1] = GetY(p);
                                    q[k, 2] = GetZ(p, depthFrame);


                                    int posy=Convert.ToInt16(p.Y);
                                    int posx = Convert.ToInt16(p.X);

                                    Console.WriteLine($"x is {posx}");
                                    Console.WriteLine($"y is { posy}");

                                    ShowDepthFrame(posx,posy, depthFrame);

                                
                                    k++;
                                }
                            t[0, 0] = q[0, 0];
                            t[0, 1] = q[0, 1];
                            t[0, 2] = q[0, 2];
                            t[1, 0] = q[k - 1, 0];
                            t[1, 1] = q[k - 1, 1];
                            t[1, 2] = q[k - 1, 2];
                            savedStroke = t;
                            wasClicked = false;
                            
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        
        
        private void ShowInfraredFrame(InfraredFrame infraredFrame)
        {

            // InfraredFrame is IDisposable

            if (infraredFrame != null)
            {
                FrameDescription infraredFrameDescription = infraredFrame.FrameDescription;
                /// We are using WPF (Windows Presentation Foundation)
                using (KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
                {
                    // verify data and write the new infrared frame data to the display bitmap
                    if (((infraredFrameDescription.Width * infraredFrameDescription.Height) == (infraredBuffer.Size / infraredFrameDescription.BytesPerPixel)) &&
                        (infraredFrameDescription.Width == this.bitmap.PixelWidth) && (infraredFrameDescription.Height == this.bitmap.PixelHeight))
                    {
                        this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size, infraredFrameDescription);
                    }
                }
            }
        }


        private void ShowDepthFrame(int x, int y, DepthFrame depthFrame)
        {
            if (depthFrame != null)
            {
                FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                // the fastest way to process the body index data is to directly access the underlying buffer
                using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                {
                    // verify data and write the color data to the display bitmap
                    if (((depthFrameDescription.Width * depthFrameDescription.Height) == (depthBuffer.Size / depthFrameDescription.BytesPerPixel)) &&
                        (depthFrameDescription.Width == this.bitmap.PixelWidth) && (depthFrameDescription.Height == this.bitmap.PixelHeight))
                    {
                        // Note: In order to see the full range of depth (including the less reliable far field depth) we are setting maxDepth to the extreme potential depth threshold
                        ushort maxDepth = ushort.MaxValue;
                        this.ProcessDepthFrameData(x, y,depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth, depthFrameDescription);

                    }
                }
            }
        }


        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        private unsafe void ProcessDepthFrameData(int x, int y, IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth, FrameDescription depthFrameDescription)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            this.bitmap.Lock();

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);

            }

            if (x > 0) {
                Console.WriteLine("Calculate Depth");
                Console.WriteLine($"depth is {(frameData[x + (y * 512)])/1000.00 } raw: {(frameData[x + (y * 512)])}");
            }




            this.bitmap.WritePixels(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight), this.depthPixels, this.bitmap.PixelWidth, 0);

            this.bitmap.Unlock();
            FrameDisplayImage.Source = this.bitmap;
        }

        /// Directly accesses the underlying image buffer of the InfraredFrame to create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct access to the native memory pointed to by the infraredFrameData pointer.
        /// Activate "unsafe" in the solution properties > on the left >Build > Check Allow unsafe code
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize, FrameDescription infraredFrameDescription)
        {
            // infrared frame data is a 16 bit value
            ushort* frameData = (ushort*)infraredFrameData;

            // lock the target bitmap
            this.bitmap.Lock();

            // get the pointer to the bitmap's back buffer
            float* backBuffer = (float*)this.bitmap.BackBuffer;

            // process the infrared data
            for (int i = 0; i < (int)(infraredFrameDataSize / infraredFrameDescription.BytesPerPixel); ++i)
            {
                // since we are displaying the image as a normalized grey scale image, we need to convert from
                // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
            }

            // mark the entire bitmap as needing to be drawn
            this.bitmap.AddDirtyRect(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight));

            // unlock the bitmap
            this.bitmap.Unlock();
            FrameDisplayImage.Source = this.bitmap;
        }

        private void Button_Submit(object sender, RoutedEventArgs e)
        {
            strokes = ic.Strokes.Clone();
            if (strokes.Count() > 1)
            {
                //Initialize the variables to pass to the MessageBox.Show method
                string message = "You can only process 1 line, you have entered " + strokes.Count() + " lines";
                string caption = "Error detected in Input";
                MessageBoxButtons button1 = MessageBoxButtons.OK;
                //Display the MessageBox
                var result2 = System.Windows.Forms.MessageBox.Show(message, caption, button1);

            }
            else if (strokes.Count()==0)
            {
                //Initialize the variables to pass to the MessageBox.Show method
                string message = "No line found";
                string caption = "Error detected in Input";
                MessageBoxButtons button1 = MessageBoxButtons.OK;
                //Display the MessageBox
                var result2 = System.Windows.Forms.MessageBox.Show(message, caption, button1);
                
            }
            else if(strokeSaved ==true) 
            { 
            Window1 window = new Window1(savedStroke);
            window.Show();
            this.Close();  
            }
            else {
                //Initialize the variables to pass to the MessageBox.Show method
                string message = "Please Save your line";
                string caption = "Error detected in Input";
                MessageBoxButtons button1 = MessageBoxButtons.OK;
                //Display the MessageBox
                var result2 = System.Windows.Forms.MessageBox.Show(message, caption, button1);
            }
        }
        private void Button_Infrared(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.Infrared);
        }

        private void Button_Depth(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.Depth);
        }
        private void Button_Save(object sender, RoutedEventArgs e)
        {
            strokes = ic.Strokes.Clone();
            if (strokes.Count() > 1)
            {
                //Initialize the variables to pass to the MessageBox.Show method
                string message = "You can only process 1 line, you have entered " + strokes.Count() + " lines";
                string caption = "Error detected in Input";
                MessageBoxButtons button1 = MessageBoxButtons.OK;
                //Display the MessageBox
                var result2 = System.Windows.Forms.MessageBox.Show(message, caption, button1);

            }
            if (strokes.Count() == 0)
            {
                string message = "You have not drawn a line";
                string caption = "Error detected in Input";
                MessageBoxButtons button2 = MessageBoxButtons.OK;
                //Display the MessageBox
                var result1 = System.Windows.Forms.MessageBox.Show(message, caption, button2);

            }
            else
            {
                
                wasClicked = true;
                strokeSaved = true;
            }
        }

        
        private double GetX(StylusPoint sp, DepthFrame depthFrame)
        {
            int depthWidth = depthFrame.FrameDescription.Width;
            int depthHeight = depthFrame.FrameDescription.Height;
            ushort[] depthframeData = new ushort[depthWidth * depthHeight];
            depthFrame.CopyFrameDataToArray(depthframeData);

            CameraSpacePoint[] csp = new CameraSpacePoint[512 * 424];
            this.coordinateMapper.MapDepthFrameToCameraSpace(depthframeData, csp);

            CameraSpacePoint XPosition = csp[(512 * Convert.ToInt16(sp.Y)) + Convert.ToInt16(sp.X)];
            return XPosition.X * -100;
        }
        private double GetY(StylusPoint sp)
        {
            return 424 - sp.Y;
        }
        private double GetZ(StylusPoint sp, DepthFrame depthFrame)
        {
            int depthWidth = depthFrame.FrameDescription.Width;
            int depthHeight = depthFrame.FrameDescription.Height;

            ushort[] depthframeData = new ushort[depthWidth * depthHeight];
            depthFrame.CopyFrameDataToArray(depthframeData);
            CameraSpacePoint[] csp = new CameraSpacePoint[512 * 424];
            this.coordinateMapper.MapDepthFrameToCameraSpace(depthframeData, csp);

            //Depth(Z Position) of specified coordinate
            CameraSpacePoint DepthPosition = csp[(512 * Convert.ToInt16(sp.Y)) + Convert.ToInt16(sp.X)];

            Console.WriteLine($"get z is {DepthPosition}");

            return DepthPosition.Z ;
        }
        

    }
}
