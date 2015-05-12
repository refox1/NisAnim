﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;

using OpenTK;

using NisAnim.IO;
using NisAnim.OpenGL;

namespace NisAnim.Conversion
{
    [DisplayName("Txf List Entry")]
    public class ObfTxfListEntry
    {
        [DisplayName("Parent")]
        public ObfTxfList Parent { get; private set; }

        [DisplayName("Unknown 0x00")]
        public uint Unknown0x00 { get; private set; }
        [DisplayName("Txf Data")]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public Txf TxfData { get; private set; }

        public ObfTxfListEntry(ObfTxfList parent, EndianBinaryReader reader)
        {
            Parent = parent;

            Unknown0x00 = reader.ReadUInt32();
            TxfData = new Txf(reader);
        }
    }

    [DisplayName("Txf List")]
    public class ObfTxfList
    {
        [DisplayName("Parent")]
        public Obf Parent { get; private set; }

        [DisplayName("Number of Textures")]
        public uint NumTextures { get; private set; }
        [DisplayName("Texture Offsets")]
        public uint[] TextureOffsets { get; private set; }

        [DisplayName("Textures")]
        public ObfTxfListEntry[] Textures { get; private set; }

        public ObfTxfList(Obf parent, EndianBinaryReader reader)
        {
            Parent = parent;

            long position = reader.BaseStream.Position;

            NumTextures = reader.ReadUInt32();

            TextureOffsets = new uint[NumTextures];
            for (int i = 0; i < TextureOffsets.Length; i++) TextureOffsets[i] = reader.ReadUInt32();

            Textures = new ObfTxfListEntry[NumTextures];
            for (int i = 0; i < Textures.Length; i++)
            {
                reader.BaseStream.Seek(position + TextureOffsets[i], SeekOrigin.Begin);
                Textures[i] = new ObfTxfListEntry(this, reader);
            }
        }
    }

    [DisplayName("Vertex")]
    public class ObfVertex
    {
        public const int Size = 0x3C;

        [DisplayName("Parent")]
        public ObfPrimitiveListEntry Parent { get; private set; }

        [DisplayName("Position")]
        public Vector3 Position { get; private set; }
        [DisplayName("Unknown Float 0x0C")]
        public float UnknownFloat0x0C { get; private set; }
        [DisplayName("Unknown Float 0x10")]
        public float UnknownFloat0x10 { get; private set; }
        [DisplayName("Unknown Float 0x14")]
        public float UnknownFloat0x14 { get; private set; }
        [DisplayName("Unknown Float 0x18")]
        public float UnknownFloat0x18 { get; private set; }
        [DisplayName("Unknown Float 0x1C")]
        public float UnknownFloat0x1C { get; private set; }
        [DisplayName("Unknown Float 0x20")]
        public float UnknownFloat0x20 { get; private set; }
        [DisplayName("Unknown Float 0x24")]
        public float UnknownFloat0x24 { get; private set; }
        [DisplayName("Unknown Float 0x28")]
        public float UnknownFloat0x28 { get; private set; }
        [DisplayName("Unknown Float 0x2C")]
        public float UnknownFloat0x2C { get; private set; }
        [DisplayName("Unknown Float 0x30")]
        public float UnknownFloat0x30 { get; private set; }
        [DisplayName("Texture Coords")]
        public Vector2 TextureCoord { get; private set; }

        public ObfVertex(ObfPrimitiveListEntry parent, EndianBinaryReader reader)
        {
            Parent = parent;

            Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            UnknownFloat0x0C = reader.ReadSingle();
            UnknownFloat0x10 = reader.ReadSingle();
            UnknownFloat0x14 = reader.ReadSingle();
            UnknownFloat0x18 = reader.ReadSingle();
            UnknownFloat0x1C = reader.ReadSingle();
            UnknownFloat0x20 = reader.ReadSingle();
            UnknownFloat0x24 = reader.ReadSingle();
            UnknownFloat0x28 = reader.ReadSingle();
            UnknownFloat0x2C = reader.ReadSingle();
            UnknownFloat0x30 = reader.ReadSingle();
            TextureCoord = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }
    }

    [DisplayName("Primitive List Entry")]
    public class ObfPrimitiveListEntry
    {
        [DisplayName("Parent")]
        public ObfPrimitiveList Parent { get; private set; }

