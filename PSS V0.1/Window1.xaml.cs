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
using System.Windows.Shapes;
using System.Drawing;

//Kinect Libraries
using Microsoft.Kinect;

namespace PSS_V0._1
{
    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        /// Kinect Sensor object
        private KinectSensor kinectSensor = null;

        //**// Object to write the image to show on the interface the results of the trajectory
        /// Drawing group for body rendering output
        private DrawingGroup drawingGroup;
        /// Drawing image that we will display
        private DrawingImage imageSource;

        // Reader to receive the information from the camera
        private MultiSourceFrameReader multiSourceFrameReader = null;

        //***************** FOR TRAJECTORIES //

        /// Array for the bodies
        private Body[] bodies = null;
        ulong[] bodies_ids = { 0, 0, 0, 0, 0, 0 };

        
        //Data for each body
        List<System.Windows.Media.Brush> bodyBrushes = new List<System.Windows.Media.Brush>();
        public double dperPixZ = 0;
        public double dperPixX = 0;

        public double[,] linePoints;
        public Window1(double[,] line)
        {
            

            // Initialize the sensor
            this.kinectSensor = KinectSensor.GetDefault();

            // Create the drawing group we'll use for drawing as we have a set of geometries to insert
            this.drawingGroup = new DrawingGroup();
            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Object with the information
            this.DataContext = this;

            // Get body information from the sensor
            this.multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body);
            this.multiSourceFrameReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            //Create the colors to identify each person
            this.bodyIndexColors();
            // open the sensor
            this.kinectSensor.Open();
            //Points from monitoring Line
            this.linePoints = line;

