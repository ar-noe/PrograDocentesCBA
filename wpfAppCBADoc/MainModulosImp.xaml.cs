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
using System.Data.SqlClient;

namespace wpfAppCBADoc
{
    public partial class MainModulosImp : Window
    {
        private DataClassesDocentesCBA2DataContext dcDB;
        private string connectionString;

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
                connectionString = ConfigurationManager.ConnectionStrings["wpfAppCBADoc.Properties.Settings.PrograCBADocentesConnectionString"].ConnectionString;
                dcDB = new DataClassesDocentesCBA2DataContext(connectionString);
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
                // Cargar cursos
                var cursos = (from c in dcDB.Curso
                              select new
                              {
                                  IdCurso = c.IdCurso,
                                  Nombre = c.Nombre
                              }).ToList();
                cmbCurso.ItemsSource = cursos;
                cmbCurso.DisplayMemberPath = "Nombre";
                cmbCurso.SelectedValuePath = "IdCurso";

                // Cargar sucursales
                var sucursales = (from s in dcDB.Sucursal
                                  select new
                                  {
                                      IdSucursal = s.IdSucursal,
                                      Alias = s.Alias
                                  }).ToList();
                cmbSucursal.ItemsSource = sucursales;
                cmbSucursal.DisplayMemberPath = "Alias";
                cmbSucursal.SelectedValuePath = "IdSucursal";

                // Cargar bimestres 
                var bimestres = (from b in dcDB.Bimestre
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

                // Inicialmente deshabilitar combobox dependientes
                cmbModulo.IsEnabled = false;
                cmbAula.IsEnabled = false;

            }
            catch (Exception ex)
            {
                ShowMessage("Error cargando datos: " + ex.Message, true);
            }
        }

        private void LoadModulosByCurso(int idCurso)
        {
            try
            {
                var modulos = (from m in dcDB.Modulo
                               where m.IdCurso == idCurso
                               select new
                               {
                                   IdModulo = m.IdModulo,
                                   Nombre = m.Nombre
                               }).ToList();

                cmbModulo.ItemsSource = modulos;
                cmbModulo.DisplayMemberPath = "Nombre";
                cmbModulo.SelectedValuePath = "IdModulo";
                cmbModulo.IsEnabled = true;

                // Limpiar selección si no hay módulos
                if (modulos.Count == 0)
                {
                    cmbModulo.SelectedIndex = -1;
                    cmbModulo.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error cargando módulos: " + ex.Message, true);
            }
        }

        private void LoadAulasBySucursal(int idSucursal)
        {
            try
            {
                var aulas = (from a in dcDB.Aula
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
                cmbAula.IsEnabled = true;

                // Limpiar selección si no hay aulas
                if (aulas.Count == 0)
                {
                    cmbAula.SelectedIndex = -1;
                    cmbAula.IsEnabled = false;
                }
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
                // Usar SQL directo para evitar problemas de LINQ
                string query = @"
                    SELECT 
                        mi.IdModuloImp,
                        CASE 
                            WHEN p.IdPersona IS NULL OR p.IdPersona = 0 THEN 'Pending'
                            ELSE p.Nombres + ' ' + ISNULL(p.ApPat, '') + ' ' + ISNULL(p.ApMat, '') 
                        END AS Docente,
                        m.Nombre AS Modulo,
                        c.Nombre AS Curso,
                        a.NumeroAula AS Aula,
                        s.Alias AS Sucursal,
                        b.Gestion,
                        b.FechaInicio,
                        CASE 
                            WHEN h.IdHorario = 0 THEN 'No schedule'
                            ELSE CONVERT(varchar(5), h.HoraInicio, 108) + ' - ' + CONVERT(varchar(5), h.HoraFinal, 108)
                        END AS Horario
                    FROM ModuloImpartido mi
                    LEFT JOIN Docente d ON mi.IdDocente = d.IdDocente
                    LEFT JOIN Persona p ON d.IdPersona = p.IdPersona
                    LEFT JOIN Modulo m ON mi.IdModulo = m.IdModulo
                    LEFT JOIN Curso c ON m.IdCurso = c.IdCurso
                    LEFT JOIN Aula a ON mi.IdAula = a.IdAula
                    LEFT JOIN Sucursal s ON a.IdSucursal = s.IdSucursal
                    LEFT JOIN Bimestre b ON mi.IdBimestre = b.IdBimestre
                    LEFT JOIN Horario h ON mi.IdHorario = h.IdHorario";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            var resultado = new List<object>();
                            while (reader.Read())
                            {
                                resultado.Add(new
                                {
                                    IdModuloImp = reader["IdModuloImp"],
                                    Docente = reader["Docente"],
                                    Modulo = reader["Modulo"],
                                    Curso = reader["Curso"],
                                    Aula = reader["Aula"],
                                    Sucursal = reader["Sucursal"],
                                    Bimestre = $"{reader["Gestion"]} - {Convert.ToDateTime(reader["FechaInicio"]):MMM/yyyy}",
                                    Gestion = reader["Gestion"],
                                    Horario = reader["Horario"]
                                });
                            }
                            dgModulos.ItemsSource = resultado;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error cargando módulos impartidos: " + ex.Message, true);
            }
        }

        // Evento cuando se selecciona un curso
        private void CmbCurso_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCurso.SelectedValue != null)
            {
                int idCurso = (int)cmbCurso.SelectedValue;
                LoadModulosByCurso(idCurso);
            }
            else
            {
                cmbModulo.ItemsSource = null;
                cmbModulo.IsEnabled = false;
            }
        }

