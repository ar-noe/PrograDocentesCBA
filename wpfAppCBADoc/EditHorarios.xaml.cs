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
using System.Configuration;

namespace wpfAppCBADoc
{
    /// <summary>
    /// Lógica de interacción para EditHorarios.xaml
    /// </summary>
    public partial class EditHorarios : Window
    {
        private DataClassesDocentesCBA2DataContext dcDB;
        private string connectionString;
        private int docenteSeleccionadoId = 0;
        private int moduloSeleccionadoId = 0;

        public EditHorarios()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            LoadDocentes();
            LoadHorariosComboBox();
            LoadModulosSinHorario();
        }

        private void InitializeDatabaseConnection()
        {
            try
            {
                connectionString = ConfigurationManager.ConnectionStrings["wpfAppCBADoc.Properties.Settings.PrograCBADocentesConnectionString"].ConnectionString;
                dcDB = new DataClassesDocentesCBA2DataContext(connectionString);
            }
            catch (Exception ex)
            {
                ShowMessage("Error conectando a la base de datos: " + ex.Message, true);
            }
        }

        private void LoadDocentes()
        {
            try
            {
                var docentes = (from p in dcDB.Persona
                                join d in dcDB.Docente on p.IdPersona equals d.IdPersona
                                where p.TipoPersona == "Docente"
                                select new
                                {
                                    ID = p.IdPersona,
                                    ApPat = p.ApPat,
                                    ApMat = p.ApMat,
                                    Nombres = p.Nombres,
                                    NombreCompleto = p.Nombres + " " + p.ApPat + " " + p.ApMat,
                                    IdDocente = d.IdDocente
                                }).ToList();

                dgDocentes.ItemsSource = docentes;
            }
            catch (Exception ex)
            {
                ShowMessage("Error cargando docentes: " + ex.Message, true);
            }
        }

        private void LoadHorariosDocente(int idDocente)
        {
            try
            {
                var horariosDocente = (from mi in dcDB.ModuloImpartido
                                       join m in dcDB.Modulo on mi.IdModulo equals m.IdModulo
                                       join c in dcDB.Curso on m.IdCurso equals c.IdCurso
                                       join a in dcDB.Aula on mi.IdAula equals a.IdAula
                                       join s in dcDB.Sucursal on a.IdSucursal equals s.IdSucursal
                                       join h in dcDB.Horario on mi.IdHorario equals h.IdHorario
                                       where mi.IdDocente == idDocente && mi.IdHorario != 0
                                       select new
                                       {
                                           Hora = h.HoraInicio + " - " + h.HoraFinal,
                                           NombreCurso = c.Nombre,
                                           Aula = a.NumeroAula,
                                           Sucursal = s.Alias,
                                           IdModuloImp = mi.IdModuloImp,
                                           IdHorario = mi.IdHorario
                                       }).ToList();

                dgHorariosDocente.ItemsSource = horariosDocente;
            }
            catch (Exception ex)
            {
                ShowMessage("Error cargando horarios del docente: " + ex.Message, true);
            }
        }

        private void LoadModulosSinHorario()
        {
            try
            {
                var modulosSinHorario = (from mi in dcDB.ModuloImpartido
                                         join m in dcDB.Modulo on mi.IdModulo equals m.IdModulo
                                         join a in dcDB.Aula on mi.IdAula equals a.IdAula
                                         join s in dcDB.Sucursal on a.IdSucursal equals s.IdSucursal
                                         where mi.IdHorario == 0
                                         select new
                                         {
                                             IdModuloImp = mi.IdModuloImp,
                                             Horario = "No schedule",
                                             Nombre = m.Nombre,
                                             Aula = a.NumeroAula,
                                             Duracion = "2 hours",
                                             IdDocente = mi.IdDocente
                                         }).ToList();

                dgModulosDisponibles.ItemsSource = modulosSinHorario;
            }
            catch (Exception ex)
            {
                ShowMessage("Error cargando módulos sin horario: " + ex.Message, true);
            }
        }

        private void LoadHorariosComboBox()
        {
            try
            {
                var horarios = (from h in dcDB.Horario
                                select new
                                {
                                    IdHorario = h.IdHorario,
                                    Descripcion = h.HoraInicio + " - " + h.HoraFinal
                                }).ToList();

                cmbHorarios.ItemsSource = horarios;
                cmbHorarios.DisplayMemberPath = "Descripcion";
                cmbHorarios.SelectedValuePath = "IdHorario";
            }
            catch (Exception ex)
            {
                ShowMessage("Error cargando horarios: " + ex.Message, true);
            }
        }

        // botones para la navegación entre pestañas 
        private void btnModulos_Click(object sender, RoutedEventArgs e)
        {
            MainModulosImp modulos = new MainModulosImp();
            modulos.Show();
            this.Close();
        }

        private void btnAulas_Click(object sender, RoutedEventArgs e)
        {
            MainClassroom aula = new MainClassroom();
            aula.Show();
            this.Close();
        }

        //mostrar mensajes de error u otros
        private void ShowMessage(string message, bool isError)
        {
            MessageBox.Show(message, isError ? "Error" : "Información",
                          MessageBoxButton.OK,
                          isError ? MessageBoxImage.Error : MessageBoxImage.Information);
        }

        private void Button_Click(object sender, RoutedEventArgs e)//salir
        {
            MainWindow signUp = new MainWindow();
            signUp.Show();
            this.Close();
        }

