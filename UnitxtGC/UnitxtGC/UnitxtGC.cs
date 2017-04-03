using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Libraries.ByteArray;
using Newtonsoft.Json;
using PSO.PRS;

namespace UnitxtGC
{
    public class UnitxtGC
    {
        public void ProcessArgs(string[] args)
        {
            int mode = 0;
            string path = string.Empty;

            if (args.Length == 2)
            {
                path = args[args.Length - 1];

                if (string.Compare(args[0], "-json2bin", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    mode = 1;
                }
                else if (string.Compare(args[0], "-bin2json", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    mode = 2;
                }
            }

            if (mode == 0)
            {
                string helpStr = "\n" +
                    "Usage: unitxt -mode path\n" +
                    "\n" +
                    "    -json2bin: Converts a json text file into a pr2/pr3 file pair.\n" +
                    "    -bin2json: Converts a pr2/pr3 file pair into a json file.\n" +
                    "               For bin2json mode, specify either the pr2 or pr3 file\n" +
                    "               the program will load both files";
                Console.WriteLine(helpStr);
            }
            else
            {
                try
                {
                    ProcessFile(path, mode);
                    Console.WriteLine("Processed {0}", path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            
#if DEBUG
            try
            {
                ProcessFile(@"..\..\..\Files\text_d.pr3", 2);
                ProcessFile(@"..\..\..\Files\text_d.json", 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
#endif
        }

        private void ProcessFile(string path, int mode)
        {
            if (mode == 1)
            {
                JsonToBin(path);
            }
            else if (mode == 2)
            {
                BinToJson(path);
            }
        }

        private void JsonToBin(string filename)
        {
            byte[] data = File.ReadAllBytes(filename);
            UnitxtGCFile unitxt = JsonDeserialize<UnitxtGCFile>(data, 0, data.Length);

            int pr3_pointers = 0;
            // Add all the strings as well as the group pointer
            for (int i1 = 0; i1 < 44; i1++)
            {
                pr3_pointers += 1;
                pr3_pointers += unitxt.StringGroups[i1].entries.Count;
            }

            // Whatever, this should be enough, every time
            ByteArray baPr2 = new ByteArray(1024 * 1024);
            ByteArray baPr3 = new ByteArray((pr3_pointers + 5) * 2 + 32);

            for (int i1 = 0; i1 < 44; i1++)
            {
                for (int i2 = 0; i2 < unitxt.StringGroups[i1].entries.Count; i2++)
                {
                    // Save the string offset
                    unitxt.StringGroups[i1].stringOffsets.Add(baPr2.Position);

                    baPr2.WriteStringA(unitxt.StringGroups[i1].entries[i2], 0, unitxt.StringGroups[i1].entries[i2].Length, true);
                    baPr2.Pad(4);
                }
            }

            // Write the tables
            // We'll need the first one 
            List<int> tablePointers = new List<int>();
            baPr2.Endianess = Endianess.BigEndian;
            for (int i1 = 0; i1 < unitxt.SomeTables.Count; i1++)
            {
                // Save the table offset
                tablePointers.Add(baPr2.Position);
                for (int i2 = 0; i2 < unitxt.SomeTables[i1].Count; i2++)
                {
                    baPr2.Write(unitxt.SomeTables[i1][i2]);
                }
            }

            // We'll need this offset, it's the beginning of the short table pointer in pr3
            int tablePointer = baPr2.Position;
            for (int i1 = 0; i1 < unitxt.SomeTables.Count; i1++)
            {
                // Save the table offset
                baPr2.Write(tablePointers[i1]);
            }
            // Table count offset, needed at the end
            int tableCountOffset = baPr2.Position;
            baPr2.Endianess = Endianess.LittleEndian;
            baPr2.Write(2);
            baPr2.Endianess = Endianess.BigEndian;
            baPr2.Write(tablePointer);

            for (int i1 = 0; i1 < 44; i1++)
            {
                unitxt.StringGroups[i1].groupOffset = baPr2.Position;
                for (int i2 = 0; i2 < unitxt.StringGroups[i1].stringOffsets.Count; i2++)
                {
                    baPr2.Write(unitxt.StringGroups[i1].stringOffsets[i2]);
                }
            }
            int stringGroupOffset = baPr2.Position;
            for (int i1 = 0; i1 < 44; i1++)
            {
                baPr2.Write(unitxt.StringGroups[i1].groupOffset);
            }
            int tableCountOffsetOffset = baPr2.Position;
            baPr2.Write(tableCountOffset);
            baPr2.Write(stringGroupOffset);

            baPr2.Resize(baPr2.Position);

            // Write Pr3 data
            baPr3.Write(0x20);
            baPr3.Endianess = Endianess.BigEndian;
            baPr3.Write(pr3_pointers + 5);
            baPr3.Write(1);
            baPr3.Write(0);
            baPr3.Write(tableCountOffsetOffset);
            baPr3.Write(0);
            baPr3.Write(0);
            baPr3.Write(0);

            // Just fill this stuff
            baPr3.Write((short)(tablePointer / 4));
            baPr3.Write((short)1);
            baPr3.Write((short)2);
            for (int i1 = 0; i1 < pr3_pointers; i1++)
            {
                baPr3.Write((short)1);
            }
            baPr3.Write((short)1);
            baPr3.Write((short)1);

            uint prc_key = (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            byte[] dataPR2 = CompressPRC(baPr2.Buffer, prc_key);
            byte[] dataPR3 = CompressPRC(baPr3.Buffer, prc_key);

            File.WriteAllBytes(Path.ChangeExtension(filename, ".pr2"), dataPR2);
            File.WriteAllBytes(Path.ChangeExtension(filename, ".pr3"), dataPR3);
        }
        private void BinToJson(string filename)
        {
            // Create this early on
            UnitxtGCFile unitxt = new UnitxtGCFile();

            string pathPR2 = Path.ChangeExtension(filename, "pr2");
            string pathPR3 = Path.ChangeExtension(filename, "pr3");

            byte[] dataPR2 = File.ReadAllBytes(pathPR2);
            byte[] dataPR3 = File.ReadAllBytes(pathPR3);
            
            dataPR2 = DecompressPRC(dataPR2);
            dataPR3 = DecompressPRC(dataPR3);

            ByteArray baPR2 = new ByteArray(dataPR2);
            ByteArray baPR3 = new ByteArray(dataPR3);
            
            // This offset is not BE
            int shortPointerTableOffset = baPR3.ReadI32();
            baPR3.Endianess = Endianess.BigEndian;
            int shortPointerTableCount = baPR3.ReadI32();
            // We don't care about the rest
            baPR3.Position = shortPointerTableOffset;
            
            List<int> shortPointerTable = new List<int>();
            int chain = 0;
            for (int i1 = 0; i1 < shortPointerTableCount; i1++)
            {
                chain = baPR3.ReadI16() * 4 + chain;
                shortPointerTable.Add(chain);
            }

            // Read starting pointers for the PR2 data
            // Last 2 pointers are the ones we need
            baPR2.Position = shortPointerTable[shortPointerTable.Count - 2];
            baPR2.Endianess = Endianess.BigEndian;
            int unitxtTablesPointer = baPR2.ReadI32();
            int unitxtStringGroupsPointer = baPR2.ReadI32();

            // Judging by other REL files this is the count, but the data says 023C0000... 
            // Could be an error in the data? We'll find out
            baPR2.Position = unitxtTablesPointer;
            baPR2.Endianess = Endianess.LittleEndian;
            int unitxtTableCount = baPR2.ReadI32();
            baPR2.Endianess = Endianess.BigEndian;
            // Set the actual count we don't care about that value
            unitxtTableCount = 2;
            int unitxtTablePointer = baPR2.ReadI32();
            
            for (int i1 = 0; i1 < unitxtTableCount; i1++)
            {
                baPR2.Position = baPR2.ReadI32(unitxtTablePointer + i1 * 4);

                unitxt.SomeTables.Add(new List<short>());
                // Each table has 112 entries, apparently
                for (int i2 = 0; i2 < 112; i2++)
                {
                    short value = baPR2.ReadI16();
                    unitxt.SomeTables[i1].Add(value);
                }
            }
            
            for (int i1 = 0; i1 < 44; i1++)
            {
                unitxt.StringGroups.Add(new UnitxtGCGroup() { name = string.Format("Group {0:D2}", i1) });

                int groupPointer = shortPointerTable[shortPointerTable.Count - 46 + i1];
                int groupAddress = baPR2.ReadI32(groupPointer);

                int nextGroupPointer = shortPointerTable[shortPointerTable.Count - 46 + i1 + 1];
                int nextGroupAddress = baPR2.ReadI32(nextGroupPointer);
                if (i1 >= 43)
                {
                    nextGroupPointer = shortPointerTable[shortPointerTable.Count - 1];
                    nextGroupAddress = baPR2.ReadI32(nextGroupPointer);
                }

                while (groupAddress < nextGroupAddress)
                {
                    int stringPointer = baPR2.ReadI32(groupAddress);
                    try
                    {
                        string text = baPR2.ReadStringA(-1, stringPointer);
                        unitxt.StringGroups[i1].entries.Add(text);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                    groupAddress += 4;
                }
                unitxt.StringGroups[i1].count = unitxt.StringGroups[i1].entries.Count;
            }

            string jsonText = JsonSerialize(unitxt, true);
            File.WriteAllText(Path.ChangeExtension(filename, ".json"), jsonText);
        }
        
        private byte[] DecompressPRC(byte[] data)
        {
            ByteArray ba = new ByteArray(data);
            ba.Endianess = Endianess.BigEndian;

            int size = ba.ReadI32();
            uint key = ba.ReadU32();

            byte[] result = new byte[(int)((ba.Length - 8 + 3) & 0xFFFFFFFC)];
            ba.Read(result, 0, ba.Length - 8);

            PRC prc = new PRC(key);
            prc.CryptData(result, 0, result.Length, true);
            Array.Resize(ref result, size);

            result = PRS.Decompress(result);

            return result;
        }
        private byte[] CompressPRC(byte[] data, uint key)
        {
            int dprs_size = data.Length;

            data = PRS.Compress(data);
            int cprs_size = data.Length;

            PRC prc = new PRC(key);
            Array.Resize(ref data, (int)((data.Length + 3) & 0xFFFFFFFC));
            prc.CryptData(data, 0, cprs_size, true);

            ByteArray ba = new ByteArray(cprs_size + 8);
            ba.Endianess = Endianess.BigEndian;

            ba.Write(dprs_size);
            ba.Write(key);
            ba.Write(data, 0, cprs_size);
            
            return ba.Buffer;
        }

        public static string JsonSerialize(object data, bool format = false)
        {
            StringBuilder sb = new StringBuilder();
            JsonTextWriter jw = new JsonTextWriter(new StringWriter(sb));
            JsonSerializer js = new JsonSerializer();

            if (format)
            {
                jw.Formatting = Formatting.Indented;
                jw.Indentation = 4;
                jw.IndentChar = ' ';
            }

            js.Serialize(jw, data);

            return sb.ToString();
        }
        public static T JsonDeserialize<T>(byte[] data, int offset, int count)
        {
            using (StreamReader sr = new StreamReader(new MemoryStream(data, offset, count)))
            {
                JsonSerializer json = new JsonSerializer();
                json.ObjectCreationHandling = ObjectCreationHandling.Replace;
                return json.Deserialize<T>(new JsonTextReader(sr));
            }
        }
    }
}
