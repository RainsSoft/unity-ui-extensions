using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyTeam.UI {

    /// <summary>
    /// UI资源加载接口
    /// </summary>
    public interface IUIResLoader {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="fullpath">abname/assetname</param>
        /// <returns></returns>
        UnityEngine.Object LoadResSync(string fullpath);
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="fullpath">abname/assetname</param>
        /// <param name="callback">加载完成回调</param>
        void LoadResAsync(string fullpath, Action<UnityEngine.Object> callback);
    }
}
