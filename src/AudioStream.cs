using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MusicBeePlugin
{

    public static class AudioStream
    {
        public class TranscodeOptions
        {
            public bool usePCM = true;
            public bool useMusicBeeSettings = false;
            public bool enableDSP = false;
            public Plugin.ReplayGainMode replayGainMode = Plugin.ReplayGainMode.Off;
            public List<Plugin.FileCodec> formats = null;

            internal static TranscodeOptions WithDefaultTranscodeFormats()
            {
                TranscodeOptions result = new TranscodeOptions { };

                result.formats = new List<Plugin.FileCodec> {
                    Plugin.FileCodec.Unknown,
                    Plugin.FileCodec.Flac,
                    Plugin.FileCodec.Ogg,
                    Plugin.FileCodec.WavPack,
                    Plugin.FileCodec.Wma,
                    Plugin.FileCodec.Tak,
                    Plugin.FileCodec.Mpc,
                    Plugin.FileCodec.Asx,
                    Plugin.FileCodec.Pcm,
                    Plugin.FileCodec.Opus,
                    Plugin.FileCodec.Spx,
                    Plugin.FileCodec.Dsd,
                    Plugin.FileCodec.AacNoContainer
                };

                return result;
            }
        }

        public class BASSStream : Stream
        {
            private const int BASS_ERROR_CODE_OK = 0;
            private const int BASS_POS_BYTE = 0;
            private const int BASS_DATA_FLOAT = 256;

            [StructLayout(LayoutKind.Sequential)]
            private struct ChannelInfo
            {
                public int freq;
                public int chans;
                public int flags;
                public int ctype;
                public int origres;
                public int plugin;
                public int sample;
                public IntPtr filename;
            }

            [DllImport("bass.dll")]
            private extern static int BASS_ChannelGetInfo(int handle, out ChannelInfo info);

            [DllImport("bass.dll")]
            private extern static int BASS_ChannelGetData(int handle, [In, Out] byte[] buffer, int length);

            [DllImport("bass.dll", EntryPoint = "BASS_ChannelGetData")]
            private extern static int BASS_ChannelGetData_Float(int handle, [In, Out] float[] buffer, int length);

            [DllImport("bass.dll")]
            private extern static int BASS_ChannelGetLength(int handle, int mode);

            [DllImport("bass.dll")]
            private extern static bool BASS_ChannelSetPosition(int handle, long position, int mode);

            [DllImport("bass.dll")]
            private extern static int BASS_ErrorGetCode();

            [DllImport("bass.dll")]
            private extern static bool BASS_StreamFree(int handle);

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                Debug.Assert(origin == SeekOrigin.Begin);

                long offsetIntoBassStream = convertFloatToPCM ? (offset * 2) : offset;

                if (BASS_ChannelSetPosition(handle, offsetIntoBassStream - WAV_HEADER_SIZE, BASS_POS_BYTE)) {
                    position = offset;
                }

                return position;
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRead = 0;

                if (offset > WAV_HEADER_SIZE) {
                    Seek(offset - WAV_HEADER_SIZE, SeekOrigin.Begin);
                }

                if (convertFloatToPCM) {
                    // With PCM encoding, 4 bytes of the underlying 32-bit stream are read
                    // for every 2 bytes of this 16-bit stream

                    if (position < WAV_HEADER_SIZE) {
                        bytesRead = WriteWAVHeader(buffer, (int)position);
                        offset += bytesRead;
                        count -= bytesRead;
                    }

                    int floatsToRead = count / 2;

                    if (pcmBuffer.Length < floatsToRead) {
                        pcmBuffer = new float[floatsToRead];
                    }

                    int bassBytesRead = BASS_ChannelGetData_Float(handle, pcmBuffer, count * 2);

                    for (int i = 0; i < (bassBytesRead / 4); i++) {
                        short value = (short)(pcmBuffer[i] * 32767f);

                        ushort uvalue;
                        unchecked { uvalue = (ushort)value; }

                        buffer[(i * 2) + offset] = (byte)(value);
                        buffer[(i * 2) + offset + 1] = (byte)(value >> 8);
                    }

                    bytesRead += (bassBytesRead / 2);
                } else {
                    if (position < WAV_HEADER_SIZE) {
                        bytesRead = WriteWAVHeader(buffer, (int)position);

                        int countWithoutHeader = count - WAV_HEADER_SIZE;
                        byte[] tempBuffer = new byte[countWithoutHeader];

                        int bassBytesRead = BASS_ChannelGetData(handle, tempBuffer, countWithoutHeader);
                        Buffer.BlockCopy(tempBuffer, 0, buffer, WAV_HEADER_SIZE, bassBytesRead);
                        bytesRead += bassBytesRead;
                    } else {
                        bytesRead = BASS_ChannelGetData(handle, buffer, count);
                    }
                }

                position += bytesRead;

                return bytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return true; } }
            public override bool CanWrite { get { return false; } }

            public override long Length
            {
                get
                {
                    int audioLength = BASS_ChannelGetLength(handle, BASS_POS_BYTE);

                    if (convertFloatToPCM) {
                        return (audioLength / 2) + WAV_HEADER_SIZE;
                    } else {
                        return audioLength + WAV_HEADER_SIZE;
                    }
                }
            }

            public override long Position
            {
                get { return position; }
                set { Seek(value, SeekOrigin.Begin); }
            }

            protected override void Dispose(bool disposing)
            {
                if (handle == 0) {
                    return;
                }

                if (disposing) {
                    BASS_StreamFree(handle);
                    handle = 0;
                }
            }

            public bool FloatingPointBassStream
            {
                get { return (info.flags & BASS_DATA_FLOAT) == BASS_DATA_FLOAT; }
            }

            public bool PCMBassStream
            {
                get { return (info.flags & BASS_DATA_FLOAT) == 0; }
            }

            private bool convertFloatToPCM = false;
            private int handle = 0;
            private ChannelInfo info;
            private long position = 0;

            float[] pcmBuffer = null;

            public BASSStream(string file, TranscodeOptions options)
            {
                handle = Plugin.mbApi.Player_OpenStreamHandle(file, options.useMusicBeeSettings, options.enableDSP, options.replayGainMode);

                if (handle == 0) {
                    int status = BASS_ErrorGetCode();
                    throw new FormatException(String.Format("Opening {0} stream returned error code: {1}", file, status));
                }

                BASS_ChannelGetInfo(handle, out info);
                
                // If we're responsible for PCM conversion all indexing into the underlying stream must be doubled
                convertFloatToPCM = FloatingPointBassStream && options.usePCM;

                if (convertFloatToPCM) {
                    pcmBuffer = new float[] { };
                }
            }

            private const int WAV_HEADER_SIZE = 44;

            private const uint RIFF = 0x46464952;
            private const uint WAVE = 0x45564157;
            private const uint FMT = 0x20746d66;
            private const uint DATA = 0x61746164;
            private const uint SUBCHUNK1_SIZE = 16;
            private const short AUDIO_FORMAT_PCM = 1;
            private const short AUDIO_FORMAT_FLOAT = 3;

            private int WriteWAVHeader(byte[] buffer, int offset)
            {
                Debug.Assert(offset < WAV_HEADER_SIZE);
                Debug.Assert(buffer.Length > WAV_HEADER_SIZE);

                byte[] header = new byte[44];

                int size = BASS_ChannelGetLength(handle, BASS_POS_BYTE);

                if (size == -1) {
                    throw new FormatException();
                }

                short bitsPerSample;
                short audioFormat;

                if (PCMBassStream || convertFloatToPCM) {
                    bitsPerSample = 16;
                    audioFormat = AUDIO_FORMAT_PCM;
                } else {
                    bitsPerSample = 32;
                    audioFormat = AUDIO_FORMAT_FLOAT;
                }

                if (convertFloatToPCM) {
                    size /= 2;
                }

                short channels = (short)info.chans;
                short blockAlign = (short)(channels * (bitsPerSample / 8));

                int sampleRate = info.freq;
                int byteRate = sampleRate * channels * (bitsPerSample / 8);

                using (BinaryWriter writer = new BinaryWriter(new MemoryStream(header))) {
                    writer.Write(RIFF);              // ChunkID
                    writer.Write(36 + size);         // ChunkSize
                    writer.Write(WAVE);              // Format
                    writer.Write(FMT);               // SubChunk1ID
                    writer.Write(SUBCHUNK1_SIZE);    // SubChunk1Size
                    writer.Write(audioFormat);       // AudioFormat
                    writer.Write(channels);          // NumChannels
                    writer.Write(sampleRate);        // SampleRate
                    writer.Write(byteRate);          // ByteRate
                    writer.Write(blockAlign);        // BlockAlign
                    writer.Write(bitsPerSample);     // BitsPerSample
                    writer.Write(DATA);              // SubChunk2ID
                    writer.Write(size);              // SubChunk2Size
                }

                Buffer.BlockCopy(header, offset, buffer, offset, WAV_HEADER_SIZE - offset);

                return header.Length - offset;
            }
        }

        public static Stream Open(string file)
        {
            Stream result;

            if (IsTranscodeRequired(GetExtension(file))) {
                result = new BASSStream(file, Plugin.settings.transcode);
            } else {
                result = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            return result;
        }

        public static string GetFormatFromFileName(string file)
        {
            string result = GetExtension(file);

            if (IsTranscodeRequired(result)) {
                result = "wav";
            }

            return result;
        }

        private static string GetExtension(string file)
        {
            string ext = Path.GetExtension(file);
            string result = ext.Substring(1);

            return result;
        }

        private static bool IsTranscodeRequired(string ext)
        {
            Plugin.FileCodec codec = GetFileCodec(ext);

            return Plugin.settings.transcode.formats.Contains(codec);
        }

        internal static Plugin.FileCodec GetFileCodec(string ext)
        {
            Plugin.FileCodec result = Plugin.FileCodec.Unknown;

            switch (ext.ToLower()) {
                case "mp3": {
                    result = Plugin.FileCodec.Mp3;
                } break;
                case "m4a":
                case "m4b":
                case "m4p":
                case "m4r":
                case "3gp":
                case "mp4":
                case "aac": {
                    // alac might sometimes be reported as aac
                    // but anything that supports aac should support alac, too
                    result = Plugin.FileCodec.Aac;
                } break;
                case "flac": {
                    result = Plugin.FileCodec.Flac;
                } break;
                case "ogv":
                case "oga":
                case "ogx":
                case "ogm":
                case "ogg": {
                    result = Plugin.FileCodec.Ogg;
                } break;
                case "wv":
                case "wavpack": {
                    result = Plugin.FileCodec.WavPack;
                } break;
                case "wma": {
                    result = Plugin.FileCodec.Wma;
                } break;
                case "tak": {
                    result = Plugin.FileCodec.Tak;
                } break;
                case "mpc":
                case "mpp":
                case "mp+": {
                    result = Plugin.FileCodec.Mpc;
                } break;
                case "wav":
                case "wave": {
                    result = Plugin.FileCodec.Wave;
                } break;
                case "asx": {
                    result = Plugin.FileCodec.Asx;
                } break;
                case "alac": {
                    result = Plugin.FileCodec.Alac;
                } break;
                case "aif":
                case "aifc":
                case "aiff": {
                    result = Plugin.FileCodec.Aiff;
                } break;
                case "l16":
                case "au":
                case "pcm": {
                    result = Plugin.FileCodec.Pcm;
                } break;
                case "opus": {
                    result = Plugin.FileCodec.Opus;
                } break;
                case "spx": {
                    result = Plugin.FileCodec.Spx;
                } break;
                case "dsd": {
                    result = Plugin.FileCodec.Dsd;
                } break;
            }

            return result;
        }

        internal static bool IsAudioFile(string file)
        {
            return GetFileCodec(GetExtension(file)) != Plugin.FileCodec.Unknown;
        }

    }
}
