/*
* This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not
* distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class SnappyProfilerViewer : EditorWindow {
    // The fixed width for the columns such as "Calls" and "Self Time", which are numerical
    private const float numericalDataColumnWidth = 75f;

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
    
    private float cellHeight;
    private Rect columnHeadersRect;
    private float headerLeftOffset = 0f;
    private float cellTopOffset;
    private float cellLeftOffset;
    private float functionNameCellWidth;

    private Texture2D frameIntensityTexture;

    private static readonly Color peakIntensityColour = new Color(1f, 0.85f, 0f, 1f);

    [MenuItem("Jesse Stiller/Snappy Profiler Viewer...")]
    private static void CreateAndShow() {
        GetWindow<SnappyProfilerViewer>(false, "Snappy", true);
    }

    private void OnFocus() {
        int numberOfFrames = ProfilerDriver.lastFrameIndex - ProfilerDriver.firstFrameIndex;
        frameIntensityTexture = new Texture2D(Screen.width, 1);
        
        float frameTimeTotal = 0f;
        float[] frameIntensities = new float[numberOfFrames];

        for(int f = 0; f < numberOfFrames; f++) {
            ProfilerProperty property = new ProfilerProperty();
            property.SetRoot(f + ProfilerDriver.firstFrameIndex, ProfilerColumn.DontSort, ProfilerViewType.RawHierarchy);

            frameIntensities[f] = float.Parse(property.frameTime);
            frameTimeTotal += frameIntensities[f];

            //int numberOfProperties = 0;
            //while(property.Next(true)) {
            //    numberOfProperties++;
            //}
            //frameIntensities[f] = numberOfProperties;
        }

        //for(int f = 0; f < numberOfFrames; f++) {
        //    float coefficient = (float)System.Math.Log(frameIntensities[f], frameTimeTotal);
        //    //float coefficient = (frameIntensities[f] / frameTimeTotal) * 10f;
        //    frameIntensityTexture.SetPixel(f, 0, new Color(coefficient, coefficient, coefficient, 1f));
        //}

        float frameSize = 1f / (numberOfFrames - 1);

        for(int i = 0; i < Screen.width; i++) {
            float coefficient = (float)i / Screen.width;
            int closestFrame = Mathf.RoundToInt(coefficient * (numberOfFrames - 1));
            int secondClosestFrame;
            if(System.Math.Truncate(coefficient) >= 0.5f) {
                secondClosestFrame = closestFrame + 1;
                if(secondClosestFrame > numberOfFrames) secondClosestFrame -= 2;
            } else {
                secondClosestFrame = closestFrame - 1;
                if(secondClosestFrame < 0) secondClosestFrame += 2;
            }

            float closestFramePosition = (float)closestFrame / numberOfFrames;
            float secondClosestFramePosition = (float)secondClosestFrame / numberOfFrames;

            float t = (closestFramePosition - coefficient) / frameSize;

            float closestIntensity = frameIntensities[closestFrame];
            float secondClosestIntensity = frameIntensities[secondClosestFrame];

            frameIntensityTexture.SetPixel(i, 0, 
                Color.Lerp(
                    Color.Lerp(Color.black, peakIntensityColour, (float)System.Math.Log(frameIntensities[secondClosestFrame] * 2f, frameTimeTotal)),
                    Color.Lerp(Color.black, peakIntensityColour, (float)System.Math.Log(frameIntensities[closestFrame] * 2f, frameTimeTotal))
                    //Color.Lerp(Color.black, peakIntensityColour, (float)System.Math.Log(frameIntensities[secondClosestFrame], frameTimeTotal)), 
                    //Color.Lerp(Color.black, peakIntensityColour, (float)System.Math.Log(frameIntensities[closestFrame], frameTimeTotal))
                , t));
        }

        frameIntensityTexture.Apply();
    }
    
    private void OnGUI() {
        Rect frameIntensityRect = EditorGUILayout.GetControlRect(GUILayout.Height(40f));
        EditorGUI.DrawPreviewTexture(frameIntensityRect, frameIntensityTexture);

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
        
        cellHeight = GUI.skin.label.CalcHeight(new GUIContent("Rubbish"), 200f);

        /**
        * Draw the column headers
        */
        headerLeftOffset = 0f;
        columnHeadersRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
        columnHeadersRect.width -= 15f; // Account for the width of the vertical scrollbar
        float functionNameHeaderWidth = columnHeadersRect.width - numericalDataColumnWidth * 6f;
        float columnHeadersLeft = columnHeadersRect.x;
        
        DrawColumnHeader("Function Name", ProfilerColumn.FunctionName, functionNameHeaderWidth, columnHeadersStyleLeft);
        DrawColumnHeader("Total %", ProfilerColumn.TotalPercent, numericalDataColumnWidth, columnHeadersStyleCentered);
        DrawColumnHeader("Self %", ProfilerColumn.SelfPercent, numericalDataColumnWidth, columnHeadersStyleCentered);
        DrawColumnHeader("Calls %", ProfilerColumn.Calls, numericalDataColumnWidth, columnHeadersStyleCentered);
        DrawColumnHeader("GC Alloc", ProfilerColumn.GCMemory, numericalDataColumnWidth, columnHeadersStyleCentered);
        DrawColumnHeader("Total Time", ProfilerColumn.TotalTime, numericalDataColumnWidth, columnHeadersStyleCentered);
        DrawColumnHeader("Self Time", ProfilerColumn.SelfTime, numericalDataColumnWidth, columnHeadersStyleCentered);
        
        Rect scrollViewRect = EditorGUILayout.GetControlRect(new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) });
        Rect viewRect = new Rect(scrollViewRect.x, scrollViewRect.y, scrollViewRect.width, cellHeight * cachedProfilerProperties.Count);
        scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, viewRect);

        int firstVisibleProperty = Mathf.Max(Mathf.CeilToInt(scrollPosition.y / cellHeight) - 1, 0);
        int lastVisibleProperty = Mathf.Min(firstVisibleProperty + Mathf.CeilToInt(scrollViewRect.height / cellHeight + 1), cachedProfilerProperties.Count);
        
        if(Event.current.type == EventType.Repaint) {
            for(int i = firstVisibleProperty; i < lastVisibleProperty; i++) {
                cellTopOffset = i * cellHeight + columnHeadersRect.y + cellHeight + 2f;

                // Background
                GUIStyle backgroundStyle = (i % 2 == 0 ? evenRowStyle : oddRowStyle);
                backgroundStyle.Draw(new Rect(0f, cellTopOffset, scrollViewRect.width, cellHeight), GUIContent.none, 0);

                cellLeftOffset = (cachedProfilerProperties[i].depth - 1) * 15f + 4f;
                functionNameCellWidth = functionNameHeaderWidth - (cachedProfilerProperties[i].depth - 1) * 15f;

                DrawDataCell(cachedProfilerProperties[i].functionName, functionNameCellWidth, GUI.skin.label);
                DrawDataCell(cachedProfilerProperties[i].totalPercent, numericalDataColumnWidth, rightAlignedLabel);
                DrawDataCell(cachedProfilerProperties[i].selfPercent, numericalDataColumnWidth, rightAlignedLabel);
                DrawDataCell(cachedProfilerProperties[i].calls, numericalDataColumnWidth, rightAlignedLabel);
                DrawDataCell(cachedProfilerProperties[i].gcMemory, numericalDataColumnWidth, rightAlignedLabel);
                DrawDataCell(cachedProfilerProperties[i].totalTime, numericalDataColumnWidth, rightAlignedLabel);
                DrawDataCell(cachedProfilerProperties[i].selfTime, numericalDataColumnWidth, rightAlignedLabel);
            }
        }
        GUI.EndScrollView();
    }

    private void DrawDataCell(string data, float width, GUIStyle style) {
        EditorGUI.LabelField(new Rect(cellLeftOffset, cellTopOffset, width, cellHeight), data, style);
        cellLeftOffset += width;
    }

    private void DrawColumnHeader(string label, ProfilerColumn column, float width, GUIStyle style) {
        bool toggleResult = GUI.Toggle(new Rect(headerLeftOffset, columnHeadersRect.y, width, columnHeadersRect.height),
            columnToSort == column, label, style);

        if(toggleResult == true) ColumnToSort = column;

        headerLeftOffset += width;
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
