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
    /// Interaction logic for EditarModuloImpartido.xaml
    /// </summary>
    public partial class EditarModuloImpartido : Window
    {
        private DataClassesDocentesBDDataContext dcBd;
        private int idModuloImp;
        private ModuloImpartido moduloActual;

        public EditarModuloImpartido(int idModuloImp, DataClassesDocentesBDDataContext dcBd)
        {
            InitializeComponent();
            this.idModuloImp = idModuloImp;
            this.dcBd = dcBd;
            CargarDatosModulo();
            CargarComboboxes();
        }

        private void CargarDatosModulo()
        {
            try
            {
                moduloActual = dcBd.ModuloImpartido.FirstOrDefault(mi => mi.IdModuloImp == idModuloImp);

                if (moduloActual != null)
                {
                    // Cargar información actual usando LINQ to SQL
                    var infoActual = (from mi in dcBd.ModuloImpartido
                                      join p in dcBd.Persona on mi.IdPersona equals p.IdPersona
                                      join m in dcBd.Modulo on mi.IdModulo equals m.IdModulo
                                      join c in dcBd.Curso on m.IdCurso equals c.IdCurso
                                      join a in dcBd.Aula on mi.IdAula equals a.IdAula
                                      join s in dcBd.Sucursal on a.IdSucursal equals s.IdSucursal
                                      join b in dcBd.Bimestre on mi.IdBimestre equals b.IdBimestre
                                      where mi.IdModuloImp == idModuloImp
                                      select new
                                      {
                                          Docente = p.Nombres + " " + p.ApPat + " " + p.ApMat,
                                          Modulo = m.Nombre,
                                          Curso = c.Nombre,
                                          Aula = a.NumeroAula,
                                          Sucursal = s.Alias,
                                          Bimestre = b.Gestion,
                                          FechaInicio = b.FechaInicio
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
                        cmbDocenteEdit.SelectedValue = moduloActual.IdPersona;
                        cmbModuloEdit.SelectedValue = moduloActual.IdModulo;
                        cmbBimestreEdit.SelectedValue = moduloActual.IdBimestre;

                        // Cargar sucursal y aula actual
                        var aulaActual = dcBd.Aula.FirstOrDefault(a => a.IdAula == moduloActual.IdAula);
                        if (aulaActual != null)
                        {
                            cmbSucursalEdit.SelectedValue = aulaActual.IdSucursal;
                            // El combobox de aulas se cargará automáticamente por el SelectionChanged
                            CmbSucursalEdit_SelectionChanged(null, null); // Forzar carga inicial
                            cmbAulaEdit.SelectedValue = moduloActual.IdAula;
                        }
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

                    // Mostrar advertencia
                    var borderInfo = ((Grid)((GroupBox)Content).Content).Children
                        .OfType<Border>()
                        .FirstOrDefault(b => b.Background.ToString().Contains("LightPinkBrush"));

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
                // Si hay error en la verificación, continuar de todos modos
                System.Diagnostics.Debug.WriteLine("Error verificando estudiantes: " + ex.Message);
            }
        }

        private void CargarComboboxes()
        {
            try
            {
                // Cargar docentes usando LINQ to SQL
                var docentes = (from p in dcBd.Persona
                                join u in dcBd.Usuario on p.IdPersona equals u.IdPersona
                                join r in dcBd.Rol on u.IdRol equals r.IdRol
                                where r.Nombre == "Docente"
                                select new
                                {
                                    IdPersona = p.IdPersona,
                                    NombreCompleto = p.Nombres + " " + p.ApPat + " " + p.ApMat
                                }).ToList();
                cmbDocenteEdit.ItemsSource = docentes;
                cmbDocenteEdit.DisplayMemberPath = "NombreCompleto";
                cmbDocenteEdit.SelectedValuePath = "IdPersona";

                // Cargar módulos usando LINQ to SQL
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

                // Cargar sucursales usando LINQ to SQL
                var sucursales = (from s in dcBd.Sucursal
                                  select new
                                  {
                                      IdSucursal = s.IdSucursal,
                                      Alias = s.Alias
                                  }).ToList();
                cmbSucursalEdit.ItemsSource = sucursales;
                cmbSucursalEdit.DisplayMemberPath = "Alias";
                cmbSucursalEdit.SelectedValuePath = "IdSucursal";

                // Cargar bimestres usando LINQ to SQL - SIN ToString en la consulta
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

                    // Cargar aulas usando LINQ to SQL con join
                    var aulas = (from a in dcBd.Aula
                                 join ea in dcBd.EstadoAula on a.IdEstadoA equals ea.IdEstadoA
                                 where a.IdSucursal == idSucursal && a.IdEstadoA == 1 // 1 = Activo
                                 select new
                                 {
                                     IdAula = a.IdAula,
                                     NumeroAula = a.NumeroAula,
                                     Capacidad = a.Capacidad,
                                     Estado = ea.Estado
                                 }).ToList();

                    cmbAulaEdit.ItemsSource = aulas;
                    cmbAulaEdit.DisplayMemberPath = "NumeroAula";
                    cmbAulaEdit.SelectedValuePath = "IdAula";

                    // Si solo hay un aula disponible, seleccionarla automáticamente
                    if (aulas.Count == 1)
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

                // Verificar si el módulo impartido aún existe usando LINQ to SQL
                moduloActual = (from mi in dcBd.ModuloImpartido
                                where mi.IdModuloImp == idModuloImp
                                select mi).FirstOrDefault();

                if (moduloActual == null)
                {
                    MessageBox.Show("El módulo impartido ya no existe", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                // Verificar que no haya conflictos de horario/aula usando LINQ to SQL
                bool existeConflicto = (from mi in dcBd.ModuloImpartido
                                        where mi.IdAula == (int)cmbAulaEdit.SelectedValue &&
                                              mi.IdBimestre == (int)cmbBimestreEdit.SelectedValue &&
                                              mi.IdModuloImp != idModuloImp // Excluir el actual
                                        select mi).Any();

                if (existeConflicto)
                {
                    MessageBox.Show("Ya existe un módulo impartido en esta aula para el bimestre seleccionado",
                                  "Conflicto", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Actualizar datos
                moduloActual.IdPersona = (int)cmbDocenteEdit.SelectedValue;
                moduloActual.IdModulo = (int)cmbModuloEdit.SelectedValue;
                moduloActual.IdAula = (int)cmbAulaEdit.SelectedValue;
                moduloActual.IdBimestre = (int)cmbBimestreEdit.SelectedValue;
                // IdHorario se mantiene como null

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
            if (cmbDocenteEdit.SelectedValue == null)
            {
                MessageBox.Show("Seleccione un docente", "Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbDocenteEdit.Focus();
                return false;
            }

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
            // Asegurarse de que DialogResult tenga un valor si se cierra la ventana
            if (this.DialogResult == null)
            {
                this.DialogResult = false;
            }
            base.OnClosed(e);
        }
    }
}
