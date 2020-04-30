
using System;
using System.Collections.Generic;

namespace MumbleSharp.Audio.Codecs
{
    public interface IVoiceCodec
    {
        /// <summary>
        /// decode the given frame of encoded data into 16 bit PCM
        /// </summary>
        /// <param name="encodedData"></param>
        /// <param name="emptyPcmBuffer"></param>
        /// <returns></returns>
        float[] Decode(byte[] encodedData, float[] emptyPcmBuffer);

        /// <summary>
        /// The set of allowed frame sizes for encoding
        /// </summary>
        IEnumerable<int> PermittedEncodingFrameSizes { get; }

        /// <summary>
        /// Encode a frame of data (must be a permitted size)
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        byte[] Encode(ArraySegment<byte> pcm);
    }
}
