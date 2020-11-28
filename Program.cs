using Potato.Fastboot;
using SharpAdbClient;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ADB
{
    internal static class Program
    {
        private const string Pattern = "i*.img";
        private const string PatternDownloading = "*Unconfirmed*";

        private static async Task Main()
        {
            StartServerResult serverResult;
            var server = new AdbServer();

            try
            {
                serverResult = server.StartServer(@"C:\Program Files (x86)\Essential\ADB\adb.exe", false);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("ADB path not found!");
                await Task.Delay(1500);
                return;
            }

            Console.WriteLine(serverResult == StartServerResult.Started
                ? "ADB server started!"
                : "ADB server already started!");

            DeviceData device;
            var fastboot = new Fastboot();
            var connected = false;
            var client = new AdbClient();

            do
            {
                device = client.GetDevices().FirstOrDefault();
                if (device == null)
                {
                    Console.WriteLine("No adb devices found! Checking if system is in fastboot and sleeping 1.5s.");

                    try
                    {
                        fastboot.Connect();
                        Console.WriteLine("Found fastboot device!");
                        connected = true;
                    }
                    catch
                    {
                        Console.WriteLine("No fastboot devices found!");
                        await Task.Delay(1500);
                    }
                }
                else
                {
                    Console.WriteLine("Device found! Rebooting to bootloader.");

                    client.ExecuteRemoteCommand("reboot bootloader", device, null);
                }
            } while (device == null && !connected);

            FileInfo file;
            var dirInfo = new DirectoryInfo(@"C:\Users\Kuran Kaname\Downloads");
            bool firstCycle = true;

            do
            {
                file = (from f in dirInfo.GetFiles(PatternDownloading) orderby f.LastWriteTime descending select f)
                    .FirstOrDefault() ??
                    (from f in dirInfo.GetFiles(Pattern) orderby f.LastWriteTime descending select f)
                    .FirstOrDefault();

                if (file?.Name.Contains("crdownload") ?? true)
                {
                    if (firstCycle)
                    {
                        Console.WriteLine();
                        firstCycle = false;
                    }

                    Console.WriteLine("File is still downloading! Sleeping 1.5s.");
                    await Task.Delay(1500);
                }
            } while (file?.Name.Contains("crdownload") ?? true);

            Console.WriteLine();
            Console.WriteLine($"Found file: {file.Name}");
            Console.WriteLine();

            while (!connected)
            {
                try
                {
                    fastboot.Connect();
                    connected = true;
                    Console.WriteLine("Fastboot device found!");
                }
                catch
                {
                    Console.WriteLine("No fastboot devices found! Sleeping 1.5s.");
                    await Task.Delay(1500);
                }
            }

            var slot = fastboot.Command("getvar:current-slot");
            Console.WriteLine($"Current slot is: {slot.Payload}");

            fastboot.UploadData($@"{file.Directory}\{file.Name}");

            string flashSlot;

            if (slot.Payload.Contains("a"))
            {
                flashSlot = "flash:boot_a";
            }
            else if (slot.Payload.Contains("b"))
            {
                flashSlot = "flash:boot_b";
            }
            else
            {
                Console.WriteLine("No slot found!");
                await Task.Delay(2500);
                return;
            }

            Fastboot.Response result = fastboot.Command(flashSlot);

            if (result.Status == Fastboot.Status.Okay)
            {
                Console.WriteLine("Flash succesful! Rebooting!");
                fastboot.Command("reboot");
            }
            else
            {
                Console.WriteLine($"Flash unsuccesful! Status: {result.Status}");
            }

            await Task.Delay(2500).ConfigureAwait(false);
        }
    }
}
