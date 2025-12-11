using System;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using System.Text.RegularExpressions;//para validaciones

namespace wpfAppCBADoc
{
    public partial class LogInUsr : Window
    {
        private DataClassesDocentesCBA2DataContext dcBd;
        private Persona persona;
        private LogIn ventanaAnterior;


        public LogInUsr(Persona personaCreada, LogIn ventanaAnterior, DataClassesDocentesCBA2DataContext dataContext)
        {
            InitializeComponent();
            this.persona = personaCreada;
            this.ventanaAnterior = ventanaAnterior;

            this.dcBd = dataContext;

            LoadComboBoxData();
            MostrarInformacionPersona();
        }

        private void LoadComboBoxData()
        {
            try
            {
                var queryRoles = from rol in dcBd.Rol
                                 where rol.Nombre != "Estudiante"
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

                // Validaciones
                if (!ValidarDatosUsuario())
                {
                    return;
                }

                if (cmbUserType.SelectedItem == null)
                {
                    ShowMessage("Please select a user type", true);
                    return;
                }

                // Verificar si usuario ya existe
                var usuarioExiste = dcBd.Usuario.Any(usr => usr.Correo == correoUsr);
                if (usuarioExiste)
                {
                    ShowMessage("This email is already registered", true);
                    return;
                }

                // Obtener rol seleccionado
                var rolSeleccionado = cmbUserType.SelectedItem as Rol;
                if (rolSeleccionado == null)
                {
                    ShowMessage("Please select a valid user type", true);
                    return;
                }

                // se crea el registro segun el tipo de persona
                string tipoPersona = rolSeleccionado.Nombre == "Docente" ? "Docente" : "Administrativo";

                // crear registro segun usr
                CrearUsuario(tipoPersona, correoUsr, passwrdUsr, rolSeleccionado);

            }
            catch (Exception ex)
            {
                ShowMessage("Registration error: " + ex.Message, true);
            }
        }

        public bool ValidarDatosUsuario()
        {
            string correoUsr = txtUsername.Text.Trim();
            string passwrdUsr = txtPassword.Password;

            string patronEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            string patronPassword = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";

            if (string.IsNullOrEmpty(correoUsr))
            {
                ShowMessage("Please enter email", true);
                return false;
            }
            else if (!Regex.IsMatch(correoUsr, patronEmail))
            {
                ShowMessage("Please enter a valid email", true);
                return false;
            }

            if (string.IsNullOrEmpty(passwrdUsr))
            {
                ShowMessage("Please enter password", true);
                return false;
            }
            else if (!Regex.IsMatch(passwrdUsr, patronPassword))
            {
                ShowMessage("Please enter a valid password (At least 8 characters, 1 number and 1 special character)", true);
                return false;
            }

            return true;
        }

        private void CrearUsuario(string tipoPersona, string correo, string password, Rol rol)
        {
            try
            {
                // obtener persona creada anteriormente
                var personaEnBD = dcBd.Persona.FirstOrDefault(p => p.IdPersona == persona.IdPersona);
                if (personaEnBD == null)
                {
                    ShowMessage("Error: Person not found", true);
                    return;
                }

                // Actualizar TipoPersona
                personaEnBD.TipoPersona = tipoPersona;

                // Crear Usuario
                Usuario nuevoUsuario = new Usuario
                {
                    Correo = correo,
                    Contrasenia = password,
                    IdRol = rol.IdRol,
                    IdPersona = personaEnBD.IdPersona
                };
                dcBd.Usuario.InsertOnSubmit(nuevoUsuario);

                // Usar Factory Method
                CrearRegistroEspecificoFactory(tipoPersona, personaEnBD.IdPersona, rol);

                // Guardar todo
                dcBd.SubmitChanges();

                // Actualizar la referencia local
                persona = personaEnBD;

                AbrirVentanaSegunRol(rol.IdRol);
            }
            catch (System.Data.SqlClient.SqlException sqlEx)
            {
                // **ERRORES ESPECÍFICOS DE SQL**
                ShowMessage($"Error SQL: {sqlEx.Message}\nNúmero: {sqlEx.Number}", true);
            }
            catch (Exception ex)
            {
                ShowMessage("Error en Factory Method: " + ex.Message, true);
            }
        }

        // Decidir  objeto crear basado en el tipoPersona
        private void CrearRegistroEspecificoFactory(string tipoPersona, int idPersona, Rol rol)
        {
            // 
            switch (tipoPersona)
            {
                case "Docente":
                    // Crear Docente con configuración específica
                    string[] especialidades = {
                        "General English", "Pronunciation Training", "Listening Comprehension",
                        "Teaching Advanced Levels", "TOEFL Preparation", "IELTS Preparation",
                        "Business English", "Media Literacy"
                    };
                    var random = new Random();

                    var docente = new Docente
                    {
                        IdPersona = idPersona,
                        Especialidad = especialidades[random.Next(0, especialidades.Length)]
                    };
                    dcBd.Docente.InsertOnSubmit(docente);
                    break;

                case "Administrativo":
                    // Crear Administrativo con configuración específica
                    var administrativo = new Administrativo
                    {
                        IdPersona = idPersona,
                        Cargo = rol.Nombre
                    };
                    dcBd.Administrativo.InsertOnSubmit(administrativo);
                    break;

                default:
                    throw new ArgumentException($"Tipo de persona no soportado: {tipoPersona}");
            }
        }

        private void AbrirVentanaSegunRol(int idRol)
        {
            if (ventanaAnterior != null)
                ventanaAnterior.Close();

            switch (idRol)
            {
                case 2: // Docente
                    Schedules schedules = new Schedules();
                    schedules.Show();
                    break;

                case 1: // Administrador
                    MainClassroom aulas = new MainClassroom();
                    aulas.Show();
                    break;

                default:
                    MainWindow signUp = new MainWindow();
                    signUp.Show();
                    break;
            }
            this.Close();
        }

        private void btnRetroceder_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Desea volver para modificar datos personales?",
                                       "Modificar Datos Personales",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (ventanaAnterior != null)
                    {
                        ventanaAnterior.ActualizarDatosPersona(persona);
                        ventanaAnterior.Show();
                    }
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al volver: " + ex.Message,
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Está seguro de cancelar la creación de cuenta?\n" +
                                       "Los datos personales serán eliminados.",
                                       "Confirmar Cancelación",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
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
                    MessageBox.Show("Error al eliminar datos: " + ex.Message,
                                  "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

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
    }
}