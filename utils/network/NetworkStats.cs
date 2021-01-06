using ZeroFormatter;
using System.Text;
using Godot;
using ByteSizeLib;

namespace Game.Networking
{
    public class NetworkStats
    {
        private int tempPackages = 0;
        public int packagesPerSecond = 0;
        private float tempTraffic = 0;
        public float NetOut = 0;
        private ulong lastTime = 0;
        public uint lastPackageAt = 0;

        public uint pingMs = 0;

        public void loop()
        {
            if ((OS.GetSystemTimeSecs() - lastTime) >= 1)
            {
                packagesPerSecond = tempPackages;
                NetOut = tempTraffic;
                tempPackages = 0;
                tempTraffic = 0;
                lastTime = OS.GetSystemTimeSecs();
            }
        }

        public void AddPackage(string message)
        {
            tempTraffic += System.Text.ASCIIEncoding.Unicode.GetByteCount(message);
            tempPackages++;
        }

        public string getNetOut()
        {
            return ByteSize.FromBytes(NetOut).ToString()  + "/s" ;
        }

    }
}

