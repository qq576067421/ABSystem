﻿using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Uzen.AB
{
    class AssetBundleUtils
    {
        public static DirectoryInfo AssetDir = new DirectoryInfo(Application.dataPath);
        public static string AssetPath = AssetDir.FullName;
        public static DirectoryInfo AssetBundlesDir = new DirectoryInfo(Application.dataPath + "/AssetBundles");
        public static string AssetBundlesPath = AssetBundlesDir.FullName;
        public static DirectoryInfo ProjectDir = AssetDir.Parent;
        public static string ProjectPath = ProjectDir.FullName;

        static Dictionary<int, AssetTarget> _object2target;
        static Dictionary<string, AssetTarget> _assetPath2target;
        static Dictionary<string, string> _fileHashCache;
        static Dictionary<string, string> _fileHashOld;

        public static void Init()
        {
            _object2target = new Dictionary<int, AssetTarget>();
            _assetPath2target = new Dictionary<string, AssetTarget>();
            _fileHashCache = new Dictionary<string, string>();
            _fileHashOld = new Dictionary<string, string>();
            LoadCache();
        }

        public static void ClearCache()
        {
            _object2target = null;
            _assetPath2target = null;
            _fileHashCache = null;
            _fileHashOld = null;
        }

        public static void LoadCache()
        {
            string cacheTxtFilePath = Path.Combine(AssetBundlesPath, "cache.txt");
            if (File.Exists(cacheTxtFilePath))
            {
                string value = File.ReadAllText(cacheTxtFilePath);
                StringReader sr = new StringReader(value);
                while (true)
                {
                    string path = sr.ReadLine();
                    if (path == null)
                        break;

                    string hash = sr.ReadLine();

                    _fileHashOld[path] = hash;
                }
            }
        }

        public static void SaveCache()
        {
            StringBuilder sb = new StringBuilder();

            foreach (AssetTarget target in _object2target.Values)
            {
                sb.AppendLine(target.assetPath);
                sb.AppendLine(target.GetHash());
            }
            File.WriteAllText(Path.Combine(AssetBundlesPath, "cache.txt"), sb.ToString());
        }

        public static string GetProjectPath(FileInfo fi)
        {
            string fullName = fi.FullName;
            int index = fullName.IndexOf("Assets");
            return fullName.Substring(index);
        }

        public static List<AssetTarget> GetAll()
        {
            return new List<AssetTarget>(_object2target.Values);
        }

        public static Object LoadAssetObject(FileInfo file)
        {
            string fullName = file.FullName;
            int index = fullName.IndexOf("Assets");
            string relave = fullName.Substring(index);
            Object o = AssetDatabase.LoadMainAssetAtPath(relave);
            return o;
        }

        public static AssetTarget Load(Object o)
        {
            AssetTarget target = null;
            if (o != null)
            {
                int instanceId = o.GetInstanceID();

                if (_object2target.ContainsKey(instanceId))
                {
                    target = _object2target[instanceId];
                }
                else
                {
                    string assetPath = AssetDatabase.GetAssetPath(o);
                    string key = assetPath;
                    //Builtin，内置素材，path为空
                    if (string.IsNullOrEmpty(assetPath))
                        key = string.Format("Builtin______{0}", o.name);
                    else
                        key = string.Format("{0}/{1}", assetPath, instanceId);

                    if (_assetPath2target.ContainsKey(key))
                    {
                        target = _assetPath2target[key];
                    }
                    else
                    {
                        if (assetPath.StartsWith("Resources"))
                        {
                            assetPath = string.Format("{0}/{1}.{2}", assetPath, o.name, o.GetType().Name);
                        }
                        FileInfo file = new FileInfo(Path.Combine(ProjectPath, assetPath));
                        target = new AssetTarget(o, file, assetPath);
                        _object2target[instanceId] = target;
                        _assetPath2target[key] = target;
                    }
                }
            }
            return target;
        }

        public static AssetTarget Load(FileInfo file, System.Type t)
        {
            AssetTarget target = null;
            string fullPath = file.FullName;
            int index = fullPath.IndexOf("Assets");
            if (index != -1)
            {
                string assetPath = fullPath.Substring(index);
                if (_assetPath2target.ContainsKey(assetPath))
                {
                    target = _assetPath2target[assetPath];
                }
                else
                {
                    Object o = null;
                    if (t == null)
                        o = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    else
                        o = AssetDatabase.LoadAssetAtPath(assetPath, t);

                    if (o != null)
                    {
                        int instanceId = o.GetInstanceID();

                        if (_object2target.ContainsKey(instanceId))
                        {
                            target = _object2target[instanceId];
                        }
                        else
                        {
                            target = new AssetTarget(o, file, assetPath);
                            string key = string.Format("{0}/{1}", assetPath, instanceId);
                            _assetPath2target[key] = target;
                            _object2target[instanceId] = target;
                        }
                    }
                }
            }

            return target;
        }

        public static AssetTarget Load(FileInfo file)
        {
            return Load(file, null);
        }

        public static string GetFileHash(string path, bool force = false)
        {
            string _hexStr = null;
            if (_fileHashCache.ContainsKey(path) && !force)
            {
                _hexStr = _fileHashCache[path];
            }
            else if (File.Exists(path) == false)
            {
                _hexStr = "FileNotExists";
            }
            else
            {
                HashAlgorithm ha = HashAlgorithm.Create();
                FileStream fs = new FileStream(path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                byte[] bytes = ha.ComputeHash(fs);
                _hexStr = ToHexString(bytes);
                _fileHashCache[path] = _hexStr;
                fs.Close();
            }
            
            return _hexStr;
        }

        public static string GetOldHash(string path)
        {
            if (_fileHashOld.ContainsKey(path))
                return _fileHashOld[path];
            return null;
        }

        public static string ToHexString(byte[] bytes)
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2"));
                }
                hexString = strB.ToString();
            }
            return hexString;
        }
    }
}