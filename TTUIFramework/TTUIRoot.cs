/*
 * @Author: chiuan wei 
 * @Date: 2017-05-27 18:14:53 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-05-27 18:33:48
 * @Change: sgd 2017-10-11 不再使用单例模式，一个scene一个uiroot,有利于关卡切换时候资源卸载
 * @Change: sgd 2019-10-14 增加受管理的开启指定名称协程（相同名称的协程如果已经在运行则不能开启新的相同名称的协程）
 */
namespace TinyTeam.UI {
    using System.Collections;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using System.Collections.Generic;
    using System;
    using IRobotQ.Core;
    using IRobotQ;

    /// <summary>
    /// Init The UI Root
    /// 全局功能UI，在跟节点场景 
    /// UIRoot
    /// -Canvas
    /// --FixedRoot
    /// --NormalRoot
    /// --PopupRoot
    /// -Camera
    /// </summary>
    [DisallowMultipleComponent()]
    public class TTUIRoot : MonoBehaviour {
        //private static TTUIRoot m_Instance = null;
        //public static TTUIRoot Instance {
        //    get {
        //        if (m_Instance == null) {
        //            InitRoot();
        //        }
        //        return m_Instance;
        //    }
        //}
        /// <summary>
        /// 1136 1.775 苹果 1280 1.777安卓
        /// </summary>
        public const float ResolutionWidth = 1280f;
        /// <summary>
        /// 640 1.775 苹果 720 1.777安卓
        /// </summary>
        public const float ResolutionHeight = 720f;
        /// <summary>
        /// rootui 根节点
        /// </summary>
        public Transform root;
        /// <summary>
        /// fixed节点 一直固定 比如toolbar
        /// </summary>
        public Transform fixedRoot;
        /// <summary>
        /// 一般界面节点
        /// </summary>
        public Transform normalRoot;
        /// <summary>
        /// 弹出界面节点 比如独占的msgbox
        /// </summary>
        public Transform popupRoot;
        /// <summary>
        /// 需要重新赋值
        /// </summary>
        public Camera uiCamera;
        /// <summary>
        /// 关联scene名称
        /// </summary>
        public string refLevelName;
        /// <summary>
        /// 是否被销毁
        /// </summary>
        public bool IsDestoryed {
            get; private set;
        }
        /// <summary>
        /// 创建(如果已经存在则直接返回)
        /// </summary>
        /// <param name="go_uiroot_name">"UIRoot" / "UIRoot_Global" </param>
        /// <returns></returns>
        public static TTUIRoot CreateOrFind(string go_uiroot_name="UIRoot"/*UIRoot_Root*/) {
            TTUIRoot root = InitRoot(go_uiroot_name);
         
            AdaptCamera(root);
            return root;
        }
        #region 创建root相关

