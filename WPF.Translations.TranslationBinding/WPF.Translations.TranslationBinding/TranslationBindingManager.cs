using System;
using System.Globalization;
using System.Timers;
using System.Windows;

namespace WPF.Translations.TranslationBinding
{
    public class TranslationBindingManager
    {
        private static Timer timer;
        private static string cultureName = string.Empty;

        public static event EventHandler CultureChanged;

        static TranslationBindingManager()
        {
            cultureName = CultureInfo.DefaultThreadCurrentCulture.Name;

            timer = new Timer();
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string tempCulture = CultureInfo.DefaultThreadCurrentCulture.Name;

            if (tempCulture != cultureName)
            {
                cultureName = tempCulture;

                CultureChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }
}
