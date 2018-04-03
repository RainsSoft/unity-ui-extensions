using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UberLogger;
namespace UberLogger
{

    /// <summary>
    /// ʵ��UGUI��� ��־���
    /// </summary>
    //[SmartAssembly.Attributes.DoNotObfuscate()]
    [DisallowMultipleComponent()]
    public class UberLoggerAppUGUI : MonoBehaviour, UberLogger.ILogger
    {

        //��־����
        [Header("��־����")]
        public GameObject LogItemPoolRoot;
        [Header("��־��ģ��")]
        public GameObject LogItemTemplate;
        //
        public Sprite SmallErrorIcon;
        public Sprite SmallWarningIcon;
        public Sprite SmallMessageIcon;
        [Header("��־������")]
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
            //ToDo:���������ʾ����
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
            //�Ƴ�������Χ�ļ�¼
            #region ���������ʾ��־��¼��
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
                //Todo:����ָ����Ŀ
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
        private void Awake() {
            Debug.Log("uberlogger: Awake()");
            //������־�ļ�            
            UberLoggerFile logfile = new UberLoggerFile(string.Format("UberLogger.Temp{0}.log", DateTime.Now.ToString("yyyy-MM-dd")));
            //UberLogger.Logger.AddLogger(new UberLoggerFile( "UberLogger.Temp.log"), false);    
            UberLogger.Logger.AddLogger(logfile, false);
            Debug.Log("create unity log file");
            //GameObject.DontDestroyOnLoad(this.gameObject);
            //
            fpsCounter.Init(this);
            memoryCounter.Init(this);
            deviceInfoCounter.Init(this);

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
            //    OnClickButton_��ʾ����();
            //});
            UberUILongPressClick.Get(this.Button_ShowWindow.gameObject).onLongPress.AddListener(() => {
                //������ʾdebug����
                OnClickButton_��ʾ����();
            });
            this.Button_CloseWindow.onClick.AddListener(() => {
                OnClickButton_�رմ���();
            });
            this.Button_ClearLog.onClick.AddListener(() => {
                OnClickButton_�����־();
            });
            this.InputField_FilterRegex.onEndEdit.AddListener((A) => {
                OnClickButton_����(A);
            });
            this.Button_ScrollToBottom.onClick.AddListener(() => {
                OnClickButton_������־���ײ�();
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
            OnClickButton_�����־();
        }
        [Header("��־�����")]
        public Button Button_ScrollToBottom;
        #region ����FPS����
        [Header("fps:")]
        public Text Text_FPS;
        //0.5�����һ�� FPS ������UI  TEXT  ����Ч��
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
                UpdateUI_��־����();
                UpdateUI_LogsShow();
            }
        }
        private void UpdateTexts() {
            Text_FPS.text = fpsText_text;     
        }
        [Header("��־����")]
        public GameObject Plane_Window;

        [Header("�����־")]
        public Button Button_ClearLog;
        public Toggle Toggle_ShowError;
        public Toggle Toggle_ShowWarning;
        public Toggle Toggle_ShowMessage;
        public Text Text_ErrorCout;
        public Text Text_WarningCount;
        public Text Text_NormalCount;
        bool m_NeedUpdateLogShow = false;
        [Header("��־���˿�")]
        public InputField InputField_FilterRegex;
        [Header("��ʾ��־����")]
        public Button Button_ShowWindow;
        bool ShowWindow = false;
        public Sprite ButtonTexture;
        public Sprite ErrorButtonTexture;
        public void DrawActivationButton() {
            Sprite buttonTex = ButtonTexture;
            if (NoErrors > 0) {
                buttonTex = ErrorButtonTexture;
            }
            Button_ShowWindow.transform.FindChild("SR_Image").GetComponent<Image>().overrideSprite = buttonTex;
        }
        [Header("������־����")]
        public Button Button_CloseWindow;
        void UpdateUI_��־����() {
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
            //Todo:����Ҫ��ʾ����Ŀ ����log�б�
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
            //������ʾ
            for (int i = 0; i < LogInfoNeedShow.Count; i++) {
                var log = LogInfoNeedShow[i];
                Transform childlogitem = this.LogItemContain.content.GetChild(i);
                //childlogitem.FindChild("SR_BG").GetComponent<Button>().onClick.RemoveAllListeners();
                //childlogitem.FindChild("SR_BG").GetComponent<Button>().onClick.AddListener(() => {
                //    OnClickButton_LogItem(i);
                //});
                childlogitem.FindChild("SR_Blob").GetComponent<Image>().overrideSprite = GetIconForLog(log);
                //��ֹ�ַ����� ���ַ���ȡ                
                var showMessage = log.Message;
                if (showMessage.Length > 1000) {
                    showMessage = showMessage.Substring(0, 1000);
                }
                //Make all messages single line
                showMessage = showMessage.Replace(System.Environment.NewLine, " ");
                childlogitem.FindChild("SR_Text").GetComponent<Text>().text = string.Format("{0}{1}", ShowTimes ? ("[" + log.GetTimeStampAsString() + "]: ") : "", showMessage);
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
        [Header("��־���ջ")]
        public Text Text_Callstack;
        [Header("�˳�APP")]
        public Button Button_ExitApp;
        /// <summary>
        /// ������־ ȡͼ��
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
            //Todo:��ʾ��ϸ��Ϣ
            if (log.Callstack.Count > 0) {
                Text_Callstack.text = log.Callstack[0].GetFormattedMethodName();
            }
            else {
                Text_Callstack.text = "none";
            }
            for (int c1 = 1; c1 < log.Callstack.Count; c1++) {
                var frame = log.Callstack[c1];
                var methodName = frame.GetFormattedMethodName();
                if (!String.IsNullOrEmpty(methodName)) {
                    Text_Callstack.text += "\r\n" + methodName;
                }

            }
        }
        void OnClickButton_�����־() {
            OnClickButton_������־������();
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

        void OnClickButton_�������() {
            //ClearSelectedMessage();
            FilterRegex = null;
            FilterRegexText = "";
            //ÿ�붨ʱ����
            m_NeedUpdateLogShow = true;
            UpdateUI_LogsShow();
        }
        void OnClickButton_����(string filterText) {
            //string filterText = InputField_FilterRegex.text;
            if (filterText != FilterRegexText) {
                //ClearSelectedMessage();
                FilterRegexText = filterText;
            }
            //TODO:ÿ�붨ʱ��������
            System.Text.RegularExpressions.Regex filterRegex = null;
            if (!String.IsNullOrEmpty(FilterRegexText)) {
                filterRegex = new System.Text.RegularExpressions.Regex(FilterRegexText);
            }
            this.FilterRegex = filterRegex;
            m_NeedUpdateLogShow = true;
            OnClickButton_������־������();
            UpdateUI_LogsShow();
        }
        void OnClickButton_������־���ײ�() {
            LogItemContain.normalizedPosition = new Vector2(0, 0);
        }
        void OnClickButton_������־������() {
            LogItemContain.normalizedPosition = new Vector2(0, 1);
        }
        void OnClickButton_�رմ���() {
            this.Plane_Window.SetActive(false);
            ShowWindow = false;
        }
        void OnClickButton_��ʾ����() {
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

