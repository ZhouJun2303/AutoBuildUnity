using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Scripts_AOT.Utility
{
    public class MyVersion
    {
        public int Major;
        public int Minor;
        public int Patch;
        public MyVersion(int Major, int Minor, int Patch)
        {
            this.Major = Major;
            this.Minor = Minor;
            this.Patch = Patch;
        }
        public static bool operator >(MyVersion a, MyVersion b)
        {
            if (a.Major != b.Major) return a.Major > b.Major;
            if (a.Minor != b.Minor) return a.Minor > b.Minor;
            if (a.Patch != b.Patch) return a.Patch > b.Patch;
            return false;
        }

        public static bool operator <(MyVersion a, MyVersion b)
        {
            if (a.Major != b.Major) return a.Major < b.Major;
            if (a.Minor != b.Minor) return a.Minor < b.Minor;
            if (a.Patch != b.Patch) return a.Patch < b.Patch;
            return false;
        }
        public static bool operator ==(MyVersion a, MyVersion b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.Major == b.Major && a.Minor == b.Minor && a.Patch == b.Patch;
        }
        public static bool operator !=(MyVersion a, MyVersion b)
        {
            return !(a == b);
        }
        public override bool Equals(object o)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return VersionToString(this);
        }

        public static bool StringToVersion(string versionStr, out MyVersion version)
        {
            var strs = versionStr.Split('.');
            if (strs.Length != 3)
            {
                version = new MyVersion(0, 0, 0);
                return false;
            }
            int Major = 0;
            int Minor = 0;
            int Patch = 0;
            if (int.TryParse(strs[0], out Major) && int.TryParse(strs[1], out Minor) && int.TryParse(strs[2], out Patch))
            {
                version = new MyVersion(Major, Minor, Patch);
                return true;
            }

            version = new MyVersion(0, 0, 0);
            return false;
        }

        public static string VersionToString(MyVersion version)
        {
            if (version == null) return "null";
            return $"{version.Major}.{version.Minor}.{version.Patch}";
        }
    }

}
