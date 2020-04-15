using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UnityEditor.Rendering.Universal.Internal
{
    /// <summary>
    /// Contains a database of built-in resource GUIds. These are used to load built-in resource files.
    /// </summary>
    public static class ResourceGuid
    {
        /// <summary>
        /// GUId for the <c>ScriptableRendererFeature</c> template file.
        /// </summary>
        public static readonly string rendererTemplate = "51493ed8d97d3c24b94c6cffe834630b";
    }
}

namespace UnityEditor.Rendering.Universal
{
    static class EditorUtils
    {
        // Each group is separate in the menu by a menu bar
        public const int lwrpAssetCreateMenuPriorityGroup1 = CoreUtils.assetCreateMenuPriority1;
        public const int lwrpAssetCreateMenuPriorityGroup2 = CoreUtils.assetCreateMenuPriority1 + 50;
        public const int lwrpAssetCreateMenuPriorityGroup3 = lwrpAssetCreateMenuPriorityGroup2 + 50;

        public enum Unit { Metric, Percent }

        internal class Styles
        {
            //Measurements
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        public static void DrawCascadeSplitGUI<T>(ref SerializedProperty shadowCascadeSplit, float distance, Unit unit)
        {
            float[] cascadePartitionSizes = null;
            Type type = typeof(T);

            if (type == typeof(float))
            {
                cascadePartitionSizes = new float[] { shadowCascadeSplit.floatValue };
            }
            else if (type == typeof(Vector3))
            {
                Vector3 splits = shadowCascadeSplit.vector3Value;
                cascadePartitionSizes = new float[]
                {
                    Mathf.Clamp(splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[1] - splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[2] - splits[1], 0.0f, 1.0f)
                };
            }
            else if (type == typeof(Vector2))
            {
                Vector2 splits = shadowCascadeSplit.vector2Value;
                cascadePartitionSizes = new float[]
                {
                    Mathf.Clamp(splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[1] - splits[0], 0.0f, 1.0f),
                };
            }

            if (type == typeof(float))
            {
                var value = shadowCascadeSplit.floatValue;

                EditorGUI.BeginChangeCheck();
                var meterValue = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Split 1",""), (float)Math.Round(value * distance, 2), 0f, distance, null);

                if (EditorGUI.EndChangeCheck())
                {
                    var posMeter = Mathf.Clamp(meterValue, 0.01f, distance);
                    float percValue = posMeter / distance;
                    shadowCascadeSplit.floatValue = percValue;
                }
            }
            else if (type == typeof(Vector2))
            {
                for (int i = 0; i < cascadePartitionSizes.Length; ++i)
                {
                    var vec2value = shadowCascadeSplit.vector2Value;
                    var threshold = 0.1f/distance;

                    EditorGUI.BeginChangeCheck();
                    var meterValue = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i+1}",""), (float)Math.Round(vec2value[i] * distance, 2), 0f, distance, null);

                    if (EditorGUI.EndChangeCheck())
                    {
                        var posMeter = Mathf.Clamp(meterValue, 0.01f, distance);
                        float percValue = posMeter / distance;
                        if (i < cascadePartitionSizes.Length-1)
                        {
                            percValue = Math.Min((percValue), (vec2value[i+1]-threshold) );
                        }

                        if (i != 0)
                        {
                            percValue = Math.Max((percValue), (vec2value[i-1]+threshold) );
                        }

                        vec2value[i] = percValue;
                        shadowCascadeSplit.vector2Value = vec2value;
                    }
                }
            }
            else
            {
                for (int i = 0; i < cascadePartitionSizes.Length; ++i)
                {
                    var vec3value = shadowCascadeSplit.vector3Value;
                    var threshold = 0.1f/distance;

                    EditorGUI.BeginChangeCheck();
                    var meterValue = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i+1}",""), (float)Math.Round(vec3value[i] * distance, 2), 0f, distance, null);

                    if (EditorGUI.EndChangeCheck())
                    {
                        var posMeter = Mathf.Clamp(meterValue, 0.01f, distance);
                        float percValue = posMeter / distance;
                        if (i < cascadePartitionSizes.Length-1)
                        {
                            percValue = Math.Min((percValue), (vec3value[i+1]-threshold) );
                        }

                        if (i != 0)
                        {
                            percValue = Math.Max((percValue), (vec3value[i-1]+threshold) );
                        }

                        vec3value[i] = percValue;
                        shadowCascadeSplit.vector3Value = vec3value;
                    }
                }
            }

            if (cascadePartitionSizes != null)
            {
                EditorGUI.BeginChangeCheck();
                ShadowCascadeSplitGUI.HandleCascadeSliderGUI(ref cascadePartitionSizes, distance, unit);
                if (EditorGUI.EndChangeCheck())
                {
                    if (type == typeof(float))
                        shadowCascadeSplit.floatValue = cascadePartitionSizes[0];
                    else if(type == typeof(Vector2))
                    {
                        Vector2 updatedValue = new Vector2();
                        updatedValue[0] = cascadePartitionSizes[0];
                        updatedValue[1] = updatedValue[0] + cascadePartitionSizes[1];
                        shadowCascadeSplit.vector2Value = updatedValue;
                    }
                    else
                    {
                        Vector3 updatedValue = new Vector3();
                        updatedValue[0] = cascadePartitionSizes[0];
                        updatedValue[1] = updatedValue[0] + cascadePartitionSizes[1];
                        updatedValue[2] = updatedValue[1] + cascadePartitionSizes[2];
                        shadowCascadeSplit.vector3Value = updatedValue;
                    }
                }
            }
        }
    }
}
