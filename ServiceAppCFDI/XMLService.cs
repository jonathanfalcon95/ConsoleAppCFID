using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace ServiceAppCFDI
{
    public partial class XMLService : ServiceBase
    {

        Timer timer = new Timer(); // name space(using System.Timers;)
        public XMLService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is started at " + DateTime.Now);
            Console.WriteLine("service...start");
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 5000; //number in milisecinds
            timer.Enabled = true;
            //CallXMLFile();
            //GetXmlFiles();
            GetRoutes();

        }

        protected override void OnStop()
        {
            WriteToFile("Service is stopped at " + DateTime.Now);
            Console.WriteLine("service stop");
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Service is recall at " + DateTime.Now);
            Console.WriteLine("windows service call every 5 seconds ");

        }

        public void SaveXMLFile(XmlReader xmlreader)
        {
            Console.WriteLine("xml save");
            //code to read local xml files
            //string sourceFile = @"C:/Users/usuario/Downloads/modelxml/B-55238797_Ingresos_Nacional.xml";
            //XmlTextReader xmlreader = new XmlTextReader(sourceFile);
            //string xmlString = File.ReadAllText(sourceFile);
            DataSet ds = new DataSet();
            ds.ReadXml(xmlreader);
            xmlreader.Close();
            string storedProcedureName = "SaveXMLFactura";
            var connString = ConfigurationManager.ConnectionStrings["db_cfdi"].ConnectionString;
            using (var sqlConn = new SqlConnection(connString))
            {
                sqlConn.Open();
                using (var sqlCommand = new SqlCommand(storedProcedureName, sqlConn))
                {
                    sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    //sqlCommand.Parameters.Add(new SqlParameter("@cfdi", xmlString));
                    sqlCommand.Parameters.Add("@cfdi", SqlDbType.Xml).Value = ds.GetXml();

                    int cont = sqlCommand.ExecuteNonQuery();
                    Console.WriteLine("Se ejecutaron " + cont + " Querys");


                }
            }


        }

        public void GetXmlFiles()
        {
            //Console.WriteLine("xml download");
            string host = @"57.77.28.25";
            string username = "terra";
            string password = "NJ5$nm369V";



            string remoteDirectory = "/TerraMain/CFDI/";
            // string localDirectory = @"D:\Download\New folder\";

            using (var sftp = new SftpClient(host, username, password))
            {
                try
                {
                    sftp.Connect();
                    var files = sftp.ListDirectory(remoteDirectory);

                    foreach (var file in files)
                    {
                        string remoteFileName = file.Name;

                        if (!file.Name.StartsWith(".") && file.Name.EndsWith(".xml"))
                        {
                            Console.WriteLine(remoteFileName);
                            WriteToFile("Service save" + remoteFileName);
                            using (XmlReader reader = XmlReader.Create(sftp.OpenRead(remoteDirectory + file.Name)))
                            {
                                SaveXMLFile(reader);
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An exception has been caught " + e.ToString());
                    WriteToFile("An exception has been caught " + e.ToString());
                }

            }
        }

        public List<proveedor> GetRoutes()
        {
            var listaRuta = new List<proveedor>();
            var connString = ConfigurationManager.ConnectionStrings["db_cfdi"].ConnectionString;

            using (SqlConnection myConnection = new SqlConnection(connString))
            {
                string oString = "Select * from tbl_proveedor_comprobante";
                SqlCommand oCmd = new SqlCommand(oString, myConnection);
                myConnection.Open();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                  
                    proveedor newProveedor = new proveedor();
                    while (oReader.Read())
                    {
                        newProveedor.id = (int)oReader["id"];
                        newProveedor.nombre_proveedor = oReader["nombre_proveedor"].ToString();
                        newProveedor.ruta_fuente_comprobante = oReader["ruta_fuente_comprobante"].ToString();
                        newProveedor.ruta_proceso_exitoso = oReader["ruta_proceso_exitoso"].ToString();
                        newProveedor.ruta_proceso_fallido = oReader["ruta_proceso_fallido"].ToString();
                        newProveedor.tipo_comprobante = (int)oReader["tipo_comprobante_id"];
                      
                        Console.WriteLine(newProveedor.ruta_fuente_comprobante);
                        listaRuta.Add(newProveedor);
                    }

                    myConnection.Close();
                }
            }
            return listaRuta;
        }


        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to. 
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }



}
