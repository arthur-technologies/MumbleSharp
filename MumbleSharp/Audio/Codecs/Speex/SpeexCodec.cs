﻿using System;

namespace MumbleSharp.Audio.Codecs.Speex
{
    public class SpeexCodec
        : IVoiceCodec
    {
        public float[] Decode(byte[] encodedData, float[] emptyPcmBuffer)
        {
            throw new NotImplementedException();
        }

        public System.Collections.Generic.IEnumerable<int> PermittedEncodingFrameSizes
        {
            get { throw new NotImplementedException(); }
        }

        public byte[] Encode(ArraySegment<byte> pcm)
        {
            throw new NotImplementedException();
        }
    }
}