        private void FiltrarDocentes()
        {
            try
            {
                var textoBusqueda = txtDocentesSearch.Text.ToLower();

                var query = from p in dcDB.Persona
                            join d in dcDB.Docente on p.IdPersona equals d.IdPersona
                            where p.TipoPersona == "Docente"
                            where string.IsNullOrEmpty(textoBusqueda) ||
                                  p.ApMat.ToLower().Contains(textoBusqueda) ||
                                  p.ApPat.ToLower().Contains(textoBusqueda) ||
                                  p.Nombres.ToLower().Contains(textoBusqueda) ||
                                  (p.Nombres.ToLower() + " " + p.ApMat.ToLower() + " " + p.ApPat.ToLower()).Contains(textoBusqueda)
                            select new
                            {
                                ID = p.IdPersona,
                                ApPat = p.ApPat,
                                ApMat = p.ApMat,
                                Nombres = p.Nombres,
                                NombreCompleto = p.Nombres + " " + p.ApPat + " " + p.ApMat,
                                IdDocente = d.IdDocente
                            };

                var resultado = query.ToList();
                dgDocentes.ItemsSource = resultado;
            }
            catch (Exception ex)
            {
                ShowMessage("Error filtrando docentes: " + ex.Message, true);
            }
        }

        // Eventos para la búsqueda
        private void TxtBuscarDoc_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarDocentes();
        }

        private void DgDocentes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDocentes.SelectedItem != null)
            {
                dynamic docenteSeleccionado = dgDocentes.SelectedItem;
                docenteSeleccionadoId = docenteSeleccionado.IdDocente;

                // Actualizar título con nombre del docente
                txtTituloHorarioDocente.Text = $"Teacher's work schedule: {docenteSeleccionado.NombreCompleto}";

                // Cargar horarios del docente seleccionado
                LoadHorariosDocente(docenteSeleccionado.IdDocente);
            }
        }

        private void DgModulosDisponibles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgModulosDisponibles.SelectedItem != null)
            {
                dynamic moduloSeleccionado = dgModulosDisponibles.SelectedItem;
                moduloSeleccionadoId = moduloSeleccionado.IdModuloImp;
            }
        }

        private void BtnAsignarHorario_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (docenteSeleccionadoId == 0)
                {
                    ShowMessage("Please select a teacher first", true);
                    return;
                }

                var button = sender as Button;
                if (button != null)
                {
                    var modulo = button.DataContext as dynamic;
                    if (modulo != null)
                    {
                        int idModuloImp = modulo.IdModuloImp;

                        if (cmbHorarios.SelectedValue == null)
                        {
                            ShowMessage("Please select a schedule", true);
                            return;
                        }

                        int idHorario = (int)cmbHorarios.SelectedValue;

                        var moduloImpartido = dcDB.ModuloImpartido
                            .FirstOrDefault(mi => mi.IdModuloImp == idModuloImp);

                        if (moduloImpartido != null)
                        {
                            // Verificar que el módulo no tenga ya un docente asignado
                            if (moduloImpartido.IdDocente != 0 && moduloImpartido.IdDocente != docenteSeleccionadoId)
                            {
                                ShowMessage("This module already has another teacher assigned", true);
                                return;
                            }

                            // Asignar docente y horario
                            moduloImpartido.IdDocente = docenteSeleccionadoId;
                            moduloImpartido.IdHorario = idHorario;

                            dcDB.SubmitChanges();

                            ShowMessage("Schedule assigned successfully!", false);

                            // Recargar datos
                            LoadModulosSinHorario();
                            LoadHorariosDocente(docenteSeleccionadoId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error assigning schedule: " + ex.Message, true);
            }
        }

        private void BtnEliminarHorario_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    var horario = button.DataContext as dynamic;
                    if (horario != null)
                    {
                        int idModuloImp = horario.IdModuloImp;

                        var result = MessageBox.Show(
                            "Are you sure you want to remove this schedule?",
                            "Confirm Deletion",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            var moduloImpartido = dcDB.ModuloImpartido
                                .FirstOrDefault(mi => mi.IdModuloImp == idModuloImp);

                            if (moduloImpartido != null)
                            {
                                // Verificar si hay estudiantes inscritos
                                var estudiantesInscritos = dcDB.EstudianteInscrito
                                    .Any(ei => ei.IdModuloImp == idModuloImp);

                                if (estudiantesInscritos)
                                {
                                    ShowMessage("Cannot remove schedule because there are enrolled students", true);
                                    return;
                                }

                                // Restablecer horario a 0 (sin horario)
                                moduloImpartido.IdHorario = 0;

                                dcDB.SubmitChanges();

                                ShowMessage("Schedule removed successfully!", false);

                                // Recargar datos
                                LoadModulosSinHorario();
                                LoadHorariosDocente(docenteSeleccionadoId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error removing schedule: " + ex.Message, true);
            }
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            // Validar que se haya seleccionado un docente
            if (dgDocentes.SelectedItem == null)
            {
                ShowMessage("Please select a teacher", true);
                return;
            }

            // Validar que se haya seleccionado un horario en el ComboBox
            if (cmbHorarios.SelectedValue == null)
            {
                ShowMessage("Please select a schedule", true);
                return;
            }

            // Validar que se haya seleccionado un módulo a asignar
            if (dgModulosDisponibles.SelectedItem == null)
            {
                ShowMessage("Please select a module to assign", true);
                return;
            }

            // Si todas las validaciones pasan, mostrar mensaje de éxito
            ShowMessage("Operation completed successfully!", false);

            // Opcional: Limpiar selecciones
            // dgModulosDisponibles.SelectedItem = null;
            // cmbHorarios.SelectedIndex = -1;
        }
    }
}