using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace wpfAppCBADoc
{
    public partial class EditarModuloImpartido : Window
    {
        private DataClassesDocentesCBA2DataContext dcBd;
        private int idModuloImp;
        private ModuloImpartido moduloActual;

        public EditarModuloImpartido(int idModuloImp, DataClassesDocentesCBA2DataContext dcBd)
        {
            InitializeComponent();
            this.idModuloImp = idModuloImp;
            this.dcBd = dcBd;
            CargarComboboxes();
            CargarDatosModulo();
        }

        private void CargarDatosModulo()
        {
            try
            {
                moduloActual = dcBd.ModuloImpartido.FirstOrDefault(mi => mi.IdModuloImp == idModuloImp);

                if (moduloActual != null)
                {
                    // Obtener información actual usando LINQ con relaciones correctas
                    var infoActual = (from mi in dcBd.ModuloImpartido
                                      join d in dcBd.Docente on mi.IdDocente equals d.IdDocente into docenteJoin
                                      from d in docenteJoin.DefaultIfEmpty()
                                      join p in dcBd.Persona on (d != null ? d.IdPersona : 0) equals p.IdPersona into personaJoin
                                      from p in personaJoin.DefaultIfEmpty()
                                      join m in dcBd.Modulo on mi.IdModulo equals m.IdModulo
                                      join c in dcBd.Curso on m.IdCurso equals c.IdCurso
                                      join a in dcBd.Aula on mi.IdAula equals a.IdAula
                                      join s in dcBd.Sucursal on a.IdSucursal equals s.IdSucursal
                                      join b in dcBd.Bimestre on mi.IdBimestre equals b.IdBimestre
                                      where mi.IdModuloImp == idModuloImp
                                      select new
                                      {
                                          Docente = p != null ? p.Nombres + " " + p.ApPat + " " + p.ApMat : "Por asignar",
                                          Modulo = m.Nombre,
                                          Curso = c.Nombre,
                                          CursoId = c.IdCurso, // Nuevo: ID del curso
                                          Aula = a.NumeroAula,
                                          Sucursal = s.Alias,
                                          Bimestre = b.Gestion,
                                          FechaInicio = b.FechaInicio,
                                          IdDocente = mi.IdDocente,
                                          IdModulo = mi.IdModulo,
                                          IdAula = mi.IdAula,
                                          IdBimestre = mi.IdBimestre,
                                          IdSucursal = a.IdSucursal
                                      }).FirstOrDefault();

                    if (infoActual != null)
                    {
                        // Formatear fecha después de traer los datos
                        string bimestreFormateado = $"{infoActual.Bimestre} - {infoActual.FechaInicio:MMM/yyyy}";

                        txtInfoActual.Text = $"Teacher: {infoActual.Docente}\n" +
                                           $"Module: {infoActual.Modulo} ({infoActual.Curso})\n" +
                                           $"Classroom: {infoActual.Aula} - {infoActual.Sucursal}\n" +
                                           $"Bimester: {bimestreFormateado}";

                        // Establecer valores actuales en los comboboxes
                        cmbCursoEdit.SelectedValue = infoActual.CursoId;
                        // Esto activará automáticamente la carga de módulos

                        // Cargar sucursal y aula actual
                        cmbSucursalEdit.SelectedValue = infoActual.IdSucursal;
                        cmbBimestreEdit.SelectedValue = infoActual.IdBimestre;

                        // Esperar un momento para que se carguen los datos dependientes
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() =>
                            {
                                if (cmbModuloEdit.IsEnabled)
                                {
                                    cmbModuloEdit.SelectedValue = infoActual.IdModulo;
                                }
                                if (cmbAulaEdit.IsEnabled)
                                {
                                    cmbAulaEdit.SelectedValue = infoActual.IdAula;
                                }
                            }));
                    }

                    // Verificar si hay estudiantes inscritos
                    VerificarEstudiantesInscritos();
                }
                else
                {
                    MessageBox.Show("Module not found", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error obtaining the module: " + ex.Message, "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void VerificarEstudiantesInscritos()
        {
            try
            {
                bool tieneEstudiantes = dcBd.EstudianteInscrito
                    .Any(ei => ei.IdModuloImp == idModuloImp);

                if (tieneEstudiantes)
                {
                    // Deshabilitar algunos campos si hay estudiantes inscritos
                    cmbCursoEdit.IsEnabled = false;
                    cmbModuloEdit.IsEnabled = false;
                    cmbBimestreEdit.IsEnabled = false;

                    // Mostrar advertencia en el border de información
                    var borderInfo = ((Grid)Content).Children
                        .OfType<Border>()
                        .FirstOrDefault(b => b.Background?.ToString()?.Contains("LightPinkBrush") == true);

                    if (borderInfo != null)
                    {
                        var stackPanel = borderInfo.Child as StackPanel;
                        if (stackPanel != null)
                        {
                            stackPanel.Children.Add(new TextBlock
                            {
                                Text = "• Course, module and bimester are locked if they have students enrolled",
                                Foreground = System.Windows.Media.Brushes.DarkRed,
                                FontSize = 12,
                                FontWeight = FontWeights.Bold,
                                Margin = new Thickness(0, 5, 0, 0)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error verifing students: " + ex.Message);
            }
        }

        private void CargarComboboxes()
        {
            try
            {
                // Cargar cursos
                var cursos = (from c in dcBd.Curso
                              select new
                              {
                                  IdCurso = c.IdCurso,
                                  Nombre = c.Nombre
                              }).ToList();
                cmbCursoEdit.ItemsSource = cursos;
                cmbCursoEdit.DisplayMemberPath = "Nombre";
                cmbCursoEdit.SelectedValuePath = "IdCurso";

                // Cargar sucursales
                var sucursales = (from s in dcBd.Sucursal
                                  select new
                                  {
                                      IdSucursal = s.IdSucursal,
                                      Alias = s.Alias
                                  }).ToList();
                cmbSucursalEdit.ItemsSource = sucursales;
                cmbSucursalEdit.DisplayMemberPath = "Alias";
                cmbSucursalEdit.SelectedValuePath = "IdSucursal";

                // Cargar bimestres
                var bimestres = (from b in dcBd.Bimestre
                                 select new
                                 {
                                     IdBimestre = b.IdBimestre,
                                     Gestion = b.Gestion,
                                     FechaInicio = b.FechaInicio,
                                     FechaFin = b.FechaFin
                                 }).ToList();

                // Formatear después de traer los datos
                var bimestresFormateados = bimestres.Select(b => new
                {
                    IdBimestre = b.IdBimestre,
                    Descripcion = $"{b.Gestion} - {b.FechaInicio:MMM} a {b.FechaFin:MMM}"
                }).ToList();

                cmbBimestreEdit.ItemsSource = bimestresFormateados;
                cmbBimestreEdit.DisplayMemberPath = "Descripcion";
                cmbBimestreEdit.SelectedValuePath = "IdBimestre";

                // Inicialmente deshabilitar combobox dependientes
                cmbModuloEdit.IsEnabled = false;
                cmbAulaEdit.IsEnabled = false;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando datos: " + ex.Message, "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadModulosByCursoEdit(int idCurso)
        {
            try
            {
                var modulos = (from m in dcBd.Modulo
                               where m.IdCurso == idCurso
                               select new
                               {
                                   IdModulo = m.IdModulo,
                                   Nombre = m.Nombre
                               }).ToList();

                cmbModuloEdit.ItemsSource = modulos;
                cmbModuloEdit.DisplayMemberPath = "Nombre";
                cmbModuloEdit.SelectedValuePath = "IdModulo";

                // Solo habilitar si no hay estudiantes inscritos
                bool tieneEstudiantes = dcBd.EstudianteInscrito
                    .Any(ei => ei.IdModuloImp == idModuloImp);

                cmbModuloEdit.IsEnabled = !tieneEstudiantes;

                // Limpiar selección si no hay módulos
                if (modulos.Count == 0)
                {
                    cmbModuloEdit.SelectedIndex = -1;
                    cmbModuloEdit.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando módulos: " + ex.Message, "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbCursoEdit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCursoEdit.SelectedValue != null)
            {
                int idCurso = (int)cmbCursoEdit.SelectedValue;
                LoadModulosByCursoEdit(idCurso);
            }
            else
            {
                cmbModuloEdit.ItemsSource = null;
                cmbModuloEdit.IsEnabled = false;
            }
        }

        private void CmbSucursalEdit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSucursalEdit.SelectedValue != null)
            {
                try
                {
                    int idSucursal = (int)cmbSucursalEdit.SelectedValue;

                    // Cargar aulas de la sucursal seleccionada
                    var aulas = (from a in dcBd.Aula
                                 where a.IdSucursal == idSucursal && a.IdEstadoA == 1 // 1 = Activo
                                 select new
                                 {
                                     IdAula = a.IdAula,
                                     NumeroAula = a.NumeroAula,
                                     Capacidad = a.Capacidad
                                 }).ToList();

                    cmbAulaEdit.ItemsSource = aulas;
                    cmbAulaEdit.DisplayMemberPath = "NumeroAula";
                    cmbAulaEdit.SelectedValuePath = "IdAula";
                    cmbAulaEdit.IsEnabled = true;

                    // Limpiar selección si no hay aulas
                    if (aulas.Count == 0)
                    {
                        cmbAulaEdit.SelectedIndex = -1;
                        cmbAulaEdit.IsEnabled = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error cargando aulas: " + ex.Message, "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                cmbAulaEdit.ItemsSource = null;
                cmbAulaEdit.SelectedIndex = -1;
                cmbAulaEdit.IsEnabled = false;
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarFormulario())
                    return;

                // Verificar si el módulo impartido aún existe
                moduloActual = dcBd.ModuloImpartido
                    .FirstOrDefault(mi => mi.IdModuloImp == idModuloImp);

                if (moduloActual == null)
                {
                    MessageBox.Show("El módulo impartido ya no existe", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                // Verificar si hay estudiantes inscritos (para validar cambios)
                bool tieneEstudiantes = dcBd.EstudianteInscrito
                    .Any(ei => ei.IdModuloImp == idModuloImp);

                // Si hay estudiantes, no permitir cambios en curso, módulo o bimestre
                if (tieneEstudiantes)
                {
                    // Verificar si el curso cambió (aunque el combo esté deshabilitado)
                    var cursoActual = (from mi in dcBd.ModuloImpartido
                                       join m in dcBd.Modulo on mi.IdModulo equals m.IdModulo
                                       where mi.IdModuloImp == idModuloImp
                                       select m.IdCurso).FirstOrDefault();

                    if (cursoActual != (int)cmbCursoEdit.SelectedValue)
                    {
                        MessageBox.Show("Can´t change the course, there is/are student/s enrolled",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (moduloActual.IdModulo != (int)cmbModuloEdit.SelectedValue)
                    {
                        MessageBox.Show("Can´t change the module, there is/are student/s enrolled",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (moduloActual.IdBimestre != (int)cmbBimestreEdit.SelectedValue)
                    {
                        MessageBox.Show("Can´t change the bimester, there is/are student/s enrolled",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                int idBimestreSeleccionado = (int)cmbBimestreEdit.SelectedValue;
                int idAulaSeleccionada = (int)cmbAulaEdit.SelectedValue;

                // bbtener todos los horarios disponibles en el sistema (excluyendo placeholder)
                int totalHorariosDisponibles = dcBd.Horario
                    .Where(h => h.IdHorario != 0)
                    .Count();

                // contar cuántos módulos ya están asignados a esta aula en este bimestre
                int modulosAsignadosEnAulaBimestre = dcBd.ModuloImpartido
                    .Count(mi => mi.IdAula == idAulaSeleccionada &&
                                 mi.IdBimestre == idBimestreSeleccionado &&
                                 mi.IdModuloImp != idModuloImp); // Excluir el actual

                // verificar si se ha alcanzado el límite de horarios disponibles
                if (modulosAsignadosEnAulaBimestre >= totalHorariosDisponibles)
                {
                    MessageBox.Show($"This classroom already has {modulosAsignadosEnAulaBimestre} module(s) assigned" +
                                   $"this bimester. Limit: {totalHorariosDisponibles} module(s) per classroom this bimester.\n\n" +
                                   $"Each classroom only has to have as much modules asigned as schedules available each bimester.",
                                   "Limit reached", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int espaciosDisponibles = totalHorariosDisponibles - modulosAsignadosEnAulaBimestre;
                if (espaciosDisponibles <= 2) // Solo mostrar advertencia si quedan pocos espacios
                {
                    MessageBox.Show($"Warning: There is/are {espaciosDisponibles} spots left for this classroom this bimester.",
                                   "Limited spots", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Actualizar datos
                moduloActual.IdModulo = (int)cmbModuloEdit.SelectedValue;
                moduloActual.IdAula = idAulaSeleccionada;
                moduloActual.IdBimestre = idBimestreSeleccionado;
                // IdHorario se mantiene como 0 (placeholder)
                // IdDocente se mantiene como 0 (placeholder)

                // Guardar cambios
                dcBd.SubmitChanges();

                MessageBox.Show("Changes saved correctly", ":)",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving changes: " + ex.Message, "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidarFormulario()
        {
            if (cmbCursoEdit.SelectedValue == null)
            {
                MessageBox.Show("Select a course", "Validate",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCursoEdit.Focus();
                return false;
            }

            if (cmbModuloEdit.SelectedValue == null)
            {
                MessageBox.Show("Select a module", "Validate",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbModuloEdit.Focus();
                return false;
            }

            if (cmbSucursalEdit.SelectedValue == null)
            {
                MessageBox.Show("Select a Branch", "Validate",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSucursalEdit.Focus();
                return false;
            }

            if (cmbAulaEdit.SelectedValue == null)
            {
                MessageBox.Show("Select a classroom", "Validate",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbAulaEdit.Focus();
                return false;
            }

            if (cmbBimestreEdit.SelectedValue == null)
            {
                MessageBox.Show("Select a bimester", "Validate",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbBimestreEdit.Focus();
                return false;
            }

            return true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (this.DialogResult == null)
            {
                this.DialogResult = false;
            }
            base.OnClosed(e);
        }
    }
}