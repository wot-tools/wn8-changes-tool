using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WN8_Stat_Changes
{
    public class Tank
    {
        public string ID { get; set; }
        //public string ID_Tank { get; set; }
        //public string ID_Nation { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string TypeID { get; set; }
        public string Nation { get; set; }
        public string NationID { get; set; }
        public double Tier { get; set; }
        public WN8Item WN8 { get; set; }
        public WN8Item ExpectedValuesNew { get; set; }
        public WN8Item ExpectedValuesOld { get; set; }
        public WN8Item ExpectedValuesChange { get; set; }
        public double MarksOfExcellence { get; set; }
        public DateTime LastBattleTime { get; set; }

        public Tank()
        {
            WN8 = new WN8Item();
            ExpectedValuesChange = new WN8Item();
            ExpectedValuesNew = new WN8Item();
            ExpectedValuesOld = new WN8Item();
        }
    }

    public class FilterItem: INotifyPropertyChanged
    {
        public string ID { get; set; }
        public string Name { get; set; }

        private double _MOE3;
        public double MOE3
        {
            get { return _MOE3; }
            set { _MOE3 = value; OnPropertyChanged(new PropertyChangedEventArgs("MOE3")); }
        }

        private double _MOE2;
        public double MOE2
        {
            get { return _MOE2; }
            set { _MOE2 = value; OnPropertyChanged(new PropertyChangedEventArgs("MOE2")); }
        }

        private double _MOE1;
        public double MOE1
        {
            get { return _MOE1; }
            set { _MOE1 = value; OnPropertyChanged(new PropertyChangedEventArgs("MOE1")); }
        }

        private double _MOE0;
        public double MOE0
        {
            get { return _MOE0; }
            set { _MOE0 = value; OnPropertyChanged(new PropertyChangedEventArgs("MOE0")); }
        }

        private bool _IsVisible;
        public bool IsVisible
        {
            get { return _IsVisible; }
            set { _IsVisible = value; OnPropertyChanged(new PropertyChangedEventArgs("IsVisible")); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        public FilterItem()
        { }

        public FilterItem(string _name, string _id)
        {
            Name = _name;
            ID = _id;
            IsVisible = true;
        }
    }

    public class WN8Item
    {
        public double WN8 { get; set; }
        public double WN8_New { get; set; }
        public double Change { get; set; }
        public double ChangePerBattle { get; set; }

        public double Battles { get; set; }
        public double Wins { get; set; }
        public double WinRate { get; set; }
        public double Damage { get; set; }
        public double DecapPoints { get; set; }
        public double Kills { get; set; }
        public double Spots { get; set; }

        public WN8Item()
        {

        }
    }

    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            if (source != null)
                return source.IndexOf(toCheck, comp) >= 0;
            else
                return false;
        }
    }

    public static class WN8Static
    {
        public static string ServerIDEU = "eu";
        public static string ServerIDNA = "us";
        public static string ServerIDASIA = "asia";
        public static string ServerIDRU = "ru";
    }

    #region VALUE CONVERTERS
    public class WN8ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string hexstring = GetWN8ColorHexString((double)value);
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexstring));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        private string GetWN8ColorHexString(double wn8)
        {
            if (wn8 < 300)
                return "#BAAAAD";  // bad
            else if (wn8 < 450)
                return "#f11919";   // bad
            else if (wn8 < 650)
                return "#ff8a00";   // below average
            else if (wn8 < 900)
                return "#e6df27";   // average
            else if (wn8 < 1200)
                return "#77e812";   // above average
            else if (wn8 < 1600)
                return "#459300";    // good
            else if (wn8 < 2000)
                return "#2ae4ff";    // very good
            else if (wn8 < 2450)
                return "#00a0b8";    // great
            else if (wn8 < 2900)
                return "#c64cff";    // unicum
            else
                return "#8225ad";    // super_unicum
        }
    }

    public class WinRateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(GetWinRateColorHexString((double)value)));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
        
        private string GetWinRateColorHexString(double winrate)
        {
            if (winrate < 0.46)
                return "#BAAAAD";  // bad
            else if (winrate < 0.47)
                return "#f11919";   // bad
            else if (winrate < 0.48)
                return "#ff8a00";   // below average
            else if (winrate < 0.50)
                return "#e6df27";   // average
            else if (winrate < 0.52)
                return "#77e812";   // above average
            else if (winrate < 0.54)
                return "#459300";    // good
            else if (winrate < 0.56)
                return "#2ae4ff";    // very good
            else if (winrate < 0.60)
                return "#00a0b8";    // great
            else if (winrate < 0.65)
                return "#c64cff";    // unicum
            else
                return "#8225ad";    // super_unicum
        }
    }

    public class ChangeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val = (double)value;

            if (val < 0)
                return new SolidColorBrush(Colors.Tomato);
            else if (val == 0)
                return new SolidColorBrush(Colors.Black);
            else
                return new SolidColorBrush(Colors.Green);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val = (bool)value;

            if (parameter != null && parameter.ToString() == "invert")
                return val ? Visibility.Collapsed : Visibility.Visible;
            else
                return val ? Visibility.Visible : Visibility.Collapsed;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }



    public class ForegroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush scbDark = new SolidColorBrush(Colors.Black);
            SolidColorBrush scbBright = new SolidColorBrush(Colors.White);

            if(parameter.ToString()=="wn8")
            {
                if (darkforegroundcolors.Contains(GetWN8ColorHexString((double)value)))
                    return scbDark;
                else
                    return scbBright;
            }
            else
            {
                if (darkforegroundcolors.Contains(GetWinRateColorHexString((double)value)))
                    return scbDark;
                else
                    return scbBright;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }


        public static string[] darkforegroundcolors = new string[] { "#2ae4ff", "#77e812", "#e6df27" };

        private string GetWN8ColorHexString(double wn8)
        {
            if (wn8 < 300)
                return "#BAAAAD";  // bad
            else if (wn8 < 450)
                return "#f11919";   // bad
            else if (wn8 < 650)
                return "#ff8a00";   // below average
            else if (wn8 < 900)
                return "#e6df27";   // average
            else if (wn8 < 1200)
                return "#77e812";   // above average
            else if (wn8 < 1600)
                return "#459300";    // good
            else if (wn8 < 2000)
                return "#2ae4ff";    // very good
            else if (wn8 < 2450)
                return "#00a0b8";    // great
            else if (wn8 < 2900)
                return "#c64cff";    // unicum
            else
                return "#8225ad";    // super_unicum
        }

        private string GetWinRateColorHexString(double winrate)
        {
            if (winrate < 0.46)
                return "#BAAAAD";  // bad
            else if (winrate < 0.47)
                return "#f11919";   // bad
            else if (winrate < 0.48)
                return "#ff8a00";   // below average
            else if (winrate < 0.50)
                return "#e6df27";   // average
            else if (winrate < 0.52)
                return "#77e812";   // above average
            else if (winrate < 0.54)
                return "#459300";    // good
            else if (winrate < 0.56)
                return "#2ae4ff";    // very good
            else if (winrate < 0.60)
                return "#00a0b8";    // great
            else if (winrate < 0.65)
                return "#c64cff";    // unicum
            else
                return "#8225ad";    // super_unicum
        }
    }

    #endregion
}
