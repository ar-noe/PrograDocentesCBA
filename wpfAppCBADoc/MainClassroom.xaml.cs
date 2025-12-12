using System;
using System.Collections.Generic;
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
        public MainClassroom()
        {
            InitializeComponent();
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
    }
}
