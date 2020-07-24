using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using Text = TMPro.TMP_Text;
using InputField = TMPro.TMP_InputField;
using Dropdown = TMPro.TMP_Dropdown;
namespace UberLogger {
    /// <summary>
    /// 实现UGUI版的 文本使用TextMesh Pro 日志输出
    /// 但是批次会大概会翻倍增加，所以看情况看日志是否使用 TextMeshPro 来显示
    /// </summary>
    //[SmartAssembly.Attributes.DoNotObfuscate()]
    [DisallowMultipleComponent()]
    public class UberLoggerAppUGUITMP : MonoBehaviour, UberLogger.ILogger {

        //日志内容
        [Header("日志项缓存根")]
        public GameObject LogItemPoolRoot;
        [Header("日志项模板")]
        public GameObject LogItemTemplate;
        //
        public Sprite SmallErrorIcon;
        public Sprite SmallWarningIcon;
        public Sprite SmallMessageIcon;
        [Header("日志项容器")]
        public ScrollRect LogItemContain;
        #region log item pool
        Queue<GameObject> m_LogItemPool = new Queue<GameObject>();
        GameObject getLogItem() {
            if (m_LogItemPool.Count < 1) {
                for (int i = 0; i < 4; i++) {
                    GameObject go = GameObject.Instantiate<GameObject>(LogItemTemplate);
                    go.transform.SetParent(LogItemPoolRoot.transform, false);
                    go.SetActive(false);
                    m_LogItemPool.Enqueue(go);
                }
            }
            var logItem = m_LogItemPool.Dequeue();
            logItem.GetComponent<Button>().onClick.AddListener(() => {
                OnClickButton_LogItem(logItem.transform);
            });
            return logItem;
        }
        void releaseLogItem(GameObject logitem) {
            if (m_LogItemPool.Count >= 128) {
                logitem.transform.SetParent(null);
                GameObject.Destroy(logitem);
            }
            else {
                logitem.SetActive(false);
                logitem.transform.SetParent(null);
                logitem.transform.SetParent(LogItemPoolRoot.transform, false);
                logitem.transform.localScale = Vector3.one;
                m_LogItemPool.Enqueue(logitem);
                logitem.GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }
        #endregion
        //
        public int MaxLogCountShow = 200;
        public void Log(LogInfo logInfo) {
            //ToDo:限制最大显示条数
            LogInfo.Add(logInfo);
            if (logInfo.Severity == LogSeverity.Error) {
                NoErrors++;
            }
            else if (logInfo.Severity == LogSeverity.Warning) {
                NoWarnings++;
            }
            else {
                NoMessages++;
            }
            if (logInfo.Severity == LogSeverity.Error && PauseOnError) {
                UnityEngine.Debug.Break();
            }
            //移除超出范围的记录
            #region 限制最大显示日志记录数
            if (MaxLogCountShow < 100) {
                MaxLogCountShow = 100;
            }
            if (LogInfo.Count > MaxLogCountShow) {
                LogInfo log = LogInfo[0];
                LogInfo.RemoveAt(0);
                if (log.Severity == LogSeverity.Error) {
                    NoErrors--;
                }
                else if (log.Severity == LogSeverity.Warning) {
                    NoWarnings--;
                }
                else {
                    NoMessages--;
                }
                //Todo:更新指定项目
            }
            #endregion
            m_NeedUpdateLogShow = true;
        }

        void Clear() {
            LogInfo.Clear();
            LogInfoNeedShow.Clear();
            NoWarnings = 0;
            NoErrors = 0;
            NoMessages = 0;
        }
        #region
        /// <summary>
        /// Frames Per Second counter.
        /// </summary>
        public FPSCounterData fpsCounter = new FPSCounterData();
        /// <summary>
        /// Mono or heap memory counter.
        /// </summary>
        public MemoryCounterData memoryCounter = new MemoryCounterData();
        /// <summary>
        /// Device hardware info.<br/>
        /// Shows CPU name, cores (threads) count, GPU name, total VRAM, total RAM, screen DPI and screen size.
        /// </summary>
        public DeviceInfoCounterData deviceInfoCounter = new DeviceInfoCounterData();

        public bool LoggerEnabled = false;
        #endregion
        private void OnDestroy() {
            fpsCounter.Dispose();
            memoryCounter.Dispose();
            deviceInfoCounter.Dispose();
            Debug.Log("uberlogger: OnDestroy()");
            UberLogger.Logger.Unload();
        }
        public void Close() {
            //Todo:
        }
        [SerializeField()]
        private Texture fingerTexture;
        float fingerTexture_Size = 32;
        float fingerTexture_Alpha = 1f;
        bool fingerTexture_NeedDisplay = false;
        Vector2 fingerTexture_Finger = new Vector2(-1f, -1f);
        void OnGUI() {
            if (Input.GetMouseButtonDown(0)) {
                fingerTexture_Size = 32;
                fingerTexture_Alpha = 1f;
                fingerTexture_NeedDisplay = true;
                fingerTexture_Finger = Input.mousePosition;
            }
            if (fingerTexture_NeedDisplay) {
                fingerTexture_Size += Time.unscaledDeltaTime * 40f;
                fingerTexture_Alpha -= Time.unscaledDeltaTime * 1f;
                if (fingerTexture_Size > 80f) {
                    fingerTexture_NeedDisplay = false;
                }
                if (fingerTexture_Alpha < 0.1f) {
                    fingerTexture_Alpha = 0.1f;
                }
                float halfsize = fingerTexture_Size * 0.5f;
                //Vector2 finger = Input.mousePosition;
                if (fingerTexture_Finger != new Vector2(-1f, -1f)) {

                    GUI.DrawTexture(new Rect(fingerTexture_Finger.x - halfsize, Screen.height - fingerTexture_Finger.y - halfsize, fingerTexture_Size, fingerTexture_Size),
                        fingerTexture, ScaleMode.ScaleToFit, true, 0f, new Color(1f, 1f, 1f, fingerTexture_Alpha), 0, 0);

                }
            }


        }
        private void Awake() {
            Debug.Log("uberlogger: Awake()");
            //增加日志文件         
#if !DEBUG
            UberLoggerFile logfile = new UberLoggerFile(string.Format("irobotqlog.{0}.log", DateTime.Now.ToString("yyyy-MM-dd")),true);
            UberLogger.Logger.AddLogger(logfile, false);
#else
            UberLoggerFile logfile = new UberLoggerFile("irobotqlog.log", true);
            UberLogger.Logger.AddLogger(logfile, true);
#endif

            //UberLogger.Logger.AddLogger(new UberLoggerFile( "UberLogger.Temp.log"), false);    

            Debug.Log("create unity log file:" + logfile.LogFileFullPath);
            //GameObject.DontDestroyOnLoad(this.gameObject);
            //
            fpsCounter.Init(this);
            memoryCounter.Init(this);
            deviceInfoCounter.Init(this);
            Logger.Enabled = LoggerEnabled;
        }

        void Start() {
            //DontDestroyOnLoad(gameObject);
            UberLogger.Logger.AddLogger(this);
            //ClearSelectedMessage();
            timeleft = updateInterval;
            LogItemTemplate.transform.SetParent(LogItemPoolRoot.transform, false);
            LogItemTemplate.SetActive(false);
            Text_Callstack.text = "";
            Plane_Window.SetActive(false);
            //
            this.Button_ExitApp.onClick.AddListener(() => {
                OnClickButton_ExitApp();
            });
            //this.Button_ShowWindow.onClick.AddListener(()=> {
            //    OnClickButton_显示窗口();
            //});
            UberUILongPressClick.Get(this.Button_ShowWindow.gameObject).onLongPress.AddListener(() => {
                //长按显示debug窗口
                OnClickButton_显示窗口();
            });
            this.Button_CloseWindow.onClick.AddListener(() => {
                OnClickButton_关闭窗口();
            });
            this.Button_ClearLog.onClick.AddListener(() => {
                OnClickButton_清空日志();
            });
            this.InputField_FilterRegex.onEndEdit.AddListener((A) => {
                OnClickButton_过滤(A);
            });
            this.Button_ScrollToBottom.onClick.AddListener(() => {
                OnClickButton_滚动日志到底部();
            });
            //
            ActivateCounters();
        }
        private void ActivateCounters() {
            fpsCounter.Activate();
            memoryCounter.Activate();
            deviceInfoCounter.Activate();

            if (fpsCounter.Enabled || memoryCounter.Enabled || deviceInfoCounter.Enabled) {
                UpdateTexts();
            }
            OnClickButton_清空日志();
        }
        [Header("日志项滚动")]
        public Button Button_ScrollToBottom;
        #region 增加FPS计数
        [Header("fps:")]
        public Text Text_FPS;
        //0.5秒更新一次 FPS 关联的UI  TEXT  提升效率
        public float updateInterval = 0.5f;
        float deltaFps = 0f; // FPS accumulated over the interval
        int frames = 0; // Frames drawn over the interval
        float timeleft = 0.5f; // Left time for current interval
        string fpsText_text = "60 FPS";

        void Update() {
            timeleft -= Time.deltaTime;
            deltaFps += Time.timeScale / Time.deltaTime;
            ++frames;

            // Interval ended - update GUI text and start new interval
            if (timeleft <= 0f) {
                // display two fractional digits (f2 format)
                fpsText_text = string.Format("{0} FPS", (deltaFps / frames).ToString("f2"));
                if ((deltaFps / frames) < 1) {
                    fpsText_text = "";
                }
                timeleft = updateInterval;
                deltaFps = 0f;
                frames = 0;
                //
                UpdateTexts();
                UpdateUI_日志计数();
                UpdateUI_LogsShow();
            }
        }
        private void UpdateTexts() {
            Text_FPS.text = fpsText_text;
        }
        [Header("日志窗口")]
        public GameObject Plane_Window;

        [Header("清空日志")]
        public Button Button_ClearLog;
        public Toggle Toggle_ShowError;
        public Toggle Toggle_ShowWarning;
        public Toggle Toggle_ShowMessage;
        public Text Text_ErrorCout;
        public Text Text_WarningCount;
        public Text Text_NormalCount;
        bool m_NeedUpdateLogShow = false;
        [Header("日志过滤框")]
        public InputField InputField_FilterRegex;
        [Header("显示日志窗口")]
        public Button Button_ShowWindow;
        bool ShowWindow = false;
        public Sprite ButtonTexture;
        public Sprite ErrorButtonTexture;
        public void DrawActivationButton() {
            Sprite buttonTex = ButtonTexture;
            if (NoErrors > 0) {
                buttonTex = ErrorButtonTexture;
            }
            Button_ShowWindow.transform.Find("SR_Image").GetComponent<Image>().overrideSprite = buttonTex;
        }
        [Header("隐藏日志窗口")]
        public Button Button_CloseWindow;
        void UpdateUI_日志计数() {
            Text_ErrorCout.text = NoErrors.ToString();
            Text_WarningCount.text = NoWarnings.ToString();
            Text_NormalCount.text = NoMessages.ToString();
        }
        void UpdateUI_LogsShow() {
            if (!m_NeedUpdateLogShow) return;

            LogInfoNeedShow.Clear();
            ShowErrors = this.Toggle_ShowError.isOn;
            ShowWarnings = this.Toggle_ShowWarning.isOn;
            ShowMessages = this.Toggle_ShowMessage.isOn;
            //
            DrawActivationButton();
            foreach (var v in LogInfo) {
                if (this.CheckShouldShowLog(this.FilterRegex, v)) {
                    LogInfoNeedShow.Add(v);
                }

            }
            //Todo:更新要显示的项目 更新log列表
            int logContainChildCount = this.LogItemContain.content.childCount;
            if (logContainChildCount < LogInfoNeedShow.Count) {
                int needadd = LogInfoNeedShow.Count - logContainChildCount;
                for (int i = 0; i < needadd; i++) {
                    GameObject logitem = getLogItem();
                    logitem.SetActive(true);
                    logitem.transform.SetParent(null);
                    logitem.transform.SetParent(this.LogItemContain.content, false);
                    logitem.transform.localScale = Vector3.one;
                }
            }
            else if (logContainChildCount > LogInfoNeedShow.Count) {
                int needadd = logContainChildCount - LogInfoNeedShow.Count;
                for (int i = 0; i < needadd; i++) {
                    int cc = this.LogItemContain.content.childCount;
                    GameObject childlogitem = this.LogItemContain.content.GetChild(0).gameObject;
                    releaseLogItem(childlogitem);
                }
            }
            //更新显示
            for (int i = 0; i < LogInfoNeedShow.Count; i++) {
                var log = LogInfoNeedShow[i];
                Transform childlogitem = this.LogItemContain.content.GetChild(i);
                //childlogitem.FindChild("SR_BG").GetComponent<Button>().onClick.RemoveAllListeners();
                //childlogitem.FindChild("SR_BG").GetComponent<Button>().onClick.AddListener(() => {
                //    OnClickButton_LogItem(i);
                //});
                childlogitem.Find("SR_Blob").GetComponent<Image>().overrideSprite = GetIconForLog(log);
                //防止字符过长 对字符截取                
                var showMessage = log.Message;
                if (showMessage.Length > 1000) {
                    showMessage = showMessage.Substring(0, 1000);
                }
                //Make all messages single line
                showMessage = showMessage.Replace(System.Environment.NewLine, " ");
                childlogitem.Find("SR_Text").GetComponent<Text>().text = string.Format("{0}{1}", ShowTimes ? ("[" + log.GetTimeStampAsString() + "]: ") : "", showMessage);
            }
        }

        #endregion


        /// <summary>
        /// Based on filter and channel selections, should this log be shown?
        /// </summary>
        bool CheckShouldShowLog(System.Text.RegularExpressions.Regex regex, LogInfo log) {
            //if (log.Channel == CurrentChannel || CurrentChannel == "All" || (CurrentChannel == "No Channel" && String.IsNullOrEmpty(log.Channel))) {
            if ((log.Severity == LogSeverity.Message && ShowMessages)
               || (log.Severity == LogSeverity.Warning && ShowWarnings)
               || (log.Severity == LogSeverity.Error && ShowErrors)) {
                if (regex == null || regex.IsMatch(log.Message)) {
                    return true;
                }
            }
            //}

            return false;
        }
        [Header("日志项堆栈")]
        public Text Text_Callstack;
        [Header("退出APP")]
        public Button Button_ExitApp;
        /// <summary>
        /// 根据日志 取图标
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        Sprite GetIconForLog(LogInfo log) {
            if (log.Severity == LogSeverity.Error) {
                return SmallErrorIcon;
            }
            if (log.Severity == LogSeverity.Warning) {
                return SmallWarningIcon;
            }

            return SmallMessageIcon;
        }
        void OnClickButton_LogItem(Transform logItem) {
            int c = this.LogItemContain.content.childCount;
            int index = -1;
            for (int i = 0; i < c; i++) {
                if (this.LogItemContain.content.GetChild(i) == logItem) {
                    index = i;
                    break;
                }
            }
            if (index < 0) {
                this.Text_Callstack.text = "none(-1)";
                return;
            }
            LogInfo log = LogInfoNeedShow[index];
            //Todo:显示详细信息
            Text_Callstack.text = log.Message;
            int log_Callstack_Count = (log.Callstack == null ? 0 : log.Callstack.Count);
            if (log_Callstack_Count > 0) {
                log_Callstack_Count = log.Callstack.Count;
                Text_Callstack.text += log.Callstack[0].GetFormattedMethodName();
            }
            else if (string.IsNullOrEmpty(log.Callstack_String) == false) {
                Text_Callstack.text = log.Callstack_String;
            }
            else {
                Text_Callstack.text += "none";
            }
            for (int c1 = 1; c1 < log_Callstack_Count; c1++) {
                var frame = log.Callstack[c1];
                var methodName = frame.GetFormattedMethodName();
                if (!String.IsNullOrEmpty(methodName)) {
                    Text_Callstack.text += "\r\n" + methodName;
                }

            }
        }
        void OnClickButton_清空日志() {
            OnClickButton_滚动日志到顶部();
            Clear();
            List<string> infos = new List<string>();
            if (!string.IsNullOrEmpty(fpsCounter.lastText)) {
                infos.AddRange(fpsCounter.lastText.Split(Environment.NewLine[0]));
            }
            if (!string.IsNullOrEmpty(memoryCounter.lastText)) {
                infos.AddRange(memoryCounter.lastText.Split(Environment.NewLine[0]));
            }
            if (!string.IsNullOrEmpty(deviceInfoCounter.lastText)) {
                infos.AddRange(deviceInfoCounter.lastText.Split(Environment.NewLine[0]));
            }
            for (int i = 0; i < infos.Count; i++) {
                var info = infos[i].Replace('\r', ' ');
                info = info.Replace('\n', ' ');
                var logInfo = new LogInfo(null, "", LogSeverity.Message, new List<LogStackFrame>(), info.Trim());
                this.Log(logInfo);
            }
            m_NeedUpdateLogShow = true;
            UpdateUI_LogsShow();
            //

        }

        void OnClickButton_清除过滤() {
            //ClearSelectedMessage();
            FilterRegex = null;
            FilterRegexText = "";
            //每秒定时更新
            m_NeedUpdateLogShow = true;
            UpdateUI_LogsShow();
        }
        void OnClickButton_过滤(string filterText) {
            //string filterText = InputField_FilterRegex.text;
            if (filterText != FilterRegexText) {
                //ClearSelectedMessage();
                FilterRegexText = filterText;
            }
            //TODO:每秒定时更新内容
            System.Text.RegularExpressions.Regex filterRegex = null;
            if (!String.IsNullOrEmpty(FilterRegexText)) {
                filterRegex = new System.Text.RegularExpressions.Regex(FilterRegexText);
            }
            this.FilterRegex = filterRegex;
            m_NeedUpdateLogShow = true;
            OnClickButton_滚动日志到顶部();
            UpdateUI_LogsShow();
        }
        void OnClickButton_滚动日志到底部() {
            LogItemContain.normalizedPosition = new Vector2(0, 0);
        }
        void OnClickButton_滚动日志到顶部() {
            LogItemContain.normalizedPosition = new Vector2(0, 1);
        }
        void OnClickButton_关闭窗口() {
            this.Plane_Window.SetActive(false);
            ShowWindow = false;
        }
        void OnClickButton_显示窗口() {
            this.Plane_Window.SetActive(true);
            ShowWindow = true;
        }
        private void OnClickButton_ExitApp() {
            //if (Application.isEditor) {
            //UnityEditor.EditorApplication.isPaused = true;
            //}
            //else {
            Application.Quit();
            //}
        }
        //
        bool ShowTimes = true;
        List<LogInfo> LogInfo = new List<LogInfo>();
        List<LogInfo> LogInfoNeedShow = new List<LogInfo>();
        bool PauseOnError = false;
        int NoErrors;
        int NoWarnings;
        int NoMessages;
        //             
        string FilterRegexText = "";
        System.Text.RegularExpressions.Regex FilterRegex = null;
        bool ShowErrors = true;
        bool ShowWarnings = true;
        bool ShowMessages = true;

    }
}