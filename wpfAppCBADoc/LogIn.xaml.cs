using System;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace wpfAppCBADoc
{
    public partial class LogIn : Window
    {
        private DataClassesDocentesBDDataContext dcBd;
        public Persona PersonaCreada { get; private set; }

        public LogIn()
        {
            InitializeComponent();
            InitializeDatabaseConnection();

            // También ocultar el label si existe
            var labelTipoPersona = this.FindName("lblTipoPersona") as Label;
            if (labelTipoPersona != null)
                labelTipoPersona.Visibility = Visibility.Collapsed;
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

        private void btnContinuar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar datos de persona
                if (!ValidarDatosPersona())
                    return;

                // CREAR NUEVA PERSONA - Sin IdTipoPersona
                PersonaCreada = new Persona
                {
                    CI = txtCI.Text.Trim(),
                    Nombres = txtNombres.Text.Trim(),
                    ApPat = txtApPat.Text.Trim(),
                    ApMat = txtApMat.Text.Trim(),
                    FechaNac = dpFechaNac.SelectedDate.Value
                };

                // Insertar persona en la base de datos
                dcBd.Persona.InsertOnSubmit(PersonaCreada);
                dcBd.SubmitChanges();

                ShowMessage("Personal data saved successfully!", false);

                // Abrir ventana de registro de usuario (ahora llamada LogInUsr)
                LogInUsr logInUsr = new LogInUsr(PersonaCreada, this);
                logInUsr.Show();

                // Ocultar esta ventana en lugar de cerrarla
                this.Hide();
            }
            catch (Exception ex)
            {
                ShowMessage("Error saving personal data: " + ex.Message, true);
                PersonaCreada = null;
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
                this.Close();
            }
        }

        private bool ValidarDatosPersona()
        {
            if (string.IsNullOrEmpty(txtCI.Text.Trim()))
            {
                ShowMessage("Please enter ID number", true);
                return false;
            }

            if (string.IsNullOrEmpty(txtNombres.Text.Trim()))
            {
                ShowMessage("Please enter names", true);
                return false;
            }

            if (string.IsNullOrEmpty(txtApPat.Text.Trim()))
            {
                ShowMessage("Please enter last name", true);
                return false;
            }

            if (string.IsNullOrEmpty(txtApMat.Text.Trim()))
            {
                ShowMessage("Please enter mother's last name", true);
                return false;
            }

            if (dpFechaNac.SelectedDate == null)
            {
                ShowMessage("Please select birth date", true);
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