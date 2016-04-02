using MusicBeePlugin;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;


public class ArtworkData
{
    public string type;
    public Stream data;
}

public class Artwork
{
    private static Plugin.PictureLocations GET_ARTWORK_FLAGS = (Plugin.PictureLocations)0xFF;
    private static byte[][] MAGIC_JPEG = { new byte[] { 0xFF, 0xD8, 0xFF, 0xDB }, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 } };
    private static byte[][] MAGIC_GIF = { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 } };
    private static byte[] MAGIC_PNG = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static int MAX_MAGIC_LENGTH = MAGIC_PNG.Length;

    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int memcmp(byte[] buf1, byte[] buf2, UIntPtr n);

    private static string GetImageTypeFromBuffer(byte[] buffer)
    {
        Debug.Assert(buffer != null);

        foreach (var magic in MAGIC_JPEG) {
            if (memcmp(magic, buffer, (UIntPtr)magic.Length) == 0) {
                return "jpeg";
            }
        }

        foreach (var magic in MAGIC_GIF) {
            if (memcmp(magic, buffer, (UIntPtr)magic.Length) == 0) {
                return "gif";
            }
        }


        if (memcmp(MAGIC_PNG, buffer, (UIntPtr)MAGIC_JPEG.Length) == 0) {
            return "png";
        }

        return null;
    }

    private static int swapEndianness(int x)
    {
        unchecked {
            uint val = (uint)x;
            return (int)(((val & 0xFF000000) >> 24)
                       | ((val & 0x00FF0000) >> 8)
                       | ((val & 0x0000FF00) << 8)
                       | ((val & 0x000000FF) << 24));
        }
    }

    [Conditional("DEBUG")]
    private static void ReportFailure(string file, byte[] bytes)
    {
        Debug.WriteLine(String.Format("Could not determine image type of {8} with signature {0} {1} {2} {3} {4} {5} {6} {7}",
            bytes[0].ToString("X2"),
            bytes[1].ToString("X2"),
            bytes[2].ToString("X2"),
            bytes[3].ToString("X2"),
            bytes[4].ToString("X2"),
            bytes[5].ToString("X2"),
            bytes[6].ToString("X2"),
            bytes[7].ToString("X2"),
            file));
    }

    private static ArtworkData GetArtworkFromID3(string filename)
    {
        try {
            FileStream data = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] buffer = null;

            using (BinaryReader reader = new BinaryReader(data)) {
                const int EXTENDED_HEADER = (1 << 6);
                const int APIC_FRAME_ID = 0x43495041;
                const int CLEARED_BITS_MASK = 0xE0FF;
                const int ANSI = 0;

                if (reader.ReadChar() != 'I') return null;
                if (reader.ReadChar() != 'D') return null;
                if (reader.ReadChar() != '3') return null;

                // skip version
                reader.BaseStream.Seek(2, SeekOrigin.Current);

                byte flags = reader.ReadByte();
                int size = swapEndianness(reader.ReadInt32());

                if ((flags & EXTENDED_HEADER) != 0) {
                    int extendedHeaderSize = reader.ReadInt32();
                    reader.BaseStream.Seek(extendedHeaderSize, SeekOrigin.Current);
                }

                uint frameId = reader.ReadUInt32();
                while (frameId != APIC_FRAME_ID && reader.BaseStream.Position < reader.BaseStream.Length) {
                    int chunkSize = swapEndianness(reader.ReadInt32());

                    if (chunkSize < 0) {
                        return null;
                    }

                    ushort frameFlags = reader.ReadUInt16();

                    if ((frameFlags & CLEARED_BITS_MASK) != 0) {
                        return null;
                    }

                    if (reader.BaseStream.Position + chunkSize > reader.BaseStream.Length) {
                        return null;
                    }

                    reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);

                    frameId = reader.ReadUInt32();
                }

                int imageSize = swapEndianness(reader.ReadInt32());

                //skip flags
                reader.BaseStream.Seek(2, SeekOrigin.Current);

                byte encoding = reader.ReadByte();

                for (byte limit = 0, terminator = 0xFF; limit < 64 && terminator != 0; limit++) {
                    terminator = reader.ReadByte();
                }

                // skip picture type
                reader.BaseStream.Seek(1, SeekOrigin.Current);

                if (encoding == ANSI) {
                    for (byte limit = 0, terminator = 0xFF; limit < 64 && terminator != 0; limit++) {
                        terminator = reader.ReadByte();
                    }
                } else {
                    for (short limit = 0, terminator = -1; limit < 64 && terminator != 0; limit++) {
                        terminator = reader.ReadInt16();
                    }
                }

                buffer = reader.ReadBytes(imageSize);
            }

            string type = GetImageTypeFromBuffer(buffer);

            if (type != null) {
                ArtworkData result = new ArtworkData
                {
                    type = type,
                    data = new MemoryStream(buffer)
                };

                return result;
            } else {
                ReportFailure(filename, buffer);
            }
        } catch (Exception) {
            return null;
        }

        return null;
    }

    public static ArtworkData OpenArtworkFile(string file)
    {
        ArtworkData result = null;

        FileInfo info = new FileInfo(file);

        if (info.Exists) {
            FileStream data = info.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

            byte[] bytes = new byte[MAX_MAGIC_LENGTH];
            data.Read(bytes, 0, MAX_MAGIC_LENGTH);
            string type = GetImageTypeFromBuffer(bytes);

            data.Seek(0, SeekOrigin.Begin);

            if (type != null) {
                result = new ArtworkData
                {
                    type = type,
                    data = data
                };
            } else {
                ReportFailure(file, bytes);
            }
        }

        return result;
    }

    public static ArtworkData GetArtworkForTrack(string file)
    {
        ArtworkData result = null;

        string artworkFile = String.Empty;
        byte[] artworkData = { };
        if (Plugin.mbApi.Library_GetArtworkEx(file, 0, true, ref GET_ARTWORK_FLAGS, ref artworkFile, ref artworkData) == true && artworkData != null) {
            string type = GetImageTypeFromBuffer(artworkData);

            if (type != null) {
                result = new ArtworkData
                {
                    type = type,
                    data = new MemoryStream(artworkData)
                };
            } else {
                ReportFailure(file, artworkData);
            }
        } else {
            // Sometimes GetArtworkEx fails...
            foreach (var rawPattern in Plugin.artworkPatterns) {
                string pattern = rawPattern.Replace("<Filename>", Path.GetFileNameWithoutExtension(file));

                // ...if there is embedded artwork, GetArtworkEx does not fail
#if false
                if (pattern == "Embedded") {
                    result = GetArtworkFromID3(file);
                    if (result != null) {
                        break;
                    }
                } 
                else
#endif
                {
                    var matches = Directory.EnumerateFiles(Path.GetDirectoryName(file), pattern, SearchOption.AllDirectories);

                    if (matches.Count() > 10) {
                        // We've hit some parent directory
                        continue;
                    }

                    foreach (var artwork in matches) {
                        if (AudioStream.IsAudioFile(artwork) == false) {
                            result = OpenArtworkFile(artwork);
                            if (result != null) {
                                goto done;
                            }
                        }
                    }
                }
            }
            done:;
        }

        return result;
    }

    public static ArtworkData GetArtwork(string track)
    {
        ArtworkData result = GetArtworkForTrack(track);

        return result;
    }
}