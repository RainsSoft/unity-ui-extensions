namespace TinyTeam.UI {
    using System.Collections;
    using System;
    using UnityEngine;
    using Object = UnityEngine.Object;
    /// <summary>
    /// Bind Some Delegate Func For Yours.
    /// </summary>
    public class TTUIBind : MonoBehaviour {
        static bool isBind = false;
        /// <summary>
        /// 资源加载接口
        /// </summary>
        public static IUIResLoader UIResLoader;
        public static void Bind() {
            if(!isBind) {
                isBind = true;
                //Debug.LogWarning("Bind For UI Framework.");

                //bind for your loader api to load UI.
                TTUIPage.delegateSyncLoadUI = LoadAssetBundleSync;
                //TTUIPage.delegateAsyncLoadUI = UILoader.Load;
                TTUIPage.delegateAsyncLoadUI = LoadAssetBundleAsync;
            }
        }
        public static Object LoadAssetBundleSync(string name) {
            if(UIResLoader != null) {
                return UIResLoader.LoadResSync(name);
            }
            //同步加载
            var go = Resources.Load(name);
            return go;
        }
        public static void LoadAssetBundleAsync(string name, Action<UnityEngine.Object> callback) {
            //异步加载
            //Todo:这里使用resource加载
            if(UIResLoader != null) {
                UIResLoader.LoadResAsync(name, callback);
            }
            else {
                var go = Resources.Load(name);
                callback(go);
            }
        }
    }
}