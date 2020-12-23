using System;
using System.IO;
using System.Net;
using System.Text;

namespace IFCImportUI
{
    class Api
    {
        public static int Run( ref string dest )
        {
            int status = -1;

            try
            {
                // DEBUG
                using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(@"../../../dest.txt"))
                {
                    file.WriteLine("{ \"command\":\"createProject\", \"activities\":" + dest + "}");
                }
                // ~DEBUG


                WebRequest request = WebRequest.Create("http://localhost:8080");
                request.Method = "POST";
                byte[] bytes = Encoding.UTF8.GetBytes("{ \"command\":\"createProject\", \"activities\":" + dest + "}");

                request.ContentLength = bytes.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(bytes, 0, bytes.Length);
                dataStream.Close();

                WebResponse response = request.GetResponse();
                using (dataStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();
                    status = 0;
                }
                response.Close();
            }
            catch(Exception e)
            {
                ;
            }
            return status;
        }
    }
}
