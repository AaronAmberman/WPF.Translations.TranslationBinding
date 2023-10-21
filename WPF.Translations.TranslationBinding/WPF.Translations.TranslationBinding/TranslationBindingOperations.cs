using System;
using System.Globalization;
using System.Timers;
using System.Windows;
using System.Windows.Data;

namespace WPF.Translations.TranslationBinding
{
    public static class TranslationBindingOperations
    {
        #region Fields

        private static Timer timer;
        private static string cultureName;
        private static bool refreshAutomatically;
        private static ITranslationProvider translationProvider;
        private static bool useUICulture;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether or not the CultureChanged event fires automatically after 
        /// CultureInfo.DefaultThreadCurrentCulture (or CultureInfo.DefaultThreadCurrentUICulture 
        /// (if UseUICulture = true)) changes. False requires FireCultureChanged to be called 
        /// manually. Default is false. 
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

        /// <summary>Gets or sets the translations provider.</summary>
        public static ITranslationProvider TranslationProvider
        {
            get => translationProvider;
            set => translationProvider = value;
        }

        /// <summary>
        /// Gets or sets whether or not to look at CultureInfo.DefaultThreadCurrentCulture 
        /// or CultureInfo.DefaultThreadCurrentUICulture. Default is false.
        /// </summary>
        public static bool UseUICulture
        {
            get => useUICulture;
            set => useUICulture = value;
        }

        #endregion


        #region Events

        /// <summary>
        /// Occurs when manually fired (FireCultureChanged) or whenever 
        /// CultureInfo.DefaultThreadCurrentCulture (or CultureInfo.DefaultThreadCurrentUICulture (if UseUICulture = true)) 
        /// changes (if RefreshAutomatically = true).
        /// </summary>
        public static event EventHandler CultureChanged;

        #endregion

        #region Constructors

        static TranslationBindingOperations()
        {
            cultureName = GetCurrentCultureName();
        }

        #endregion

        #region Methods

        internal static string GetCurrentCultureName()
        {
            return UseUICulture ? CultureInfo.DefaultThreadCurrentUICulture?.Name : CultureInfo.DefaultThreadCurrentCulture?.Name;
        }

        internal static object GetValueFromBinding(Binding b, DependencyObject d, DependencyProperty p)
        {
            object result = null;

            if (d != null)
            {
                BindingOperations.SetBinding(d, p, b);
                result = d.GetValue(p);
                BindingOperations.ClearBinding(d, p);
            }

            return result;
        }

        /// <summary>Forces the CultureChanged event to occur regardless of whether or not the culture actually changed.</summary>
        public static void RefreshTranslations()
        {
            CultureChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string tempCulture = GetCurrentCultureName();

            if (tempCulture != cultureName)
            {
                cultureName = tempCulture;

                RefreshTranslations();
            }
        }

        #endregion
    }
}
