using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace WPF.Translations.TranslationBinding
{
    /// <summary>A {TranslationBinding} for XAML. This class cannot be inherited.</summary>
    [TypeConverter(typeof(TranslationBindingExtensionConverter))]
    [MarkupExtensionReturnType(typeof(string))]
    public sealed class TranslationBindingExtension : MarkupExtension
    {
        #region Fields

        private static Dictionary<string, string> cachedTranslations = new Dictionary<string, string>();
        private string fallbackValue = "[Translation default fallback value.]";
        private DependencyObject internalTarget;
        private Binding parameter;
        private Binding parameter2;
        private Binding parameter3;
        private object root;
        private DependencyProperty targetProperty;
        private List<string> translationsIncluded;
        private string translationKey;
        private ITranslationProvider translationProvider;
        private Assembly translationProviderAssembly;
        private Type translationProviderType;
        private WeakReference weakTargetReference;

        #endregion

        #region Properties

        /// <summary>Gets or sets the fallback to use if the translation cannot be found or fails.</summary>
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

        internal string InternalFallbackValue => string.IsNullOrWhiteSpace(FallbackValue) ? string.Empty : FallbackValue;

        /// <summary>Gets or sets the parameter to feed into the translation string for string.Format purposes.</summary>
        public Binding Parameter
        {
            get => parameter;
            set
            {
                if (value == null)
                    throw new InvalidOperationException("Binding property Parameter cannot be null.");

                parameter = value;
            }
        }

        /// <summary>Gets or sets the second parameter to feed into the translation string for string.Format purposes.</summary>
        public Binding Parameter2
        {
            get => parameter2;
            set
            {
                if (value == null)
                    throw new InvalidOperationException("Binding property Parameter2 cannot be null.");

                parameter2 = value;
            }
        }

        /// <summary>Gets or sets the third parameter to feed into the translation string for string.Format purposes.</summary>
        public Binding Parameter3
        {
            get => parameter3;
            set
            {
                if (value == null)
                    throw new InvalidOperationException("Binding property Parameter3 cannot be null.");

                parameter3 = value;
            }
        }

        internal DependencyObject InternalTarget
        {
            get
            {
                if (internalTarget == null) 
                    internalTarget = (DependencyObject)weakTargetReference.Target;

                return internalTarget;
            }
        }

        /// <summary>Gets or sets the key to look for in the translation files.</summary>
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

        private string FormatTranslation(string value, object parameter, object parameter2, object parameter3) 
        {
            if (parameter != null && parameter2 != null && parameter3 != null)
                return string.Format(value, parameter, parameter2, parameter3);
            else if (parameter != null && parameter2 != null)
                return string.Format(value, parameter, parameter2);
            else if (parameter != null && parameter3 != null)
                return string.Format(value, parameter, parameter3);
            else if (parameter2 != null && parameter3 != null)
                return string.Format(value, parameter2, parameter3);
            else if (parameter != null)
                return string.Format(value, parameter);
            else if (parameter2 != null)
                return string.Format(value, parameter2);
            else if (parameter3 != null)
                return string.Format(value, parameter3);
            
            return value;
        }

        private void GetTranslationProviderAssembly(IServiceProvider serviceProvider)
        {
            root = TranslationBindingOperations.GetRootObjectForTranslationBinding(serviceProvider, InternalTarget);
            
            // if we could not find a root then use the entry assembly
            if (root == null)
                translationProviderAssembly = Assembly.GetEntryAssembly();
            else
            {
                Type type = root.GetType();

                // load the assembly the tpye is from
                translationProviderAssembly = Assembly.GetAssembly(type);

                // if that didn't work use the entry assembly as a back up
                if (translationProviderAssembly == null)
                    translationProviderAssembly = Assembly.GetEntryAssembly();
            }

            if (translationProviderAssembly == null)
                throw new CultureNotFoundException("TranslationBinding could not locate translation provider assembly.");

            /*
             * look for an instance of ITranslationProvider in the assembly, 
             * if one is found then use that to provide included translations
             */
            translationProviderType = translationProviderAssembly.GetTypes().FirstOrDefault(t => typeof(ITranslationProvider).IsAssignableFrom(t));
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            /*
             * Example from MS...
             * https://github.com/XAMLMarkupExtensions/XAMLMarkupExtensions/blob/master/src/Base/NestedMarkupExtension.cs
             */

            // if we don't have an incoming service provider then just return our translation binding instance
            if (serviceProvider == null) return this;

            IProvideValueTarget provideValueTarget = (IProvideValueTarget)serviceProvider?.GetService(typeof(IProvideValueTarget));

            // if we don't have an incoming service provider then just return our translation binding instance
            if (provideValueTarget == null) return this;

            DependencyObject dependencyObject = provideValueTarget?.TargetObject as DependencyObject;

            /*
             * if we don't have our DependencyObject then more than likely we have a System.Windows.SharedDp 
             * and the XAML processor will call us again with the appropriate value
             */
            if (dependencyObject == null) return this;

            targetProperty = provideValueTarget?.TargetProperty as DependencyProperty;

            // we must be bound to a DependencyProperty in order for this to work properly
            if (targetProperty == null)
                throw new NotSupportedException("The incoming property must be a DependencyProperty.");

            // hold onto our target weakly
            weakTargetReference = new WeakReference(dependencyObject, false);

            // we don't need to translate anything if we are in design mode
            if (DesignerProperties.GetIsInDesignMode(InternalTarget)) return InternalFallbackValue;

            // connect our instance to the signal that will tell us we need to retranslate
            TranslationBindingOperations.CultureChanged += TranslationBindingOperations_CultureChanged;

            try
            {
                GetTranslationProviderAssembly(serviceProvider);

                if (translationProviderType == null)
                {
                    // if the developers did not provide an ITranslationProvider then we need to locate the resources ourselves
                    translationsIncluded = TranslationBindingOperations.GetTranslationResourcesFromAssemblyManifest(translationProviderAssembly);

                    // if we somehow could not find the included collection of translations then we cannot locate our resources, so bail
                    if (translationsIncluded == null || translationsIncluded.Count == 0)
                        return InternalFallbackValue;

                    if (!TranslationsIncludedHasCulture())
                        return InternalFallbackValue;
                }

                /*
                 * There could be multiple translation sources at play (from multiple assemblies (this is because the collection of 
                 * translations is cached statically)), we will do we what can to read in those as well. If there is a conflicting 
                 * key then that key won't be added.
                 */
                ReadInTranslations(TranslationBindingOperations.GetCurrentCultureName());

                string translated = Translate();

                return translated;
            }
            catch (Exception)
            {
                return InternalFallbackValue;
            }
        }

        private void ReadInTranslations(string culture)
        {
            if (translationProviderType == null)
            {
                /*
                 * If the developers did not provide a ITranslationProvider in their assembly then this API, 
                 * by default, will assume the translation resource is embedded in the assembly because it was
                 * marked as a "Resource" under properties and will use pack application strings to attempt to 
                 * locate resources.
                 * 
                 * There is a hard coded nomenclature in this scenario as well. The API looks for 
                 * pack://application:,,,/Translations/Translations.{culture}.xaml. So your translations 
                 * resources must be in a Translations directory and be called Translations.{culture}.xaml. Where
                 * culture is the name of the desired culture; en, en-GB, es, es-MX, fr, ut, ko, zh-Hans, etc.
                 * It should be noted that the first part of the pack URI string doesn't matter but the second 
                 * half must point to a Translation directory that has Translations.{culture}.xaml. E.g.
                 * pack://application:,,,/Translations/Translations.de.xaml
                 * pack://application:,,,AssemblyName;component/Translations/Translations.en-GB.xaml
                 * pack://application:,,,AssemblyName;v1.2.3.4;component/Translations/Translations.zh-Hans.xaml
                 * etc.
                 * 
                 * Key thing to note the culture name must exactly match what the 
                 * CultureInfo.DefaultThreadCurrentCulture (or CultureInfo.DefaultThreadCurrentUICulture 
                 * (if UseUICulture = true)) was set to or else the translation will not be found.
                 * 
                 * If we are here then we are sure our culture is found in our resources.
                 */

                // this API requires XAML resources to be marked as Resource so pack application strings work
                AssemblyName name = translationProviderAssembly.GetName();

                string resource = $"pack://application:,,,/{name.Name};v{name.Version};component/Translations/Translations.{culture}.xaml";

                ResourceDictionary resourceDictionary = new ResourceDictionary
                {
                    Source = new Uri(resource)
                };

                /*
                 * move translations to a dictionary so we have fast lookup (only needs to be done once, until languaged is changed),
                 * translations can also come from multiple sources so we will accumulate translations as we go (no duplicate keys 
                 * though)
                 */
                foreach (string key in resourceDictionary.Keys)
                {
                    if (cachedTranslations.ContainsKey(key)) continue;

                    cachedTranslations.Add(key, resourceDictionary[key].ToString());
                }
            }
            else
            {
                // create an instance of the translation provider and get our translations for the culture
                if (translationProvider == null)
                    translationProvider = (ITranslationProvider)Activator.CreateInstance(translationProviderType);

                IDictionary<string, string> translations = translationProvider.GetTranslationsForCulture(culture);

                /*
                 * move translations to a dictionary so we have fast lookup (only needs to be done once, until languaged is changed),
                 * translations can also come from multiple sources so we will accumulate translations as we go (no duplicate keys 
                 * though)
                 */
                foreach (var translation in translations)
                {
                    if (cachedTranslations.ContainsKey(translation.Key)) continue;

                    cachedTranslations.Add(translation.Key, translation.Value);
                }
            }
        }

        private string Translate()
        {
            string val = string.Empty;

            // we could not get our translations for some reason so just return the fallback value
            if (cachedTranslations.Count == 0)
                val = InternalFallbackValue;
            else
            {
                if (cachedTranslations.ContainsKey(TranslationKey))
                {
                    object parameter1Value = null;
                    object parameter2Value = null;
                    object parameter3Value = null;

                    if (Parameter != null)
                        parameter1Value = TranslationBindingOperations.GetValueFromBinding(Parameter, InternalTarget, targetProperty);

                    if (Parameter2 != null)
                        parameter2Value = TranslationBindingOperations.GetValueFromBinding(Parameter2, InternalTarget, targetProperty);

                    if (Parameter3 != null)
                        parameter3Value = TranslationBindingOperations.GetValueFromBinding(Parameter3, InternalTarget, targetProperty);

                    val = FormatTranslation(cachedTranslations[TranslationKey], parameter1Value, parameter2Value, parameter3Value);
                }
                else val = InternalFallbackValue;
            }

            return val;
        }

        private void TranslationBindingOperations_CultureChanged(object sender, EventArgs e)
        {
            cachedTranslations.Clear();

            if (weakTargetReference.IsAlive)
            {
                if (!TranslationsIncludedHasCulture())
                {
                    InternalTarget.Dispatcher.Invoke(() =>
                    {
                        InternalTarget.SetValue(targetProperty, InternalFallbackValue);
                    });

                    return;
                }

                ReadInTranslations(TranslationBindingOperations.GetCurrentCultureName());

                InternalTarget.Dispatcher.Invoke(() =>
                {
                    string translated = Translate();

                    InternalTarget.SetValue(targetProperty, translated);
                });
            }
            else
            {
                // when the culture changes if our target is not alive then just clean up
                TranslationBindingOperations.CultureChanged -= TranslationBindingOperations_CultureChanged;

                weakTargetReference = null;
                targetProperty = null;
                translationKey = null;
                fallbackValue = null;
            }
        }

        private bool TranslationsIncludedHasCulture()
        {
            bool matched = false;

            string culture = TranslationBindingOperations.GetCurrentCultureName();

            foreach (string ti in translationsIncluded)
            {
                if (ti.Contains(culture, StringComparison.OrdinalIgnoreCase))
                    matched = true;
            }

            return matched;
        }

        #endregion
    }
}
