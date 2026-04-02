using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(VumarkActionEntry))]
public class VumarkActionEntryDrawer : PropertyDrawer
{
    private const float VerticalSpacing = 4f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var vumarkIdProp = property.FindPropertyRelative("vumarkId");
        var actionTypeProp = property.FindPropertyRelative("actionType");
        var textProp = property.FindPropertyRelative("text");
        var sceneNameProp = property.FindPropertyRelative("sceneName");

        float y = position.y;
        float width = position.width;

        string header = string.IsNullOrWhiteSpace(vumarkIdProp.stringValue)
            ? label.text
            : vumarkIdProp.stringValue;

        Rect foldoutRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, header, true);
        y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        Rect idRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(idRect, vumarkIdProp);
        y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

        Rect actionRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(actionRect, actionTypeProp);
        y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

        VumarkActionType actionType = (VumarkActionType)actionTypeProp.enumValueIndex;

        if (actionType == VumarkActionType.ShowText)
        {
            Rect textLabelRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(textLabelRect, textProp.displayName);
            y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

            float textAreaHeight = EditorGUIUtility.singleLineHeight * 3f;
            Rect textAreaRect = new Rect(position.x, y, width, textAreaHeight);
            textProp.stringValue = EditorGUI.TextArea(textAreaRect, textProp.stringValue);
            y += textAreaHeight + VerticalSpacing;
        }
        else if (actionType == VumarkActionType.LoadScene)
        {
            Rect sceneRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(sceneRect, sceneNameProp);
            y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var actionTypeProp = property.FindPropertyRelative("actionType");
        var textProp = property.FindPropertyRelative("text");

        float height = 0f;

        height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // foldout

        if (!property.isExpanded)
            return height;

        height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // vumarkId
        height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // actionType

        VumarkActionType actionType = (VumarkActionType)actionTypeProp.enumValueIndex;

        if (actionType == VumarkActionType.ShowText)
        {
            height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // label "Text"
            height += EditorGUIUtility.singleLineHeight * 3f + VerticalSpacing; // text area
        }
        else if (actionType == VumarkActionType.LoadScene)
        {
            height += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        }

        return height;
    }
}