        [DisplayName("Number of Vertex Indices")]
        public uint NumVertexIndices { get; private set; }
        [DisplayName("Texture ID")]
        public uint TextureID { get; private set; }
        [DisplayName("Unknown 0x08")]
        public int Unknown0x08 { get; private set; }
        [DisplayName("Unknown Float 0x0C")]
        public float UnknownFloat0x0C { get; private set; }
        [DisplayName("Unknown Float 0x10")]
        public float UnknownFloat0x10 { get; private set; }
        [DisplayName("Unknown Float 0x14")]
        public float UnknownFloat0x14 { get; private set; }
        [DisplayName("Unknown Float 0x18")]
        public float UnknownFloat0x18 { get; private set; }
        [DisplayName("Unknown Float 0x1C")]
        public float UnknownFloat0x1C { get; private set; }
        [DisplayName("Unknown 0x20")]
        public ushort Unknown0x20 { get; private set; }
        [DisplayName("Unknown 0x22")]
        public ushort Unknown0x22 { get; private set; }
        [DisplayName("Unknown 0x24")]
        public ushort Unknown0x24 { get; private set; }
        [DisplayName("Unknown 0x26")]
        public ushort Unknown0x26 { get; private set; }
        [DisplayName("Unknown 0x28")]
        public ushort Unknown0x28 { get; private set; }
        [DisplayName("Unknown 0x2A")]
        public ushort Unknown0x2A { get; private set; }
        [DisplayName("Unknown 0x2C")]
        public int Unknown0x2C { get; private set; }
        [DisplayName("Vertex Indices")]
        public uint[] VertexIndices { get; private set; }

        [DisplayName("Vertices")]
        public ObfVertex[] Vertices { get; set; }

        public ObfPrimitiveListEntry(ObfPrimitiveList parent, EndianBinaryReader reader)
        {
            Parent = parent;

            NumVertexIndices = reader.ReadUInt32();
            TextureID = reader.ReadUInt32();
            Unknown0x08 = reader.ReadInt32();
            UnknownFloat0x0C = reader.ReadSingle();
            UnknownFloat0x10 = reader.ReadSingle();
            UnknownFloat0x14 = reader.ReadSingle();
            UnknownFloat0x18 = reader.ReadSingle();
            UnknownFloat0x1C = reader.ReadSingle();
            Unknown0x20 = reader.ReadUInt16();
            Unknown0x22 = reader.ReadUInt16();
            Unknown0x24 = reader.ReadUInt16();
            Unknown0x26 = reader.ReadUInt16();
            Unknown0x28 = reader.ReadUInt16();
            Unknown0x2A = reader.ReadUInt16();
            Unknown0x2C = reader.ReadInt32();

            VertexIndices = new uint[NumVertexIndices];
            for (int i = 0; i < VertexIndices.Length; i++) VertexIndices[i] = reader.ReadUInt32();
        }

        public string PrepareRender(GLHelper glHelper)
        {
            string objectName = string.Format("{0}_hash-{1}", this.GetType().Name, this.GetHashCode());

            List<GLVertex> glVertices = new List<GLVertex>();
            List<uint> glIndices = new List<uint>();

            for (int j = 0; j < Vertices.Length; j++)
            {
                ObfVertex vertex = Vertices[j];

                glVertices.Add(new GLVertex(
                    vertex.Position,
                    OpenTK.Vector3.Zero,
                    OpenTK.Graphics.Color4.White,
                    vertex.TextureCoord));

                glIndices.Add((uint)j);
            }
            glHelper.Buffers.AddVertices(objectName, glVertices.ToArray());
            glHelper.Buffers.AddIndices(objectName, glIndices.ToArray(), OpenTK.Graphics.OpenGL.PrimitiveType.Triangles);

            glHelper.Textures.AddTexture(objectName, Parent.Parent.TxfList.Textures[TextureID].TxfData.Images.FirstOrDefault().Bitmap);

            return objectName;
        }
    }

    [DisplayName("Primitive List")]
    public class ObfPrimitiveList
    {
        [DisplayName("Parent")]
        public Obf Parent { get; private set; }

        [DisplayName("Number of Primitives")]
        public uint NumPrimitives { get; private set; }
        [DisplayName("Primitive Offset")]
        public uint[] PrimitiveOffsets { get; private set; }

        [DisplayName("Primitives")]
        public ObfPrimitiveListEntry[] Primitives { get; private set; }

