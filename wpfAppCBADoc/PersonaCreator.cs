using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfAppCBADoc
{
    public class PersonaCreator
    {
        // Factory Method principal
        public Persona CrearPersona(string tipo)
        {
            IPersonaFactory factory;

            if (tipo == "Docente")
            {
                factory = new DocenteFactory();
            }
            else if (tipo == "Administrativo")
            {
                factory = new AdministrativoFactory();
            }
            else
            {
                throw new ArgumentException($"Tipo '{tipo}' no válido");
            }

            return factory.CrearPersona();
        }

        // Método todo-en-uno para creación rápida
        public Persona CrearPersonaConDatos(string tipo, string ci, string nombres,
                                          string apPat, string apMat, DateTime fechaNac)
        {
            var persona = CrearPersona(tipo);
            persona.CI = ci;
            persona.Nombres = nombres;
            persona.ApPat = apPat;
            persona.ApMat = apMat;
            persona.FechaNac = fechaNac;
            return persona;
        }
    }
}
