// Copyright (c) 2020 SceneGate

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
namespace SceneGate.PC.Unity
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// https://github.com/Perfare/AssetStudio/blob/master/AssetStudio/SerializedFile.cs
    /// </remarks>
    public class Binary2UnityAssetsBundle : IConverter<BinaryFormat, NodeContainerFormat>
    {
        public NodeContainerFormat Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var container = new NodeContainerFormat();

            AssetsBundleInfo info = ReadHeader(source.Stream);
            var infoNode = new Node("bundle_info", info);
            container.Root.Add(infoNode);

            var reader = new DataReader(source.Stream) {
                Endianness = info.IsBigEndian ? EndiannessMode.BigEndian : EndiannessMode.LittleEndian,
            };

            var types = ReadTypes(reader);
            ReadNodes(reader, info, container.Root, types);

            uint scriptCount = reader.ReadUInt32();
            for (int i = 0; i < scriptCount; i++) {
                uint fileIndex = reader.ReadUInt32();
                ulong fileId = reader.ReadUInt64();
            }

            uint externalCount = reader.ReadUInt32();
            for (int i = 0; i < externalCount; i++) {
                reader.ReadString(); // temp empty
                reader.ReadBytes(0x10); // guid
                reader.ReadUInt32(); // type
                string filePath = reader.ReadString();
            }

            var refTypes = ReadTypes(reader);
            info.UserInfo = reader.ReadString(Encoding.UTF8);

            return container;
        }

        private AssetsBundleInfo ReadHeader(DataStream stream)
        {
            // Header at 0, then skip metadata size and file size
            stream.Position = 8;
            var reader = new DataReader(stream) {
                Endianness = EndiannessMode.BigEndian,
            };

            var info = new AssetsBundleInfo();
            info.Version = reader.ReadInt32();
            info.DataOffset = reader.ReadUInt32();
            info.IsBigEndian = reader.ReadUInt32() != 0;
            info.UnityVersion = reader.ReadString();
            info.TargetPlatform = reader.ReadByte();

            return info;
        }

        private List<AssetType> ReadTypes(DataReader reader)
        {
            bool hasTypeTree = reader.ReadUInt32() != 0;
            int typeCount = reader.ReadInt32();
            var types = new List<AssetType>(typeCount);
            for (int i = 0; i < typeCount; i++) {
                var id = (AssetTypeId)reader.ReadUInt32();
                var type = new AssetType(id) {
                    IsStripped = reader.ReadByte() != 0,
                    ScriptIndex = reader.ReadUInt16(),
                };

                if (id == AssetTypeId.MonoBehaviour) {
                    type.ScriptHash = reader.ReadBytes(0x10);
                }

                type.Hash = reader.ReadBytes(0x10);
                types.Add(type);

                if (hasTypeTree) {
                    throw new NotSupportedException();
                }
            }

            return types;
        }

        private void ReadNodes(DataReader reader, AssetsBundleInfo info, Node root, IList<AssetType> types)
        {
            uint objectCount = reader.ReadUInt32();
            reader.SkipPadding(4);
            for (int i = 0; i < objectCount; i++) {
                ulong pathId = reader.ReadUInt64();
                long offset = reader.ReadUInt32() + info.DataOffset;
                uint length = reader.ReadUInt32();
                int typeIndex = reader.ReadInt32();
                var type = (typeIndex < types.Count) ? types[typeIndex] : null;

                var data = new DataStream(reader.Stream, offset, length);
                var asset = new Asset(data, type);

                // Try to get the name, usually at the beginning of the file
                string name = $"asset_{pathId}";
                if (type.Kind == AssetTypeId.TextAsset && type.Kind == AssetTypeId.Texture) {
                    reader.Stream.PushToPosition(offset);
                    int nameLength = reader.ReadInt32();
                    name = (nameLength > 0)
                        ? reader.ReadString(nameLength, Encoding.UTF8)
                        : name;
                    reader.Stream.PopPosition();
                }

                var node = new Node(name, asset);
                string folder = GetFolderName(type?.Kind ?? AssetTypeId.Unknown);
                NodeFactory.CreateContainersForChild(root, folder, node);
            }
        }

        private string GetFolderName(AssetTypeId typeId) => typeId.ToString();
    }
}