        public ObfPrimitiveList(Obf parent, EndianBinaryReader reader)
        {
            Parent = parent;

            long position = reader.BaseStream.Position;

            NumPrimitives = reader.ReadUInt32();

            PrimitiveOffsets = new uint[NumPrimitives];
            for (int i = 0; i < PrimitiveOffsets.Length; i++) PrimitiveOffsets[i] = reader.ReadUInt32();

            Primitives = new ObfPrimitiveListEntry[NumPrimitives];
            for (int i = 0; i < Primitives.Length; i++)
            {
                reader.BaseStream.Seek(position + PrimitiveOffsets[i], SeekOrigin.Begin);
                Primitives[i] = new ObfPrimitiveListEntry(this, reader);
            }
        }
    }

    [DisplayName("Obf File")]
    public class Obf : BaseFile
    {
        public const string FileNamePattern = "(.*?)\\.(obf)$";

        [DisplayName("File Size")]
        public uint FileSize { get; private set; }
        [DisplayName("Unknown 0x04")]
        public uint Unknown0x04 { get; private set; }
        [DisplayName("Unknown Offset 0x08")]
        public uint UnknownOffset0x08 { get; private set; }
        [DisplayName("Unknown Offset 0x0C")]
        public uint UnknownOffset0x0C { get; private set; }
        [DisplayName("Unknown Offset 0x10")]
        public uint UnknownOffset0x10 { get; private set; }
        [DisplayName("Unknown Offset 0x14")]
        public uint UnknownOffset0x14 { get; private set; }
        [DisplayName("Unknown Offset 0x18")]
        public uint UnknownOffset0x18 { get; private set; }
        [DisplayName("Unknown Offset 0x1C")]
        public uint UnknownOffset0x1C { get; private set; }
        [DisplayName("Unknown Offset 0x20")]
        public uint UnknownOffset0x20 { get; private set; }
        [DisplayName("Primitive List Offset?")]
        public uint PrimitiveListOffset { get; private set; }
        [DisplayName("Vertex Data Offset?")]
        public uint VertexDataOffset { get; private set; }
        [DisplayName("Txf List Offset")]
        public uint TxfListOffset { get; private set; }

        [DisplayName("Primitive List")]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public ObfPrimitiveList PrimitiveList { get; private set; }

        [DisplayName("Txf List")]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public ObfTxfList TxfList { get; private set; }

        bool disposed = false;

        public Obf(string filePath)
            : base(filePath)
        {
            using (EndianBinaryReader reader = new EndianBinaryReader(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Endian.BigEndian))
            {
                FileSize = reader.ReadUInt32();
                Unknown0x04 = reader.ReadUInt32();
                UnknownOffset0x08 = reader.ReadUInt32();
                UnknownOffset0x0C = reader.ReadUInt32();
                UnknownOffset0x10 = reader.ReadUInt32();
                UnknownOffset0x14 = reader.ReadUInt32();
                UnknownOffset0x18 = reader.ReadUInt32();
                UnknownOffset0x1C = reader.ReadUInt32();
                UnknownOffset0x20 = reader.ReadUInt32();
                PrimitiveListOffset = reader.ReadUInt32();
                VertexDataOffset = reader.ReadUInt32();
                TxfListOffset = reader.ReadUInt32();

                reader.BaseStream.Seek(TxfListOffset, SeekOrigin.Begin);
                TxfList = new ObfTxfList(this, reader);

                reader.BaseStream.Seek(PrimitiveListOffset, SeekOrigin.Begin);
                PrimitiveList = new ObfPrimitiveList(this, reader);

                for (int i = 0; i < PrimitiveList.Primitives.Length; i++)
                {
                    ObfPrimitiveListEntry primitiveListEntry = PrimitiveList.Primitives[i];

                    primitiveListEntry.Vertices = new ObfVertex[primitiveListEntry.NumVertexIndices];
                    for (int j = 0; j < primitiveListEntry.Vertices.Length; j++)
                    {
                        reader.BaseStream.Seek(VertexDataOffset + (ObfVertex.Size * primitiveListEntry.VertexIndices[j]), SeekOrigin.Begin);
                        primitiveListEntry.Vertices[j] = new ObfVertex(primitiveListEntry, reader);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    for (int i = 0; i < TxfList.Textures.Length; i++)
                        TxfList.Textures[i].TxfData.Dispose();
                }

                this.disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
