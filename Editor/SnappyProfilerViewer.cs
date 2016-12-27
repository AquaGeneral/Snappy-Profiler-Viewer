/*
* This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not
* distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class SnappyProfilerViewer : EditorWindow {
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

    private List<ProfilerRowInfo> cachedProfilerProperties = new List<ProfilerRowInfo>();
    private ProfilerProperty profilerProperty;
    private int frameIndex = 0;
    
    private Vector2 scrollPosition;

    private static GUIStyle rightAlignedLabel, columnHeadersStyleLeft, columnHeadersStyleCentered, evenRowStyle, oddRowStyle;

    private ProfilerColumn columnToSort = ProfilerColumn.TotalTime;
    private ProfilerColumn ColumnToSort {
        set {
            if(value == columnToSort) return;
            columnToSort = value;
            ColumnToSortChanged();
        }
    }
    
    private Rect columnHeadersRect;
    private float leftOffset = 0f;

    [MenuItem("Jesse Stiller/Snappy Profiler Viewer...")]
    private static void CreateAndShow() {
        GetWindow<SnappyProfilerViewer>(false, "Snappy", true);
    }

    private void OnGUI() {
        if(rightAlignedLabel == null) {
            rightAlignedLabel = new GUIStyle(GUI.skin.label);
            rightAlignedLabel.alignment = TextAnchor.MiddleRight;
            rightAlignedLabel.padding = new RectOffset(0, 3, 0, 0);
        }
        if(columnHeadersStyleLeft == null) {
            columnHeadersStyleLeft = new GUIStyle("OL title");
            columnHeadersStyleLeft.alignment = TextAnchor.MiddleLeft;
        }
        if(columnHeadersStyleCentered == null) {
            columnHeadersStyleCentered = new GUIStyle("OL title");
            columnHeadersStyleCentered.alignment = TextAnchor.MiddleCenter;
        }
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
        frameIndex = EditorGUILayout.IntSlider("Frame", frameIndex, ProfilerDriver.firstFrameIndex, ProfilerDriver.lastFrameIndex);
        if(EditorGUI.EndChangeCheck()) {
            UpdateProperties();
        }

        if(profilerProperty.frameDataReady == false) return;

        EditorGUILayout.LabelField("Properties", cachedProfilerProperties.Count.ToString("N0"));
        
        float height = GUI.skin.label.CalcHeight(new GUIContent("Rubbish"), 200f);

        /**
        * Draw the column headers
        */
        leftOffset = 0f;
        columnHeadersRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
        columnHeadersRect.width -= 15f; // Account for the width of the vertical scrollbar
        float functionNameHeaderWidth = columnHeadersRect.width - numericalDataColumnWidth * 6f;
        float columnHeadersLeft = columnHeadersRect.x;
        
        DrawColumnHeader("Function Name", ProfilerColumn.FunctionName, functionNameHeaderWidth);
        DrawColumnHeader("Total %", ProfilerColumn.TotalPercent, numericalDataColumnWidth);
        DrawColumnHeader("Self %", ProfilerColumn.SelfPercent, numericalDataColumnWidth);
        DrawColumnHeader("Calls %", ProfilerColumn.Calls, numericalDataColumnWidth);
        DrawColumnHeader("GC Memory %", ProfilerColumn.GCMemory, numericalDataColumnWidth);
        DrawColumnHeader("Total Time", ProfilerColumn.TotalTime, numericalDataColumnWidth);
        DrawColumnHeader("Self Time", ProfilerColumn.SelfTime, numericalDataColumnWidth);
        
        Rect scrollViewRect = EditorGUILayout.GetControlRect(new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) });
        Rect viewRect = new Rect(scrollViewRect.x, scrollViewRect.y, scrollViewRect.width, height * cachedProfilerProperties.Count);
        scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, viewRect);

        int firstVisibleProperty = Mathf.Max(Mathf.CeilToInt(scrollPosition.y / height) - 1, 0);
        int lastVisibleProperty = Mathf.Min(firstVisibleProperty + Mathf.CeilToInt(scrollViewRect.height / height + 1), cachedProfilerProperties.Count);
        
        if(Event.current.type == EventType.Repaint) {
            for(int i = firstVisibleProperty; i < lastVisibleProperty; i++) {
                float cellTopOffset = i * height + 56f;

                // Background
                GUIStyle backgroundStyle = (i % 2 == 0 ? evenRowStyle : oddRowStyle);
                backgroundStyle.Draw(new Rect(0f, cellTopOffset, scrollViewRect.width, height), GUIContent.none, 0);

                float cellLeftOffset = (cachedProfilerProperties[i].depth - 1) * 15f + 4f;
                float functionNameCellWidth = functionNameHeaderWidth - (cachedProfilerProperties[i].depth - 1) * 15f;
                
                // Funtion Name
                EditorGUI.LabelField(new Rect(cellLeftOffset, cellTopOffset, functionNameCellWidth, height), cachedProfilerProperties[i].functionName);

                cellLeftOffset += functionNameCellWidth;
                EditorGUI.LabelField(new Rect(cellLeftOffset, cellTopOffset, numericalDataColumnWidth, height), cachedProfilerProperties[i].totalPercent, rightAlignedLabel);

                cellLeftOffset += numericalDataColumnWidth;
                EditorGUI.LabelField(new Rect(cellLeftOffset, cellTopOffset, numericalDataColumnWidth, height), cachedProfilerProperties[i].selfPercent, rightAlignedLabel);

                cellLeftOffset += numericalDataColumnWidth;
                EditorGUI.LabelField(new Rect(cellLeftOffset, cellTopOffset, numericalDataColumnWidth, height), cachedProfilerProperties[i].calls, rightAlignedLabel);

                cellLeftOffset += numericalDataColumnWidth;
                EditorGUI.LabelField(new Rect(cellLeftOffset, cellTopOffset, numericalDataColumnWidth, height), cachedProfilerProperties[i].gcMemory, rightAlignedLabel);

                cellLeftOffset += numericalDataColumnWidth;
                EditorGUI.LabelField(new Rect(cellLeftOffset, cellTopOffset, numericalDataColumnWidth, height), cachedProfilerProperties[i].totalTime, rightAlignedLabel);

                cellLeftOffset += numericalDataColumnWidth;
                EditorGUI.LabelField(new Rect(cellLeftOffset, cellTopOffset, numericalDataColumnWidth, height), cachedProfilerProperties[i].selfTime, rightAlignedLabel);
            }
        }
        GUI.EndScrollView();
    }
    
    private void DrawColumnHeader(string label, ProfilerColumn column, float width) {
        bool toggleResult = GUI.Toggle(new Rect(leftOffset, columnHeadersRect.y, width, columnHeadersRect.height),
            columnToSort == column, label, columnHeadersStyleLeft);

        if(toggleResult == true) ColumnToSort = column;

        leftOffset += width;
    }

    private void ColumnToSortChanged() {
        UpdateProperties();
    }

    private void UpdateProperties() {
        profilerProperty = new ProfilerProperty();
        profilerProperty.SetRoot(frameIndex, columnToSort, ProfilerViewType.Hierarchy);
        profilerProperty.onlyShowGPUSamples = false;

        cachedProfilerProperties.Clear();

        while(profilerProperty.Next(true)) {
            cachedProfilerProperties.Add(new ProfilerRowInfo(profilerProperty));
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
        }
    }
}
