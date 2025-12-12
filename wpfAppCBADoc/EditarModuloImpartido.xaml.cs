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
                                      from d in docenteJoin.DefaultIfEmpty() // LEFT JOIN
                                      join p in dcBd.Persona on (d != null ? d.IdPersona : 0) equals p.IdPersona into personaJoin
                                      from p in personaJoin.DefaultIfEmpty() // LEFT JOIN
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

                        txtInfoActual.Text = $"Docente: {infoActual.Docente}\n" +
                                           $"Módulo: {infoActual.Modulo} ({infoActual.Curso})\n" +
                                           $"Aula: {infoActual.Aula} - {infoActual.Sucursal}\n" +
                                           $"Bimestre: {bimestreFormateado}";

                        // Establecer valores actuales en los comboboxes
                        cmbModuloEdit.SelectedValue = infoActual.IdModulo;
                        cmbBimestreEdit.SelectedValue = infoActual.IdBimestre;

                        // Cargar sucursal y aula actual
                        cmbSucursalEdit.SelectedValue = infoActual.IdSucursal;

                        // Forzar carga inicial de aulas
                        CmbSucursalEdit_SelectionChanged(null, null);

                        // Esperar un momento para que se carguen las aulas
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() =>
                            {
                                cmbAulaEdit.SelectedValue = infoActual.IdAula;
                            }));
                    }

                    // Verificar si hay estudiantes inscritos
                    VerificarEstudiantesInscritos();
                }
                else
                {
                    MessageBox.Show("No se encontró el módulo impartido", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando datos del módulo: " + ex.Message, "Error",
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
                                Text = "• Módulo y bimestre bloqueados por estudiantes inscritos",
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
                System.Diagnostics.Debug.WriteLine("Error verificando estudiantes: " + ex.Message);
            }
        }

        private void CargarComboboxes()
        {
            try
            {
                // Cargar módulos
                var modulos = (from m in dcBd.Modulo
                               join c in dcBd.Curso on m.IdCurso equals c.IdCurso
                               select new
                               {
                                   IdModulo = m.IdModulo,
                                   NombreModulo = m.Nombre,
                                   Curso = c.Nombre
                               }).ToList();
                cmbModuloEdit.ItemsSource = modulos;
                cmbModuloEdit.DisplayMemberPath = "NombreModulo";
                cmbModuloEdit.SelectedValuePath = "IdModulo";

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

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando datos: " + ex.Message, "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
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

                    // Si solo hay un aula disponible, seleccionarla automáticamente
                    if (aulas.Count == 1 && cmbAulaEdit.SelectedIndex == -1)
                    {
                        cmbAulaEdit.SelectedIndex = 0;
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

                // Si hay estudiantes, no permitir cambios en módulo o bimestre
                if (tieneEstudiantes)
                {
                    if (moduloActual.IdModulo != (int)cmbModuloEdit.SelectedValue)
                    {
                        MessageBox.Show("No se puede cambiar el módulo porque hay estudiantes inscritos",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (moduloActual.IdBimestre != (int)cmbBimestreEdit.SelectedValue)
                    {
                        MessageBox.Show("No se puede cambiar el bimestre porque hay estudiantes inscritos",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Verificar que no haya conflictos de aula/bimestre
                bool existeConflicto = dcBd.ModuloImpartido
                    .Any(mi => mi.IdAula == (int)cmbAulaEdit.SelectedValue &&
                               mi.IdBimestre == (int)cmbBimestreEdit.SelectedValue &&
                               mi.IdModuloImp != idModuloImp);

                if (existeConflicto)
                {
                    MessageBox.Show("Ya existe un módulo impartido en esta aula para el bimestre seleccionado",
                                  "Conflicto", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Actualizar datos (NOTA: no actualizamos IdDocente porque no tenemos el combo)
                moduloActual.IdModulo = (int)cmbModuloEdit.SelectedValue;
                moduloActual.IdAula = (int)cmbAulaEdit.SelectedValue;
                moduloActual.IdBimestre = (int)cmbBimestreEdit.SelectedValue;
                // IdHorario se mantiene como 0 (placeholder)
                // IdDocente se mantiene como 0 (placeholder)

                // Guardar cambios
                dcBd.SubmitChanges();

                MessageBox.Show("✅ Cambios guardados exitosamente", "Éxito",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error guardando cambios: " + ex.Message, "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidarFormulario()
        {
            if (cmbModuloEdit.SelectedValue == null)
            {
                MessageBox.Show("Seleccione un módulo", "Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbModuloEdit.Focus();
                return false;
            }

            if (cmbSucursalEdit.SelectedValue == null)
            {
                MessageBox.Show("Seleccione una sucursal", "Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSucursalEdit.Focus();
                return false;
            }

            if (cmbAulaEdit.SelectedValue == null)
            {
                MessageBox.Show("Seleccione un aula", "Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbAulaEdit.Focus();
                return false;
            }

            if (cmbBimestreEdit.SelectedValue == null)
            {
                MessageBox.Show("Seleccione un bimestre", "Validación",
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