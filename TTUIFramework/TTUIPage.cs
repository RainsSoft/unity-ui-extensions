namespace TinyTeam.UI
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Each Page Mean one UI 'window'
    /// 3 steps:
    /// instance ui > refresh ui by data > show
    /// 
    /// by chiuan
    /// 2015-09
    /// 迁移静态操作方法到 TTUIRoot中,支持不同的level都有一个TTUIRoot
    /// by sgd
    /// 2017-11
    /// 为了适应不同分辨率,DPI，屏幕尺寸，我们UI在做的时候必须做一些取舍，以保持一套UI适应大部分PC,移动端
    /// UI设计规范:
    ///  1：定义不同屏幕纵横比的最小屏幕像素下，能保证功能UI不能重叠（也就是说低于这个最小配置的我们就不考虑了，UI重叠问题就不管了）
    ///     我们定义最小Y像素高度为640,最小X像素宽度为800，设计UI的时候保证像素在最小800x640时，UI不能重叠
    ///  2：设计UI时候我们一般基于高度适配ROOT Canvas Scaler脚本
    ///     UIScaleMode->ScaleWithScreenSize
    ///     Match height:1
    ///     ReferenceResolution-> x:1280/y:720=1.77 (大部分安卓机型，IPone5（1136/640=1.78）的比例差不多，需要注意的是屏幕高度是640)
    ///     也就是说我们在 1280/720屏幕尺寸设计的UI，我们一要保证纵向UI功能按钮在高度为640像素时，能全部显示并不能有重叠。
    ///     二要保证横向UI功能按钮在宽度为1000像素时，能全部显示并不能有重叠配合.
    ///  3：对于 宽小于1000，高小于640，我们对 ROOT 进行缩放适配
    ///     Canvas Scaler脚本
    ///     UIScaleMode->ScaleWithScreenSize
    ///     ScreenMatchMode->Expand
    ///     配合xARM+Aspect+and+Resolution+Master插件 我们取最优值
    ///  4：功能按钮尽量靠边对齐(上下左右)，中间的UI如果居中对齐，需要注意在纵横比比较小的时候，不能叠一起
    ///      配合xARM+Aspect+and+Resolution+Master插件，进行UI取舍
    ///by sgd 
    ///2017-12
    /// </summary>

    #region define

    public enum UIType
    {
        Normal,
        Fixed,
        PopUp,
        None,      //独立的窗口
    }

    public enum UIMode
    {
        DoNothing,
        HideOther,     // 闭其他界面
        NeedBack,      // 点击返回按钮关闭当前,不关闭其他界面(需要调整好层级关系)
        NoNeedBack,    // 关闭TopBar,关闭其他界面,不加入backSequence队列
    }

    public enum UICollider
    {
        None,      // 显示该界面不包含碰撞背景
        Normal,    // 碰撞透明背景
        WithBg,    // 碰撞非透明背景
    }
    #endregion
    /// <summary>
    /// 进行改造，每个scene一个UIROOT,在切换scene的时候 直接销毁老的UIROOT就好了
    /// </summary>
    public abstract class TTUIPage 
    {
        /// <summary>
        /// 所有UI节点下面的子节点必须为UI节点，而不是Transform子节点
        /// </summary>
        /// <param name="root"></param>
        /// <param name="allchilds"></param>
        protected void GetUIObjAllChilds(RectTransform root, ref List<RectTransform> allchilds) {
            for (int i = 0; i < root.childCount; i++) {
                RectTransform child = root.GetChild(i) as RectTransform;

#if DEBUG
                //Debug.Log("GetUIObjAllChilds(...)=>"+child.name);
                for (int j = 0; j < allchilds.Count; j++) {
                    if (string.Compare(allchilds[j].name, child.name, true) == 0) {
                        //Debug.Log("重复子节点名称:GetUIObjAllChilds(...)=>" + child.name);
                        break;
                    }
                }
#endif
                allchilds.Add(child);
                GetUIObjAllChilds(child, ref allchilds);
            }
        }
        public string name = string.Empty;
        /// <summary>
        /// 是否被销毁
        /// </summary>
        public bool IsDestoryed { get; private set; }
        //this page's id
        public int id = -1;

        //this page's type
        public UIType type = UIType.Normal;

        //how to show this page.
        public UIMode mode = UIMode.DoNothing;

        //the background collider mode
        public UICollider collider = UICollider.None;

        //path to load ui
        public string uiPath = string.Empty;

        //this ui's gameobject
        public GameObject gameObject;
        public Transform transform;

        ////all pages with the union type
        //private static Dictionary<string, TTUIPage> m_allPages;
        //public static Dictionary<string, TTUIPage> allPages { get { return m_allPages; } }

        ////control 1>2>3>4>5 each page close will back show the previus page.
        //private static List<TTUIPage> m_currentPageNodes;
        //public static List<TTUIPage> currentPageNodes { get { return m_currentPageNodes; } }

        //record this ui load mode.async or sync.
        internal bool isAsyncUI = false;

        //this page active flag
        protected bool isActived = false;

        //refresh page 's data.
        private object m_data = null;
        protected object data { get { return m_data; } }
        /// <summary>
        /// 初始默认隐藏页面后 data就设置为null,现在已经改造
        /// 当执行 _realse时候 data才设置为null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T getDataT<T>() {
            return (T)m_data;
        }
        public void setData(object data) {
            m_data = data;
        }
        /// <summary>
        /// 外面赋值 指定加载UI预设方式（比如从assetbundle加载）
        /// </summary>
        //delegate load ui function.
        public static Func<string, Object> delegateSyncLoadUI = null;
        /// <summary>
        /// 外面赋值 指定加载UI预设方式（比如从assetbundle加载）
        /// </summary>
        public static Action<string, Action<Object>> delegateAsyncLoadUI = null;
        #region 对外提供辅助方法
        /// <summary>
        /// [同步]创建额外独立的UI，其特性是由外面管理，只和page共用加载prefab方法
        /// </summary>
        /// <param name="uipath"></param>
        /// <returns></returns>
        public static GameObject CreateUIByPathOnly(string uipath) {
            GameObject go = null;
            //1:instance UI
            if (string.IsNullOrEmpty(uipath) == false) {
                if (delegateSyncLoadUI != null) {
                    Object o = delegateSyncLoadUI(uipath);
                    go = o != null ? GameObject.Instantiate(o) as GameObject : null;
                }
                else {
                    go = GameObject.Instantiate(Resources.Load(uipath)) as GameObject;
                }
                //protected.
                if (go == null) {
                    Debug.LogError("[UI] Cant sync load your ui prefab.");
                }
            }
            return go;
        }
        /// <summary>
        /// [异步]创建额外独立的UI，其特性是由外面管理，只和page共用加载prefab方法
        /// </summary>
        /// <param name="uipath"></param>
        /// <returns></returns>        
        public static IEnumerator AsyncCreateUIByPathOnly(string uipath, Action<GameObject> uiGoCreateCallback) {
            GameObject go = null;
            bool _loading = true;
            delegateAsyncLoadUI(uipath, (o) => {
                go = o != null ? GameObject.Instantiate(o) as GameObject : null;
                _loading = false;
            }
            );
            //这里的超时跳出,有啥用啊,前面的异步加载又无法中止
            float _t0 = Time.realtimeSinceStartup;
            while (_loading) {
                if (Time.realtimeSinceStartup - _t0 >= 10.0f) {
                    Debug.LogError("[UI] WTF async load your ui prefab timeout!");
                    //超时跳出
                    yield break;
                }
                yield return null;
            }
            if (uiGoCreateCallback != null) {
                uiGoCreateCallback(go);
            }
        }
        /// <summary>
        ///[同步]辅助方法:拉伸填充到父节点
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="uiParent"></param>
        public static void AnchorUIGoToParent(RectTransform ui, RectTransform uiParent, bool StretchFill) {
            if (ui == null || uiParent == null) return;

            //移除canvas           
            CanvasScaler cs = ui.GetComponent<CanvasScaler>();
            if (cs != null) {
                //GameObject.Destroy(cs);
                cs.enabled = false;
            }
            GraphicRaycaster gr = ui.GetComponent<GraphicRaycaster>();
            if (gr != null) {
                //GameObject.Destroy(gr);
                //gr.enabled = false;
            }
            Canvas cp = ui.GetComponent<Canvas>();
            if (cp != null) {
                //GameObject.Destroy(cp);
                //对子canvas的处理
                //cp.enabled = false;
            }
            //
            //check if this is ugui or (ngui)?
            Vector3 anchorPos = Vector3.zero;
            Vector2 sizeDel = Vector2.zero;
            Vector3 scale = Vector3.one;
            //if (ui != null) {
            anchorPos = ui.anchoredPosition;
            sizeDel = ui.sizeDelta;
            scale = ui.localScale;
            //}
            ui.SetParent(uiParent, false);
            //if (ui.GetComponent<RectTransform>() != null) {
            ui.anchoredPosition = anchorPos;
            ui.sizeDelta = sizeDel;
            ui.localScale = scale;
            //}
            //子canvas保持和父canvas 一致
            //RectTransform rtf = ui.GetComponent<RectTransform>();
            //if (rtf != null && rtf.parent != null) {
            if (StretchFill) {
                ui.anchorMax = new Vector2(1f, 1f);
                ui.anchorMin = new Vector2(0f, 0f);
                ui.anchoredPosition = new Vector2(0f, 0f);
                ui.anchoredPosition3D = new Vector3(0f, 0f, 0f);
                ui.offsetMax = new Vector2(0f, 0f);
                ui.offsetMin = new Vector2(0f, 0f);
                //
                ui.sizeDelta = new Vector2(0f, 0f);
            }
            //}
        }
        #endregion

        #region virtual api

        ///When Instance UI Ony Once.
        public abstract void Awake(GameObject go);

        ///Show UI Refresh Eachtime.
        public abstract void Refresh();
        
        ///Active this UI
        public virtual void Active() {
            this.gameObject.SetActive(true);
            isActived = true;
        }

        /// <summary>
        /// Only Deactive UI wont clear Data.
        /// </summary>
        public virtual void Hide() {
            this.gameObject.SetActive(false);
            isActived = false;
            //set this page's data null when hide.
            //this.m_data = null;
            Resources.UnloadUnusedAssets();
        }

        #endregion

        #region internal api
        protected TTUIRoot TTUIRoot_Instance;
        private TTUIPage() { }
        /// <summary>
        /// 继承当前实现
        /// </summary>
        /// <param name="levelName">指定scene level名称 我们每个scene level必须有一个uiroot_ 对象</param>
        /// <param name="type"></param>
        /// <param name="mod"></param>
        /// <param name="col"></param>
        public TTUIPage(string levelName/*scene level节点名称*/, UIType type, UIMode mod, UICollider col) {
            this.type = type;
            this.mode = mod;
            this.collider = col;
            this.name = this.GetType().ToString();
            TTUIRoot_Instance = TTUIRoot.CreateOrFind(levelName);
            //when create one page.
            //bind special delegate .
            TTUIBind.Bind();
            //Debug.LogWarning("[UI] create page:" + ToString());
        }

        /// <summary>
        /// Sync Show UI Logic
        /// </summary>
        protected internal void Show() {
            //1:instance UI
            if (this.gameObject == null && string.IsNullOrEmpty(uiPath) == false) {
                GameObject go = null;
                //if (delegateSyncLoadUI != null) {
                //    Object o = delegateSyncLoadUI(uiPath);
                //    go = o != null ? GameObject.Instantiate(o) as GameObject : null;
                //}
                //else {
                //    go = GameObject.Instantiate(Resources.Load(uiPath)) as GameObject;
                //}
                go = CreateUIByPathOnly(uiPath);
                //protected.
                if (go == null) {
                    //Debug.LogError("[UI] Cant sync load your ui prefab.");
                    return;
                }

                AnchorUIGameObject(go);

                //after instance should awake init.
                Awake(go);

                //mark this ui sync ui
                isAsyncUI = false;
            }

            //:animation or init when active.
            Active();

            //:refresh ui component.
            Refresh();

            //:popup this node to top if need back.
            PopNode(this);
        }

        /// <summary>
        /// Async Show UI Logic
        /// </summary>
        protected internal void Show(Action callback) {
            //TTUIRoot.Instance.StartCoroutine(AsyncShow(callback));
            TTUIRoot_Instance.StartCoroutine(AsyncShow(callback));
        }

        IEnumerator AsyncShow(Action callback) {
            //1:Instance UI
            //FIX:support this is manager multi gameObject,instance by your self.
            if (this.gameObject == null && string.IsNullOrEmpty(uiPath) == false) {
                GameObject go = null;
                //bool _loading = true;
                /*
                delegateAsyncLoadUI(uiPath, (o) => {
                    go = o != null ? GameObject.Instantiate(o) as GameObject : null;
                    AnchorUIGameObject(go);
                    Awake(go);
                    isAsyncUI = true;
                    _loading = false;

                    //:animation active.
                    Active();

                    //:refresh ui component.
                    Refresh();

                    //:popup this node to top if need back.
                    PopNode(this);

                    if (callback != null) callback();
                });

                float _t0 = Time.realtimeSinceStartup;
                while (_loading) {
                    if (Time.realtimeSinceStartup - _t0 >= 10.0f) {
                        Debug.LogError("[UI] WTF async load your ui prefab timeout!");
                        yield break;
                    }
                    yield return null;
                }
                */


                var loadAsync = AsyncCreateUIByPathOnly(uiPath, (o) => {
                    go = o;
                    //
                    AnchorUIGameObject(go);
                    Awake(go);
                    isAsyncUI = true;
                    //_loading = false;

                    //:animation active.
                    Active();

                    //:refresh ui component.
                    Refresh();

                    //:popup this node to top if need back.
                    PopNode(this);

                    if(callback != null)
                        callback();
                });
                while(loadAsync.MoveNext()) {
                    yield return null;
                }


            }
            else {
                //:animation active.
                Active();

                //:refresh ui component.
                Refresh();

                //:popup this node to top if need back.
                PopNode(this);

                if (callback != null) callback();
            }
        }

        internal bool CheckIfNeedBack() {
            if (type == UIType.Fixed || type == UIType.PopUp || type == UIType.None) return false;
            else if (mode == UIMode.NoNeedBack || mode == UIMode.DoNothing) return false;
            return true;
        }
        /// <summary>
        /// 对预设预处理
        /// </summary>
        /// <param name="ui"></param>
        protected virtual void AnchorUIGameObject_Before(GameObject ui) {
            //移除canvas           
            CanvasScaler cs = ui.GetComponent<CanvasScaler>();
            if (cs != null) {
                //GameObject.Destroy(cs);
                cs.enabled = false;
            }
            GraphicRaycaster gr = ui.GetComponent<GraphicRaycaster>();
            if (gr != null) {
                //GameObject.Destroy(gr);
                //gr.enabled = false;
            }
            Canvas cp = ui.GetComponent<Canvas>();
            if (cp != null) {
                //GameObject.Destroy(cp);
                //对子canvas的处理
                //cp.enabled = false;
            }
        }
        protected virtual void AnchorUIGameObject_After(GameObject ui) {
            //子canvas保持和父canvas 一致
            RectTransform rtf = ui.GetComponent<RectTransform>();
            if (rtf != null && rtf.parent != null) {
                rtf.anchorMax = new Vector2(1f, 1f);
                rtf.anchorMin = new Vector2(0f, 0f);
                rtf.anchoredPosition = new Vector2(0f, 0f);
                rtf.anchoredPosition3D = new Vector3(0f, 0f, 0f);
                rtf.offsetMax = new Vector2(0f, 0f);
                rtf.offsetMin = new Vector2(0f, 0f);
                //
                rtf.sizeDelta = new Vector2(0f, 0f);
            }
        }
        protected void AnchorUIGameObject(GameObject ui) {
            //if (TTUIRoot.Instance == null || ui == null) return;
            if (TTUIRoot_Instance == null || ui == null) return;
            AnchorUIGameObject_Before(ui);
            this.gameObject = ui;
            this.transform = ui.transform;

            //check if this is ugui or (ngui)?
            Vector3 anchorPos = Vector3.zero;
            Vector2 sizeDel = Vector2.zero;
            Vector3 scale = Vector3.one;
            if (ui.GetComponent<RectTransform>() != null) {
                anchorPos = ui.GetComponent<RectTransform>().anchoredPosition;
                sizeDel = ui.GetComponent<RectTransform>().sizeDelta;
                scale = ui.GetComponent<RectTransform>().localScale;
            }
            else {
                anchorPos = ui.transform.localPosition;
                scale = ui.transform.localScale;
            }

            //Debug.Log("anchorPos:" + anchorPos + "|sizeDel:" + sizeDel);

            if (type == UIType.Fixed) {
                //ui.transform.SetParent(TTUIRoot.Instance.fixedRoot);
                ui.transform.SetParent(TTUIRoot_Instance.fixedRoot);
            }
            else if (type == UIType.Normal) {
                //ui.transform.SetParent(TTUIRoot.Instance.normalRoot);
                ui.transform.SetParent(TTUIRoot_Instance.normalRoot);
            }
            else if (type == UIType.PopUp) {
                //ui.transform.SetParent(TTUIRoot.Instance.popupRoot);
                ui.transform.SetParent(TTUIRoot_Instance.popupRoot);
            }


            if (ui.GetComponent<RectTransform>() != null) {
                ui.GetComponent<RectTransform>().anchoredPosition = anchorPos;
                ui.GetComponent<RectTransform>().sizeDelta = sizeDel;
                ui.GetComponent<RectTransform>().localScale = scale;
            }
            else {
                ui.transform.localPosition = anchorPos;
                ui.transform.localScale = scale;
            }
            AnchorUIGameObject_After(ui);
        }

        public override string ToString() {
            return ">Name:" + name + ",ID:" + id + ",Type:" + type.ToString() + ",ShowMode:" + mode.ToString() + ",Collider:" + collider.ToString();
        }

        public bool isActive() {
            //fix,if this page is not only one gameObject
            //so,should check isActived too.
            bool ret = gameObject != null && gameObject.activeSelf;
            return ret || isActived;
        }

        #endregion

        #region static api
        private void PopNode(TTUIPage page) {
            TTUIRoot_Instance.PopNode(page);
        }