            InitializeComponent();
        }

        // Colors for the positional ellipses
        private void bodyIndexColors()
        {

            this.bodyBrushes.Add(System.Windows.Media.Brushes.Red);
            this.bodyBrushes.Add(System.Windows.Media.Brushes.Black);
            this.bodyBrushes.Add(System.Windows.Media.Brushes.Green);
            this.bodyBrushes.Add(System.Windows.Media.Brushes.Blue);
            this.bodyBrushes.Add(System.Windows.Media.Brushes.Indigo);
            this.bodyBrushes.Add(System.Windows.Media.Brushes.Violet);

        }

        //Create each ellipse (circle) used to show the position of the person in the camera's field of view
        private Ellipse createBody(double coord_x, double coord_y, System.Windows.Media.Brush brush)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Fill = brush;
            ellipse.Width = 15;
            ellipse.Height = 15;
            this.fieldOfView.Children.Add(ellipse);
            Canvas.SetLeft(ellipse, coord_x);
            Console.WriteLine("ellipse x: " + coord_x + " ellipse y: " + coord_y);
            Canvas.SetTop(ellipse, fieldOfView.ActualHeight - coord_y);
            return ellipse;
        }

        private Line createLine(double[,] points)
        {
            Line myLine = new Line();
            dperPixZ = (double)fieldOfView.ActualHeight / 5000;
            myLine.Stroke = System.Windows.Media.Brushes.LightSteelBlue;

            myLine.X1 = points[0, 0];
            myLine.X2 = points[1, 0];
           
            Console.WriteLine($"l1 x: {points[0, 0]} l1 y: {points[0, 2]}");

            myLine.Y1 = fieldOfView.ActualHeight-(fieldOfView.ActualHeight*points[0,2])/4.5;
            myLine.Y2 = fieldOfView.ActualHeight-(fieldOfView.ActualHeight*points[1,2])/4.5;

            Console.WriteLine($"l1 corrected x: {myLine.X1} l1 y: {myLine.Y1}");
            Console.WriteLine($"l2 corrected x: {myLine.X2} l2 y: {myLine.Y2}");
            myLine.HorizontalAlignment = HorizontalAlignment.Left;
            myLine.VerticalAlignment = VerticalAlignment.Center;
            myLine.StrokeThickness = 2;
            this.fieldOfView.Children.Add(myLine);
            return myLine;
        }
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }


        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var bodyFrame = reference.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {

                    //Get the number the bodies in the scene
                    bodies = new Body[bodyFrame.BodyFrameSource.BodyCount];

                    bodyFrame.GetAndRefreshBodyData(bodies);

                    List<Body> tracked_bodies = bodies.Where(body => body.IsTracked == true).ToList();

                    // Here we draw the path travelled during the session with pixel size traces
                    var moves = fieldOfView.Children.OfType<Ellipse>().ToList();
                    foreach (Ellipse ellipse in moves)
                    {
                        ellipse.Width = 1;
                        ellipse.Height = 1;
                    }
                    tst1.Text = Convert.ToString(linePoints[0, 0]);
                    tst2.Text = Convert.ToString(linePoints[0, 2]);
                    tst3.Text = Convert.ToString(linePoints[1, 0]);
                    tst4.Text = Convert.ToString(linePoints[1, 2]);

                    // Create bodies in the scene
                    createLine(linePoints);
                    DrawTracked_Bodies(tracked_bodies);
                    

                }
            }
        }
        //Needs two set points as start and endpoint to draw a line where the monitoring takes place
        
       
        private void DrawTracked_Bodies(List<Body> tracked_bodies)
        {
            for (int last_id = 0; last_id < 6; last_id++)
            {
                if (bodies_ids[last_id] == 0)
                    continue;

                bool is_tracked = false;

                for (int new_id = 0; new_id < tracked_bodies.Count; new_id++)
                {
                    if (tracked_bodies[new_id].TrackingId == bodies_ids[last_id])
                    {
                        is_tracked = true;
                        break;
                    }
                }

                if (!is_tracked)
                {
                    bodies_ids[last_id] = 0;
                }
            }

            // Check if someone new entered the scene
            for (int new_id = 0; new_id < tracked_bodies.Count; new_id++)
            {
                dperPixZ = (double)fieldOfView.ActualHeight / 5000;
                double bodyX = tracked_bodies[new_id].Joints[JointType.SpineMid].Position.X * dperPixZ * 1000 * (-1);
                double bodyZ = tracked_bodies[new_id].Joints[JointType.SpineMid].Position.Z * dperPixZ * 1000;

                ulong current_id = tracked_bodies[new_id].TrackingId;

                // true: if body was previosly tracked
                // false: if its a new body just entered the scene
                bool is_tracked = false;

                // First check previously tracked bodies
                for (int exist_id = 0; exist_id < 6; exist_id++)
                {
                    if (bodies_ids[exist_id] == current_id)
                    {
                        is_tracked = true;
                        createBody(fieldOfView.ActualWidth / 2 + bodyX, bodyZ, bodyBrushes[exist_id]);
                        coord_body.Content = coordinatesFieldofView(tracked_bodies[exist_id]);
                        break;
                    }
                }

                // If not previously tracked, then fill first empty spot in the list of tracking bodies
                if (!is_tracked)
                {
                    for (int fill_id = 0; fill_id < 6; fill_id++)
                    {
                        if (bodies_ids[fill_id] == 0)
                        {
                            bodies_ids[fill_id] = current_id;

                            createBody(fieldOfView.ActualWidth / 2 + bodyX, bodyZ, bodyBrushes[fill_id]);
                            coord_body.Content = coordinatesFieldofView(tracked_bodies[new_id]);

                            break;
                        }
                    }
                }
            }
        }

        private string coordinatesFieldofView(Body current_body)
        {

            //From the Skeleton Joints we use as position the SpineMid coordinates
            // Remember that Z represents the depth and thus from the perspective of a Cartesian plane it represents Y from a top view
            double coord_y = Math.Round(current_body.Joints[JointType.SpineMid].Position.Z, 2);
            // Remember that X represents side to side movement. The center of the camera marks origin (0,0). 
            // As the Kinect is mirrored we multiple by times -1
            double coord_x = Math.Round(current_body.Joints[JointType.SpineMid].Position.X, 2) * (-1);

            return "Body Coordinates: X: " + coord_x + " Y: " + coord_y;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Draw Kinect Vision Area based on the size of our canvas
            int canvasHeight = (int)fieldOfView.ActualHeight;
            int canvasWidth = (int)fieldOfView.ActualWidth;

            PointCollection myPointCollection = new PointCollection();

            //The Kinect has a horizontal field of view opened by 70°, ergo we evaluate a triangle containing one of these angles
            // 35° to each side from the origin

            int x = Convert.ToInt16((canvasHeight * Math.Sin(Math.PI / 180) * 35) + canvasWidth / 2);
            int x1 = Convert.ToInt16(canvasWidth / 2 - (canvasHeight * Math.Sin(Math.PI / 180) * 35));

            // 3 Verticed for the field of view
            myPointCollection.Add(new System.Windows.Point(x, canvasWidth / 2));
            myPointCollection.Add(new System.Windows.Point(x1, canvasWidth / 2));
            myPointCollection.Add(new System.Windows.Point(canvasWidth / 2, canvasHeight));

            //Creating the triangle from the 3 vertices

            Polygon myPolygon = new Polygon();
            myPolygon.Points = myPointCollection;
            myPolygon.Fill = System.Windows.Media.Brushes.Purple;
            myPolygon.Width = canvasWidth;
            myPolygon.Height = canvasHeight;
            myPolygon.Stretch = Stretch.Fill;
            myPolygon.Stroke = System.Windows.Media.Brushes.Purple;
            myPolygon.StrokeThickness = 1;
            myPolygon.Opacity = 0.2;

            //Add the triangle in our canvas
            myGrid.Children.Add(myPolygon);
        }
    }
}