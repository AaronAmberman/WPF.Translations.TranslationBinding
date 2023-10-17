using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

namespace WPF.Translations.TranslationBinding
{
    [TypeConverter(typeof(TranslationBindingExtensionConverter))]
    [MarkupExtensionReturnType(typeof(string))]
    public class TranslationBindingExtension : MarkupExtension
    {
        #region Fields

        private List<string> cachedIncludedTranslations = new List<string>();
        private static Dictionary<string, string> cachedTranslations = new Dictionary<string, string>();
        private string fallbackValue;
        private DependencyProperty targetProperty;
        private string translationKey;
        private WeakReference weakTargetReference;

        #endregion

        #region Properties

        public string FallbackValue
        {
            get => fallbackValue;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("FallbackValue key cannot be null, empty or consist of white-space characters only.");

                fallbackValue = value;
            }
        }

        public string Parameter { get; set; }
        public string Parameter2 { get; set; }
        public string Parameter3 { get; set; }

        public string TranslationKey
        {
            get => translationKey;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("Translation key cannot be null, empty or consist of white-space characters only.");

                translationKey = value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>Initializes a new instance of the <see cref="TranslationBindingExtension"/> class.</summary>
        public TranslationBindingExtension()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TranslationBindingExtension"/> class.</summary>
        /// <param name="key">The key to the translation we are looking for.</param>
        public TranslationBindingExtension(string key)
        {
            TranslationKey = key;
        }

        /// <summary>Initializes a new instance of the <see cref="TranslationBindingExtension"/> class.</summary>
        /// <param name="key">The key to the translation we are looking for.</param>
        /// <param name="fallback">The fallback value to use if the key is not found.</param>
        public TranslationBindingExtension(string key, string fallback)
        {
            TranslationKey = key;
            FallbackValue = fallback;
        }

        #endregion

        #region Methods

        private void TranslationBindingManager_CultureChanged(object sender, EventArgs e)
        {
            if (!cachedTranslations.ContainsKey(CultureInfo.DefaultThreadCurrentCulture.Name))
                cachedTranslations.Clear();

            if (weakTargetReference.IsAlive)
            {
                DependencyObject target = (DependencyObject)weakTargetReference.Target;
                string backUpReturn = string.IsNullOrWhiteSpace(FallbackValue) ? string.Empty : FallbackValue;

                if (!cachedIncludedTranslations.Contains(CultureInfo.DefaultThreadCurrentCulture.Name.ToLower()))
                {
                    target.Dispatcher.Invoke(() =>
                    {
                        target.SetValue(targetProperty, backUpReturn);
                    });

                    return;
                }

                ReadInTranslations(CultureInfo.DefaultThreadCurrentCulture.Name);

                target.Dispatcher.Invoke(() =>
                {
                    if (cachedTranslations.ContainsKey(TranslationKey))
                        target.SetValue(targetProperty, cachedTranslations[TranslationKey]);
                    else
                        target.SetValue(targetProperty, backUpReturn);
                });
            }
            else
            {
                // when the culture changes if our target is not alive then just clean up
                Debug.WriteLine($"TranslationKey:{TranslationKey} clean up");

                TranslationBindingManager.CultureChanged -= TranslationBindingManager_CultureChanged;

                weakTargetReference = null;
                targetProperty = null;
                translationKey = null;
                fallbackValue = null;
            }
        }

        private void GetIncludedTranslationNames()
        {
            // this method only needs to run once for us to determine the translations that were included
            if (cachedIncludedTranslations.Count > 0) return;

            var assembly = Assembly.GetEntryAssembly();

            string resName = assembly.GetName().Name + ".g.resources";

            string[] resources = null;

            using (var stream = assembly.GetManifestResourceStream(resName))
            {
                using (var reader = new System.Resources.ResourceReader(stream))
                {
                    resources = reader.Cast<DictionaryEntry>().Select(entry => entry.Key.ToString()).ToArray();
                }
            }

            if (resources == null) return;

            List<string> res = resources.Where(x => x.StartsWith("translation", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x).ToList();

            List<string> transRes = new List<string>();

            foreach (string resource in res)
            {
                string r = resource.Replace(".xaml", "");
                string match = r.Substring(r.LastIndexOf('.') + 1);

                transRes.Add(match);
            }

            cachedIncludedTranslations.AddRange(transRes);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // determine culture from thread
            string backUpReturn = string.IsNullOrWhiteSpace(FallbackValue) ? string.Empty : FallbackValue;

            if (serviceProvider == null) return backUpReturn;

            ValidateTargetObjectAndProperty(serviceProvider);

            if (DesignerProperties.GetIsInDesignMode((DependencyObject)weakTargetReference.Target)) return backUpReturn;

            // this should probably be some kind of WeakEventManager/IWeakEventListener or some how tie a WeakReference to the DependencyObject
            TranslationBindingManager.CultureChanged += TranslationBindingManager_CultureChanged;

            try
            {
                GetIncludedTranslationNames();

                if (cachedIncludedTranslations.Count == 0) return backUpReturn;

                // make sure the culture exists in the collection of translation resources
                if (!cachedIncludedTranslations.Contains(CultureInfo.DefaultThreadCurrentCulture.Name.ToLower())) return backUpReturn;

                if (cachedTranslations.Count == 0)
                    ReadInTranslations(CultureInfo.DefaultThreadCurrentCulture.Name);

                string val = string.Empty;

                if (cachedTranslations.ContainsKey(TranslationKey))
                    val =  cachedTranslations[TranslationKey];
                else
                    val = backUpReturn;

                return val;
            }
            catch (Exception)
            {
                return backUpReturn;
            }
        }

        private void ReadInTranslations(string culture)
        {
            // this API requires resources to be marked as Resource so pack application strings work
            string resource = $"pack://application:,,,/Translations/Translations.{culture}.xaml";

            ResourceDictionary resourceDictionary = new ResourceDictionary
            {
                Source = new Uri(resource)
            };

            // move translations to a dictionary so we have fast lookup (only needs to be done once, until languaged is changed)
            foreach (string key in resourceDictionary.Keys)
                cachedTranslations.Add(key, resourceDictionary[key].ToString());
        }

        private void ValidateTargetObjectAndProperty(IServiceProvider serviceProvider)
        {
            IProvideValueTarget provideValueTarget = (IProvideValueTarget)serviceProvider?.GetService(typeof(IProvideValueTarget));

            DependencyObject target = provideValueTarget?.TargetObject as DependencyObject;

            if (target == null) throw new NotSupportedException("TargetObject must be a DependencyObject.");

            weakTargetReference = new WeakReference(target, false);

            targetProperty = provideValueTarget?.TargetProperty as DependencyProperty;

            if (targetProperty == null) throw new NotSupportedException("TargetProperty must be a DependencyProperty.");
        }

        #endregion
    }
}
