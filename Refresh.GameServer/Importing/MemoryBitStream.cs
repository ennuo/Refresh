using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Refresh.GameServer.Importing;

public class MemoryBitStream(byte[] data)
{
    /// <summary>
    ///     The number of bits in the stream.
    /// </summary>
    public int Length { get; } = data.Length * 8;
    
    /// <summary>
    ///     The current bit position in the stream.
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    ///     The current byte position in the stream.
    /// </summary>
    public int BytePosition => this.Position >> 3;
    
    /// <summary>
    ///     The number of bytes in the stream.
    /// </summary>
    public int ByteLength { get; } = data.Length;

    /// <summary>
    ///     The number of bits left in the stream.
    /// </summary>
    public int BitsRemaining => this.Length - this.Position;
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBit()
    {
        byte b = (byte)(data[this.Position >> 3] & 1 << (this.Position & 7));
        this.Position += 1;
        return b != 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        byte value = default;
        const int bits = sizeof(byte) * 8;
        
        if (this.Position % 8 == 0)
        {
            value = data[this.Position >> 3];
            this.Position += 8;
            return value;
        }
        
        for (int i = 0; i < bits; ++i)
        {
            bool b = this.ReadBit();
            value |= (byte)((b ? 1 : 0) << (i & 7));
        }
        
        return value;
    }

    public int ReadInt32()
    {
        int i = 0;
        int value = 0;
        while (true)
        {
            byte b = this.ReadByte();
            value |= (int)((b & 0x7f) << 7 * i);
            if ((b & 0x80) == 0) break;
            ++i;
        }
        
        return value >> 1 ^ -(value & 1);
    }
    
    public ushort ReadUInt16()
    {
        int i = 0;
        ushort value = 0;
        while (true)
        {
            byte b = this.ReadByte();
            value |= (ushort)((b & 0x7f) << 7 * i);
            if ((b & 0x80) == 0) break;
            ++i;
        }
        
        return value;
    }
    
    public uint ReadUInt32()
    {
        int i = 0;
        uint value = 0;
        while (true)
        {
            byte b = this.ReadByte();
            value |= (uint)((b & 0x7f) << 7 * i);
            if ((b & 0x80) == 0) break;
            ++i;
        }
        
        return value;
    }
    
    public ulong ReadUInt64()
    {
        int i = 0;
        ulong value = 0;
        while (true)
        {
            byte b = this.ReadByte();
            value |= (ulong)(b & 0x7f) << 7 * i;
            if ((b & 0x80) == 0) break;
            ++i;
        }
        
        return value;
    }

    public float ReadSingle()
    {
        // This is pretty dumb, you could probably just do a standard bitwise
        // trick to read a 32-bit integer, but lazy, optimise later.
        Span<byte> buf = stackalloc byte[sizeof(float)];
        for (int i = 0; i < buf.Length; ++i)
            buf[i] = this.ReadByte();
        return BinaryPrimitives.ReadSingleBigEndian(buf);
    }
    
    public string ReadString()
    {
        StringBuilder result = new();
        while (true)
        {
            char c = (char)this.ReadByte();
            if (c == 0) break;
            result.Append(c);
        }
        
        return result.ToString();
    }

    public void ReadExactly(Span<byte> span)
    {
        if (this.Position % 8 == 0)
        {
            data.AsSpan(this.Position >> 3, span.Length).CopyTo(span);
            this.Position += span.Length * 8;
            return;
        }
        
        for (int i = 0; i < span.Length; ++i)
            span[i] = this.ReadByte();
    }
}