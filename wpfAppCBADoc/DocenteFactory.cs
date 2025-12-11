using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfAppCBADoc
{
    public class DocenteFactory : IPersonaFactory
    {
        public Persona CrearPersona()
        {
            return new Persona { TipoPersona = "Docente" };
        }
    }

}
