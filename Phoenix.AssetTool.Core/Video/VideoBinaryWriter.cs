using BCnEncoder.Encoder;
using FFMpegCore;
using FFMpegCore.Enums;
using Phoenix.AssetTool.Core.Build;
using Phoenix.AssetTool.Core.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using PixelFormat = BCnEncoder.Encoder.PixelFormat;

namespace Phoenix.AssetTool.Core.Video
{
    public static class VideoBinaryWriter
    {
        public static void Build(AssetBuildStatus status, VideoLoadOptions options, 
            string sourcePath, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var mediaInfo = FFProbe.Analyse(sourcePath);
            
            var videoStream = mediaInfo.PrimaryVideoStream;
            if (videoStream == null)
                throw new Exception("No video stream found in file");

            var sourceWidth = videoStream.Width;
            var sourceHeight = videoStream.Height;
            var sourceFps = (float)videoStream.FrameRate;
            var duration = mediaInfo.Duration.TotalSeconds;

            var targetWidth = Math.Min(sourceWidth, options.MaxWidth);
            var targetHeight = Math.Min(sourceHeight, options.MaxHeight);

            if (targetWidth % 2 != 0) targetWidth--;
            if (targetHeight % 2 != 0) targetHeight--;

            var targetFps = options.FrameRate > 0 ? options.FrameRate : sourceFps;
            var frameCount = (int)Math.Ceiling(duration * targetFps);

            status.MaxSteps = 3 + frameCount + (options.ExtractAudio && mediaInfo.PrimaryAudioStream != null ? 1 : 0);
            status.Step = 0;

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var framePattern = Path.Combine(tempDir, "frame_%06d.png");
                
                FFMpegArguments
                    .FromFileInput(sourcePath)
                    .OutputToFile(framePattern, false, args => args
                        .WithVideoFilters(filterOptions => filterOptions
                            .Scale(targetWidth, targetHeight))
                        .WithFramerate(targetFps)
                        .ForceFormat("image2"))
                    .ProcessSynchronously();

                var frameFiles = Directory.GetFiles(tempDir, "frame_*.png")
                    .OrderBy(f => f)
                    .ToArray();

                if (frameFiles.Length == 0)
                    throw new Exception("No frames extracted from video");

                frameCount = frameFiles.Length;

                var format = options.Format.ConvertFormat();
                var encoder = new BcEncoder
                {
                    OutputOptions =
                    {
                        Format = format,
                        GenerateMipMaps = options.GenerateMipMaps,
                        Quality = CompressionQuality.Balanced
                    }
                };

                byte[][] frameData;
                int[][]? mipLengths = null;
                int mipCount = 1;
                Vector2[][]? mipSizes = null;

                frameData = new byte[frameCount][];

                if (options.GenerateMipMaps)
                {
                    mipSizes = new Vector2[frameCount][];
                    mipLengths = new int[frameCount][];
                }

                for (int i = 0; i < frameCount; i++)
                {
                    status.Step = i + 1;

                    using var image = Image.Load<Rgba32>(frameFiles[i]);
                    int w = image.Width;
                    int h = image.Height;
                    byte[] pixelData = new byte[w * h * 4];
                    image.CopyPixelDataTo(pixelData);

                    if (options.GenerateMipMaps)
                    {
                        var mips = encoder.CalculateNumberOfMipLevels(w, h);
                        mipCount = mips;
                        var mipSizesForFrame = new Vector2[mips];
                        mipSizesForFrame[0] = new Vector2(w, h);
                        for (int m = 1; m < mips; m++)
                        {
                            encoder.CalculateMipMapSize(w, h, m, out int mWidth, out int mHeight);
                            mipSizesForFrame[m] = new Vector2(mWidth, mHeight);
                        }
                        mipSizes[i] = mipSizesForFrame;

                        var encodedMips = encoder.EncodeToRawBytes(pixelData, w, h, PixelFormat.Rgba32);
                        var mipLens = new int[encodedMips.Length];
                        var allMipData = new List<byte>();
                        for (int m = 0; m < encodedMips.Length; m++)
                        {
                            mipLens[m] = encodedMips[m].Length;
                            allMipData.AddRange(encodedMips[m]);
                        }
                        mipLengths[i] = mipLens;

                        using var ms = new MemoryStream();
                        ms.Write(BitConverter.GetBytes(encodedMips[0].Length));
                        ms.Write(encodedMips[0]);
                        for (int m = 1; m < encodedMips.Length; m++)
                        {
                            ms.Write(BitConverter.GetBytes(mipLens[m]));
                            ms.Write(BitConverter.GetBytes((int)mipSizesForFrame[m].X));
                            ms.Write(BitConverter.GetBytes((int)mipSizesForFrame[m].Y));
                            ms.Write(encodedMips[m]);
                        }
                        frameData[i] = ms.ToArray();
                    }
                    else
                    {
                        var encoded = encoder.EncodeToRawBytes(pixelData, w, h, PixelFormat.Rgba32);
                        frameData[i] = encoded[0];
                    }
                }

                byte[]? audioPCM = null;
                int audioSampleRate = 0;
                int audioChannels = 0;
                short audioBitsPerSample = 16;
                bool hasAudio = false;

                if (options.ExtractAudio && mediaInfo.PrimaryAudioStream != null)
                {
                    status.Step = frameCount + 1;

                    var audioStream = mediaInfo.PrimaryAudioStream;
                    audioSampleRate = audioStream.SampleRateHz;
                    audioChannels = audioStream.Channels;

                    var tempWav = Path.Combine(tempDir, "audio.wav");
                    
                    FFMpegArguments
                        .FromFileInput(sourcePath)
                        .OutputToFile(tempWav, false, args => args
                            .ForceFormat("wav")
                            .WithAudioCodec("pcm_s16le"))
                        .ProcessSynchronously();

                    if (File.Exists(tempWav))
                    {
                        audioPCM = ExtractWavPCM(tempWav, out audioSampleRate, out audioChannels, out audioBitsPerSample);
                        hasAudio = audioPCM != null && audioPCM.Length > 0;
                    }
                }

                status.Step = status.MaxSteps;

                using var fs = File.Create(outputPath);
                using var bw = new BinaryWriter(fs);

                bw.Write("PHXV");
                bw.Write((uint)1);

                bw.Write(targetWidth);
                bw.Write(targetHeight);
                bw.Write(frameCount);
                bw.Write(targetFps);
                bw.Write((byte)options.Format);
                bw.Write(options.GenerateMipMaps);
                bw.Write(hasAudio);

                if (hasAudio)
                {
                    bw.Write(audioSampleRate);
                    bw.Write((short)audioChannels);
                    bw.Write(audioBitsPerSample);
                    bw.Write(audioPCM!.Length);
                    bw.Write(audioPCM);
                }

                var frameIndexOffset = (int)fs.Position;
                bw.Write(frameIndexOffset + 8 + (frameCount * 8));
                bw.Write(frameCount);

                var frameDataStart = (int)fs.Position + (frameCount * 8);

                for (int i = 0; i < frameCount; i++)
                {
                    bw.Write(frameDataStart);
                    bw.Write(frameData[i].Length);
                    frameDataStart += frameData[i].Length;
                }

                for (int i = 0; i < frameCount; i++)
                {
                    bw.Write(frameData[i]);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        private static byte[]? ExtractWavPCM(string wavPath, out int sampleRate, out int channels, out short bitsPerSample)
        {
            sampleRate = 0;
            channels = 0;
            bitsPerSample = 16;

            using var fs = File.OpenRead(wavPath);
            using var br = new BinaryReader(fs);

            string riff = new string(br.ReadChars(4));
            if (riff != "RIFF")
                return null;

            br.ReadInt32();
            string wave = new string(br.ReadChars(4));
            if (wave != "WAVE")
                return null;

            byte[]? pcmData = null;

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                string chunkId = new string(br.ReadChars(4));
                int chunkSize = br.ReadInt32();

                switch (chunkId)
                {
                    case "fmt ":
                        short audioFormat = br.ReadInt16();
                        channels = br.ReadInt16();
                        sampleRate = br.ReadInt32();
                        br.ReadInt32();
                        br.ReadInt16();
                        bitsPerSample = br.ReadInt16();
                        if (chunkSize > 16)
                            br.BaseStream.Position += (chunkSize - 16);
                        break;

                    case "data":
                        pcmData = br.ReadBytes(chunkSize);
                        break;

                    default:
                        br.BaseStream.Position += chunkSize;
                        break;
                }

                if ((chunkSize & 1) != 0)
                    br.BaseStream.Position += 1;
            }

            return pcmData;
        }
    }
}