        #region 相机适配 迁移到脚本组件TTUICameraAdapter
        static void AdaptCamera(TTUIRoot uiroot) {
            return;
            //定义 宽小于1000或者高小于640的使用缩放模式 //iphone6 1136x640 
            if (Screen.width < 1000 || Screen.height < 640) {
                CanvasScaler cscaler = uiroot.GetComponent<CanvasScaler>();
                //修改为缩放模式，保证屏幕再小也能显示整个UI
                cscaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
            //UI适配 在不同纵横比屏幕中会有重叠或者 有黑边问题
            Camera camera = uiroot.uiCamera;
            //if (camera == null) {
            //    camera = GetComponent<Camera>();
            //}

            float screenAspect = (float)Screen.width / (float)Screen.height;
            float designAspect = ResolutionWidth / ResolutionHeight;

            if (designAspect < screenAspect) //屏幕分辨率过大，宽度过长,则屏幕横向留出黑边,高度不变
            {
                float tarWidth = Screen.height * designAspect;//求出实际要显示的宽度
                float tarWidthRadio = tarWidth / Screen.width;//求出宽度百分比
                float posW = (1 - tarWidthRadio) / 2;//宽的起点
                camera.rect = new Rect(posW, 0, tarWidthRadio, 1);
            }
            else if (designAspect > screenAspect)//屏幕分辨率过小，高度过高，纵向留黑边,宽度不变
            {
                float tarHeight = Screen.width / designAspect;
                float tarHeightRadio = tarHeight / Screen.height;
                float posH = (1 - tarHeightRadio) / 2;
                camera.rect = new Rect(0, posH, 1, tarHeightRadio);
            }
            else {
                camera.rect = new Rect(0, 0, 1, 1);
            }
        }
        static void AdaptCameraOrtho(TTUIRoot uiroot) {
            // //UI适配 适用于orthographicSize为1的正交相机
            // Camera camera = uiroot.uiCamera;
            // //获取设备宽高  
            //float device_width = Screen.width;
            //float device_height = Screen.height;
            // //计算宽高比例  
            // float standard_aspect = ResolutionWidth / ResolutionHeight;
            // float device_aspect = device_width / device_height;
            // //计算矫正比例  
            // float adjustor =01f;
            // if (device_aspect < standard_aspect) {
            //     adjustor = standard_aspect / device_aspect;
            //     //Debug.Log(standard_aspect);  
            // }
            // Debug.Log("屏幕的比例" + adjustor);
            // if (adjustor < 2 && adjustor > 0) {
            //     camera.orthographicSize = adjustor;
            // }


        }

        #endregion
        static TTUIRoot InitRoot(string go_uiroot_name) {
            //改造为使用预设模式
            GameObject goRoot = GameObject.Find(go_uiroot_name);
            if (goRoot == null) {
                goRoot = Instantiate<GameObject>(ResManager.Singleton.LoadAsset<GameObject>("UIFrame/UIRoot.prefab"));
                goRoot.name = go_uiroot_name;
               // SceneManager.MoveGameObjectToScene(goRoot, SceneManager.GetSceneByName(levelname));
            }
            TTUIRoot m_Instance = goRoot.GetComponent<TTUIRoot>();
            m_Instance.root = goRoot.transform;
            m_Instance.uiCamera = goRoot.transform.Find("UICamera").GetComponent<Camera>();
            m_Instance.normalRoot = goRoot.transform.Find("NormalRoot");
            m_Instance.fixedRoot = goRoot.transform.Find("FixedRoot");
            m_Instance.popupRoot = goRoot.transform.Find("PopupRoot");
            return m_Instance;
            ////使用预设模式
            //GameObject go = new GameObject("UIRoot");
            //go.layer = LayerMask.NameToLayer("UI");
            //m_Instance = go.AddComponent<TTUIRoot>();
            //go.AddComponent<RectTransform>();

            //Canvas can = go.AddComponent<Canvas>();
            //can.renderMode = RenderMode.ScreenSpaceCamera;
            //can.pixelPerfect = true;

            //go.AddComponent<GraphicRaycaster>();

            //m_Instance.root = go.transform;

            //GameObject camObj = new GameObject("UICamera");
            //camObj.layer = LayerMask.NameToLayer("UI");
            //camObj.transform.parent = go.transform;
            //camObj.transform.localPosition = new Vector3(0, 0, -100f);
            //Camera cam = camObj.AddComponent<Camera>();
            //cam.clearFlags = CameraClearFlags.Depth;
            //cam.orthographic = true;
            //cam.farClipPlane = 200f;
            //can.worldCamera = cam;
            //cam.cullingMask = 1 << 5;
            //cam.nearClipPlane = -50f;
            //cam.farClipPlane = 50f;

            //m_Instance.uiCamera = cam;

            ////add audio listener
            //camObj.AddComponent<AudioListener>();
            //camObj.AddComponent<GUILayer>();

            //CanvasScaler cs = go.AddComponent<CanvasScaler>();
            //cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            //cs.referenceResolution = new Vector2(ResolutionWidth, ResolutionHeight);
            //cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            //////add auto scale camera fix size.
            ////TTCameraScaler tcs = go.AddComponent<TTCameraScaler>();
            ////tcs.scaler = cs;

            ////set the raycaster
            ////GraphicRaycaster gr = go.AddComponent<GraphicRaycaster>();

            //GameObject subRoot;

            //subRoot = CreateSubCanvasForRoot(go.transform, 0);
            //subRoot.name = "NormalRoot";
            //m_Instance.normalRoot = subRoot.transform;
            //m_Instance.normalRoot.transform.localScale = Vector3.one;

            //subRoot = CreateSubCanvasForRoot(go.transform, 250);
            //subRoot.name = "FixedRoot";
            //m_Instance.fixedRoot = subRoot.transform;
            //m_Instance.fixedRoot.transform.localScale = Vector3.one;

            //subRoot = CreateSubCanvasForRoot(go.transform, 500);
            //subRoot.name = "PopupRoot";
            //m_Instance.popupRoot = subRoot.transform;
            //m_Instance.popupRoot.transform.localScale = Vector3.one;

            ////add Event System
            //GameObject esObj = GameObject.Find("EventSystem");
            //if (esObj != null) {
            //    GameObject.DestroyImmediate(esObj);
            //}

            //GameObject eventObj = new GameObject("EventSystem");
            //eventObj.layer = LayerMask.NameToLayer("UI");
            //eventObj.transform.SetParent(go.transform);
            //eventObj.AddComponent<EventSystem>();
            //eventObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        }
        static GameObject CreateSubCanvasForRoot(Transform root, int sort) {
            GameObject go = new GameObject("canvas");
            go.transform.parent = root;
            go.layer = LayerMask.NameToLayer("UI");

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 0);
            rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 0);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            //  Canvas can = go.AddComponent<Canvas>();
            //  can.overrideSorting = true;
            //  can.sortingOrder = sort;
            //  go.AddComponent<GraphicRaycaster>();

            return go;
        }
        #endregion

