using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;

using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

using Windows.Storage;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Drawing;

using System.Runtime.Serialization;
using Image = Microsoft.UI.Xaml.Controls.Image;
using NPOI.Util;
using System.Drawing.Imaging;
using Microsoft.WindowsAPICodePack.Shell;
//using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

//All code that interacts with the window is stored here. I hate it but I'm not going to spend time 
//figuring out how to make it cleaner because deadline.


//things to do for files
//get file icon
//same them in object
//tag them
//see if they can open in their default program

//Things to do for flow
//remember what irl hours the user best works

namespace VenatArtAssistant
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void test_Click(object sender, RoutedEventArgs e)
        {
            FileHandle();
        }


        //!!
        //Save and load zone
        //!!

        ApplicationDataContainer userSettings = ApplicationData.Current.LocalSettings;

        private void SaveData()
        {
            userSettings.Values["totalTime"] = totalTime;
        }

        private void LoadData()
        {
            if (userSettings.Values.ContainsKey("totalTime"))
            {
                totalTime = (TimeSpan)userSettings.Values["totalTime"];
                TotalTimeLog.Text = "Total Session Time: " + totalTime.ToString();
            }
        }

        //!!
        //Timer zone for tracking time
        //!!
        DispatcherTimer dispatcherTimer;
        DateTimeOffset startTime;
        DateTimeOffset lastTime;
        DateTimeOffset stopTime;
        TimeSpan span;
        TimeSpan totalTime;

        bool timing = false;

        //checks every 10 seconds if the mouse has moved
        int mouseCheck = 10;
        int mouseTick = 0;
        int mouseTimeOutCount = 0;
        //will time out after 5 minutes. Timeout is in seconds devided by 10.
        public int mouseTimeout = 30;

        Windows.Foundation.Point currMouse;
        Windows.Foundation.Point lastMouse;


        private void SessionToggleButton_Click(object sender, RoutedEventArgs e)
        {
            MouseTracker MT = new MouseTracker();
            currMouse = MT.GetCursorPosition();
            timeToggle();
        }

        public void timeToggle()
        {
            if (timing)
            {
                SessionToggle.Content = "Start Session";
                timing = false;
            }
            else
            {

                SessionToggle.Content = ("Stop Session");
                timing = true;
                DispatcherTimerSetup();
            }
        }

        public void DispatcherTimerSetup()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;

            //Timespan is recording in seconds, it is like (hours, minutes, seconds)
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);

            TimerLog.Text = "Recording session";

            startTime = DateTimeOffset.Now;
            lastTime = startTime;
            span = oldTime;
            mouseTick = 0;

            dispatcherTimer.Start();

        }

        void dispatcherTimer_Tick(object sender, object e)
        {
            DateTimeOffset time = DateTimeOffset.Now;
            span = time - lastTime;
            lastTime = time;

            mouseTick++;

            if (mouseTick == mouseCheck)
            {
                MouseTracker MT = new MouseTracker();
                lastMouse = currMouse;
                currMouse = MT.GetCursorPosition();

                if (currMouse != lastMouse)
                {
                    mouseTimeOutCount = 0;
                }
                else
                {
                    mouseTimeOutCount++;

                    if (mouseTimeOutCount == mouseTimeout)
                    {
                        stopTime = time;
                        dispatcherTimer.Stop();
                        span = (stopTime - startTime) + span;
                        waitTimerSetup();
                    }
                }
                mouseTick = 0;
            }


            if (timing == false)
            {
                stopTime = time;
                dispatcherTimer.Stop();
                span = (stopTime - startTime) + span;
                TimerLog.Text = "Time spent this session: " + span.ToString() + "\n";
                totalTime = totalTime + span;
                TotalTimeLog.Text = "Total Session Time: " + totalTime.ToString();
                SaveData();
            }


        }

        //!!
        // Popup for seeing if you are still working
        //!!
        DispatcherTimer waitTimer;
        bool waiting = false;
        int confTime;
        //2 Minutes in seconds
        int confTimeOut = 120;
        TimeSpan oldTime;

        public void waitTimerSetup()
        {
            waitTimer = new DispatcherTimer();
            waitTimer.Tick += waitTimer_Tick;

            oldTime = span;
            waiting = true;

            //Timespan is recording in seconds, it is like (hours, minutes, seconds)
            waitTimer.Interval = new TimeSpan(0, 0, 1);

            ContinueButton.Visibility = Visibility.Visible;
            confTime = 0;
            toastReminder();
            waitTimer.Start();

        }

        void waitTimer_Tick(object sender, object e)
        {
            confTime++;
            TimerLog.Text = span.ToString() + "\n";
            if ((confTime >= confTimeOut) && waiting == true)
            {
                waitTimer.Stop();

                TimerLog.Text = "Time spent this session: " + span.ToString() + "\n";
                totalTime = totalTime + span;
                TotalTimeLog.Text = "Total Session Time: " + totalTime.ToString();
                SaveData();

                span = TimeSpan.Zero;
                oldTime = TimeSpan.Zero;
                waiting = false;
                ContinueButton.Visibility = Visibility.Collapsed;
                timeToggle();
            }

            //user confirms to keep working
            else if (waiting == false)
            {
                ContinueButton.Visibility = Visibility.Collapsed;
                waitTimer.Stop();
                DispatcherTimerSetup();
            }
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            waiting = false;
        }

        //!!
        //Toast Zone
        //!!
        public void toastReminder()
        {
            var content = new ToastContent
            {
                Launch = "...",
                ActivationType = ToastActivationType.Background,
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
            {
                new AdaptiveText()
                {
                    Text = "Are you still working?"
                },
                new AdaptiveText()
                {
                    Text = "Return to the app to continue"
                }
            }
                    }

                }
            };
            var notifier = ToastNotificationManager.CreateToastNotifier();
            var notification = new ToastNotification(content.GetXml());
            notifier.Show(notification);
        }

        //!!
        //File zone
        //!!

        string fileName;

        public void FileHandle()
        {
            //wipPath will need to be inputted by user
            string wipPath = @"C:\Users\evans\OneDrive\Pictures";
            string[] files = Directory.GetFiles(wipPath);
            TestText.Text = null;

         

            foreach (string file in files)
            {
                var fInfo = new FileInfo(file);

                if (Directory.Exists(file) || fInfo.Attributes.HasFlag(System.IO.FileAttributes.Hidden))
                {
                    //skip
                }
                else
                {
                    TextBlock textBlock = new TextBlock();
                    String name = System.IO.Path.GetFileName(file);
                    textBlock.Text = name;
                    textBlock.Name = name;
                    Panel.Children.Add(textBlock);

                    fileName = name;
                    AddButton();
                   
                }
            }

        }

        public void AddButton()
        {
             Button button = new Button();
             button.Content = "+";
             button.Name = fileName + "BTN";
             button.Click += AddTag;
             Panel.Children.Add(button);
        }


        private void AddTag(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Content = "ASS";
        }

    }
}
 