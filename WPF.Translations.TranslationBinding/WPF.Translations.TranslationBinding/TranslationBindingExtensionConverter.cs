using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace WPF.Translations.TranslationBinding
{
    public class TranslationBindingExtensionConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                TranslationBindingExtension translationBinding = value as TranslationBindingExtension;

                if (translationBinding == null)
                    throw new ArgumentException("The provided type must be of type TranslationBindingExtension.", "value");

                return new InstanceDescriptor(typeof(TranslationBindingExtension).GetConstructor(new Type[] { typeof(string), typeof(string) }),
                    new object[] { translationBinding.TranslationKey, translationBinding.FallbackValue });
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
