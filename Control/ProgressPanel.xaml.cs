using System;
using System.Windows;
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

            spinnerAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1),
                RepeatBehavior = RepeatBehavior.Forever
            };
        }

        // =====================================================
        // PROPIEDAD DE DEPENDENCIA: ProgressValue (int)
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
            var newValue = (int)e.NewValue;
            panel.ProgressBarControl.Value = newValue;
        }

        // =====================================================
        // PROPIEDAD DE DEPENDENCIA: StatusText (string)
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
            panel.StatusText.Text = (string)e.NewValue;
        }

        // =====================================================
        // PROPIEDAD DE DEPENDENCIA: IsSpinnerActive (bool)
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
        // MÉTODOS DE CONTROL DEL SPINNER
        // =====================================================
        public void StartSpinner()
        {
            // Aplicar animación al RotateTransform del Ellipse
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
        // MÉTODOS LEGACY (mantenidos por compatibilidad)
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