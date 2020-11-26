using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using Microsoft.Win32;

namespace WpfApp1
{

    enum ExportStatus { auto, notLoaded, waitLoading, failedToLoad, loaded, exported, waitExporting, failedToExport };

    public class Node : INotifyPropertyChanged
    {
        public ObservableCollection<Node> Nodes { get; set; }

        public int Level { get; set; }
        public string GlobalId { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string CalculatedVolume { get; set; }
        public string Volume { get; set; }
        public string Cost { get; set; }

        private bool isExpanded;
        public bool IsExpanded {
            get { return isExpanded; }
            set
            {
                if (value != this.isExpanded)
                {
                    this.isExpanded = value;
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        private bool isChecked;
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (value != this.IsChecked)
                {
                    this.isChecked = value;
                    NotifyPropertyChanged("IsChecked");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }

    public partial class MainWindow : Window
    {
        Ifc _ifc;
        string _msgNotLoaded = "Please choose an IFC file...";
        string _msgWaitLoading = "Please wait while loading IFC...";
        string _msgFailedToLoad = "Failed to load IFC...";
        string _msgLoaded = "You may now export the data into Spider Project";
        string _msgExported = "IFC data has been successfully exported!";
        string _msgWaitExporting = "Please wait while exporting IFC...";
        string _msgFailedToExport = "Error exporting IFC...";

        System.Windows.Media.SolidColorBrush _disableColor = new System.Windows.Media.SolidColorBrush(SystemColors.GrayTextColor);
        System.Windows.Media.SolidColorBrush _activeColor = new System.Windows.Media.SolidColorBrush(SystemColors.ControlTextColor);

        ObservableCollection<Node> _nodes;
        string _wexbimPath;

        public MainWindow()
        {
            InitializeComponent();
            setIfcStatusControls(ExportStatus.notLoaded);
        }

        private void setIfcStatusControls( ExportStatus status )
        {
            if (status == ExportStatus.auto && _nodes == null)
                tbChooseIFC.Text = _msgNotLoaded;
            else if (status == ExportStatus.auto && _nodes != null)
                tbChooseIFC.Text = "";
            else if (status == ExportStatus.waitLoading)
                tbChooseIFC.Text = _msgWaitLoading;
            else if (status == ExportStatus.loaded)
                tbChooseIFC.Text = _msgLoaded;
            else if (status == ExportStatus.notLoaded)
                tbChooseIFC.Text = _msgNotLoaded;
            else if (status == ExportStatus.failedToLoad)
                tbChooseIFC.Text = _msgFailedToLoad;
            else if (status == ExportStatus.waitExporting)
                tbChooseIFC.Text = _msgWaitExporting;
            else if (status == ExportStatus.exported)
                tbChooseIFC.Text = _msgExported;
            else if (status == ExportStatus.failedToExport)
                tbChooseIFC.Text = _msgFailedToExport;
            if (_nodes != null && _nodes.Count > 0)
            {
                cmbLevels.IsEnabled = true;
                btnWexbimPath.IsEnabled = true;
                btnExport.IsEnabled = true;
                tbVolumePropertyName.IsEnabled = false;
                tbCostPropertyName.IsEnabled = false;
                tbVolumePropertyNameTitle.Foreground = _disableColor;
                tbCostPropertyNameTitle.Foreground = _disableColor;
                cmbLevelsTitle.Foreground = _activeColor;
            }
            else
            {
                cmbLevels.IsEnabled = false;
                btnWexbimPath.IsEnabled = false;
                btnExport.IsEnabled = false;
                tbVolumePropertyName.IsEnabled = true;
                tbCostPropertyName.IsEnabled = true;
                tbVolumePropertyNameTitle.Foreground = _activeColor;
                tbCostPropertyNameTitle.Foreground = _activeColor;
                cmbLevelsTitle.Foreground = _disableColor;
            }
        }

        private void cmbLevels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            expandToLevel(cmb.SelectedIndex + 1, _nodes);
        }

        private void tvIFC_Expanded(object sender, RoutedEventArgs e)
        {
            if (tvIFC.SelectedItem != null)
                (tvIFC.SelectedItem as Node).IsExpanded = !(tvIFC.SelectedItem as Node).IsExpanded;
        }

        private void expandToLevel( int maxLevel, ObservableCollection<Node> nodes)
        {
            foreach(var node in nodes)
            {
                if (node.Level < maxLevel) {
                    node.IsExpanded = true;
                }
                else {
                    node.IsExpanded = false;
                }
                if (node.Nodes != null) {
                    expandToLevel(maxLevel, node.Nodes);
                }
            }
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            setIfcStatusControls(ExportStatus.waitLoading);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true )
            {
                if (_nodes != null)
                {
                    tvIFC.ItemsSource = null;
                    _nodes.Clear();
                    cmbLevels.Items.Clear();
                }
                else
                {
                    _nodes = new ObservableCollection<Node>();
                }
                int maxLevel=1;
                _ifc = new Ifc(openFileDialog.FileName, ref _nodes, ref maxLevel, 
                    tbVolumePropertyName.Text, tbCostPropertyName.Text);
                if( _ifc.isParsedOk() )
                {
                    tvIFC.ItemsSource = _nodes;
                    setIfcStatusControls(ExportStatus.loaded);
                }
                else
                {
                    setIfcStatusControls(ExportStatus.failedToLoad);
                }
                expandToLevel(1, _nodes);

                for (int i = 1; i <= maxLevel; i++)
                    cmbLevels.Items.Add(new ComboBoxItem { Content = i.ToString() });
                cmbLevels.SelectedIndex = 0;
            } else
            {
                setIfcStatusControls(ExportStatus.auto);
            }
        }

        private void btnWexbimPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                _wexbimPath = openFileDialog.FileName;
                tbWexbimPath.Text = _wexbimPath;
            } else
            {
                tbWexbimPath.Text = null;
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            setIfcStatusControls(ExportStatus.waitExporting);

            string dest = "";
            int n = 0;
            dest += "{ array: [ ";
            exportNodes(ref _nodes, ref dest, 0, ref n);
            dest += " ] }";

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"../../../dest.txt"))
            {
                file.WriteLine(dest);
            }

            setIfcStatusControls(ExportStatus.exported);
        }

        private void exportNodes( ref ObservableCollection<Node> nodes, ref string dest, int skippedLevels, ref int n )
        {
            int skipLevel;

            foreach( var node in nodes )
            {
                if( node.IsChecked ) {
                    if (n > 0)
                        dest += ",";                                                      
                    dest += "{";
                    dest += "\"Level\":" + (node.Level - skippedLevels).ToString();
                    dest += ", \"Code\":\"" + node.GlobalId + "\"";
                    dest += ", \"Name\":\"" + node.Name + "\"";
                    dest += ", \"CalculatedVolume\":\"" + node.CalculatedVolume + "\"";
                    dest += ", \"Volume\":\"" + node.Volume + "\"";
                    dest += ", \"Cost\":\"" + node.Cost + "\"";
                    dest += ", \"f_Model:\"" + node.GlobalId + "\"";
                    dest += "}";
                    n++;
                    skipLevel = 0;
                }
                else {
                    skipLevel = 1;
                }

                if (!node.IsExpanded)
                    continue;
                if (node.Nodes == null)
                    continue;
                if (node.Nodes.Count == 0)
                    continue;
                ObservableCollection<Node> childNodes = node.Nodes;
                exportNodes(ref childNodes, ref dest, skippedLevels + skipLevel, ref n);
            }
        }
    }
}
