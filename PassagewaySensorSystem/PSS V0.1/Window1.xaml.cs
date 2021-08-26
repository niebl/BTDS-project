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
using System.Net.Http;

//Kinect Libraries
using Microsoft.Kinect;

namespace PSS_V0._1
{
    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public static String postURL;

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
        private TrackedBody[] trackedBodies =
        {
            new TrackedBody(0,0,false),
            new TrackedBody(0,0,false),
            new TrackedBody(0,0,false),
            new TrackedBody(0,0,false),
            new TrackedBody(0,0,false),
            new TrackedBody(0,0,false),
        };
        
        //Data for each body
        List<System.Windows.Media.Brush> bodyBrushes = new List<System.Windows.Media.Brush>();
        public double dperPixZ = 0;
        public double dperPixX = 0;

        public static double[,] linePoints;

        //Http Client Object which will be responsible for sending all the requests
        //https://stackoverflow.com/questions/4015324/how-to-make-an-http-post-web-request
        private static readonly HttpClient client = new HttpClient();

        //class representing x,y coordinates in body space
        public class Coordinate
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
            public Coordinate(double x, double y)
            {
                this.x = x;
                this.y = y;
                this.confirmed = true;
            }
        }



        public Window1(double[,] line, String newPostURL)
        {
            postURL = newPostURL;

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
            linePoints = line;

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
            Canvas.SetTop(ellipse, fieldOfView.ActualHeight - coord_y);
            return ellipse;
        }

        //this new function works a little different because the line that is given is already in the same coordinate system as the body coordinates
        //old function commented out above for comparison
        private Line createLine(double[,] points)
        {
            Line myLine = new Line();
            myLine.Stroke = System.Windows.Media.Brushes.LightSteelBlue;
            myLine.X1 = fieldOfView.ActualWidth / 2 + points[0, 0];
            myLine.X2 = fieldOfView.ActualWidth / 2 + points[1, 0];
            myLine.Y1 = fieldOfView.ActualHeight - points[0, 2];
            myLine.Y2 = fieldOfView.ActualHeight - points[1, 2];

            myLine.HorizontalAlignment = HorizontalAlignment.Left;
            myLine.VerticalAlignment = VerticalAlignment.Center;
            myLine.StrokeThickness = 2;

            this.fieldOfView.Children.Clear();
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

                    String testline = "pt1: " + Convert.ToString(linePoints[0, 0]) + "," + Convert.ToString(linePoints[0, 2]);

                    logConsole(testline);


                    // Create bodies in the scene
                    createLine(linePoints);
                    DrawTracked_Bodies(tracked_bodies);
                    bodies_To_Inhabitants(tracked_bodies);


                }
            }
        }
        
        private void bodies_To_Inhabitants(List<Body> tracked_bodies)
        {
            for(int inhabIndex = 0; inhabIndex < 6; inhabIndex++)
            {
                trackedBodies[inhabIndex].tracked = bodies_ids[inhabIndex] != 0 ;

                if (trackedBodies[inhabIndex].tracked)
                {
                    Body tracked_body = tracked_bodies.Find(x => x.TrackingId == bodies_ids[inhabIndex]);
                    var current_body = tracked_body.Joints[JointType.SpineMid];

                    dperPixZ = (double)fieldOfView.ActualHeight / 5000;

                    trackedBodies[inhabIndex].update(
                        Math.Round(current_body.Position.X, 2) * dperPixZ * 1000 * (-1),
                        Math.Round(current_body.Position.Z, 2) * dperPixZ * 1000,
                        
                        true, //this question is answered in if-clause a few lines up
                        bodies_ids[inhabIndex]
                    );
                }
                else
                {
                    continue; //probably not necessary in retrospect, but I think the compiler already takes care of it.
                }
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

                // First check previously tracked bodies
                for (int exist_id = 0; exist_id < 6; exist_id++)
                {
                    if (bodies_ids[exist_id] == current_id)
                    {
                        is_tracked = true;
                        createBody(fieldOfView.ActualWidth / 2 + bodyX, bodyZ, bodyBrushes[exist_id]);
                        //coord_body.Content = coordinatesFieldofView(tracked_bodies[exist_id]);
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
                            //coord_body.Content = coordinatesFieldofView(tracked_bodies[new_id]);

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

        //logs the string to the window console
        public void logConsole(String consoleoOut)
        {
            windowConsole.Text = windowConsole.Text + Environment.NewLine + consoleoOut;
            ConsoleScroller.ScrollToBottom();
        }

        //checks for a crossing of a given line segment and the threshold
        // based on https://www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/?ref=lbp
        // return 0: no crossing
        // return 1: in
        // return -1: out
        // threshold {x,y}, trajectorySegment {x,y}
        private static int thresholdCrossed(double[,] trajectory)
        {
            int returnValue = 0;

            Coordinate p1 = new Coordinate(linePoints[0, 0], linePoints[0, 2]);
            Coordinate q1 = new Coordinate(linePoints[1, 0], linePoints[1, 2]);
            Coordinate p2 = new Coordinate(trajectory[0, 0], trajectory[0, 1]);
            Coordinate q2 = new Coordinate(trajectory[1, 0], trajectory[1, 1]);

            //check if the lines intersect
            if (!doIntersect(p1, q1, p2, q2))
            {
                return 0;
            }

            //check direction of intersection
            returnValue = orientation(p1, q1, p2);

            //clockwise means entering, counterclockwise means leaving  
            switch (returnValue)
            {
                case 0: return 0;
                case 1: return -1;
                case 2: return 1;

            }
          
            return 0;
        }

        // Given three colinear points p, q, r, the function checks if
        // point q lies on line segment 'pr'
        private static Boolean onSegment(Coordinate p, Coordinate q, Coordinate r)
        {
            if (q.x <= Math.Max(p.x, r.x) && q.x >= Math.Min(p.x, r.x) &&
                q.y <= Math.Max(p.y, r.y) && q.y >= Math.Min(p.y, r.y))
                return true;

            return false;
        }

        // To find orientation of ordered triplet (p, q, r).
        // The function returns following values
        // 0 --> p, q and r are colinear
        // 1 --> Clockwise
        // 2 --> Counterclockwise
        private static int orientation(Coordinate p, Coordinate q, Coordinate r)
        {
            // See https://www.geeksforgeeks.org/orientation-3-ordered-points/
            // for details of below formula.
            double val = (q.y - p.y) * (r.x - q.x) -
                    (q.x - p.x) * (r.y - q.y);

            if (val == 0) return 0; // colinear

            return (val > 0) ? 1 : 2; // clock or counterclock wise
        }

        // The main function that returns true if line segment 'p1q1'
        // and 'p2q2' intersect.
        private static Boolean doIntersect(Coordinate p1, Coordinate q1, Coordinate p2, Coordinate q2)
        {
            // Find the four orientations needed for general and
            // special cases
            int o1 = orientation(p1, q1, p2);
            int o2 = orientation(p1, q1, q2);
            int o3 = orientation(p2, q2, p1);
            int o4 = orientation(p2, q2, q1);

            // General case
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1
            if (o1 == 0 && onSegment(p1, p2, q1)) return true;

            // p1, q1 and q2 are colinear and q2 lies on segment p1q1
            if (o2 == 0 && onSegment(p1, q2, q1)) return true;

            // p2, q2 and p1 are colinear and p1 lies on segment p2q2
            if (o3 == 0 && onSegment(p2, p1, q2)) return true;

            // p2, q2 and q1 are colinear and q1 lies on segment p2q2
            if (o4 == 0 && onSegment(p2, q1, q2)) return true;

            return false; // Doesn't fall in any of the above cases
        }

        //todo, save body id also, to differentiate between old and new bodies
        public class TrackedBody
        {
            public double x { get; set; }
            public double y { get; set; }
            private double prevX;
            private double prevY;
            public ulong trackingID { get; set; }

            public bool tracked { get; set; }

            private int updatesSinceLastEvent;
            private int passageStatus;

            public TrackedBody(double x, double y, bool tracked)
            {
                this.x = x;
                this.y = y;
                this.prevX = x;
                this.prevY = y;

                this.trackingID = 0;

                this.tracked = tracked;
                this.updatesSinceLastEvent = 0;
            }

            public void update(double x, double y, bool tracked, ulong trackingID)
            {
                this.prevX = this.x;
                this.prevY = this.y;
                this.x = x;
                this.y = y;

                this.tracked = tracked;
                this.updatesSinceLastEvent += 1;

                double[,] traj = new double[,] {
                    { this.prevX, this.prevY }, 
                    { this.x, this.y } 
                };

                if(trackingID == this.trackingID)
                {
                    this.passageStatus += thresholdCrossed(traj);
                }

                if (this.updatesSinceLastEvent > 50)
                {
                    Console.WriteLine($"body: x:{this.x}, y:{this.y}");
                    Console.WriteLine($"line: x1:{linePoints[0,0]}, y1:{linePoints[0,2]}       x2:{linePoints[1, 0]}, y2:{linePoints[1, 2]}");
                    this.postUpdate();

                }

                this.trackingID = trackingID;
            }

            private void postUpdate()
            {
                if(this.passageStatus != 0)
                {
                    String eventText = "";

                    if (this.passageStatus == 1)
                    {
                        eventText = "in";
                    } else if(this.passageStatus == -1)
                    {
                        eventText = "out";
                    }

                    DateTime currentTime = DateTime.Now;
                    long currentUnix = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

                    Console.WriteLine($"reporting passage {this.passageStatus}, sending to {postURL}, timestamp {currentUnix}");
                    

                    var postValues = new Dictionary<string, string>
                        {
                            {"timestamp", currentUnix.ToString() },
                            {"event", eventText }
                        };

                    var postContent = new FormUrlEncodedContent(postValues);
                    sendPost(postURL, postContent);

                    this.passageStatus = 0;
                    this.updatesSinceLastEvent = 0;
                }
            }

            private async void sendPost(String URL, FormUrlEncodedContent content)
            {
                var response = await client.PostAsync(URL, content);
                //logConsole(await response.Content.ReadAsStringAsync());
            }


        }

    }
}