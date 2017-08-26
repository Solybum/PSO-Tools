using System.Windows;

namespace PSOBB_Input_Map
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel mvm;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this.mvm = new MainViewModel();
        }
    }
}
