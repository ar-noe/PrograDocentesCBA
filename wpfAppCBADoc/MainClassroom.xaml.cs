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
    /// Interaction logic for MainClassroom.xaml
    /// </summary>
    public partial class MainClassroom : Window
    {
        DataClassesDocentesCBA2DataContext dcDB;

        public MainClassroom()
        {
            InitializeComponent();

             string connStr = ConfigurationManager.ConnectionStrings["wpfAppCBADoc.Properties.Settings.PrograCBADocentesConnectionString"].ConnectionString;
            dcDB = new DataClassesDocentesCBA2DataContext(connStr);

            loadDataDB();
        }



        private void loadDataDB()
        {
            try
            {
                //cargar aulas
                var searchQuery = (from aulas in dcDB.Aula
                                   join suc in dcDB.Sucursal on aulas.IdSucursal equals suc.IdSucursal
                                   select aulas.NumeroAula + " - " + suc.Alias).ToList();

                cmbSelectClassroom.ItemsSource = searchQuery;


                //cargar estado
                var statusQuery = (from status in dcDB.EstadoAula
                                   select status.Descripcion).ToList();

                cmbSelectState.ItemsSource = statusQuery;
                

            }
            catch (Exception e)
            {
                {
                    ShowMessage("Error: " + e.Message, true);
                }
            }
        }




        // botones para la navegación entre pestañas 
        private void btnHorarios_Click(object sender, RoutedEventArgs e)
        {
            EditHorarios horarios = new EditHorarios();
            horarios.Show();
            this.Close();
        }

        private void btnModulos_Click(object sender, RoutedEventArgs e)
        {
            MainModulosImp modulos = new MainModulosImp();
            modulos.Show();
            this.Close();
        }
        private void btnDeleteProfessor_Click(object sender, RoutedEventArgs e)
        {
            DeleteProfessor delProf = new DeleteProfessor();
            delProf.Show();
            this.Close();
        }

        //mostrar mensajes de error u otros
        private void ShowMessage(string message, bool isError)
        {
            MessageBox.Show(message, isError ? "Error" : "Información",
                          MessageBoxButton.OK,
                          isError ? MessageBoxImage.Error : MessageBoxImage.Information);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow signUp = new MainWindow();
            signUp.Show();
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                // Aula seleccionada
                string aulaSeleccionada = cmbSelectClassroom.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(aulaSeleccionada))
                {
                    ShowMessage("Por favor, seleccione un aula", true);
                    return;
                }

                string numeroAula = aulaSeleccionada.Split(' ')[0].Trim();

                var aulaToModify = dcDB.Aula
                    .FirstOrDefault(a => a.NumeroAula == numeroAula);

                if (aulaToModify == null)
                {
                    ShowMessage("No se encontró el aula seleccionada", true);
                    return;
                }

                // Estado seleccionado
                string estadoSeleccionado = cmbSelectState.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(estadoSeleccionado))
                {
                    ShowMessage("Por favor, seleccione un estado", true);
                    return;
                }

                int idEstado = dcDB.EstadoAula
                    .Where(s => s.Descripcion == estadoSeleccionado)
                    .Select(s => s.IdEstadoA)
                    .FirstOrDefault();

                if (idEstado == 0)
                {
                    ShowMessage("Estado no válido seleccionado", true);
                    return;
                }

                aulaToModify.IdEstadoA = idEstado;

                dcDB.SubmitChanges();

                ShowMessage("¡Estado del aula actualizado correctamente!", false);

                // Opcional: Limpiar selecciones o recargar datos
                cmbSelectClassroom.SelectedIndex = -1;
                cmbSelectState.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowMessage($"Error al actualizar el aula: {ex.Message}", true);
            }
        }

    }
}
