using System.Windows;
using System.Windows.Controls;

namespace CustomControlTesting
{
    /// <summary>Testing TranslationBinding in a custom control.</summary>
    public class TranslationBindingCustomControl : Control
    {
        static TranslationBindingCustomControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TranslationBindingCustomControl), new FrameworkPropertyMetadata(typeof(TranslationBindingCustomControl)));
        }
    }
}