#if STATIC_API
        private static bool CheckIfNeedBack(TTUIPage page) {
            return page != null && page.CheckIfNeedBack();
        }

        /// <summary>
        /// make the target node to the top.
        /// </summary>
        private static void PopNode(TTUIPage page) {
            List<TTUIPage> m_currentPageNodes = page.TTUIRoot_Instance.currentPageNodes; 
            if (m_currentPageNodes == null) {
                Debug.LogError("[UI] TTUIRoot_Instance.currentPageNodes is null.");
                //m_currentPageNodes = new List<TTUIPage>();
                return;
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

        private static void HideOldNodes() {
            List<TTUIPage> m_currentPageNodes = page.TTUIRoot_Instance.currentPageNodes;

            if (m_currentPageNodes.Count < 0) return;
            TTUIPage topPage = m_currentPageNodes[m_currentPageNodes.Count - 1];
            if (topPage.mode == UIMode.HideOther) {
                //form bottm to top.
                for (int i = m_currentPageNodes.Count - 2; i >= 0; i--) {
                    if (m_currentPageNodes[i].isActive())
                        m_currentPageNodes[i].Hide();
                }
            }
        }

        public static void ClearNodes() {
            m_currentPageNodes.Clear();
        }

        private static void ShowPage<T>(Action callback, object pageData, bool isAsync) where T : TTUIPage, new() {
            Type t = typeof(T);
            string pageName = t.ToString();

            if (m_allPages != null && m_allPages.ContainsKey(pageName)) {
                ShowPage(pageName, m_allPages[pageName], callback, pageData, isAsync);
            }
            else {
                T instance = new T();
                ShowPage(pageName, instance, callback, pageData, isAsync);
            }
        }

        private static void ShowPage(string pageName, TTUIPage pageInstance, Action callback, object pageData, bool isAsync) {
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
                page.m_data = pageData;

                if (isAsync)
                    page.Show(callback);
                else
                    page.Show();
            }
        }

        /// <summary>
        /// Sync Show Page
        /// </summary>
        public static void ShowPage<T>() where T : TTUIPage, new() {
            ShowPage<T>(null, null, false);
        }

        /// <summary>
        /// Sync Show Page With Page Data Input.
        /// </summary>
        public static void ShowPage<T>(object pageData) where T : TTUIPage, new() {
            ShowPage<T>(null, pageData, false);
        }

        public static void ShowPage(string pageName, TTUIPage pageInstance) {
            ShowPage(pageName, pageInstance, null, null, false);
        }

        public static void ShowPage(string pageName, TTUIPage pageInstance, object pageData) {
            ShowPage(pageName, pageInstance, null, pageData, false);
        }

        /// <summary>
        /// Async Show Page with Async loader bind in 'TTUIBind.Bind()'
        /// </summary>
        public static void ShowPage<T>(Action callback) where T : TTUIPage, new() {
            ShowPage<T>(callback, null, true);
        }

        public static void ShowPage<T>(Action callback, object pageData) where T : TTUIPage, new() {
            ShowPage<T>(callback, pageData, true);
        }

        /// <summary>
        /// Async Show Page with Async loader bind in 'TTUIBind.Bind()'
        /// </summary>
        public static void ShowPage(string pageName, TTUIPage pageInstance, Action callback) {
            ShowPage(pageName, pageInstance, callback, null, true);
        }

        public static void ShowPage(string pageName, TTUIPage pageInstance, Action callback, object pageData) {
            ShowPage(pageName, pageInstance, callback, pageData, true);
        }

        /// <summary>
        /// close current page in the "top" node.
        /// </summary>
        public static void ClosePage() {
            //Debug.Log("Back&Close PageNodes Count:" + m_currentPageNodes.Count);

            if (m_currentPageNodes == null || m_currentPageNodes.Count <= 1) return;

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
        public static void ClosePage(TTUIPage target) {
            if (target == null) return;
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

        public static void ClosePage<T>() where T : TTUIPage {
            Type t = typeof(T);
            string pageName = t.ToString();

            if (m_allPages != null && m_allPages.ContainsKey(pageName)) {
                ClosePage(m_allPages[pageName]);
            }
            else {
                Debug.LogError(pageName + "havnt show yet!");
            }
        }

        public static void ClosePage(string pageName) {
            if (m_allPages != null && m_allPages.ContainsKey(pageName)) {
                ClosePage(m_allPages[pageName]);
            }
            else {
                Debug.LogError(pageName + " havnt show yet!");
            }
        }
#endif
        #endregion
        protected abstract void Destory();
        /// <summary>
        /// 释放引用
        /// </summary>
        internal void _release() {
            this.Destory();
            this.isActived = false;
            this.TTUIRoot_Instance = null;
            this.m_data = null;
            //this ui's gameobject, 其对象由root管理以及销毁，这里只是引用
            this.gameObject = null;
            this.transform = null;
            //
            this.IsDestoryed = true;
        }
    }//TTUIPage
}//namespace