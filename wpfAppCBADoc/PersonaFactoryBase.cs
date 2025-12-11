using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfAppCBADoc
{
    public abstract class PersonaFactoryBase : IPersonaFactory
    {
        public abstract Persona CrearPersona();
        public abstract string TipoPersona { get; }

        // Método protegido para configurar datos básicos
        protected void ConfigurarDatosBasicos(Persona persona, string ci, string nombres,
                                             string apPat, string apMat, DateTime fechaNac)
        {

            persona.CI = ci;
            persona.Nombres = nombres;
            persona.ApPat = apPat;
            persona.ApMat = apMat;
            persona.FechaNac = fechaNac;
            persona.TipoPersona = TipoPersona;
        }
    }
}
