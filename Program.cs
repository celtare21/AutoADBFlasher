using Potato.Fastboot;
using SharpAdbClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ADB
{
    class Program
    {
        static void Main(string[] args)
        {
            AdbClient client = new AdbClient();
            List<DeviceData> devices;
            DeviceData device;
            Fastboot fastboot;
            Fastboot.Response result, slot;
            DirectoryInfo dirInfo;
            FileInfo file;
            string pattern = "*new*", slot_name;

            client = new AdbClient();
            do
            {

                devices = client.GetDevices();
                if (devices.Count == 0)
                {
                    Console.WriteLine("No adb devices found! Sleeping 1.5s.");
                    Thread.Sleep(1500);
                }
            } while (devices.Count == 0);
            Console.WriteLine("\nDevice found!\n");

            device = devices.First();
            client.ExecuteRemoteCommand("reboot bootloader", device, null);

            dirInfo = new DirectoryInfo("C:\\Users\\Kuran Kaname\\Downloads");
            do
            {
                try
                {
                    file = (from f in dirInfo.GetFiles(pattern) orderby f.LastWriteTime descending select f).First();
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("No file found!");
                    Console.ReadKey();
                    return;
                }
                if (file.ToString().Contains("crdownload"))
                {
                    Console.WriteLine("File is still downloading! Sleeping 1.5s.");
                    Thread.Sleep(1500);
                }
            } while (file.ToString().Contains("crdownload"));
            Console.WriteLine($"\nFound file: {file.Name}\n");

            fastboot = new Fastboot();
            do
            {
                try
                {
                    fastboot.Connect();
                    break;
                }
                catch
                {
                    Console.WriteLine("No fastboot devices found! Sleeping 1.5s.");
                    Thread.Sleep(1500);
                }
            } while (true);

            Console.WriteLine("\nFastboot device found!");
            slot = fastboot.Command("getvar:current-slot");
            Console.WriteLine($"Current slot is: {slot.Payload}");

            fastboot.UploadData($"{file.Directory}\\{file.Name}");

            if (string.Equals(slot.Payload, "a"))
                slot_name = "flash:boot_a";
            else
                slot_name = "flash:boot_b";

            result = fastboot.Command(slot_name);
            if (string.Equals(result.Status.ToString(), "Okay"))
            {
                Console.WriteLine("Flash succesful! Rebooting!");
                fastboot.Command("reboot");
            }
            else
                Console.WriteLine($"Flash unsuccesful! Status: {result.Status}");

            Console.ReadKey();
        }
    }
}
