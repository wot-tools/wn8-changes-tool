using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
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

namespace WN8_Stat_Changes
{
    public partial class MainWindow : Window
    {
        public List<Tank> Tanks;
        public List<Tank> ExpectedValues_New;
        public List<Tank> ExpectedValues_Old;
        public List<FilterItem> Tiers;
        public List<FilterItem> Types;
        public List<FilterItem> Nations;
        public WN8Item WN8_Old { get; set; }
        public WN8Item WN8_New { get; set; }
        public WN8Item PlayerTotal { get; set; }
        public WN8Item PlayerTotalOld { get; set; }
        public WN8Item PlayerTotalNew { get; set; }
        BackgroundWorker worker;
        string playerName;
        string clanTag;
        BitmapImage clanLogo;
        Dictionary<string, string> expectedValuesVersions;
        Dictionary<string, string> Servers;
        string oldVersion;
        string newVersion;
        string serverID;
        bool mlgChecking;
        private DataGrid currentDataGrid;

        #region window
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            gridOverallChanges.Visibility = Visibility.Collapsed;

            clanLogo = new BitmapImage();

            expectedValuesVersions = GetExpectedValuesVersions();
            Servers = GetServersDict();
            FillFilterDataGrids();

            comboBoxServer.ItemsSource = Servers;
            comboBoxServer.SelectedIndex = 0;

            comboBoxNewVersion.ItemsSource = expectedValuesVersions;
            comboBoxNewVersion.SelectedIndex = expectedValuesVersions.Count - 1;
            comboBoxOldVersion.ItemsSource = expectedValuesVersions;
            comboBoxOldVersion.SelectedIndex = comboBoxNewVersion.SelectedIndex - 1;
            Title += " " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void DelayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            AddFilter();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TB_PlayerName.Focus();

            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && playerName != null)
            {
                DG_Tanks.ItemsSource = Tanks;
                //DG_Tanks.Items.SortDescriptions.Clear();
                //DG_Tanks.Items.SortDescriptions.Add(new SortDescription("WN8.Battles", ListSortDirection.Descending));

                gridOverallChanges.Visibility = Visibility.Visible;
                ExpanderMOEDetails.Visibility = Visibility.Visible;
                SetOverallStuff(playerName);
                FillMOEFilterDataGrids();
            }