        #region 界面内开启协程
        class CoItem {
            internal IEnumerator m_Itor;
            internal string Name = "";
            internal Coroutine Co;
        }
        Dictionary<string, CoItem> m_dict = new Dictionary<string, CoItem>(StringComparer.OrdinalIgnoreCase);
        public enum CoEndType {
            /// <summary>
            /// 正常结束,包括执行错误也是
            /// </summary>
            Normal = 0,
            /// <summary>
            /// 通过CoStop主动结束
            /// </summary>
            BeStopped = 1
        }
        /// <summary>
        /// 协程运行完毕时发生
        /// </summary>
        public event Action<string, CoEndType> OnCoroutineEnd;
        public Coroutine CoStart(string name, IEnumerator co) {
            if (IsCoRuning(name)) {
                Debug.LogError("TTUIRoot.CoManager已经开启:" + name);
                return null;
            }
            CoItem it = new CoItem { Name = name, m_Itor = co };
            m_dict.Add(name, it);
            it.Co = this.StartCoroutine(InternalStartCo(it));
            return it.Co;
        }
        private IEnumerator InternalStartCo(CoItem it) {
            bool wait = false;
            Debug.LogWarning("TTUIRoot.CoManager开启:" + it.Name);
            while (true) {
                try {
                    wait = it.m_Itor.MoveNext();
                }
                catch (Exception ee) {
                    //记录
                    Debug.LogError("TTUIRoot.Comanager执行发生异常,Name:" + it == null ? "null" : it.Name + " Error:" + ee);
                    wait = false;
                }
                if (wait) {
                    yield return it.m_Itor.Current;
                }
                else {
                    Debug.LogWarning("TTUIRoot.CoManager结束:" + it == null ? "null" : it.Name);
                    m_dict.Remove(it.Name);
                    if (OnCoroutineEnd != null) {
                        OnCoroutineEnd(it.Name, CoEndType.Normal);
                    }
                    break;
                }
            }

        }
        public void CoStop(string name) {
            CoItem it;
            if (m_dict.TryGetValue(name, out it)) {
                this.StopCoroutine(it.Co);

                Debug.LogWarning("[TTUIRoot].CoStop:" + name);
                m_dict.Remove(name);
                if (OnCoroutineEnd != null) {
                    OnCoroutineEnd(it.Name, CoEndType.BeStopped);
                }
            }

        }
        /// <summary>
        /// 返回true,表示自定义名称的协程还在执行
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsCoRuning(string name) {
            return m_dict.ContainsKey(name);
        }
        #endregion

