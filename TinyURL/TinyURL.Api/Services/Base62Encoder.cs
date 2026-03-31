namespace TinyURL.Api.Services;

public sealed class Base62Encoder
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public string Encode(long value, int minimumLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

        Span<char> buffer = stackalloc char[16];
        var position = buffer.Length;
        var current = value;

        do
        {
            buffer[--position] = Alphabet[(int)(current % Alphabet.Length)];
            current /= Alphabet.Length;
        }
        while (current > 0);

        var encoded = new string(buffer[position..]);
        return encoded.Length >= minimumLength
            ? encoded
            : encoded.PadLeft(minimumLength, '0');
    }
}
