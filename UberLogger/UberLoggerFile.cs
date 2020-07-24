using System.IO;
using UnityEngine;
using UberLogger;
namespace UberLogger {
    /// <summary>
    /// A basic file logger backend
    /// </summary>
    public class UberLoggerFile : UberLogger.ILogger {
        private StreamWriter LogFileWriter;
        private bool IncludeCallStacks;

        /// <summary>
        /// 直接传日志文件名，不要传路径,默认存储在默认缓存目录中
        /// Constructor. Make sure to add it to UberLogger via Logger.AddLogger();
        /// filename is relative to Application.persistentDataPath
        /// if includeCallStacks is true it will dump out the full callstack for all logs, at the expense of big log files.
        /// </summary>
        public UberLoggerFile(string filename, bool includeCallStacks = false) {
            IncludeCallStacks = includeCallStacks;
            string of = filename;
            bool hasIo = false;
            int retry = 0;
        again:
            hasIo = false;
            retry++;
            LogFileFullPath = System.IO.Path.Combine(Application.temporaryCachePath, of);
            Debug.Log("Initialising file logging to " + LogFileFullPath);
            try {
                LogFileWriter = new StreamWriter(LogFileFullPath, true);
            }
            catch (IOException io) {
                hasIo = true;
            }
            if (hasIo) {
                if (retry > 5) {

                }
                else {
                    of = Path.GetFileNameWithoutExtension(filename) + "_" + retry.ToString() + Path.GetExtension(filename);
                    goto again;
                }
            }
            LogFileWriter.AutoFlush = true;
        }
        public string LogFileFullPath {
            get;
            private set;
        }
        public void Log(LogInfo logInfo) {
            lock (this) {
                LogFileWriter.WriteLine(logInfo.GetTimeStampAsString() + ": " + logInfo.Message);
                //只记录错误的callstack
                if (logInfo.Severity == LogSeverity.Error && IncludeCallStacks) {
                    if (string.IsNullOrEmpty(logInfo.Callstack_String) == false) {
                        LogFileWriter.WriteLine(logInfo.Callstack_String);
                    }
                    else if (logInfo.Callstack != null && logInfo.Callstack.Count > 0) {
                        foreach (var frame in logInfo.Callstack) {
                            LogFileWriter.WriteLine(frame.GetFormattedMethodName());
                        }
                        LogFileWriter.WriteLine();
                    }

                }
                LogFileWriter.Flush();
            }
        }
        public void Close() {
            lock (this) {
                LogFileWriter.Flush();
            }
            LogFileWriter.Close();
        }
    }

}