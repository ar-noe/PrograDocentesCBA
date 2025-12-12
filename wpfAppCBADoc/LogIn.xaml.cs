using System;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using System.Text.RegularExpressions;//para validaciones

namespace wpfAppCBADoc
{
    public partial class LogIn : Window
    {
        private DataClassesDocentesCBA2DataContext dcBd;
        public Persona PersonaCreada { get; private set; }
        private PersonaCreator personaFactory;

        public LogIn()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            personaFactory = new PersonaCreator();
        }

        private void InitializeDatabaseConnection()
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["wpfAppCBADoc.Properties.Settings.PrograCBADocentesConnectionString"].ConnectionString;
                dcBd = new DataClassesDocentesCBA2DataContext(connStr);
            }
            catch (Exception ex)
            {
                ShowMessage("Error connecting to database: " + ex.Message, true);
            }
        }

        private void btnContinuar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarDatosPersona())
                {
                    return;
                }
                    

                // Crear persona
                PersonaCreada = new Persona
                {
                    CI = txtCI.Text.Trim(),
                    Nombres = txtNombres.Text.Trim(),
                    ApPat = txtApPat.Text.Trim(),
                    ApMat = txtApMat.Text.Trim(),
                    FechaNac = dpFechaNac.SelectedDate.Value,
                    TipoPersona = "" 
                };

                // Insertar persona
                dcBd.Persona.InsertOnSubmit(PersonaCreada);
                dcBd.SubmitChanges();

                ShowMessage("Datos personales guardados!", false);

                // Pasar el MISMO DataContext
                LogInUsr logInUsr = new LogInUsr(PersonaCreada, this, dcBd);
                logInUsr.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                ShowMessage("Error: " + ex.Message, true);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to cancel registration?",
                                       "Confirm Cancellation",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MainWindow signUp = new MainWindow();
                signUp.Show();
                this.Close();
            }
        }

        private bool ValidarDatosPersona()
        {

            string ciR = txtCI.Text.Trim();
            string namesR = txtNombres.Text.Trim();
            string patLstName = txtApPat.Text.Trim();
            string matLstName = txtApMat.Text.Trim();
            var fechaR = dpFechaNac.SelectedDate;

            string patronCi = @"^\d{7,8}-?\d{1}$";
            string patronNameG = @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$";


            if (string.IsNullOrEmpty(ciR))
            {
                ShowMessage("Please enter ID number", true);
                return false;
            }
            else if (!Regex.IsMatch(ciR, patronCi))
            {
                ShowMessage("Please enter a valid ID number (only digits)", true);
                return false;
            }

            if (string.IsNullOrEmpty(namesR))
            {
                ShowMessage("Please enter names", true);
                return false;
            }
            else if (!Regex.IsMatch(namesR, patronNameG))
            {
                ShowMessage("Please enter valid names", true);
                return false;
            }

            if (string.IsNullOrEmpty(patLstName) && string.IsNullOrEmpty(matLstName))
            {
                ShowMessage("Please enter at leat one last name", true);
                return false;
            }
            else if (!Regex.IsMatch(patLstName, patronNameG))
            {
                ShowMessage("Please enter a valid last name", true);
                return false;
            }
            else if (!Regex.IsMatch(matLstName, patronNameG))
            {
                ShowMessage("Please enter a valid mother's last name", true);
                return false;
            }

            if (fechaR == null)
            {
                ShowMessage("Please select birth date", true);
                return false;
            }
            else if (fechaR > DateTime.Now)
            {
                ShowMessage("Please select a valid birth date", true);
                return false;
            }

                // Eliminar validación de TipoPersona
                return true;
        }

        public void ActualizarDatosPersona(Persona personaActualizada)
        {
            // Actualizar los controles con los nuevos datos
            txtCI.Text = personaActualizada.CI;
            txtNombres.Text = personaActualizada.Nombres;
            txtApPat.Text = personaActualizada.ApPat;
            txtApMat.Text = personaActualizada.ApMat;
            dpFechaNac.SelectedDate = personaActualizada.FechaNac;

            // Mostrar mensaje
            ShowMessage("Personal data updated successfully!", false);
        }

        private void ShowMessage(string message, bool isError)
        {
            txtMessage.Text = message;
            txtMessage.Foreground = isError ? Brushes.Red : Brushes.Green;
            txtMessage.Visibility = Visibility.Visible;
        }
    }
}