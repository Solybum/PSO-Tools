using System;
using System.IO;

namespace GC_Unitxt
{
    class Program
    {
        public static string ReadString(byte[] data, int offset)
        {
            string ret = "";
            while (data[offset] != 0)
            {
                ret += Convert.ToChar(data[offset]);
                offset++;
            }
            return ret;
        }
        public static uint SwapEndian(uint n)
        {
            return ((n & 0xff000000) >> 24) | ((n & 0x00ff0000) >> 8) | ((n & 0x0000ff00) << 8) | (n << 24);
        }
        public static int SwapEndian(int n)
        {
            uint val = (uint)(n);
            return (int)(((val & 0xff000000) >> 24) | ((val & 0x00ff0000) >> 8) | ((val & 0x0000ff00) << 8) | (val << 24));
        }
        public static ushort SwapEndian(ushort n)
        {
            return (ushort)(((n & 0xff00) >> 8) | (n << 8));
        }
        public static short SwapEndian(short n)
        {
            ushort val = (ushort)(n);
            return (short)(((n & 0xff00) >> 8) | (n << 8));
        }

        static void OldMain(string[] args)
        {
            //try
            {
                args = new string[] { "E:\\text.bin", "E:\\text.pr3" };
                if (args.Length == 1)
                {
                    if (args[0].EndsWith(".txt"))
                    {
                        string file = args[0].Substring(0, args[0].LastIndexOf(".txt"));
                        // TODO send file name for read
                    }
                }
                else if (args.Length == 2)
                {
                    if (args[0].EndsWith(".bin"))
                    {
                        string file = args[0].Substring(0, args[0].LastIndexOf(".bin"));
                        ReadGCUnitxt(file);
                    }
                }
            }
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //}
            Console.Read();
        }

        public static void ReadGCUnitxt(string file)
        {
            string txtFile = file + ".txt";
            byte[] pr3 = File.ReadAllBytes(file + ".pr3");
            byte[] pr2 = File.ReadAllBytes(file + ".bin");
            int off;

            int[] groupPtr = new int[44];
            int[] groupAdr = new int[44];

            off = SwapEndian(BitConverter.ToInt32(pr2, pr2.Length - 4));
            for (int i1 = 0; i1 < 44; i1++)
            {
                groupPtr[i1] = SwapEndian(BitConverter.ToInt32(pr2, off + i1 * 4));
                groupAdr[i1] = SwapEndian(BitConverter.ToInt32(pr2, groupPtr[i1]));
            }

            int stringCount = SwapEndian(BitConverter.ToInt32(pr3, 4)) - 48;
            int[] strAdr = new int[stringCount];
            off = SwapEndian(BitConverter.ToInt32(pr2, pr2.Length - 8));
            for (int i1 = 0; i1 < strAdr.Length; i1++)
            {
                strAdr[i1] = SwapEndian(BitConverter.ToInt32(pr2, off + i1 * 4 + 8));
            }

            string currStr;
            for (int i1 = 0; i1 < strAdr.Length; i1++)
            {
                currStr = ReadString(pr2, strAdr[i1]);
                currStr = currStr.Replace("\t", "\\t").Replace("\n", "\\n");
                //Console.WriteLine(currStr);
            }

            for (int i1 = 0; i1 < 44; i1++)
            {
                currStr = ReadString(pr2, groupAdr[i1]);
                currStr = currStr.Replace("\t", "\\t").Replace("\n", "\\n");
                Console.WriteLine("Group {0:D2} {1:X8} {2:X8} : {3}", i1, groupPtr[i1], groupAdr[i1], currStr);
            }
        }
    }
}

