using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Grasshopper.Kernel;

namespace QD_TeklaLink.Loader
{
    public static class QD_LoaderEngine
    {
        private static readonly object _syncLock = new object();
        private static bool _isLoaded;
        private static string _targetLoadedVersion;
        private static bool _isHooked;
        private static bool _isResolveHooked;

        public static bool IsLoaded
        {
            get
            {
                lock (_syncLock)
                {
                    return _isLoaded;
                }
            }
        }

        public static string LoadedVersion
        {
            get
            {
                lock (_syncLock)
                {
                    return _targetLoadedVersion;
                }
            }
        }

        public static void Initialize()
        {
            lock (_syncLock)
            {
                if (_isLoaded)
                    return;

                Log("[QD Loader] Initializing QD Dynamic Loader...");

                try
                {
                    // Hook ribbon layout to ensure harmonious subcategory panel sizing in Tab "QD"
                    QD_RibbonStyler.HookRibbon();

                    string detectedVersion = null;
                    string detectedBinPath = null;

                    // 1. Detect from running TeklaStructures OS processes (highest fidelity)
                    var processInfo = DetectFromProcesses();
                    if (processInfo.HasValue)
                    {
                        detectedVersion = processInfo.Value.Version;
                        detectedBinPath = processInfo.Value.BinPath;
                        Log($"[QD Loader] Detected running Tekla Structures {detectedVersion} at: {detectedBinPath}");
                    }

                    // 2. Fallback: Detect from currently loaded assemblies in AppDomain
                    if (string.IsNullOrEmpty(detectedVersion))
                    {
                        var appDomainInfo = DetectFromAppDomain();
                        if (appDomainInfo.HasValue)
                        {
                            detectedVersion = appDomainInfo.Value.Version;
                            detectedBinPath = appDomainInfo.Value.BinPath;
                            Log($"[QD Loader] Detected loaded Tekla assembly for version {detectedVersion} in AppDomain.");
                        }
                    }

                    // 3. Fallback: Fallback to installed version in Registry or default (2021)
                    if (string.IsNullOrEmpty(detectedVersion))
                    {
                        var regInfo = DetectFromRegistry();
                        if (regInfo.HasValue)
                        {
                            detectedVersion = regInfo.Value.Version;
                            detectedBinPath = regInfo.Value.BinPath;
                            Log($"[QD Loader] Detected installed Tekla {detectedVersion} from registry at: {detectedBinPath}");
                        }
                    }

                    // 4. If version detected, hook assembly resolve and load target DLL immediately
                    if (!string.IsNullOrEmpty(detectedVersion))
                    {
                        if (!string.IsNullOrEmpty(detectedBinPath))
                        {
                            AddTeklaAssemblyResolve(detectedBinPath);
                        }

                        LoadTargetAssembly(detectedVersion);
                    }
                    else
                    {
                        // 5. If not detected yet, subscribe to AssemblyLoad event for deferred loading
                        if (!_isHooked)
                        {
                            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
                            _isHooked = true;
                            Log("[QD Loader] Tekla Structures not detected yet. Listening for assembly load events...");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"[QD Loader] Initialization exception: {ex}");
                }
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            try
            {
                if (_isLoaded)
                    return;

                var assembly = args.LoadedAssembly;
                if (assembly == null)
                    return;

                string detectedVersion = TryExtractTeklaVersionFromAssembly(assembly);
                if (!string.IsNullOrEmpty(detectedVersion))
                {
                    lock (_syncLock)
                    {
                        if (_isHooked)
                        {
                            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
                            _isHooked = false;
                        }

                        string binPath = null;
                        try
                        {
                            if (!string.IsNullOrEmpty(assembly.Location))
                                binPath = Path.GetDirectoryName(assembly.Location);
                        }
                        catch { }

                        if (string.IsNullOrEmpty(binPath))
                        {
                            var proc = DetectFromProcesses();
                            if (proc.HasValue)
                                binPath = proc.Value.BinPath;
                        }

                        if (!string.IsNullOrEmpty(binPath))
                            AddTeklaAssemblyResolve(binPath);

                        LoadTargetAssembly(detectedVersion);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[QD Loader] OnAssemblyLoad exception: {ex.Message}");
            }
        }

        private static (string Version, string BinPath)? DetectFromProcesses()
        {
            try
            {
                var processes = Process.GetProcessesByName("TeklaStructures");
                if (processes != null && processes.Length > 0)
                {
                    foreach (var proc in processes)
                    {
                        try
                        {
                            var module = proc.MainModule;
                            if (module != null && !string.IsNullOrEmpty(module.FileName))
                            {
                                string binPath = Path.GetDirectoryName(module.FileName);
                                var fvi = module.FileVersionInfo;
                                if (fvi != null)
                                {
                                    if (fvi.FileMajorPart >= 2016 && fvi.FileMajorPart <= 2035)
                                        return (fvi.FileMajorPart.ToString(), binPath);

                                    if (fvi.ProductMajorPart >= 2016 && fvi.ProductMajorPart <= 2035)
                                        return (fvi.ProductMajorPart.ToString(), binPath);

                                    string verStr = fvi.ProductVersion ?? fvi.FileVersion ?? string.Empty;
                                    var matchVer = Regex.Match(verStr, @"\b(20[2-3][0-9])\b");
                                    if (matchVer.Success)
                                        return (matchVer.Groups[1].Value, binPath);
                                }

                                var matchPath = Regex.Match(module.FileName, @"Tekla Structures[\\/](20[2-3][0-9])", RegexOptions.IgnoreCase);
                                if (matchPath.Success)
                                    return (matchPath.Groups[1].Value, binPath);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return null;
        }

        private static (string Version, string BinPath)? DetectFromAppDomain()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    string ver = TryExtractTeklaVersionFromAssembly(asm);
                    if (!string.IsNullOrEmpty(ver))
                    {
                        string bin = null;
                        try
                        {
                            if (!string.IsNullOrEmpty(asm.Location))
                                bin = Path.GetDirectoryName(asm.Location);
                        }
                        catch { }
                        return (ver, bin);
                    }
                }
            }
            catch { }
            return null;
        }

        private static (string Version, string BinPath)? DetectFromRegistry()
        {
            // Probe common recent versions descending
            string[] candidateYears = { "2026", "2025", "2024", "2023", "2022", "2021", "2020" };
            foreach (var year in candidateYears)
            {
                try
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Trimble\Tekla Structures\{year}.0\setup"))
                    {
                        if (key != null)
                        {
                            var mainDir = key.GetValue("MainDir") as string;
                            if (!string.IsNullOrEmpty(mainDir))
                            {
                                string candidateNt = Path.Combine(mainDir, "nt", "bin");
                                string candidateBin = Path.Combine(mainDir, "bin");
                                if (Directory.Exists(candidateNt))
                                    return (year, candidateNt);
                                if (Directory.Exists(candidateBin))
                                    return (year, candidateBin);
                            }
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        private static string TryExtractTeklaVersionFromAssembly(Assembly assembly)
        {
            try
            {
                if (assembly == null)
                    return null;

                var asmName = assembly.GetName();
                string name = asmName?.Name;
                if (string.IsNullOrEmpty(name))
                    return null;

                if (name.StartsWith("Tekla.Structures", StringComparison.OrdinalIgnoreCase))
                {
                    var ver = asmName.Version;
                    if (ver != null && ver.Major >= 2016 && ver.Major <= 2035)
                        return ver.Major.ToString();

                    var match = Regex.Match(name, @"\b(20[2-3][0-9])\b");
                    if (match.Success)
                        return match.Groups[1].Value;
                }

                if (name.StartsWith("GrasshopperTekla", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(name, @"\b(20[2-3][0-9])\b");
                    if (match.Success)
                        return match.Groups[1].Value;

                    var ver = asmName.Version;
                    if (ver != null && ver.Major >= 2016 && ver.Major <= 2035)
                        return ver.Major.ToString();
                }
            }
            catch { }
            return null;
        }

        private static void AddTeklaAssemblyResolve(string tsBinPath)
        {
            if (_isResolveHooked || string.IsNullOrEmpty(tsBinPath) || !Directory.Exists(tsBinPath))
                return;

            _isResolveHooked = true;
            var probeDirs = new[]
            {
                tsBinPath,
                Path.Combine(tsBinPath, "plugins"),
                Path.Combine(tsBinPath, "dialogs"),
                Path.Combine(tsBinPath, "Net48Runtime")
            };

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                try
                {
                    var requestedName = new AssemblyName(args.Name).Name;
                    if (string.IsNullOrEmpty(requestedName) || requestedName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                        return null;

                    foreach (var dir in probeDirs)
                    {
                        if (!Directory.Exists(dir))
                            continue;

                        string candidate = Path.Combine(dir, requestedName + ".dll");
                        if (File.Exists(candidate))
                        {
                            return Assembly.LoadFrom(candidate);
                        }
                    }
                }
                catch { }
                return null;
            };

            Log($"[QD Loader] Registered Tekla assembly resolver probing: {tsBinPath}");
        }

        public static void LoadTargetAssembly(string version)
        {
            if (string.IsNullOrEmpty(version))
                return;

            lock (_syncLock)
            {
                if (_isLoaded)
                    return;

                try
                {
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string targetFolder = Path.Combine(appData, "Grasshopper", "QD_TeklaLink");
                    if (!Directory.Exists(targetFolder))
                    {
                        Log($"[QD Loader] Directory does not exist: {targetFolder}");
                        return;
                    }

                    string filePatternUnderscore = Path.Combine(targetFolder, $"QD_G-TDrawingLink_{version}.dll");
                    string filePatternDot = Path.Combine(targetFolder, $"QD_G-TDrawingLink.{version}.dll");

                    string targetDll = null;
                    if (File.Exists(filePatternUnderscore))
                        targetDll = filePatternUnderscore;
                    else if (File.Exists(filePatternDot))
                        targetDll = filePatternDot;

                    if (targetDll == null)
                    {
                        Log($"[QD Loader] Matching DLL for Tekla {version} not found in: {targetFolder} (expected QD_G-TDrawingLink_{version}.dll)");
                        return;
                    }

                    var cs = Grasshopper.Instances.ComponentServer;
                    if (cs == null)
                    {
                        Log("[QD Loader] Grasshopper.Instances.ComponentServer is null. Cannot load assembly.");
                        return;
                    }

                    // Look up GH_ComponentServer.LoadGHA(GH_ExternalFile file, bool loadOneByOne)
                    var loadGhaMethod = typeof(Grasshopper.Kernel.GH_ComponentServer).GetMethod(
                        "LoadGHA",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    if (loadGhaMethod != null)
                    {
                        var externalFile = new GH_ExternalFile(targetDll);
                        object result = loadGhaMethod.Invoke(cs, new object[] { externalFile, false });

                        _isLoaded = true;
                        _targetLoadedVersion = version;

                        Log($"[QD Loader] LoadGHA invoked for Tekla {version} from {targetDll}. Result: {result}");

                        TryRefreshRibbon();
                    }
                    else
                    {
                        Log("[QD Loader] Could not find LoadGHA method on GH_ComponentServer.");
                    }
                }
                catch (Exception ex)
                {
                    Log($"[QD Loader] Error loading QD_G-TDrawingLink for Tekla {version}: {ex}");
                }
            }
        }

        private static void TryRefreshRibbon()
        {
            try
            {
                var docEditor = Grasshopper.Instances.DocumentEditor;
                if (docEditor != null)
                {
                    var ribbonProp = docEditor.GetType().GetProperty("Ribbon", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var ribbon = ribbonProp?.GetValue(docEditor, null);
                    if (ribbon != null)
                    {
                        var populateMethod = ribbon.GetType().GetMethod("PopulateRibbon", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (populateMethod != null)
                        {
                            if (docEditor.InvokeRequired)
                            {
                                docEditor.BeginInvoke(new Action(() =>
                                {
                                    try 
                                    { 
                                        populateMethod.Invoke(ribbon, null); 
                                        QD_RibbonStyler.FormatQdRibbonPanels();
                                    } 
                                    catch { }
                                }));
                            }
                            else
                            {
                                populateMethod.Invoke(ribbon, null);
                                QD_RibbonStyler.FormatQdRibbonPanels();
                            }
                            Log("[QD Loader] Canvas Ribbon populated and styled.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[QD Loader] TryRefreshRibbon exception: {ex.Message}");
            }
        }

        internal static void Log(string message)
        {
            string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string logFolder = Path.Combine(appData, "Grasshopper", "QD_TeklaLink");
                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);

                string logPath = Path.Combine(logFolder, "loader.log");
                File.AppendAllText(logPath, formatted + Environment.NewLine);
            }
            catch { }

            try
            {
                Debug.WriteLine(formatted);
            }
            catch { }

            try
            {
                LogToRhinoApp(formatted);
            }
            catch { }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void LogToRhinoApp(string formatted)
        {
            Rhino.RhinoApp.WriteLine(formatted);
        }
    }
}
