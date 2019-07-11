using System;
using System.Runtime.InteropServices;
using System.Security;

namespace TestAdobeLiveStream
{
    static class SecureStringHelper
    {
        public static string ConvertToUnsecureString(SecureString secureString)
        {
            if (secureString == null)
                throw new ArgumentNullException("A null was passed to ConvertToUnsecureString (SecureString secureString) method.  Please pass a non-null value.");

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        public static SecureString ConvertToSecureString(string stringToSecure)
        {
            if (stringToSecure == null || stringToSecure == "")
                throw new ArgumentNullException("A null was passed to ConvertToSecureString (string stringToSecure) method.  Please pass a non-null value.");

            var secureStr = new SecureString();
            if (stringToSecure.Length > 0)
            {
                foreach (var c in stringToSecure.ToCharArray()) secureStr.AppendChar(c);
            }
            return secureStr;
        }

    }
}
