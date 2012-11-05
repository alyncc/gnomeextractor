using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GnomeExtractor.Properties;
using System.Resources;
using System.Reflection;
using Infralution.Localization.Wpf;
using System.ComponentModel;
using System.Threading;
using System.Globalization;

namespace GnomeExtractor
{
    public partial class FastEditor : Window
    {
        bool isOkClicked;
        bool isFixedMode;
        ResourceManager resourceManager;
        int value;

        public FastEditor()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(CultureManager.UICulture.Name);
            resourceManager = new ResourceManager("GnomeExtractor.Properties.Resources", Assembly.GetExecutingAssembly());

            InitializeComponent();

            fixedEditSlider.Value = Settings.Default.FastEditValue;
            isFixedMode = Settings.Default.FastEditModeIsFixed;
            ControlStats();
        }

        #region Properties
        /// <summary>
        /// Возвращает значение True, если нажата кнопка ОК
        /// </summary>
        public bool IsOkClicked
        { get { return isOkClicked; } }

        /// <summary>
        /// Возвращает значение, введенное пользователем в окне
        /// </summary>
        public int Value
        { get { return value; } }

        /// <summary>
        /// Возвращает значение True, если пользователь выбрал редактирование фиксированной величиной
        /// </summary>
        public bool IsFixedMode
        { get { return isFixedMode; } }
        #endregion

        private void fixedModeRadioButton_Click(object sender, RoutedEventArgs e)
        {
            isFixedMode = true;
            ControlStats();
        }

        private void randomModeRadioButton_Click(object sender, RoutedEventArgs e)
        {
            isFixedMode = false;
            ControlStats();
        }

        private void ControlStats()
        {
            randomModeRadioButton.IsChecked = !(fixedModeRadioButton.IsChecked = isFixedMode);
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            value = (int)fixedEditSlider.Value;
            
            isOkClicked = true;
            Settings.Default.FastEditValue = (int)fixedEditSlider.Value;
            Settings.Default.FastEditModeIsFixed = isFixedMode = (bool)fixedModeRadioButton.IsChecked;
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int temp;
            if (!Int32.TryParse((sender as TextBox).Text, out temp)) fastEditorTextBox.Text = "5";
            else if (temp < 5) fastEditorTextBox.Text = "5";
            fastEditorTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }
    }
}
