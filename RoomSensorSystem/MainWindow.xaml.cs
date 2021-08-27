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

using System.Globalization;
using System.Net.Http;
using Microsoft.Kinect;

namespace RoomSensorSystem
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

        private Post_Interval intervalCounter = new Post_Interval(1000);
        private String post_Url;

        //***************** FOR TRAJECTORIES //

        /// Array for the bodies
        private Body[] bodies = null;
        ulong[] bodies_ids = { 0, 0, 0, 0, 0, 0 };

        private Tracked_Inhabitant[] tracked_Inhabitants = { 
            new Tracked_Inhabitant(0,0, false),
            new Tracked_Inhabitant(0,0, false),
            new Tracked_Inhabitant(0,0, false),
            new Tracked_Inhabitant(0,0, false),
            new Tracked_Inhabitant(0,0, false),
            new Tracked_Inhabitant(0,0, false),
        };


        //Data for each body
        List<Brush> bodyBrushes = new List<Brush>();
        public double dperPixZ = 0;
        public double dperPixX = 0;

        //Http Client Object which will be responsible for sending all the requests
        //https://stackoverflow.com/questions/4015324/how-to-make-an-http-post-web-request
        private static readonly HttpClient client = new HttpClient();

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

        class Tracked_Inhabitant
        {
            public double x { get; set; }
            public double y { get; set; }
            public bool tracked { get; set; }
            public double nearestNeighbor { get; set; }

            public Tracked_Inhabitant(double x, double y, bool tracked)
            {
                this.x = x;
                this.y = y;
                this.tracked = tracked;
                this.nearestNeighbor = double.MaxValue;
            }

            private double calculateDistance(Tracked_Inhabitant them)
            {
                double distance = Math.Sqrt(
                        Math.Pow(them.x-this.x,2) + Math.Pow(them.y-this.y, 2)
                    );

                return distance;
            }

            public double calculateNearestNeighbor(Tracked_Inhabitant[] neighbors)
            {
                double nearestNeighbor = double.MaxValue;

                for(int i = 0; i < 6; i++)
                {   
                    if (neighbors[i].tracked)
                    {
                        double distance = this.calculateDistance(neighbors[i]);
                        if(distance > 0)
                        {
                            nearestNeighbor = distance;
                        }
                    }
                }

                this.nearestNeighbor = nearestNeighbor;
                return nearestNeighbor;
            }

        }

        public class Post_Interval
        {
            public bool active { get; set; }
            public int interval { get; set; } //in milliseconds
            public long lastTick { get; set; }

            public Post_Interval(int interval)
            {
                this.interval = interval;
                this.active = false;
                this.lastTick = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond; //last tick in UNIX milliseconds
            }

        }

        // Colors for the positional ellipses
        private void bodyIndexColors()
        {

            this.bodyBrushes.Add(Brushes.Red);
            this.bodyBrushes.Add(Brushes.Black);
            this.bodyBrushes.Add(Brushes.Green);
            this.bodyBrushes.Add(Brushes.Blue);
            this.bodyBrushes.Add(Brushes.Indigo);
            this.bodyBrushes.Add(Brushes.Violet);

        }

        //Create each ellipse (circle) used to show the position of the person in the camera's field of view
        private Ellipse createBody(double coord_x, double coord_y, Brush brush)
        {
            double view_x = fieldOfView.ActualWidth / 2 + coord_x;
            Ellipse ellipse = new Ellipse();
            ellipse.Fill = brush;
            ellipse.Width = 15;
            ellipse.Height = 15;
            this.fieldOfView.Children.Add(ellipse);
            Canvas.SetLeft(ellipse, view_x);
            Canvas.SetTop(ellipse, fieldOfView.ActualHeight - coord_y);
            return ellipse;
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

                    // Create bodies in the scene
                    DrawTracked_Bodies(tracked_bodies);
                    bodies_To_Inhabitants(tracked_bodies);
                    calculateNearestNeighbors();

                    //Check the timer and see if it is necessary to send a new POST
                    if (intervalCounter.active)
                    {
                        long nextTick = intervalCounter.lastTick + intervalCounter.interval;
                        long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        if (now >= nextTick)
                        {
                            intervalCounter.lastTick = now;

                            DateTime currentTime = DateTime.Now;
                            long currentUnix = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

                            //Send the post
                            var postValues = new Dictionary<string, string>
                            {
                                {"timestamp", currentUnix.ToString() },
                                {"inhabitants", inhabitantsPostString() }
                            };

                            //String logString =
                            //tracked_Inhabitants[0].tracked.ToString() + $" x:{tracked_Inhabitants[0].x},y:{tracked_Inhabitants[0].y}" + ", " +
                            //tracked_Inhabitants[1].tracked.ToString() + $" x:{tracked_Inhabitants[1].x},y:{tracked_Inhabitants[1].y} " + ", .... ";

                            //logConsole(logString);

                            var postContent = new FormUrlEncodedContent(postValues);
                            sendPost(post_Url, postContent);
                        }
                    }

                }
            }
        }

        /**
         * Very quick and dirty function that creates a string which contains the positions of every tracked body in the room.
         * could be a LOT more elegant, but yolo.
         */
        private String inhabitantsPostString()
        {
            String outString = "[";

            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";

            bool trailingComma = false;
            for (int i = 0; i < 6; i++)
            {
                if (tracked_Inhabitants[i].tracked)
                {
                    if (trailingComma) { outString = outString + ","; }
                    String bodyX = tracked_Inhabitants[i].x.ToString(nfi);
                    String bodyY = tracked_Inhabitants[i].y.ToString(nfi);
                    String neighbor = tracked_Inhabitants[i].nearestNeighbor.ToString(nfi);

                    outString = outString + $"{{\"x\":{bodyX},\"y\":{bodyY},\"nearestNeighbor\":{neighbor}}}";

                    trailingComma = true;
                }
            }

            outString = outString + "]";
            return outString;
        }

        /**
         * This function will take the list of tracked bodies that arrives from the Multisourceframe
         * and turns it into a list of Tracked_Inhabitant, which can later be used to send in the
         * POST to the server
        */
        private void bodies_To_Inhabitants(List<Body> tracked_bodies)
        {
            //the loop assigning bodies_ids is not necessary if DrawTracked_Bodies is called before

            for(int inhabIndex = 0; inhabIndex < 6; inhabIndex++)
            {
                tracked_Inhabitants[inhabIndex].tracked = (bodies_ids[inhabIndex] != 0);

                if(tracked_Inhabitants[inhabIndex].tracked) //if the body is currently tracked by the sensor
                {
                    //we search for the body with the corresponding ID
                    Body tracked_body = tracked_bodies.Find(x => x.TrackingId == bodies_ids[inhabIndex]);

                    var current_body = tracked_body.Joints[JointType.SpineMid];

                    //fill the Objects with the correct coordinates
                    tracked_Inhabitants[inhabIndex].x = Math.Round(current_body.Position.Z, 2);
                    tracked_Inhabitants[inhabIndex].y = Math.Round(current_body.Position.X, 2) * (-1);

                }
                else
                {
                    continue;
                }

            }
            
        }

        private void calculateNearestNeighbors()
        {
            for(int i = 0; i < 6; i++)
            {
                tracked_Inhabitants[i].calculateNearestNeighbor(tracked_Inhabitants);
            }
        }

        private async void sendPost(String URL, FormUrlEncodedContent content)
        {
            var response = await client.PostAsync(URL, content);
            //logConsole(await response.Content.ReadAsStringAsync());
        }

        private void DrawTracked_Bodies(List<Body> tracked_bodies)
        {
            //loop assigning bodies_ids
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
                        createBody(bodyX, bodyZ, bodyBrushes[exist_id]);
                        //coord_body.Content = coordinatesFieldofViewReadable(tracked_bodies[exist_id]);

                        //logConsole(coordinatesFieldofViewReadable(tracked_bodies[exist_id]));

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

                            createBody(bodyX, bodyZ, bodyBrushes[fill_id]);
                            //coord_body.Content = coordinatesFieldofViewReadable(tracked_bodies[new_id]);                            

                            break;
                        }
                    }
                }
            }
        }

        private string coordinatesFieldofViewReadable(Body current_body)
        {
            Tracked_Inhabitant inhabitant = coordinatesFieldofView(current_body);
            String returnString = "Body Coordinates: X: " + inhabitant.x + " Y: " + inhabitant.y;

            //inhabitant = null;


            return returnString;
        }

        private Tracked_Inhabitant coordinatesFieldofView(Body current_body)
        {
            Tracked_Inhabitant inhabitant = new Tracked_Inhabitant(
                Math.Round(current_body.Joints[JointType.SpineMid].Position.X, 2) * (-1), //x
                Math.Round(current_body.Joints[JointType.SpineMid].Position.Z, 2), //y
                true
                );
            //From the Skeleton Joints we use as position the SpineMid coordinates
            // Remember that Z represents the depth and thus from the perspective of a Cartesian plane it represents Y from a top view
            // Remember that X represents side to side movement. The center of the camera marks origin (0,0). 
            // As the Kinect is mirrored we multiple by times -1

            return inhabitant;
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
            myPointCollection.Add(new Point(x, canvasWidth / 2));
            myPointCollection.Add(new Point(x1, canvasWidth / 2));
            myPointCollection.Add(new Point(canvasWidth / 2, canvasHeight));

            //Creating the triangle from the 3 vertices

            Polygon myPolygon = new Polygon();
            myPolygon.Points = myPointCollection;
            myPolygon.Fill = Brushes.Purple;
            myPolygon.Width = canvasWidth;
            myPolygon.Height = canvasHeight;
            myPolygon.Stretch = Stretch.Fill;
            myPolygon.Stroke = Brushes.Purple;
            myPolygon.StrokeThickness = 1;
            myPolygon.Opacity = 0.2;

            //Add the triangle in our canvas
            topView.Children.Add(myPolygon);


            logConsole("Window initialised");
        }

        private void confirmSettings_Click(object sender, RoutedEventArgs e)
        {
            String newURL = postUrl.Text;
            int newInterval = Convert.ToInt32(intervalLength.Text);

            logConsole("confirmed Settings: ");
            logConsole($"URL: \"{newURL}\"    Interval-length: {newInterval}");

            post_Url = newURL;
            intervalCounter.interval = newInterval;
            intervalCounter.active = true;
        }

        //logs the string to the window console
        private void logConsole(String consoleoOut)
        {
            windowConsole.Text = windowConsole.Text + Environment.NewLine + consoleoOut;
            ConsoleScroller.ScrollToBottom();
        }


    }
}
