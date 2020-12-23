using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace IFCImportUI
{
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
        enum ExportStatus { auto, notLoaded, waitLoading, failedToLoad, loaded,
            exported, waitExporting, failedToExport };

        Ifc _ifc;
        string _msgNotLoaded = "Please choose an IFC file...";
        string _msgWaitLoading = "Please wait while loading the IFC file...";
        string _msgFailedToLoad = "Failed to load the IFC file...";
        string _msgLoaded = "You may now export the data into Spider Project";
        string _msgExported = "IFC data has been successfully exported!";
        string _msgWaitExporting = "Please wait while exporting the IFC file...";
        string _msgFailedToExport = "Error exporting the IFC file...";

        System.Windows.Media.SolidColorBrush _disableColor = new System.Windows.Media.SolidColorBrush(SystemColors.GrayTextColor);
        System.Windows.Media.SolidColorBrush _activeColor = new System.Windows.Media.SolidColorBrush(SystemColors.ControlTextColor);

        ObservableCollection<Node> _nodes;
        string _wexbimPath;

        public MainWindow()
        {
            InitializeComponent();
            setIfcStatusControls(ExportStatus.notLoaded);
            tvIFC.Height = System.Windows.SystemParameters.PrimaryScreenHeight - 200;  // 500;
        }

        private void setIfcStatusControls( ExportStatus status )
        {
            if (status == ExportStatus.auto && _nodes == null)
                tbPrompt.Text = _msgNotLoaded;
            else if (status == ExportStatus.auto && _nodes != null)
                tbPrompt.Text = "";
            else if (status == ExportStatus.waitLoading)
                tbPrompt.Text = _msgWaitLoading;
            else if (status == ExportStatus.loaded)
                tbPrompt.Text = _msgLoaded;
            else if (status == ExportStatus.notLoaded)
                tbPrompt.Text = _msgNotLoaded;
            else if (status == ExportStatus.failedToLoad)
                tbPrompt.Text = _msgFailedToLoad;
            else if (status == ExportStatus.waitExporting)
                tbPrompt.Text = _msgWaitExporting;
            else if (status == ExportStatus.exported)
                tbPrompt.Text = _msgExported;
            else if (status == ExportStatus.failedToExport)
                tbPrompt.Text = _msgFailedToExport;
            if (_nodes != null && _nodes.Count > 0)
            {
                cmbLevels.IsEnabled = true;
                btnWexbimPath.IsEnabled = true;
                btnExport.IsEnabled = true;
                //tbVolumePropertyName.IsEnabled = false;
                //tbCostPropertyName.IsEnabled = false;
                tbVolumePropertyNameTitle.Foreground = _disableColor;
                tbCostPropertyNameTitle.Foreground = _disableColor;
                cmbLevelsTitle.Foreground = _activeColor;
            }
            else
            {
                cmbLevels.IsEnabled = false;
                btnWexbimPath.IsEnabled = false;
                btnExport.IsEnabled = false;
                //tbVolumePropertyName.IsEnabled = true;
                //tbCostPropertyName.IsEnabled = true;
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

        private async void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            setIfcStatusControls(ExportStatus.waitLoading);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true )
            {
                tvIFC.ItemsSource = null;
                cmbLevels.Items.Clear();

                string vname = tbVolumePropertyName.Text;
                string cname = tbCostPropertyName.Text;
                int maxLevel = await Task.Run(() => {
                    return LoadNodes(openFileDialog.FileName, ref vname, ref cname);
                });
                if( _ifc.isParsedOk() )
                {
                    tvIFC.ItemsSource = _nodes;
                    setIfcStatusControls(ExportStatus.loaded);
                    tbIfcPath.Text = openFileDialog.FileName;
                    tbIfcPath.Height = System.Double.NaN;// GridLength.Auto;
                }
                else
                {
                    setIfcStatusControls(ExportStatus.failedToLoad);
                    tbIfcPath.Text = null;
                    tbIfcPath.Height = 0;
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

        private int LoadNodes( string fileName, ref string volumeProperty, ref string costProperty  )
        {
            if (_nodes != null)
                _nodes.Clear();
            else
                _nodes = new ObservableCollection<Node>();
            int maxLevel = 1;
            _ifc = new Ifc( fileName, ref _nodes, ref maxLevel, ref volumeProperty, ref costProperty );
            return maxLevel;
        }

        private void btnWexbimPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = false;
            openFileDialog.Filter = "Wexbim Files(*.wexbim)|*.wexbim|All Files(*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                _wexbimPath = openFileDialog.FileName;
                tbWexbimPath.Text = _wexbimPath;
                tbWexbimPath.Height = System.Double.NaN;
            } else
            {
                _wexbimPath = null;
                tbWexbimPath.Text = null;
                tbWexbimPath.Height = 0;
            }
        }

        private async void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if( !_ifc.isParsedOk() )
                return;
            setIfcStatusControls(ExportStatus.waitExporting);

            //System.Threading.Thread th = new System.Threading.Thread( () => Export(ref _wexbimPath) );
            //th.Start();
            int status = await Task.Run( () => { return Export(ref _wexbimPath); } );
            if (status == 0)
                setIfcStatusControls(ExportStatus.exported);
            else
                setIfcStatusControls(ExportStatus.failedToExport);
        }

        private int Export( ref string _wexbimPath )
        {
            if (_wexbimPath != null && !_wexbimPath.Equals(""))    // If there is a wexbim path specified
            {
                int wexbimStatus = _ifc.createWexbim(_wexbimPath);
            }
            int n = 0;
            string dest = "[";
            exportNodes(ref _nodes, ref dest, 0, ref n);
            dest += "]";
            int status = Api.Run(ref dest);
            return status;
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
                    if( node.CalculatedVolume.Length > 0 )
                        dest += ", \"CalculatedVolume\":" + node.CalculatedVolume;
                    if( node.Volume.Length > 0 )
                        dest += ", \"VolPlan\":" + node.Volume;
                    if( node.Cost.Length > 0 )
                        dest += ", \"Cost\":" + node.Cost;
                    dest += ", \"f_Model\":\"" + node.GlobalId + "\"";
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
