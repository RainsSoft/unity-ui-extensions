using UnityEngine;
using System.Collections;
namespace TinyTeam.UI
{

    /// <summary>
    /// 通用的gameobject关联自定义数据，通常用于预设中保存数据
    /// </summary>
    //[SmartAssembly.Attributes.DoNotObfuscate()]
    [DisallowMultipleComponent()]
    public class TTUILinkData : MonoBehaviour
    {
        /// <summary>
        /// 获取关联数据，如果没有，则增加TTUILinkData组件。使用时候要注意，
        /// 只有明确要关联数据的控件才使用该方法
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static TTUILinkData Get(GameObject go) {
            TTUILinkData lister = go.GetComponent<TTUILinkData>();
            if (lister == null) {
                lister = go.AddComponent<TTUILinkData>();
            }
            return lister;
        }
        public string tagStr1;//可永久序列化，可临时
        public string tagStr2;
        public string tagStr3;
        //
        public object tagObj1 { get; set; }//用于临时
        public object tagObj2 { get; set; }
        public object tagObj3 { get; set; }

        void OnDestroy() {
            tagObj1 = null;
            tagObj2 = null;
            tagObj3 = null;
        }
    }


}
