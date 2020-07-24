using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace UberLogger
{
   
   
    /// <summary>
	/// Base class for all counters.
	/// </summary>
	[Serializable]
    public abstract class BaseCounterData
    {
        protected MonoBehaviour main;
        [SerializeField]
        protected bool enabled = true;       
        //
        internal StringBuilder text;
        internal bool dirty = false;        
        /// <summary>
        /// Enables or disables counter with immediate label refresh.
        /// </summary>
        public bool Enabled {
            get { return enabled; }
            set {
                if (enabled == value || !Application.isPlaying) return;

                enabled = value;

                if (enabled) {
                    Activate();
                }
                else {
                    Deactivate();
                }
                //main.UpdateTexts();
            }
        }

 

  

        /// <summary>
        /// Updates counter's value and forces label refresh.
        /// </summary>
        public void Refresh() {
            if (!enabled || !Application.isPlaying) return;
            UpdateValue(true);
            //main.UpdateTexts();
        }

        // you have to cache color html tag to avoid extra alloactions
        //protected abstract void CacheCurrentColor();

        internal virtual void UpdateValue() {
            UpdateValue(false);
        }

        internal virtual void UpdateValue(bool force) { }

        public void Init(MonoBehaviour reference) {
            main = reference;
        }

        public void Dispose() {
            //main = null;

            if (text != null) {
                text.Remove(0, text.Length);
                text = null;
            }
        }

        public virtual void Activate() {
            //if (main.OperationMode == AFPSCounterOperationMode.Normal) {
                if (text == null) {
                    text = new StringBuilder(100);
                }
                else {
                    text.Remove(0, text.Length);
                }
            //}
        }

        internal virtual void Deactivate() {
            if (text != null) {
                text.Remove(0, text.Length);
            }
            //main.MakeDrawableLabelDirty(anchor);
        }
    }
    /// <summary>
	/// Shows frames per second counter.
	/// </summary>
	[Serializable]
    public class FPSCounterData : BaseCounterData
    {
        //private const string COROUTINE_NAME = "UpdateFPSCounter";
        private IEnumerator UpdateFPSCounter() {
            
            
            FPSCounterData fpsCounter = this;
            while (true) {
                float previousUpdateTime = Time.time;
                int previousUpdateFrames = Time.frameCount;

                yield return new WaitForSeconds(fpsCounter.UpdateInterval);

                float timeElapsed = Time.time - previousUpdateTime;
                int framesChanged = Time.frameCount - previousUpdateFrames;

                // flooring FPS
                int fps = (int)(framesChanged / (timeElapsed / Time.timeScale));

                fpsCounter.newValue = fps;
                fpsCounter.UpdateValue(false);
                //UpdateTexts();
            }
        }
        //private const string FPS_TEXT_START = "<color=#{0}><b>FPS: ";
        private const string FPS_TEXT_START = " FPS: ";
        //private const string FPS_TEXT_END = "</b></color>";

        //private const string MIN_TEXT_START = "\n<color=#{0}><b>MIN: ";
        private const string MIN_TEXT_START = " MIN: ";
        //private const string MIN_TEXT_END = "</b></color> ";

        //private const string MAX_TEXT_START = "<color=#{0}><b>MAX: ";
        private const string MAX_TEXT_START = " MAX: ";
        //private const string MAX_TEXT_END = "</b></color>";

        //private const string AVG_TEXT_START = " <color=#{0}><b>AVG: ";
        private const string AVG_TEXT_START = " AVG: ";
        //private const string AVG_TEXT_END = "</b></color>";

        /// <summary>
        /// If FPS will drop below this value, colorWarning will be used for counter text.
        /// </summary>
        public int warningLevelValue = 30;

        /// <summary>
        /// If FPS will be equal or less this value, colorCritical will be used for counter text.
        /// </summary>
        public int criticalLevelValue = 10;

        ///// <summary>
        ///// Average FPS counter accumulative data will be reset on new scene load if enabled.
        ///// </summary>
        //public bool resetAverageOnNewScene = false;

        ///// <summary>
        ///// Minimum and maximum FPS readings will be reset on new scene load if enabled.
        ///// </summary>
        //public bool resetMinMaxOnNewScene = false;
        public string lastText = "";
        /// <summary>
        /// Last calculated FPS value.
        /// </summary>
        [HideInInspector]
        public int lastValue = 0;

        /// <summary>
        /// Last calculated Average FPS value.
        /// </summary>
        [HideInInspector]
        public int lastAverageValue = 0;

        /// <summary>
        /// Last minimum FPS value.
        /// </summary>
        [HideInInspector]
        public int lastMinimumValue = -1;

        /// <summary>
        /// Last maximum FPS value.
        /// </summary>
        [HideInInspector]
        public int lastMaximumValue = -1;

        [SerializeField]
        [Range(0.1f, 10f)]
        private float updateInterval = 0.5f;

        [SerializeField]
        private bool showAverage = true;

        [SerializeField]
        [Range(0, 100)]
        private int averageFromSamples = 100;

        [SerializeField]
        private bool showMinMax = true;

     

        internal int newValue;

   

        private bool inited;

        private int currentAverageSamples;
        private float currentAverageRaw;
        private float[] accumulatedAverageSamples;

        public FPSCounterData() {
            //color = new Color32(85, 218, 102, 255);
            
        }
        
        #region properties
        /// <summary>
        /// Counter's value update interval.
        /// </summary>
        public float UpdateInterval {
            get { return updateInterval; }
            set {
                if (Math.Abs(updateInterval - value) < 0.001f || !Application.isPlaying) return;

                updateInterval = value;
                if (!enabled) return;

                RestartCoroutine();
            }
        }

        /// <summary>
        /// Shows Average FPS calculated from specified #AverageFromSamples amount or since game / scene start, depending on %AverageFromSamples value and #resetAverageOnNewScene toggle.
        /// </summary>
        public bool ShowAverage {
            get { return showAverage; }
            set {
                if (showAverage == value || !Application.isPlaying) return;
                showAverage = value;
                if (!enabled) return;
                if (!showAverage) ResetAverage();

                Refresh();
            }
        }

        /// <summary>
        /// Amount of last samples to get average from. Set 0 to get average from all samples since startup or level load. One Sample recorded per #UpdateInterval.
        /// </summary>
        public int AverageFromSamples {
            get { return averageFromSamples; }
            set {
                if (averageFromSamples == value || !Application.isPlaying) return;
                averageFromSamples = value;
                if (!enabled) return;

                if (averageFromSamples > 0) {
                    if (accumulatedAverageSamples == null) {
                        accumulatedAverageSamples = new float[averageFromSamples];
                    }
                    else if (accumulatedAverageSamples.Length != averageFromSamples) {
                        Array.Resize(ref accumulatedAverageSamples, averageFromSamples);
                    }
                }
                else {
                    accumulatedAverageSamples = null;
                }
                ResetAverage();
                Refresh();
            }
        }

        /// <summary>
        /// Shows minimum and maximum FPS readings since game / scene start, depending on #resetMinMaxOnNewScene toggle.
        /// </summary>
        public bool ShowMinMax {
            get { return showMinMax; }
            set {
                if (showMinMax == value || !Application.isPlaying) return;
                showMinMax = value;
                if (!enabled) return;
                if (!showMinMax) ResetMinMax();

                Refresh();
            }
        }

       

        #endregion

        /// <summary>
        /// Resets Average FPS counter accumulative data.
        /// </summary>
        public void ResetAverage() {
            lastAverageValue = 0;
            currentAverageSamples = 0;
            currentAverageRaw = 0;

            if (averageFromSamples > 0 && accumulatedAverageSamples != null) {
                Array.Clear(accumulatedAverageSamples, 0, accumulatedAverageSamples.Length);
            }
        }

        /// <summary>
        /// Resets minimum and maximum FPS readings.
        /// </summary>
        public void ResetMinMax() {
            lastMinimumValue = -1;
            lastMaximumValue = -1;
            UpdateValue(true);
            dirty = true;
        }

        public override void Activate() {
            if (!enabled || inited) return;
            base.Activate();
            inited = true;

            lastValue = 0;

            //if (main.OperationMode == AFPSCounterOperationMode.Normal) {
            //    if (colorCached == null) {
            //        CacheCurrentColor();
            //    }

            //    if (colorWarningCached == null) {
            //        CacheWarningColor();
            //    }

            //    if (colorCriticalCached == null) {
            //        CacheCriticalColor();
            //    }

                text.Append("0");//text.Append(colorCriticalCached).Append("0").Append(FPS_TEXT_END);                
                dirty = true;
            //}
            
            main.StartCoroutine(UpdateFPSCounter());
        }

        internal override void Deactivate() {
            if (!inited) return;
            base.Deactivate();

            main.StopCoroutine(UpdateFPSCounter());
            ResetMinMax();
            ResetAverage();
            lastValue = 0;

            inited = false;
        }

        internal override void UpdateValue(bool force) {
            if (!enabled) return;

            if (lastValue != newValue || force) {
                lastValue = newValue;
                dirty = true;
            }

            int currentAverageRounded = 0;
            if (showAverage) {
                if (averageFromSamples == 0) {
                    currentAverageSamples++;
                    currentAverageRaw += (lastValue - currentAverageRaw) / currentAverageSamples;
                }
                else {
                    if (accumulatedAverageSamples == null) {
                        accumulatedAverageSamples = new float[averageFromSamples];
                        ResetAverage();
                    }

                    accumulatedAverageSamples[currentAverageSamples % averageFromSamples] = lastValue;
                    currentAverageSamples++;

                    currentAverageRaw = GetAverageFromAccumulatedSamples();
                }

                currentAverageRounded = Mathf.RoundToInt(currentAverageRaw);

                if (lastAverageValue != currentAverageRounded || force) {
                    lastAverageValue = currentAverageRounded;
                    dirty = true;
                }
            }

            if (showMinMax && dirty) {
                if (lastMinimumValue == -1)
                    lastMinimumValue = lastValue;
                else if (lastValue < lastMinimumValue) {
                    lastMinimumValue = lastValue;
                    dirty = true;
                }

                if (lastMaximumValue == -1)
                    lastMaximumValue = lastValue;
                else if (lastValue > lastMaximumValue) {
                    lastMaximumValue = lastValue;
                    dirty = true;
                }
            }

            if (dirty ) {
                //string color;

                //if (lastValue >= warningLevelValue)
                //    color = colorCached;
                //else if (lastValue <= criticalLevelValue)
                //    color = colorCriticalCached;
                //else
                //    color = colorWarningCached;

                text.Length = 0;
                text.Append(FPS_TEXT_START).Append(lastValue);//text.Append(color).Append(lastValue).Append(FPS_TEXT_END);

                if (showAverage) {
                    //    if (currentAverageRounded >= warningLevelValue)
                    //        color = colorCachedAvg;
                    //    else if (currentAverageRounded <= criticalLevelValue)
                    //        color = colorCriticalCachedAvg;
                    //    else
                    //        color = colorWarningCachedAvg;

                    text.Append(AVG_TEXT_START).Append(currentAverageRounded);//    text.Append(color).Append(currentAverageRounded).Append(AVG_TEXT_END);
                }

                if (showMinMax) {
                    //    if (lastMinimumValue >= warningLevelValue)
                    //        color = colorCachedMin;
                    //    else if (lastMinimumValue <= criticalLevelValue)
                    //        color = colorCriticalCachedMin;
                    //    else
                    //        color = colorWarningCachedMin;

                    text.Append(MIN_TEXT_START).Append(lastMinimumValue);//    text.Append(color).Append(lastMinimumValue).Append(MIN_TEXT_END);

                    //    if (lastMaximumValue >= warningLevelValue)
                    //        color = colorCachedMax;
                    //    else if (lastMaximumValue <= criticalLevelValue)
                    //        color = colorCriticalCachedMax;
                    //    else
                    //        color = colorWarningCachedMax;

                    text.Append(MAX_TEXT_START).Append(lastMaximumValue);//    text.Append(color).Append(lastMaximumValue).Append(MAX_TEXT_END);
                }
                lastText = text.ToString();
            }
        }

        private void RestartCoroutine() {
            main.StopCoroutine(UpdateFPSCounter());
            main.StartCoroutine(UpdateFPSCounter());
        }

        private float GetAverageFromAccumulatedSamples() {
            float averageFps;
            float totalFps = 0;

            for (int i = 0; i < averageFromSamples; i++) {
                totalFps += accumulatedAverageSamples[i];
            }

            if (currentAverageSamples < averageFromSamples) {
                averageFps = totalFps / currentAverageSamples;
            }
            else {
                averageFps = totalFps / averageFromSamples;
            }

            return averageFps;
        }
    }

    /// <summary>
	/// Shows additional device information.
	/// </summary>
	[Serializable]
    public class DeviceInfoCounterData : BaseCounterData
    {
        [HideInInspector]
        public string lastText = "";

        [SerializeField]
        private bool cpuModel = true;

        [SerializeField]
        private bool gpuModel = true;

        [SerializeField]
        private bool ramSize = true;

        [SerializeField]
        private bool screenData = true;

        private bool inited;

        public DeviceInfoCounterData() {
            //color = new Color32(172, 172, 172, 255);
            //anchor = LabelAnchor.LowerLeft;
        }

        #region properties
        /// <summary>
        /// Shows CPU model name and maximum supported threads count.
        /// </summary>
        public bool CpuModel {
            get { return cpuModel; }
            set {
                if (cpuModel == value || !Application.isPlaying) return;
                cpuModel = value;
                if (!enabled) return;

                Refresh();
            }
        }

        /// <summary>
        /// Shows GPU model name, supported shader model (if possible) and total Video RAM size (if possible).
        /// </summary>
        public bool GpuModel {
            get { return gpuModel; }
            set {
                if (gpuModel == value || !Application.isPlaying) return;
                gpuModel = value;
                if (!enabled) return;

                Refresh();
            }
        }

        /// <summary>
        /// Shows total RAM size.
        /// </summary>
        public bool RamSize {
            get { return ramSize; }
            set {
                if (ramSize == value || !Application.isPlaying) return;
                ramSize = value;
                if (!enabled) return;

                Refresh();
            }
        }

        /// <summary>
        /// Shows screen resolution, size and DPI (if possible).
        /// </summary>
        public bool ScreenData {
            get { return screenData; }
            set {
                if (screenData == value || !Application.isPlaying) return;
                screenData = value;
                if (!enabled) return;

                Refresh();
            }
        }
        #endregion

        //protected override void CacheCurrentColor() {
        //    colorCached = "<color=#" + AFPSCounter.Color32ToHex(color) + ">";
        //}

        public override void Activate() {
            if (!enabled || inited || !HasData()) return;
            base.Activate();
            inited = true; 
            UpdateValue();
        }

        internal override void Deactivate() {
            if (!inited) return;
            base.Deactivate();

            if (text != null) text.Length = 0;
            //main.MakeDrawableLabelDirty(anchor);

            inited = false;
        }

        internal override void UpdateValue(bool force) {
            if (!inited && (HasData())) {
                Activate();
                return;
            }

            if (inited && (!HasData())) {
                Deactivate();
                return;
            }

            if (!enabled) return;

            bool needNewLine = false;

            text.Remove(0, text.Length);

            if (cpuModel) {
                text.Append("CPU: ").Append(SystemInfo.processorType).Append(" (").Append(SystemInfo.processorCount).Append(" threads)");
                needNewLine = true;
            }

            if (gpuModel) {
                if (needNewLine) text.Append(Environment.NewLine);
                text.Append("GPU: ").Append(SystemInfo.graphicsDeviceName);

                bool showSm = false;
                int sm = SystemInfo.graphicsShaderLevel;
                if (sm == 20) {
                    text.Append(" (SM: 2.0");
                    showSm = true;
                }
                else if (sm == 30) {
                    text.Append(" (SM: 3.0");
                    showSm = true;
                }
                else if (sm == 40) {
                    text.Append(" (SM: 4.0");
                    showSm = true;
                }
                else if (sm == 41) {
                    text.Append(" (SM: 4.1");
                    showSm = true;
                }
                else if (sm == 50) {
                    text.Append(" (SM: 5.0");
                    showSm = true;
                }

                int vram = SystemInfo.graphicsMemorySize;
                if (vram > 0) {
                    if (showSm) {
                        text.Append(", VRAM: ").Append(vram).Append(" MB)");
                    }
                    else {
                        text.Append("(VRAM: ").Append(vram).Append(" MB)");
                    }
                }
                else if (showSm) {
                    text.Append(")");
                }
                needNewLine = true;
            }

            if (ramSize) {
                if (needNewLine) text.Append(Environment.NewLine);

                int ram = SystemInfo.systemMemorySize;

                if (ram > 0) {
                    text.Append("RAM: ").Append(ram).Append(" MB");
                    needNewLine = true;
                }
            }

            if (screenData) {
                if (needNewLine) text.Append(Environment.NewLine);
                Resolution res = Screen.currentResolution;

                text.Append("Screen: ").Append(res.width).Append("x").Append(res.height).Append("@").Append(res.refreshRate).Append("Hz (window size: ").Append(Screen.width).Append("x").Append(Screen.height);
                float dpi = Screen.dpi;
                if (dpi <= 0) {
                    text.Append(")");
                }
                else {
                    text.Append(", DPI: ").Append(dpi).Append(")");
                }
            }

            lastText = text.ToString();

            //if (main.OperationMode == AFPSCounterOperationMode.Normal) {
            //    text.Insert(0, colorCached);
            //    text.Append("</color>");
            //}
            //else {
            //    text.Length = 0;
            //}

            dirty = true;
        }

        private bool HasData() {
            return cpuModel || gpuModel || ramSize || screenData;
        }
    }
    
    /// <summary>
    /// Shows memory usage data.
    /// </summary>
    [Serializable]
    public class MemoryCounterData : BaseCounterData
    {
        public const int MEMORY_DIVIDER = 1048576; // 1024^2

        //private const string COROUTINE_NAME = "UpdateMemoryCounter";
        private IEnumerator UpdateMemoryCounter() {
            MemoryCounterData memoryCounter = this;
            while (true) {
                memoryCounter.UpdateValue();
                //UpdateTexts();
                yield return new WaitForSeconds(memoryCounter.UpdateInterval);
            }
        }
        //private const string TEXT_START = "<color=#{0}><b>";
        private const string LINE_START_TOTAL = "MEM (total): ";
        private const string LINE_START_ALLOCATED = "MEM (alloc): ";
        private const string LINE_START_MONO = "MEM (mono): ";
        private const string LINE_END = " MB";
        //private const string TEXT_END = "</b></color>";
        public string lastText = "";
        /// <summary>
        /// Last total memory readout.
        /// </summary>
        /// In megs if #PreciseValues is false, in bytes otherwise.
        /// @see TotalReserved
        [HideInInspector]
        public long lastTotalValue = 0;

        /// <summary>
        /// Last allocated memory readout.
        /// </summary>
        /// In megs if #PreciseValues is false, in bytes otherwise.
        /// @see Allocated
        [HideInInspector]
        public long lastAllocatedValue = 0;

        /// <summary>
        /// Last Mono memory readout.
        /// </summary>
        /// In megs if #PreciseValues is false, in bytes otherwise.
        /// @see MonoUsage
        [HideInInspector]
        public long lastMonoValue = 0;

        [SerializeField]
        [Range(0.1f, 10f)]
        private float updateInterval = 1f;

        [SerializeField]
        private bool preciseValues;

        [SerializeField]
        private bool totalReserved = true;

        [SerializeField]
        private bool allocated = true;

        [SerializeField]
        private bool monoUsage = true;

        private bool inited;

        public MemoryCounterData() {
            //color = new Color32(234, 238, 101, 255);
        }

        /// <summary>
        /// Counter's value update interval.
        /// </summary>
        public float UpdateInterval {
            get { return updateInterval; }
            set {
                if (Math.Abs(updateInterval - value) < 0.001f || !Application.isPlaying) return;
                updateInterval = value;
                if (!enabled) return;

                RestartCoroutine();
            }
        }

        /// <summary>
        /// Allows to output memory usage more precisely thus using more system resources.
        /// </summary>
        public bool PreciseValues {
            get { return preciseValues; }
            set {
                if (preciseValues == value || !Application.isPlaying) return;
                preciseValues = value;
                if (!enabled) return;

                Refresh();
            }
        }

        /// <summary>
        /// Allows to see private memory amount reserved for application. This memory can’t be used by other applications.
        /// </summary>
        public bool TotalReserved {
            get { return totalReserved; }
            set {
                if (totalReserved == value || !Application.isPlaying) return;
                totalReserved = value;
                if (!totalReserved) lastTotalValue = 0;
                if (!enabled) return;

                Refresh();
            }
        }

        /// <summary>
        /// Allows to see amount of memory, currently allocated by application.
        /// </summary>
        public bool Allocated {
            get { return allocated; }
            set {
                if (allocated == value || !Application.isPlaying) return;
                allocated = value;
                if (!allocated) lastAllocatedValue = 0;
                if (!enabled) return;

                Refresh();
            }
        }

        /// <summary>
        /// Allows to see amount of memory, allocated by managed Mono objects, 
        /// such as UnityEngine.Object and everything derived from it for example.
        /// </summary>
        public bool MonoUsage {
            get { return monoUsage; }
            set {
                if (monoUsage == value || !Application.isPlaying) return;
                monoUsage = value;
                if (!monoUsage) lastMonoValue = 0;
                if (!enabled) return;

                Refresh();
            }
        }

        //protected override void CacheCurrentColor() {
        //    colorCached = String.Format(TEXT_START, AFPSCounter.Color32ToHex(color));
        //}

        public override void Activate() {
            if (!enabled || inited || !HasData()) return;
            base.Activate();
            inited = true;

            lastTotalValue = 0;
            lastAllocatedValue = 0;
            lastMonoValue = 0;

            //if (main.OperationMode == AFPSCounterOperationMode.Normal) {
            //if (colorCached == null) {
            //    colorCached = String.Format(TEXT_START, AFPSCounter.Color32ToHex(color));
            //}

            //if (text == null) {
            //    text = new StringBuilder(200);
            //}
            //else {
            //    text.Length = 0;
            //}

            //text.Append(colorCached);

                if (totalReserved) {
                    if (preciseValues) {
                        text.Append(LINE_START_TOTAL).AppendFormat("{0:F}", 0).Append(LINE_END);
                    }
                    else {
                        text.Append(LINE_START_TOTAL).Append(0).Append(LINE_END);
                    }
                }

                if (allocated) {
                    if (text.Length > 0) text.Append(Environment.NewLine);
                    if (preciseValues) {
                        text.Append(LINE_START_ALLOCATED).AppendFormat("{0:F}", 0).Append(LINE_END);
                    }
                    else {
                        text.Append(LINE_START_ALLOCATED).Append(0).Append(LINE_END);
                    }
                }

                if (monoUsage) {
                    if (text.Length > 0) text.Append(Environment.NewLine);
                    if (preciseValues) {
                        text.Append(LINE_START_MONO).AppendFormat("{0:F}", 0).Append(LINE_END);
                    }
                    else {
                        text.Append(LINE_START_MONO).Append(0).Append(LINE_END);
                    }
                }

                //text.Append(TEXT_END);
                dirty = true;
            //}

            main.StartCoroutine(UpdateMemoryCounter());
        }

        internal override void Deactivate() {
            if (!inited) return;
            base.Deactivate();

            if (text != null) text.Length = 0;

            main.StopCoroutine(UpdateMemoryCounter());
            //main.MakeDrawableLabelDirty(anchor);

            inited = false;
        }

        internal override void UpdateValue(bool force) {
            if (!enabled) return;

            if (force) {
                if (!inited && (HasData())) {
                    Activate();
                    return;
                }

                if (inited && (!HasData())) {
                    Deactivate();
                    return;
                }
            }

            if (totalReserved) {
                long value = Profiler.GetTotalReservedMemoryLong();
                uint divisionResult = 0;

                bool newValue;
                if (preciseValues) {
                    newValue = (lastTotalValue != value);
                }
                else {
                    divisionResult = (uint)(value / MEMORY_DIVIDER);
                    newValue = (lastTotalValue != divisionResult);
                }

                if (newValue || force) {
                    if (preciseValues) {
                        lastTotalValue = value;
                    }
                    else {
                        lastTotalValue = divisionResult;
                    }

                    dirty = true;
                }
            }

            if (allocated) {
                long value = Profiler.GetTotalAllocatedMemoryLong();
                uint divisionResult = 0;

                bool newValue;
                if (preciseValues) {
                    newValue = (lastAllocatedValue != value);
                }
                else {
                    divisionResult = (uint)(value / MEMORY_DIVIDER);
                    newValue = (lastAllocatedValue != divisionResult);
                }

                if (newValue || force) {
                    if (preciseValues) {
                        lastAllocatedValue = value;
                    }
                    else {
                        lastAllocatedValue = divisionResult;
                    }

                    dirty = true;
                }
            }

            if (monoUsage) {
                long monoMemory = GC.GetTotalMemory(false);
                long monoDivisionResult = 0;

                bool newValue;
                if (preciseValues) {
                    newValue = (lastMonoValue != monoMemory);
                }
                else {
                    monoDivisionResult = monoMemory / MEMORY_DIVIDER;
                    newValue = (lastMonoValue != monoDivisionResult);
                }

                if (newValue || force) {
                    if (preciseValues) {
                        lastMonoValue = monoMemory;
                    }
                    else {
                        lastMonoValue = monoDivisionResult;
                    }

                    dirty = true;
                }
            }
            if (dirty ) {
                bool needNewLine = false;

                text.Length = 0;
                //text.Append(colorCached);

                if (totalReserved) {
                    text.Append(LINE_START_TOTAL);

                    if (preciseValues) {
                        text.AppendFormat("{0:F}", lastTotalValue / (float)MEMORY_DIVIDER);
                    }
                    else {
                        text.Append(lastTotalValue);
                    }
                    text.Append(LINE_END);
                    needNewLine = true;
                }

                if (allocated) {
                    if (needNewLine) text.Append(Environment.NewLine);
                    text.Append(LINE_START_ALLOCATED);

                    if (preciseValues) {
                        text.AppendFormat("{0:F}", lastAllocatedValue / (float)MEMORY_DIVIDER);
                    }
                    else {
                        text.Append(lastAllocatedValue);
                    }
                    text.Append(LINE_END);
                    needNewLine = true;
                }

                if (monoUsage) {
                    if (needNewLine) text.Append(Environment.NewLine);
                    text.Append(LINE_START_MONO);

                    if (preciseValues) {
                        text.AppendFormat("{0:F}", lastMonoValue / (float)MEMORY_DIVIDER);
                    }
                    else {
                        text.Append(lastMonoValue);
                    }

                    text.Append(LINE_END);
                }

                //text.Append(TEXT_END);
                lastText = text.ToString();
            }
        }

        private void RestartCoroutine() {
            main.StopCoroutine(UpdateMemoryCounter());
            main.StartCoroutine(UpdateMemoryCounter());
        }

        private bool HasData() {
            return totalReserved || allocated || monoUsage;
        }
    }
}
