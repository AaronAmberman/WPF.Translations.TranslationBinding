using System.Windows;

namespace TranslationBindingTesting.ViewModels
{
    internal class ProgressViewModel : ViewModelBase
    {
        #region Fields

        private Visibility progressDialogVisbility = Visibility.Collapsed;
        private bool progressIsIndeterminate = false;
        private double progressMax = 1.0;
        private double progressMin = 0.0;
        private string progressMessage;
        private string progressTitle;
        private double progressValue = 0.0;

        #endregion

        #region Properties

        public Visibility ProgressDialogVisbility
        {
            get => progressDialogVisbility;
            set
            {
                progressDialogVisbility = value;
                OnPropertyChanged();
            }
        }

        public bool ProgressIsIndeterminate
        {
            get => progressIsIndeterminate;
            set
            {
                progressIsIndeterminate = value;
                OnPropertyChanged();
            }
        }

        public double ProgressMax
        {
            get => progressMax;
            set
            {
                progressMax = value;
                OnPropertyChanged();
            }
        }

        public double ProgressMin
        {
            get => progressMin;
            set
            {
                progressMin = value;
                OnPropertyChanged();
            }
        }

        public string ProgressMessage
        {
            get => progressMessage;
            set
            {
                progressMessage = value;
                OnPropertyChanged();
            }
        }

        public string ProgressTitle
        {
            get => progressTitle;
            set
            {
                progressTitle = value;
                OnPropertyChanged();
            }
        }

        public double ProgressValue
        {
            get => progressValue;
            set
            {
                progressValue = value;
                OnPropertyChanged();
            }
        }

        #endregion
    }
}
