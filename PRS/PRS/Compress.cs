namespace PSO.PRS
{
    class PRSCompression
    {
        internal static void Compress(Context ctx)
        {
            while (ctx.src_pos < ctx.src.Length)
            {
                int offset = 0;
                int length = 0;
                int mlen = 0;

                for (int y = (ctx.src_pos - 1); (y >= 0) && (y >= (ctx.src_pos - 0x1FF0)) && mlen < 256; y--)
                {
                    mlen = 1;
                    if (ctx.src[y] == ctx.src[ctx.src_pos])
                    {
                        do
                        {
                            mlen++;
                        } while (mlen <= 256 &&
                            ctx.src_pos + mlen <= ctx.src.Length &&
                            ctx.src[y + mlen - 1] == ctx.src[ctx.src_pos + mlen - 1]);

                        mlen--;
                        if (((mlen >= 2 && y - ctx.src_pos >= -0x100) || mlen >= 3) && mlen > length)
                        {
                            offset = y - ctx.src_pos;
                            length = mlen;
                        }
                    }
                }
                if (length == 0)
                {
                    SetBit(ctx, 1);
                    CopyLiteral(ctx);
                }
                else
                {
                    CopyBlock(ctx, offset, length);
                    ctx.src_pos += length;
                }
            }

            WriteEOF(ctx);
        }

        internal static void SetBit(Context ctx, int bit)
        {
            if (ctx.bits-- == 0)
            {
                ctx.dst[ctx.flag_pos] = ctx.flag;
                ctx.flag_pos = ctx.dst_pos;
                ctx.dst_pos += 1;

                ctx.flag = 0;
                ctx.bits = 7;
            }
            ctx.flag >>= 1;
            if (bit != 0)
            {
                ctx.flag |= 0x80;
            }
        }

        internal static void CopyLiteral(Context ctx)
        {
            ctx.dst[ctx.dst_pos] = ctx.src[ctx.src_pos];
            ctx.src_pos += 1;
            ctx.dst_pos += 1;
        }
        internal static void CopyBlock(Context ctx, int offset, int length)
        {
            if ((length >= 2) && (length <= 5) && (offset >= -256))
            {
                SetBit(ctx, 0);
                SetBit(ctx, 0);
                SetBit(ctx, (length - 2) & 2);
                SetBit(ctx, (length - 2) & 1);
                WriteLiteral(ctx, (byte)(offset));
            }
            else if (/*length >= 3 &&*/ length <= 9)
            {
                SetBit(ctx, 0);
                SetBit(ctx, 1);
                WriteLiteral(ctx, (byte)(((offset << 3) & 0xF8) | ((length - 2) & 0x07)));
                WriteLiteral(ctx, (byte)(offset >> 5));
            }
            else /*if (length > 9)*/
            {
                //if (length > 256)
                //{
                //    length = 256;
                //}

                SetBit(ctx, 0);
                SetBit(ctx, 1);
                WriteLiteral(ctx, (byte)((offset << 3) & 0xF8));
                WriteLiteral(ctx, (byte)(offset >> 5));
                WriteLiteral(ctx, (byte)(length - 1));
            }
        }

        internal static void WriteLiteral(Context ctx, byte value)
        {
            ctx.dst[ctx.dst_pos] = value;
            ctx.dst_pos += 1;
        }
        internal static void WriteFinalFlags(Context ctx)
        {
            ctx.flag >>= ctx.bits;
            ctx.dst[ctx.flag_pos] = ctx.flag;
        }
        internal static void WriteEOF(Context ctx)
        {
            SetBit(ctx, 0);
            SetBit(ctx, 1);
            WriteFinalFlags(ctx);
            WriteLiteral(ctx, 0);
            WriteLiteral(ctx, 0);
        }
    }
}