            gridOverlay.Visibility = Visibility.Collapsed;
            AddFilter();
            DG_Tanks.Items.SortDescriptions.Add(new SortDescription("WN8.Battles", ListSortDirection.Descending));
        }

        private Dictionary<string, string> GetServersDict()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            dict.Add(WN8Static.ServerIDEU, "EU");
            dict.Add(WN8Static.ServerIDNA, "NA");
            dict.Add(WN8Static.ServerIDASIA, "ASIA");
            dict.Add(WN8Static.ServerIDRU, "RU");

            return dict;
        }

        private void FillFilterDataGrids()
        {
            Tiers = new List<FilterItem>();
            for (int i = 1; i < 11; i++)
            {
                Tiers.Add(new FilterItem(i.ToString(), i.ToString()));
            }

            dataGridTier.ItemsSource = Tiers;

            Types = new List<FilterItem>();
            Types.Add(new FilterItem("Light Tank", "lightTank"));
            Types.Add(new FilterItem("Medium Tank", "mediumTank"));
            Types.Add(new FilterItem("Heavy Tank", "heavyTank"));
            Types.Add(new FilterItem("Tank Destroyer", "AT-SPG"));
            Types.Add(new FilterItem("SPG", "SPG"));

            dataGridType.ItemsSource = Types;

            Nations = new List<FilterItem>();

            Nations.Add(new FilterItem("U.S.A.", "usa"));
            Nations.Add(new FilterItem("Czechoslovakia", "czech"));
            Nations.Add(new FilterItem("France", "france"));
            Nations.Add(new FilterItem("U.S.S.R.", "ussr"));
            Nations.Add(new FilterItem("China", "china"));
            Nations.Add(new FilterItem("U.K.", "uk"));
            Nations.Add(new FilterItem("Japan", "japan"));
            Nations.Add(new FilterItem("Germany", "germany"));
            Nations.Add(new FilterItem("Sweden", "sweden"));

            Nations = Nations.OrderBy(x => x.Name).ToList();

            dataGridNation.ItemsSource = Nations;
        }

        private void FillMOEFilterDataGrids()
        {
            //dataGridTier.ItemsSource = null;
            dataGridTierMOE3.ItemsSource = Tiers.Where(x => Convert.ToDouble(x.ID) >= 5);
            //dataGridTier.ItemsSource = null;
            dataGridTypeMOE3.ItemsSource = Types;
            //dataGridTier.ItemsSource = null;
            dataGridNationMOE3.ItemsSource = Nations;
        }

        private void ShowErrorMessageBox(string text, string caption)
        {
            MessageBox.Show(text,caption, MessageBoxButton.OK, MessageBoxImage.Error);
            Dispatcher.Invoke(() => TB_PlayerName.Focus());
            Dispatcher.Invoke(() => TB_PlayerName.CaretIndex = TB_PlayerName.Text.Length);
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            tbOverlayText.Text = e.UserState.ToString();
        }

        private string GetServerID()
        {
            return comboBoxServer.SelectedValue as string;
        }

        private string GetAPIKey(string serverID)
        {
            if (serverID == WN8Static.ServerIDEU)
                return "c3709917cc33d9b0845aa3b5d7bebc13";
            else if (serverID == WN8Static.ServerIDNA)
                return "4ce1dae156486643ada2b40a0a7479af";
            else if (serverID == WN8Static.ServerIDASIA)
                return "689b7d3873b114411e3e047411c1ced9";
            else if (serverID == WN8Static.ServerIDRU)
                return "489d6c81b95e446235f6414aa672bc58";

            return "";
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string playerid = "";
            
            string playername = e.Argument.ToString();
            string appID = GetAPIKey(serverID);

            worker.ReportProgress(0, "Getting Player ID");
            string requesturl = $@"wot/account/list/?application_id={appID}&search=" + playername;

            try
            {
                string json = GetStringFromAPI(requesturl, serverID);

                JObject jo = JObject.Parse(json);

                foreach (JObject jplayer in jo["data"].Children<JObject>())
                {
                    if (String.Equals(playername, jplayer["nickname"].ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        playerid = jplayer["account_id"].ToString();
                        playername = jplayer["nickname"].ToString();
                        playerName = playername;
                    }
                }
            }
            catch
            {
                ShowErrorMessageBox(String.Format("An Error occured. Could not get wn8 changes for player \"{0}\"", playername), "Error!");
                e.Cancel = true;
                return;
            }

            if (!String.IsNullOrEmpty(playerid))
            {
                try
                {

                    worker.ReportProgress(0, String.Format("Getting {0}'s account info", playername));
                    HandlePlayerClan(playerid, appID, serverID);
                    worker.ReportProgress(0, "Getting expected values");
                    //if (ExpectedValues_Old == null)
                    ExpectedValues_Old = GetExpectedValues(oldVersion);

                    //if (ExpectedValues_New == null)
                    ExpectedValues_New = GetExpectedValues(newVersion);

                    worker.ReportProgress(0, String.Format("Getting {0}'s vehicles", playername));
                    GenerateTankList(playerid, appID, serverID);
                    worker.ReportProgress(0, "Calculating wn8");
                    CalcWN8();
                    worker.ReportProgress(0, "Getting tank information");
                    SetTankNames(appID, serverID);
                    worker.ReportProgress(0, "Getting Marks of Excellence");
                    GetMarksOfExcellenceOfPlayer(playerid, appID, serverID);
                    worker.ReportProgress(0, "Getting Marks of Excellence Details");
                    GatherMOEDetailData();
                }
                catch
                {
                    ShowErrorMessageBox(String.Format("An Error occured. Could not get wn8 changes for player \"{0}\"", playername), "Error!");
                    e.Cancel = true;
                    return;
                }
            }
            else
            {
                ShowErrorMessageBox(String.Format("Could not find player \"{0}\"", playername), "Error!");
                e.Cancel = true;
                return;
            }
        }
        #endregion

        private void GetMarksOfExcellenceOfPlayer(string playerID, string appID, string serverID)
        {
            string requesturl = $@"https://api.worldoftanks.{GetURLSuffix(serverID)}/wot/tanks/achievements/?application_id={appID}&fields=tank_id%2Cachievements&account_id={playerID}";

            WebClient wc = new WebClient
            {
                Proxy = null,
                Encoding = Encoding.UTF8
            };

            JObject jo = JObject.Parse(wc.DownloadString(requesturl));

            foreach(JToken token in jo["data"][playerID].Children())
            {
                JObject jTank = JObject.Parse(token.ToString());

                if (jTank["achievements"]["marksOnGun"] != null)
                {
                    Tank t = new Tank();
                    double marks = Convert.ToDouble(jTank["achievements"]["marksOnGun"]);
                    string tankID = jTank["tank_id"].ToString();

                    if (Tanks.Any(x => x.ID == tankID))
                    {
                        Tanks.First(x => x.ID == tankID).MarksOfExcellence = marks;
                    }
                    else
                    {

                    }
                }
            }
        }

        private void ResetFilterItem(FilterItem fItem)
        {
            fItem.MOE0 = 0;
            fItem.MOE1 = 0;
            fItem.MOE2 = 0;
            fItem.MOE3 = 0;
        }

        private void GatherMOEDetailData()
        {
            Tiers.ForEach(x => ResetFilterItem(x));
            Types.ForEach(x => ResetFilterItem(x));
            Nations.ForEach(x => ResetFilterItem(x));

            foreach (Tank t in Tanks)
            {
                if (Tiers.Any(x => x.ID == t.Tier.ToString()))
                {
                    FilterItem fItem = Tiers.First(x => x.ID == t.Tier.ToString());

                    fItem.MOE0 += t.MarksOfExcellence == 0 ? 1 : 0;
                    fItem.MOE1 += t.MarksOfExcellence == 1 ? 1 : 0;
                    fItem.MOE2 += t.MarksOfExcellence == 2 ? 1 : 0;
                    fItem.MOE3 += t.MarksOfExcellence == 3 ? 1 : 0;
                }

                if (Types.Any(x => x.ID == t.TypeID))
                {
                    FilterItem fItem = Types.First(x => x.ID == t.TypeID);

                    fItem.MOE0 += t.MarksOfExcellence == 0 ? 1 : 0;
                    fItem.MOE1 += t.MarksOfExcellence == 1 ? 1 : 0;
                    fItem.MOE2 += t.MarksOfExcellence == 2 ? 1 : 0;
                    fItem.MOE3 += t.MarksOfExcellence == 3 ? 1 : 0;
                }

                if (Nations.Any(x => x.ID == t.NationID))
                {
                    FilterItem fItem = Nations.First(x => x.ID == t.NationID);

                    fItem.MOE0 += t.MarksOfExcellence == 0 ? 1 : 0;
                    fItem.MOE1 += t.MarksOfExcellence == 1 ? 1 : 0;
                    fItem.MOE2 += t.MarksOfExcellence == 2 ? 1 : 0;
                    fItem.MOE3 += t.MarksOfExcellence == 3 ? 1 : 0;
                }
            }
        }

        private Dictionary<string,string> GetExpectedValuesVersions()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            for (int i = 15; i <= GetNewestExpectedValuesVersion(); i++)
            {
                dict.Add(i.ToString(), "v" + i.ToString());
            }

            return dict;
        }

        private void BT_GetPlayerStats_Click(object sender, RoutedEventArgs e)
        {
            GetWN8ChangesForPlayer();
        }

        private void TB_PlayerName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                GetWN8ChangesForPlayer();
            }
        }

        private void SetOverallStuff(string playername)
        {         
            if(!String.IsNullOrEmpty(clanTag))
            {
                tbPlayerName.Text = String.Format("{0} [{1}]", playername, clanTag);

                try
                {
                    if (File.Exists(String.Format("{0}/clanLogo.png", AppDomain.CurrentDomain.BaseDirectory)))
                    {
                        imgClanLogo.Source = null;
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.CacheOption = BitmapCacheOption.OnLoad; //WICHTIG!!!!
                        img.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        img.UriSource = new Uri(String.Format("{0}/clanLogo.png", AppDomain.CurrentDomain.BaseDirectory));
                        img.EndInit();
                        //img.Freeze();
                        imgClanLogo.Source = img;

                        //imgClanLogo.Source = new BitmapImage(new Uri(String.Format("{0}/clanLogo.png", AppDomain.CurrentDomain.BaseDirectory)));
                        imgClanLogo.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        imgClanLogo.Visibility = Visibility.Collapsed;
                    }
                }
                catch
                {
                    MessageBox.Show("Could not load clan logo!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    imgClanLogo.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                tbPlayerName.Text = playername;
                imgClanLogo.Visibility = Visibility.Collapsed;
            }

            tbNewRating.Text = PlayerTotal.WN8_New.ToString("N0");
            tbNewRating.Foreground = (SolidColorBrush)(new WN8ColorConverter()).Convert(PlayerTotal.WN8_New, null, null, null);

            tbOldRating.Text = PlayerTotal.WN8.ToString("N0");
            tbOldRating.Foreground = (SolidColorBrush)(new WN8ColorConverter()).Convert(PlayerTotal.WN8, null, null, null);

            tbRatingChange.Text = String.Format("{0}{1:N0}", PlayerTotal.Change > 0 ? "+" : "", PlayerTotal.Change);

            tbOldVersion.Text = expectedValuesVersions[oldVersion];
            tbNewVersion.Text = expectedValuesVersions[newVersion];

            tbRatingChange.Foreground = GetChangeBrush(PlayerTotal.Change);

            tbVersions.Text = String.Format("{0} → {1}", expectedValuesVersions[oldVersion], expectedValuesVersions[newVersion]);

            tbMarksOfExellence3.Text = GetMarksOfExcellenceText(3);
            tbMarksOfExellence2.Text = GetMarksOfExcellenceText(2);
            tbMarksOfExellence1.Text = GetMarksOfExcellenceText(1);
            tbMarksOfExellence0.Text = GetMarksOfExcellenceText(0);
        }

        private string GetMarksOfExcellenceText(double marksCount)
        {
            //return String.Format("{0}x {1} Marks of Excellence", Tanks.Count(x => x.MarksOfExcellence == marksCount), marksCount);
            double tankCount = Tanks.Count(x => x.MarksOfExcellence == marksCount && x.Tier >= 5);

            return String.Format("{0} ({1:P2})", tankCount, tankCount / Tanks.Count(x => x.Tier >= 5));
        }

        private SolidColorBrush GetChangeBrush(double val)
        {
            if (val < 0)
                return new SolidColorBrush(Colors.Tomato);
            else if (val == 0)
                return new SolidColorBrush(Colors.Black);
            else
                return new SolidColorBrush(Colors.MediumSeaGreen);
        }

        private void PlaySound()
        {
            if (mlgChecking)
            {
                try
                {
                    SoundPlayer player = new SoundPlayer(Properties.Resources.johncena);
                    player.Load();
                    player.Play();
                }
                catch
                {
                    ShowErrorMessageBox("Could not play MLG checking sound.", "Sound error");
                }
            }
        }

        private void GetWN8ChangesForPlayer()
        {
            if (!worker.IsBusy && !String.IsNullOrWhiteSpace(TB_PlayerName.Text.Trim()))
            {
                //clanLogo = null;
                //imgClanLogo.Source = null;
                //gridOverallChanges.Visibility = Visibility.Collapsed;
                gridOverlay.Visibility = Visibility.Visible;

                PlaySound();

                oldVersion = comboBoxOldVersion.SelectedValue.ToString();
                newVersion = comboBoxNewVersion.SelectedValue.ToString();

                worker.RunWorkerAsync(TB_PlayerName.Text.Trim());
            }
        }

        private void GenerateTankList(string playerid, string appID, string serverID)
        {
            Tanks = new List<Tank>();
            string requesturl = $"wot/tanks/stats/?application_id={appID}&account_id=" + playerid;
            WebClient wc = new WebClient();
            wc.Proxy = null;
            wc.Encoding = Encoding.UTF8;
            JObject jo = JObject.Parse(GetStringFromAPI(requesturl, serverID));

            foreach (JObject jtank in jo["data"][playerid].Children<JObject>())
            {
                Tank t = new Tank();

                t.WN8.Spots = Convert.ToDouble(jtank["all"]["spotted"]);
                t.WN8.Damage = Convert.ToDouble(jtank["all"]["damage_dealt"]);
                t.WN8.Kills = Convert.ToDouble(jtank["all"]["frags"]);
                t.WN8.DecapPoints = Convert.ToDouble(jtank["all"]["dropped_capture_points"]);
                t.WN8.Wins = Convert.ToDouble(jtank["all"]["wins"]);
                t.WN8.Battles = Convert.ToDouble(jtank["all"]["battles"]);

                t.WN8.Spots /= t.WN8.Battles;
                t.WN8.Damage /= t.WN8.Battles;
                t.WN8.Kills /= t.WN8.Battles;
                t.WN8.DecapPoints /= t.WN8.Battles;

                t.WN8.WinRate = t.WN8.Wins / t.WN8.Battles;
                t.ID = jtank["tank_id"].ToString();

                Tanks.Add(t);
            }
        }
        private void CalcWN8()
        {
            WN8_Old = new WN8Item();
            WN8_New = new WN8Item();
            PlayerTotal = new WN8Item();
            PlayerTotalOld = new WN8Item();
            PlayerTotalNew = new WN8Item();

            foreach (Tank t in Tanks)
            {
                WN8Item exp_old = new WN8Item();
                WN8Item exp_new = new WN8Item();

                if (t.WN8.Battles > 0)
                {
                    PlayerTotal.Damage += t.WN8.Damage * t.WN8.Battles;
                    PlayerTotal.DecapPoints += t.WN8.DecapPoints * t.WN8.Battles;
                    PlayerTotal.Kills += t.WN8.Kills * t.WN8.Battles;
                    PlayerTotal.Wins += t.WN8.Wins;
                    PlayerTotal.Spots += t.WN8.Spots * t.WN8.Battles;
                    PlayerTotal.Battles += t.WN8.Battles;

                    // error handling: tenks nicht in old expected

                    if (ExpectedValues_Old.Any(x => x.ID == t.ID))
                    {
                        exp_old = ExpectedValues_Old.First(x => x.ID == t.ID).WN8;

                        WN8_Old.Damage += exp_old.Damage * t.WN8.Battles;
                        WN8_Old.DecapPoints += exp_old.DecapPoints * t.WN8.Battles;
                        WN8_Old.Kills += exp_old.Kills * t.WN8.Battles;
                        WN8_Old.Wins += exp_old.WinRate * t.WN8.Battles;
                        WN8_Old.Spots += exp_old.Spots * t.WN8.Battles;
                        WN8_Old.Battles += t.WN8.Battles;

                        PlayerTotalOld.Damage += t.WN8.Damage * t.WN8.Battles;
                        PlayerTotalOld.DecapPoints += t.WN8.DecapPoints * t.WN8.Battles;
                        PlayerTotalOld.Kills += t.WN8.Kills * t.WN8.Battles;
                        PlayerTotalOld.Wins += t.WN8.Wins;
                        PlayerTotalOld.Spots += t.WN8.Spots * t.WN8.Battles;
                        PlayerTotalOld.Battles += t.WN8.Battles;

                        SetExpectedValues(t.ExpectedValuesOld, exp_old);
                    }

                    if (ExpectedValues_New.Any(x => x.ID == t.ID))
                    {
                        exp_new = ExpectedValues_New.First(x => x.ID == t.ID).WN8;

                        WN8_New.Damage += exp_new.Damage * t.WN8.Battles;
                        WN8_New.DecapPoints += exp_new.DecapPoints * t.WN8.Battles;
                        WN8_New.Kills += exp_new.Kills * t.WN8.Battles;
                        WN8_New.Wins += exp_new.WinRate * t.WN8.Battles;
                        WN8_New.Spots += exp_new.Spots * t.WN8.Battles;
                        WN8_New.Battles += t.WN8.Battles;

                        PlayerTotalNew.Damage += t.WN8.Damage * t.WN8.Battles;
                        PlayerTotalNew.DecapPoints += t.WN8.DecapPoints * t.WN8.Battles;
                        PlayerTotalNew.Kills += t.WN8.Kills * t.WN8.Battles;
                        PlayerTotalNew.Wins += t.WN8.Wins;
                        PlayerTotalNew.Spots += t.WN8.Spots * t.WN8.Battles;
                        PlayerTotalNew.Battles += t.WN8.Battles;

                        SetExpectedValues(t.ExpectedValuesNew, exp_new);
                    }
                }

                CalculateExpectedValuesChange(t);

                t.WN8.WN8_New = CalcWN8(t.WN8, exp_new);
                t.WN8.WN8 = CalcWN8(t.WN8, exp_old);
                t.WN8.Change = t.WN8.WN8_New - t.WN8.WN8;
                t.WN8.ChangePerBattle = t.WN8.Change / t.WN8.Battles;
            }

            WN8_Old.WinRate = WN8_Old.Wins / WN8_Old.Battles;
            WN8_New.WinRate = WN8_New.Wins / WN8_New.Battles;
            PlayerTotal.WinRate = PlayerTotal.Wins / PlayerTotal.Battles;
            PlayerTotalOld.WinRate = PlayerTotal.Wins / PlayerTotal.Battles;
            PlayerTotalNew.WinRate = PlayerTotal.Wins / PlayerTotal.Battles;

            PlayerTotal.WN8 = CalcWN8(PlayerTotalOld, WN8_Old);
            PlayerTotal.WN8_New = CalcWN8(PlayerTotalNew, WN8_New);
            //PlayerTotal.WN8 = CalcWN8(PlayerTotal, WN8_Old);
            //PlayerTotal.WN8_New = CalcWN8(PlayerTotal, WN8_New);
            PlayerTotal.Change = PlayerTotal.WN8_New - PlayerTotal.WN8;
        }
        private void SetTankNames(string appID, string serverID)
        {
            string requesturl = $@"wot/encyclopedia/vehicles/?application_id={appID}";
            JObject jo = JObject.Parse(GetStringFromAPI(requesturl, serverID));

            foreach (JToken tankToken in jo["data"].Children())
            {
                JObject jTank = JObject.Parse(tankToken.First.ToString());

                if (Tanks.Any(x => x.ID == jTank["tank_id"].ToString()))
                {
                    Tank t = Tanks.First(x => x.ID == jTank["tank_id"].ToString());

                    t.Name = jTank["name"].ToString();
                    t.Tier = Convert.ToDouble(jTank["tier"]);
                    t.TypeID = jTank["type"].ToString();
                    t.Type = GetTypeNameFromID(t.TypeID);
                    t.NationID = jTank["nation"].ToString();
                    t.Nation = GetNationNameFromID(t.NationID);
                }
                else
                {

                }
            }

            // add missing tanks because WG sucks dicks
            if (Tanks.Any(x => x.ID == "57121"))
            {
                Tank manualTank = Tanks.First(x => x.ID == "57121");

                manualTank.Name = "M46 Patton KR";
                manualTank.Tier = 8;
                manualTank.TypeID = "mediumTank";
                manualTank.Type = GetTypeNameFromID(manualTank.TypeID);
                manualTank.NationID = "usa";
                manualTank.Nation = GetNationNameFromID(manualTank.NationID);
            }

            if (Tanks.Any(x => x.ID == "14353"))
            {
                Tank manualTank = Tanks.First(x => x.ID == "14353");

                manualTank.Name = "Aufklärungspanzer Panther";
                manualTank.Tier = 7;
                manualTank.TypeID = "lightTank";
                manualTank.Type = GetTypeNameFromID(manualTank.TypeID);
                manualTank.NationID = "germany";
                manualTank.Nation = GetNationNameFromID(manualTank.NationID);
            }

            if (Tanks.Any(x => x.ID == "50961"))
            {
                Tank manualTank = Tanks.First(x => x.ID == "50961");

                manualTank.Name = "leKpz M 41 90 mm";
                //manualTank.NameShort = "M 41 90 GF";
                manualTank.NationID = "germany";
                manualTank.Nation = GetNationNameFromID(manualTank.NationID);
                manualTank.TypeID = "lightTank";
                manualTank.Type = GetTypeNameFromID(manualTank.TypeID);
                manualTank.Tier = 8;
                //manualTank.IsPremium = true;
                //manualTank.Tag = "G120_M41_90_GrandFinal";
            }
        }

        private string GetNationNameFromID(string id)
        {
            if (id == "usa") return "U.S.A.";
            else if (id == "czech") return "Czechoslovakia";
            else if (id == "france") return "France";
            else if (id == "ussr") return "U.S.S.R.";
            else if (id == "china") return "China";
            else if (id == "uk") return "U.K.";
            else if (id == "japan") return "Japan";
            else if (id == "germany") return "Germany";
            else if (id == "sweden") return "Sweden";
            else return "unknown";
        }
        private string GetTypeNameFromID(string id)
        {
            if (id == "heavyTank") return "Heavy Tank";
            else if (id == "AT-SPG") return "Tank Destroyer";
            else if (id == "mediumTank") return "Medium Tank";
            else if (id == "lightTank") return "Light Tank";
            else if (id == "SPG") return "SPG";
            else return "unknown";
        }

        private void SetExpectedValues(WN8Item tankItem, WN8Item expectedItem)
        {
            tankItem.Damage = expectedItem.Damage;
            tankItem.DecapPoints = expectedItem.DecapPoints;
            tankItem.Kills = expectedItem.Kills;
            tankItem.Spots = expectedItem.Spots;
            tankItem.WinRate = expectedItem.WinRate;
        }

        private void CalculateExpectedValuesChange(Tank t)
        {
            t.ExpectedValuesChange.Damage = t.ExpectedValuesNew.Damage - t.ExpectedValuesOld.Damage;
            t.ExpectedValuesChange.DecapPoints = t.ExpectedValuesNew.DecapPoints - t.ExpectedValuesOld.DecapPoints;
            t.ExpectedValuesChange.Kills = t.ExpectedValuesNew.Kills - t.ExpectedValuesOld.Kills;
            t.ExpectedValuesChange.Spots = t.ExpectedValuesNew.Spots - t.ExpectedValuesOld.Spots;
            t.ExpectedValuesChange.WinRate = t.ExpectedValuesNew.WinRate - t.ExpectedValuesOld.WinRate;
        }

        private string GetURLSuffix(string serverID)
        {
            if (serverID == WN8Static.ServerIDEU)
                return "eu";
            else if (serverID == WN8Static.ServerIDNA)
                return "com";
            else if (serverID == WN8Static.ServerIDASIA)
                return "asia";
            else if (serverID == WN8Static.ServerIDRU)
                return "ru";

            return "";
        }

        private string GetStringFromAPI(string request, string serverID)
        {
            WebClient wc = new WebClient
            {
                Proxy = null,
                Encoding = Encoding.UTF8
            };
            return wc.DownloadString($@"https://api.worldoftanks.{GetURLSuffix(serverID)}/{request}");
        }

        private void HandlePlayerClan(string playerID, string appID, string serverID)
        {
            string requesturl = $@"https://api.worldoftanks.{GetURLSuffix(serverID)}/wot/account/info/?application_id={appID}&account_id={playerID}";

            WebClient wc = new WebClient();
            wc.Proxy = null;
            wc.Encoding = Encoding.UTF8;

            JObject jo = JObject.Parse(wc.DownloadString(requesturl));

            string clanDBID = jo["data"][playerID]["clan_id"].ToString();
            
            if (!String.IsNullOrEmpty(clanDBID) && clanDBID != "0")
            {
                requesturl = $@"https://api.worldoftanks.{GetURLSuffix(serverID)}/wgn/clans/info/?application_id={appID}&clan_id={clanDBID}";

                wc = new WebClient();
                wc.Proxy = null;
                wc.Encoding = Encoding.UTF8;

                jo = JObject.Parse(wc.DownloadString(requesturl));

                clanTag = jo["data"][clanDBID]["tag"].ToString();

                string imgURL = jo["data"][clanDBID]["emblems"]["x32"]["portal"].ToString();

                wc.DownloadFile(imgURL, String.Format("{0}/clanLogo.png", AppDomain.CurrentDomain.BaseDirectory));
            }
            else
            {
                clanTag = "";
                clanLogo = null;
            }
        }

        private int GetNewestExpectedValuesVersion()
        {
            try
            {
                string requesturl = @"http://www.wnefficiency.net/exp/expected_tank_values_latest.json";

                WebClient wc = new WebClient();
                wc.Proxy = null;
                wc.Encoding = Encoding.UTF8;
                wc.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache);

                Random rand = new Random();

                requesturl += $"?rand={rand.Next()}{rand.Next()}";

                JObject jo = JObject.Parse(wc.DownloadString(requesturl));

                return Convert.ToInt32(jo["header"]["version"]);
            }
            catch
            {
                int fallbackversion = 27;
                string errortext = String.Format("Encountered an error while looking for the newest version of the expected values.\nUsing the v{0} as the newest version.", fallbackversion);
                MessageBox.Show(errortext, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return fallbackversion;
            }
        }

        private List<Tank> GetExpectedValues(string version)
        {
            List<Tank> tanks = new List<Tank>();

            string valurl = String.Format("http://www.wnefficiency.net/exp/expected_tank_values_{0}.json", version);

            WebClient wc = new WebClient
            {
                Proxy = null,
                Encoding = Encoding.UTF8
            };

            JObject jo = JObject.Parse(wc.DownloadString(valurl));

            foreach(JObject jtank in jo["data"].Children<JObject>())
            {
                Tank t = new Tank();
                t.ID = jtank["IDNum"].ToString();
                //t.ID_Tank = jtank["tankid"].ToString();
                //t.ID_Nation = jtank["countryid"].ToString();

                t.WN8.Damage = GetDouble(jtank, "expDamage");
                t.WN8.Kills = GetDouble(jtank, "expFrag");
                t.WN8.Spots = GetDouble(jtank, "expSpot");
                t.WN8.WinRate = GetDouble(jtank, "expWinRate") / 100;
                t.WN8.DecapPoints = GetDouble(jtank, "expDef");

                tanks.Add(t);
            }

            return tanks;
        }

        public double CalcWN8(WN8Item vals, WN8Item expvals)
        {
            if (IsEmptyExpectedValue(expvals))
            {
                return 0;
            }
            else
            {
                if (vals.Battles > 0)
                {
                    double rDAMAGE = vals.Damage / expvals.Damage;
                    double rDEF = vals.DecapPoints / expvals.DecapPoints;
                    double rFRAG = vals.Kills / expvals.Kills;
                    double rSPOT = vals.Spots / expvals.Spots;
                    double rWIN = vals.WinRate / expvals.WinRate;

                    double rWINc = Math.Max(0, (rWIN - 0.71) / (1 - 0.71));
                    double rDAMAGEc = Math.Max(0, (rDAMAGE - 0.22) / (1 - 0.22));
                    double rFRAGc = Math.Max(0, Math.Min(rDAMAGEc + 0.2, (rFRAG - 0.12) / (1 - 0.12)));
                    double rSPOTc = Math.Max(0, Math.Min(rDAMAGEc + 0.1, (rSPOT - 0.38) / (1 - 0.38)));
                    double rDEFc = Math.Max(0, Math.Min(rDAMAGEc + 0.1, (rDEF - 0.10) / (1 - 0.10)));

                    return Math.Ceiling(980 * rDAMAGEc + 210 * rDAMAGEc * rFRAGc + 155 * rFRAGc * rSPOTc + 75 * rDEFc * rFRAGc + 145 * Math.Min(1.8, rWINc));
                }
                else
                    return -1;
            }
        }

        private bool IsEmptyExpectedValue(WN8Item item)
        {
            return (item.Damage == 0) || (item.DecapPoints == 0) || (item.Kills == 0) || (item.Spots == 0) || (item.WinRate == 0);
        }

        public double GetDouble(JObject jo, string prop)
        {
            return Convert.ToDouble(jo[prop]);
        }

        #region filter
        private void AddFilter()
        {
            if (DG_Tanks.ItemsSource != null)
            {
                ICollectionView cv = CollectionViewSource.GetDefaultView(DG_Tanks.ItemsSource);

                cv.Filter = DataGridFilter;

                DG_Tanks.ItemsSource = cv;
            }
        }


        private bool DataGridFilter(object obj)
        {
            Tank t = (Tank)obj;

            string searchString = textBoxSearch.Text.Trim();

            if (!String.IsNullOrWhiteSpace(searchString))
            {
                if (!t.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // MoE
            if (!checkBoxMoE3.IsChecked.Value && t.MarksOfExcellence == 3)
                return false;

            if (!checkBoxMoE2.IsChecked.Value && t.MarksOfExcellence == 2)
                return false;

            if (!checkBoxMoE1.IsChecked.Value && t.MarksOfExcellence == 1)
                return false;

            if (!checkBoxMoE0.IsChecked.Value && t.MarksOfExcellence == 0)
                return false;

            // tank
            if (!Tiers.Any(x => x.ID == t.Tier.ToString() && x.IsVisible))
                return false;

            if (!Types.Any(x => x.ID == t.TypeID.ToString() && x.IsVisible))
                return false;

            if (!Nations.Any(x => x.ID == t.NationID && x.IsVisible))
                return false;

            if (checkBoxMiscHideZeroBattleTanks.IsChecked.Value && t.WN8.Battles == 0)
                return false;

            return true;
        }

        private void checkBoxMiscHideZeroBattleTanks_Click(object sender, RoutedEventArgs e)
        {
            AddFilter();
        }

        private void checkBoxMoE3_Click(object sender, RoutedEventArgs e)
        {
            AddFilter();
        }

        private void checkBoxColumnsNewExpected_Click(object sender, RoutedEventArgs e)
        {
            foreach (DataGridColumn dataGridColumn in DG_Tanks.Columns)
            {
                if (dataGridColumn.SortMemberPath.StartsWith("ExpectedValuesNew."))
                {
                    dataGridColumn.Visibility = checkBoxColumnsNewExpected.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
                }

                if (dataGridColumn.SortMemberPath.StartsWith("ExpectedValuesOld."))
                {
                    dataGridColumn.Visibility = checkBoxColumnsOldExpected.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
                }

                if (dataGridColumn.SortMemberPath.StartsWith("ExpectedValuesChange."))
                {
                    dataGridColumn.Visibility = checkBoxColumnsChangesExpected.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void textBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            AddFilter();
        }
        #endregion


        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            AddFilter();
        }

        #region context menu
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            List<FilterItem> sourcelist = (List<FilterItem>)currentDataGrid.ItemsSource;
            sourcelist.ForEach(x => x.IsVisible = true);
            AddFilter();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            List<FilterItem> sourcelist = (List<FilterItem>)currentDataGrid.ItemsSource;
            sourcelist.ForEach(x => x.IsVisible = false);
            AddFilter();
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            List<FilterItem> sourcelist = (List<FilterItem>)currentDataGrid.ItemsSource;
            sourcelist.ForEach(x => x.IsVisible = !x.IsVisible);
            AddFilter();
        }
        private void dataGridNation_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            currentDataGrid = (DataGrid)e.Source;
        }
        #endregion



        private void comboBoxServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            serverID = GetServerID();
        }

        private void checkBoxMLGChecking_Checked(object sender, RoutedEventArgs e)
        {
            mlgChecking = true;
        }

        private void checkBoxMLGChecking_Unchecked(object sender, RoutedEventArgs e)
        {
            mlgChecking = false;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }
    }
}
