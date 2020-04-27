﻿using System;
using System.Collections.Generic;

namespace MumbleSharp.Audio.Codecs.Opus
{
    public class OpusCodec
        : IVoiceCodec
    {
        private readonly Mumble.OpusDecoder _decoder;
        private readonly Mumble.OpusEncoder _encoder;
        private readonly int _sampleRate;
        private readonly ushort _frameSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpusCodec"/> class.
        /// </summary>
        /// <param name="sampleRate">The sample rate in Hertz (samples per second).</param>
        /// <param name="sampleBits">The sample bit depth.</param>
        /// <param name="sampleChannels">The sample channels (1 for mono, 2 for stereo).</param>
        /// <param name="frameSize">Size of the frame in samples.</param>
        public OpusCodec(int sampleRate = Constants.DEFAULT_AUDIO_SAMPLE_RATE, byte sampleBits = Constants.DEFAULT_AUDIO_SAMPLE_BITS, byte channels = Constants.DEFAULT_AUDIO_SAMPLE_CHANNELS, ushort frameSize = Constants.DEFAULT_AUDIO_FRAME_SIZE)
        {
            _sampleRate = sampleRate;
            _frameSize = frameSize;
            _decoder = new Mumble.OpusDecoder(sampleRate, channels) {EnableForwardErrorCorrection = true};
            _encoder = new Mumble.OpusEncoder(sampleRate, channels) { EnableForwardErrorCorrection = true };
            //_decoder = new OpusDecoder(sampleRate, channels) { EnableForwardErrorCorrection = true };
            //_encoder = new OpusEncoder(sampleRate, channels) { EnableForwardErrorCorrection = true };
        }

        public byte[] Decode(byte[] encodedData)
        {
            if (encodedData == null)
            {
                _decoder.Decode(null, 0, 0, new byte[_sampleRate / _frameSize], 0);
                return null;
            }

            int samples = Mumble.OpusDecoder.GetSamples(encodedData, 0, encodedData.Length, _sampleRate);
            if (samples < 1)
                return null;

            byte[] dst = new byte[samples * sizeof(ushort)];
            //_decoder.Decode(encodedData, 0, encodedData.Length, dst, 0);
            int length = _decoder.Decode(encodedData, 0, encodedData.Length, dst, 0);
            if (dst.Length != length)
                Array.Resize(ref dst, length);
            return dst;
        }

        public IEnumerable<int> PermittedEncodingFrameSizes
        {
            get
            {
                return _encoder.PermittedFrameSizes;
            }
        }

        public byte[] Encode(ArraySegment<byte> pcm)
        {
            var samples = pcm.Count / sizeof(ushort);
            var numberOfBytes = _encoder.FrameSizeInBytes(samples);

            byte[] dst = new byte[numberOfBytes];
            int encodedBytes = _encoder.Encode( pcm.Array, pcm.Offset, dst, 0, samples);

            //without it packet will have huge zero-value-tale
            Array.Resize(ref dst, encodedBytes);

            return dst;
        }
    }
}
