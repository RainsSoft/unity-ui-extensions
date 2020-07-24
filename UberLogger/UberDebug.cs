using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
namespace UberLogger
{
    //Helper functions to make logging easier
    public static class UberDebug
    {
        #region
        public static bool IsMobilePlatform {
            get {
                if (Application.isMobilePlatform) {
                    return true;
                }

                switch (Application.platform) {
#if UNITY_5 || UNITY_2018
                    case RuntimePlatform.WSAPlayerARM:
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerX86:
#else
					case RuntimePlatform.MetroPlayerARM:
					case RuntimePlatform.MetroPlayerX64:
					case RuntimePlatform.MetroPlayerX86:
#endif
                        return true;

                    default:
                        return false;
                }
            }
        }
        /// <summary>
        /// Performs a stack trace to see where things went wrong
        /// for error reporting.
        /// </summary>
        public static string GetErrorLocation(int level = 1, bool showOnlyLast = false) {

            StackTrace stackTrace = new StackTrace();
            string result = "";
            string declaringType = "";

            for (int v = stackTrace.FrameCount - 1; v > level; v--) {
                if (v < stackTrace.FrameCount - 1)
                    result += " --> ";
                StackFrame stackFrame = stackTrace.GetFrame(v);
                if (stackFrame.GetMethod().DeclaringType.ToString() == declaringType)
                    result = "";    // only report the last called method within every class
                declaringType = stackFrame.GetMethod().DeclaringType.ToString();
                result += declaringType + ":" + stackFrame.GetMethod().Name;
            }

            if (showOnlyLast) {
                try {
                    result = result.Substring(result.LastIndexOf(" --> "));
                    result = result.Replace(" --> ", "");
                }
                catch {
                }
            }

            return result;

        }


        /// <summary>
        /// Returns the 'syntax style' formatted version of a type name.
        /// for example: passing 'System.Single' will return 'float'.
        /// </summary>
        public static string GetTypeAlias(Type type) {

            string s = "";

            if (!m_TypeAliases.TryGetValue(type, out s))
                return type.ToString();

            return s;

        }


        /// <summary>
        /// Dictionary of type aliases for error messages.
        /// </summary>
        private static readonly Dictionary<Type, string> m_TypeAliases = new Dictionary<Type, string>()
        {

        { typeof(void), "void" },
        { typeof(byte), "byte" },
        { typeof(sbyte), "sbyte" },
        { typeof(short), "short" },
        { typeof(ushort), "ushort" },
        { typeof(int), "int" },
        { typeof(uint), "uint" },
        { typeof(long), "long" },
        { typeof(ulong), "ulong" },
        { typeof(float), "float" },
        { typeof(double), "double" },
        { typeof(decimal), "decimal" },
        { typeof(object), "object" },
        { typeof(bool), "bool" },
        { typeof(char), "char" },
        { typeof(string), "string" },
        { typeof(UnityEngine.Vector2), "Vector2" },
        { typeof(UnityEngine.Vector3), "Vector3" },
        { typeof(UnityEngine.Vector4), "Vector4" }

    };


        /// <summary>
        /// Activates or deactivates a gameobject for any Unity version.
        /// </summary>
        public static void Activate(GameObject obj, bool activate = true) {

#if UNITY_3_5
		obj.SetActiveRecursively(activate);
#else
            obj.SetActive(activate);
#endif

        }


        /// <summary>
        /// Returns active status of a gameobject for any Unity version.
        /// </summary>
        public static bool IsActive(GameObject obj) {

#if UNITY_3_5
		return obj.active;
#else
            return obj.activeSelf;
#endif

        }


        /// <summary>
        /// shows or hides the mouse cursor in a way suitable for the
        /// current unity version
        /// </summary>
        public static bool LockCursor {

            // compile only for unity 5+
#if (!(UNITY_4_6 || UNITY_4_5 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0 || UNITY_3_5))
            get {
                return ((Cursor.lockState == CursorLockMode.Locked) ? true : false);
            }
            set {
                // toggling cursor visible and invisible is currently buggy in the Unity 5
                // editor so we need to toggle brute force with custom arrow art
#if UNITY_EDITOR
				Cursor.SetCursor((value ? InvisibleCursor : VisibleCursor), Vector2.zero, CursorMode.Auto);
				Cursor.visible = value ? InvisibleCursor : VisibleCursor;
#else
                // running in a build so toggling visibility should work fine
                Cursor.visible = !value;
#endif
                Cursor.lockState = (value ? CursorLockMode.Locked : CursorLockMode.None);
            }
#else
		// compile only for unity 4.6 and older
		get { return Screen.lockCursor; }
		set { Screen.lockCursor = value; }
#endif

        }
#endregion

        [StackTraceIgnore]
        static public void Log(UnityEngine.Object context, string message, params object[] par) {
            UberLogger.Logger.Log("", context, LogSeverity.Message, message, par);
        }

        [StackTraceIgnore]
        static public void Log(string message, params object[] par) {
            UberLogger.Logger.Log("", null, LogSeverity.Message, message, par);
        }

        [StackTraceIgnore]
        static public void LogChannel(UnityEngine.Object context, string channel, string message, params object[] par) {
            UberLogger.Logger.Log(channel, context, LogSeverity.Message, message, par);
        }

        [StackTraceIgnore]
        static public void LogChannel(string channel, string message, params object[] par) {
            UberLogger.Logger.Log(channel, null, LogSeverity.Message, message, par);
        }


        [StackTraceIgnore]
        static public void LogWarning(UnityEngine.Object context, object message, params object[] par) {
            UberLogger.Logger.Log("", context, LogSeverity.Warning, message, par);
        }

        [StackTraceIgnore]
        static public void LogWarning(object message, params object[] par) {
            UberLogger.Logger.Log("", null, LogSeverity.Warning, message, par);
        }

        [StackTraceIgnore]
        static public void LogWarningChannel(UnityEngine.Object context, string channel, string message, params object[] par) {
            UberLogger.Logger.Log(channel, context, LogSeverity.Warning, message, par);
        }

        [StackTraceIgnore]
        static public void LogWarningChannel(string channel, string message, params object[] par) {
            UberLogger.Logger.Log(channel, null, LogSeverity.Warning, message, par);
        }

        [StackTraceIgnore]
        static public void LogError(UnityEngine.Object context, object message, params object[] par) {
            UberLogger.Logger.Log("", context, LogSeverity.Error, message, par);
        }

        [StackTraceIgnore]
        static public void LogError(object message, params object[] par) {
            UberLogger.Logger.Log("", null, LogSeverity.Error, message, par);
        }

        [StackTraceIgnore]
        static public void LogErrorChannel(UnityEngine.Object context, string channel, string message, params object[] par) {
            UberLogger.Logger.Log(channel, context, LogSeverity.Error, message, par);
        }

        [StackTraceIgnore]
        static public void LogErrorChannel(string channel, string message, params object[] par) {
            UberLogger.Logger.Log(channel, null, LogSeverity.Error, message, par);
        }


        //Logs that will not be caught by UberLogger
        //Useful for debugging UberLogger
        [LogUnityOnly]
        static public void UnityLog(object message) {
            UnityEngine.Debug.Log(message);
        }

        [LogUnityOnly]
        static public void UnityLogWarning(object message) {
            UnityEngine.Debug.LogWarning(message);
        }

        [LogUnityOnly]
        static public void UnityLogError(object message) {
            UnityEngine.Debug.LogError(message);
        }
    }

}