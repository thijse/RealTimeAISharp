using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealtimeInteractiveConsole
{
    public class AudioDeviceHelper
    {
        //public string GetDefaultInputDevice()
        //{
        //    return WaveInEvent.GetCapabilities(WaveInEvent.DeviceNumber).ProductName;
        //}

        public void ShowInputDevices()
        {
            int waveInDevices = WaveInEvent.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveInEvent.GetCapabilities(waveInDevice);
                Console.WriteLine("Device {0}: {1}, {2} channels", waveInDevice, deviceInfo.ProductName, deviceInfo.Channels);
            }
        }

        // Function that enumerates the available output devices numbered, and allows the user to select one.
        public int SelectInputDevice()
        {
            int waveInDevices = WaveInEvent.DeviceCount;
            Console.WriteLine("INPUT DEVICES");
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveInEvent.GetCapabilities(waveInDevice);
                Console.WriteLine("Device {0}: {1}, {2} channels", waveInDevice, deviceInfo.ProductName, deviceInfo.Channels);
            }
            Console.Write("Select the input device number: [0] ");
            var input = Console.ReadLine(); if (string.IsNullOrEmpty(input)) { input = "0"; }

            int inputDeviceNumber = int.Parse(input);
            return inputDeviceNumber;
        }

        //Select the output device
        public int SelectOutputDevice()
        {
            int waveOutDevices = WaveInEvent.DeviceCount;
            Console.WriteLine("OUTPUT DEVICES");
            for (int waveOutDevice = -1; waveOutDevice < waveOutDevices; waveOutDevice++)
            {
                try
                {
                    var deviceInfo = WaveOut.GetCapabilities(waveOutDevice);
                    Console.WriteLine("Device {0}: {1}, {2} channels", waveOutDevice, deviceInfo.ProductName, deviceInfo.Channels);

                }
                catch (Exception e)
                {
                    Console.WriteLine("Device {0}: {1}", waveOutDevice, e.Message);
                }
            }
            Console.Write("Select the output device number: [0]");
            var input = Console.ReadLine(); if (string.IsNullOrEmpty(input)) { input = "0"; }
            int outputDeviceNumber = int.Parse(input);
            return outputDeviceNumber;
        }

    }
}