        #region page管理
        //all pages with the union type
        private Dictionary<string, TTUIPage> m_allPages = new Dictionary<string, TTUIPage>();
        public Dictionary<string, TTUIPage> allPages {
            get {
                return m_allPages;
            }
        }

        //control 1>2>3>4>5 each page close will back show the previus page.
        private List<TTUIPage> m_currentPageNodes;//= new List<TTUIPage>();
        public List<TTUIPage> currentPageNodes {
            get {
                return m_currentPageNodes;
            }
        }
        #endregion
        //public static Func<string, Object> delegateSyncLoadUI = null;
        ///// <summary>
        ///// 外面赋值 指定加载UI预设方式（比如从assetbundle加载）
        ///// </summary>
        //public static Action<string, Action<Object>> delegateAsyncLoadUI = null;
        #region static api

        bool CheckIfNeedBack(TTUIPage page) {
            return page != null && page.CheckIfNeedBack();
        }

        /// <summary>
        /// make the target node to the top.
        /// </summary>
        internal void PopNode(TTUIPage page) {
            //List<TTUIPage> m_currentPageNodes = page.TTUIRoot_Instance.currentPageNodes;
            if (m_currentPageNodes == null) {
                //Debug.LogError("[UI] currentPageNodes is null.");
                m_currentPageNodes = new List<TTUIPage>();
                //return;
            }

            if (page == null) {
                Debug.LogError("[UI] page popup is null.");
                return;
            }

            //sub pages should not need back.
            if (CheckIfNeedBack(page) == false) {
                return;
            }

            bool _isFound = false;
            for (int i = 0; i < m_currentPageNodes.Count; i++) {
                if (m_currentPageNodes[i].Equals(page)) {
                    m_currentPageNodes.RemoveAt(i);
                    m_currentPageNodes.Add(page);
                    _isFound = true;
                    break;
                }
            }

            //if dont found in old nodes
            //should add in nodelist.
            if (!_isFound) {
                m_currentPageNodes.Add(page);
            }

            //after pop should hide the old node if need.
            HideOldNodes();
        }

        private void HideOldNodes() {
            //List<TTUIPage> m_currentPageNodes = page.TTUIRoot_Instance.currentPageNodes;

            if (m_currentPageNodes.Count < 0)
                return;
            TTUIPage topPage = m_currentPageNodes[m_currentPageNodes.Count - 1];
            if (topPage.mode == UIMode.HideOther) {
                //form bottm to top.
                for (int i = m_currentPageNodes.Count - 2; i >= 0; i--) {
                    if (m_currentPageNodes[i].isActive())
                        m_currentPageNodes[i].Hide();
                }
            }
        }

        public void ClearNodes() {
            m_currentPageNodes.Clear();
        }
        private void ShowPage<T>(Action callback, object pageData, bool isAsync) where T : TTUIPage, new() {
            ShowPage<T>(typeof(T).ToString(), callback, pageData, isAsync);
        }
        private void ShowPage<T>(string tpagename, Action callback, object pageData, bool isAsync) where T : TTUIPage, new() {
            Type t = typeof(T);
            string pageName = tpagename;//t.ToString();

            if (m_allPages != null && m_allPages.ContainsKey(pageName)) {
                ShowPage(pageName, m_allPages[pageName], callback, pageData, isAsync);
            }
            else {
                T instance = new T();
                ShowPage(pageName, instance, callback, pageData, isAsync);
            }
        }

