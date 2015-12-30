using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using Mono.Unix;

namespace NzbDrone.Mono
{
    public interface IProcMountProvider
    {
        List<IMount> GetMounts();
    }

    public class ProcMountProvider : IProcMountProvider
    {
        private static string[] _fixedTypes = "ext3,ext2,ext4,vfat,fuseblk,xfs,jfs,msdos,ntfs,minix,hfs,hfsplus,qnx4,ufs,btrfs".Split(',');
        private static string[] _networkDriveTypes = "cifs,nfs,nfs4,nfsd,sshfs".Split(',');

        private static Dictionary<string, bool> _fileSystems;

        private readonly Logger _logger;

        public ProcMountProvider(Logger logger)
        {
            _logger = logger;
        }

        public List<IMount> GetMounts()
        {
            if (File.Exists(@"/proc/mounts"))
            {
                var lines = File.ReadAllLines(@"/proc/mounts");

                return lines.Select(ParseLine).OfType<IMount>().ToList();
            }

            return new List<IMount>();
        }

        private static Dictionary<string, bool> GetFileSystems()
        {
            if (_fileSystems == null)
            {
                var result = new Dictionary<string, bool>();
                if (File.Exists(@"/proc/filesystems"))
                {
                    var lines = File.ReadAllLines(@"/proc/filesystems");

                    foreach (var line in lines)
                    {
                        var split = line.Split('\t');

                        result.Add(split[1], split[0] != "nodev");
                    }
                }
                else
                {
                    foreach (var type in _fixedTypes)
                    {
                        result.Add(type, true);
                    }
                }
                _fileSystems = result;
            }

            return _fileSystems;
        }

        private IMount ParseLine(string line)
        {
            var split = line.Split(' ');

            if (split.Length != 6)
            {
                _logger.Debug("Unable to parser /proc/mount line: {0}", line);
            }

            var name = split[0];
            var mount = split[1];
            var type = split[2];
            var options = ParseOptions(split[3]);

            var driveType = DriveType.Unknown;
            
            if (name.StartsWith("/dev/") || GetFileSystems().GetValueOrDefault(type, false))
            {
                // Not always fixed, but lets assume it.
                driveType = DriveType.Fixed;
            }

            if (_networkDriveTypes.Contains(type))
            {
                driveType = DriveType.Network;
            }

            if (type == "zfs")
            {
                driveType = DriveType.Fixed;
            }

            return new ProcMount(driveType, name, mount, type, options);
        }

        private Dictionary<string, string> ParseOptions(string options)
        {
            var result = new Dictionary<string, string>();

            foreach (var option in options.Split(','))
            {
                var split = option.Split(new[] { '=' }, 1);

                result.Add(split[0], split.Length == 2 ? split[1] : string.Empty);
            }

            return result;
        }
    }

    public class ProcMount : IMount
    {
        private readonly UnixDriveInfo _unixDriveInfo;

        public ProcMount(DriveType driveType, string name, string mount, string type, Dictionary<string, string> options)
        {
            DriveType = driveType;
            Name = name;
            RootDirectory = mount;
            DriveFormat = type;

            _unixDriveInfo = new UnixDriveInfo(mount);
        }

        public long AvailableFreeSpace
        {
            get { return _unixDriveInfo.AvailableFreeSpace; }
        }

        public string DriveFormat { get; private set; }

        public DriveType DriveType { get; private set; }

        public bool IsReady
        {
            get { return _unixDriveInfo.IsReady; }
        }

        public string Name { get; private set; }

        public string RootDirectory { get; private set; }

        public long TotalFreeSpace
        {
            get { return _unixDriveInfo.TotalFreeSpace; }
        }

        public long TotalSize
        {
            get { return _unixDriveInfo.TotalSize; }
        }

        public string VolumeLabel
        {
            get { return _unixDriveInfo.VolumeLabel; }
        }
    }

}
