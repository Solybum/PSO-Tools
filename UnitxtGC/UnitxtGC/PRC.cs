using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitxtGC
{
    public class PRC
    {
        private uint[] keys;
        private uint position;

        public PRC(uint key)
        {
            keys = new uint[56];

            CreateKeys(key);
        }

        private void CreateKeys(uint key)
        {
            uint index, key2;

            key2 = 1;
            keys[55] = key;

            for (int i1 = 0x15; i1 <= 0x46E; i1 += 0x15)
            {
                index = (uint)(i1 % 55);
                key -= key2;
                keys[index] = key2;
                key2 = key;
                key = keys[index];
            }

            MixKeys();
            MixKeys();
            MixKeys();
            MixKeys();
            position = 56;
        }
        private void MixKeys()
        {
            for (int i1 = 0x18, i2 = 0x01; i1 > 0; i1--, i2++)
            {
                keys[i2] -= keys[i2 + 0x1F];
            }
            for (int i1 = 0x1F, i2 = 0x19; i1 > 0; i1--, i2++)
            {
                keys[i2] -= keys[i2 - 0x18];
            }
        }

        public void CryptData(byte[] data, int index, int length, bool big_endian = false)
        {
            length += index;
            length = (int)((length + 3) & 0xFFFFFFFC);

            for (int i1 = index; i1 < length; i1 += 4)
            {
                CryptU32(data, i1, big_endian);
            }
        }
        private void CryptU32(byte[] data, int offset, bool big_endian)
        {
            if (position == 56)
            {
                MixKeys();
                position = 1;
            }

            if (big_endian)
            {
                data[offset + 0] ^= (byte)(keys[position] >> 24);
                data[offset + 1] ^= (byte)(keys[position] >> 16);
                data[offset + 2] ^= (byte)(keys[position] >> 8);
                data[offset + 3] ^= (byte)(keys[position] >> 0);
            }
            else
            {
                data[offset + 0] ^= (byte)(keys[position] >> 0);
                data[offset + 1] ^= (byte)(keys[position] >> 8);
                data[offset + 2] ^= (byte)(keys[position] >> 16);
                data[offset + 3] ^= (byte)(keys[position] >> 24);
            }
            position++;
        }
    }
}
