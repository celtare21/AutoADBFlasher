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
            AdbServer server;
            AdbClient client;
            List<DeviceData> devices;
            StartServerResult server_result;
            Fastboot fastboot;
            Fastboot.Response result = null, slot;
            DirectoryInfo dirInfo;
            FileInfo file = null;
            string pattern = "i*.img", pattern_downloading = "*Unconfirmed*", flash_slot = null;
            bool connected = false;

            server = new AdbServer();
            try
            {
                server_result = server.StartServer(@"C:\Program Files (x86)\Essential\ADB\adb.exe", restartServerIfNewer: false);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("ADB path not found!");
                Console.ReadKey();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception {ex} occured!");
                Console.ReadKey();
                return;
            }

            if (server_result.ToString().Equals("Started"))
                Console.WriteLine("ADB server started!\n");
            else
                Console.WriteLine("ADB server already started!\n");

            fastboot = new Fastboot();

            client = new AdbClient();
            do
            {
                devices = client.GetDevices();
                if (devices.Count == 0)
                {
                    Console.WriteLine("No adb devices found! Sleeping 1.5s and checking if system is in fastboot.");

                    try
                    {
                        fastboot.Connect();
                        Console.WriteLine("\nFound fastboot device!\n");
                        connected = true;
                    }
                    catch
                    {
                        Console.WriteLine("No fastboot devices found!");
                        Thread.Sleep(1500);
                    }
                }
                else
                {
                    Console.WriteLine("\nDevice found! Rebooting to bootloader.\n");

                    client.ExecuteRemoteCommand("reboot bootloader", devices.First(), null);
                }
            } while (devices.Count == 0 && !connected);

            dirInfo = new DirectoryInfo("C:\\Users\\Kuran Kaname\\Downloads");
            do
            {
                try
                {
                    file = (from f in dirInfo.GetFiles(pattern_downloading) orderby f.LastWriteTime descending select f).First();
                    if (file.Exists)
                    {
                        Console.WriteLine("File is still downloading! Sleeping 1.5s.");
                        Thread.Sleep(1500);
                        continue;
                    }
                }
                catch (InvalidOperationException)
                {
                    try
                    {
                        file = (from f in dirInfo.GetFiles(pattern) orderby f.LastWriteTime descending select f).First();
                        break;
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("No file found!");
                        Console.ReadKey();
                    }

                    return;
                }

                Console.WriteLine("File is still downloading! Sleeping 1.5s.");
                Thread.Sleep(1500);
            } while (file.ToString().Contains("crdownload"));
            Console.WriteLine($"\nFound file: {file.Name}\n");

            while (!connected)
            {
                try
                {
                    fastboot.Connect();
                    connected = true;
                    Console.WriteLine("\nFastboot device found!");
                }
                catch
                {
                    Console.WriteLine("No fastboot devices found! Sleeping 1.5s.");
                    Thread.Sleep(1500);
                }
            }

            slot = fastboot.Command("getvar:current-slot");
            Console.WriteLine($"Current slot is: {slot.Payload}");

            fastboot.UploadData($"{file.Directory}\\{file.Name}");

            if (slot.Payload.Contains("a"))
            {
                flash_slot = "flash:boot_a";
            }
            else if (slot.Payload.Contains("b"))
            {
                flash_slot = "flash:boot_b";
            }
            else
            {
                Console.WriteLine("No slot found!");
                Console.ReadLine();
                return;
            }

            try
            {
                result = fastboot.Command(flash_slot);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Flash unsuccesful! Status: {ex}");
                Console.ReadKey();
                return;
            }

            if (string.Equals(result.Status.ToString(), "Okay"))
            {
                Console.WriteLine("Flash succesful! Rebooting!");
                fastboot.Command("reboot");
            }
            else
            {
                Console.WriteLine($"Flash unsuccesful! Status: {result.Status}");
            }

            Console.ReadKey();
        }
    }
}
