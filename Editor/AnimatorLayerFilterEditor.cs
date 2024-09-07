using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using nadena.dev.modular_avatar.core;
using UnityEditor.IMGUI.Controls;
using System;

namespace Narazaka.VRChat.AnimatorLayerFilter.Editor
{
    [CustomEditor(typeof(AnimatorLayerFilter))]
    public class AnimatorLayerFilterEditor : UnityEditor.Editor
    {
        SerializedProperty onlyNames;
        SerializedProperty ignoreNames;

        TreeViewState treeViewState;
        LayerTreeView layerTreeView;

        bool changed;
        string[] layerNamesCache = new string[0];

        void OnEnable()
        {
            onlyNames = serializedObject.FindProperty(nameof(AnimatorLayerFilter.onlyNames));
            ignoreNames = serializedObject.FindProperty(nameof(AnimatorLayerFilter.ignoreNames));
            treeViewState = new TreeViewState();
            layerTreeView = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(onlyNames, true);
            EditorGUILayout.PropertyField(ignoreNames, true);

            var animatorLayerFilter = target as AnimatorLayerFilter;
            var mergeAnimators = animatorLayerFilter.GetComponents<ModularAvatarMergeAnimator>();
            if (mergeAnimators.Length > 0)
            {
                var layerNames = mergeAnimators.Select(mergeAnimator => mergeAnimator.animator as AnimatorController).Where(animator => animator != null).SelectMany(animator => animator.layers.Select(layer => layer.name)).ToArray();
                if (layerTreeView == null || !layerNames.SequenceEqual(layerNamesCache) || serializedObject.hasModifiedProperties || changed)
                {
                    changed = false;
                    layerNamesCache = layerNames;
                    layerTreeView = new LayerTreeView(treeViewState, layerNames, animatorLayerFilter.onlyNames, animatorLayerFilter.ignoreNames)
                    {
                        toggleOnly = (layerName) =>
                        {
                            changed = true;
                            for (var i = 0; i < onlyNames.arraySize; ++i)
                            {
                                if (onlyNames.GetArrayElementAtIndex(i).stringValue == layerName)
                                {
                                    onlyNames.DeleteArrayElementAtIndex(i);
                                    return;
                                }
                            }
                            onlyNames.InsertArrayElementAtIndex(onlyNames.arraySize);
                            onlyNames.GetArrayElementAtIndex(onlyNames.arraySize - 1).stringValue = layerName;
                        },
                        toggleIgnore = (layerName) =>
                        {
                            changed = true;
                            for (var i = 0; i < ignoreNames.arraySize; ++i)
                            {
                                if (ignoreNames.GetArrayElementAtIndex(i).stringValue == layerName)
                                {
                                    ignoreNames.DeleteArrayElementAtIndex(i);
                                    return;
                                }
                            }
                            ignoreNames.InsertArrayElementAtIndex(ignoreNames.arraySize);
                            ignoreNames.GetArrayElementAtIndex(ignoreNames.arraySize - 1).stringValue = layerName;
                        },
                    };
                }
                if (layerTreeView != null)
                {
                    var treeViewRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.Height(layerTreeView.totalHeight));
                    layerTreeView.OnGUI(treeViewRect);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        class LayerTreeView : TreeView
        {
            static MultiColumnHeader CreateHeader() => new MultiColumnHeader(new MultiColumnHeaderState(new[]
            {
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Layer"), width = 100f },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Only") },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Ignore") },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Result") },
            }));

            public Action<string> toggleOnly;
            public Action<string> toggleIgnore;
            IList<string> layerNames;
            IEnumerable<string> onlyNames;
            IEnumerable<string> ignoreNames;

            public LayerTreeView(TreeViewState state, IList<string> layerNames, IEnumerable<string> onlyNames, IEnumerable<string> ignoreNames) : base(state, CreateHeader())
            {
                this.layerNames = layerNames;
                this.onlyNames = onlyNames;
                this.ignoreNames = ignoreNames;
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };
                SetupParentsAndChildrenFromDepths(root, layerNames.Select((layerName, index) => new LayerTreeViewItem()
                {
                    id = index,
                    depth = 0,
                    displayName = layerName,
                    only = onlyNames.Contains(layerName),
                    ignore = ignoreNames.Contains(layerName),
                    result = !ignoreNames.Contains(layerName) && (onlyNames.Count() == 0 || onlyNames.Contains(layerName)),
                } as TreeViewItem).ToList());
                return root;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                for (var i = 0; i < args.GetNumVisibleColumns(); i++)
                {
                    CellGUI(args.GetCellRect(i), args.item as LayerTreeViewItem, args.GetColumn(i), ref args);
                }
            }

            void CellGUI(Rect cellRect, LayerTreeViewItem item, int column, ref RowGUIArgs args)
            {
                CenterRectUsingSingleLineHeight(ref cellRect);
                switch (column)
                {
                    case 0:
                        EditorGUI.LabelField(cellRect, item.displayName);
                        break;
                    case 1:
                        EditorGUI.BeginChangeCheck();
                        item.only = EditorGUI.Toggle(cellRect, item.only);
                        if (EditorGUI.EndChangeCheck() && toggleOnly != null) toggleOnly(item.displayName);
                        break;
                    case 2:
                        EditorGUI.BeginChangeCheck();
                        item.ignore = EditorGUI.Toggle(cellRect, item.ignore);
                        if (EditorGUI.EndChangeCheck() && toggleIgnore != null) toggleIgnore(item.displayName);
                        break;
                    case 3:
                        EditorGUI.LabelField(cellRect, item.result ? "O" : "X");
                        break;
                }
            }
        }

        class LayerTreeViewItem : TreeViewItem
        {
            public bool only;
            public bool ignore;
            public bool result;
        }
    }
}
