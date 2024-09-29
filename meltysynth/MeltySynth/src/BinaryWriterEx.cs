using System.IO;
using System.Text;
using System;
internal static class BinaryWriterEx
{
    public static void WriteFourCC(this BinaryWriter writer, string fourCC)
    {
        if (fourCC.Length != 4)
        {
            throw new ArgumentException("FourCC must be exactly 4 characters long.");
        }

        var data = Encoding.ASCII.GetBytes(fourCC);

        for (var i = 0; i < data.Length; i++)
        {
            var value = data[i];
            if (!(32 <= value && value <= 126))
            {
                data[i] = (byte)'?';
            }
        }

        writer.Write(data);
    }

    public static void WriteFixedLengthString(this BinaryWriter writer, string value, int length)
    {
        var data = new byte[length];
        var stringBytes = Encoding.ASCII.GetBytes(value);

        var writeLength = Math.Min(stringBytes.Length, length);

        Array.Copy(stringBytes, 0, data, 0, writeLength);

        writer.Write(data);
    }

    public static void WriteInt16BigEndian(this BinaryWriter writer, short value)
    {
        var b1 = (byte)((value >> 8) & 0xFF);
        var b2 = (byte)(value & 0xFF);
        writer.Write(b1);
        writer.Write(b2);
    }

    public static void WriteInt32BigEndian(this BinaryWriter writer, int value)
    {
        var b1 = (byte)((value >> 24) & 0xFF);
        var b2 = (byte)((value >> 16) & 0xFF);
        var b3 = (byte)((value >> 8) & 0xFF);
        var b4 = (byte)(value & 0xFF);
        writer.Write(b1);
        writer.Write(b2);
        writer.Write(b3);
        writer.Write(b4);
    }

    public static void WriteIntVariableLength(this BinaryWriter writer, int value)
    {
        if (value < 0 || value > 0x0FFFFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "The value must be between 0 and 0x0FFFFFFF.");
        }

        var buffer = new byte[4];
        int index = 3;

        buffer[index] = (byte)(value & 0x7F);
        while (value > 0x7F)
        {
            value >>= 7;
            index--;
            buffer[index] = (byte)((value & 0x7F) | 0x80);
        }

        for (; index < 4; index++)
        {
            writer.Write(buffer[index]);
        }
    }
}
