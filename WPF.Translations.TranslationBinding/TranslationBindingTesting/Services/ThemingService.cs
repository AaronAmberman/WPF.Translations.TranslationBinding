using System.Windows;
using TranslationBindingTesting.Theming;

namespace TranslationBindingTesting.Services
{
    internal class ThemingService
    {
        #region Fields

        private Theme theme;

        #endregion

        #region Properties

        public Theme Theme
        {
            get => theme;
            set
            {
                theme = value;

                ChangeTheme(value);
            }
        }

        #endregion

        #region Methods

        private void ChangeTheme(Theme theme)
        {
            foreach (ResourceDictionary rd in Application.Current.Resources.MergedDictionaries)
            {
                if (rd is ThemeDictionary td)
                    td.UpdateTheme(theme);
            }
        }

        #endregion
    }
}
