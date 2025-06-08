using Microsoft.Win32;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace AskDB.Commons.Helpers
{
    public static class CryptographyHelper
    {
        public static byte[] GetMachineGuidAesKey()
        {
            const string registryKey = @"SOFTWARE\Microsoft\Cryptography";
            const string valueName = "MachineGuid";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey, writable: false))
            {
                if (key != null)
                {
                    object guidValue = key.GetValue(valueName);
                    if (guidValue != null)
                    {
                        var guid = guidValue.ToString();
                        var user = WindowsIdentity.GetCurrent().User?.Value ?? string.Empty;
                        return SHA256.HashData(Encoding.UTF8.GetBytes($"{guid}-{user}"));
                    }
                }
            }

            throw new InvalidOperationException("Unable to retrieve the machine GUID from the registry.");
        }
    }
}
