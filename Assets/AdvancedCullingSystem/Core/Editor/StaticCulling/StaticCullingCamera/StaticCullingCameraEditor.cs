using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NGS.AdvancedCullingSystem.Static
{
    [CustomEditor(typeof(StaticCullingCamera))]
    public class StaticCullingCameraEditor : Editor
    {
        private static bool ShowFrustum;

        protected new StaticCullingCamera target
        {
            get
            {
                return base.target as StaticCullingCamera;
            }
        }

        private SerializedProperty _drawCellsProp;
        private SerializedProperty _toleranceProp;
        private Camera _camera;


        private void OnEnable()
        {
            _drawCellsProp = serializedObject.FindProperty("_drawCells");
            _toleranceProp = serializedObject.FindProperty("_tolerance");
            _camera = target.GetComponent<Camera>();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            ShowFrustum = EditorGUILayout.Toggle("Show Frustum", ShowFrustum);

            if (EditorGUI.EndChangeCheck())
                ResetSceneCamerasCullingMatrices();

            EditorGUILayout.PropertyField(_drawCellsProp);
            _toleranceProp.floatValue = EditorGUILayout.Slider("Tolerance", _toleranceProp.floatValue, 0, 3);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if (!Application.isPlaying)
                return;

            if (!ShowFrustum)
                return;

            foreach (var camera in SceneView.GetAllSceneCameras())
                camera.cullingMatrix = _camera.cullingMatrix;
        }

        private void OnDisable()
        {
            ResetSceneCamerasCullingMatrices();
        }


        private void ResetSceneCamerasCullingMatrices()
        {
            foreach (var camera in SceneView.GetAllSceneCameras())
                camera.ResetCullingMatrix();
        }
    }
}
