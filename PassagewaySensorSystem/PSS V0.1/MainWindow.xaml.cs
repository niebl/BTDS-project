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

    public partial class MainWindow : Window
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

        private Tracked_Person mainPerson = new Tracked_Person(0, 0, false);

        private Coordinate outside = new Coordinate(0,0,false);
        private Coordinate inside = new Coordinate(0,0,false);
        private Coordinate thresholdP1 = new Coordinate(0, 0, false);
        private Coordinate thresholdP2 = new Coordinate(0, 0, false);

        private double[,] threshold;

        //Data for each body
        List<System.Windows.Media.Brush> bodyBrushes = new List<System.Windows.Media.Brush>();
        public double dperPixZ = 0;
        public double dperPixX = 0;

        public MainWindow()
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

            InitializeComponent();
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


                    // Create bodies in the scene

                    DrawTracked_Bodies(tracked_bodies);


                }
            }
        }

        class Tracked_Person
        {
            public double x { get; set; }
            public double y { get; set; }
            public bool tracked { get; set; }
            public double nearestNeighbor { get; set; }

            public Tracked_Person(double x, double y, bool tracked)
            {
                this.x = x;
                this.y = y;
                this.tracked = tracked;
            }

        }

        class Coordinate
        {
            public double x { get; set; }
            public double y { get; set; }
            public bool confirmed { get; set; }

            public Coordinate(double x, double y, bool confirmed)
            {
                this.x = x;
                this.y = y;
                this.confirmed = confirmed;
            }

            public void add(Coordinate otherCoord)
            {
                this.x += otherCoord.x;
                this.y += otherCoord.y;
            }

            public void substract(Coordinate otherCoord)
            {
                this.x -= otherCoord.x;
                this.y -= otherCoord.y;
            }

            private double degToRad(double degrees)
            {
                return degrees * Math.PI / 180.0;
            }

            //rotate clockwise around origin 0,0 by 90 °
            public void rotate()
            {
                //swap the two coordinates
                this.x += this.y;
                this.y = this.x - this.y;
                this.x = this.x - this.y;

                this.y = -this.y;


                /*
                 * https://matthew-brett.github.io/teaching/rotation_2d.html
                degrees = degToRad(degrees);

                double newX = Math.Cos(degrees * this.x) - Math.Sin(degrees * this.y);
                double newY = Math.Sin(degrees * this.x) + Math.Cos(degrees * this.y);

                this.x = newX;
                this.y = newY;
                */
            }
        }

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
                mainPerson.tracked = false;

                // First check previously tracked bodies
                for (int exist_id = 0; exist_id < 6; exist_id++)
                {
                    if (bodies_ids[exist_id] == current_id)
                    {
                        is_tracked = true;
                        createBody(fieldOfView.ActualWidth / 2 + bodyX, bodyZ, bodyBrushes[exist_id]);

                        mainPerson.x = bodyX;
                        mainPerson.y = bodyZ;
                        mainPerson.tracked = true;

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

                            mainPerson.x = bodyX;
                            mainPerson.y = bodyZ;
                            mainPerson.tracked = true;

                            break;
                        }
                    }
                }
            }
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
            Canvas.SetTop(ellipse, fieldOfView.ActualHeight - coord_y);
            return ellipse;
        }

        private void Button_Outside(object sender, RoutedEventArgs e)
        {
            if (mainPerson.tracked)
            {
                outside.x = mainPerson.x;
                outside.y = mainPerson.y;
                outside.confirmed = true;
            }
            else
            {
                outside.confirmed = false;
                logConsole("no body to register outside position");
            }

            logConsole("outside");
            logConsole("x: " + outside.x.ToString());
            logConsole("y: " + outside.y.ToString());

            if (inside.confirmed && outside.confirmed)
            {
                calculate_threshold();
                createLine(thresholdP1, thresholdP2);

            }
        }

        private void Button_Inside(object sender, RoutedEventArgs e)
        {
            if (mainPerson.tracked)
            {
                inside.x = mainPerson.x;
                inside.y = mainPerson.y;
                inside.confirmed = true;
            }
            else
            {
                inside.confirmed = false;
                logConsole("no body to register inside position");
            }

            logConsole("inside");
            logConsole("x: " + inside.x.ToString());
            logConsole("y: " + inside.y.ToString());

            if(inside.confirmed && outside.confirmed)
            {
                calculate_threshold();
                createLine(thresholdP1, thresholdP2);
            }
        }

        private void calculate_threshold()
        {
            //so far it only takes the two points and rotates them by 90 degrees clockwise

            //get origin
            Coordinate origin = new Coordinate(
                (outside.x + inside.x) / 2,
                (outside.y + inside.y) / 2,
                true
            );

            //substract origin from coords
            Coordinate newInside = inside;
            newInside.substract(origin);
            Coordinate newOutside = outside;
            newOutside.substract(origin);

            //rotate 90° clockwise
            newInside.rotate();
            newOutside.rotate();

            //add origin to coords
            newInside.add(origin);
            newOutside.add(origin);

            thresholdP1 = newInside;
            thresholdP2 = newOutside;

            logConsole("new threshold");
            logConsole("x1: " + thresholdP1.x.ToString() + "    x2: " + thresholdP2.x.ToString());
            logConsole("y1: " + thresholdP1.y.ToString() + "    y2: " + thresholdP2.y.ToString());
        }

        private void Button_Clear(object sender, RoutedEventArgs e)
        {
            outside.confirmed = false;
            inside.confirmed = false;

            thresholdP1.confirmed = false;
            thresholdP2.confirmed = false;
        }

        private void Button_Submit(object sender, RoutedEventArgs e)
        {

        }

        private Line createLine(Coordinate point1, Coordinate point2)
        {
            Line myLine = new Line();
            myLine.Stroke = System.Windows.Media.Brushes.LightSteelBlue;
            myLine.X1 = fieldOfView.ActualWidth / 2 + point1.x;
            myLine.X2 = fieldOfView.ActualWidth / 2 + point2.x;
            myLine.Y1 = fieldOfView.ActualHeight - point1.y;
            myLine.Y2 = fieldOfView.ActualHeight - point2.y;

            myLine.StrokeThickness = 2;

            this.fieldOfView.Children.Add(myLine);

            return myLine;
        }


        //logs the string to the window console
        private void logConsole(String consoleoOut)
        {
            windowConsole.Text = windowConsole.Text + Environment.NewLine + consoleoOut;
            ConsoleScroller.ScrollToBottom();
        }
    }
}