        // Evento cuando se selecciona una sucursal
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
                cmbAula.IsEnabled = false;
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
                    IdDocente = 0, // Docente placeholder
                    IdAula = (int)cmbAula.SelectedValue,
                    IdBimestre = (int)cmbBimestre.SelectedValue,
                    IdHorario = 0 // Horario placeholder
                };

                // Insertar en la base de datos
                dcDB.ModuloImpartido.InsertOnSubmit(nuevoModuloImpartido);
                dcDB.SubmitChanges();

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
            if (cmbCurso.SelectedValue == null)
            {
                ShowMessage("Seleccione un curso", true);
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
            cmbCurso.SelectedIndex = -1;
            cmbModulo.SelectedIndex = -1;
            cmbSucursal.SelectedIndex = -1;
            cmbAula.SelectedIndex = -1;
            cmbBimestre.SelectedIndex = -1;

            // Deshabilitar combobox dependientes
            cmbModulo.IsEnabled = false;
            cmbAula.IsEnabled = false;
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
                        var ventanaEdicion = new EditarModuloImpartido(idModuloImp, dcDB);
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
                            var moduloAEliminar = dcDB.ModuloImpartido
                                .FirstOrDefault(mi => mi.IdModuloImp == idModuloImp);

                            if (moduloAEliminar != null)
                            {
                                // Verificar si hay estudiantes inscritos
                                var estudiantesInscritos = dcDB.EstudianteInscrito
                                    .Any(ei => ei.IdModuloImp == idModuloImp);

                                if (estudiantesInscritos)
                                {
                                    ShowMessage("No se puede eliminar el módulo porque tiene estudiantes inscritos", true);
                                    return;
                                }

                                dcDB.ModuloImpartido.DeleteOnSubmit(moduloAEliminar);
                                dcDB.SubmitChanges();

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

                var query = from mi in dcDB.ModuloImpartido
                            join d in dcDB.Docente on mi.IdDocente equals d.IdDocente into docenteJoin
                            from d in docenteJoin.DefaultIfEmpty()
                            join p in dcDB.Persona on d.IdPersona equals p.IdPersona into personaJoin
                            from p in personaJoin.DefaultIfEmpty()
                            join m in dcDB.Modulo on mi.IdModulo equals m.IdModulo
                            join c in dcDB.Curso on m.IdCurso equals c.IdCurso
                            join a in dcDB.Aula on mi.IdAula equals a.IdAula
                            join s in dcDB.Sucursal on a.IdSucursal equals s.IdSucursal
                            join b in dcDB.Bimestre on mi.IdBimestre equals b.IdBimestre
                            where string.IsNullOrEmpty(textoBusqueda) ||
                                  ((p.IdPersona == null || p.IdPersona == 0 ? "Pending" :
                                    (p.Nombres ?? "") + " " + (p.ApPat ?? "") + " " + (p.ApMat ?? "")).ToLower().Contains(textoBusqueda) ||
                                   m.Nombre.ToLower().Contains(textoBusqueda) ||
                                   c.Nombre.ToLower().Contains(textoBusqueda) ||
                                   a.NumeroAula.ToLower().Contains(textoBusqueda) ||
                                   s.Alias.ToLower().Contains(textoBusqueda) ||
                                   b.Gestion.ToLower().Contains(textoBusqueda))
                            select new
                            {
                                mi.IdModuloImp,
                                Docente = p.IdPersona == null || p.IdPersona == 0 ? "Pending" :
                                         (p.Nombres ?? "") + " " + (p.ApPat ?? "") + " " + (p.ApMat ?? ""),
                                Modulo = m.Nombre,
                                Curso = c.Nombre,
                                Aula = a.NumeroAula,
                                Sucursal = s.Alias,
                                b.Gestion,
                                b.FechaInicio
                            };

                var resultado = query.AsEnumerable() // Traer a memoria para usar métodos de C#
                    .Select(x => new
                    {
                        x.IdModuloImp,
                        x.Docente,
                        x.Modulo,
                        x.Curso,
                        x.Aula,
                        x.Sucursal,
                        Bimestre = $"{x.Gestion} - {x.FechaInicio:MMM/yyyy}",
                        x.Gestion
                    }).ToList();

                dgModulos.ItemsSource = resultado;
            }
            catch (Exception ex)
            {
                ShowMessage("Error filtrando módulos: " + ex.Message, true);
            }
        }

        // Métodos para navegación entre pestañas 
        private void BtnHorarios_Click(object sender, RoutedEventArgs e)
        {
            EditHorarios horarios = new EditHorarios();
            horarios.Show();
            this.Close();
        }

        private void BtnAulas_Click(object sender, RoutedEventArgs e)
        {
            MainClassroom aula = new MainClassroom();
            aula.Show();
            this.Close();
        }
        private void btnDeleteProfessor_Click(object sender, RoutedEventArgs e)
        {
            DeleteProfessor delProf = new DeleteProfessor();
            delProf.Show();
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