using MumbleSharp.Audio.Codecs;
//using NAudio.Wave;
using System;
using System.Collections.Generic;
using Mumble;
using UnityEngine;

namespace MumbleSharp.Audio
{
    /// <summary>
    /// Buffers up encoded audio packets and provides a constant stream of sound (silence if there is no more audio to decode)
    /// </summary>
    public class AudioDecodingBuffer// : IWaveProvider
    {
        private readonly int _sampleRate;
        private readonly ushort _frameSize;
        //public WaveFormat WaveFormat { get; private set; }
        private int _decodedOffset;
        private int _decodedCount;
        private readonly float[] _decodedBuffer;


        /// <summary>
        /// Initializes a new instance of the <see cref="AudioDecodingBuffer"/> class which Buffers up encoded audio packets and provides a constant stream of sound (silence if there is no more audio to decode).
        /// </summary>
        /// <param name="sampleRate">The sample rate in Hertz (samples per second).</param>
        /// <param name="sampleBits">The sample bit depth.</param>
        /// <param name="sampleChannels">The sample channels (1 for mono, 2 for stereo).</param>
        /// <param name="frameSize">Size of the frame in samples.</param>
        public AudioDecodingBuffer(int sampleRate = Constants.DEFAULT_AUDIO_SAMPLE_RATE, byte sampleBits = Constants.DEFAULT_AUDIO_SAMPLE_BITS, byte sampleChannels = Constants.DEFAULT_AUDIO_SAMPLE_CHANNELS, ushort frameSize = Constants.DEFAULT_AUDIO_FRAME_SIZE)
        {
            //WaveFormat = new WaveFormat(sampleRate, sampleBits, sampleChannels);
            _sampleRate = sampleRate;
            _frameSize = frameSize;
            _decodedBuffer = new float[sampleRate * (sampleBits / 8) * sampleChannels];
        }

        private long _nextSequenceToDecode;
        private readonly List<BufferPacket> _encodedBuffer = new List<BufferPacket>(); 

        private IVoiceCodec _codec;

        public int Read(float[] buffer, int offset, int count)
        {
            int readCount = 0;
            while (readCount < count)
            {
                readCount += ReadFromBuffer(buffer, offset + readCount, count - readCount);

                //Try to decode some more data into the buffer
                if (!FillBuffer())
                {
                    break;
                }
                else
                {
                    Debug.Log("FillerBuffer FAlse");
                }
            }

            if (readCount == 0)
            {
                //Return silence
                Array.Clear(buffer, 0, buffer.Length);
                return count;
            }

            return readCount;
        }

        /// <summary>
        /// Add a new packet of encoded data
        /// </summary>
        /// <param name="sequence">Sequence number of this packet</param>
        /// <param name="data">The encoded audio packet</param>
        /// <param name="codec">The codec to use to decode this packet</param>
        public void AddEncodedPacket(long sequence, byte[] data, IVoiceCodec codec)
        {
            if(sequence == 0)
                _nextSequenceToDecode = 0;

            if (_codec == null)
                _codec = codec;
            else if (_codec != null && _codec != codec)
                ChangeCodec(codec);

            //If the next seq we expect to decode comes after this packet we've already missed our opportunity!
            if (_nextSequenceToDecode > sequence)
                return;

            _encodedBuffer.Add(new BufferPacket {
                Data = data,
                Sequence = sequence
            });
        }

        private void ChangeCodec(IVoiceCodec codec)
        {
            //Decode all buffered packets using current codec
            while (_encodedBuffer.Count > 0)
                FillBuffer();

            _codec = codec;
        }

        private BufferPacket? GetNextEncodedData()
        {
            if (_encodedBuffer.Count == 0)
                return null;

            int minIndex = 0;
            for (int i = 1; i < _encodedBuffer.Count; i++)
                minIndex = _encodedBuffer[minIndex].Sequence < _encodedBuffer[i].Sequence ? minIndex : i;

            var packet = _encodedBuffer[minIndex];
            _encodedBuffer.RemoveAt(minIndex);

            return packet;
        }

        /// <summary>
        /// Read data that has already been decoded
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private int ReadFromBuffer(float[] dst, int offset, int count)
        {
            //Copy as much data as we can from the buffer up to the limit
            int readCount = Math.Min(count, _decodedCount);
           //int readCount = count;
            Array.Copy(_decodedBuffer, _decodedOffset, dst, offset, readCount);
            _decodedCount -= readCount;
            _decodedOffset += readCount;

            //When the buffer is emptied, put the start offset back to index 0
            if (_decodedCount == 0)
                _decodedOffset = 0;

            //If the offset is nearing the end of the buffer then copy the data back to offset 0
            if ((_decodedOffset > _decodedCount) && (_decodedOffset + _decodedCount) > _decodedBuffer.Length * 0.9)
                Buffer.BlockCopy(_decodedBuffer, _decodedOffset, _decodedBuffer, 0, _decodedCount);

            return readCount;
        }
        
        const int SubBufferSize = MumbleConstants.OUTPUT_FRAME_SIZE * MumbleConstants.MAX_FRAMES_PER_PACKET * MumbleConstants.MAX_CHANNELS;

        
        private float[] GetBufferToDecodeInto()
        {
            //TODO use an allocator
            return new float[SubBufferSize];
        }

        /// <summary>
        /// Decoded data into the buffer
        /// </summary>
        /// <returns></returns>
        private bool FillBuffer()
        {
            var packet = GetNextEncodedData();
            if (!packet.HasValue)
                return false;

            ////todo: _nextSequenceToDecode calculation is wrong, which causes this to happen for almost every packet!
            ////Decode a null to indicate a dropped packet
            //if (packet.Value.Sequence != _nextSequenceToDecode)
            //    _codec.Decode(null);
            float[] emptyPcmBuffer = GetBufferToDecodeInto();
            var d = _codec.Decode(packet.Value.Data, emptyPcmBuffer);
            _nextSequenceToDecode = packet.Value.Sequence + d.Length / (_sampleRate / _frameSize);

            Array.Copy(d, 0, _decodedBuffer, _decodedOffset, d.Length);
            _decodedCount += d.Length;
            return true;
        }

        private struct BufferPacket
        {
            public byte[] Data;
            public long Sequence;
        }
    }
}
