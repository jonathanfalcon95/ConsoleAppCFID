﻿using System;
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
            CallXMLFile();
        }

        protected override void OnStop()
        {
            WriteToFile("Service is stopped at " + DateTime.Now);
            Console.WriteLine("service stop");
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Service is recall at " + DateTime.Now);
            Console.WriteLine("service...call");






        }

        public void CallXMLFile()
        {
            Console.WriteLine("xml read");
            string sourceFile = @"D:/Download/modelxml/B-55238797_Ingresos_Nacional.xml";
            XmlTextReader xmlreader = new XmlTextReader(sourceFile);
            string xmlString = File.ReadAllText(sourceFile);
            DataSet ds = new DataSet();
            ds.ReadXml(xmlreader);
            xmlreader.Close();
            Console.WriteLine(xmlString);
            var connString = ConfigurationManager.ConnectionStrings["db_cfdi"].ConnectionString;
            using (var sqlConn = new SqlConnection(connString))
            {
                sqlConn.Open();
                using (var sqlCommand = new SqlCommand("SaveXMLNotaDeCredito", sqlConn))
                {
                    sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    //sqlCommand.Parameters.Add(new SqlParameter("@cfdi", xmlString));
                    sqlCommand.Parameters.Add("@cfdi", SqlDbType.Xml).Value = ds.GetXml();
                    //sqlCommand.Parameters.AddWithValue("@cfdi", xmlString);
                    sqlCommand.ExecuteNonQuery();
                }
            }


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
