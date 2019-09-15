using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ConfigGUI.MultiSelect;
using OsuRTDataProvider.Listen;
using RealTimePPDisplayer.Displayer;
using RealTimePPDisplayer.MultiOutput;
using Sync.Tools;

namespace RealTimePPDisplayer.Gui
{
    /// <summary>
    /// OverlayEditor.xaml 的交互逻辑
    /// </summary>
    partial class MultiOutputEditor : Window
    {
        public static Dictionary<string,object> OsuModes => typeof(OsuPlayMode).GetEnumNames().Where((s) => s != "Unknown").ToDictionary(s=>s,s=>s as object);
        public static IEnumerable<string> MultiDisplayerTypes => RealTimePPDisplayerPlugin.Instance.MultiDisplayerTypes;
        public static IEnumerable<string> FormatterTypes => RealTimePPDisplayerPlugin.Instance.FormatterTypes;

        class MultiOutputItemProxy : DependencyObject,INotifyPropertyChanged
        {
            private MultiOutputItem _object;

            #region Property
            public string Name
            {
                get => _object.name;
                set
                {
                    OnNameChange?.Invoke(_object.name,value);
                    _object.name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }

            public string Format
            {
                get => _object.format;
                set
                {
                    _object.format = value;
                    OnPropertyChanged(nameof(Format));
                    OnFormatChange?.Invoke(_object.name, _object.format);
                }
            }

            public string Type
            {
                get => _object.type;
                set
                {
                    _object.type = value;
                    OnPropertyChanged(nameof(Type));
                    OnDisplayerTypeChange?.Invoke(_object.name, value);
                }
            }

            public bool Smooth
            {
                get => _object.smooth;
                set {
                    _object.smooth = value;
                    OnPropertyChanged(nameof(Smooth));
                    OnSmoothChange?.Invoke(_object.name, value); 
                }
            }

            public string Formatter
            {
                get => _object.formatter;
                set
                {
                    if (value != _object.formatter)
                    {
                        if (MessageBox.Show("Changing formatter will clear the format, will it continue?",
                            "Hint",
                            MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            _object.formatter = value;
                            OnFormatterChange?.Invoke(_object.name, value);
                            OnPropertyChanged(nameof(Format));
                        }
                    }
                    OnPropertyChanged(nameof(Formatter));
                }
            }

            public Dictionary<string,object> Modes
            {
                get => _object.modes.Split(',').Select(s=>s.Trim()).ToDictionary(s=>s,s=>s as object);
                set {
                    _object.modes = string.Join(",",value.Keys);
                    OnPropertyChanged(nameof(Modes));
                }
            }

            #endregion

            public MultiOutputItemProxy(MultiOutputItem obj, MultiOutputEditor win)
            {
                _object = obj;
                DeleteItem = new DeleteCommand(win);
                OpenFormatEditor = new OpenFormatEditorCommand(win);
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                //Setting.OverlayConfigs.WriteToMmf(_needUpdateFontPropertyList.Contains(propertyName));
            }

            public DeleteCommand DeleteItem { get; set; }
            public OpenFormatEditorCommand OpenFormatEditor { get; set; }

            public class DeleteCommand : ICommand
            {
                private MultiOutputEditor m_window;

                public event EventHandler CanExecuteChanged
                {
                    add { }
                    remove { }
                }

                public DeleteCommand(MultiOutputEditor window)
                {
                    m_window = window;
                }

                public bool CanExecute(object parameter) => true;

                public void Execute(object parameter)
                {
                    var proxy = parameter as MultiOutputItemProxy;

                    m_window._observableCollection.Remove(proxy);
                    OnDisplayerRemove?.Invoke(proxy._object.name);
                    Setting.MultiOutputItems.Remove(proxy._object);
                }
            }

            public class OpenFormatEditorCommand : ICommand
            {
                private MultiOutputEditor m_window;

                class FormatProxy : IConfigurable
                {
                    private MultiOutputItemProxy _object;
                    public FormatProxy(MultiOutputItemProxy @object)
                    {
                        _object = @object;
                    }

                    public ConfigurationElement FormatElement
                    {
                        get => _object.Format;
                        set => _object.Format = value.ToString();
                    }

                    #region unused
                    public void onConfigurationLoad()
                    {
                        
                    }

                    public void onConfigurationReload()
                    {
                        
                    }

                    public void onConfigurationSave()
                    {
                        
                    }

                    #endregion
                }

                public event EventHandler CanExecuteChanged
                {
                    add { }
                    remove { }
                }

                public OpenFormatEditorCommand(MultiOutputEditor window)
                {
                    m_window = window;
                }

                public bool CanExecute(object parameter) => true;

                public void Execute(object parameter)
                {
                    var proxy = parameter as MultiOutputItemProxy;
                    var elementInstance = new FormatProxy(proxy);
                    var prop = typeof(FormatProxy).GetProperty("FormatElement");
                    var formatter = RealTimePPDisplayerPlugin.Instance.NewFormatter(proxy._object.formatter, proxy._object.format);
                    var formatEditor = new FormatEditor(prop, elementInstance,formatter);
                    formatEditor.ShowDialog();
                }
            }
        }

        public static event Action<string> OnDisplayerRemove;
        public static event Action<MultiOutputItem> OnDisplayerNew;
        public static event Action<string, string> OnDisplayerTypeChange;
        public static event Action<string,string> OnNameChange;
        public static event Action<string,string> OnFormatChange;
        public static event Action<string, string> OnFormatterChange;
        public static event Action<string, bool> OnSmoothChange;

        private ObservableCollection<MultiOutputItemProxy> _observableCollection;

        public MultiOutputEditor()
        {
            InitializeComponent();
            _observableCollection = new ObservableCollection<MultiOutputItemProxy>(Setting.MultiOutputItems.Select(c=>new MultiOutputItemProxy(c,this)));

            ConfigList.ItemsSource = _observableCollection;
        }

        private void OverlayEditor_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private int FindMaxNumber()
        {
            Regex regex = new Regex(@"multi-(\d)*");
            int max = 0;
            foreach(var item in Setting.MultiOutputItems)
            {
                string ns = regex.Match(item.name).Groups[1].Value;
                if(int.TryParse(ns,out int n)){
                    max = Math.Max(max, n);
                }
            }
            return max;
        }

        private void AddNewItemButton_Click(object sender, RoutedEventArgs e)
        {
            string name = $"multi-{FindMaxNumber()+1}";

            var item = new MultiOutputItem()
            {
                name = name,
                format = RealTimePPDisplayerPlugin.Instance.GetFormatterDefaultFormat("rtpp-fmt"),
                type = RealTimePPDisplayerPlugin.Instance.MultiDisplayerTypes.FirstOrDefault(),
                smooth = false,
                modes = "Osu",
                formatter = "rtpp-fmt"
            };
            OnDisplayerNew?.Invoke(item);
            var proxy = new MultiOutputItemProxy(item, this);
            _observableCollection.Add(proxy);
            Setting.MultiOutputItems.Add(item);
        }

        private void MultiSelectComboBox_Click(object sender, RoutedEventArgs e)
        {
            var s = sender as MultiSelectComboBox;
            var proxy = s.DataContext as MultiOutputItemProxy;
            proxy.Modes = s.SelectedItems;
        }
    }
}
