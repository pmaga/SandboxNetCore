using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SpanExtern
{
    class Program
    {
        static unsafe void Main(string[] args)
        {
            puts("AaaaA");
            puts("BbbbB".ToBytesTerminatedByZero());

            var c = "CcccC".ToBytesTerminatedByZero();
            var cHandler = GCHandle.Alloc(c, GCHandleType.Pinned);
            var cAddr = cHandler.AddrOfPinnedObject();
            puts(cAddr);
            cHandler.Free();

            var d = "DddddD\0";
            fixed (char* dPtr = d)
            {
                var b = (byte*)dPtr;
                puts(b);
            }

            var e = "EeeeeE".ToBytesTerminatedByZero().AsSpan();
            fixed (byte* ePtr = e)
            {
                puts(ePtr);
            }

            var f = "FffffF".ToBytesTerminatedByZero().AsSpan();
            fixed (byte* fPtr = &f.GetPinnableReference())
            {
                puts(fPtr);
            }

            var g = "GggggG".ToBytesTerminatedByZero();
            fixed (byte* gPtr = g)
            {
                var gLen = strlen(gPtr);
                var gLenChar = (char) gLen;

                puts((byte*)&gLenChar);
            }

            var h = stackalloc byte[5];
            h[0] = (byte)'H';
            h[1] = (byte)'h';
            h[2] = (byte)'h'; // 0
            h[3] = (byte)'h';
            h[4] = 0;
            puts(h);

            var i = stackalloc byte[3];
            i[0] = (byte)'I';
            i[1] = (byte)'i';
            i[2] = 0;
            var iSpan = new ReadOnlySpan<byte>(i, 3);
            fixed (byte* iSpanPtr = iSpan)
            {
                puts(iSpanPtr);
            }


            puts(Utf8StringWithZero.Create("The end"));

            Console.ReadLine();
        }

        [DllImport("msvcrt")]
        public static extern int puts([MarshalAs(UnmanagedType.LPUTF8Str)]string s);

        [DllImport("msvcrt")]
        public static extern int puts(byte[] s);

        [DllImport("msvcrt")]
        public static extern int puts(IntPtr s);

        [DllImport("msvcrt")]
        public static extern unsafe int puts(byte* s);

        [DllImport("msvcrt")]
        public static extern unsafe int puts(int* s);

        public static unsafe int puts(Utf8StringWithZero utf8SWithZero)
        {
            fixed (byte* ptr = &utf8SWithZero.GetPinnableReference())
            {
                return puts(ptr);
            }
        }

        [DllImport("msvcrt")]
        public static extern unsafe int strlen(byte* s);


    }
    // https://docs.microsoft.com/en-us/cpp/c-runtime-library/reference/puts-putws?view=vs-2019




    public readonly ref struct Utf8StringWithZero
    {
        private readonly ReadOnlySpan<byte> _span;

        private Utf8StringWithZero(ReadOnlySpan<byte> span)
        {
            _span = span;
        }

        public ref readonly byte GetPinnableReference()
        {
            return ref _span.GetPinnableReference();
        }

        public static Utf8StringWithZero Create(string s)
        {
            if (s == null)
            {
                return new Utf8StringWithZero(ReadOnlySpan<byte>.Empty);
            }
            return new Utf8StringWithZero(s.ToBytesTerminatedByZero());
        }

        public static Utf8StringWithZero Create(ReadOnlySpan<byte> span)
        {
            if (span.Length > 0 && span[^1] != 0)
            {
                throw new AggregateException("Zero terminator is required.");
            }
            return new Utf8StringWithZero(span);
        }

        public unsafe string ToUtf8String()
        {
            if (_span.Length == 0)
            {
                return null;
            }

            fixed (byte* ptr = _span)
            {
                return Encoding.UTF8.GetString(ptr, _span.Length - 1); //minus zero terminator
            }
        }
    }


    public static class StringExtensions
    {
        public static byte[] ToBytesTerminatedByZero(this string s)
        {
            var bytesCount = Encoding.UTF8.GetByteCount(s);
            var bytes = new byte[bytesCount + 1];
            var writtenBytes = Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
            bytes[writtenBytes] = 0; //NUL, e.g. "Hi" => 72 105 0

            return bytes;
        }
    }
}
