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
    /// Interaction logic for MainModulosImp.xaml
    /// </summary>
    public partial class MainModulosImp : Window
    {
        private DataClassesDocentesBDDataContext dcBd;

        public MainModulosImp()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            LoadComboBoxData();
            LoadModulosImpartidos();
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
                ShowMessage("Error conectando a la base de datos: " + ex.Message, true);
            }
        }

        private void LoadComboBoxData()
        {
            try
            {
                // Cargar docentes (solo aquellos con rol de Docente)
                var docentes = (from p in dcBd.Persona
                                join u in dcBd.Usuario on p.IdPersona equals u.IdPersona
                                join r in dcBd.Rol on u.IdRol equals r.IdRol
                                where r.Nombre == "Docente"
                                select new
                                {
                                    IdPersona = p.IdPersona,
                                    NombreCompleto = p.Nombres + " " + p.ApPat + " " + p.ApMat
                                }).ToList();
                cmbDocente.ItemsSource = docentes;
                cmbDocente.DisplayMemberPath = "NombreCompleto";
                cmbDocente.SelectedValuePath = "IdPersona";

                // Cargar módulos
                var modulos = (from m in dcBd.Modulo
                               join c in dcBd.Curso on m.IdCurso equals c.IdCurso
                               select new
                               {
                                   IdModulo = m.IdModulo,
                                   NombreModulo = m.Nombre,
                                   Curso = c.Nombre
                               }).ToList();
                cmbModulo.ItemsSource = modulos;
                cmbModulo.DisplayMemberPath = "NombreModulo";
                cmbModulo.SelectedValuePath = "IdModulo";

                // Cargar sucursales
                var sucursales = (from s in dcBd.Sucursal
                                  select new
                                  {
                                      IdSucursal = s.IdSucursal,
                                      Alias = s.Alias
                                  }).ToList();
                cmbSucursal.ItemsSource = sucursales;
                cmbSucursal.DisplayMemberPath = "Alias";
                cmbSucursal.SelectedValuePath = "IdSucursal";

                // Cargar bimestres - SIN ToString en la consulta
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

                cmbBimestre.ItemsSource = bimestresFormateados;
                cmbBimestre.DisplayMemberPath = "Descripcion";
                cmbBimestre.SelectedValuePath = "IdBimestre";

            }
            catch (Exception ex)
            {
                ShowMessage("Error cargando datos: " + ex.Message, true);
            }
        }

        private void LoadAulasBySucursal(int idSucursal)
        {
            try
            {
                var aulas = (from a in dcBd.Aula
                             where a.IdSucursal == idSucursal && a.IdEstadoA == 1 // Asumiendo 1 = Activo
                             select new
                             {
                                 IdAula = a.IdAula,
                                 NumeroAula = a.NumeroAula,
                                 Capacidad = a.Capacidad
                             }).ToList();

                cmbAula.ItemsSource = aulas;
                cmbAula.DisplayMemberPath = "NumeroAula";
                cmbAula.SelectedValuePath = "IdAula";
            }
            catch (Exception ex)
            {
                ShowMessage("Error cargando aulas: " + ex.Message, true);
            }
        }

        private void LoadModulosImpartidos()
        {
            try
            {
                var modulosImpartidos = (from mi in dcBd.ModuloImpartido
                                         join p in dcBd.Persona on mi.IdPersona equals p.IdPersona
                                         join m in dcBd.Modulo on mi.IdModulo equals m.IdModulo
                                         join c in dcBd.Curso on m.IdCurso equals c.IdCurso
                                         join a in dcBd.Aula on mi.IdAula equals a.IdAula
                                         join s in dcBd.Sucursal on a.IdSucursal equals s.IdSucursal
                                         join b in dcBd.Bimestre on mi.IdBimestre equals b.IdBimestre
                                         select new
                                         {
                                             IdModuloImp = mi.IdModuloImp,
                                             Docente = p.Nombres + " " + p.ApPat + " " + p.ApMat,
                                             Modulo = m.Nombre,
                                             Curso = c.Nombre,
                                             Aula = a.NumeroAula,
                                             Sucursal = s.Alias,
                                             Bimestre = b.Gestion,
                                             FechaInicio = b.FechaInicio,
                                             Gestion = b.Gestion
                                         }).ToList();

                // Formatear después de traer los datos
                var modulosFormateados = modulosImpartidos.Select(m => new
                {
                    m.IdModuloImp,
                    m.Docente,
                    m.Modulo,
                    m.Curso,
                    m.Aula,
                    m.Sucursal,
                    Bimestre = $"{m.Bimestre} - {m.FechaInicio:MMM/yyyy}",
                    m.Gestion
                }).ToList();

                dgModulos.ItemsSource = modulosFormateados;
            }
            catch (Exception ex)
            {
                ShowMessage("Error cargando módulos impartidos: " + ex.Message, true);
            }
        }

        private void BtnAgregarModulo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarFormulario())
                    return;

                // Crear nuevo módulo impartido
                var nuevoModuloImpartido = new ModuloImpartido
                {
                    IdModulo = (int)cmbModulo.SelectedValue,
                    IdPersona = (int)cmbDocente.SelectedValue,
                    IdAula = (int)cmbAula.SelectedValue,
                    IdBimestre = (int)cmbBimestre.SelectedValue,
                    IdHorario = 0 // Siempre null como solicitaste
                };

                // Insertar en la base de datos
                dcBd.ModuloImpartido.InsertOnSubmit(nuevoModuloImpartido);
                dcBd.SubmitChanges();

                ShowMessage("Módulo impartido agregado exitosamente!", false);
                LimpiarFormulario();
                LoadModulosImpartidos();
            }
            catch (Exception ex)
            {
                ShowMessage("Error agregando módulo impartido: " + ex.Message, true);
            }
        }

        private bool ValidarFormulario()
        {
            if (cmbDocente.SelectedValue == null)
            {
                ShowMessage("Seleccione un docente", true);
                return false;
            }

            if (cmbModulo.SelectedValue == null)
            {
                ShowMessage("Seleccione un módulo", true);
                return false;
            }

            if (cmbSucursal.SelectedValue == null)
            {
                ShowMessage("Seleccione una sucursal", true);
                return false;
            }

            if (cmbAula.SelectedValue == null)
            {
                ShowMessage("Seleccione un aula", true);
                return false;
            }

            if (cmbBimestre.SelectedValue == null)
            {
                ShowMessage("Seleccione un bimestre", true);
                return false;
            }

            return true;
        }

        private void BtnLimpiarFormulario_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            cmbDocente.SelectedIndex = -1;
            cmbModulo.SelectedIndex = -1;
            cmbSucursal.SelectedIndex = -1;
            cmbAula.SelectedIndex = -1;
            cmbBimestre.SelectedIndex = -1;
        }

        private void CmbSucursal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSucursal.SelectedValue != null)
            {
                int idSucursal = (int)cmbSucursal.SelectedValue;
                LoadAulasBySucursal(idSucursal);
            }
            else
            {
                cmbAula.ItemsSource = null;
            }
        }

        private void BtnEditarModulo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    var moduloImpartido = button.DataContext as dynamic;
                    if (moduloImpartido != null)
                    {
                        int idModuloImp = moduloImpartido.IdModuloImp;

                        // Abrir ventana de edición
                        var ventanaEdicion = new EditarModuloImpartido(idModuloImp, dcBd);
                        ventanaEdicion.Owner = this;
                        bool? resultado = ventanaEdicion.ShowDialog();

                        // Recargar datos después de editar si se guardaron cambios
                        if (resultado == true)
                        {
                            LoadModulosImpartidos();
                            ShowMessage("Módulo actualizado exitosamente!", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error editando módulo: " + ex.Message, true);
            }
        }

        private void BtnEliminarModulo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    var moduloImpartido = button.DataContext as dynamic;
                    if (moduloImpartido != null)
                    {
                        int idModuloImp = moduloImpartido.IdModuloImp;

                        var result = MessageBox.Show(
                            "¿Está seguro de eliminar este módulo impartido?",
                            "Confirmar Eliminación",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            var moduloAEliminar = dcBd.ModuloImpartido
                                .FirstOrDefault(mi => mi.IdModuloImp == idModuloImp);

                            if (moduloAEliminar != null)
                            {
                                // Verificar si hay estudiantes inscritos
                                var estudiantesInscritos = dcBd.EstudianteInscrito
                                    .Any(ei => ei.IdModuloImp == idModuloImp);

                                if (estudiantesInscritos)
                                {
                                    ShowMessage("No se puede eliminar el módulo porque tiene estudiantes inscritos", true);
                                    return;
                                }

                                dcBd.ModuloImpartido.DeleteOnSubmit(moduloAEliminar);
                                dcBd.SubmitChanges();

                                ShowMessage("Módulo impartido eliminado exitosamente!", false);
                                LoadModulosImpartidos();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error eliminando módulo: " + ex.Message, true);
            }
        }

        private void TxtBuscarModulo_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarModulos();
        }

        private void BtnBuscarModulo_Click(object sender, RoutedEventArgs e)
        {
            FiltrarModulos();
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            LoadModulosImpartidos();
            ShowMessage("Datos actualizados", false);
        }

        private void FiltrarModulos()
        {
            try
            {
                var textoBusqueda = txtBuscarModulo.Text.ToLower();

                var modulosFiltrados = (from mi in dcBd.ModuloImpartido
                                        join p in dcBd.Persona on mi.IdPersona equals p.IdPersona
                                        join m in dcBd.Modulo on mi.IdModulo equals m.IdModulo
                                        join c in dcBd.Curso on m.IdCurso equals c.IdCurso
                                        join a in dcBd.Aula on mi.IdAula equals a.IdAula
                                        join s in dcBd.Sucursal on a.IdSucursal equals s.IdSucursal
                                        join b in dcBd.Bimestre on mi.IdBimestre equals b.IdBimestre
                                        where p.Nombres.ToLower().Contains(textoBusqueda) ||
                                              p.ApPat.ToLower().Contains(textoBusqueda) ||
                                              p.ApMat.ToLower().Contains(textoBusqueda) ||
                                              m.Nombre.ToLower().Contains(textoBusqueda) ||
                                              c.Nombre.ToLower().Contains(textoBusqueda) ||
                                              a.NumeroAula.ToLower().Contains(textoBusqueda) ||
                                              s.Alias.ToLower().Contains(textoBusqueda) ||
                                              b.Gestion.ToLower().Contains(textoBusqueda)
                                        select new
                                        {
                                            IdModuloImp = mi.IdModuloImp,
                                            Docente = p.Nombres + " " + p.ApPat + " " + p.ApMat,
                                            Modulo = m.Nombre,
                                            Curso = c.Nombre,
                                            Aula = a.NumeroAula,
                                            Sucursal = s.Alias,
                                            Bimestre = b.Gestion,
                                            FechaInicio = b.FechaInicio,
                                            Gestion = b.Gestion
                                        }).ToList();

                // Formatear después de traer los datos
                var modulosFormateados = modulosFiltrados.Select(m => new
                {
                    m.IdModuloImp,
                    m.Docente,
                    m.Modulo,
                    m.Curso,
                    m.Aula,
                    m.Sucursal,
                    Bimestre = $"{m.Bimestre} - {m.FechaInicio:MMM/yyyy}",
                    m.Gestion
                }).ToList();

                dgModulos.ItemsSource = modulosFormateados;
            }
            catch (Exception ex)
            {
                ShowMessage("Error filtrando módulos: " + ex.Message, true);
            }
        }

        // Métodos para navegación entre pestañas (placeholder)
        private void BtnHorarios_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("Funcionalidad de Horarios en desarrollo", false);
        }

        private void BtnDocentes_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("Funcionalidad de Docentes en desarrollo", false);
        }

        private void BtnModulos_Click(object sender, RoutedEventArgs e)
        {
            // Ya estamos en esta pestaña
        }

        private void BtnAulas_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("Funcionalidad de Aulas en desarrollo", false);
        }

        private void ShowMessage(string message, bool isError)
        {
            // Puedes implementar un sistema de mensajes similar al que tienes en LogIn
            MessageBox.Show(message, isError ? "Error" : "Información",
                          MessageBoxButton.OK,
                          isError ? MessageBoxImage.Error : MessageBoxImage.Information);
        }
    }
}
