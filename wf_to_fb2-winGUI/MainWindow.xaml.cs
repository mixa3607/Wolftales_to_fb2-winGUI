using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using MahApps.Metro.Controls;
using System.Xml.Linq;
using System.IO;
using System.Windows.Media.Animation;
using System.Xml.Serialization;

namespace wf_to_fb2
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public struct Novel
    {
        public string BookName { get; set; }
        public string Url { get; set; }
        public string AuthorFName { get; set; }
        public string AuthorLName { get; set; }
        public string CoverImgB64 { get; set; }
    }
    public partial class MainWindow : MetroWindow
    {
        Storyboard animation_main, animation_sub;
        //int CurrentNovelId;
        //private bool ChEvent = true;
        private bool VolEvent = true;
        private BackgroundWorker GetParseBook, ParseSelected;
        private string CurrentBook;
        private Novel[] Novels;
        private int Method;
        public MainWindow()
        {
            InitializeComponent();


            //init novels
            if (File.Exists("Novels.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Novel[]));
                Stream reader = new FileStream("Novels.xml", FileMode.Open);
                Novels = (Novel[])serializer.Deserialize(reader);
                reader.Close();
            }
            else
            {
                Novels = new Novel[]
                {
                    new Novel
                    {
                        BookName = "Stellar Transformations",
                        Url = "http://wolftales.ru/stellar-transformations",
                        AuthorFName = "IET",
                        AuthorLName = "I Eat Tomatoes",
                        CoverImgB64 = null
                    },
                    new Novel
                    {
                        BookName = "USAW",
                        Url = "http://wolftales.ru/usaw",
                        AuthorFName = "ESC",
                        AuthorLName = "Endless Sea of Clouds",
                        CoverImgB64 = null
                    },
                    new Novel
                    {
                        BookName = "Gam3",
                        Url = "http://wolftales.ru/gam3",
                        AuthorFName = "Cosimo",
                        AuthorLName = "Yap",
                        CoverImgB64 = null
                    },
                    new Novel
                    {
                        BookName = "Lazy Dungeon Master (droped)",
                        Url = "http://wolftales.ru/ldm",
                        AuthorFName = "Onikage",
                        AuthorLName = "Spanner",
                        CoverImgB64 = null
                    }
                };
            }
            


            ListNovels.ItemsSource = Novels;
            ListNovels.SelectedIndex = 0;

            //init animation
            animation_main = (Storyboard)FindResource("RollOnClock");
            animation_sub = (Storyboard)FindResource("RollBackClock");

            //init buttons
            LoadTree.Click += LoadTree_Click;
            SelectAll.Click += SelectAll_Click;
            DeselectAll.Click += DeselectAll_Click;
            ParseChapters.Click += ParseChapters_Click;

            //init parse book thread
            GetParseBook = new BackgroundWorker
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false
            };
            GetParseBook.DoWork += GetParseBook_DoWork;
            GetParseBook.RunWorkerCompleted += GetParseBook_RunWorkerCompleted;

            ParseSelected = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };
            ParseSelected.DoWork += ParseSelected_DoWork;
            ParseSelected.ProgressChanged += ParseSelected_ProgressChanged;
            ParseSelected.RunWorkerCompleted += ParseSelected_RunWorkerCompleted;
        }

        private void ParseChapters_Click(object sender, RoutedEventArgs e)
        {
            ProgressOfParsing.Value = 0;
            ChaptersContainer.IsEnabled = false;
            ParseSelected.RunWorkerAsync(new object[] { ChaptersTree.ItemsSource as ObservableCollection<Vol>, CurrentBook});
        }




        //##################################### THREAD OF PARSE PAGES ################################
        private void ParseSelected_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                var xml = (e.Result as object[])[0] as XDocument;
                var name = (e.Result as object[])[1] as string;
                string offset = "";
                for (int i = 1; File.Exists(name + offset + ".fb2"); i++)
                {
                    offset = " (" + i + ")";
                }
                xml.Save(name + offset + ".fb2");
                MessageBox.Show(this, "Complete. Book name is " + name + offset + ".fb2", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show(this, "Saving error", "error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            ChaptersContainer.IsEnabled = true;
            
        }

        private void ParseSelected_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;
            if (progress == 0)
            {
                ProgressOfParsing.IsIndeterminate = true;
            }
            else
            {
                ProgressOfParsing.IsIndeterminate = false;
                ProgressOfParsing.Value = progress;

            }
        }

        private void ParseSelected_DoWork(object sender, DoWorkEventArgs e)
        {
            var index = (e.Argument as object[])[1] as string;
            var vols = (e.Argument as object[])[0] as ObservableCollection<Vol>;
            var bw = sender as BackgroundWorker;
            int progress = 0;

            string CoverImgB64 = null;
            string FName = "translated by";
            string LName = "Wolftales";

            bw.ReportProgress(progress);
            if (Method == 1)
            {
                
                Dispatcher.Invoke(delegate () {
                    CoverImgB64 = ((Novel)ListNovels.SelectedItem).CoverImgB64;
                    FName = ((Novel)ListNovels.SelectedItem).AuthorFName;
                    LName = ((Novel)ListNovels.SelectedItem).AuthorLName;
                });
            }
            
            string BookName = Parser.Get_BookName(Parser.Get_Page(index));
            
            DateTime time = DateTime.Now;
            var scelet = Parser.MakeFB2Sample(FName, LName, BookName, time, CoverImgB64);


            var vols_xml = new List<XElement>();
            foreach (var vol in vols)
            {
                if (vol.Checked == true || vol.Checked == null)
                {
                    var vol_xml = new XElement("section", new XElement("title", new XElement("p", vol.Text)));
                    foreach (var chapter in vol.Chapters)
                    {
                        if (chapter.Checked && chapter.URL != "")
                        {
                            var html = Parser.Get_Page(chapter.URL);
                            vol_xml.Add(Parser.Parse(html));
                        }
                        progress++;
                        bw.ReportProgress(progress);
                    }
                    vols_xml.Add(vol_xml);
                }
                else
                {
                    progress += vol.Chapters.Count;
                    bw.ReportProgress(progress);
                }
            }
            var result = Parser.Linker(scelet, vols_xml);
            e.Result = new object[] { result, BookName };
        }
        //############################################################################################



        //##################################### THREAD OF PARSE TREE #################################
        private void GetParseBook_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LoadingAnimation.Visibility = Visibility.Collapsed;
            animation_sub.Stop(this);
            animation_main.Stop(this);
            try
            {
                ObservableCollection<Vol> volumes = e.Result as ObservableCollection<Vol>;
                ChaptersTree.ItemsSource = volumes;
                ProgressOfParsing.Maximum = ChaptersCount(volumes);
                NovelList.IsEnabled = true;
            }
            catch
            {
                NovelList.IsEnabled = false;
                ChaptersTree.ItemsSource = null;
                MessageBox.Show("pleace check url and internet connection", "getting page error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            
        }

        private void GetParseBook_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = Parser.Get_Tree(e.Argument as string); //http://wolftales.ru/stellar-transformations
        }
        //############################################################################################





        //################################# EVENTS FROM FORM #########################################

        private void LoadTree_Click(object sender, RoutedEventArgs e)
        {
            if (ByName.IsChecked == true)
            {
                CurrentBook = ((Novel)ListNovels.SelectedItem).Url;
                Method = 1;
            }
            else if (ByLink.IsChecked == true)
            {
                Method = 2;
                CurrentBook = LinkBookBox.Text;
            }
            else
            {
                MessageBox.Show("pleace select mothod", "error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            
            LoadingAnimation.Visibility = Visibility.Visible;
            animation_sub.Begin(this, true);
            animation_main.Begin(this, true);
            GetParseBook.RunWorkerAsync(CurrentBook);
        }

        private void Vol_Checked(object sender, RoutedEventArgs e)
        {
            if (VolEvent)
            {
                foreach (var chapter in ((sender as CheckBox).DataContext as Vol).Chapters)
                {
                    chapter.Checked = true;
                }
            }
            
        }
        private void Vol_Unchecked(object sender, RoutedEventArgs e)
        {
            if (VolEvent)
            {
                foreach (var chapter in ((sender as CheckBox).DataContext as Vol).Chapters)
                {
                    chapter.Checked = false;
                }
            }
            
        }

        //############################################################################################






        //################################## ADDITIONAL FUNCTIONS ####################################
        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            CheckAll(false);
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            CheckAll(true);
        }
        public void CheckAll(bool state)
        {
            var vols = ChaptersTree.ItemsSource as ObservableCollection<Vol>;
            foreach (var vol in vols)
            {
                vol.Checked = state;
                foreach (var chapter in vol.Chapters)
                {
                    chapter.Checked = state;
                }
            }

        }
        private void Ch_Unchecked(object sender, RoutedEventArgs e)
        {
            VolEvent = false;
            var vol = ChaptersTree.Items[((sender as CheckBox).DataContext as Chapter).InVolume] as Vol;
            foreach (var chapter in vol.Chapters)
            {
                if (chapter.Checked)
                {
                    vol.Checked = null;
                    break;
                }
                vol.Checked = false;
            }
            VolEvent = true;
        }

        

        private void Ch_Checked(object sender, RoutedEventArgs e)
        {
            VolEvent = false;
            var vol = ChaptersTree.Items[((sender as CheckBox).DataContext as Chapter).InVolume] as Vol;
            foreach (var chapter in vol.Chapters)
            {
                if (!chapter.Checked)
                {
                    vol.Checked = null;
                    break;
                }
                vol.Checked = true;
            }
            VolEvent = true;
        }

        public int ChaptersCount(ObservableCollection<Vol> volumes)
        {
            int ch_count = 0;
            foreach (var vol in volumes)
            {
                ch_count += vol.Chapters.Count;
            }
            return ch_count;
        }
    }
}
