namespace Libraries.PRS
{
    class Context
    {
        internal int bits;
        internal byte flag;
        internal int flag_pos;

        internal byte[] src;
        internal int src_pos;
        internal byte[] dst;
        internal int dst_pos;

        internal Context(byte[] src)
        {
            this.src = src;
            this.dst = new byte[src.Length];
        }
        internal void Reset()
        {
            src_pos = 0;
            dst_pos = 0;

            flag = 0;
            bits = 0;
        }
    }
}
