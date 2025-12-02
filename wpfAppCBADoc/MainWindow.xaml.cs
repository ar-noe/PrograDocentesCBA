using System;
using System.Linq;
using System.Windows;

namespace wpfAppCBADoc
{
    public partial class MainWindow : Window
    {
        private DataClassesDocentesBDDataContext dcBd;

        public MainWindow()
        {
            InitializeComponent();
            dcBd = new DataClassesDocentesBDDataContext();
        }

        private void btnGoToLogin_Click(object sender, RoutedEventArgs e)
        {
            // Abrir ventana de Login
            ManagerMainWindow loginWindow = new ManagerMainWindow();
            loginWindow.Show();

            // Cerrar ventana actual
            this.Close();
        }

        private void btnGoToSignUp_Click(object sender, RoutedEventArgs e)
        {
            string correoUsr = txtEmail.Text.Trim();
            string passwrdUsr = txtPassword.Password;

            // Validar campos vacíos
            if (string.IsNullOrEmpty(correoUsr) || string.IsNullOrEmpty(passwrdUsr))
            {
                MessageBox.Show("Please enter both email and password", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Buscar usuario en la base de datos usando sintaxis de consulta LINQ
                var queryUsuario = from usr in dcBd.Usuario
                                   where usr.Correo == correoUsr && usr.Contrasenia == passwrdUsr
                                   select usr;

                var usuario = queryUsuario.FirstOrDefault();

                if (usuario == null)
                {
                    MessageBox.Show("Invalid email or password", "Login Failed",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Redirigir según el rol
                switch (usuario.IdRol)
                {
                    case 2: // Docente
                        MainDoc mainDoc = new MainDoc();
                        mainDoc.Show();
                        this.Close();
                        break;
                    case 3: // Administrador
                        MainAdmin mainAdmin = new MainAdmin();
                        mainAdmin.Show();
                        this.Close();
                        break;
                    default:
                        MessageBox.Show("Unknown user role", "Error",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during login: " + ex.Message, "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para el botón Exit si es necesario
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to exit?", "Confirm Exit",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}