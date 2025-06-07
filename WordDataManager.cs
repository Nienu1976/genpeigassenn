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
    // ★↑ここまで↑★
    [Header("札選択フェイズで獲得/残った札のリスト")]

    public List<SelectableWordEntry> GenjiSelectedCards { get; set; } = new List<SelectableWordEntry>();
    public List<SelectableWordEntry> HeishiSelectedCards { get; set; } = new List<SelectableWordEntry>();
    public List<SelectableWordEntry> EmptyCardsList { get; private set; } = new List<SelectableWordEntry>();
    // ★↑ここまで↑★
    public int GenjiMaxAnswersPerPlayer { get; set; } = 4; // 源氏軍の一人あたりの上限解答数
    public int HeishiMaxAnswersPerPlayer { get; set; } = 4; // 平氏軍の一人あたりの上限解答数

    void Awake()
    {
        Debug.Log($"★★★★ WordDataManager: Awake() 実行開始。GameObject名: {gameObject.name} ★★★★"); // ★追加★

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンをまたいでも消えないようにする
            Debug.Log($"★★★★ WordDataManager: Instance に {gameObject.name} を設定しました。★★★★"); // ★変更★
        }
        else if (Instance != this)
        {
            // 既に別のインスタンスが存在する場合
            Debug.LogWarning($"★★★★ WordDataManager: Instance は既に存在しています ({Instance.gameObject.name})。新しい {gameObject.name} は破棄します。★★★★"); // ★変更★
            Destroy(gameObject);
        }
        else
        {
            // Instance == this の場合 (通常は起こらないはずだが念のため)
            Debug.LogWarning($"★★★★ WordDataManager: Instance は既にこの {gameObject.name} でした。何もしません。★★★★"); // ★追加★
        }
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

        // 新しい試合の準備なので、前の試合の獲得札と空札の記録はクリアする
        GenjiSelectedCards.Clear();
        HeishiSelectedCards.Clear();
        EmptyCardsList.Clear(); // ★ここもクリア★
        Debug.Log($"WordDataManager: チーム設定保存＆獲得札・空札リストクリア。");
    }

    // ★↓「選択された札をチームのリストに追加する」関数 (これは以前ご提案したものと同じはず)↓★
    public void AddSelectedCardToTeam(string teamInitial, SelectableWordEntry cardEntry)
    {
        if (teamInitial == "A") // 仮に源氏を"A"とする
        {
            if (!GenjiSelectedCards.Any(card => card.DisplayNumber == cardEntry.DisplayNumber)) // 重複追加を防ぐ
            {
                GenjiSelectedCards.Add(cardEntry);
            }
            Debug.Log($"★WordDataManager★ 源氏軍が札獲得！ 現在の源氏軍獲得札リスト ({GenjiSelectedCards.Count}枚):");
            foreach (var c in GenjiSelectedCards) { Debug.Log($"- 番号:{c.DisplayNumber}, ワード:{c.Word}"); }

        }
        else if (teamInitial == "B") // 仮に平氏を"B"とする
        {
            if (!HeishiSelectedCards.Any(card => card.DisplayNumber == cardEntry.DisplayNumber)) // 重複追加を防ぐ
            {
                HeishiSelectedCards.Add(cardEntry);
            }
            Debug.Log($"★WordDataManager★ 平氏軍が札獲得！ 現在の平氏軍獲得札リスト ({HeishiSelectedCards.Count}枚):");
            foreach (var c in HeishiSelectedCards) { Debug.Log($"- 番号:{c.DisplayNumber}, ワード:{c.Word}"); }

        }
        // Debug.Log($"WordDataManager: チーム{teamInitial}が札「{cardEntry.DisplayNumber}:{cardEntry.Word}」を獲得。");
    }
    // ★↓「Undoのために、最後に獲得した札をチームのリストから削除する」関数 (これも以前ご提案したものと同じはず)↓★
    public bool RemoveLastSelectedCardFromTeam(string teamInitial, SelectableWordEntry cardToRemove) // どの札を消すか明確に指定
    {
        bool removed = false;
        if (teamInitial == "A")
        {
            removed = GenjiSelectedCards.Remove(cardToRemove);
        }
        else if (teamInitial == "B")
        {
            removed = HeishiSelectedCards.Remove(cardToRemove);
        }
        // if(removed) Debug.Log($"WordDataManager: チーム{teamInitial}の札「{cardToRemove.DisplayNumber}」をUndo。");
        return removed;
    }
    public void FinalizeCardSelectionAndDetermineEmptyCards()
    {
        EmptyCardsList.Clear();
        if (SelectableWordsList == null || SelectableWordsList.Count == 0) return;

        // 全ての選択可能なワードについて、それが源氏または平氏に選ばれたかチェック
        foreach (SelectableWordEntry selectableCard in SelectableWordsList)
        {
            bool isSelectedByGenji = GenjiSelectedCards.Any(card => card.DisplayNumber == selectableCard.DisplayNumber);
            bool isSelectedByHeishi = HeishiSelectedCards.Any(card => card.DisplayNumber == selectableCard.DisplayNumber);

            if (!isSelectedByGenji && !isSelectedByHeishi) // どちらにも選ばれていなければ空札
            {
                EmptyCardsList.Add(selectableCard);
            }
        }
        Debug.Log($"WordDataManager: 空札を決定・保存しました。空札の数: {EmptyCardsList.Count}枚");
        // 確認用ログ
        // foreach(var emptyCard in EmptyCardsList) { Debug.Log($"空札: {emptyCard.DisplayNumber}:{emptyCard.Word}"); }
    }
    // --- ここからダミーメソッド ---
    public void RestoreAllCardStates(object genjiCardsState, object heishiCardsState, object emptyCardsState) { }
    public bool RemoveCardFromGenjiList(int index) // ★戻り値を bool に変更★
    {
        if (GenjiSelectedCards != null && index >= 0 && index < GenjiSelectedCards.Count)
        {
            string removedWord = GenjiSelectedCards[index].Word; // ログ用に名前を覚えておく
            string removedNumber = GenjiSelectedCards[index].DisplayNumber;
            GenjiSelectedCards.RemoveAt(index);
            Debug.Log($"WordDataManager: 源氏軍の札「{removedWord}」(番号:{removedNumber}) をリストから削除しました。残り: {GenjiSelectedCards.Count}枚");
            return true; // ★削除成功なので true を返す★
        }
        else
        {
            Debug.LogWarning($"WordDataManager: 源氏軍リストからの札削除に失敗。無効なインデックス: {index}");
            return false; // ★削除失敗なので false を返す★
        }
    }
    public bool RemoveCardFromHeishiList(int index) // ★戻り値を bool に変更★
    {
        if (HeishiSelectedCards != null && index >= 0 && index < HeishiSelectedCards.Count)
        {
            string removedWord = HeishiSelectedCards[index].Word;
            string removedNumber = HeishiSelectedCards[index].DisplayNumber;
            HeishiSelectedCards.RemoveAt(index);
            Debug.Log($"WordDataManager: 平氏軍の札「{removedWord}」(番号:{removedNumber}) をリストから削除しました。残り: {HeishiSelectedCards.Count}枚");
            return true; // ★削除成功なので true を返す★
        }
        else
        {
            Debug.LogWarning($"WordDataManager: 平氏軍リストからの札削除に失敗。無効なインデックス: {index}");
            return false; // ★削除失敗なので false を返す★
        }
    }
    public bool RemoveCardFromEmptyList(int index) // ★引数を int index に、戻り値を bool に変更★
    {
        if (EmptyCardsList != null && index >= 0 && index < EmptyCardsList.Count)
        {
            string removedWord = EmptyCardsList[index].Word; // ログ用に名前を覚えておく
            string removedNumber = EmptyCardsList[index].DisplayNumber;
            EmptyCardsList.RemoveAt(index);
            Debug.Log($"WordDataManager: 空札「{removedWord}」(番号:{removedNumber}) をリストから削除しました。残り: {EmptyCardsList.Count}枚");
            return true; // ★削除成功なので true を返す★
        }
        else
        {
            Debug.LogWarning($"WordDataManager: 空札リストからの札削除に失敗。無効なインデックス: {index}");
            return false; // ★削除失敗なので false を返す★
        }
    }
    public bool RemoveSpecificCardFromTeam(string teamInitial, SelectableWordEntry cardToRemove)
    {
        if (string.IsNullOrEmpty(cardToRemove.Word)) // 無効なカードデータなら何もしない
        {
            Debug.LogWarning($"[WordDataManager.RemoveSpecific] 削除対象のカードデータが無効です (Wordがnullまたは空)。");
            return false;
        }

        List<SelectableWordEntry> targetList = null;
        string teamNameForLog = "";

        if (teamInitial == "A")
        {
            targetList = GenjiSelectedCards;
            teamNameForLog = "源氏軍";
        }
        else if (teamInitial == "B")
        {
            targetList = HeishiSelectedCards;
            teamNameForLog = "平氏軍";
        }
        else
        {
            Debug.LogError($"[WordDataManager.RemoveSpecific] 無効な teamInitial「{teamInitial}」が指定されました。");
            return false;
        }

        if (targetList == null)
        {
            Debug.LogError($"[WordDataManager.RemoveSpecific] {teamNameForLog}の札リスト(targetList)がnullです。");
            return false;
        }

        // DisplayNumber と Word の両方が一致する最初の要素を探す
        int indexToRemove = -1;
        for (int i = 0; i < targetList.Count; i++)
        {
            if (targetList[i].DisplayNumber == cardToRemove.DisplayNumber && targetList[i].Word == cardToRemove.Word)
            {
                indexToRemove = i;
                break;
            }
        }

        if (indexToRemove != -1)
        {
            targetList.RemoveAt(indexToRemove);
            Debug.Log($"[WordDataManager.RemoveSpecific] {teamNameForLog}のリストから札「{cardToRemove.Word}」(番号:{cardToRemove.DisplayNumber}) を削除しました。残り: {targetList.Count}枚");
            return true;
        }
        else
        {
            Debug.LogWarning($"[WordDataManager.RemoveSpecific] {teamNameForLog}のリストに札「{cardToRemove.Word}」(番号:{cardToRemove.DisplayNumber}) が見つかりませんでした。削除できませんでした。");
            return false;
        }
    }

}
