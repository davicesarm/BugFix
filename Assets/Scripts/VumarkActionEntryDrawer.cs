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
        var textNoHintsProp = property.FindPropertyRelative("textNoHints");
        var sceneNameProp = property.FindPropertyRelative("sceneName");
        var modelPrefabProp = property.FindPropertyRelative("modelPrefab");
        var trophyToUnlockProp = property.FindPropertyRelative("trophyToUnlock");

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
            EditorGUI.LabelField(textLabelRect, "Texto (com dicas)");
            y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

            float textAreaHeight = EditorGUIUtility.singleLineHeight * 3f;
            Rect textAreaRect = new Rect(position.x, y, width, textAreaHeight);
            textProp.stringValue = EditorGUI.TextArea(textAreaRect, textProp.stringValue);
            y += textAreaHeight + VerticalSpacing;

            Rect textNoHintsLabelRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(textNoHintsLabelRect, "Texto (sem dicas)");
            y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

            Rect textNoHintsAreaRect = new Rect(position.x, y, width, textAreaHeight);
            textNoHintsProp.stringValue = EditorGUI.TextArea(textNoHintsAreaRect, textNoHintsProp.stringValue);
            y += textAreaHeight + VerticalSpacing;
        }
        else if (actionType == VumarkActionType.LoadScene)
        {
            Rect sceneRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(sceneRect, sceneNameProp);
            y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        }
        else if (actionType == VumarkActionType.ShowModel3D)
        {
            Rect modelLabelRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(modelLabelRect, "Modelo 3D");
            y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

            Rect modelFieldRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(modelFieldRect, modelPrefabProp, GUIContent.none);
            y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        }

        // Independente do tipo de carta: qualquer uma pode opcionalmente desbloquear um troféu.
        Rect trophyLabelRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(trophyLabelRect, "Troféu ao ler (opcional)");
        y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

        Rect trophyFieldRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(trophyFieldRect, trophyToUnlockProp, GUIContent.none);
        y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var actionTypeProp = property.FindPropertyRelative("actionType");
        float height = 0f;

        height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // foldout

        if (!property.isExpanded)
            return height;

        height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // vumarkId
        height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // actionType

        VumarkActionType actionType = (VumarkActionType)actionTypeProp.enumValueIndex;

        if (actionType == VumarkActionType.ShowText)
        {
            height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // label "Texto (com dicas)"
            height += EditorGUIUtility.singleLineHeight * 3f + VerticalSpacing; // text area (com dicas)
            height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // label "Texto (sem dicas)"
            height += EditorGUIUtility.singleLineHeight * 3f + VerticalSpacing; // text area (sem dicas)
        }
        else if (actionType == VumarkActionType.LoadScene)
        {
            height += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        }
        else if (actionType == VumarkActionType.ShowModel3D)
        {
            height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // label "Modelo 3D"
            height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // campo modelPrefab
        }

        height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // label "Troféu ao ler (opcional)"
        height += EditorGUIUtility.singleLineHeight + VerticalSpacing; // campo trophyToUnlock

        return height;
    }
}
