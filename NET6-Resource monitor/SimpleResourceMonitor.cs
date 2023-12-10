using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NET6_Resource_monitor
{
    internal class SimpleResourceMonitor
    {
        public string NetworkCardName { get; set; } = "";

        public void Initialize()
        {
            InitCpu();
            InitGpu();
            InitRam();
            InitTemperature();
            if(!string.IsNullOrEmpty(NetworkCardName)) InitNetWork(NetworkCardName);
        }

        public ResourceMonitorPacket Get()
        {
            var packet = new ResourceMonitorPacket();
            packet.Cpu = GetCpu();
            packet.Ram = GetRam();
            packet.Ssd = GetSsd();
            packet.Gpu = GetGpu();
            packet.Network = string.IsNullOrEmpty(NetworkCardName) ? 0 : GetNewWork();
            packet.Temperature = GetTemperature();

            return packet;
        }       

        PerformanceCounter cpuCounter;
        private void InitCpu()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }
        private double GetCpu()
        {
            return cpuCounter.NextValue();
        }

        PerformanceCounter ramCounter;
        PerformanceCounter ramCommitCounter;
        private void InitRam()
        {
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            ramCommitCounter = new PerformanceCounter("Memory", "Committed Bytes");
        }

        private double GetRam()
        {
            var available = ramCounter.NextValue();
            var commit = ramCommitCounter.NextValue() / 1024 / 1024;
            var free = available / (available + commit);
            return 100 * (1 - free);
        }

        List<PerformanceCounter> gpuCounters;
        private void InitGpu()
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var counterNames = category.GetInstanceNames();

            gpuCounters = counterNames
                                .Where(counterName => counterName.EndsWith("engtype_3D"))
                                .SelectMany(counterName => category.GetCounters(counterName))
                                .Where(counter => counter.CounterName.Equals("Utilization Percentage"))
                                .ToList();
        }

        private double GetGpu()
        {
            gpuCounters.ForEach(x => x.NextValue());
            return gpuCounters.Sum(x => x.NextValue());
        }


        private double GetSsd()
        {
            DriveInfo drive = new DriveInfo("C");
            double percentFree = 100 * (double)drive.TotalFreeSpace / drive.TotalSize;
            return 100 - percentFree;
        }

        private void printNetworkCards()
        {
            PerformanceCounterCategory category = new PerformanceCounterCategory("Network Interface");
            string[] instancename = category.GetInstanceNames();

            foreach (string name in instancename)
            {
                Debug.WriteLine(name);
            }
        }

        PerformanceCounter bandwidthCounter;
        PerformanceCounter dataSentCounter;
        PerformanceCounter dataReceivedCounter;
        private void InitNetWork(string networkCard)
        {
            bandwidthCounter = new PerformanceCounter("Network Interface", "Current Bandwidth", networkCard);
            dataSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", networkCard);
            dataReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", networkCard);
        }

        private double GetNewWork()
        {
            const int numberOfIterations = 10;
            float bandwidth = bandwidthCounter.NextValue();

            float sendSum = 0;
            float receiveSum = 0;
            for (int index = 0; index < numberOfIterations; index++)
            {
                sendSum += dataSentCounter.NextValue();
                receiveSum += dataReceivedCounter.NextValue();
            }
            float dataSent = sendSum;
            float dataReceived = receiveSum;

            double utilization = (8 * (dataSent + dataReceived)) / (bandwidth * numberOfIterations) * 100;
            return utilization;
        }


        ManagementObjectSearcher managementSearcher;
        private void InitTemperature()
        {
            managementSearcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");
        }

        private double GetTemperature()
        {
            List<double> result = new List<double>();
            foreach (ManagementObject obj in managementSearcher.Get())
            {
                double temperature = Convert.ToDouble(obj["CurrentTemperature"].ToString());
                temperature = (temperature - 2732) / 10.0;
                result.Add(temperature);
            }

            return result.First();
        }
    }
}
