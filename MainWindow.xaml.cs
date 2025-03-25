using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Windows.Storage;
using Microsoft.UI;
using FontFamily = Microsoft.UI.Xaml.Media.FontFamily;
using static VenatArtAssistant.MainWindow;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.ComponentModel.Design.Serialization;
using System.Xml;
using NPOI.SS.Formula.Functions;
using System.Windows.Documents;
using System.Security.Policy;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


//All code that interacts with the window is stored here. I hate it but I'm not going to spend time 
//figuring out how to make it cleaner because deadline.


//Things to do for flow
//remember what irl hours the user best works
//remember best tags
//ask after session which file they worked on
//show best hours/tags

//save load

//save and load best working time and best tags

//other
//cleanup
//export

namespace VenatArtAssistant
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            LoadData();
        }


        //button for loading new files
        private void path_Click(object sender, RoutedEventArgs e)
        {
            string wPath = pathBOX.Text;
            FileHandle(wPath);

        }

        //!!
        //Save and load zone
        //!!

        ApplicationDataContainer userSettings = ApplicationData.Current.LocalSettings;

        private void SaveData()
        {
            userSettings.Values["totalTime"] = totalTime;
            userSettings.Values["wips"] = savedStuff;

            string json = JsonConvert.SerializeObject(wipList, Newtonsoft.Json.Formatting.Indented);
            userSettings.Values["list"] = json;

            
        }

   
        private void LoadData()
        {
            if (userSettings.Values.ContainsKey("totalTime"))
            {
                totalTime = (TimeSpan)userSettings.Values["totalTime"];
                TotalTimeLog.Text = "Total Session Time: " + totalTime.ToString();
            }

            savedStuff = (bool)userSettings.Values["wips"];

            if (savedStuff == true)
            {
                Object value = userSettings.Values["list"];
                wipList = JsonConvert.DeserializeObject<List<WIP>>(value.ToString());
                AddWipStack();
            }
        }

        public static T ReadFromJsonFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                var fileContents = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(fileContents);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
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
                startHour = CheckLocalTime();
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
                endHour = CheckLocalTime();
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
                endHour = CheckLocalTime();
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

        //Hour Save Zone
        //
        public int startHour;
        public int endHour;

        public int CheckLocalTime()
        {
            DateTime now = DateTime.Now;
            int currHour = now.Hour;
            return currHour;
        }

        //one for each hour
        public class HourScore
        {
            public int h00 { get; set; }
            public int h01 { get; set; }
            public int h02 { get; set; }
            public int h03 { get; set; }
            public int h04 { get; set; }
            public int h05 { get; set; }
            public int h06 { get; set; }
            public int h07 { get; set; }
            public int h08 {  get; set; }
            public int h09 { get; set; }
            public int h10 { get; set; }
            public int h11 { get; set; }
            public int h12 { get; set; }
            public int h13 { get; set; }
            public int h14 { get; set; }
            public int h15 { get; set; }
            public int h16 { get; set; }
            public int h17 { get; set; }
            public int h18 { get; set; }
            public int h19 { get; set; }
            public int h20 { get; set; }
            public int h21 { get; set; }
            public int h22 { get; set; }
            public int h23 { get; set; }
            public int h24 { get; set; }
        }

        //!!
        //File zone
        //!!

        string fileName;

        List<WIP> wipList = new List<WIP>();

        public class WIP 
        {
            // public string FileSavePath = @"C:\Users\evans\Source\Repos\VenatArtAssistant1\save.txt";
            public string name { get; set; }
            public string filePath { get; set; }
            public List<string> tags = new List<string>();

        }
     
        List <TextBlock> tagTextBlockList = new List<TextBlock>();
        List<TextBox> tagBoxList = new List<TextBox>();

        List<StackPanel> wipStacksList = new List<StackPanel>();

        bool savedStuff = false;

        public void FileHandle(string wPath)
        {
            //wipPath will need to be inputted by user
            string wipPath = @wPath;
            try
            {
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
                        stackPanel.Name = wip.name + "STK";
                        stackPanel.Background = new SolidColorBrush(Colors.LightBlue);
                        stackPanel.Margin = new Thickness(50, 10, 10, 10);
                        stackPanel.Orientation = Orientation.Vertical;
                        stackPanel.MinHeight = 200;
                        stackPanel.VerticalAlignment = VerticalAlignment.Stretch;
                        Panel.Children.Add(stackPanel);
                        wipStacksList.Add(stackPanel);

                        textBlock.Text = wip.name;
                        textBlock.Name = wip.name + "TXTBOX";
                        textBlock.Foreground = new SolidColorBrush(Colors.DarkSlateGray);
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


                        savedStuff = true;
                        

                    }
                }
            }

            catch (System.IO.DirectoryNotFoundException)
            {
                return;
            }

            
            SaveData();
        }

        public void AddWipStack()
        {
            for (int i = 0; i < wipList.Count; i++)
            {
                TextBlock textBlock = new TextBlock();

                StackPanel stackPanel = new StackPanel();
                stackPanel.Name = wipList.ElementAt(i).name + "STK";
                stackPanel.Background = new SolidColorBrush(Colors.LightBlue);
                stackPanel.Margin = new Thickness(50, 10, 10, 10);
                stackPanel.Orientation = Orientation.Vertical;
                stackPanel.MinHeight = 200;
                stackPanel.VerticalAlignment = VerticalAlignment.Stretch;
                Panel.Children.Add(stackPanel);
                wipStacksList.Add(stackPanel);

                textBlock.Text = wipList.ElementAt(i).name;
                textBlock.Name = wipList.ElementAt(i).name + "TXTBOX";
                textBlock.Foreground = new SolidColorBrush(Colors.DarkSlateGray);
                textBlock.FontFamily = new FontFamily("Calibri");
                textBlock.Margin = new Thickness(5);

                //these two don't actually do anything right now
                textBlock.TextWrapping = TextWrapping.Wrap;
                textBlock.FontSize = 16;

                stackPanel.Children.Add(textBlock);

                TextBlock tb = new TextBlock();
                if (wipList.ElementAt(i).tags.Count > 0)
                {
                    for (int j = 0; j < wipList.ElementAt(i).tags.Count; j++)
                    {
                        tb.Text = tb.Text + wipList.ElementAt(i).tags.ElementAt(j) + "\n";
                    }
                }
                else
                {
                    tb.Text = "";
                }
                tb.Name = wipList.ElementAt(i).name + "TAGS";
                tb.Foreground = new SolidColorBrush(Colors.DarkCyan);
                tb.FontFamily = new FontFamily("Calibri");
                tb.FontSize = 14;
                tb.Margin = new Thickness(5);
                tagTextBlockList.Add(tb);
                stackPanel.Children.Add(tb);

                fileName = wipList.ElementAt(i).name;

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

            SaveData();

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


        //!!
        //deleting zone
        //!!

        List<CheckBox> stuffInPop = new List<CheckBox>();
        private void PopupDel(object sender, RoutedEventArgs e)
        {
            if (!pop.IsOpen)
            {
                pop.IsOpen = true;
                //add a list
                string buttName = ((Button)sender).Name.ToString();
                string wipName = buttName.Remove(buttName.Length - 3);
                CheckBox checkBox = new CheckBox();
                checkBox.Content = wipName;
                checkBox.Foreground = new SolidColorBrush(Colors.AliceBlue);
                checkBox.Margin = new Thickness(5);
                checkBox.Checked += wipChecked;
                checkBox.Unchecked += wipUnchecked;
                PopPanel.Children.Add(checkBox);

                stuffInPop.Add(checkBox);

                int index = wipList.FindIndex(o => o.name == wipName);
                if (wipList.ElementAt(index).tags.Count > 0)
                {
                    for (int i = 0; i < wipList.ElementAt(index).tags.Count; i++)
                    {
                        CheckBox cb = new CheckBox();
                        string tag = wipList.ElementAt(index).tags.ElementAt(i).ToString();
                        cb.Content = tag;
                        cb.Foreground = new SolidColorBrush(Colors.AliceBlue);
                        cb.Margin = new Thickness(5);
                        PopPanel.Children.Add(cb);

                        stuffInPop.Add(cb);

                    }
                }

            }
        }

        private void ReturnDel(object sender, RoutedEventArgs e)
        {

            pop.IsOpen = false;
            PopPanel.Children.Clear();
            stuffInPop.Clear();
        }

        private void wipChecked(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < stuffInPop.Count; i++) {
                stuffInPop.ElementAt(i).IsChecked = true;
                 }
        }

        private void wipUnchecked(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < stuffInPop.Count; i++)
            {
                stuffInPop.ElementAt(i).IsChecked = false;
            }
        }

        private void DeleteDel(object sender, RoutedEventArgs e)
        {
            pop.IsOpen = false;
            string objName = stuffInPop.ElementAt(0).Content.ToString();
            int index = wipList.FindIndex(o => o.name == objName);

            int numOfDeletedTags = 0;

            for (int i = 0; i < stuffInPop.Count; i++)
            {
               
                    if (stuffInPop.ElementAt(0).IsChecked == true)
                    {
                        string stackName = objName + "STK";
                        int stackIndex = wipStacksList.FindIndex(o => o.Name == stackName);

                        Panel.Children.RemoveAt(stackIndex);
                        wipStacksList.RemoveAt(stackIndex);

                        wipList.RemoveAt(index);
                        break;
                    }
              
                    else if (stuffInPop.ElementAt(i).IsChecked == true)
                    {
                        wipList.ElementAt(index).tags.RemoveAt(i - 1 - numOfDeletedTags);
                         numOfDeletedTags++;
                    } 
               
            }
                PopPanel.Children.Clear();
                stuffInPop.Clear();
            RefreshList(objName, index);

            SaveData();

        }

        private void RefreshList(string name, int index)
        {
            string tagBoxName = name + "TAGS";
            int tagIndex = tagTextBlockList.FindIndex(o => o.Name == tagBoxName);

            tagTextBlockList.ElementAt(tagIndex).Text = "";
            if (wipList.ElementAt(index).tags.Count > 0)
            {
                for (int i = 0; i < wipList.ElementAt(index).tags.Count; i++)
                {
                    string tag = wipList.ElementAt(index).tags.ElementAt(i).ToString();
                   tagTextBlockList.ElementAt(tagIndex).Text = tagTextBlockList.ElementAt(tagIndex).Text + tag + "\n";
                }
            }

        }
    }
}