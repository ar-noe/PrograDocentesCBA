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
    /// Interaction logic for Schedules.xaml
    /// </summary>
    public partial class Schedules : Window
    {

        DataClassesDocentesCBA2DataContext dcDB;
        int idP;
        public Schedules(int idProfessor)
        {
            InitializeComponent();
            string connStr = ConfigurationManager.ConnectionStrings["WpfAppDBLingP3.Properties.Settings.ConnectionString"].ConnectionString;

            dcDB = new DataClassesDocentesCBA2DataContext(connStr);

            idP = idProfessor;

            loadDataDB(idP);
        }

        //salir de la sesión
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow signUp = new MainWindow();
            signUp.Show();
            this.Close();
        }



        //mostrar mensajes de error u otros
        private void ShowMessage(string message, bool isError)
        {
            MessageBox.Show(message, isError ? "Error" : "Información",
                          MessageBoxButton.OK,
                          isError ? MessageBoxImage.Error : MessageBoxImage.Information);
        }

        private void loadDataDB(int idP)
        {
            try
            {
                var searchQuery = (from personId in dcDB.Docente
                                          where personId.IdPersona == idP
                                          select personId).ToList();

                foreach (var prof in searchQuery)
                {
                    var profFound = (from professor in dcDB.ModuloImpartido
                                       where professor.IdDocente == prof.IdDocente
                                       select professor).ToList();
                    dgModulos.ItemsSource = profFound;
                }
            }
            catch (Exception e)
            {
                {
                    MessageBox.Show("Error: " + e.Message);
                }
            }
        }
    }
}
