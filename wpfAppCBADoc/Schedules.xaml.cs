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
            string connStr = ConfigurationManager.ConnectionStrings["wpfAppCBADoc.Properties.Settings.PrograCBADocentesConnectionString"].ConnectionString;

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
                                     join h in dcDB.Horario on professor.IdHorario equals h.IdHorario
                                     join m in dcDB.Modulo on professor.IdModulo equals m.IdModulo
                                     join c in dcDB.Curso on professor.IdModulo equals c.IdCurso
                                     where professor.IdDocente == prof.IdDocente
                                     select new
                                     {
                                         Horario = h.HoraInicio + " - " + h.HoraFinal,
                                         Modulo = m.Nombre,
                                         Curso = c.Nombre,
                                         ID = professor.IdModuloImp
                                     }).ToList();

                    dgModulos.ItemsSource = profFound;
                }
            }
            catch (Exception e)
            {
                {
                    ShowMessage("Error: " + e.Message, true);
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (dgModulos.SelectedValue != null)
            {
                dynamic idModulo = dgModulos.SelectedItem; //omite la comprobación de tipos en tiempo de compilación, es decir que soluciona al ejecutar, no al compilar (confia, google me lo dijo) por eso reconoce de esta manera el id y no con 'var'

                int id = idModulo.ID;

                EditarModulo seeInfo = new EditarModulo(id);

                seeInfo.Show();
                this.Close();
            }
            else
            {
                ShowMessage("Select a module to view information", true);
            }
        }

    }
}
