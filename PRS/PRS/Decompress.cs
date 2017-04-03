namespace Libraries.PRS
{
    class PRSDecompression
    {
        internal static int Decompress(Context ctx, bool size_only)
        {
            bool copy = !size_only;

            while (true)
            {
                if (ReadBit(ctx) == 1)
                {
                    CopyByte(ctx, copy);
                }
                else
                {
                    int length;
                    int offset;

                    if (ReadBit(ctx) == 1)
                    {
                        offset = ReadShort(ctx);
                        if (offset == 0)
                        {
                            break;
                        }

                        length = offset & 7;
                        offset = (offset >> 3) - 8192;
                        if (length == 0)
                        {
                            length = ReadByte(ctx) + 1;
                        }
                        else
                        {
                            length += 2;
                        }
                    }
                    else
                    {
                        length = ((ReadBit(ctx) << 1) | ReadBit(ctx)) + 2;
                        offset = ReadByte(ctx) - 256;
                    }

                    while (length > 0)
                    {
                        CopyByteAt(ctx, offset, copy);
                        length -= 1;
                    }
                }
            }
            return ctx.dst_pos;
        }

        internal static int ReadBit(Context ctx)
        {
            if (ctx.bits == 0)
            {
                ctx.flag = ReadByte(ctx);
                ctx.bits = 8;
            }

            int flag = ctx.flag & 1;
            ctx.flag >>= 1;
            ctx.bits -= 1;
            return flag;
        }
        internal static byte ReadByte(Context ctx)
        {
            int result;
            result = ctx.src[ctx.src_pos];
            ctx.src_pos += 1;
            return (byte)result;
        }
        internal static ushort ReadShort(Context ctx)
        {
            int result;
            result = ctx.src[ctx.src_pos] + (ctx.src[ctx.src_pos + 1] << 8);
            ctx.src_pos += 2;
            return (ushort)result;
        }

        internal static void CopyByte(Context ctx, bool copy)
        {
            if (copy)
            {
                ctx.dst[ctx.dst_pos] = ctx.src[ctx.src_pos];
            }
            ctx.src_pos += 1;
            ctx.dst_pos += 1;
        }
        internal static void CopyByteAt(Context ctx, int offset, bool copy)
        {
            if (copy)
            {
                ctx.dst[ctx.dst_pos] = ctx.dst[ctx.dst_pos + offset];
            }
            ctx.dst_pos += 1;
        }
    }
}
