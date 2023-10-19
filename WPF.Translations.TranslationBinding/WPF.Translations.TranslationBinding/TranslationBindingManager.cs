using System;
using System.Globalization;
using System.Timers;

namespace WPF.Translations.TranslationBinding
{
    public class TranslationBindingManager
    {
        #region Fields

        private static Timer timer;
        private static string cultureName;
        private static bool refreshAutomatically;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether or not the CultureChanged event fires automatically after 
        /// CultureInfo.DefaultThreadCurrentCulture changes. False requires FireCultureChanged 
        /// to be called manually. Default is false. 
        /// </summary>
        public static bool RefreshAutomatically
        {
            get => refreshAutomatically;
            set
            {
                refreshAutomatically = value;

                if (value)
                {
                    timer = new Timer();
                    timer.Interval = 500;
                    timer.Elapsed += Timer_Elapsed;
                    timer.Start();
                }
                else
                {
                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                }
            }
        }

        #endregion


        #region Events

        /// <summary>
        /// Occurs when manually fired (FireCultureChanged) or whenever 
        /// CultureInfo.DefaultThreadCurrentCulture changes (if RefreshAutomatically = true).
        /// </summary>
        public static event EventHandler CultureChanged;

        #endregion

        #region Constructors

        static TranslationBindingManager()
        {
            cultureName = CultureInfo.DefaultThreadCurrentCulture.Name;
        }

        #endregion

        #region Methods

        /// <summary>Forces the CultureChanged event to occur regardless of whether or not the culture actually changed.</summary>
        public static void RefreshTranslations()
        {
            CultureChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string tempCulture = CultureInfo.DefaultThreadCurrentCulture.Name;

            if (tempCulture != cultureName)
            {
                cultureName = tempCulture;

                RefreshTranslations();
            }
        }

        #endregion
    }
}
