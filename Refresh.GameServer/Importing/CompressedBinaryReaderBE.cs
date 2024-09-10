using System.Buffers.Binary;
using System.Text;

namespace Refresh.GameServer.Importing;

public class CompressedBinaryReaderBE : BinaryReader
{
    public CompressedBinaryReaderBE(Stream input) : base(input)
    {
        
    }
    
    public override short ReadInt16()
    {
        int i = 0;
        short value = 0;
        while (true)
        {
            byte b = this.ReadByte();
            value |= (short)((b & 0x7f) << 7 * i);
            if ((b & 0x80) == 0) break;
            ++i;
        }
        
        return value;
    }
    
    public override int ReadInt32()
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
    
    public override ushort ReadUInt16()
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
    
    public override uint ReadUInt32()
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
    
    public override long ReadInt64()
    {
        int i = 0;
        long value = 0;
        while (true)
        {
            byte b = this.ReadByte();
            value |= (long)(b & 0x7f) << 7 * i;
            if ((b & 0x80) == 0) break;
            ++i;
        }
        
        return value >> 1 ^ -(value & 1);;
    }
    
    public override ulong ReadUInt64()
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

    public override float ReadSingle()
    {
        Span<byte> buf = stackalloc byte[sizeof(float)];
        this.BaseStream.ReadExactly(buf);
        return BinaryPrimitives.ReadSingleBigEndian(buf);
    }

    public string ReadTerminatedString()
    {
        StringBuilder result = new StringBuilder();
        while (true)
        {
            char c = (char)this.ReadByte();
            if (c == 0) break;
            result.Append(c);
        }
        
        return result.ToString();
    }
}