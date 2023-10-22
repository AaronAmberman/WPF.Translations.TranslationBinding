using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Timers;
using System.Windows;
using System.Windows.Data;

namespace WPF.Translations.TranslationBinding
{
    public static class TranslationBindingOperations
    {
        #region Fields

        private static Dictionary<string, string> cachedTranslations = new Dictionary<string, string>();
        private static string cultureName;
        private static bool refreshAutomatically;
        private static Timer timer;
        private static ITranslationProvider translationProvider;
        private static bool useUICulture;

        #endregion

        #region Properties

        internal static Dictionary<string, string> CachedTranslations => cachedTranslations;

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

        /// <summary>Gets the translation by the specified key.</summary>
        /// <param name="key">The key for the translation to look for.</param>
        /// <returns>The translation or null if the key could not be found.</returns>
        public static string GetTranslation(string key)
        {
            if (CachedTranslations.ContainsKey(key)) 
                return CachedTranslations[key];

            return null;
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

        /// <summary>
        /// Reads in the translations for the culture currently set to 
        /// CultureInfo.DefaultThreadCurrentCulture (or CultureInfo.DefaultThreadCurrentUICulture (if UseUICulture = true)).
        /// </summary>
        /// <returns>False if an exception occurred, otherwise true.</returns>
        /// <remarks>
        /// This method does not have to be called manually. The API will call this when needed however there are stuations
        /// where the developer might need translations before the XAML processor/renderer gets to the first TranslationBinding.
        /// In these cases the translations won't be available yet. To accommodate this need this method can be called and the 
        /// translations will be read.
        /// This method does not need to be called whenever the culture changes, this API will handle this. This is essentially
        /// preloading the translations.
        /// </remarks>
        public static bool ReadInTranslationsForCulture()
        {
            return ReadInTranslationsForCulture(GetCurrentCultureName());
        }

        /// <summary>Attempts to read in the translations for a given culture.</summary>
        /// <param name="culture">The culture to read in translations for.</param>
        /// <returns>False if an exception occurred, otherwise true.</returns>
        /// <remarks>
        /// This method does not have to be called manually. The API will call this when needed however there are stuations
        /// where the developer might need translations before the XAML processor/renderer gets to the first TranslationBinding.
        /// In these cases the translations won't be available yet. To accommodate this need this method can be called and the 
        /// translations will be read.
        /// This method does not need to be called whenever the culture changes, this API will handle this. This is essentially
        /// preloading the translations.
        /// </remarks>
        public static bool ReadInTranslationsForCulture(string culture)
        {
            try
            {
                IDictionary<string, string> translations = TranslationProvider.GetTranslationsForCulture(culture);

                foreach (var translation in translations)
                {
                    if (CachedTranslations.ContainsKey(translation.Key)) continue;

                    CachedTranslations.Add(translation.Key, translation.Value);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred attempting to read in the translations.{Environment.NewLine}{ex}");

                return false;
            }
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
