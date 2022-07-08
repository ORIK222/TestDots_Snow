using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public static class Sha256Utility
{
    public static string sha256_hash(byte[] value)
    {
        StringBuilder Sb = new StringBuilder();
        using (SHA256 hash = SHA256Managed.Create())
        {
            //Encoding enc = Encoding.UTF8;
            byte[] result = hash.ComputeHash(value);// enc.GetBytes(value));

            foreach (byte b in result)
                Sb.Append(b.ToString("x2"));
        }

        return Sb.ToString();
    }
}
