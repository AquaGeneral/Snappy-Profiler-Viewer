/*
* This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not
* distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System;
using System.Collections.Generic;
using UnityEngine;

public class OptimizedProfilerWindow : EditorWindow {
    private static readonly GUIContent[] headers = new GUIContent[] {
        new GUIContent("Function Name"),
        new GUIContent("Total %"),
        new GUIContent("Self %"),
        new GUIContent("Calls"),
        new GUIContent("GC Alloc"),
        new GUIContent("Total Time"),
        new GUIContent("Self Time")
    };
    // The fixed width for the columns such as "Calls" and "Self Time", which are numerical
    private const float numericalDataColumnWidth = 68f;

    private List<ProfilerRowInfo> profilerProperties = new List<ProfilerRowInfo>();
    private ProfilerProperty profilerProperty;
    private int frameIndex = 0;
    
    private Vector2 scrollPosition;

    private static GUIStyle columnHeadersStyle, evenRowStyle, oddRowStyle;

    [MenuItem("Jesse Stiller/Optimized Profiler...")]
    private static void CreateAndShow() {
        GetWindow<OptimizedProfilerWindow>(false, "Optimized Profiler", true);
    }

    private void OnGUI() {
        if(columnHeadersStyle == null) columnHeadersStyle = new GUIStyle("OL title");
        if(evenRowStyle == null) evenRowStyle = new GUIStyle("OL EntryBackEven");
        if(oddRowStyle == null) oddRowStyle = new GUIStyle("OL EntryBackOdd");

        if(ProfilerDriver.firstFrameIndex == -1) {
            EditorGUILayout.HelpBox("Begin profiling to have data to view.", MessageType.Warning);
            return;
        }

        if(profilerProperty == null) {
            frameIndex = ProfilerDriver.firstFrameIndex;
            UpdateProperties();
        }

        EditorGUI.BeginChangeCheck();
        frameIndex = EditorGUILayout.IntSlider(frameIndex, ProfilerDriver.firstFrameIndex, ProfilerDriver.lastFrameIndex);
        if(EditorGUI.EndChangeCheck()) {
            UpdateProperties();
        }

        if(profilerProperty.frameDataReady == false) return;

        EditorGUILayout.LabelField("Properties", profilerProperties.Count.ToString("N0"));
        
        float height = GUI.skin.label.CalcHeight(new GUIContent("Rubbish"), 200f);
        // Draw the column headers
        Rect columnHeadersRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
        float functionNameWidth = columnHeadersRect.width - numericalDataColumnWidth * 6f;

        float columnHeadersLeft = columnHeadersRect.x;

        GUI.Box(new Rect(columnHeadersLeft, columnHeadersRect.y, functionNameWidth, columnHeadersRect.height), "Function Name", columnHeadersStyle);
        columnHeadersLeft += functionNameWidth;

        GUI.Box(new Rect(columnHeadersLeft, columnHeadersRect.y, numericalDataColumnWidth, columnHeadersRect.height), "Total %", columnHeadersStyle);
        columnHeadersLeft += numericalDataColumnWidth;

        GUI.Box(new Rect(columnHeadersLeft, columnHeadersRect.y, numericalDataColumnWidth, columnHeadersRect.height), "Self %", columnHeadersStyle);
        columnHeadersLeft += numericalDataColumnWidth;

        GUI.Box(new Rect(columnHeadersLeft, columnHeadersRect.y, numericalDataColumnWidth, columnHeadersRect.height), "Calls", columnHeadersStyle);
        columnHeadersLeft += numericalDataColumnWidth;

        GUI.Box(new Rect(columnHeadersLeft, columnHeadersRect.y, numericalDataColumnWidth, columnHeadersRect.height), "GC Memory", columnHeadersStyle);
        columnHeadersLeft += numericalDataColumnWidth;

        GUI.Box(new Rect(columnHeadersLeft, columnHeadersRect.y, numericalDataColumnWidth, columnHeadersRect.height), "Total Time", columnHeadersStyle);
        columnHeadersLeft += numericalDataColumnWidth;

        GUI.Box(new Rect(columnHeadersLeft, columnHeadersRect.y, numericalDataColumnWidth, columnHeadersRect.height), "Self Time", columnHeadersStyle);
        columnHeadersLeft += numericalDataColumnWidth;

        Rect scrollViewRect = EditorGUILayout.GetControlRect(new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) });
        Rect viewRect = new Rect(scrollViewRect.x, scrollViewRect.y, scrollViewRect.width, height * profilerProperties.Count);
        scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, viewRect);

        int firstVisibleProperty = Mathf.CeilToInt(scrollPosition.y / height);
        int lastVisibleProperty = Mathf.Min(firstVisibleProperty + Mathf.CeilToInt(scrollViewRect.height / height + 1), profilerProperties.Count);
        
        if(Event.current.type == EventType.Repaint) {
            for(int i = firstVisibleProperty; i < lastVisibleProperty; i++) {
                // Background
                GUIStyle backgroundStyle = (i % 2 == 0 ? evenRowStyle : oddRowStyle);
                backgroundStyle.Draw(new Rect(0f, i * height + 24f, scrollViewRect.width, height), GUIContent.none, 0);

                // Funtion Name
                EditorGUI.LabelField(new Rect(profilerProperties[i].depth * 10f, i * height + 24f, scrollViewRect.width, height), profilerProperties[i].functionName);
            }
        }
        GUI.EndScrollView();
    }

    private void UpdateProperties() {
        profilerProperty = new ProfilerProperty();
        profilerProperty.SetRoot(frameIndex, ProfilerColumn.SelfTime, ProfilerViewType.Hierarchy);
        profilerProperty.onlyShowGPUSamples = false;

        profilerProperties.Clear();

        while(profilerProperty.Next(true)) {
            profilerProperties.Add(new ProfilerRowInfo(profilerProperty));
        }
    }

    private struct ProfilerRowInfo {
        internal int depth;
        internal string propertyPath, functionName, totalPercent, selfPercent, calls, gcMemory, totalTime, selfTime;

        public ProfilerRowInfo(ProfilerProperty profilerProperty) {
            propertyPath = profilerProperty.propertyPath;
            depth = profilerProperty.depth;
            functionName = profilerProperty.GetColumn(ProfilerColumn.FunctionName);
            totalPercent = profilerProperty.GetColumn(ProfilerColumn.TotalPercent);
            selfPercent = profilerProperty.GetColumn(ProfilerColumn.SelfPercent);
            calls = profilerProperty.GetColumn(ProfilerColumn.Calls);
            gcMemory = profilerProperty.GetColumn(ProfilerColumn.GCMemory);
            totalTime = profilerProperty.GetColumn(ProfilerColumn.TotalTime);
            selfTime = profilerProperty.GetColumn(ProfilerColumn.SelfTime);
            
            //WarningCount = 12,
            //ObjectName = 13
        }
    }
}
