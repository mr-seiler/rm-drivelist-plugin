﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rainmeter;

namespace PluginDriveList
{
    internal class Measure
    {

        internal List<DriveType> listedTypes;
        internal DriveInfo[] allDrives;
        internal List<string> listedDrives;
        internal int currentIndex;

        internal Measure()
        {
            listedTypes = new List<DriveType> { DriveType.Fixed, DriveType.Network, DriveType.Ram };
            listedDrives = new List<string>();
            currentIndex = 0;
        }

        internal void Reload(Rainmeter.API rm, ref double maxValue)
        {
            bool listOptical = (rm.ReadInt("Optical", 0) == 1 ? true : false);
            bool listRemovable = (rm.ReadInt("Removable", 0) == 1 ? true : false);
            bool listNetwork = (rm.ReadInt("Network", 0) == 1 ? true : false);

            updateTypeList(listOptical, listRemovable, listNetwork);

            allDrives = DriveInfo.GetDrives();
        }

        internal double Update()
        {
            DriveInfo[] newDrives = DriveInfo.GetDrives();
            
            if (!newDrives.Equals(allDrives)) // connected drives have changed somehow, regenerate list
            {
                allDrives = newDrives;
                updateListedDrives();
            }

            if (listedDrives.Count < 1)
                API.Log(API.LogType.Error, "DriveList: no drives?!");

            return (double)listedDrives.Count;
        }

        internal string GetString()
        {
            try
            {
                return listedDrives[currentIndex];
            }
            catch
            {
#if DEBUG
                API.Log(API.LogType.Warning, "Waiting on a valid index");
#endif
                return "C:\\";
            }
        }

        internal void ExecuteBang(string args)
        {
            switch (args.ToLowerInvariant())
            {
                case "forward":
                    currentIndex = ((currentIndex + 1) % listedDrives.Count);
                    break;
                case "backward":
                    currentIndex = (int)( (currentIndex - 1) - ( Math.Floor( (currentIndex - 1D) / listedDrives.Count ) * listedDrives.Count ) );
                    break;
                default:
                    API.Log(API.LogType.Error, "DriveList: Invalid command \"" + args + "\"");
                    break;
            }
        }

        private void updateTypeList(bool incOpt, bool incRem, bool incNet)
        {
            bool already = listedTypes.Contains(DriveType.CDRom);
            if (incOpt && !already)
            {
                listedTypes.Add(DriveType.CDRom);
            }
            else if (!incOpt && already)
            {
                listedTypes.Remove(DriveType.CDRom);
            }

            already = listedTypes.Contains(DriveType.Removable);
            if (incRem && !already)
            {
                listedTypes.Add(DriveType.Removable);
            }
            else if (!incOpt && already)
            {
                listedTypes.Remove(DriveType.Removable);
            }

            already = listedTypes.Contains(DriveType.Network);
            if (incNet && !already)
            {
                listedTypes.Add(DriveType.Network);
            }
            else if (!incNet && already)
            {
                listedTypes.Remove(DriveType.Network);
            }
        }

        private void updateListedDrives()
        {
            listedDrives.Clear();
            foreach (DriveInfo d in allDrives)
            {
#if DEBUG
                API.Log(API.LogType.Debug, "Found drive " + d.Name);
#endif
                if (d.IsReady && listedTypes.Contains(d.DriveType))
                {
                    listedDrives.Add(d.Name);
#if DEBUG
                        API.Log(API.LogType.Debug, "Added drive " + d.Name);
#endif
                }
            }

            if (currentIndex >= listedDrives.Count)
            {
                currentIndex = listedDrives.Count - 1;
            }
        }

    }

    public static class Plugin
    {
        [DllExport]
        public unsafe static void Initialize(void** data, void* rm)
        {
            uint id = (uint)((void*)*data);
            Measures.Add(id, new Measure());
        }

        [DllExport]
        public unsafe static void Finalize(void* data)
        {
            uint id = (uint)data;
            Measures.Remove(id);
        }

        [DllExport]
        public unsafe static void Reload(void* data, void* rm, double* maxValue)
        {
            uint id = (uint)data;
            Measures[id].Reload(new Rainmeter.API((IntPtr)rm), ref *maxValue);
        }

        [DllExport]
        public unsafe static double Update(void* data)
        {
            uint id = (uint)data;
            return Measures[id].Update();
        }

        [DllExport]
        public unsafe static char* GetString(void* data)
        {
            uint id = (uint)data;
            fixed (char* s = Measures[id].GetString()) return s;
        }

        [DllExport]
        public unsafe static void ExecuteBang(void* data, char* args)
        {
            uint id = (uint)data;
            Measures[id].ExecuteBang(new string(args));
        }

        internal static Dictionary<uint, Measure> Measures = new Dictionary<uint, Measure>();
    }
}
