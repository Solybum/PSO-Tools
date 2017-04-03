using System;

namespace Libraries.PRS
{
    /// <summary>
    /// PRS library
    /// </summary>
    public class PRS
    {
        /// <summary>
        /// Compress a byte array and return the processed data in a new byte array
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Compress(byte[] data)
        {
            Context ctx = new Context(data);

            PRSCompression.Compress(ctx);
            Array.Resize(ref ctx.dst, ctx.dst_pos);

            return ctx.dst;
        }
        
        /// <summary>
        /// Decompress a byte array and return the processed data in a new byte array
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] data)
        {
            Context ctx = new Context(data);

            int decompressed_size = PRSDecompression.Decompress(ctx, true);
            Array.Resize(ref ctx.dst, decompressed_size);
            ctx.Reset();
            PRSDecompression.Decompress(ctx, false);

            return ctx.dst;
        }
    }
}
