// EnumBindDrawer.cs (put into Assets/Editor)
using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EnumBindAttribute), true)]
public class EnumBindDrawer : PropertyDrawer
{
    static bool s_logged = false;
    static readonly Regex s_arrayIndexRegex = new Regex(@"\.Array\.data\[(\d+)\]$", RegexOptions.Compiled);
    static readonly Regex s_camelSplitRegex = new Regex(@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (EnumBindAttribute)attribute;

        // Log once for debug (remove if not needed)
        if (!s_logged)
        {
            Debug.Log($"EnumBindDrawer running for property '{property.propertyPath}' on object '{property.serializedObject.targetObject?.name}'");
            s_logged = true;
        }

        // If enum type invalid, fallback to default drawing
        if (attr.EnumType == null || !attr.EnumType.IsEnum)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        string[] names = Enum.GetNames(attr.EnumType);

        // If Unity called drawer for an array element (e.g. "popupList.Array.data[0]"),
        // change the label to the enum name (nicely formatted), and also clamp parent array size
        var match = s_arrayIndexRegex.Match(property.propertyPath);
        if (match.Success)
        {
            // clamp parent array size so user can't add beyond enum length
            string parentPath = property.propertyPath.Substring(0, match.Index); // e.g. "popupList"
            var parentProp = property.serializedObject.FindProperty(parentPath);
            if (parentProp != null && parentProp.isArray)
            {
                if (parentProp.arraySize > names.Length)
                {
                    parentProp.arraySize = names.Length;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            if (int.TryParse(match.Groups[1].Value, out int idx))
            {
                string nameLabel = (idx >= 0 && idx < names.Length) ? NicifyEnumName(names[idx]) : $"Element {idx}";
                var customLabel = new GUIContent(nameLabel);
                EditorGUI.PropertyField(position, property, customLabel, true);
                return;
            }
        }

        // If we get here: property is the array itself (not an element). We draw custom foldout and full list.
        if (!property.isArray)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        float lineH = EditorGUIUtility.singleLineHeight;
        float vSpace = EditorGUIUtility.standardVerticalSpacing;

        // Header (foldout)
        Rect headRect = new Rect(position.x, position.y, position.width, lineH);
        property.isExpanded = EditorGUI.Foldout(headRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            // Ensure array size does not exceed enum length
            if (property.arraySize > names.Length)
            {
                property.arraySize = names.Length;
                property.serializedObject.ApplyModifiedProperties();
            }

            // Draw each element with enum name label
            for (int i = 0; i < names.Length; i++)
            {
                Rect elemRect = new Rect(position.x, position.y + lineH + vSpace + i * (lineH + vSpace),
                                         position.width, lineH);
                var elem = property.GetArrayElementAtIndex(i);
                if (elem != null)
                {
                    EditorGUI.PropertyField(elemRect, elem, new GUIContent(NicifyEnumName(names[i])), true);
                }
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attr = (EnumBindAttribute)attribute;

        // If drawer applied to element level, single line
        var match = s_arrayIndexRegex.Match(property.propertyPath);
        if (match.Success)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        if (attr.EnumType == null || !attr.EnumType.IsEnum || !property.isArray)
            return EditorGUIUtility.singleLineHeight;

        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        int count = Enum.GetNames(attr.EnumType).Length;
        float lineH = EditorGUIUtility.singleLineHeight;
        float vSpace = EditorGUIUtility.standardVerticalSpacing;

        return lineH + (lineH + vSpace) * count + vSpace;
    }

    // Nicify: split camel-case and also keep underscores (if any)
    static string NicifyEnumName(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return raw;
        // Replace underscores with spaces first
        string s = raw.Replace('_', ' ');
        // Insert spaces between camel-humps
        s = s_camelSplitRegex.Replace(s, " ");
        // Optionally, you could run ToLower/ToTitleCase; here we keep original casing except spacing.
        return s.Trim();
    }
}