using UnityEngine;
using System.Collections;
namespace TinyTeam.UI {

    /// <summary>
    /// UI相机适配器,多出屏幕部分给黑边,看情况UI相机选择是否附加当前组件
    /// </summary>
    //[SmartAssembly.Attributes.DoNotObfuscate()]
    [DisallowMultipleComponent()]
    [RequireComponent(typeof(Camera))]
    public class CameraAdapter : MonoBehaviour {
        /// <summary>
        /// UI相机视口比例
        /// </summary>
        public float Aspect {
            get {
                return aspect;
            }
            set {
                if (value > 0f) {
                    aspect = value;
                }
                RefreshCameraRect();
            }
        }

        /// <summary>
        /// UI相机视口比例
        /// </summary>
        private float aspect = 16 / (float)9;

        private bool defaultExcute = false;

        private int defaultMask = 0;
        private Color defaultColor;
        public bool NeedUpdateRect = false;
        public Camera uiCamera;
        public bool IsMainCamera = false;
        [Header("勾选此选项宽度不加黑边")]
        public bool WidthAdapter = true;

        void Awake() {
            defaultExcute = false;
        }

        void Start() {
            SetRefreshCameraRect();
        }

        private void OnPostRender() {
            if (NeedUpdateRect && !IsMainCamera) {
                UpdateRect();
                NeedUpdateRect = false;
            }
        }

        void OnApplicationFocus(bool hasFocus) {
            if (hasFocus) {
                RefreshCameraRect();
            }
        }

        private void SetRefreshCameraRect() {
            if (!defaultExcute) {
                RefreshCameraRect();
                defaultExcute = true;
            }
        }

        public void RefreshCameraRect() {
            if (uiCamera == null) {
                uiCamera = GetComponent<Camera>();
            }
            float aspectNow = Screen.width / (float)Screen.height;
            if (aspect > 0f && Mathf.Abs(aspectNow - aspect) > 0.01) {
                defaultMask = uiCamera.cullingMask;
                uiCamera.cullingMask = LayerMask.GetMask("Nothing");
                if (!defaultExcute) {
                    defaultColor = uiCamera.backgroundColor;
                }
                uiCamera.backgroundColor = new Color(0, 0, 0, 1);
                if (gameObject.activeInHierarchy) {
                    NeedUpdateRect = true;
                    uiCamera.rect = new Rect(0, 0, 1, 1);
                    uiCamera.Render();
                    //uiCamera.RenderDontRestore();
                }

            }
            else {
                uiCamera.rect = new Rect(0, 0, 1, 1);
            }
        }


        public void UpdateRect() {
            int defaultScreenWith = Screen.width;
            int defaultScreenHeight = Screen.height;
            float aspectNow = defaultScreenWith / (float)defaultScreenHeight;
            float targetH = 1f;
            float targetV = 1f;
            if (aspectNow > aspect) {
                if (!WidthAdapter) {
                    targetV = (defaultScreenHeight * aspect) / defaultScreenWith;
                }

            }
            else {
                targetH = defaultScreenWith / (defaultScreenHeight * aspect);
            }

            uiCamera.backgroundColor = defaultColor;
            uiCamera.cullingMask = defaultMask;
            if (targetH < 1f || targetV < 1f)//上下左右都切黑边
                                             //if (targetH < 1f)//只有上下切黑边，去掉左右切黑边
            {
                Rect rect = new Rect((1f - targetV) / 2f, (1f - targetH) / 2f, targetV, targetH);
                uiCamera.rect = rect;
            }
            else {
                uiCamera.pixelRect = new Rect(uiCamera.pixelRect.x,
                uiCamera.pixelRect.y, Screen.width, Screen.height);
                uiCamera.rect = new Rect(0, 0, 1, 1);
            }
        }
    }

    class CameraAspectRatioEnforcer : MonoBehaviour {

        private Camera tempCamera;
        public Camera TempCamera {
            get {
                if (tempCamera == null)
                    tempCamera = GetComponent<Camera>();
                return tempCamera;
            }
        }

        public float targetWidth = 1280f;
        public float targetHeight = 720f;

        private void LateUpdate() {
            // set the desired aspect ratio (the values in this example are
            // hard-coded for 16:9, but you could make them into public
            // variables instead so you can set them at design time)
            float targetaspect = targetWidth / targetHeight;

            // determine the game window's current aspect ratio
            float windowaspect = (float)Screen.width / (float)Screen.height;

            // current viewport height should be scaled by this amount
            float scaleheight = windowaspect / targetaspect;

            // if scaled height is less than current height, add letterbox
            if (scaleheight < 1.0f) {
                Rect rect = TempCamera.rect;

                rect.width = 1.0f;
                rect.height = scaleheight;

                rect.x = 0;
                rect.y = (1.0f - scaleheight) / 2.0f;

                TempCamera.rect = rect;
            }
            else // add pillarbox
            {
                float scalewidth = 1.0f / scaleheight;

                Rect rect = TempCamera.rect;

                rect.width = scalewidth;
                rect.height = 1.0f;

                rect.x = (1.0f - scalewidth) / 2.0f;
                rect.y = 0;

                TempCamera.rect = rect;
            }
        }
    }
}
