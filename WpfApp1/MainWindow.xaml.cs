using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using Microsoft.Win32;

namespace WpfApp1
{
    public class Node : INotifyPropertyChanged
    {
        public ObservableCollection<Node> Nodes { get; set; }

        public int Level { get; set; }
        public string GlobalId { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string Volume { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }

    public partial class MainWindow : Window
    {
        ObservableCollection<Node> _nodes;

        public MainWindow()
        {
            InitializeComponent();
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
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true || true)
            {
                _nodes = new ObservableCollection<Node>();
                int maxLevel=1;
                Ifc.parseFile(openFileDialog.FileName, ref _nodes, ref maxLevel);
                tvIFC.ItemsSource = _nodes;
                expandToLevel(1, _nodes);

                for (int i = 1; i <= maxLevel; i++)
                    cmbLevels.Items.Add(new ComboBoxItem { Content = i.ToString() });
                cmbLevels.SelectedIndex = 0;
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
