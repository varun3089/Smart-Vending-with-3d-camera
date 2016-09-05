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
using System.Drawing;
using System.IO;
using FaceID;
using ZXing;
using Microsoft.Isam.Esent.Collections.Generic;
namespace SVM1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread processingThread;
        private PXCMSenseManager senseManager;
        private PXCMFaceConfiguration.RecognitionConfiguration recognitionConfig;
        private PXCMFaceData faceData;
        private PXCMFaceData.RecognitionData recognitionData;

        private PXCMSession session;
        private PXCMAudioSource audioSource;
        private PXCMSpeechRecognition speechRecognition;

        private MainWindow2 window2;

        private Int32 numFacesDetected;
        private string userId;
        private string dbState;
        private const int DatabaseUsers = 1000;
        private const string DatabaseName = "UserDB";
        private const string DatabaseFilename = @"c:\svmfacedb\database2.bin";
        private bool doRegister;
        private bool doUnregister;
        private int faceRectangleHeight;
        private int faceRectangleWidth;
        private int faceRectangleX;
        private int faceRectangleY;
        private PersistentDictionary<string, string> _persistentDict;
        private String userName;
        private String newUserName;
        public bool FirstWindow = true;
        public bool SecondWindow = false;

        private MainWindow _mainwindow;
        System.Windows.Threading.Dispatcher _disp;

        private PXCMFaceConfiguration.ExpressionsConfiguration expressionConfiguration;
        private float faceAverageDepth;
        private float headRoll;
        private float headPitch;
        private float headYaw;
        private const int TotalExpressions = 5;
        private int[] expressionScore = new int[TotalExpressions];

        private PXCMHandModule hand;
        private PXCMHandData handData;
        private PXCMHandData.GestureData gestureData;
        private PXCMHandData.IHand ihand;
        private PXCMHandData.JointData[][] nodes;
        private PXCMHandConfiguration handConfig;

        private enum FaceExpression
        {
            None,
            Kiss,
            Open,
            Smile,
            Tongue
        };

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

        private Int32 nhands;
        private Int32 handId;
        private float handTipX;
        private float handTipY;
        private float handTipZ;
        bool leftHand = false;
        

        private float swipehandTipX;
        private float swipehandTipY;
        private float swipehandTipZ;
        bool startLeftTracking = false;
        bool startRightTracking = false;
        int framecount = 0;
        



        public MainWindow()
        {
            _mainwindow = this;
            _disp = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            nodes = new PXCMHandData.JointData[][] { new PXCMHandData.JointData[0x20], new PXCMHandData.JointData[0x20] };
            //currentPoint = new Point();
            gesture = Gesture.Undefined;
            //xValues = new float[arraySize];
            //yValues = new float[arraySize];
            //zValues = new float[arraySize];
            //xyzValueIndex = 0;
            //_persistentDict = new CPersistentDictionary(@"c:\svmfacedb\facerecog.dict");
            _persistentDict = new PersistentDictionary<string, string>("Names2");
            
            //_persistentDict.Add("100", "Shailesh");
            //_persistentDict.Save();

            //_persistentDict.Add("200", "jannu");
            //_persistentDict.Save();


            //_persistentDict.Dispose();

            InitializeComponent();
            rectFaceMarker.Visibility = Visibility.Hidden;
            //chkShowFaceMarker.IsChecked = true;
            numFacesDetected = 0;
            userId = string.Empty;
            dbState = string.Empty;
            doRegister = false;
            doUnregister = false;

            // Start SenseManage and configure the face module
            ConfigureRealSense();
            ConfigureRealSenseSpeech();
            //ConfigureHandModule();


            // Start the worker thread
            processingThread = new Thread(new ThreadStart(ProcessingThread));
            processingThread.Start();


            //var dictionary = new PersistentDictionary<string, string>("Names");
            //Console.WriteLine("What is your first name?");
            //string firstName ="100";
            //if (dictionary.ContainsKey("100"))
            //{
            //    Console.WriteLine("Welcome back {0} {1}",
            //        "100",
            //        dictionary["100"]);
            //}
            //else
            //{
            //    Console.WriteLine(
            //        "I don't know you, {0}. What is your last name?",
            //        "100");
            //    dictionary["100"] = "SHAILESH";
            //}
        }

        private void ConfigureRealSenseSpeech()
        {
            // Instantiate session and audio source objects
            session = PXCMSession.CreateInstance();
            audioSource = session.CreateAudioSource();

            // Select the first audio device
            PXCMAudioSource.DeviceInfo deviceInfo;
            deviceInfo = new PXCMAudioSource.DeviceInfo();
            audioSource.QueryDeviceInfo(0, out deviceInfo);
            audioSource.SetDevice(deviceInfo);

            // Set the audio recording volume
            audioSource.SetVolume(0.2f);

            // Create a speech recognition instance
            session.CreateImpl<PXCMSpeechRecognition>(out speechRecognition);

            // Initialize the speech recognition module
            PXCMSpeechRecognition.ProfileInfo profileInfo;
            speechRecognition.QueryProfile(0, out profileInfo);
            profileInfo.language = PXCMSpeechRecognition.LanguageType.LANGUAGE_US_ENGLISH;
            speechRecognition.SetProfile(profileInfo);

            // Build and set the active grammar
            pxcmStatus status = speechRecognition.BuildGrammarFromFile(1, PXCMSpeechRecognition.GrammarFileType.GFT_JSGF, "grammarsvm.jsgf");

            if (status == pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                speechRecognition.SetGrammar(1);
            }
            else
            {
                MessageBox.Show("Java Speech Grammar Format (JSGF) file not found!");
                this.Close();
            }

            // Display device information
            //lblDeviceInfo.Content = string.Format("[Device: {0}, Language Profile: {1}]", deviceInfo.name, profileInfo.language);

            // Set the speech recognition handler
            PXCMSpeechRecognition.Handler handler = new PXCMSpeechRecognition.Handler();
            handler.onRecognition = OnRecognition;
            speechRecognition.StartRec(null, handler);
        }


        public void OnRecognition(PXCMSpeechRecognition.RecognitionData data)
        {
            

            if (data.scores[0].label < 0)
            {
                if (data.scores[0].confidence > 40)
                {
                    //MessageBox.Show(data.scores[0].sentence);

                    if (((data.scores[0].sentence == "search") || (data.scores[0].sentence == "find") ||(data.scores[0].sentence == "show")) && FirstWindow)
                    {
                        SecondWindow = true;
                        _disp.BeginInvoke((Action)(() =>
                        {
                            ShowProducts();

                        }));
        
                    }

                    if ((data.scores[0].sentence == "next") && SecondWindow)
                    {
                        FirstWindow = false;
                        _disp.BeginInvoke((Action)(() =>
                        {
                            window2.GoNext();


                        }));
                    }

                    if ((data.scores[0].sentence == "back") && SecondWindow)
                    { 
                        FirstWindow = false;
                        _disp.BeginInvoke((Action)(() =>
                        {
                            window2.GoBack();


                        }));

                    }
                    if ((data.scores[0].sentence == "buy") && SecondWindow)
                    {
                        FirstWindow = false;
                        _disp.BeginInvoke((Action)(() =>
                        {
                            window2.Buy();


                        }));
                    }

                    if (((data.scores[0].sentence == "logout") || (data.scores[0].sentence == "close")) && SecondWindow)
                    {
                        FirstWindow = true;
                        _disp.BeginInvoke((Action)(() =>
                        {
                            window2.LogOut();


                        }));
                    }
                    //// Search for a match in the numbers array
                    //for (ctr = 0; ctr < SetSize; ctr++)
                    //{
                    //    if (numbers[ctr] == data.scores[0].sentence)
                    //    {
                    //        userAnswer = ctr;
                    //        break;
                    //    }
                    //}

                    //// Determine if user's verbal answer is correct or wrong
                    //answerState = (ctr == product) ? AnswerState.Correct : AnswerState.Wrong;

                    //// Update the UI and load another problem
                    //UpdateUI();
                    //Thread.Sleep(PlayPause);
                    //LoadNextProblem();
                }
            }
        }


        private void ConfigureRealSense()
        {
            PXCMFaceModule faceModule;
            PXCMFaceConfiguration faceConfig;

            // Start the SenseManager and session  
            senseManager = PXCMSenseManager.CreateInstance();

            // Enable the color stream
            senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 640, 480, 30);
            //senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 550, 550, 30);

            // Enable the face module
            senseManager.EnableFace();
            //senseManager.EnableHand();
            
            faceModule = senseManager.QueryFace();
            faceConfig = faceModule.CreateActiveConfiguration();

            // Configure for 3D face tracking (if camera cannot support depth it will revert to 2D tracking)
            faceConfig.SetTrackingMode(PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH);

            expressionConfiguration = faceConfig.QueryExpressions();
            expressionConfiguration.Enable();
            expressionConfiguration.EnableAllExpressions();


            // Enable facial recognition
            recognitionConfig = faceConfig.QueryRecognition();
            recognitionConfig.Enable();

            // Create a recognition database
            PXCMFaceConfiguration.RecognitionConfiguration.RecognitionStorageDesc recognitionDesc = new PXCMFaceConfiguration.RecognitionConfiguration.RecognitionStorageDesc();
            recognitionDesc.maxUsers = DatabaseUsers;
            recognitionConfig.CreateStorage(DatabaseName, out recognitionDesc);
            recognitionConfig.UseStorage(DatabaseName);
            LoadDatabaseFromFile();
            recognitionConfig.SetRegistrationMode(PXCMFaceConfiguration.RecognitionConfiguration.RecognitionRegistrationMode.REGISTRATION_MODE_CONTINUOUS);

            // Apply changes and initialize
            faceConfig.ApplyChanges();
            senseManager.Init();
            faceData = faceModule.CreateOutput();

            // Mirror image
            senseManager.QueryCaptureManager().QueryDevice().SetMirrorMode(PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL);

            // Release resources
            faceConfig.Dispose();
            faceModule.Dispose();
        }


        private void ConfigureHandModule()
        {
            //timerCount = Stopwatch.StartNew();
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


        private void ProcessingThread()
        {
            // Start AcquireFrame/ReleaseFrame loop
            while (senseManager.AcquireFrame(true) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                // Acquire the color image data
                PXCMCapture.Sample sample = senseManager.QuerySample();
                Bitmap colorBitmap;
                PXCMImage.ImageData colorData;
                int topScore = 0;
                FaceExpression expression = FaceExpression.None;

                sample.color.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out colorData);
                colorBitmap = colorData.ToBitmap(0, sample.color.info.width, sample.color.info.height);

                try
                {
                    IBarcodeReader reader = new BarcodeReader();
                    // load a bitmap
                    //var barcodeBitmap = (Bitmap)Bitmap.LoadFrom("C:\\sample-barcode-image.png");
                    // detect and decode the barcode inside the bitmap
                    var result = reader.Decode(colorBitmap);
                    // do something with the result
                    if (result != null)
                    {
                        MessageBox.Show(result.BarcodeFormat.ToString());
                        MessageBox.Show(result.Text);
                    }
                }
                catch(Exception ex)
                {

                }

                // Get face data
                if (faceData != null)
                {
                    faceData.Update();
                    numFacesDetected = faceData.QueryNumberOfDetectedFaces();

                    if (numFacesDetected > 0)
                    {
                        // Get the first face detected (index 0)
                        PXCMFaceData.Face face = faceData.QueryFaceByIndex(0);

                        // Retrieve face location data
                        PXCMFaceData.DetectionData faceDetectionData = face.QueryDetection();
                        if (faceDetectionData != null)
                        {
                            PXCMRectI32 faceRectangle;
                            faceDetectionData.QueryBoundingRect(out faceRectangle);
                            if ((faceRectangle.h > 90) || (faceRectangle.w > 90))
                            {
                                faceRectangleHeight = faceRectangle.h * 3/2;
                                faceRectangleWidth = faceRectangle.w * 3/2;
                            }
                            else if (((faceRectangle.h < 90) || (faceRectangle.w < 90)) && ((faceRectangle.h > 70) || (faceRectangle.w > 70)))
                            {
                                faceRectangleHeight = faceRectangle.h * 2;
                                faceRectangleWidth = faceRectangle.w * 2;
                            }
                            else
                            {
                                faceRectangleHeight = faceRectangle.h * 5/2;
                                faceRectangleWidth = faceRectangle.w * 5/2;
                            }
                            faceRectangleX = faceRectangle.x;
                            faceRectangleY = faceRectangle.y;
                        }

                        // Retrieve pose estimation data
                        PXCMFaceData.PoseData facePoseData = face.QueryPose();
                        if (facePoseData != null)
                        {
                            PXCMFaceData.PoseEulerAngles headAngles;
                            facePoseData.QueryPoseAngles(out headAngles);
                            headRoll = headAngles.roll;
                            headPitch = headAngles.pitch;
                            headYaw = headAngles.yaw;
                        }

                        // Retrieve expression data
                        PXCMFaceData.ExpressionsData expressionData = face.QueryExpressions();

                        if (expressionData != null)
                        {
                            PXCMFaceData.ExpressionsData.FaceExpressionResult score;

                            expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_KISS, out score);
                            expressionScore[Convert.ToInt32(FaceExpression.Kiss)] = score.intensity;

                            expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_MOUTH_OPEN, out score);
                            expressionScore[Convert.ToInt32(FaceExpression.Open)] = score.intensity;

                            expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_SMILE, out score);
                            expressionScore[Convert.ToInt32(FaceExpression.Smile)] = score.intensity;

                            expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_TONGUE_OUT, out score);

                            
                            expressionScore[Convert.ToInt32(FaceExpression.Tongue)] = score.intensity;

                            // Determine the highest scoring expression
                            for (int i = 1; i < TotalExpressions; i++)
                            {
                                if (expressionScore[i] > topScore) { expression = (FaceExpression)i; }
                            }
                        }

                        // Process face recognition data
                        if (face != null)
                        {
                            // Retrieve the recognition data instance
                            recognitionData = face.QueryRecognition();

                            // Set the user ID and process register/unregister logic
                            if (recognitionData.IsRegistered())
                            {
                                userId = Convert.ToString(recognitionData.QueryUserID());

                                if (doUnregister)
                                {
                                    recognitionData.UnregisterUser();
                                    SaveDatabaseToFile();
                                    doUnregister = false;
                                    if (_persistentDict.ContainsKey(userId) == true)
                                        _persistentDict.Remove(userId);
                                }
                            }
                            else
                            {
                                if (doRegister)
                                {
                                    int uId = recognitionData.RegisterUser();
                                    SaveDatabaseToFile();

                                    if (newUserName != "")
                                    {
                                        if (_persistentDict.ContainsKey(uId.ToString()) == false)
                                        {
                                            _persistentDict.Add(uId.ToString(), newUserName);
                                            _persistentDict.Flush();
                                            newUserName = "";
                                        }
                                    }

                                    // Capture a jpg image of registered user
                                    colorBitmap.Save("image.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                                    doRegister = false;
                                }
                                else
                                {
                                    userId = "New User";
                                }
                            }
                        }
                    }
                    else
                    {
                        userId = "No users in view";
                    }
                }

                //hand = senseManager.QueryHand();

                //if (hand != null)
                //{

                //    // Retrieve the most recent processed data
                //    handData = hand.CreateOutput();
                //    handData.Update();

                //    // Get number of tracked hands
                //    nhands = handData.QueryNumberOfHands();

                //    if (nhands > 0)
                //    {
                //        // Retrieve hand identifier
                //        handData.QueryHandId(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, 0, out handId);

                //        // Retrieve hand data
                //        handData.QueryHandDataById(handId, out ihand);

                //        PXCMHandData.BodySideType bodySideType = ihand.QueryBodySide();
                //        if (bodySideType == PXCMHandData.BodySideType.BODY_SIDE_LEFT)
                //        {
                //            leftHand = true;
                //        }
                //        else if (bodySideType == PXCMHandData.BodySideType.BODY_SIDE_RIGHT)
                //        {
                //            leftHand = false;
                //        }



                //        // Retrieve all hand joint data
                //        for (int i = 0; i < nhands; i++)
                //        {
                //            for (int j = 0; j < 0x20; j++)
                //            {
                //                PXCMHandData.JointData jointData;
                //                ihand.QueryTrackedJoint((PXCMHandData.JointType)j, out jointData);
                //                nodes[i][j] = jointData;
                //            }
                //        }

                //        // Get world coordinates for tip of middle finger on the first hand in camera range
                //        handTipX = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionWorld.x;
                //        handTipY = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionWorld.y;
                //        handTipZ = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionWorld.z;


                //        swipehandTipX = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionImage.x;
                //        swipehandTipY = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionImage.y;
                //        swipehandTipZ = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionImage.z;

                //        //Console.Out.WriteLine("Before x={0}", swipehandTipX);
                //        //Console.Out.WriteLine("Before speed={0}", nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].speed.x);

                //        // Retrieve gesture data
                //        if (handData.IsGestureFired("spreadfingers", out gestureData)) { gesture = Gesture.FingerSpread; }
                //        else if (handData.IsGestureFired("two_fingers_pinch_open", out gestureData)) { gesture = Gesture.Pinch; }
                //        else if (handData.IsGestureFired("wave", out gestureData)) { gesture = Gesture.Wave; }
                //        else if (handData.IsGestureFired("swipe_left", out gestureData)) { gesture = Gesture.SwipeLeft; }
                //        else if (handData.IsGestureFired("swipe_right", out gestureData)) { gesture = Gesture.SwipeRight; }
                //        else if (handData.IsGestureFired("fist", out gestureData)) { gesture = Gesture.Fist; }
                //        else if (handData.IsGestureFired("thumb_up", out gestureData)) { gesture = Gesture.Thumb; }

                //    }
                //    else
                //    {
                //        gesture = Gesture.Undefined;
                //    }

                //    //UpdateUI();
                //    if (handData != null) handData.Dispose();
                //}

                // Display the color stream and other UI elements
                //UpdateUI(colorBitmap, expression, gesture);


                UpdateUI(colorBitmap, expression);

                // Release resources
                colorBitmap.Dispose();
                sample.color.ReleaseAccess(colorData);
                sample.color.Dispose();

                // Release the frame
                senseManager.ReleaseFrame();
            }
        }


        private void UpdateUI(Bitmap bitmap, FaceExpression expression)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                // Display  the color image
                if (bitmap != null)
                {
                    imgColorStream.Source = ConvertBitmap.BitmapToBitmapSource(bitmap);
                }

                if (_persistentDict.ContainsKey(userId.ToString()))
                    userName = _persistentDict[userId.ToString()].ToString();
                else
                    userName = "";

                if (userName == "")
                    lblName.Content = userId;
                else
                    lblName.Content = userName;
                // Update UI elements
                //lblNumFacesDetected.Content = String.Format("Faces Detected: {0}", numFacesDetected);
                //lblUserId.Content = String.Format("User ID: {0}", userId);
                //lblDatabaseState.Content = String.Format("Database: {0}", dbState);

                // Change picture border color depending on if user is in camera view
                if (numFacesDetected > 0)
                {
                    //bdrPictureBorder.BorderBrush = System.Windows.Media.Brushes.LightGreen;
                    switch (expression)
                    {
                        case FaceExpression.None:
                            imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/shame.png"));
                            break;
                        case FaceExpression.Kiss:
                            imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/kissy.png"));
                            break;
                        case FaceExpression.Open:
                            imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/surprise.png"));
                            break;
                        case FaceExpression.Smile:
                            imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/happy.png"));
                            break;
                        case FaceExpression.Tongue:
                            imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/tongue.png"));
                            break;
                        default:
                            imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/shame.png"));
                            break;
                    }

                    // Make the BlockHead visible
                    imgFace.Visibility = Visibility.Visible;

                    //// Scale the BlockHead
                    //imgFace.Width = faceRectangleWidth;
                    //imgFace.Height = faceRectangleHeight;

                    //// Rotate the BlockHead based on Roll orientation data
                    //RotateTransform headRotateTransform = new RotateTransform(headRoll);
                    //headRotateTransform.CenterX = imgFace.Width / 2;
                    //headRotateTransform.CenterY = imgFace.Height / 2;
                    //imgFace.RenderTransform = headRotateTransform;

                    //// Display the BlockHead on the canvas
                    //Canvas.SetRight(imgFace, faceRectangleX - 15);
                    //Canvas.SetTop(imgFace, faceRectangleY);
                }
                else
                {
                    imgFace.Visibility = Visibility.Hidden;
                    //bdrPictureBorder.BorderBrush = System.Windows.Media.Brushes.Red;
                }

                // Show or hide face marker
                if (numFacesDetected > 0)
                {
                    // Show face marker
                    rectFaceMarker.Height = faceRectangleHeight;
                    rectFaceMarker.Width = faceRectangleWidth;
                    Canvas.SetLeft(rectFaceMarker, faceRectangleX);
                    Canvas.SetTop(rectFaceMarker, faceRectangleY);
                    rectFaceMarker.Visibility = Visibility.Visible;

                    //// Show floating ID label
                    //lblFloatingId.Content = String.Format("User ID: {0}", userId);
                    //Canvas.SetLeft(lblFloatingId, faceRectangleX);
                    //Canvas.SetTop(lblFloatingId, faceRectangleY - 20);
                    //lblFloatingId.Visibility = Visibility.Visible;
                }
                else
                {
                    // Hide the face marker and floating ID label
                    rectFaceMarker.Visibility = Visibility.Hidden;
                    //lblFloatingId.Visibility = Visibility.Hidden;
                }



                //Console.Out.WriteLine("Swipe X" + swipehandTipX);






                //if ((swipehandTipX < 200) && (swipehandTipX > 0) && (leftHand == false))
                //{
                //    startLeftTracking = true;
                //}
                //else
                //    if ((swipehandTipX > 400) && (leftHand == true))
                //    {
                //        startRightTracking = true;
                //    }


                //if ((startLeftTracking) || (startRightTracking))
                //{
                //    framecount++;
                //}

                //if ((swipehandTipX > 450) && (startLeftTracking))
                //{
                //    Console.Out.WriteLine("FrameCount " + framecount);
                //    startLeftTracking = false;
                //    framecount = 0;
                //    window2.GoBack();
                //}

                //if ((swipehandTipX < 100) && (startRightTracking) && (swipehandTipX != 0))
                //{
                //    Console.Out.WriteLine("FrameCount " + framecount);
                //    startRightTracking = false;
                //    framecount = 0;
                //    window2.GoNext();
                //}

                //if (framecount > 100)
                //{
                //    startLeftTracking = false;
                //    startRightTracking = false;
                //    framecount = 0;
                //}

                //if ((gesture == Gesture.Fist) && (FirstWindow))
                //{
                //    //senseManager.PauseHand(true);
                //    SecondWindow = true;
                //    _disp.BeginInvoke((Action)(() =>
                //    {
                //        ShowProducts();

                //    }));
                //}

                //if ((gesture == Gesture.Thumb) && (SecondWindow))
                //{
                //    window2.Buy();
                //}
                // Show or hide face marker
                //if ((numFacesDetected > 0) && (chkShowFaceMarker.IsChecked == true))
                //{
                //    // Show face marker
                //    rectFaceMarker.Height = faceRectangleHeight;
                //    rectFaceMarker.Width = faceRectangleWidth;
                //    Canvas.SetLeft(rectFaceMarker, faceRectangleX);
                //    Canvas.SetTop(rectFaceMarker, faceRectangleY);
                //    rectFaceMarker.Visibility = Visibility.Visible;

                //    // Show floating ID label
                //    lblFloatingId.Content = String.Format("User ID: {0}", userId);
                //    Canvas.SetLeft(lblFloatingId, faceRectangleX);
                //    Canvas.SetTop(lblFloatingId, faceRectangleY - 20);
                //    lblFloatingId.Visibility = Visibility.Visible;
                //}
                //else
                //{
                //    // Hide the face marker and floating ID label
                //    rectFaceMarker.Visibility = Visibility.Hidden;
                //    lblFloatingId.Visibility = Visibility.Hidden;
                //}
            }));

            // Release resources
            bitmap.Dispose();
        }

        //private void UpdateUI(Bitmap bitmap, FaceExpression expression,Gesture gesture)
        //{
        //    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
        //    {
        //        // Display  the color image
        //        if (bitmap != null)
        //        {
        //            imgColorStream.Source = ConvertBitmap.BitmapToBitmapSource(bitmap);
        //        }

        //        if (_persistentDict.ContainsKey(userId.ToString()))
        //            userName = _persistentDict[userId.ToString()].ToString();
        //        else
        //            userName = "";

        //        if (userName == "")
        //            lblName.Content = userId;
        //        else
        //            lblName.Content = userName;
        //        // Update UI elements
        //        //lblNumFacesDetected.Content = String.Format("Faces Detected: {0}", numFacesDetected);
        //        //lblUserId.Content = String.Format("User ID: {0}", userId);
        //        //lblDatabaseState.Content = String.Format("Database: {0}", dbState);

        //        // Change picture border color depending on if user is in camera view
        //        if (numFacesDetected > 0)
        //        {
        //            //bdrPictureBorder.BorderBrush = System.Windows.Media.Brushes.LightGreen;
        //            switch (expression)
        //            {
        //                case FaceExpression.None:
        //                    imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/shame.png"));
        //                    break;
        //                case FaceExpression.Kiss:
        //                    imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/kissy.png"));
        //                    break;
        //                case FaceExpression.Open:
        //                    imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/surprise.png"));
        //                    break;
        //                case FaceExpression.Smile:
        //                    imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/happy.png"));
        //                    break;
        //                case FaceExpression.Tongue:
        //                    imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/tongue.png"));
        //                    break;
        //                default:
        //                    imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/shame.png"));
        //                    break;
        //            }

        //            // Make the BlockHead visible
        //            imgFace.Visibility = Visibility.Visible;

        //            //// Scale the BlockHead
        //            //imgFace.Width = faceRectangleWidth;
        //            //imgFace.Height = faceRectangleHeight;

        //            //// Rotate the BlockHead based on Roll orientation data
        //            //RotateTransform headRotateTransform = new RotateTransform(headRoll);
        //            //headRotateTransform.CenterX = imgFace.Width / 2;
        //            //headRotateTransform.CenterY = imgFace.Height / 2;
        //            //imgFace.RenderTransform = headRotateTransform;

        //            //// Display the BlockHead on the canvas
        //            //Canvas.SetRight(imgFace, faceRectangleX - 15);
        //            //Canvas.SetTop(imgFace, faceRectangleY);
        //        }
        //        else
        //        {
        //            imgFace.Visibility = Visibility.Hidden;
        //            //bdrPictureBorder.BorderBrush = System.Windows.Media.Brushes.Red;
        //        }

        //        // Show or hide face marker
        //        if (numFacesDetected > 0)
        //        {
        //            // Show face marker
        //            rectFaceMarker.Height = faceRectangleHeight;
        //            rectFaceMarker.Width = faceRectangleWidth;
        //            Canvas.SetLeft(rectFaceMarker, faceRectangleX);
        //            Canvas.SetTop(rectFaceMarker, faceRectangleY);
        //            rectFaceMarker.Visibility = Visibility.Visible;

        //            //// Show floating ID label
        //            //lblFloatingId.Content = String.Format("User ID: {0}", userId);
        //            //Canvas.SetLeft(lblFloatingId, faceRectangleX);
        //            //Canvas.SetTop(lblFloatingId, faceRectangleY - 20);
        //            //lblFloatingId.Visibility = Visibility.Visible;
        //        }
        //        else
        //        {
        //            // Hide the face marker and floating ID label
        //            rectFaceMarker.Visibility = Visibility.Hidden;
        //            //lblFloatingId.Visibility = Visibility.Hidden;
        //        }



        //        //Console.Out.WriteLine("Swipe X" + swipehandTipX);






        //        if ((swipehandTipX < 200) && (swipehandTipX > 0) && (leftHand == false))
        //        {
        //            startLeftTracking = true;
        //        }
        //        else
        //            if ((swipehandTipX > 400) && (leftHand == true))
        //            {
        //                startRightTracking = true;
        //            }


        //        if ((startLeftTracking) || (startRightTracking))
        //        {
        //            framecount++;
        //        }

        //        if ((swipehandTipX > 450) && (startLeftTracking))
        //        {
        //            Console.Out.WriteLine("FrameCount " + framecount);
        //            startLeftTracking = false;
        //            framecount = 0;
        //            window2.GoBack();
        //        }

        //        if ((swipehandTipX < 100) && (startRightTracking) && (swipehandTipX != 0))
        //        {
        //            Console.Out.WriteLine("FrameCount " + framecount);
        //            startRightTracking = false;
        //            framecount = 0;
        //            window2.GoNext();
        //        }

        //        if (framecount > 100)
        //        {
        //            startLeftTracking = false;
        //            startRightTracking = false;
        //            framecount = 0;
        //        }

        //        if ((gesture == Gesture.Fist) && (FirstWindow))
        //        {
        //            //senseManager.PauseHand(true);
        //            SecondWindow = true;
        //            _disp.BeginInvoke((Action)(() =>
        //            {
        //                ShowProducts();

        //            }));
        //        }

        //        if ((gesture == Gesture.Thumb) && (SecondWindow))
        //        {
        //            window2.Buy();
        //        }
        //       // Show or hide face marker
        //        //if ((numFacesDetected > 0) && (chkShowFaceMarker.IsChecked == true))
        //        //{
        //        //    // Show face marker
        //        //    rectFaceMarker.Height = faceRectangleHeight;
        //        //    rectFaceMarker.Width = faceRectangleWidth;
        //        //    Canvas.SetLeft(rectFaceMarker, faceRectangleX);
        //        //    Canvas.SetTop(rectFaceMarker, faceRectangleY);
        //        //    rectFaceMarker.Visibility = Visibility.Visible;

        //        //    // Show floating ID label
        //        //    lblFloatingId.Content = String.Format("User ID: {0}", userId);
        //        //    Canvas.SetLeft(lblFloatingId, faceRectangleX);
        //        //    Canvas.SetTop(lblFloatingId, faceRectangleY - 20);
        //        //    lblFloatingId.Visibility = Visibility.Visible;
        //        //}
        //        //else
        //        //{
        //        //    // Hide the face marker and floating ID label
        //        //    rectFaceMarker.Visibility = Visibility.Hidden;
        //        //    lblFloatingId.Visibility = Visibility.Hidden;
        //        //}
        //    }));

        //    // Release resources
        //    bitmap.Dispose();
        //}

        private void LoadDatabaseFromFile()
        {
            if (File.Exists(DatabaseFilename))
            {
                Byte[] buffer = File.ReadAllBytes(DatabaseFilename);
                recognitionConfig.SetDatabaseBuffer(buffer);
                dbState = "Loaded";
            }
            else
            {
                dbState = "Not Found";
            }
        }

        private void SaveDatabaseToFile()
        {
            // Allocate the buffer to save the database
            PXCMFaceData.RecognitionModuleData recognitionModuleData = faceData.QueryRecognitionModule();
            Int32 nBytes = recognitionModuleData.QueryDatabaseSize();
            Byte[] buffer = new Byte[nBytes];

            // Retrieve the database buffer
            recognitionModuleData.QueryDatabaseBuffer(buffer);

            // Save the buffer to a file
            // (NOTE: production software should use file encryption for privacy protection)
            File.WriteAllBytes(DatabaseFilename, buffer);
            dbState = "Saved";
        }

        private void DeleteDatabaseFile()
        {
            if (File.Exists(DatabaseFilename))
            {
                File.Delete(DatabaseFilename);
                dbState = "Deleted";
            }
            else
            {
                dbState = "Not Found";
            }
        }

        private void ReleaseResources()
        {
         
            speechRecognition.StopRec();
            speechRecognition.Dispose();
            audioSource.Dispose();
            session.Dispose();
            // Stop the worker thread
            processingThread.Abort();

            // Release resources
            faceData.Dispose();
            senseManager.Dispose();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            doRegister = true;
        }

        private void btnUnregister_Click(object sender, RoutedEventArgs e)
        {
            doUnregister = true;
        }

        private void btnSaveDatabase_Click(object sender, RoutedEventArgs e)
        {
            SaveDatabaseToFile();
        }

        private void btnDeleteDatabase_Click(object sender, RoutedEventArgs e)
        {
            DeleteDatabaseFile();
        }
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            ReleaseResources();
            this.Close();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ReleaseResources();
        }

        private void registerUser_Click(object sender, RoutedEventArgs e)
        {
            doRegister = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            newUserName = txtUserName.Text;
            doRegister = true;
            pup.IsOpen = false;
        }

        private void pup_Opened(object sender, EventArgs e)
        {
            txtUserName.Text = "";
            newUserName = "";
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
           // _persistentDict.Dispose();
        }

        private void UnReg_Click(object sender, RoutedEventArgs e)
        {
            doUnregister = true;
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            ShowProducts();

        }

        private void ShowProducts()
        {
            SecondWindow = true;
            window2 = new MainWindow2();
            window2._firstWindow = this;
            window2.lblName.Content = lblName.Content;
            window2.ShowDialog();
        }
        //private void ShowProducts()
        //{
        //    if (window2 == null)
        //    {
        //        SecondWindow = true;
        //        window2 = new MainWindow2();
        //        window2._firstWindow = this;
        //        window2.lblName.Content = lblName.Content;
        //        window2.ShowDialog();
        //    }
        //}
    }
}
