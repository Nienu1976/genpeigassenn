using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// 問題文と答えのペア、そして問題番号も保持するための構造体
[System.Serializable]
public struct FullQuizDataEntry
{
    public string QuestionNumber;
    public string QuestionText;
    public string AnswerWord;

    public FullQuizDataEntry(string number, string question, string answer)
    {
        QuestionNumber = number;
        QuestionText = question;
        AnswerWord = answer;
    }
}

// ワード選択フェイズ用の、番号とワードのペアを保持するための構造体
[System.Serializable]
public struct SelectableWordEntry
{
    public string DisplayNumber;
    public string Word;

    public SelectableWordEntry(string number, string word)
    {
        DisplayNumber = number;
        Word = word;
    }
}



public class WordDataManager : MonoBehaviour
{
    public List<SelectableWordEntry> SelectableWordsList { get; private set; } = new List<SelectableWordEntry>();
    public List<FullQuizDataEntry> MasterQuizDataList { get; private set; } = new List<FullQuizDataEntry>();

    private const int SELECTABLE_CSV_NUMBER_COLUMN = 0;
    private const int SELECTABLE_CSV_WORD_COLUMN = 1;
    private const int MASTER_CSV_NUMBER_COLUMN = 0;
    private const int MASTER_CSV_QUESTION_COLUMN = 1;
    private const int MASTER_CSV_ANSWER_COLUMN = 2;

    public enum LoadResult { Success, FileNotFound, FormatError, EmptyFile, UnknownError }
    public static WordDataManager Instance { get; private set; }

[Header("チーム設定情報")]
public int GenjiPlayerCount { get; set; } = 1; 
public List<string> GenjiPlayerNames { get; private set; } = new List<string>();
public int HeishiPlayerCount { get; set; } = 1; 
public List<string> HeishiPlayerNames { get; private set; } = new List<string>();
    // ★↓チームごとの上限解答数を追加↓★
    public int GenjiMaxAnswersPerPlayer { get; set; } = 4; // 源氏軍の一人あたりの上限解答数
    public int HeishiMaxAnswersPerPlayer { get; set; } = 4; // 平氏軍の一人あたりの上限解答数
    // ★↑ここまで↑★

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else if (Instance != this) { Destroy(gameObject); }
    }

    public LoadResult LoadSelectableWordsFromCSV(string filePath)
    {
        SelectableWordsList.Clear();
        if (string.IsNullOrEmpty(filePath)) { Debug.LogError("LoadSelectableWords: ファイルパスが空です。"); return LoadResult.FileNotFound; }
        if (!File.Exists(filePath)) { Debug.LogError($"LoadSelectableWords: ファイルが見つかりません: {filePath}"); return LoadResult.FileNotFound; }
        try
        {
            string fileContent = File.ReadAllText(filePath, new UTF8Encoding(false));
            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) { Debug.LogWarning($"LoadSelectableWords: {Path.GetFileName(filePath)} は空か改行のみ。"); return LoadResult.EmptyFile; }
            for (int i = 0; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                if (values.Length > Mathf.Max(SELECTABLE_CSV_NUMBER_COLUMN, SELECTABLE_CSV_WORD_COLUMN))
                {
                    string num = values[SELECTABLE_CSV_NUMBER_COLUMN].Trim();
                    string wordStr = values[SELECTABLE_CSV_WORD_COLUMN].Trim();
                    if (!string.IsNullOrEmpty(num) && !string.IsNullOrEmpty(wordStr))
                    {
                        SelectableWordsList.Add(new SelectableWordEntry(num, wordStr));
                    }
                }
            }
            if (SelectableWordsList.Count == 0 && lines.Length > 0) { return LoadResult.FormatError; }
            Debug.Log($"LoadSelectableWords: {SelectableWordsList.Count} 個のワードを格納。");
            return LoadResult.Success;
        }
        catch (System.Exception e) { Debug.LogError($"LoadSelectableWords: エラー: {e.Message}"); return LoadResult.UnknownError; }
    }

    public LoadResult LoadMasterQuizDataFromCSV(string filePath, bool skipHeader = true)
    {
        MasterQuizDataList.Clear();
        if (string.IsNullOrEmpty(filePath)) { return LoadResult.FileNotFound; }
        if (!File.Exists(filePath)) { return LoadResult.FileNotFound; }
        try
        {
            string fileContent = File.ReadAllText(filePath, new UTF8Encoding(false));
            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == (skipHeader ? 1 : 0) || lines.Length == 0) { return LoadResult.EmptyFile; }
            for (int i = 0; i < lines.Length; i++)
            {
                if (skipHeader && i == 0) continue;
                string[] values = lines[i].Split(',');
                if (values.Length > Mathf.Max(MASTER_CSV_NUMBER_COLUMN, MASTER_CSV_QUESTION_COLUMN, MASTER_CSV_ANSWER_COLUMN))
                {
                    MasterQuizDataList.Add(new FullQuizDataEntry(values[MASTER_CSV_NUMBER_COLUMN].Trim(), values[MASTER_CSV_QUESTION_COLUMN].Trim(), values[MASTER_CSV_ANSWER_COLUMN].Trim()));
                }
            }
            if (MasterQuizDataList.Count == 0 && lines.Length > (skipHeader ? 1 : 0)) { return LoadResult.FormatError; }
            Debug.Log($"LoadMasterQuizData: {MasterQuizDataList.Count} 個のクイズデータを格納。");
            return LoadResult.Success;
        }
        catch (System.Exception e) { Debug.LogError($"LoadMasterQuizData: エラー: {e.Message}"); return LoadResult.UnknownError; }
    }
        // ★↓チーム情報を設定するための新しい関数を追加↓★
    public void SetTeamSettings(int genjiCount, List<string> genjiNames, int genjiMaxAnswers, 
                                int heishiCount, List<string> heishiNames, int heishiMaxAnswers)
    {
        GenjiPlayerCount = genjiCount;
        GenjiPlayerNames = new List<string>(genjiNames);
        GenjiMaxAnswersPerPlayer = genjiMaxAnswers; // ★追加★

        HeishiPlayerCount = heishiCount;
        HeishiPlayerNames = new List<string>(heishiNames);
        HeishiMaxAnswersPerPlayer = heishiMaxAnswers; // ★追加★

        Debug.Log($"WordDataManager: チーム設定を保存しました。");
        Debug.Log($"源氏: {GenjiPlayerCount}人, 名前: [{string.Join(", ", GenjiPlayerNames)}], 上限解答: {GenjiMaxAnswersPerPlayer}");
        Debug.Log($"平氏: {HeishiPlayerCount}人, 名前: [{string.Join(", ", HeishiPlayerNames)}], 上限解答: {HeishiMaxAnswersPerPlayer}");
    }
    // ★↑ここまで新しい関数を追加↑★
}
