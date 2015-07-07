using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections;

namespace WinTestLogCompress
{
    class Qso
    {
        public int qsoTime;
        public string dxCall;
        public string opCall;
        public int bandId;
        public int modeId;
        public int rstTx;
        public int rstRx;

        public DateTime convertQsoTimeToDateTime()
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return epoch.AddSeconds(this.qsoTime);
        }

        public Int16 minsSinceDXPedStart()
        {
            DateTime qsoTime = this.convertQsoTimeToDateTime();
            //TODO: Change 1st June to 1st July (but I need it in June for debugging at the moment)
            DateTime dxpedStart = new DateTime(2015, 6, 1, 0, 0, 0, 0);
            TimeSpan diff = qsoTime - dxpedStart;
            return Convert.ToInt16(diff.TotalMinutes - 32768);
        }

        public Int32 convertBandIdToBand()
        {
            switch (this.bandId)
            {
                case 1: return 160; 
                case 2: return 80; 
                case 3: return 40; 
                case 4: return 30; 
                case 5: return 20; 
                case 6: return 17; 
                case 7: return 15; 
                case 8: return 12; 
                case 9: return 10;
                case 10: return 6;
                default: return 0;
            }
        }

        public string convertModeIdToMode()
        {
            switch (this.modeId)
            {
                case 0: return "CW";
                case 1: return "SSB";
                case 2: return "RTTY";
                case 3: return "FM";
                case 4: return "PSK";
                default: return "?";
            }
        }

        public string displayQso()
        {
            string q = System.String.Format("QSO: {0} on {1}m {2} at {3}, op: {4}", this.dxCall, this.convertBandIdToBand().ToString(), this.convertModeIdToMode().ToString(), this.convertQsoTimeToDateTime().ToShortTimeString(), this.opCall);
            return q;
        }

        public Int16 convertOpCallToId()
        {
            switch (this.opCall)
            {
                case "G3ZAY": return 1;
                case "M0BLF": return 2;
                case "M0HSW": return 3;
                case "M0TJH": return 4;
                case "M0VFC": return 5;
                case "M1BXF": return 6;
                default: return 0;
            }
        }

        private Int16 writeBitArray(int[] bA)
        {
                string bits = "";
                for (int i = 0; i < 8; i++)
                {
                    bits = bits + bA[i];
                }
                return Convert.ToInt16(bits, 2);
        }

        public byte[] compress()
        {
            // Write start byte (=11111111)
            byte startByte = 255;

            byte[] compressed = new byte[(6 + this.dxCall.Length)];
            compressed[0] = startByte;
            Int16 minsSinceStart = this.minsSinceDXPedStart();
            byte[] mins = BitConverter.GetBytes(minsSinceStart);
            Buffer.BlockCopy(mins, 0, compressed, 1, 2);
            // Byte 3: band (4 bits, LSB); op (4 bits, MSB)
            byte bandAndOp = (byte)((this.bandId & 0x0F) | (this.convertOpCallToId() << 4 & 0xF0));
            compressed[3] = bandAndOp;
            // Byte 4: mode (2 bits, LSB); callsign length (6 bits; MSB)
            byte modeAndCallsignLength = (byte)((this.modeId & 0x03) | (this.dxCall.Length << 2 & 0xFC));
            compressed[4] = modeAndCallsignLength;

            byte[] dxCallAscii = Encoding.ASCII.GetBytes(this.dxCall);
            Buffer.BlockCopy(dxCallAscii, 0, compressed, 5, dxCallAscii.Length);
            string hash;
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                hash = Convert.ToBase64String(md5.ComputeHash(compressed));
                char[] firstHashChar = hash.Substring(0, 1).ToCharArray();
                Buffer.BlockCopy(firstHashChar, 0, compressed, compressed.Length-1, 1);
            }
            string debug = "";
            foreach (byte compressedByte in compressed)
            {
                debug = debug + compressedByte.ToString() + " ";
            }
            Console.WriteLine("Compressed: " + debug + "\nHash: " + hash);
            return compressed;
            
        }
    }
}
