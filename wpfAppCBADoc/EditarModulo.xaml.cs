using System;
using System.Configuration;
using System.Linq;
using System.Windows;

namespace wpfAppCBADoc
{
    /// <summary>
    /// Lógica de interacción para EditarModulo.xaml
    /// </summary>
    public partial class EditarModulo : Window
    {
        DataClassesDocentesCBA2DataContext dcDB;

        public EditarModulo(int idModule)
        {
            InitializeComponent();

            string connStr = ConfigurationManager.ConnectionStrings["wpfAppCBADoc.Properties.Settings.PrograCBADocentesConnectionString"].ConnectionString;

            dcDB = new DataClassesDocentesCBA2DataContext(connStr);


            loadDataDB(idModule);
        }


        //mostrar mensajes de error u otros
        private void ShowMessage(string message, bool isError)
        {
            MessageBox.Show(message, isError ? "Error" : "Información",
                          MessageBoxButton.OK,
                          isError ? MessageBoxImage.Error : MessageBoxImage.Information);
        }


        private void loadDataDB(int idP)
        {
            try
            {
                var moduleFound = (from moduloInfo in dcDB.ModuloImpartido
                                   join h in dcDB.Horario on moduloInfo.IdHorario equals h.IdHorario
                                   join m in dcDB.Modulo on moduloInfo.IdModulo equals m.IdModulo
                                   join c in dcDB.Curso on m.IdCurso equals c.IdCurso
                                   join a in dcDB.Aula on moduloInfo.IdAula equals a.IdAula
                                   join s in dcDB.Sucursal on a.IdSucursal equals s.IdSucursal
                                   join d in dcDB.Docente on moduloInfo.IdDocente equals d.IdDocente
                                   join p in dcDB.Persona on d.IdPersona equals p.IdPersona
                                   join b in dcDB.Bimestre on moduloInfo.IdBimestre equals b.IdBimestre
                                   where moduloInfo.IdModuloImp == idP
                                   select new
                                   {
                                       Docente = p.Nombres + " " + p.ApPat + " " + p.ApMat,
                                       Modulo = m.Nombre,
                                       Nombre = c.Nombre,
                                       Alias = s.Alias,
                                       Ubicacion = s.Ubicacion,
                                       Aula = a.NumeroAula,
                                       HoraInicio = h.HoraInicio,
                                       HoraFinal = h.HoraFinal,
                                       FechaInicio = b.FechaInicio,
                                       FechaFin = b.FechaFin,
                                       AulaCap = a.Capacidad
                                   }).FirstOrDefault();

                if (moduleFound == null)
                {
                    ShowMessage("No se encontró información del módulo", true);
                    return;
                }

                // Asignar valores seguros usando operador null-conditional
                txtNombreCurso.Text = moduleFound.Nombre ?? "";
                txtModulo.Text = moduleFound.Modulo ?? "";
                txtDocente.Text = moduleFound.Docente ?? "";

                // Ubicación: TEXT necesita ToString()
                string ubicacion = moduleFound.Ubicacion?.ToString() ?? "";
                txtSucursal.Text = $"{moduleFound.Alias ?? ""} ({ubicacion})";

                txtAula.Text = moduleFound.Aula?.ToString() ?? "";

                // Horario: igual que en Schedules
                txtHorario.Text = $"{moduleFound.HoraInicio} - {moduleFound.HoraFinal}";

                // Fechas: manejar como DateTime
                string fechaInicioStr = "";
                string fechaFinStr = "";

                if (moduleFound.FechaInicio is DateTime fechaInicio)
                {
                    fechaInicioStr = fechaInicio.ToString("dd/MM/yyyy");
                }
                else
                {
                    fechaInicioStr = moduleFound.FechaInicio.ToString() ?? "";
                }

                if (moduleFound.FechaFin is DateTime fechaFin)
                {
                    fechaFinStr = fechaFin.ToString("dd/MM/yyyy");
                }
                else
                {
                    fechaFinStr = moduleFound.FechaFin.ToString() ?? "";
                }

                txtBimestre.Text = $"{fechaInicioStr} - {fechaFinStr}";

                txtCapacidadAula.Text = moduleFound.AulaCap.ToString() ?? "";


                var numberEst = dcDB.EstudianteInscrito.Count(num => num.IdModuloImp == idP);
                txtCantidadEstudiantes.Text = numberEst.ToString();
            }
            catch (Exception e)
            {
                ShowMessage("Error: " + e.Message, true);
            }
        }
    }
}
