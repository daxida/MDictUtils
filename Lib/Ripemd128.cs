using Org.BouncyCastle.Crypto.Digests;

namespace Lib;


public static class Ripemd128
{
    // Wrapper in case we end up implementing it ourselves
    public static byte[] ComputeHash(byte[] message)
    {
        return ComputeRipeMd128Hash(message);
    }

    // https://github.com/bcgit/bc-csharp/blob/4b87b5e7d6b42d1028838efe356730411446a8f5/crypto/src/crypto/digests/RipeMD128Digest.cs#L11
    private static byte[] ComputeRipeMd128Hash(byte[] inputBytes)
    {
        var ripemd128 = new RipeMD128Digest();
        ripemd128.BlockUpdate(inputBytes, 0, inputBytes.Length);
        byte[] result = new byte[ripemd128.GetDigestSize()];
        ripemd128.DoFinal(result, 0);
        return result;
    }

    // Shouldn't be here
    // https://github.com/liuyug/mdict-utils/blob/64e15b99aca786dbf65e5a2274f85547f8029f2e/mdict_utils/base/readmdict.py#L58
    public static byte[] FastDecrypt(byte[] data, byte[] key)
    {
        byte[] result = new byte[data.Length];
        byte previous = 0x36;

        for (int i = 0; i < data.Length; i++)
        {
            byte current = data[i];

            // Rotate nibbles: (b >> 4 | b << 4) & 0xff
            byte t = (byte)(((current >> 4) | (current << 4)) & 0xFF);

            t = (byte)(t ^ previous ^ (i & 0xFF) ^ key[i % key.Length]);

            previous = current;
            result[i] = t;
        }

        return result;
    }
}
