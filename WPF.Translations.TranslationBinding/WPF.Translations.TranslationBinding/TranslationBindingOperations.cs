using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Xaml;

namespace WPF.Translations.TranslationBinding
{
    public static class TranslationBindingOperations
    {
        #region Fields

        private static Timer timer;
        private static string cultureName;
        private static bool refreshAutomatically;
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

        /// <summary>
        /// Gets or sets whether or not to look at CultureInfo.DefaultThreadCurrentCulture 
        /// or CultureInfo.DefaultThreadCurrentUICulture. Default is false.
        /// </summary>
        public static bool UseUICulture
        {
            get => useUICulture;
            set
            {
                useUICulture = value;

                cultureName = GetCurrentCultureName();
            }
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
            cultureName = CultureInfo.DefaultThreadCurrentUICulture?.Name;
        }

        #endregion

        #region Methods

        internal static string GetCurrentCultureName()
        {
            return UseUICulture ? CultureInfo.DefaultThreadCurrentUICulture?.Name : CultureInfo.DefaultThreadCurrentCulture?.Name;
        }

        internal static object GetRootObjectForTranslationBinding(IServiceProvider serviceProvider, DependencyObject target)
        {
            IRootObjectProvider rootObjectProvider = (IRootObjectProvider)serviceProvider?.GetService(typeof(IRootObjectProvider));

            object root = null;

            // attempt to find a root object one way or another
            if (rootObjectProvider == null || rootObjectProvider.RootObject == null)
            {
                /*
                 * The XAML root object provider is unable to give us the root object, this is more than likely
                 * do to the fact that the TranslationBinding is probably in a ControlTemplate, so we will attempt 
                 * to get the first control found on the closest TemplateParent property in the control hierarchy.
                 * 
                 * We need a FrameworkElement or FrameworkContentElement to determine this.
                 */

                FrameworkWrapper fw = new FrameworkWrapper(target);

                if (fw != null)
                {
                    if (fw.Parent == null && fw.TemplatedParent == null) root = null;
                    else
                    {
                        while (fw != null && fw.TemplatedParent == null)
                        {
                            // if both Parent and TemplatedParent are null then we have no where to go
                            if (fw.Parent == null) break;

                            fw = new FrameworkWrapper(fw.Parent);
                        }

                        if (fw != null) root = fw.TemplatedParent;
                    }
                }
            }
            else
            {
                root = rootObjectProvider.RootObject;
            }

            return root;
        }

        /*
         * This method will return the list of culture names cut from the resource name
         * so we know what translations were included, we expect a list like...
         * de,
         * en, 
         * en-GB, 
         * fr, 
         * it, 
         * etc.
         * 
         * We also require pack application strings be default if no ITranslationProvider was
         * provided. So something like...
         * pack://application:,,,/Translations/Translations.de.xaml
         * pack://application:,,,/Translations/Translations.en.xaml
         * pack://application:,,,/Translations/Translations.en-GB.xaml
         * pack://application:,,,/Translations/Translations.fr.xaml
         * pack://application:,,,/Translations/Translations.it.xaml
         */
        internal static List<string> GetTranslationResourcesFromAssemblyManifest(Assembly assembly)
        {
            // now that we have our assembly lets look for resource names
            string resName = assembly.GetName().Name + ".g.resources";
            string[] resources = null;

            using (var stream = assembly.GetManifestResourceStream(resName))
            {
                using (var reader = new System.Resources.ResourceReader(stream))
                {
                    resources = reader.Cast<DictionaryEntry>().Select(entry => entry.Key.ToString()).ToArray();
                }
            }

            if (resources == null) return new List<string>();

            // we require the resources to be in a translation and named something like Translations.en.xaml
            List<string> res = resources.Where(x => x.StartsWith("translation", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x).ToList();
            List<string> transRes = new List<string>();

            foreach (string resource in res)
            {
                string r = resource.Replace(".xaml", "");
                string match = r.Substring(r.LastIndexOf('.') + 1);

                transRes.Add(match);
            }

            // from time to time we may see a baml entry in here (this should be excluded 100% of the time)
            string baml = transRes.FirstOrDefault(str => str.Equals("baml", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(baml)) 
                transRes.Remove(baml);

            return transRes;
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
