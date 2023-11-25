using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(HideConditionAttribute))]
public class HideConditionPropertyDrawer : PropertyDrawer
{
    private enum Visibility
    {
        Visible,
        Disabled,
        Hidden
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool oldGuiEnabled = GUI.enabled;

        Visibility visibility = GetVisibility(property);
        if (visibility is Visibility.Visible or Visibility.Disabled)
        {
            if (visibility == Visibility.Disabled)
            {
                GUI.enabled = false;
            }

            EditorGUI.PropertyField(position, property);
            if (visibility == Visibility.Disabled)
            {
                GUI.enabled = oldGuiEnabled;
            }
        }
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        Visibility visibility = GetVisibility(property);
        if (visibility is Visibility.Visible or Visibility.Disabled)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }

        return -EditorGUIUtility.standardVerticalSpacing;
    }

    private Visibility GetVisibility(SerializedProperty property)
    {
        HideConditionAttribute conditionAttribute = (HideConditionAttribute)attribute;

        if (conditionAttribute == null)
        {
            return Visibility.Visible;
        }

        string condition = conditionAttribute.Condition;
        SerializedObject serializedObject = property.serializedObject;

        if (serializedObject == null)
        {
            return Visibility.Visible;
        }
        object target = PropertyDrawerExtensionMethods.GetTargetObjectOfProperty(property);
        Type test = target.GetType();
        FieldInfo conditionProperty = test.GetField(condition);


        if (conditionProperty == null)
        {
            EditorGUILayout.HelpBox(
                $"The condition used on the HideCondition ({condition}) can't be serialized.",
                MessageType.Error);
            return Visibility.Visible;
        }

        if (conditionProperty.FieldType != typeof(bool))
        {
            EditorGUILayout.HelpBox(
                $"The condition used on the HideCondition ({condition}) attribute is not a boolean.",
                MessageType.Error);
            return Visibility.Visible;
        }

        if (!(bool)conditionProperty.GetValue(target))
        {
            return Visibility.Visible;
        }

        if (conditionAttribute.Disable)
        {
            return Visibility.Disabled;
        }

        return Visibility.Hidden;
    }

}
[CustomPropertyDrawer(typeof(ShowConditionAttribute))]
public class ShowConditionPropertyDrawer : PropertyDrawer
{
    private enum Visibility
    {
        Visible,
        Disabled,
        Hidden
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool oldGuiEnabled = GUI.enabled;

        Visibility visibility = GetVisibility(property);
        if (visibility is Visibility.Visible or Visibility.Disabled)
        {
            if (visibility == Visibility.Disabled)
            {
                GUI.enabled = false;
            }

            EditorGUI.PropertyField(position, property);
            if (visibility == Visibility.Disabled)
            {
                GUI.enabled = oldGuiEnabled;
            }
        }
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        Visibility visibility = GetVisibility(property);
        if (visibility is Visibility.Visible or Visibility.Disabled)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }

        return -EditorGUIUtility.standardVerticalSpacing;
    }

    private Visibility GetVisibility(SerializedProperty property)
    {
        ShowConditionAttribute conditionAttribute = (ShowConditionAttribute)attribute;

        if (conditionAttribute == null)
        {
            return Visibility.Visible;
        }

        string condition = conditionAttribute.Condition;
        SerializedObject serializedObject = property.serializedObject;

        if (serializedObject == null)
        {
            return Visibility.Visible;
        }
        object target = PropertyDrawerExtensionMethods.GetTargetObjectOfProperty(property);
        Type test = target.GetType();
        FieldInfo conditionProperty = test.GetField(condition);


        if (conditionProperty == null)
        {
            EditorGUILayout.HelpBox(
                $"The condition used on the HideCondition ({condition}) can't be serialized.",
                MessageType.Error);
            return Visibility.Visible;
        }

        if (conditionProperty.FieldType != typeof(bool))
        {
            EditorGUILayout.HelpBox(
                $"The condition used on the HideCondition ({condition}) attribute is not a boolean.",
                MessageType.Error);
            return Visibility.Visible;
        }

        if ((bool)conditionProperty.GetValue(target))
        {
            return Visibility.Visible;
        }

        if (conditionAttribute.Disable)
        {
            return Visibility.Disabled;
        }

        return Visibility.Hidden;
    }

}

public static class PropertyDrawerExtensionMethods
{
    public static object GetTargetObjectOfProperty(SerializedProperty prop)
    {
        string path = prop.propertyPath.Replace(".Array.data[", "[");
        object obj = prop.serializedObject.targetObject;
        System.Collections.Generic.List<string> tempelements = path.Split('.').ToList();
        tempelements.RemoveAt(tempelements.Count - 1);
        string[] elements = tempelements.ToArray();
        foreach (string element in elements)
        {
            if (element.Contains("["))
            {
                string elementName = element.Substring(0, element.IndexOf("["));
                int index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                obj = GetValue_Imp(obj, elementName, index);
            }
            else
            {
                obj = GetValue_Imp(obj, element);
            }
        }
        return obj;
    }

    private static object GetValue_Imp(object source, string name)
    {
        if (source == null)
            return null;
        Type type = source.GetType();

        while (type != null)
        {
            FieldInfo f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f != null)
                return f.GetValue(source);

            PropertyInfo p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p != null)
                return p.GetValue(source, null);

            type = type.BaseType;
        }
        return null;
    }

    private static object GetValue_Imp(object source, string name, int index)
    {
        System.Collections.IEnumerable enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
        if (enumerable == null) return null;
        System.Collections.IEnumerator enm = enumerable.GetEnumerator();
        //while (index-- >= 0)
        //    enm.MoveNext();
        //return enm.Current;

        for (int i = 0; i <= index; i++)
        {
            if (!enm.MoveNext()) return null;
        }
        return enm.Current;
    }
}
#endif