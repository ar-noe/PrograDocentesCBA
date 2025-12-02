using System;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace wpfAppCBADoc
{
    public partial class LogInUsr : Window
    {
        private DataClassesDocentesBDDataContext dcBd;
        private Persona persona;
        private LogIn ventanaAnterior;

        // Constructor que recibe 2 argumentos: Persona y ventana anterior
        public LogInUsr(Persona personaCreada, LogIn ventanaAnterior)
        {
            InitializeComponent();
            this.persona = personaCreada;
            this.ventanaAnterior = ventanaAnterior;
            InitializeDatabaseConnection();
            LoadComboBoxData();
            MostrarInformacionPersona();
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

        private void LoadComboBoxData()
        {
            try
            {
                // Cargar tipos de roles usando sintaxis de consulta LINQ
                var queryRoles = from rol in dcBd.Rol
                                 select rol;

                cmbUserType.ItemsSource = queryRoles.ToList();
                cmbUserType.DisplayMemberPath = "Nombre";
                cmbUserType.SelectedValuePath = "IdRol";

                if (cmbUserType.Items.Count > 0)
                    cmbUserType.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowMessage("Error loading roles: " + ex.Message, true);
            }
        }

        private void MostrarInformacionPersona()
        {
            txtInfoPersona.Text = $"{persona.Nombres} {persona.ApPat} {persona.ApMat}\n" +
                                 $"ID: {persona.CI} | Birth Date: {persona.FechaNac:dd/MM/yyyy}";
        }

        private void btnCrearUsuario_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string correoUsr = txtUsername.Text.Trim();
                string passwrdUsr = txtPassword.Password;

                // Validaciones de usuario
                if (string.IsNullOrEmpty(correoUsr) || string.IsNullOrEmpty(passwrdUsr))
                {
                    ShowMessage("Please enter both email and password", true);
                    return;
                }

                if (cmbUserType.SelectedItem == null)
                {
                    ShowMessage("Please select a user type", true);
                    return;
                }

                // VERIFICAR si el usuario YA EXISTE usando sintaxis de consulta LINQ
                var queryUsuarioExiste = from usr in dcBd.Usuario
                                         where usr.Correo == correoUsr
                                         select usr;

                bool usuarioExiste = queryUsuarioExiste.Any();

                if (usuarioExiste)
                {
                    ShowMessage("This email is already registered. Please use a different email.", true);
                    return;
                }

                // Obtener el rol seleccionado
                var rolSeleccionado = cmbUserType.SelectedItem as Rol;
                if (rolSeleccionado == null)
                {
                    ShowMessage("Please select a valid user type", true);
                    return;
                }

                // CREAR NUEVO USUARIO
                Usuario nuevoUsuario = new Usuario
                {
                    Correo = correoUsr,
                    Contrasenia = passwrdUsr,
                    IdRol = rolSeleccionado.IdRol,
                    IdPersona = persona.IdPersona
                };

                // Insertar usuario en la base de datos
                dcBd.Usuario.InsertOnSubmit(nuevoUsuario);
                dcBd.SubmitChanges();

                ShowMessage($"User registered successfully! Welcome {correoUsr}", false);

                MessageBox.Show("Registration completed successfully!", "Success",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                // Cerrar ambas ventanas
                this.Close();
                if (ventanaAnterior != null)
                    ventanaAnterior.Close();
            }
            catch (Exception ex)
            {
                ShowMessage("Registration error: " + ex.Message, true);
            }
        }

        private void btnRetroceder_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you want to go back to modify personal data?",
                                       "Modify Personal Data",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Mostrar la ventana anterior
                    if (ventanaAnterior != null)
                    {
                        ventanaAnterior.ActualizarDatosPersona(persona);
                        ventanaAnterior.Show();
                    }

                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error returning to previous window: " + ex.Message,
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to cancel account creation?\n" +
                                       "The personal data will be deleted.",
                                       "Confirm Cancellation",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Eliminar la persona creada usando sintaxis de consulta LINQ
                try
                {
                    var queryPersona = from p in dcBd.Persona
                                       where p.IdPersona == persona.IdPersona
                                       select p;

                    var personaAEliminar = queryPersona.FirstOrDefault();

                    if (personaAEliminar != null)
                    {
                        dcBd.Persona.DeleteOnSubmit(personaAEliminar);
                        dcBd.SubmitChanges();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting personal data: " + ex.Message,
                                  "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Mostrar ventana anterior y cerrar esta
                if (ventanaAnterior != null)
                    ventanaAnterior.Show();

                this.Close();
            }
        }

        private void ShowMessage(string message, bool isError)
        {
            txtMessage.Text = message;
            txtMessage.Foreground = isError ? Brushes.Red : Brushes.Green;
            txtMessage.Visibility = Visibility.Visible;
        }

        private void cmbUserType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            
        }
    }
}