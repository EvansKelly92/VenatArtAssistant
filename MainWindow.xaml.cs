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

using System.Runtime.Serialization;
using Image = Microsoft.UI.Xaml.Controls.Image;

using System.Drawing.Imaging;

//using System.Windows.Media.Imaging;
using System.Threading.Tasks;

using System.Security.Cryptography.X509Certificates;


using System.Reflection;
//using  Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using FontFamily = Microsoft.UI.Xaml.Media.FontFamily;
using Microsoft.UI.Windowing;







// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

//All code that interacts with the window is stored here. I hate it but I'm not going to spend time 
//figuring out how to make it cleaner because deadline.


//things to do for timer
//Auto Save function
//Overall work time displayed

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

        Point currMouse;
        Point lastMouse;


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

        List<WIP> wipList = new List<WIP>();
        public class WIP : List<WIP>
        {
            public string name;
            public string filePath;
            public List<string> tags = new List<string>();

        }
     
        List <TextBlock> tagTextBlockList = new List<TextBlock>();
        List<TextBox> tagBoxList = new List<TextBox>();
        public void FileHandle()
        {
            //wipPath will need to be inputted by user
            string wipPath = @"C:\Users\evans\OneDrive\Pictures\Pictures\Pictures";
            string[] files = Directory.GetFiles(wipPath);



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

                  
                    WIP wip = new WIP();
                    wip.name = name;
                    wip.filePath = file;

                    wipList.Add(wip);

                    StackPanel stackPanel = new StackPanel();
                    stackPanel.Name = wip.name+"STK";
                    stackPanel.Background = new SolidColorBrush(Colors.WhiteSmoke);
                    stackPanel.Margin = new Thickness(10);
                    stackPanel.Orientation = Orientation.Vertical;
                    stackPanel.VerticalAlignment = VerticalAlignment.Stretch;
                    Panel.Children.Add(stackPanel);

                    textBlock.Text = wip.name;
                    textBlock.Name = wip.name + "TXTBOX";
                    textBlock.Foreground = new SolidColorBrush(Colors.DimGray);
                    textBlock.FontFamily = new FontFamily("Calibri");
                    textBlock.Margin = new Thickness(5);

                    //these two don't actually do anything right now
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    textBlock.FontSize = 16;

                    stackPanel.Children.Add(textBlock);

                    TextBlock tb = new TextBlock();
                    tb.Text = "";
                    tb.Name = wip.name + "TAGS";
                    tb.Foreground = new SolidColorBrush(Colors.DarkCyan);
                    tb.FontFamily = new FontFamily("Calibri");
                    tb.FontSize = 14;
                    tb.Margin = new Thickness(5);
                    tagTextBlockList.Add(tb);
                    stackPanel.Children.Add(tb);

                    fileName = wip.name;

                    StackPanel sp = new StackPanel();
                    sp.Orientation = Orientation.Horizontal;
                    sp.Margin = new Thickness(5);
                    stackPanel.Children.Add(sp);

                    TextBox tbx = new TextBox();
                    tbx.Name = fileName + "TB";
                    tbx.PlaceholderText = "Add a tag";
                    tbx.Background = new SolidColorBrush(Colors.DimGray);
                    tbx.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
                    tagBoxList.Add(tbx);
                    sp.Children.Add(tbx);

                    Button button = new Button();
                    button.Content = "+";
                    button.Name = fileName;
                    button.Click += AddTag;
                    button.Background = new SolidColorBrush(Colors.DimGray);
                    button.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
                    sp.Children.Add(button);

                    Button butt = new Button();
                    butt.Content = "X";
                    butt.Name = fileName + "DEL";
                    butt.Click += PopupDel;
                    butt.Background = new SolidColorBrush(Colors.DimGray);
                    butt.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
                    sp.Children.Add(butt);

                }
            }

        }


        private void AddTag(object sender, RoutedEventArgs e)
        {
         
            string wipName = ((Button)sender).Name.ToString();
            int item = wipList.FindIndex(o => o.name == wipName);
            
            string boxName = wipName + "TB";
            int tagBoxItem = tagBoxList.FindIndex(o => o.Name == boxName);

            string tagItem = tagBoxList.ElementAt(tagBoxItem).Text;
            wipList.ElementAt(item).tags.Add(tagItem);
            tagBoxList.ElementAt(tagBoxItem).Text = "";

            UpdateTagBlock(wipName, item);
        }


        private void UpdateTagBlock(string buttName, int index)
        {
            string tagBoxName = buttName + "TAGS";
            int uiNumber = tagTextBlockList.FindIndex(o => o.Name == tagBoxName);

            tagTextBlockList.ElementAt(uiNumber).Text = "";

            if (wipList.ElementAt(index).tags.Count > 0)
            {

                for (int i = 0; i < wipList.ElementAt(index).tags.Count; i++)
                {
                    string tag = wipList.ElementAt(index).tags.ElementAt(i).ToString();

                    tagTextBlockList.ElementAt(uiNumber).Text = tagTextBlockList.ElementAt(uiNumber).Text + tag + "\n";
                }
            }
            else
            {
                
            }
        }

        private void PopupDel(object sender, RoutedEventArgs e)
        {
            if (!pop.IsOpen) { 
               pop.IsOpen = true; 
                //add a list
            }
        }

        private void ReturnDel(object sender, RoutedEventArgs e)
        {
            //delete shitfrom popup
            pop.IsOpen = false;
        }
    }
}