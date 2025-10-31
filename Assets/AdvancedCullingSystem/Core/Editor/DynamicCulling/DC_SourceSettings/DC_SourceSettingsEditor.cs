using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace NGS.AdvancedCullingSystem.Dynamic
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DC_SourceSettings))]
    public class DC_SourceSettingsEditor : Editor
    {
        private new DC_SourceSettings target
        {
            get
            {
                return base.target as DC_SourceSettings;
            }
        }

        private SerializedProperty _controllerIdProp;
        private SerializedProperty _sourceTypeProp;
        private SerializedProperty _isIncompatibleProp;
        private SerializedProperty _incompatibilityReasonProp;
        private SourceSettingsStrategyEditor _strategyEditor;


        private void OnEnable()
        {
            _controllerIdProp = serializedObject.FindAutoProperty(nameof(target.ControllerID));
            _sourceTypeProp = serializedObject.FindProperty("_sourceType");
            _isIncompatibleProp = serializedObject.FindAutoProperty(nameof(target.IsIncompatible));
            _incompatibilityReasonProp = serializedObject.FindAutoProperty(nameof(target.IncompatibilityReason));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _strategyEditor = GetSourceSettingsStrategyEditor();
            _strategyEditor?.SetContext(serializedObject);

            DrawHelpBox();

            if (DrawProperties())
                ApplyModifiedProperties();

            EditorGUILayout.Space();

            if (GUILayout.Button("Check Compatibility"))
                CheckCompatibilities();
        }

        private void OnSceneGUI()
        {
            _strategyEditor?.OnSceneGUI(target);
        }


        private SourceSettingsStrategyEditor GetSourceSettingsStrategyEditor()
        {
            if (!_sourceTypeProp.hasMultipleDifferentValues)
            {
                SourceType sourceType = (SourceType)_sourceTypeProp.enumValueIndex;

                if (sourceType == SourceType.SingleMesh)
                {
                    if (_strategyEditor is RendererSourceSettingsStrategyEditor)
                        return _strategyEditor;
                    
                    return new RendererSourceSettingsStrategyEditor();
                }

                if (sourceType == SourceType.LODGroup)
                {
                    if (_strategyEditor is LODGroupSourceSettingsStrategyEditor)
                        return _strategyEditor;

                    return new LODGroupSourceSettingsStrategyEditor();
                }

                if (sourceType == SourceType.Custom)
                {
                    if (_strategyEditor is CustomSourceSettingsStrategyEditor)
                        return _strategyEditor;

                    return new CustomSourceSettingsStrategyEditor();
                }
            }

            return null;
        }

        private void DrawHelpBox()
        {
            if (_isIncompatibleProp.boolValue && !_isIncompatibleProp.hasMultipleDifferentValues)
            {
                string text = _incompatibilityReasonProp.stringValue;

                EditorGUILayout.HelpBox(text, MessageType.Warning);

                EditorGUILayout.Space();
            }
        }

        private bool DrawProperties()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_controllerIdProp);
            EditorGUILayout.PropertyField(_sourceTypeProp);

            _strategyEditor?.DrawProperties();

            EditorGUI.EndDisabledGroup();

            return EditorGUI.EndChangeCheck();
        }

        private void ApplyModifiedProperties()
        {
            _strategyEditor?.ApplyModifiedProperties(targets);

            foreach (var target in targets)
            {
                DC_SourceSettings current = target as DC_SourceSettings;

                if (!_controllerIdProp.hasMultipleDifferentValues)
                    current.ControllerID = _controllerIdProp.intValue;

                if (!_sourceTypeProp.hasMultipleDifferentValues)
                    current.SourceType = (SourceType) _sourceTypeProp.enumValueIndex;

                EditorUtility.SetDirty(target);
            }
        }

        private void CheckCompatibilities()
        {
            foreach (var target in targets)
            {
                DC_SourceSettings current = target as DC_SourceSettings;

                if (current.CheckCompatibility())
                {
                    Debug.Log(current.name + " is compatible!");
                }
                else
                {
                    Debug.Log(current.name + " is incompatible. Reason : " + current.IncompatibilityReason);
                }
            }
        }


        private abstract class SourceSettingsStrategyEditor
        {
            protected SerializedObject Context { get; private set; }
            private bool _propertiesChanged;


            public void SetContext(SerializedObject context)
            {
                Context = context;
                OnContextSet();
            }

            public void DrawProperties()
            {
                EditorGUI.BeginChangeCheck();

                DrawPropertiesInternal();

                _propertiesChanged = EditorGUI.EndChangeCheck();
            }

            public void ApplyModifiedProperties(Object[] targets)
            {
                if (!_propertiesChanged)
                    return;

                ApplyModifiedPropertiesInternal(targets);

                _propertiesChanged = false;
            }

            public virtual void OnSceneGUI(DC_SourceSettings target)
            {
                
            }


            protected abstract void OnContextSet();

            protected abstract void DrawPropertiesInternal();
            
            protected abstract void ApplyModifiedPropertiesInternal(Object[] targets);   
        }

        private class RendererSourceSettingsStrategyEditor : SourceSettingsStrategyEditor
        {
            private SerializedProperty _cullingMethodProp;
            private SerializedProperty _convexColliderProp;

            protected override void OnContextSet()
            {
                SerializedProperty strategyProp = Context.FindProperty("_strategy");

                _cullingMethodProp = strategyProp.FindPropertyRelative("_cullingMethod");
                _convexColliderProp = strategyProp.FindPropertyRelative("_convexCollider");
            }

            protected override void DrawPropertiesInternal()
            {
                EditorGUILayout.PropertyField(_cullingMethodProp);
                EditorGUILayout.PropertyField(_convexColliderProp);
            }

            protected override void ApplyModifiedPropertiesInternal(Object[] targets)
            {
                if (!_cullingMethodProp.hasMultipleDifferentValues)
                {
                    foreach (var target in targets)
                    {
                        (target as DC_SourceSettings).GetStrategy<DC_RendererSourceSettingsStrategy>()
                            .CullingMethod = (CullingMethod)_cullingMethodProp.enumValueIndex;
                    }
                }

                if (!_convexColliderProp.hasMultipleDifferentValues)
                {
                    foreach (var target in targets)
                    {
                        (target as DC_SourceSettings).GetStrategy<DC_RendererSourceSettingsStrategy>()
                            .ConvexCollider = _convexColliderProp.boolValue;
                    }
                }
            }
        }

        private class LODGroupSourceSettingsStrategyEditor : SourceSettingsStrategyEditor
        {
            private SerializedProperty _cullingMethodProp;

            protected override void OnContextSet()
            {
                _cullingMethodProp = Context
                    .FindProperty("_strategy")
                    .FindPropertyRelative("_cullingMethod");
            }

            protected override void DrawPropertiesInternal()
            {
                EditorGUILayout.PropertyField(_cullingMethodProp);
            }

            protected override void ApplyModifiedPropertiesInternal(Object[] targets)
            {
                if (!_cullingMethodProp.hasMultipleDifferentValues)
                {
                    foreach (var target in targets)
                    {
                        (target as DC_SourceSettings).GetStrategy<DC_LODGroupSourceSettingsStrategy>()
                        .CullingMethod = (CullingMethod)_cullingMethodProp.enumValueIndex;
                    }
                }
            }
        }

        private class CustomSourceSettingsStrategyEditor : SourceSettingsStrategyEditor
        {
            private static BoxBoundsHandle BoundsHandle;

            private SerializedProperty _strategyProp;
            private SerializedProperty _localBoundsProp;
            private SerializedProperty _onVisibleEventProp;
            private SerializedProperty _onInvisibleEventProp;


            public override void OnSceneGUI(DC_SourceSettings target)
            {
                if (BoundsHandle == null)
                    BoundsHandle = new BoxBoundsHandle();

                DC_CustomSourceSettingsStrategy strategy = 
                    target.GetStrategy<DC_CustomSourceSettingsStrategy>();

                Bounds localBounds = strategy.LocalBounds;

                BoundsHandle.center = target.transform.position + localBounds.center;
                BoundsHandle.size = localBounds.size;

                BoundsHandle.DrawHandle();

                localBounds.center = BoundsHandle.center - target.transform.position;
                localBounds.size = BoundsHandle.size;

                strategy.LocalBounds = localBounds;
            }


            protected override void OnContextSet()
            {
                _strategyProp = Context.FindProperty("_strategy");
                _localBoundsProp = _strategyProp.FindPropertyRelative("_localBounds");
                _onVisibleEventProp = _strategyProp.FindPropertyRelative("_onVisible");
                _onInvisibleEventProp = _strategyProp.FindPropertyRelative("_onInvisible");
            }

            protected override void DrawPropertiesInternal()
            {
                EditorGUILayout.Space();

                if (_strategyProp.hasMultipleDifferentValues)
                {     
                    EditorGUILayout.HelpBox("Multiediting is not supported for such SourceType", MessageType.Info, true);
                    return;
                }

                EditorGUILayout.PropertyField(_localBoundsProp);

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(_onVisibleEventProp);

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(_onInvisibleEventProp);
            }

            protected override void ApplyModifiedPropertiesInternal(Object[] targets)
            {
                if (_strategyProp.hasMultipleDifferentValues)
                    return;

                Context.ApplyModifiedProperties();
            }
        }
    }
}
