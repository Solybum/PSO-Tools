using System;
using System.Collections.Generic;
using System.IO;
using Libraries.ByteArray;
using PSOCT.Unitxt;

namespace PSOCT
{
    public abstract class UnitxtGC
    {
        public static int stringGroupCount = 44;

        public static void JsonToBin(string filename)
        {
            byte[] data = File.ReadAllBytes(filename);
            Dictionary<string, int> stringAddresses = new Dictionary<string, int>();
            UnitxtFile unitxt = Json.Deserialize<UnitxtFile>(data, 0, data.Length);

            int pr3_pointers = 0;
            // Add all the strings as well as the group pointer
            for (int i1 = 0; i1 < stringGroupCount; i1++)
            {
                pr3_pointers += 1;
                pr3_pointers += unitxt.StringGroups[i1].entries.Count;
            }

            // Whatever, this should be enough, every time
            ByteArray baPr2 = new ByteArray(1024 * 1024);
            ByteArray baPr3 = new ByteArray((pr3_pointers + 5) * 2 + 32);

            for (int i1 = 0; i1 < stringGroupCount; i1++)
            {
                for (int i2 = 0; i2 < unitxt.StringGroups[i1].entries.Count; i2++)
                {
                    // Only add this string if we don't have it yet, 
                    // Gotta save the bytes
                    if (!stringAddresses.ContainsKey(unitxt.StringGroups[i1].entries[i2]))
                    {
                        // Save it's address
                        stringAddresses[unitxt.StringGroups[i1].entries[i2]] = baPr2.Position;
                        // Write it out
                        baPr2.WriteStringA(unitxt.StringGroups[i1].entries[i2], 0, unitxt.StringGroups[i1].entries[i2].Length, true);
                        // Some padding?
                        baPr2.Pad(4);
                    }
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
            baPr2.Write(unitxt.tableValue);
            baPr2.Endianess = Endianess.BigEndian;
            baPr2.Write(tablePointer);

            for (int i1 = 0; i1 < stringGroupCount; i1++)
            {
                unitxt.StringGroups[i1].groupOffset = baPr2.Position;
                for (int i2 = 0; i2 < unitxt.StringGroups[i1].entries.Count; i2++)
                {
                    // Instead of getting the addresses from the strings themselves
                    // Just use the dict, no duplicates :)
                    baPr2.Write(stringAddresses[unitxt.StringGroups[i1].entries[i2]]);
                }
            }
            int stringGroupOffset = baPr2.Position;
            for (int i1 = 0; i1 < stringGroupCount; i1++)
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
            byte[] dataPR2 = PSOCT.CompressPRC(baPr2.Buffer, prc_key, true);
            byte[] dataPR3 = PSOCT.CompressPRC(baPr3.Buffer, prc_key, true);

            File.WriteAllBytes(Path.ChangeExtension(filename, ".pr2"), dataPR2);
            File.WriteAllBytes(Path.ChangeExtension(filename, ".pr3"), dataPR3);
        }
        public static void BinToJson(string filename)
        {
            // Create this early on
            UnitxtFile unitxt = new UnitxtFile();

            string pathPR2 = Path.ChangeExtension(filename, "pr2");
            string pathPR3 = Path.ChangeExtension(filename, "pr3");

            byte[] dataPR2 = File.ReadAllBytes(pathPR2);
            byte[] dataPR3 = File.ReadAllBytes(pathPR3);

            dataPR2 = PSOCT.DecompressPRC(dataPR2, true);
            dataPR3 = PSOCT.DecompressPRC(dataPR3, true);

            ByteArray baPR2 = new ByteArray(dataPR2);
            ByteArray baPR3 = new ByteArray(dataPR3);

            int shortPointerTableOffset = baPR3.ReadI32();
            baPR3.Endianess = Endianess.BigEndian;
            int shortPointerTableCount = baPR3.ReadI32();
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
            unitxt.tableValue = baPR2.ReadI32();
            baPR2.Endianess = Endianess.BigEndian;
            int unitxtTablePointer = baPR2.ReadI32();

            for (int i1 = 0; i1 < 2; i1++)
            {
                baPR2.Position = baPR2.ReadI32(unitxtTablePointer + i1 * 4);

                unitxt.SomeTables.Add(new List<short>());
                for (int i2 = 0; i2 < 0xE0; i2++)
                {
                    short value = baPR2.ReadI16();
                    unitxt.SomeTables[i1].Add(value);
                }
            }

            for (int i1 = 0; i1 < stringGroupCount; i1++)
            {
                unitxt.StringGroups.Add(new UnitxtGroup() { name = string.Format("Group {0:D2}", i1) });

                int groupPointer = shortPointerTable[shortPointerTable.Count - (stringGroupCount + 2) + i1];
                int groupAddress = baPR2.ReadI32(groupPointer);

                int nextGroupPointer = shortPointerTable[shortPointerTable.Count - (stringGroupCount + 2) + (i1 + 1)];
                int nextGroupAddress = baPR2.ReadI32(nextGroupPointer);
                if (i1 >= (stringGroupCount - 1))
                {
                    nextGroupPointer = shortPointerTable[shortPointerTable.Count - 1];
                    nextGroupAddress = baPR2.ReadI32(nextGroupPointer);
                }

                while (groupAddress < nextGroupAddress)
                {
                    int stringPointer = baPR2.ReadI32(groupAddress);
                    string text = baPR2.ReadStringA(-1, stringPointer);
                    unitxt.StringGroups[i1].entries.Add(text);

                    groupAddress += 4;
                }
                unitxt.StringGroups[i1].count = unitxt.StringGroups[i1].entries.Count;
            }

            string jsonText = Json.Serialize(unitxt, true);
            File.WriteAllText(Path.ChangeExtension(filename, ".json"), jsonText);
        }
    }
}
