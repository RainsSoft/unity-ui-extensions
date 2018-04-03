using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UberLogger
{
    /// <summary>
    /// UI长按触发click脚本
    /// </summary>
    /// <summary>
    /// 轻量级 按钮“监听器”组件 支持 鼠标长按触发click事件
    /// </summary>
    [DisallowMultipleComponent()]
    public class UberUILongPressClick : MonoBehaviour, IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        /// <summary>
        /// 得到“监听器”组件
        /// </summary>
        /// <param name="go">监听的游戏对象</param>
        /// <returns>
        /// 监听器
        /// </returns>
        public static UberUILongPressClick Get(GameObject go) {
            UberUILongPressClick lister = go.GetComponent<UberUILongPressClick>();
            if (lister == null) {
                lister = go.AddComponent<UberUILongPressClick>();                
            }
            return lister;
        }
        [SerializeField]
        private Button.ButtonClickedEvent _onLongPress = new Button.ButtonClickedEvent();
        private bool _handled;
        private bool _pressed;
        private float _pressedTime;
        public float LongPressDuration = 0.9f;
        public Button.ButtonClickedEvent onLongPress {
            get { return _onLongPress; }
            set { _onLongPress = value; }
        }
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) {
        }
        public  void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) {           
            _pressed = false;
        }

        public  void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData) {
           
            if (eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left) {
                return;
            }

            _pressed = true;
            _handled = false;
            _pressedTime = Time.realtimeSinceStartup;
        }

        public  void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData) {
            if (!_handled) {
               // base.OnPointerUp(eventData);
            }

            _pressed = false;
        }

        public  void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData) {
            if (!_handled) {
                //base.OnPointerClick(eventData);
               
            }
        }

        private void Update() {
            if (!_pressed) {
                return;
            }

            if (Time.realtimeSinceStartup - _pressedTime >= LongPressDuration) {
                _pressed = false;
                _handled = true;
                onLongPress.Invoke();
            }
        }
    }
}
