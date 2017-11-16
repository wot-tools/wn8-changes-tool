using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WN8_Stat_Changes
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag)));
        }
    }
}
