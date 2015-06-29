using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WinTestLogCompress
{

    public class RecvBroadcst
    {
        public static void Main()
        {
            Socket sock = new Socket(AddressFamily.InterNetwork,
                            SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Any, 9871);
            sock.Bind(iep);
            EndPoint ep = (EndPoint)iep;
            Console.WriteLine("Ready to receive...");
            int qsoCount = 0;
            List<byte[]> qsoBlock = new List<byte[]>();
                
            while (sock.IsBound == true)
            {
                string rx = getUdpLine(sock, ep);
                Console.WriteLine(rx);
                if (rx.Contains("ADDQSO:"))
                {
                    byte[] compressedQso = handleQso(rx);
                    qsoCount++;
                    qsoBlock.Add(compressedQso);
                }
                if (qsoCount == 9)
                {
                    writeQsoBlockFile(qsoBlock);
                    qsoCount = 0;
                }
            }

            if (qsoBlock.Count >= 1)
            {
                // The socket has closed when there are unwritten QSOs in the buffer
                writeQsoBlockFile(qsoBlock);
            }
            sock.Close();
            //TODO: Rebind automatically if socket closes
        }

        private static void writeQsoBlockFile(List<byte[]> qsoBlock)
        {
            byte[] allBytes = qsoBlock.SelectMany(a => a).ToArray();
            DateTime now = DateTime.Now;
            string filename = setLogFilename();  
            File.WriteAllBytes(filename, allBytes);
        }

        private static string setLogFilename()
        {
            // Setting an incrementing serial number on the file means the server can ACK the serial
            // and we can resend the file if necessary.
            string logDir = @"C:\logs\";
            string[] filesInDir = Directory.GetFiles(logDir);
            int nextNum = filesInDir.Length;
            nextNum = nextNum + 1;
            string nextFile = nextNum.ToString().PadLeft(4, '0'); 
            return logDir + nextFile + ".bin";
        }

        private static byte[] handleQso(string rx)
        {
            Qso qso = new Qso();
            string[] values = rx.Split(' ');
            qso.bandId = Int32.Parse(values[7]);
            qso.modeId = Int32.Parse(values[6]);
            qso.qsoTime = Int32.Parse(values[4]);
            qso.dxCall = values[13].Replace("\"","");
            qso.rstTx = Int32.Parse(values[14].Replace("\"",""));
            qso.rstRx = Int32.Parse(values[15].Replace("\"",""));
            qso.opCall = values[22].Replace("\"","");
            Console.WriteLine(qso.displayQso());
            return qso.compress();
        }

        public static string getUdpLine(Socket sock, EndPoint ep)
        {
            byte[] data = new byte[1024];
            int recv = sock.ReceiveFrom(data, ref ep);
            string stringData = Encoding.ASCII.GetString(data, 0, recv);
            return stringData;
        }

    }
}