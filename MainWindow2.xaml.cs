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
using System.Threading;
using System.Diagnostics;
using System.Timers;

namespace SVM1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow2 : Window
    {

        public MainWindow2()
        {
            InitializeComponent();
            nodes = new PXCMHandData.JointData[][] { new PXCMHandData.JointData[0x20], new PXCMHandData.JointData[0x20] };
            currentPoint = new Point();
            gesture = Gesture.Undefined;
            xValues = new float[arraySize];
            yValues = new float[arraySize];
            zValues = new float[arraySize];
            xyzValueIndex = 0;
            detectionAlert = string.Empty;
            calibrationAlert = string.Empty;
            bordersAlert = string.Empty;
            detectionStatusOk = false;
            calibrationStatusOk = false;
            borderStatusOk = false;
            processingThread = new Thread(new ThreadStart(ProcessingThread));
            senseManager = PXCMSenseManager.CreateInstance();
            senseManager.EnableHand();
            senseManager.Init();
            ConfigureHandModule();
            processingThread.Start();
        }
        private enum Gesture
        {
            Undefined,
            FingerSpread,
            Pinch,
            Wave,
            SwipeLeft,
            SwipeRight,
            Fist,
            Thumb
        };

        private Gesture gesture;
        private Thread processingThread;
        private PXCMSenseManager senseManager;
        private Int32 nhands;
        private Int32 handId;
        private float handTipX;
        private float handTipY;
        private float handTipZ;


        private float swipehandTipX;
        private float swipehandTipY;
        private float swipehandTipZ;

        float swipebeginx = 0;


        private string detectionAlert;
        private string calibrationAlert;
        private string bordersAlert;
        private bool detectionStatusOk;
        private bool calibrationStatusOk;
        private bool borderStatusOk;
        private PXCMHandModule hand;
        private PXCMHandData handData;
        private PXCMHandData.GestureData gestureData;
        private PXCMHandData.IHand ihand;
        private PXCMHandData.JointData[][] nodes;
        private PXCMHandConfiguration handConfig;
        private const int MaxPointSize = 30;
        private const int DrawingCanvasHeight = 600;
        private const int DrawingCanvasWidth = 600;
        private const int arraySize = 30;
        private float[] xValues;
        private float[] yValues;
        private float[] zValues;
        private int xyzValueIndex;
        Point currentPoint;
        Brush ColorBrush;
        Stopwatch timerCount;
        private long timedifference;
        int framecount = 0;
        bool fingerSpreadStarted = false;
        float diff = 0;
        bool leftHand = false;
        public MainWindow _firstWindow;

        private void ConfigureHandModule()
        {
            timerCount = Stopwatch.StartNew();
            hand = senseManager.QueryHand();
            handConfig = hand.CreateActiveConfiguration();
            handConfig.EnableGesture("spreadfingers");
            handConfig.EnableGesture("two_fingers_pinch_open");
            handConfig.EnableGesture("wave");
            handConfig.EnableGesture("swipe");
            handConfig.EnableGesture("swipe_left");
            handConfig.EnableGesture("swipe_right");
            handConfig.EnableGesture("fist");
            handConfig.EnableGesture("thumb_up");

            handConfig.EnableAllAlerts();
            handConfig.ApplyChanges();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _firstWindow.FirstWindow = true;
            processingThread.Abort();
            if (handData != null) handData.Dispose();
            if (handConfig != null) handConfig.Dispose();
            senseManager.Dispose();
        }

        public void ProcessingThread()
        {
            // Start AcquireFrame/ReleaseFrame loop
            while (senseManager.AcquireFrame(true) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                hand = senseManager.QueryHand();

                if (hand != null)
                {

                    // Retrieve the most recent processed data
                    handData = hand.CreateOutput();
                    handData.Update();

                    // Get number of tracked hands
                    nhands = handData.QueryNumberOfHands();

                    if (nhands > 0)
                    {
                        // Retrieve hand identifier
                        handData.QueryHandId(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, 0, out handId);

                        // Retrieve hand data
                        handData.QueryHandDataById(handId, out ihand);

                        PXCMHandData.BodySideType bodySideType = ihand.QueryBodySide();
                        if (bodySideType == PXCMHandData.BodySideType.BODY_SIDE_LEFT)
                        {
                            leftHand = true;
                        }
                        else if (bodySideType == PXCMHandData.BodySideType.BODY_SIDE_RIGHT)
                        {
                            leftHand = false;
                        }



                        // Retrieve all hand joint data
                        for (int i = 0; i < nhands; i++)
                        {
                            for (int j = 0; j < 0x20; j++)
                            {
                                PXCMHandData.JointData jointData;
                                ihand.QueryTrackedJoint((PXCMHandData.JointType)j, out jointData);
                                nodes[i][j] = jointData;
                            }
                        }

                        // Get world coordinates for tip of middle finger on the first hand in camera range
                        handTipX = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionWorld.x;
                        handTipY = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionWorld.y;
                        handTipZ = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionWorld.z;
                        

                        swipehandTipX = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionImage.x;
                        swipehandTipY = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionImage.y;
                        swipehandTipZ = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionImage.z;

                        //Console.Out.WriteLine("Before x={0}", swipehandTipX);
                        //Console.Out.WriteLine("Before speed={0}", nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].speed.x);

                        // Retrieve gesture data
                        if (handData.IsGestureFired("spreadfingers", out gestureData)) { gesture = Gesture.FingerSpread; }
                        else if (handData.IsGestureFired("two_fingers_pinch_open", out gestureData)) { gesture = Gesture.Pinch; }
                        else if (handData.IsGestureFired("wave", out gestureData)) { gesture = Gesture.Wave; }
                        else if (handData.IsGestureFired("swipe_left", out gestureData)) { gesture = Gesture.SwipeLeft; }
                        else if (handData.IsGestureFired("swipe_right", out gestureData)) { gesture = Gesture.SwipeRight; }
                        else if (handData.IsGestureFired("fist", out gestureData)) { gesture = Gesture.Fist; }
                        else if (handData.IsGestureFired("thumb_up", out gestureData)) { gesture = Gesture.Thumb; }

                    }
                    else
                    {
                        gesture = Gesture.Undefined;
                    }

                    UpdateUI();
                    if (handData != null) handData.Dispose();
                }
                senseManager.ReleaseFrame();
            }
        }

        public void GoNext()
        {
            if (indexchange._elementFlow.SelectedIndex < 1)
            {
                indexchange._elementFlow.SelectedIndex = indexchange._dataSource.Count - 1;
            }
            else
            {
                indexchange._elementFlow.SelectedIndex = indexchange._elementFlow.SelectedIndex - 1;
            }
        }


        public void GoBack()
        {
            if (indexchange._elementFlow.SelectedIndex > indexchange._dataSource.Count - 2)
            {
                indexchange._elementFlow.SelectedIndex = 0;
            }
            else
            {
                indexchange._elementFlow.SelectedIndex = indexchange._elementFlow.SelectedIndex + 1;
            }
        }


        public void Buy()
        {
            indexchange._popoutDistanceSlider.Value = 2;
            AutoClosingMessageBox.Show("You have purchased this product.. Congratulations!!", "Caption", 3000);

            //indexchange._dataSource.RemoveAt(indexchange._elementFlow.SelectedIndex);
            indexchange._popoutDistanceSlider.Value = -1.85;
        }

        bool startLeftTracking = false;
        bool startRightTracking = false;

        private void UpdateUI()
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {

                // Do moving average filtering
                if (xyzValueIndex < arraySize)
                {
                    xValues[xyzValueIndex] = handTipX;
                    yValues[xyzValueIndex] = handTipY;
                    zValues[xyzValueIndex] = handTipZ;
                    xyzValueIndex++;
                }
                else
                {
                    for (int i = 0; i < arraySize - 1; i++)
                    {
                        xValues[i] = xValues[i + 1];
                        yValues[i] = yValues[i + 1];
                        zValues[i] = zValues[i + 1];
                    }

                    xValues[arraySize - 1] = handTipX;
                    yValues[arraySize - 1] = handTipY;
                    zValues[arraySize - 1] = handTipZ;
                }

                // Update Z axis ellipse
                float scaledZ = zValues.Average() * -100 + 50;

                //Console.Out.WriteLine("Swipe X" + swipehandTipX);






                if ((swipehandTipX < 200) && (swipehandTipX > 0) && (leftHand==false))
                {
                    startLeftTracking = true;
                }
                else
                if ((swipehandTipX > 400) && (leftHand==true))
                {
                    startRightTracking = true;
                }


                if ((startLeftTracking) || (startRightTracking))
                {
                    framecount++;
                }

                if ((swipehandTipX > 450) && (startLeftTracking))
                {
                    Console.Out.WriteLine("FrameCount " + framecount);
                    startLeftTracking = false;
                    framecount = 0;
                    if (indexchange._elementFlow.SelectedIndex < 1)
                    {
                        indexchange._elementFlow.SelectedIndex = indexchange._dataSource.Count-1;
                    }
                    else
                    {
                        indexchange._elementFlow.SelectedIndex = indexchange._elementFlow.SelectedIndex - 1;
                    }
                }

                if ((swipehandTipX < 100) && (startRightTracking) && (swipehandTipX !=0))
                {
                    Console.Out.WriteLine("FrameCount " + framecount);
                    startRightTracking = false;
                    framecount = 0;
                    if (indexchange._elementFlow.SelectedIndex > indexchange._dataSource.Count-2)
                    {
                        indexchange._elementFlow.SelectedIndex = 0;
                    }
                    else
                    {
                        indexchange._elementFlow.SelectedIndex = indexchange._elementFlow.SelectedIndex + 1;
                    }
                }

                if (framecount>100)
                {
                    startLeftTracking = false;
                    startRightTracking = false;
                    framecount = 0;
                }


                // Draw line
                if ((gesture == Gesture.Pinch) && (handTipX >= -0.12))
                {

                    Line line = new Line();
                    line.Stroke = ColorBrush;
                    line.StrokeThickness = scaledZ;
                    line.StrokeDashCap = PenLineCap.Round;
                    line.StrokeStartLineCap = PenLineCap.Round;
                    line.StrokeEndLineCap = PenLineCap.Round;
                    line.X1 = currentPoint.X;
                    line.Y1 = currentPoint.Y;
                    line.X2 = xValues.Average() * -3000 + 300;
                    line.Y2 = yValues.Average() * -3000 + 300;

                    currentPoint.X = xValues.Average() * -3000 + 300;
                    currentPoint.Y = yValues.Average() * -3000 + 300;

                }
                else
                {

                    currentPoint.X = xValues.Average() * -3000 + 300;
                    currentPoint.Y = yValues.Average() * -3000 + 300;
                }

                
                //if (gesture == Gesture.FingerSpread)
                //{
                //    fingerSpreadStarted = true;
                   
                //    if (swipebeginx == 0)
                //    {
                //        swipebeginx = swipehandTipX;
                //    }
                //    else
                //    {
                //        diff = swipehandTipX - swipebeginx;
                //        Console.Out.WriteLine("Difference is " + diff.ToString());
                //        Console.Out.WriteLine("x={0}", swipehandTipX );
                //        Console.Out.WriteLine("framecount" + framecount);
                //        if (framecount > 100)
                //        {
                //            swipebeginx = 0;
                //        }
                //    }
                //    //if (swipehandTipX)
                //    //Console.Out.WriteLine("x={0}, y= {1}, z={2}", handTipX,handTipY,handTipZ);
                //    //Console.Out.WriteLine("x={0}, y= {1}, z={2}", currentPoint.X, currentPoint.Y, 0);
                //}
                //else
                //{
                //    if (fingerSpreadStarted == true)
                //    {
                //        swipebeginx = 0;

                //        if (diff > 140)
                //        {
                //            //Console.Out.WriteLine("Gesture Swipe Left " + diff);
                //            //MessageBox.Show("Gesture Swipe Left");

                //            if (indexchange._elementFlow.SelectedIndex < 1)
                //            {
                //                indexchange._elementFlow.SelectedIndex = 15;
                //            }
                //            else
                //            {
                //                indexchange._elementFlow.SelectedIndex = indexchange._elementFlow.SelectedIndex - 1;
                //            }
                //        }

                //        if (diff < -140)
                //        {
                //            //Console.Out.WriteLine("Gesture Swipe right " + diff);
                //            //MessageBox.Show("Gesture Swipe Right");
                //            if (indexchange._elementFlow.SelectedIndex > 14)
                //            {
                //                indexchange._elementFlow.SelectedIndex = 0;
                //            }
                //            else
                //            {
                //                indexchange._elementFlow.SelectedIndex = indexchange._elementFlow.SelectedIndex + 1;
                //            }
                //        }
                       

                //        //MessageBox.Show("Finger Spread End");
                //        fingerSpreadStarted = false;
                //    }
                //}
                // Erase canvas on hand wave
                if (gesture == Gesture.Wave)
                {

                }
                // Update gesture info
                switch (gesture)
                {
                    case Gesture.Undefined:
                        //lblDraw.Foreground = Brushes.White;
                        //MessageBox.Show("I cannot recognize you!!!");
                        break;
                    case Gesture.FingerSpread:
                        break;    
                    case Gesture.Pinch:
                        break;
                    case Gesture.SwipeLeft:
                        //MessageBox.Show("Its Left");
                        timedifference = timerCount.ElapsedTicks;
                        Console.WriteLine(timedifference);

                        if (timedifference < 5000000)
                        {
                            break;
                        }
                        else
                        {
                            timerCount.Reset();
                            timerCount.Start();
                            //AutoClosingMessageBox.Show("Its Left", "Caption", 500);
                            if (indexchange._elementFlow.SelectedIndex < 1)
                            {
                                indexchange._elementFlow.SelectedIndex = 15;
                            }
                            else
                            {
                                indexchange._elementFlow.SelectedIndex = indexchange._elementFlow.SelectedIndex - 1;
                            }
                            break;
                        }
                    case Gesture.SwipeRight:
                        //MessageBox.Show("Its Right");
                        timedifference = timerCount.ElapsedTicks;
                        Console.WriteLine(timedifference);

                        if (timedifference < 5000000)
                        {
                            break;
                        }
                        else
                        {
                            timerCount.Reset();
                            timerCount.Start();
                            //AutoClosingMessageBox.Show("Its Right", "Caption", 500);
                            if (indexchange._elementFlow.SelectedIndex > 14)
                            {
                                indexchange._elementFlow.SelectedIndex = 0;
                            }
                            else
                            {
                                indexchange._elementFlow.SelectedIndex = indexchange._elementFlow.SelectedIndex + 1;
                            }
                            break;
                        }
                    case Gesture.Wave:
                        break;
                    case Gesture.Fist:
                        //MessageBox.Show("You have purchased this product.. Congratulations!!");
                        break;
                    case Gesture.Thumb:
                        timedifference = timerCount.ElapsedTicks;
                        Console.WriteLine(timedifference);

                        if (timedifference < 5000000)
                        {
                            break;
                        }
                        else
                        {                         
                            indexchange._popoutDistanceSlider.Value = 2;
                            AutoClosingMessageBox.Show("You have purchased this product.. Congratulations!!", "Caption", 3000);

                           // indexchange._dataSource.RemoveAt(indexchange._elementFlow.SelectedIndex);    
                            indexchange._popoutDistanceSlider.Value = -1.85;
                            timerCount.Reset();
                            timerCount.Start();
                            break;
                        }
                }
                

            }));
        }

        public class AutoClosingMessageBox
        {
            System.Threading.Timer _timeoutTimer;
            string _caption;
            AutoClosingMessageBox(string text, string caption, int timeout)
            {
                _caption = caption;
                _timeoutTimer = new System.Threading.Timer(OnTimerElapsed,
                    null, timeout, System.Threading.Timeout.Infinite);
                MessageBox.Show(text, caption);
            }
            public static void Show(string text, string caption, int timeout)
            {
                new AutoClosingMessageBox(text, caption, timeout);
            }
            void OnTimerElapsed(object state)
            {
                IntPtr mbWnd = FindWindow(null, _caption);
                if (mbWnd != IntPtr.Zero)
                    SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                _timeoutTimer.Dispose();
            }
            const int WM_CLOSE = 0x0010;
            [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
            static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Window_Closing(sender, e);
        }

        private void btnLogOut_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void LogOut()
        {
            this.Close();
        }
    }
}