        private void ShowPage(string pageName, TTUIPage pageInstance, Action callback, object pageData, bool isAsync) {
            if (string.IsNullOrEmpty(pageName) || pageInstance == null) {
                Debug.LogError("[UI] show page error with :" + pageName + " maybe null instance.");
                return;
            }

            if (m_allPages == null) {
                m_allPages = new Dictionary<string, TTUIPage>();
            }

            TTUIPage page = null;
            if (m_allPages.ContainsKey(pageName)) {
                page = m_allPages[pageName];
            }
            else {
                m_allPages.Add(pageName, pageInstance);
                page = pageInstance;
            }

            //if active before,wont active again.
            //if (page.isActive() == false)
            {
                //before show should set this data if need. maybe.!!
                page.setData(pageData);//m_data = pageData;

                if (isAsync)
                    page.Show(callback);
                else
                    page.Show();
            }
        }

        /// <summary>
        /// [同步]Sync Show Page
        /// </summary>
        public void ShowPage<T>() where T : TTUIPage, new() {
            ShowPage<T>(null, null, false);
        }
        /// <summary>
        /// [同步]使用指定的资源(指定uipath为page的名称和路径)显示页面
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uipath"></param>
        public void ShowPage<T>(string uipath) where T : TTUIPage, new() {
            T instance = new T();
            instance.name = uipath;
            instance.uiPath = uipath;
            ShowPage(uipath, instance);
        }
        /// <summary>
        /// Sync Show Page With Page Data Input.
        /// </summary>
        protected void ShowPage<T>(object pageData) where T : TTUIPage, new() {
            ShowPage<T>(null, pageData, false);
        }

        public void ShowPage(string pageName, TTUIPage pageInstance) {
            ShowPage(pageName, pageInstance, null, null, false);
        }

        protected void ShowPage(string pageName, TTUIPage pageInstance, object pageData) {
            ShowPage(pageName, pageInstance, null, pageData, false);
        }

        /// <summary>
        ///[异步] Async Show Page with Async loader bind in 'TTUIBind.Bind()'
        /// </summary>
        public void ShowPage<T>(Action callback) where T : TTUIPage, new() {
            ShowPage<T>(callback, null, true);
        }
        /// <summary>
        /// [异步]使用指定的资源(指定uipath为page的名称和路径)异步显示页面 Async Show Page with Async loader bind in 'TTUIBind.Bind()'
        /// </summary>
        public void ShowPage<T>(string uipath, Action callback) where T : TTUIPage, new() {
            T instance = new T();
            instance.name = uipath;
            instance.uiPath = uipath;
            ShowPage(uipath, instance, callback);
        }
        /// <summary>
        /// [异步]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        /// <param name="pageData"></param>
        protected void ShowPage<T>(Action callback, object pageData) where T : TTUIPage, new() {
            ShowPage<T>(callback, pageData, true);
        }

        /// <summary>
        ///[异步] Async Show Page with Async loader bind in 'TTUIBind.Bind()'
        /// </summary>
        public void ShowPage(string pageName, TTUIPage pageInstance, Action callback) {
            ShowPage(pageName, pageInstance, callback, null, true);
        }
        /// <summary>
        /// [异步]
        /// </summary>
        /// <param name="pageName"></param>
        /// <param name="pageInstance"></param>
        /// <param name="callback"></param>
        /// <param name="pageData"></param>
        protected void ShowPage(string pageName, TTUIPage pageInstance, Action callback, object pageData) {
            ShowPage(pageName, pageInstance, callback, pageData, true);
        }

        /// <summary>
        /// close current page in the "top" node.
        /// </summary>
        public void ClosePage() {
            //Debug.Log("Back&Close PageNodes Count:" + m_currentPageNodes.Count);

            if (m_currentPageNodes == null || m_currentPageNodes.Count <= 1)
                return;

            TTUIPage closePage = m_currentPageNodes[m_currentPageNodes.Count - 1];
            m_currentPageNodes.RemoveAt(m_currentPageNodes.Count - 1);

            //show older page.
            //TODO:Sub pages.belong to root node.
            if (m_currentPageNodes.Count > 0) {
                TTUIPage page = m_currentPageNodes[m_currentPageNodes.Count - 1];
                if (page.isAsyncUI)
                    ShowPage(page.name, page, () => {
                        closePage.Hide();
                    });
                else {
                    ShowPage(page.name, page);

                    //after show to hide().
                    closePage.Hide();
                }
            }
        }

