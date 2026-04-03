using System;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace POPSManager.Controls
{
    public partial class ProgressPanel : UserControl
    {
        private readonly DoubleAnimation spinnerAnimation;

        public ProgressPanel()
        {
            InitializeComponent();

            // Animación del spinner
            spinnerAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1),
                RepeatBehavior = RepeatBehavior.Forever
            };
        }

        public void UpdateProgress(int value)
        {
            ProgressBarControl.Value = value;
        }

        public void UpdateStatus(string text)
        {
            StatusText.Text = text;
        }

        public void StartSpinner()
        {
            SpinnerRotate.BeginAnimation(
                System.Windows.Media.RotateTransform.AngleProperty,
                spinnerAnimation
            );
        }

        public void StopSpinner()
        {
            SpinnerRotate.BeginAnimation(
                System.Windows.Media.RotateTransform.AngleProperty,
                null
            );
        }
    }
}
