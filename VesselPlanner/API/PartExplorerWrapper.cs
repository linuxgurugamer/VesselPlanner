using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PartExplorerAPI
{
    /// <summary>
    /// Source-only wrapper for the optional PartExplorer API.
    ///
    /// Copy this file into another mod project to call PartExplorer without adding
    /// a compile-time reference to PartExplorer.dll. The consumer project only
    /// needs the normal KSP assemblies that define AvailablePart and Part.
    /// </summary>
    public static class PartExplorerWrapper
    {
        private const string PartExplorerAssemblyName = "PartExplorer";
        private const string PartExplorerAddonTypeName = "PartExplorer.PartExplorerAddon";

        private static Assembly partExplorerAssembly;
        private static Type addonType;
        private static PropertyInfo instanceProperty;

        private static PropertyInfo apiVersionProperty;
        private static PropertyInfo partExplorerVersionProperty;
        private static PropertyInfo isOpenProperty;
        private static PropertyInfo isRestrictedProperty;
        private static PropertyInfo restrictedPartCountProperty;
        private static PropertyInfo restrictionSourceTitleProperty;
        private static PropertyInfo isPickerActiveProperty;

        private static MethodInfo openMethod;
        private static MethodInfo closeMethod;
        private static MethodInfo showAvailablePartsMethod;
        private static MethodInfo showAvailablePartsWithTitleMethod;
        private static MethodInfo showLivePartsMethod;
        private static MethodInfo showLivePartsWithTitleMethod;
        private static MethodInfo showNamedPartsMethod;
        private static MethodInfo showNamedPartsWithTitleMethod;
        private static MethodInfo showAvailablePartMethod;
        private static MethodInfo showLivePartMethod;
        private static MethodInfo showNamedPartMethod;
        private static MethodInfo compareAvailablePartsMethod;
        private static MethodInfo compareAvailablePartsWithTitleMethod;
        private static MethodInfo compareLivePartsMethod;
        private static MethodInfo compareLivePartsWithTitleMethod;
        private static MethodInfo compareNamedPartsMethod;
        private static MethodInfo compareNamedPartsWithTitleMethod;
        private static MethodInfo pickAvailablePartsMethod;
        private static MethodInfo pickAvailablePartsWithTitleMethod;
        private static MethodInfo pickAvailablePartsFullMethod;
        private static MethodInfo pickLivePartsMethod;
        private static MethodInfo pickLivePartsWithTitleMethod;
        private static MethodInfo pickLivePartsFullMethod;
        private static MethodInfo pickNamedPartsMethod;
        private static MethodInfo pickNamedPartsWithTitleMethod;
        private static MethodInfo pickNamedPartsFullMethod;
        private static MethodInfo cancelPartPickerMethod;
        private static MethodInfo clearRestrictionMethod;

        /// <summary>Last wrapper error. Empty after the most recent successful API call.</summary>
        public static string LastError { get; private set; } = string.Empty;

        /// <summary>True when PartExplorer.dll is loaded and exposes the basic public API.</summary>
        public static bool IsInstalled { get { return ResolveApi(); } }

        /// <summary>True when the PartExplorer API and persistent addon instance are ready.</summary>
        public static bool IsAvailable { get { return ResolveApi() && GetInstance() != null; } }

        /// <summary>
        /// PartExplorer integration API version. Version 1 is returned for older
        /// PartExplorer builds that expose the original ShowParts API but no explicit
        /// ApiVersion property. Zero means PartExplorer could not be resolved.
        /// </summary>
        public static int ApiVersion
        {
            get
            {
                if (!ResolveApi())
                    return 0;
                if (apiVersionProperty == null)
                    return 1;
                try { return Convert.ToInt32(apiVersionProperty.GetValue(null, null)); }
                catch { return 1; }
            }
        }

        /// <summary>Installed PartExplorer version, or an empty string if unavailable.</summary>
        public static string PartExplorerVersion
        {
            get
            {
                if (!ResolveApi())
                    return string.Empty;
                try
                {
                    if (partExplorerVersionProperty != null)
                        return Convert.ToString(partExplorerVersionProperty.GetValue(null, null)) ?? string.Empty;
                    return partExplorerAssembly == null || partExplorerAssembly.GetName().Version == null
                        ? string.Empty
                        : partExplorerAssembly.GetName().Version.ToString();
                }
                catch { return string.Empty; }
            }
        }

        /// <summary>True when the main PartExplorer window is currently open.</summary>
        public static bool IsOpen { get { return ReadInstanceProperty(isOpenProperty, false); } }

        /// <summary>True while an external API caller is restricting the Parts universe.</summary>
        public static bool IsRestricted { get { return ReadInstanceProperty(isRestrictedProperty, false); } }

        /// <summary>Number of unique parts in the current external restriction.</summary>
        public static int RestrictedPartCount { get { return ReadInstanceProperty(restrictedPartCountProperty, 0); } }

        /// <summary>Optional title supplied for the current external restriction.</summary>
        public static string RestrictionSourceTitle { get { return ReadInstanceProperty(restrictionSourceTitleProperty, string.Empty); } }

        /// <summary>True while PartExplorer is acting as a part picker for another mod.</summary>
        public static bool IsPickerActive { get { return ReadInstanceProperty(isPickerActiveProperty, false); } }

        /// <summary>Opens PartExplorer without changing its current page or restriction.</summary>
        public static bool Open() { return InvokeNoArgs(openMethod, "Open()", 2); }

        /// <summary>Closes PartExplorer and its child windows.</summary>
        public static bool Close() { return InvokeNoArgs(closeMethod, "Close()", 2); }

        public static bool ShowParts(IEnumerable<AvailablePart> parts)
        {
            return InvokeOneArg(showAvailablePartsMethod, parts, "ShowParts(IEnumerable<AvailablePart>)");
        }

        public static bool ShowParts(IEnumerable<AvailablePart> parts, string sourceTitle)
        {
            if (!ResolveApi()) return false;
            if (showAvailablePartsWithTitleMethod != null)
                return Invoke(showAvailablePartsWithTitleMethod, new object[] { parts, sourceTitle }, "ShowParts(IEnumerable<AvailablePart>, string)");
            return ShowParts(parts);
        }

        public static bool ShowParts(IEnumerable<Part> parts)
        {
            return InvokeOneArg(showLivePartsMethod, parts, "ShowParts(IEnumerable<Part>)");
        }

        public static bool ShowParts(IEnumerable<Part> parts, string sourceTitle)
        {
            if (!ResolveApi()) return false;
            if (showLivePartsWithTitleMethod != null)
                return Invoke(showLivePartsWithTitleMethod, new object[] { parts, sourceTitle }, "ShowParts(IEnumerable<Part>, string)");
            return ShowParts(parts);
        }

        /// <summary>Shows parts identified by KSP internal part name or partUrl.</summary>
        public static bool ShowParts(IEnumerable<string> partNames)
        {
            return InvokeOneArgRequiredApi(showNamedPartsMethod, partNames, "ShowParts(IEnumerable<string>)", 2);
        }

        /// <summary>Shows named parts and displays an optional caller/source title.</summary>
        public static bool ShowParts(IEnumerable<string> partNames, string sourceTitle)
        {
            return InvokeRequiredApi(showNamedPartsWithTitleMethod, new object[] { partNames, sourceTitle }, "ShowParts(IEnumerable<string>, string)", 2);
        }

        public static bool ShowPart(AvailablePart part)
        {
            if (!ResolveApi()) return false;
            if (showAvailablePartMethod != null)
                return Invoke(showAvailablePartMethod, new object[] { part }, "ShowPart(AvailablePart)");
            return ShowParts(part == null ? null : new[] { part });
        }

        public static bool ShowPart(Part part)
        {
            if (!ResolveApi()) return false;
            if (showLivePartMethod != null)
                return Invoke(showLivePartMethod, new object[] { part }, "ShowPart(Part)");
            return ShowParts(part == null ? null : new[] { part });
        }

        public static bool ShowPart(string partName)
        {
            if (!ResolveApi()) return false;
            if (showNamedPartMethod != null)
                return Invoke(showNamedPartMethod, new object[] { partName }, "ShowPart(string)");
            return ShowParts(string.IsNullOrEmpty(partName) ? null : new[] { partName });
        }

        public static bool CompareParts(IEnumerable<AvailablePart> parts)
        {
            return InvokeOneArgRequiredApi(compareAvailablePartsMethod, parts, "CompareParts(IEnumerable<AvailablePart>)", 2);
        }

        public static bool CompareParts(IEnumerable<AvailablePart> parts, string sourceTitle)
        {
            return InvokeRequiredApi(compareAvailablePartsWithTitleMethod, new object[] { parts, sourceTitle }, "CompareParts(IEnumerable<AvailablePart>, string)", 2);
        }

        public static bool CompareParts(IEnumerable<Part> parts)
        {
            return InvokeOneArgRequiredApi(compareLivePartsMethod, parts, "CompareParts(IEnumerable<Part>)", 2);
        }

        public static bool CompareParts(IEnumerable<Part> parts, string sourceTitle)
        {
            return InvokeRequiredApi(compareLivePartsWithTitleMethod, new object[] { parts, sourceTitle }, "CompareParts(IEnumerable<Part>, string)", 2);
        }

        public static bool CompareParts(IEnumerable<string> partNames)
        {
            return InvokeOneArgRequiredApi(compareNamedPartsMethod, partNames, "CompareParts(IEnumerable<string>)", 2);
        }

        public static bool CompareParts(IEnumerable<string> partNames, string sourceTitle)
        {
            return InvokeRequiredApi(compareNamedPartsWithTitleMethod, new object[] { partNames, sourceTitle }, "CompareParts(IEnumerable<string>, string)", 2);
        }

        /// <summary>Opens PartExplorer as a picker over AvailablePart candidates.</summary>
        public static bool PickPart(IEnumerable<AvailablePart> parts, Action<AvailablePart> onSelected)
        {
            return InvokeRequiredApi(
                pickAvailablePartsMethod,
                new object[] { parts, onSelected },
                "PickPart(IEnumerable<AvailablePart>, Action<AvailablePart>)", 3);
        }

        public static bool PickPart(
            IEnumerable<AvailablePart> parts,
            string sourceTitle,
            Action<AvailablePart> onSelected)
        {
            return InvokeRequiredApi(
                pickAvailablePartsWithTitleMethod,
                new object[] { parts, sourceTitle, onSelected },
                "PickPart(IEnumerable<AvailablePart>, string, Action<AvailablePart>)", 3);
        }

        public static bool PickPart(
            IEnumerable<AvailablePart> parts,
            string sourceTitle,
            Action<AvailablePart> onSelected,
            Action onCancelled)
        {
            return InvokeRequiredApi(
                pickAvailablePartsFullMethod,
                new object[] { parts, sourceTitle, onSelected, onCancelled },
                "PickPart(IEnumerable<AvailablePart>, string, Action<AvailablePart>, Action)", 3);
        }

        /// <summary>Picker overload for live KSP Part candidates.</summary>
        public static bool PickPart(IEnumerable<Part> parts, Action<AvailablePart> onSelected)
        {
            return InvokeRequiredApi(
                pickLivePartsMethod,
                new object[] { parts, onSelected },
                "PickPart(IEnumerable<Part>, Action<AvailablePart>)", 3);
        }

        public static bool PickPart(
            IEnumerable<Part> parts,
            string sourceTitle,
            Action<AvailablePart> onSelected)
        {
            return InvokeRequiredApi(
                pickLivePartsWithTitleMethod,
                new object[] { parts, sourceTitle, onSelected },
                "PickPart(IEnumerable<Part>, string, Action<AvailablePart>)", 3);
        }

        public static bool PickPart(
            IEnumerable<Part> parts,
            string sourceTitle,
            Action<AvailablePart> onSelected,
            Action onCancelled)
        {
            return InvokeRequiredApi(
                pickLivePartsFullMethod,
                new object[] { parts, sourceTitle, onSelected, onCancelled },
                "PickPart(IEnumerable<Part>, string, Action<AvailablePart>, Action)", 3);
        }

        /// <summary>Picker overload for KSP internal part names or partUrl identifiers.</summary>
        public static bool PickPart(IEnumerable<string> partNames, Action<AvailablePart> onSelected)
        {
            return InvokeRequiredApi(
                pickNamedPartsMethod,
                new object[] { partNames, onSelected },
                "PickPart(IEnumerable<string>, Action<AvailablePart>)", 3);
        }

        public static bool PickPart(
            IEnumerable<string> partNames,
            string sourceTitle,
            Action<AvailablePart> onSelected)
        {
            return InvokeRequiredApi(
                pickNamedPartsWithTitleMethod,
                new object[] { partNames, sourceTitle, onSelected },
                "PickPart(IEnumerable<string>, string, Action<AvailablePart>)", 3);
        }

        public static bool PickPart(
            IEnumerable<string> partNames,
            string sourceTitle,
            Action<AvailablePart> onSelected,
            Action onCancelled)
        {
            return InvokeRequiredApi(
                pickNamedPartsFullMethod,
                new object[] { partNames, sourceTitle, onSelected, onCancelled },
                "PickPart(IEnumerable<string>, string, Action<AvailablePart>, Action)", 3);
        }

        /// <summary>Cancels an active PartExplorer picker and invokes its cancellation callback.</summary>
        public static bool CancelPartPicker()
        {
            return InvokeNoArgs(cancelPartPickerMethod, "CancelPartPicker()", 3);
        }

        /// <summary>Removes an external ShowParts/CompareParts/PickPart restriction.</summary>
        public static bool ClearPartListRestriction()
        {
            return InvokeNoArgs(clearRestrictionMethod, "ClearPartListRestriction()", 1);
        }

        /// <summary>Clears cached reflection metadata, mainly for test/reload cases.</summary>
        public static void Reset()
        {
            partExplorerAssembly = null;
            addonType = null;
            instanceProperty = null;
            apiVersionProperty = null;
            partExplorerVersionProperty = null;
            isOpenProperty = null;
            isRestrictedProperty = null;
            restrictedPartCountProperty = null;
            restrictionSourceTitleProperty = null;
            isPickerActiveProperty = null;
            openMethod = null;
            closeMethod = null;
            showAvailablePartsMethod = null;
            showAvailablePartsWithTitleMethod = null;
            showLivePartsMethod = null;
            showLivePartsWithTitleMethod = null;
            showNamedPartsMethod = null;
            showNamedPartsWithTitleMethod = null;
            showAvailablePartMethod = null;
            showLivePartMethod = null;
            showNamedPartMethod = null;
            compareAvailablePartsMethod = null;
            compareAvailablePartsWithTitleMethod = null;
            compareLivePartsMethod = null;
            compareLivePartsWithTitleMethod = null;
            compareNamedPartsMethod = null;
            compareNamedPartsWithTitleMethod = null;
            pickAvailablePartsMethod = null;
            pickAvailablePartsWithTitleMethod = null;
            pickAvailablePartsFullMethod = null;
            pickLivePartsMethod = null;
            pickLivePartsWithTitleMethod = null;
            pickLivePartsFullMethod = null;
            pickNamedPartsMethod = null;
            pickNamedPartsWithTitleMethod = null;
            pickNamedPartsFullMethod = null;
            cancelPartPickerMethod = null;
            clearRestrictionMethod = null;
            LastError = string.Empty;
        }

        private static bool ResolveApi()
        {
            if (addonType != null && instanceProperty != null &&
                showAvailablePartsMethod != null && showLivePartsMethod != null &&
                clearRestrictionMethod != null)
                return true;

            Reset();
            try
            {
                partExplorerAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
                    string.Equals(a.GetName().Name, PartExplorerAssemblyName, StringComparison.OrdinalIgnoreCase));
                if (partExplorerAssembly == null)
                    return Fail("PartExplorer.dll is not loaded.");

                addonType = partExplorerAssembly.GetType(PartExplorerAddonTypeName, false);
                if (addonType == null)
                    return Fail("PartExplorer.PartExplorerAddon was not found.");

                BindingFlags pubStatic = BindingFlags.Public | BindingFlags.Static;
                BindingFlags pubInstance = BindingFlags.Public | BindingFlags.Instance;
                instanceProperty = addonType.GetProperty("Instance", pubStatic);
                apiVersionProperty = addonType.GetProperty("ApiVersion", pubStatic);
                partExplorerVersionProperty = addonType.GetProperty("PartExplorerVersion", pubStatic);
                isOpenProperty = addonType.GetProperty("IsOpen", pubInstance);
                isRestrictedProperty = addonType.GetProperty("IsRestricted", pubInstance);
                restrictedPartCountProperty = addonType.GetProperty("RestrictedPartCount", pubInstance);
                restrictionSourceTitleProperty = addonType.GetProperty("RestrictionSourceTitle", pubInstance);
                isPickerActiveProperty = addonType.GetProperty("IsPickerActive", pubInstance);

                openMethod = GetMethod("Open", Type.EmptyTypes);
                closeMethod = GetMethod("Close", Type.EmptyTypes);
                showAvailablePartsMethod = GetMethod("ShowParts", new[] { typeof(IEnumerable<AvailablePart>) });
                showAvailablePartsWithTitleMethod = GetMethod("ShowParts", new[] { typeof(IEnumerable<AvailablePart>), typeof(string) });
                showLivePartsMethod = GetMethod("ShowParts", new[] { typeof(IEnumerable<Part>) });
                showLivePartsWithTitleMethod = GetMethod("ShowParts", new[] { typeof(IEnumerable<Part>), typeof(string) });
                showNamedPartsMethod = GetMethod("ShowParts", new[] { typeof(IEnumerable<string>) });
                showNamedPartsWithTitleMethod = GetMethod("ShowParts", new[] { typeof(IEnumerable<string>), typeof(string) });
                showAvailablePartMethod = GetMethod("ShowPart", new[] { typeof(AvailablePart) });
                showLivePartMethod = GetMethod("ShowPart", new[] { typeof(Part) });
                showNamedPartMethod = GetMethod("ShowPart", new[] { typeof(string) });
                compareAvailablePartsMethod = GetMethod("CompareParts", new[] { typeof(IEnumerable<AvailablePart>) });
                compareAvailablePartsWithTitleMethod = GetMethod("CompareParts", new[] { typeof(IEnumerable<AvailablePart>), typeof(string) });
                compareLivePartsMethod = GetMethod("CompareParts", new[] { typeof(IEnumerable<Part>) });
                compareLivePartsWithTitleMethod = GetMethod("CompareParts", new[] { typeof(IEnumerable<Part>), typeof(string) });
                compareNamedPartsMethod = GetMethod("CompareParts", new[] { typeof(IEnumerable<string>) });
                compareNamedPartsWithTitleMethod = GetMethod("CompareParts", new[] { typeof(IEnumerable<string>), typeof(string) });

                Type availablePartCallback = typeof(Action<AvailablePart>);
                Type cancelCallback = typeof(Action);
                pickAvailablePartsMethod = GetMethod("PickPart", new[] { typeof(IEnumerable<AvailablePart>), availablePartCallback });
                pickAvailablePartsWithTitleMethod = GetMethod("PickPart", new[] { typeof(IEnumerable<AvailablePart>), typeof(string), availablePartCallback });
                pickAvailablePartsFullMethod = GetMethod("PickPart", new[] { typeof(IEnumerable<AvailablePart>), typeof(string), availablePartCallback, cancelCallback });
                pickLivePartsMethod = GetMethod("PickPart", new[] { typeof(IEnumerable<Part>), availablePartCallback });
                pickLivePartsWithTitleMethod = GetMethod("PickPart", new[] { typeof(IEnumerable<Part>), typeof(string), availablePartCallback });
                pickLivePartsFullMethod = GetMethod("PickPart", new[] { typeof(IEnumerable<Part>), typeof(string), availablePartCallback, cancelCallback });
                pickNamedPartsMethod = GetMethod("PickPart", new[] { typeof(IEnumerable<string>), availablePartCallback });
                pickNamedPartsWithTitleMethod = GetMethod("PickPart", new[] { typeof(IEnumerable<string>), typeof(string), availablePartCallback });
                pickNamedPartsFullMethod = GetMethod("PickPart", new[] { typeof(IEnumerable<string>), typeof(string), availablePartCallback, cancelCallback });
                cancelPartPickerMethod = GetMethod("CancelPartPicker", Type.EmptyTypes);
                clearRestrictionMethod = GetMethod("ClearPartListRestriction", Type.EmptyTypes);

                if (instanceProperty == null || showAvailablePartsMethod == null ||
                    showLivePartsMethod == null || clearRestrictionMethod == null)
                {
                    Reset();
                    return Fail("The loaded PartExplorer version does not expose the expected basic API.");
                }

                LastError = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                Reset();
                return Fail("Unable to initialize the PartExplorer API wrapper: " + GetBaseMessage(ex));
            }
        }

        private static MethodInfo GetMethod(string name, Type[] parameters)
        {
            return addonType == null ? null : addonType.GetMethod(
                name, BindingFlags.Public | BindingFlags.Instance, null, parameters, null);
        }

        private static object GetInstance()
        {
            if (!ResolveApi())
                return null;
            try { return instanceProperty.GetValue(null, null); }
            catch (Exception ex)
            {
                Fail("Unable to read PartExplorerAddon.Instance: " + GetBaseMessage(ex));
                return null;
            }
        }

        private static T ReadInstanceProperty<T>(PropertyInfo property, T fallback)
        {
            if (!ResolveApi() || property == null)
                return fallback;
            object instance = GetInstance();
            if (instance == null)
                return fallback;
            try
            {
                object value = property.GetValue(instance, null);
                return value == null ? fallback : (T)Convert.ChangeType(value, typeof(T));
            }
            catch { return fallback; }
        }

        private static bool InvokeNoArgs(MethodInfo method, string description, int minimumApiVersion)
        {
            return InvokeRequiredApi(method, null, description, minimumApiVersion);
        }

        private static bool InvokeOneArg(MethodInfo method, object arg, string description)
        {
            if (!ResolveApi()) return false;
            return Invoke(method, new[] { arg }, description);
        }

        private static bool InvokeOneArgRequiredApi(MethodInfo method, object arg, string description, int minimumApiVersion)
        {
            return InvokeRequiredApi(method, new[] { arg }, description, minimumApiVersion);
        }

        private static bool InvokeRequiredApi(MethodInfo method, object[] args, string description, int minimumApiVersion)
        {
            if (!ResolveApi())
                return false;
            if (method == null || ApiVersion < minimumApiVersion)
                return Fail(description + " requires PartExplorer API version " + minimumApiVersion + " or later.");
            return Invoke(method, args, description);
        }

        private static bool Invoke(MethodInfo method, object[] args, string description)
        {
            if (!ResolveApi())
                return false;
            if (method == null)
                return Fail(description + " is not supported by the installed PartExplorer version.");

            object instance = GetInstance();
            if (instance == null)
                return Fail("PartExplorer is installed, but PartExplorerAddon.Instance is not available yet.");

            try
            {
                method.Invoke(instance, args);
                LastError = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                return Fail("PartExplorer " + description + " failed: " + GetBaseMessage(ex));
            }
        }

        private static bool Fail(string message)
        {
            LastError = message ?? string.Empty;
            return false;
        }

        private static string GetBaseMessage(Exception ex)
        {
            if (ex == null) return "Unknown error.";
            Exception current = ex;
            while (current.InnerException != null) current = current.InnerException;
            return string.IsNullOrEmpty(current.Message) ? current.GetType().Name : current.Message;
        }
    }
}
