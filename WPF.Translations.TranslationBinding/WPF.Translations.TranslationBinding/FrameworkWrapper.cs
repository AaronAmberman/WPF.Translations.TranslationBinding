using System.Windows;

namespace WPF.Translations.TranslationBinding
{
    internal class FrameworkWrapper
    {
        #region Fields

        private FrameworkElement fe;
        private FrameworkContentElement fce;

        #endregion

        #region Properties

        public DependencyObject Parent
        {
            get
            {
                if (fe != null) return fe.Parent;
                if (fce != null) return fce.Parent;

                return null;
            }
        }

        public DependencyObject TemplatedParent
        {
            get
            {
                if (fe != null) return fe.TemplatedParent;
                if (fce != null) return fce.TemplatedParent;

                return null;
            }
        }

        #endregion

        #region Constructors

        public FrameworkWrapper(DependencyObject doObj)
        {
            fe = doObj as FrameworkElement;
            fce = doObj as FrameworkContentElement;
        }

        #endregion
    }
}
