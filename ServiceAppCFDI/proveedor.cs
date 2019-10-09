using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceAppCFDI
{
   public class proveedor
    {
        public int id { get; set; }
        public string nombre_proveedor { get; set; }
        public string ruta_fuente_comprobante { get; set; }
        public string ruta_proceso_exitoso { get; set; }
        public string ruta_proceso_fallido { get; set; }
        public int  tipo_comprobante { get; set; }
       

    }
}
