using System.Runtime.InteropServices;
using System.Text;

namespace WinFormsTest
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            var EncodingProvider=CodePagesEncodingProvider.Instance;
            var names=EncodingProvider.GetEncodings().Select(x=>x.Name).ToArray();
            Encoding.RegisterProvider(EncodingProvider);
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}