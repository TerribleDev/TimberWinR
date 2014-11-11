using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace TimberWinR.ExtractID
{
    class MsiHandle : SafeHandleMinusOneIsInvalid
    {
        public MsiHandle()
            : base(true)
        { }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.MsiCloseHandle(handle) == 0;
        }
    }

    class NativeMethods
    {
        const string MsiDll = "Msi.dll";

        [DllImport(MsiDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public extern static uint MsiOpenPackageW(string szPackagePath, out MsiHandle product);

        [DllImport(MsiDll, ExactSpelling = true)]
        public extern static uint MsiCloseHandle(IntPtr hAny);

        [DllImport(MsiDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        static extern uint MsiGetProductPropertyW(MsiHandle hProduct, string szProperty, StringBuilder value, ref int length);


        [DllImport(MsiDll, ExactSpelling = true)]
        public static extern int MsiSetInternalUI(int value, IntPtr hwnd);

        public static uint MsiGetProductProperty(MsiHandle hProduct, string szProperty, out string value)
        {
            StringBuilder sb = new StringBuilder(1024);
            int length = sb.Capacity;
            uint err;
            value = null;
            if (0 == (err = MsiGetProductPropertyW(hProduct, szProperty, sb, ref length)))
            {
                sb.Length = length;
                value = sb.ToString();
                return 0;
            }

            return err;
        }
    }



    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Expecting MSI and Tempolate file arguments");
                return 1;
            }

            string msiDirectory = args[0];
            string updateFile = args[1];
            string newFile = args[2];
          
            string msiFile = Directory.GetFiles(msiDirectory, "TimberWinR*.msi").FirstOrDefault();

            NativeMethods.MsiSetInternalUI(2, IntPtr.Zero); // Hide all UI. Without this you get a MSI dialog

            MsiHandle msi;
            uint err;
            if (0 != (err = NativeMethods.MsiOpenPackageW(msiFile, out msi)))
            {
                Console.Error.WriteLine("Can't open MSI, error {0}", err);
                return 1;
            }

            // Strings available in all MSIs
            string productCode;
            using (msi)
            {
                if (0 != NativeMethods.MsiGetProductProperty(msi, "ProductCode", out productCode))
                    throw new InvalidOperationException("Can't obtain product code");

                string contents = File.ReadAllText(args[1]);

                contents = contents.Replace("${PROJECTGUID}", productCode);

                File.WriteAllText(args[2], contents);

                Console.WriteLine("Updated {0} ProductID: {1}", args[2], productCode);

                return 0;
            }

            Console.Error.WriteLine("Failed for some reason");
        }
    }
}
