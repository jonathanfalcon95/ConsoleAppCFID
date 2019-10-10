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
            timer.Interval = Convert.ToDouble(ConfigurationManager.AppSettings["timer"]); //number in milisecinds
            timer.Enabled = true;
            ProcessProviderRoutes();
        }

        protected override void OnStop()
        {
            WriteToFile("Service is stopped at " + DateTime.Now);
            Console.WriteLine("service stop");
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Service is recall at " + DateTime.Now);
            Console.WriteLine("windows service recall...");
            ProcessProviderRoutes();

        }
        public void ProcessProviderRoutes()
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


                    while (oReader.Read())
                    {
                        proveedor newProveedor = new proveedor();
                        newProveedor.id = (int)oReader["id"];
                        newProveedor.nombre_proveedor = oReader["nombre_proveedor"].ToString();
                        newProveedor.ruta_fuente_comprobante = oReader["ruta_fuente_comprobante"].ToString();
                        newProveedor.ruta_proceso_exitoso = oReader["ruta_proceso_exitoso"].ToString();
                        newProveedor.ruta_proceso_fallido = oReader["ruta_proceso_fallido"].ToString();
                        newProveedor.tipo_comprobante = (int)oReader["tipo_comprobante_id"];

                        //Console.WriteLine(newProveedor.ruta_fuente_comprobante);
                        listaRuta.Add(newProveedor);
                    }

                    myConnection.Close();
                }
            }

            foreach (var prov in listaRuta)
            {
                Console.WriteLine(prov.ruta_fuente_comprobante);
                //por cada proveedor se recorre el metodo para procesar xml
                ProcessXmlFiles(prov.ruta_fuente_comprobante, prov.ruta_proceso_exitoso, prov.ruta_proceso_fallido, prov.tipo_comprobante);


            }


        }



        public void ProcessXmlFiles(string route = "", string routedexito = "", string routefallo = "", int tipoxml = 1)
        {

            string host = ConfigurationManager.AppSettings["host"];
            string username = ConfigurationManager.AppSettings["username"];
            string password = ConfigurationManager.AppSettings["password"];
            string SPName; //name for stored procedure
            if (tipoxml == 1)
                SPName = ConfigurationManager.AppSettings["storedProcedure1"];
            else
                if (tipoxml == 2)
                SPName = ConfigurationManager.AppSettings["storedProcedure2"];
            else
            if (tipoxml == 3)
                SPName = ConfigurationManager.AppSettings["storedProcedure3"];
            else

                SPName = ConfigurationManager.AppSettings["storedProcedure4"];

            bool retry = false;
            do
            {
                bool retrying = retry;
                retry = false;

                using (var sftp = new SftpClient(host, username, password))
                {

                    try
                    {
                        sftp.Connect();
                        var files = sftp.ListDirectory(route).Where(f => !f.IsDirectory).Take(int.Parse(ConfigurationManager.AppSettings["numberXML"]));
                
                        foreach (var file in files)
                        {
                            

                            string remoteFileName = file.Name;
                            bool fileExist = CheckIfRemoteFileExists(sftp, routedexito, remoteFileName);

                            if (!file.Name.StartsWith(".") && file.Name.EndsWith(".xml") && !fileExist && !retrying)
                            {
                                Console.WriteLine(remoteFileName);
                                WriteToFile("Service save" + remoteFileName);
                                using (XmlReader reader = XmlReader.Create(sftp.OpenRead(route + file.Name)))
                                {
                                    SaveXMLFile(reader, SPName);
                                }

                                file.MoveTo(routedexito + remoteFileName);

                            }
                            else
                                if (retrying || fileExist)//si se reintento la conexion o el nombre del archivo es duplicado
                                 {
                                      file.MoveTo(routefallo + remoteFileName);
                                 }

                            
                        }
                        //sftp.Disconnect();
                    }
                    catch (Exception e)
                    {

                        retry = true;
                        //filecatch.MoveTo(routefallo);
                        Console.WriteLine("An exception has been caught " + e.ToString());
                        string error = WriteToFile("An 1 exception has been caught " + e.ToString());
                        Console.WriteLine(error);
                        //using (var fileStream = new FileStream(error, FileMode.Open))
                        //{
                        //    //Console.WriteLine("Uploading {0} ({1:N0} bytes)", uploadfile, fileStream.Length);
                        //    sftp.BufferSize = 4 * 1024; // bypass Payload error large files
                        //    sftp.UploadFile(fileStream, error);
                        //}
                    }

                }
            }
            while (retry);
        }
        public void SaveXMLFile(XmlReader xmlreader, string SPName = "")
        {
            Console.WriteLine("xml save");
            DataSet ds = new DataSet();
            ds.ReadXml(xmlreader);
            xmlreader.Close();
            string storedProcedureName = SPName;
            var connString = ConfigurationManager.ConnectionStrings["db_cfdi"].ConnectionString;
            using (var sqlConn = new SqlConnection(connString))
            {
                sqlConn.Open();
                using (var sqlCommand = new SqlCommand(storedProcedureName, sqlConn))
                {
                    sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add("@cfdi", SqlDbType.Xml).Value = ds.GetXml();

                    int cont = sqlCommand.ExecuteNonQuery();
                    Console.WriteLine("Se ejecutaron " + cont + " Querys");


                }
            }


        }


        public bool CheckIfRemoteFileExists(SftpClient sftpClient, string remoteFolderName, string remotefileName)
        {
            bool isFileExists = sftpClient
                                .ListDirectory(remoteFolderName)
                                .Any(
                                        f => f.IsRegularFile &&
                                        f.Name.ToLower() == remotefileName.ToLower()
                                    );
            return isFileExists;
        }



        public string WriteToFile(string Message)
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
            return filepath;
        }
    }



}
