using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace POPSManager.Controls
{
    public partial class ProgressPanel : System.Windows.Controls.UserControl
    {
        private readonly DoubleAnimation spinnerAnimation;

        public ProgressPanel()
        {
            InitializeComponent();

            spinnerAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1),
                RepeatBehavior = RepeatBehavior.Forever
            };
        }

        // =====================================================
        // ProgressValue
        // =====================================================
        public static readonly DependencyProperty ProgressValueProperty =
            DependencyProperty.Register(
                nameof(ProgressValue),
                typeof(int),
                typeof(ProgressPanel),
                new PropertyMetadata(0, OnProgressValueChanged));

        public int ProgressValue
        {
            get => (int)GetValue(ProgressValueProperty);
            set => SetValue(ProgressValueProperty, value);
        }

        private static void OnProgressValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (ProgressPanel)d;
            panel.ProgressBarControl.Value = (int)e.NewValue;
        }

        // =====================================================
        // StatusText
        // =====================================================
        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register(
                nameof(StatusText),
                typeof(string),
                typeof(ProgressPanel),
                new PropertyMetadata(string.Empty, OnStatusTextChanged));

        public string StatusText
        {
            get => (string)GetValue(StatusTextProperty);
            set => SetValue(StatusTextProperty, value);
        }

        private static void OnStatusTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (ProgressPanel)d;
            panel.StatusTextBlock.Text = (string)e.NewValue;
        }

        // =====================================================
        // IsSpinnerActive
        // =====================================================
        public static readonly DependencyProperty IsSpinnerActiveProperty =
            DependencyProperty.Register(
                nameof(IsSpinnerActive),
                typeof(bool),
                typeof(ProgressPanel),
                new PropertyMetadata(false, OnIsSpinnerActiveChanged));

        public bool IsSpinnerActive
        {
            get => (bool)GetValue(IsSpinnerActiveProperty);
            set => SetValue(IsSpinnerActiveProperty, value);
        }

        private static void OnIsSpinnerActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (ProgressPanel)d;

            if ((bool)e.NewValue)
                panel.StartSpinner();
            else
                panel.StopSpinner();
        }

        // =====================================================
        // Spinner
        // =====================================================
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

        // =====================================================
        // Métodos legacy
        // =====================================================
        public void UpdateProgress(int value)
        {
            ProgressValue = value;
        }

        public void UpdateStatus(string text)
        {
            StatusText = text;
        }
    }
}