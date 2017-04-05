using System;
using Libraries.ByteArray;
using PSO.PRS;

namespace PSOCT
{
    class PSOCT
    {
        static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            Console.WriteLine("Unhandled exception\n{0}", ex);
        }

        static void Main(string[] args)
        {
            AppDomain domain = AppDomain.CurrentDomain;
            domain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            new PSOCT().MainNonStatic(args);

#if DEBUG
            try
            {
                UnitxtDC.BinToJson(@"..\..\..\Files\DC\text_n.pr2");
                //UnitxtDC.JsonToBin(@"..\..\..\Files\DC\text_n.json");
                
                //UnitxtGC.BinToJson(@"..\..\..\Files\GC\text_o.pr2");
                //UnitxtGC.JsonToBin(@"..\..\..\Files\GC\text_n.json");

                //UnitxtXB.BinToJson(@"..\..\..\Files\XB\text_o.pr2");
                //UnitxtXB.JsonToBin(@"..\..\..\Files\XB\text_n.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("DEBUG: Execution finished, press any key to exit");
            Console.Read();
#endif
        }

        public void MainNonStatic(string[] args)
        {
            int system = 0;
            int mode = 0;
            string path = string.Empty;

            if (args.Length == 2)
            {
                // Path is always last
                path = args[args.Length - 1];

                if (string.Compare(args[0], "-dc", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    Console.WriteLine("Cannot use DC mode right now");
                    //system = 1;
                }
                else if (string.Compare(args[0], "-gc", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    system = 2;
                }
                else if (string.Compare(args[0], "-xb", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    system = 3;
                }

                if (string.Compare(args[1], "-json2bin", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    mode = 1;
                }
                else if (string.Compare(args[1], "-bin2json", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    mode = 2;
                }
            }

            if (system == 0 || mode == 0)
            {
                string helpStr = "\n" +
                    "Usage: psoct -system -mode path\n" +
                    "\n" +
                    "    -system:   Which system the file to be processed belongs to\n" +
                    "        -dc\n" +
                    "        -gc\n" +
                    "        -xb\n" +
                    "\n" +
                    "    -mode:     Selects which operation mode to use\n" +
                    "        -json2bin: Converts a json text file into a pr2/pr3 file pair.\n" +
                    "        -bin2json: Converts a pr2/pr3 file pair into a json file.\n" +
                    "\n" +
                    "    path:      Selects file to process";
                Console.WriteLine(helpStr);
            }
            else
            {
                try
                {
                    ProcessFile(path, system, mode);
                    Console.WriteLine("Processed {0}", path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        private void ProcessFile(string path, int system, int mode)
        {
            if (system == 1)
            {
                if (mode == 1)
                {
                    UnitxtDC.JsonToBin(path);
                }
                else if (mode == 2)
                {
                    UnitxtDC.BinToJson(path);
                }
            }
            else if (system == 2)
            {
                if (mode == 1)
                {
                    UnitxtGC.JsonToBin(path);
                }
                else if (mode == 2)
                {
                    UnitxtGC.BinToJson(path);
                }
            }
            else if (system == 3)
            {
                if (mode == 1)
                {
                    UnitxtXB.JsonToBin(path);
                }
                else if (mode == 2)
                {
                    UnitxtXB.BinToJson(path);
                }
            }
        }

        internal static byte[] DecompressPRC(byte[] data, bool big_endian)
        {
            ByteArray ba = new ByteArray(data);
            if (big_endian)
            {
                ba.Endianess = Endianess.BigEndian;
            }

            int size = ba.ReadI32();
            uint key = ba.ReadU32();

            byte[] result = new byte[(int)((ba.Length - 8 + 3) & 0xFFFFFFFC)];
            ba.Read(result, 0, ba.Length - 8);

            PRC prc = new PRC(key);
            prc.CryptData(result, 0, result.Length, big_endian);
            Array.Resize(ref result, size);

            result = PRS.Decompress(result);

            return result;
        }
        internal static byte[] CompressPRC(byte[] data, uint key, bool big_endian)
        {
            int dprs_size = data.Length;

            data = PRS.Compress(data);
            int cprs_size = data.Length;

            PRC prc = new PRC(key);
            Array.Resize(ref data, (int)((data.Length + 3) & 0xFFFFFFFC));
            prc.CryptData(data, 0, cprs_size, big_endian);

            ByteArray ba = new ByteArray(cprs_size + 8);
            if (big_endian)
            {
                ba.Endianess = Endianess.BigEndian;
            }

            ba.Write(dprs_size);
            ba.Write(key);
            ba.Write(data, 0, cprs_size);

            return ba.Buffer;
        }
    }
}
