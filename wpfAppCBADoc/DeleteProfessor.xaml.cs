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
    /// Lógica de interacción para DeleteProfessor.xaml
    /// </summary>
    /// 
    public partial class DeleteProfessor : Window
    {
        DataClassesDocentesCBA2DataContext dcDB;
        public DeleteProfessor()
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
                var searchQuery = (from personId in dcDB.Persona
                                   where personId.TipoPersona == "Docente"
                                   select personId).ToList();



                //var allClients = dcDB.Persona.ToList();
                dgProfessors.ItemsSource = searchQuery;
            }
            catch (Exception e)
            {
                {
                    MessageBox.Show("Error: " + e.Message);
                }
            }
        }

        private void btnDeleteSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string profToSearch = txtSearchProfessor.Text;
                if (profToSearch == "")
                {
                    MessageBox.Show("Enter a valid name");
                    return;
                }
                var searchQuery = from p in dcDB.Persona
                                  where p.Nombres.Contains(profToSearch)
                                  select p;

                dgProfessors.ItemsSource = searchQuery.ToList();
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void BtnDeleteProf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(dgProfessors.SelectedItem is Persona selProf))
                {
                    MessageBox.Show("Please Select a Professor before Deleting");
                    return;
                }
                var confirmElm = MessageBox.Show("Would you like to eliminate: \"" + selProf.Nombres + "\" ?", "Delete Professor?", MessageBoxButton.YesNo);
                if (confirmElm == MessageBoxResult.Yes)
                {
                    var docente = dcDB.Docente.FirstOrDefault(d => d.IdPersona == selProf.IdPersona);

                    if (docente != null)
                    {
                        var modulos = dcDB.ModuloImpartido.Where(m => m.IdDocente == docente.IdDocente).ToList();
                        foreach (var modulo in modulos)
                        {
                            modulo.IdDocente = 0;
                        }
                        var horarios = dcDB.ModuloImpartido.Where(m => m.IdDocente == docente.IdDocente).ToList();
                        foreach (var horario in horarios)
                        {
                            horario.IdHorario = 0;
                        }

                        var delDoc = dcDB.Docente.FirstOrDefault(d => d.IdPersona == selProf.IdPersona);
                        if (delDoc != null)
                            dcDB.Docente.DeleteOnSubmit(delDoc);
                    }

                    var delUsr = dcDB.Usuario.FirstOrDefault(u => u.IdPersona == selProf.IdPersona);
                    if (delUsr != null)
                        dcDB.Usuario.DeleteOnSubmit(delUsr);

                    dcDB.Persona.DeleteOnSubmit(selProf);

                    dcDB.SubmitChanges();
                    loadDataDB();
                    MessageBox.Show("User and related accounts have been deleted");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        //navegación

        private void BtnHorarios_Click(object sender, RoutedEventArgs e)
        {
            EditHorarios horarios = new EditHorarios();
            horarios.Show();
            this.Close();
        }

        private void btnModulos_Click(object sender, RoutedEventArgs e)
        {
            MainModulosImp module = new MainModulosImp();
            module.Show();
            this.Close();
        }

        private void BtnAulas_Click(object sender, RoutedEventArgs e)
        {
            MainClassroom aula = new MainClassroom();
            aula.Show();
            this.Close();
        }
    }
}
