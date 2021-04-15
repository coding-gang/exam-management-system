﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data.SqlClient;
using System.Data;
using System.Windows.Forms;

namespace eManagerSystem.Application.Catalog.Server
{
   public class ServerService  : IServerService 
    {
       
        IPEndPoint IP;
        Socket server;
        List<Socket> clientList;
        private readonly string strCon = @"SERVER=DESKTOP-4ICDD5V\SQLEXPRESS;Database =ExamManagement;User Id=test;password=nguyenmautuan123";
    
 
        public void Connect()
        {
            clientList = new List<Socket>();
       
        
            IP = new IPEndPoint(IPAddress.Any, 9999);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(IP);

            Thread Listen = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        server.Listen(100);
                        Socket client = server.Accept();
                        clientList.Add(client);
                        Thread receive = new Thread(Receive);
                        receive.IsBackground = true;
                        receive.Start(client);
                    }
                }
                catch
                {
                    IP = new IPEndPoint(IPAddress.Any, 9999);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
              
            });
            Listen.IsBackground = true;
            Listen.Start();
                

        }
        public void SendFile(string filePath)
        {
            foreach( Socket client in clientList)
            {
                if (filePath != String.Empty)
                {
                    SendData sendData = new SendData
                    {
                        option = Serialize("Send File"),
                        data = GetFilePath(filePath)
                    };
                    client.Send(Serialize(sendData));
                   


                }
            }
           
        }
        public delegate void UpdateHandler(object sender, UpdateEventArgs args);
        public event UpdateHandler EventUpdateHandler;
        public class UpdateEventArgs : EventArgs
        {
            public string mssv { get; set; }

        }
        public void Updates(string MSSV)
        {
            UpdateEventArgs args = new UpdateEventArgs();
         
                args.mssv = MSSV;
                EventUpdateHandler.Invoke(this, args);
          

        }
        public string Messgase { get; set; }

        public void  Receive(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);
                  SendData receiveData = new SendData();
                   receiveData = (SendData)Deserialize(data);
                    switch ((string)Deserialize(receiveData.option))
                    {
                        case "Send Accept":
                           var mssv = (string)Deserialize(receiveData.data);
                            Updates(mssv);
                            break;
                     
                        default:
                            break;
                    }

                }

            }
            catch(Exception er)
            {
                throw er;
              //  clientList.Remove(client);
               // client.Close();
            }
        }
     

        public void Close()
        {
            server.Close();
        }

        public byte[] GetFilePath(string filePath)
        {
          //  var name = Path.GetFileName(filePath);
            byte[] fNameByte = Encoding.ASCII.GetBytes(filePath);
            byte[] fileData = File.ReadAllBytes(filePath);
            byte[] serverData = new byte[4 + fNameByte.Length + fileData.Length];
            byte[] fNameLength = BitConverter.GetBytes(fNameByte.Length);
            fNameLength.CopyTo(serverData, 0);
            fNameByte.CopyTo(serverData, 4);
            fileData.CopyTo(serverData,4+fNameByte.Length);
            return serverData;
        }

        public object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();
           return formatter.Deserialize(stream);
           
        }

        private void hasParameter(SqlCommand cmd, string query, object[] para = null)
        {
            int i = 0;
            foreach (string parameter in query.Split(' ').ToArray().Where(p => p.Contains('@')))
            {
                cmd.Parameters.AddWithValue(parameter, para[i]);

                i++;
            }
        }

        private byte[] Serialize(object data)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(memoryStream, data);
            memoryStream.Close();
            return memoryStream.ToArray();
        }


        public DataTable ExcuteDataReader(string query, object[] para = null)
        {
            try
            {
                DataTable data = new DataTable();
                using (SqlConnection conn = new SqlConnection(strCon))
                {

                    SqlCommand cmd = new SqlCommand(query, conn);
                    if (para != null)
                    {

                        {
                            hasParameter(cmd, query, para);
                        }

                    }
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(data);


                }
                return data;
            }
            catch (Exception err)
            {
                throw err;
            }

        }

        public IEnumerable<Students> readAll(int gradeId)
        {
            DataTable dataTable = ExcuteDataReader("usp_getAllStudentBySubject @gradeId", new object[] { gradeId });
            List<Students> listStudents = new List<Students>();
            foreach (DataRow row in dataTable.Rows)
            {
                Students students = new Students(row);
                listStudents.Add(students);
            
            }
            return listStudents;
        }

        public List<Students> ReadAll(int gradeId)
        {
            return (List<Students>)readAll(gradeId);
        }

        public IEnumerable<Grade> getAllGrade()
        {
            DataTable dataTable = ExcuteDataReader("usp_getGrade");
            List<Grade> listGrades = new List<Grade>();
            foreach (DataRow row in dataTable.Rows)
            {
                Grade grades = new Grade(row);
                listGrades.Add(grades);

            }
            return listGrades;
        }

   

        public void SendUser(string option,List<Students> students)
        {
            foreach (Socket client in clientList)
            {
                if (option != String.Empty)
                {
                    SendData sendData = new SendData
                    {
                        option = Serialize("Send User"),
                        data = Serialize(students)
                    };
                    client.Send(Serialize(sendData));
                }
            }
        }

        public IEnumerable<Subject> getAllSubject()
        {
                DataTable dataTable = ExcuteDataReader("usp_getSubjects");
                List<Subject> listSubject = new List<Subject>();
                foreach (DataRow row in dataTable.Rows)
                {
                    Subject subject = new Subject(row);
                    listSubject.Add(subject);

                }
                return (IEnumerable<Subject>)listSubject;
            
        }

        public void SendSubject(string subject)
        {
            foreach (Socket client in clientList)
            {
                if (subject != String.Empty)
                {
                    SendData sendData = new SendData
                    {
                        option = Serialize("Send Subject"),
                        data = Serialize(subject)
                    };
                    client.Send(Serialize(sendData));
                }
            }
        }
    }
}
