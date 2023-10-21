using System.Windows;
using System.Windows.Controls;

namespace CustomControlTesting
{
    public class NestedTest : Control
    {
        static NestedTest()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NestedTest), new FrameworkPropertyMetadata(typeof(NestedTest)));
        }
    }
}
