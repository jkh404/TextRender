using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using TextRender;
using TextRender.Handles;
using UtfUnknown;

namespace WinFormsTest
{
    internal static class Program
    {
        private static readonly string filePath= "《恶灵国度》作者：弹指一笑间0_make.txt";

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [System.Runtime.InteropServices.DllImport("Kernel32")]
        public static extern void FreeConsole();
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AllocConsole();

            try
            {

                //bool[] bools = new bool[1000000000];
                //var t=int.MaxValue;

                DetectionResult result = CharsetDetector.DetectFromFile(filePath);
                var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                using TextProviderHandle textProviderHandle=new TextProviderHandle(fileStream, result.Detected.Encoding);
                Console.WriteLine(textProviderHandle.ByteCount);
                Console.WriteLine(textProviderHandle.CountRunning);
                //Thread.Sleep(6000);
                Stopwatch sw = Stopwatch.StartNew();
                Console.WriteLine(textProviderHandle.Count);
                Console.WriteLine(sw.Elapsed);
                Console.WriteLine(textProviderHandle.CountRunning);
                Console.ReadKey();
                //var EncodingProvider = CodePagesEncodingProvider.Instance;
                //var names = EncodingProvider.GetEncodings().Select(x => x.Name).ToArray();
                //Encoding.RegisterProvider(EncodingProvider);
                //ApplicationConfiguration.Initialize();
                //Application.Run(new Form1());
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);
            }
            finally
            {
                FreeConsole();
            }

        }
    }
}