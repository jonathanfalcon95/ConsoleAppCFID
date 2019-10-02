using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ServiceAppCFDI
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new MyNewService()
            };
            ServiceBase.Run(ServicesToRun);


            String URLString = "http://localhost/B-55238797_Ingresos_Nacional.xml";
            XmlTextReader files = new XmlTextReader(URLString);


            



        }
       private static void ReadXML(dynamic files)
        {
            Parallel.ForEach(files, async f =>
            foreach (var f in files)
            {
                try
                {
                    using (var stream = new StreamReader(f.OpenRead()))
                    {
                        //var xmlString = await stream.ReadToEndAsync();
                        var xmlString = stream.ReadToEnd();
                        if (!string.IsNullOrWhiteSpace(xmlString))
                        {
                            var connString = ConfigurationManager.ConnectionStrings["db_cfdi"].ConnectionString;
                            using (var sqlConn = new SqlConnection(connString))
                            {
                                sqlConn.Open();
                                using (var sqlCommand = new SqlCommand("Inserta_CDFI_Complemento", sqlConn))
                                {
                                    sqlCommand.Parameters.AddWithValue("@cdfi", xmlString);
                                    sqlCommand.ExecuteNonQuery();
                                }
                            }
                        }
                        stream.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ocurrió un problema al intentar procesar el archivo {f.Name} | ERROR: {ex.ToString()}");
                }
            }
        });
        }

    }
}