        /// <summary>
        /// Close target page
        /// </summary>
        public void ClosePage(TTUIPage target) {
            if (target == null)
                return;
            if (target.isActive() == false) {
                if (m_currentPageNodes != null) {
                    for (int i = 0; i < m_currentPageNodes.Count; i++) {
                        if (m_currentPageNodes[i] == target) {
                            m_currentPageNodes.RemoveAt(i);
                            break;
                        }
                    }
                    return;
                }
            }

            if (m_currentPageNodes != null && m_currentPageNodes.Count >= 1 && m_currentPageNodes[m_currentPageNodes.Count - 1] == target) {
                m_currentPageNodes.RemoveAt(m_currentPageNodes.Count - 1);

                //show older page.
                //TODO:Sub pages.belong to root node.
                if (m_currentPageNodes.Count > 0) {
                    TTUIPage page = m_currentPageNodes[m_currentPageNodes.Count - 1];
                    if (page.isAsyncUI)
                        ShowPage(page.name, page, () => {
                            target.Hide();
                        });
                    else {
                        ShowPage(page.name, page);
                        target.Hide();
                    }

                    return;
                }
            }
            else if (target.CheckIfNeedBack()) {
                for (int i = 0; i < m_currentPageNodes.Count; i++) {
                    if (m_currentPageNodes[i] == target) {
                        m_currentPageNodes.RemoveAt(i);
                        target.Hide();
                        break;
                    }
                }
            }

            target.Hide();
        }

        public void ClosePage<T>() where T : TTUIPage {
            Type t = typeof(T);
            string pageName = t.ToString();

            if (m_allPages != null && m_allPages.ContainsKey(pageName)) {
                ClosePage(m_allPages[pageName]);
            }
            else {
                Debug.LogWarning(pageName + "havnt show yet!");
            }
        }

        public void ClosePage(string pageName) {
            if (m_allPages != null && m_allPages.ContainsKey(pageName)) {
                ClosePage(m_allPages[pageName]);
            }
            else {
                Debug.LogWarning(pageName + " havnt show yet!");
            }
        }

        #endregion
        #region 获取page
        /// <summary>
        /// 适用用指定了page名称的对象
        /// </summary>
        /// <param name="pageName"></param>
        /// <returns></returns>
        public TTUIPage GetPage(string pageName) {
            TTUIPage page = null;
            m_allPages.TryGetValue(pageName, out page);
            return page;
        }
        /// <summary>
        /// 适用于没有指定page名称的对象，使用默认typename作为默认名称
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetPageT<T>() where T : TTUIPage {
            Type t = typeof(T);
            string pageName = t.ToString();
            TTUIPage page = null;
            m_allPages.TryGetValue(pageName, out page);
            return page as T;
        }
        #endregion
        #region 声音播放
        //bool  isSoundPause;
        public void PlaySound_ButtonClick() {
            //SoundMgr.Instance.PlaySfxSound(clipName, true);
            SoundManager.Singleton.PlayUISound("click.ogg");
        }

        //public void PauseButtonClick() {
        //    isSoundPause = !isSoundPause;
        //    if (isSoundPause) Time.timeScale = 0;
        //    else Time.timeScale = 1;
        //}

       
        #endregion

        void OnDestroy() {
           
            //Debug.LogError(this.transform.gameObject.name);
            //m_Instance = null;
            if (this.m_allPages != null) {
                foreach (var v in m_allPages) {
                    v.Value._release();
                }
            }
            this.m_allPages = null;
            this.m_currentPageNodes = null;
            this.root = null;
            this.fixedRoot = null;
            this.normalRoot = null;
            this.popupRoot = null;
            this.uiCamera = null;
            //
            this.IsDestoryed = true;
        }
    }
}