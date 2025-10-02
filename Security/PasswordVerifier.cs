using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SWebEnergia.Security
{
    public static class PasswordVerifier
    {
        private static readonly Regex HexRegex = new("^[0-9a-fA-F]+$", RegexOptions.Compiled);

        // ✅ Nuevo: Genera un salt aleatorio seguro en Base64
        public static string GenerateSalt(int size = 16)
        {
            var bytes = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        // ✅ Nuevo: Hashea la contraseña con PBKDF2 (el recomendado para registrar/recuperar)
        public static string HashPassword(string password, string salt, int iterations = 10000, int length = 32)
        {
            var saltBytes = Encoding.UTF8.GetBytes(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(length);
            return Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string saltStored, string hashStored, out string matchedAlgo)
        {
            matchedAlgo = "none";
            if (password == null) return false;
            saltStored ??= string.Empty;
            hashStored ??= string.Empty;

            // ✅ 0) Comparación directa (texto plano en BD)
            if (password == hashStored)
            {
                matchedAlgo = "PlainText";
                return true;
            }

            // ✅ 1) Comparación contra nuestro esquema recomendado (PBKDF2)
            var pbkdf2Hash = HashPassword(password, saltStored);
            if (SlowEquals(pbkdf2Hash, hashStored))
            {
                matchedAlgo = "PBKDF2-SHA256";
                return true;
            }

            var saltBytesCandidates = GetSaltByteCandidates(saltStored);

            // 2) SHA256 / SHA512
            var strVariants = new (string label, Func<string> value)[]
            {
                ("SHA256(pwd+salt)-b64", () => ToB64(SHA256(UTF8(password + saltStored)))),
                ("SHA256(salt+pwd)-b64", () => ToB64(SHA256(UTF8(saltStored + password)))),
                ("SHA256(pwd+salt)-hex", () => ToHex(SHA256(UTF8(password + saltStored)))),
                ("SHA256(salt+pwd)-hex", () => ToHex(SHA256(UTF8(saltStored + password)))),

                ("SHA512(pwd+salt)-b64", () => ToB64(SHA512(UTF8(password + saltStored)))),
                ("SHA512(salt+pwd)-b64", () => ToB64(SHA512(UTF8(saltStored + password)))),
                ("SHA512(pwd+salt)-hex", () => ToHex(SHA512(UTF8(password + saltStored)))),
                ("SHA512(salt+pwd)-hex", () => ToHex(SHA512(UTF8(saltStored + password)))),
            };

            foreach (var v in strVariants)
            {
                if (SlowEquals(v.value(), hashStored))
                {
                    matchedAlgo = v.label;
                    return true;
                }
            }

            // 3) HMAC variantes
            foreach (var saltBytes in saltBytesCandidates)
            {
                if (SlowEquals(ToB64(HMACSHA256(saltBytes, UTF8(password))), hashStored)) { matchedAlgo = "HMACSHA256-b64"; return true; }
                if (SlowEquals(ToHex(HMACSHA256(saltBytes, UTF8(password))), hashStored)) { matchedAlgo = "HMACSHA256-hex"; return true; }
                if (SlowEquals(ToB64(HMACSHA512(saltBytes, UTF8(password))), hashStored)) { matchedAlgo = "HMACSHA512-b64"; return true; }
                if (SlowEquals(ToHex(HMACSHA512(saltBytes, UTF8(password))), hashStored)) { matchedAlgo = "HMACSHA512-hex"; return true; }
            }

            // 4) PBKDF2 con iteraciones típicas antiguas
            int[] iters = new[] { 1000, 10000, 50000, 100000 };
            foreach (var saltBytes in saltBytesCandidates)
            {
                foreach (var iter in iters)
                {
                    var pbkdf2 = PBKDF2(password, saltBytes, iter, 32);
                    if (SlowEquals(ToB64(pbkdf2), hashStored)) { matchedAlgo = $"PBKDF2-{iter}-b64"; return true; }
                    if (SlowEquals(ToHex(pbkdf2), hashStored)) { matchedAlgo = $"PBKDF2-{iter}-hex"; return true; }
                }
            }

            matchedAlgo = "no-match";
            return false;
        }

        // Helpers
        private static byte[] UTF8(string s) => Encoding.UTF8.GetBytes(s ?? string.Empty);
        private static byte[] SHA256(byte[] inp) { using var a = SHA256Managed.Create(); return a.ComputeHash(inp); }
        private static byte[] SHA512(byte[] inp) { using var a = SHA512Managed.Create(); return a.ComputeHash(inp); }
        private static byte[] HMACSHA256(byte[] key, byte[] msg) { using var h = new HMACSHA256(key); return h.ComputeHash(msg); }
        private static byte[] HMACSHA512(byte[] key, byte[] msg) { using var h = new HMACSHA512(key); return h.ComputeHash(msg); }
        private static byte[] PBKDF2(string pwd, byte[] salt, int iter, int len)
        {
            using var k = new Rfc2898DeriveBytes(pwd, salt, iter, HashAlgorithmName.SHA256);
            return k.GetBytes(len);
        }
        private static string ToB64(byte[] b) => Convert.ToBase64String(b);
        private static string ToHex(byte[] b)
        {
            var sb = new StringBuilder(b.Length * 2);
            foreach (var x in b) sb.AppendFormat("{0:x2}", x);
            return sb.ToString();
        }

        private static bool SlowEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            var ba = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);
            if (ba.Length != bb.Length) return false;
            int diff = 0;
            for (int i = 0; i < ba.Length; i++) diff |= ba[i] ^ bb[i];
            return diff == 0;
        }

        private static IEnumerable<byte[]> GetSaltByteCandidates(string salt)
        {
            if (string.IsNullOrEmpty(salt))
            {
                yield return Array.Empty<byte>();
                yield break;
            }

            // Base64
            byte[]? b64 = null;
            try { b64 = Convert.FromBase64String(salt); } catch { }
            if (b64 != null && b64.Length > 0)
                yield return b64;

            // Hex
            if (HexRegex.IsMatch(salt))
                yield return HexToBytes(salt);

            // UTF8
            yield return Encoding.UTF8.GetBytes(salt);
        }

        private static byte[] HexToBytes(string hex)
        {
            hex = hex.Replace(" ", "");
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }
    }
}

