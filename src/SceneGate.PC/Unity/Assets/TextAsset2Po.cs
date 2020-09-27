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
namespace SceneGate.PC.Unity.Assets
{
    using System;
    using Yarhl.IO;
    using Yarhl.FileFormat;
    using Yarhl.Media.Text;

    public class TextAsset2Po : IConverter<IBinary, Po>
    {
        public Po Convert(IBinary source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using var stream = GetTextStream(source.Stream);
            var reader = new TextReader(stream, "utf-8");

            Po po = new Po {
                Header = new PoHeader("Among Us", "SceneGate", "en"),
            };

            while (!stream.EndOfStream) {
                string line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }

                string[] segments = line.Split(new[] { ',' }, 2);
                if (segments.Length != 2) {
                    throw new FormatException($"Invalid line: {line}");
                }

                var entry = new PoEntry {
                    Context = segments[0],
                    Original = segments[1],
                    Flags = "c-sharp-format"
                };
                po.Add(entry);
            }

            return po;
        }

        DataStream GetTextStream(DataStream stream)
        {
            var reader = new DataReader(stream);

            // Skip the file name
            stream.Position = 0;
            int nameLength = reader.ReadInt32();
            reader.ReadString(nameLength);
            reader.SkipPadding(4);

            uint dataSize = reader.ReadUInt32();

            // BMF fonts are reported as TextAsset but they are binary files
            // without text.
            bool isFont = reader.ReadString(3) == "BMF";
            stream.Position -= 3;
            if (isFont)
            {
                throw new FormatException("Invalid format: font BMF");
            }

            return new DataStream(stream, stream.Position, dataSize);
        }
    }
}