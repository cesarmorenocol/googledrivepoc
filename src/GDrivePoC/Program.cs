using GDrivePoC.Helpers;
using System;
using System.Threading.Tasks;

namespace GDrivePoC
{
    class Program
    {


        static void Main(string[] args)
        {
            // Upload a file to root file:
            var helper = new GoogleDriveHelper();
            var progress = new Progress<long>(pct => Console.WriteLine($"Progress %{pct}"));
            Task.Run(() => helper.UploadFileAsync("C:\\Temp\\SampleFiles\\Presentaion.pptx", progress));
            Console.Read();
        }
    }
}
