using System;
using System.Collections.Generic;
using System.Configuration;
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

namespace wpfAppCBADoc
{
    /// <summary>
    /// Interaction logic for ManagerMainWindow.xaml
    /// </summary>
    public partial class ManagerMainWindow : Window
    {
        private DataClassesDocentesBDDataContext dcBd;
        public ManagerMainWindow()
        {
            InitializeDatabaseConnection();
            LoadComboBoxData();
            InitializeComponent();
        }

        private void InitializeDatabaseConnection()
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["wpfAppCBADoc.Properties.Settings.CBADocentesConnectionString"].ConnectionString;
                dcBd = new DataClassesDocentesBDDataContext(connStr);
            }
            catch (Exception ex)
            {
                ShowMessage("Error connecting to database: " + ex.Message, true);
            }
        }

        private void ShowMessage(string message, bool isError)
        {
            txtMessage.Text = message;
            txtMessage.Foreground = isError ? Brushes.Red : Brushes.Green;
            txtMessage.Visibility = Visibility.Visible;
        }

        private void LoadComboBoxData()
        {
            try
            {
                // LINQ
                var queryRoles = from classroom in dcBd.Aula
                                 select classroom;

                cmbSelectClassroom.ItemsSource = queryRoles.ToList();
                cmbSelectClassroom.DisplayMemberPath = "NumeroAula";
                cmbSelectClassroom.SelectedValuePath = "IdAula";

                if (cmbSelectClassroom.Items.Count > 0)
                    cmbSelectClassroom.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowMessage("Error loading Classrooms: " + ex.Message, true);
            }
        }

        private void cmbSelectClassroom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
