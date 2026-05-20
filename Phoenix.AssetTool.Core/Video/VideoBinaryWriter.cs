using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using FFMpegCore;
using FFMpegCore.Pipes;
using Phoenix.AssetTool.Core.Build;
using Phoenix.AssetTool.Core.Texture;
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
            var estimatedFrameCount = (int)Math.Ceiling(duration * targetFps);

            status.MaxSteps = 3 + estimatedFrameCount + (options.ExtractAudio && mediaInfo.PrimaryAudioStream != null ? 1 : 0);
            status.Step = 0;

            var frameSize = targetWidth * targetHeight * 4;
            var rawFrames = ExtractRawFrames(sourcePath, targetWidth, targetHeight, targetFps, frameSize, estimatedFrameCount, status);

            if (rawFrames.Count == 0)
                throw new Exception("No frames extracted from video");

            var frameCount = rawFrames.Count;
            var format = options.Format.ConvertFormat();

            status.Step = frameCount + 1;

            var frameData = CompressFramesParallel(rawFrames, targetWidth, targetHeight, format, options.GenerateMipMaps, status);

            byte[]? audioPCM = null;
            int audioSampleRate = 0;
            int audioChannels = 0;
            short audioBitsPerSample = 16;
            bool hasAudio = false;

            if (options.ExtractAudio && mediaInfo.PrimaryAudioStream != null)
            {
                status.Step = frameCount + 2;

                var audioStream = mediaInfo.PrimaryAudioStream;
                audioSampleRate = audioStream.SampleRateHz;
                audioChannels = audioStream.Channels;

                var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                try
                {
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
                finally
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
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

        private static List<byte[]> ExtractRawFrames(string sourcePath, int width, int height, float fps, int frameSize, int estimatedFrameCount, AssetBuildStatus status)
        {
            var frames = new List<byte[]>();
            var buffer = new byte[frameSize];
            int bufferOffset = 0;

            var sink = new StreamPipeSink(async (inputStream, cancellationToken) =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int toRead = frameSize - bufferOffset;
                    int bytesRead = await inputStream.ReadAsync(buffer.AsMemory(bufferOffset, toRead), cancellationToken);

                    if (bytesRead == 0)
                        break;

                    bufferOffset += bytesRead;

                    if (bufferOffset == frameSize)
                    {
                        var frameData = new byte[frameSize];
                        Array.Copy(buffer, frameData, frameSize);
                        frames.Add(frameData);
                        bufferOffset = 0;
                        Interlocked.Increment(ref status.Step);
                    }
                }
            });

            FFMpegArguments
                .FromFileInput(sourcePath, false, args => args
                    .WithHardwareAcceleration())
                .OutputToPipe(sink, args => args
                    .WithVideoFilters(f => f.Scale(width, height))
                    .WithFramerate(fps)
                    .ForceFormat("rawvideo")
                    .ForcePixelFormat("rgba"))
                .ProcessSynchronously();

            return frames;
        }

        private static byte[][] CompressFramesParallel(List<byte[]> rawFrames, int width, int height,
            CompressionFormat format, bool generateMipMaps, AssetBuildStatus status)
        {
            var frameCount = rawFrames.Count;
            var frameData = new byte[frameCount][];

            var encoder = new BcEncoder
            {
                OutputOptions =
                {
                    Format = format,
                    GenerateMipMaps = generateMipMaps,
                    Quality = CompressionQuality.Balanced
                },
                Options =
                {
                    IsParallel = true,
                    TaskCount = Environment.ProcessorCount
                }
            };

            status.Step = 0;
            
            Parallel.For(0, frameCount, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                var pixelData = rawFrames[i];
                if (generateMipMaps)
                {
                    var mips = encoder.CalculateNumberOfMipLevels(width, height);
                    var mipSizesForFrame = new Vector2[mips];
                    mipSizesForFrame[0] = new Vector2(width, height);
                    for (int m = 1; m < mips; m++)
                    {
                        encoder.CalculateMipMapSize(width, height, m, out int mWidth, out int mHeight);
                        mipSizesForFrame[m] = new Vector2(mWidth, mHeight);
                    }

                    var encodedMips = encoder.EncodeToRawBytes(pixelData, width, height, PixelFormat.Rgba32);
                    var mipLens = new int[encodedMips.Length];
                    for (int m = 0; m < encodedMips.Length; m++)
                    {
                        mipLens[m] = encodedMips[m].Length;
                    }

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
                    var encoded = encoder.EncodeToRawBytes(pixelData, width, height, PixelFormat.Rgba32);
                    frameData[i] = encoded[0];
                }

                Interlocked.Add(ref status.Step, 1);
            });

            return frameData;
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
