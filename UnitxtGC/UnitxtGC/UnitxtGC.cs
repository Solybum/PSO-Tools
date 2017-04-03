using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Libraries.ByteArray;
using Newtonsoft.Json;

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
                ProcessFile(@"..\..\..\Files\original_text.pr2", 2);
                ProcessFile(@"..\..\..\Files\processed_text.json", 1);
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
            ByteArray dataPr2 = new ByteArray(1024 * 1024);
            ByteArray dataPr3 = new ByteArray((pr3_pointers + 5) * 2 + 32);

            for (int i1 = 0; i1 < 44; i1++)
            {
                for (int i2 = 0; i2 < unitxt.StringGroups[i1].entries.Count; i2++)
                {
                    // Save the string offset
                    unitxt.StringGroups[i1].stringOffsets.Add(dataPr2.Position);

                    dataPr2.WriteStringA(unitxt.StringGroups[i1].entries[i2], 0, unitxt.StringGroups[i1].entries[i2].Length, true);
                    dataPr2.Pad(4);
                }
            }

            // Write the tables
            // We'll need the first one 
            List<int> tablePointers = new List<int>();
            dataPr2.Endianess = Endianess.BigEndian;
            for (int i1 = 0; i1 < unitxt.SomeTables.Count; i1++)
            {
                // Save the table offset
                tablePointers.Add(dataPr2.Position);
                for (int i2 = 0; i2 < unitxt.SomeTables[i1].Count; i2++)
                {
                    dataPr2.Write(unitxt.SomeTables[i1][i2]);
                }
            }

            // We'll need this offset, it's the beginning of the short table pointer in pr3
            int tablePointer = dataPr2.Position;
            for (int i1 = 0; i1 < unitxt.SomeTables.Count; i1++)
            {
                // Save the table offset
                dataPr2.Write(tablePointers[i1]);
            }
            // Table count offset, needed at the end
            int tableCountOffset = dataPr2.Position;
            dataPr2.Endianess = Endianess.LittleEndian;
            dataPr2.Write(2);
            dataPr2.Endianess = Endianess.BigEndian;
            dataPr2.Write(tablePointer);

            for (int i1 = 0; i1 < 44; i1++)
            {
                unitxt.StringGroups[i1].groupOffset = dataPr2.Position;
                for (int i2 = 0; i2 < unitxt.StringGroups[i1].stringOffsets.Count; i2++)
                {
                    dataPr2.Write(unitxt.StringGroups[i1].stringOffsets[i2]);
                }
            }
            int stringGroupOffset = dataPr2.Position;
            for (int i1 = 0; i1 < 44; i1++)
            {
                dataPr2.Write(unitxt.StringGroups[i1].groupOffset);
            }
            int tableCountOffsetOffset = dataPr2.Position;
            dataPr2.Write(tableCountOffset);
            dataPr2.Write(stringGroupOffset);

            dataPr2.Resize(dataPr2.Position);

            // Write Pr3 data
            dataPr3.Write(0x20);
            dataPr3.Endianess = Endianess.BigEndian;
            dataPr3.Write(pr3_pointers + 5);
            dataPr3.Write(1);
            dataPr3.Write(0);
            dataPr3.Write(tableCountOffsetOffset);
            dataPr3.Write(0);
            dataPr3.Write(0);
            dataPr3.Write(0);

            // Just fill this stuff
            dataPr3.Write((short)(tablePointer / 4));
            dataPr3.Write((short)1);
            dataPr3.Write((short)2);
            for (int i1 = 0; i1 < pr3_pointers; i1++)
            {
                dataPr3.Write((short)1);
            }
            dataPr3.Write((short)1);
            dataPr3.Write((short)1);

            File.WriteAllBytes(Path.ChangeExtension(filename, ".pr2"), dataPr2.Buffer);
            File.WriteAllBytes(Path.ChangeExtension(filename, ".pr3"), dataPr3.Buffer);
        }
        private void BinToJson(string filename)
        {
            // Create this early on
            UnitxtGCFile unitxt = new UnitxtGCFile();

            string pathPR2 = Path.ChangeExtension(filename, "pr2");
            string pathPR3 = Path.ChangeExtension(filename, "pr3");

            ByteArray dataPR2 = new ByteArray(File.ReadAllBytes(pathPR2));
            ByteArray dataPR3 = new ByteArray(File.ReadAllBytes(pathPR3));
            
            // This offset is not BE
            int shortPointerTableOffset = dataPR3.ReadI32();
            dataPR3.Endianess = Endianess.BigEndian;
            int shortPointerTableCount = dataPR3.ReadI32();
            // We don't care about the rest
            dataPR3.Position = shortPointerTableOffset;
            
            List<int> shortPointerTable = new List<int>();
            int chain = 0;
            for (int i1 = 0; i1 < shortPointerTableCount; i1++)
            {
                chain = dataPR3.ReadI16() * 4 + chain;
                shortPointerTable.Add(chain);
            }

            // Read starting pointers for the PR2 data
            // Last 2 pointers are the ones we need
            dataPR2.Position = shortPointerTable[shortPointerTable.Count - 2];
            dataPR2.Endianess = Endianess.BigEndian;
            int unitxtTablesPointer = dataPR2.ReadI32();
            int unitxtStringGroupsPointer = dataPR2.ReadI32();

            // Judging by other REL files this is the count, but the data says 023C0000... 
            // Could be an error in the data? We'll find out
            dataPR2.Position = unitxtTablesPointer;
            dataPR2.Endianess = Endianess.LittleEndian;
            int unitxtTableCount = dataPR2.ReadI32();
            dataPR2.Endianess = Endianess.BigEndian;
            // Set the actual count we don't care about that value
            unitxtTableCount = 2;
            int unitxtTablePointer = dataPR2.ReadI32();
            
            for (int i1 = 0; i1 < unitxtTableCount; i1++)
            {
                dataPR2.Position = dataPR2.ReadI32(unitxtTablePointer + i1 * 4);

                unitxt.SomeTables.Add(new List<short>());
                // Each table has 112 entries, apparently
                for (int i2 = 0; i2 < 112; i2++)
                {
                    short value = dataPR2.ReadI16();
                    unitxt.SomeTables[i1].Add(value);
                }
            }
            
            for (int i1 = 0; i1 < 44; i1++)
            {
                unitxt.StringGroups.Add(new UnitxtGCGroup() { name = string.Format("Group {0:D2}", i1) });

                int groupPointer = shortPointerTable[shortPointerTable.Count - 46 + i1];
                int groupAddress = dataPR2.ReadI32(groupPointer);

                int nextGroupPointer = shortPointerTable[shortPointerTable.Count - 46 + i1 + 1];
                int nextGroupAddress = dataPR2.ReadI32(nextGroupPointer);
                if (i1 >= 43)
                {
                    nextGroupPointer = shortPointerTable[shortPointerTable.Count - 1];
                    nextGroupAddress = dataPR2.ReadI32(nextGroupPointer);
                }

                while (groupAddress < nextGroupAddress)
                {
                    int stringPointer = dataPR2.ReadI32(groupAddress);
                    try
                    {
                        string text = dataPR2.ReadStringA(-1, stringPointer);
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
