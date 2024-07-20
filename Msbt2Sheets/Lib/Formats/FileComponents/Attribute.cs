﻿using System.Text;

namespace Msbt2Sheets.Lib.Formats.FileComponents;

public class MessageAttribute
{
    public byte[] ByteData;
    public string StringData = "";

    public MessageAttribute() {}

    public MessageAttribute(byte[] byteData)
    {
        ByteData = byteData;
    }
    public MessageAttribute(byte[] byteData, string stringData)
    {
        ByteData = byteData;
        StringData = stringData;
    }

    public List<string> ToStringList(MSBP? msbp)
    {
        List<string> list = new();
        
        if (msbp == null)
        {
            list.Add($"Attributes: {BitConverter.ToString(ByteData)}");
            if (StringData != "")
            {
                list.Add($"StringAttribute: {StringData}");
            }

            return list;
        }

        using (var reader = new BinaryReader(new MemoryStream(ByteData)))
        {
            foreach (var attrInfo in msbp.AttributeInfos)
            {
                if (attrInfo.Offset >= reader.BaseStream.Length)
                {
                    continue;
                }

                reader.BaseStream.Seek(attrInfo.Offset, SeekOrigin.Begin);

                string value = "";

                switch (attrInfo.Type)
                {
                    case ParamType.UInt8:
                        value = reader.ReadByte().ToString();
                        break;
                    case ParamType.UInt16:
                        value = reader.ReadUInt16().ToString();
                        break;
                    case ParamType.UInt32:
                        value = reader.ReadUInt32().ToString();
                        break;
                    case ParamType.Int8:
                        value = ((sbyte)reader.ReadByte()).ToString();
                        break;
                    case ParamType.Int16:
                        value = reader.ReadInt16().ToString();
                        break;
                    case ParamType.Int32:
                        value = reader.ReadInt32().ToString();
                        break;
                    case ParamType.Float:
                        value = reader.ReadSingle().ToString();
                        break;
                    case ParamType.Double:
                        value = reader.ReadDouble().ToString();
                        break;
                    case ParamType.String:
                        //ushort strLength = reader.ReadUInt16();
                        //value = Encoding.Unicode.GetString(reader.ReadBytes(strLength));
                        value = reader.ReadByte().ToString();
                        break;
                    case ParamType.List:
                        byte id = reader.ReadByte();
                        value = attrInfo.List[id];
                        break;
                }
                
                list.Add($"{attrInfo.Name}: {value}");
            }
        }

        return list;
    }
}

public class AttributeInfo
{
    public string Name { get; set; }
    
    public ParamType Type { get; set; }

    public List<string> List = new();
    
    public uint Offset { get; set; }

    public AttributeInfo(string name, byte type, uint offset, List<string> list)
    {
        Name = name;
        Type = (ParamType)type;
        Offset = offset;
        List = list;
    }
}