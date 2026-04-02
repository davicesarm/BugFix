using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MiniGameQuestionEntry
{
    [TextArea(2, 5)]
    public string questionText;

    [SerializeField]
    private string[] answers = new string[3]
    {
        "",
        "",
        ""
    };

    [Range(0, 2)]
    public int correctAnswerIndex;

    public string GetAnswerText(int answerIndex, string fallback)
    {
        if (answers == null || answerIndex < 0 || answerIndex >= answers.Length)
            return fallback;

        string answer = answers[answerIndex];
        if (string.IsNullOrWhiteSpace(answer))
            return fallback;

        return answer;
    }
}

[CreateAssetMenu(fileName = "MiniGameQuestionBank", menuName = "Minigame/Question Bank")]
public class MiniGameQuestionBank : ScriptableObject
{
    [SerializeField]
    private List<MiniGameQuestionEntry> questions = new();

    public IReadOnlyList<MiniGameQuestionEntry> Questions => questions;
}
