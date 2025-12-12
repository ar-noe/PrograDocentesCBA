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
    /// <summary>
    /// Lógica de interacción para MainModulosImp.xaml
    /// </summary>
    public partial class MainModulosImp : Window
    {
        private DataClassesDocentesCBA2DataContext dcBd;
        private string connectionString;

        public MainModulosImp()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            CrearDatosPlaceholderSiNoExisten();
            LoadComboBoxData();
            LoadModulosImpartidos();
        }

        private void InitializeDatabaseConnection()
        {
            try
            {
                connectionString = ConfigurationManager.ConnectionStrings["wpfAppCBADoc.Properties.Settings.PrograCBADocentesConnectionString"].ConnectionString;
                dcBd = new DataClassesDocentesCBA2DataContext(connectionString);
            }
            catch (Exception ex)
            {
                ShowMessage("Error conectando a la base de datos: " + ex.Message, true);
            }
        }

        private void CrearDatosPlaceholderSiNoExisten()
        {
            try
            {
                // Verificar y crear placeholder para Horario
                if (!dcBd.Horario.Any(h => h.IdHorario == 0))
                {
                    dcBd.ExecuteCommand("SET IDENTITY_INSERT Horario ON");
                    dcBd.ExecuteCommand("INSERT INTO Horario (IdHorario, HoraInicio, HoraFinal) VALUES (0, '00:00:00', '00:00:01')");
                    dcBd.ExecuteCommand("SET IDENTITY_INSERT Horario OFF");
                }

                // Verificar y crear placeholder para Persona
                if (!dcBd.Persona.Any(p => p.IdPersona == 0))
                {
                    dcBd.ExecuteCommand("SET IDENTITY_INSERT Persona ON");
                    dcBd.ExecuteCommand("INSERT INTO Persona (IdPersona, CI, Nombres, ApPat, ApMat, FechaNac, TipoPersona) VALUES (0, '0000000', 'Sin', 'Asignar', 'Profesor', '2000-01-01', 'Docente')");
                    dcBd.ExecuteCommand("SET IDENTITY_INSERT Persona OFF");
                }

                // Verificar y crear placeholder para Docente
                if (!dcBd.Docente.Any(d => d.IdDocente == 0))
                {
                    dcBd.ExecuteCommand("SET IDENTITY_INSERT Docente ON");
                    dcBd.ExecuteCommand("INSERT INTO Docente (IdDocente, IdPersona, Especialidad) VALUES (0, 0, 'Pending')");
                    dcBd.ExecuteCommand("SET IDENTITY_INSERT Docente OFF");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Advertencia al crear datos placeholder: " + ex.Message);
            }
        }

        private void LoadComboBoxData()
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
                            WHEN h.IdHorario = 0 THEN 'Sin horario'
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

        private void BtnAgregarModulo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarFormulario())
                    return;

                // Verificar que el horario 0 existe
                if (!dcBd.Horario.Any(h => h.IdHorario == 0))
                {
                    CrearDatosPlaceholderSiNoExisten();
                }

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
                        b.FechaInicio
                    FROM ModuloImpartido mi
                    LEFT JOIN Docente d ON mi.IdDocente = d.IdDocente
                    LEFT JOIN Persona p ON d.IdPersona = p.IdPersona
                    LEFT JOIN Modulo m ON mi.IdModulo = m.IdModulo
                    LEFT JOIN Curso c ON m.IdCurso = c.IdCurso
                    LEFT JOIN Aula a ON mi.IdAula = a.IdAula
                    LEFT JOIN Sucursal s ON a.IdSucursal = s.IdSucursal
                    LEFT JOIN Bimestre b ON mi.IdBimestre = b.IdBimestre
                    WHERE 
                        (CASE WHEN p.IdPersona IS NULL OR p.IdPersona = 0 THEN 'Pending' ELSE p.Nombres + ' ' + ISNULL(p.ApPat, '') + ' ' + ISNULL(p.ApMat, '') END LIKE '%' + @busqueda + '%') OR
                        m.Nombre LIKE '%' + @busqueda + '%' OR
                        c.Nombre LIKE '%' + @busqueda + '%' OR
                        a.NumeroAula LIKE '%' + @busqueda + '%' OR
                        s.Alias LIKE '%' + @busqueda + '%' OR
                        b.Gestion LIKE '%' + @busqueda + '%'";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@busqueda", textoBusqueda);

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
                                    Gestion = reader["Gestion"]
                                });
                            }
                            dgModulos.ItemsSource = resultado;
                        }
                    }
                }
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