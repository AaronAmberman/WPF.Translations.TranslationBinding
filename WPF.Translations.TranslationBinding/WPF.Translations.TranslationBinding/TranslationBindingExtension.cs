using System;
using System.ComponentModel;
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

        private string fallbackValue = "[Translation default fallback value.]";
        private DependencyObject internalTarget;
        private Binding parameter;
        private Binding parameter2;
        private Binding parameter3;
        private DependencyProperty targetProperty;
        private string translationKey;
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

        internal DependencyObject InternalTarget
        {
            get
            {
                if (internalTarget == null)
                    internalTarget = (DependencyObject)weakTargetReference.Target;

                return internalTarget;
            }
        }

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

            // we have to have a translation provider
            if (TranslationBindingOperations.TranslationProvider == null)
                throw new InvalidOperationException("TranslationBindingOperations.TranslationProvider is required.");

            // connect our instance to the signal that will tell us we need to retranslate
            TranslationBindingOperations.CleanUp += TranslationBindingOperations_CleanUp;
            TranslationBindingOperations.CultureChanged += TranslationBindingOperations_CultureChanged;

            try
            {
                // if another instance read in the translations then we dont' need to
                if (TranslationBindingOperations.CachedTranslations.Count == 0)
                    TranslationBindingOperations.ReadInTranslationsForCulture(TranslationBindingOperations.GetCurrentCultureName());

                // make sure we have translations
                if (TranslationBindingOperations.CachedTranslations.Count == 0) 
                    return InternalFallbackValue;

                string translated = Translate();

                return translated;
            }
            catch (Exception)
            {
                return InternalFallbackValue;
            }
        }

        private void TranslationBindingOperations_CleanUp(object sender, EventArgs e)
        {
            TranslationBindingOperations.CleanUp -= TranslationBindingOperations_CleanUp;
            TranslationBindingOperations.CultureChanged -= TranslationBindingOperations_CultureChanged;

            weakTargetReference.Target = null;
            weakTargetReference = null;
            targetProperty = null;
            translationKey = null;
            fallbackValue = null;
            parameter = null;
            parameter2 = null;
            parameter3 = null;
        }

        private string Translate()
        {
            string val = string.Empty;

            // we could not get our translations for some reason so just return the fallback value
            if (TranslationBindingOperations.CachedTranslations.Count == 0)
                val = InternalFallbackValue;
            else
            {
                if (TranslationBindingOperations.CachedTranslations.ContainsKey(TranslationKey))
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

                    val = FormatTranslation(TranslationBindingOperations.CachedTranslations[TranslationKey], parameter1Value, parameter2Value, parameter3Value);
                }
                else val = InternalFallbackValue;
            }

            return val;
        }

        private void TranslationBindingOperations_CultureChanged(object sender, EventArgs e)
        {
            TranslationBindingOperations.CachedTranslations.Clear();

            if (weakTargetReference.IsAlive)
            {
                TranslationBindingOperations.ReadInTranslationsForCulture(TranslationBindingOperations.GetCurrentCultureName());

                InternalTarget.Dispatcher.Invoke(() =>
                {
                    string translated = InternalFallbackValue;

                    if (TranslationBindingOperations.CachedTranslations.Count > 0)
                        translated = Translate();

                    InternalTarget.SetValue(targetProperty, translated);
                });
            }
            else
            {
                TranslationBindingOperations_CleanUp(null, EventArgs.Empty);
            }
        }

        #endregion
    }
}
