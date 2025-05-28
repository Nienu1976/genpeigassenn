using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI; // ← ★★★この行を確認・追加してください★★★

public class WordSelectionManager : MonoBehaviour
{
    [Header("札のプレハブと親を設定")]
    public GameObject wordCardPrefab;
    public Transform cardGridParent;

    [Header("ターン表示UI")]
    public TextMeshProUGUI overallTurnTextDisplay;
    public TextMeshProUGUI currentSelectingPlayerNameDisplayText;

    [Header("札選択の管理")]
    public float selectedCardScaleFactor = 1.1f;
    public Color genjiTeamColor = Color.blue;
    public Color heishiTeamColor = Color.red;
    public Transform enlargedCardHolder; // ★拡大表示された札を置く親のTransform★

    [Header("役職名")]
    public string leaderRoleName = "総大将";
    public string memberRoleName = "武将";

    private int currentPlayerTurnTeamIndex = 0;
    private int currentGenjiPlayerSelectIndex = 0;
    private int currentHeishiPlayerSelectIndex = 0;
    private WordCardUI currentlyHighlightedCard = null;

    private int cardsSelectedByGenji = 0;
    private int cardsSelectedByHeishi = 0;
    private const int CARDS_TO_SELECT_PER_TEAM = 40;

    void OnEnable()
    {
        InitializeAndDisplayCards();
    }

    public void InitializeAndDisplayCards()
    {
        Debug.Log("WordSelectionManager: InitializeAndDisplayCards() 呼び出し。");
        if (!ValidateReferences()) return;

        List<SelectableWordEntry> wordsToDisplay = WordDataManager.Instance.SelectableWordsList;
        int genjiPlayerCount = WordDataManager.Instance.GenjiPlayerCount;
        List<string> initialGenjiNames = WordDataManager.Instance.GenjiPlayerNames;
        int heishiPlayerCount = WordDataManager.Instance.HeishiPlayerCount;
        List<string> initialHeishiNames = WordDataManager.Instance.HeishiPlayerNames;

        if (wordsToDisplay == null || wordsToDisplay.Count == 0) { Debug.LogWarning("WordSelectionManager: 表示するワードリストが空。"); return; }
        if (initialGenjiNames == null || genjiPlayerCount <= 0 || initialHeishiNames == null || heishiPlayerCount <= 0) { Debug.LogError("WordSelectionManager: プレイヤー情報がWordDataManagerから正しく取得できません。"); return; }

       // foreach (Transform child in cardGridParent) { Destroy(child.gameObject); }
       // if (enlargedCardHolder != null) foreach (Transform child in enlargedCardHolder) { Destroy(child.gameObject); }

        foreach (SelectableWordEntry entry in wordsToDisplay)
        {
            GameObject newCardObject = Instantiate(wordCardPrefab, cardGridParent);
            newCardObject.name = $"WordCard_{entry.DisplayNumber}";
            WordCardUI cardUI = newCardObject.GetComponent<WordCardUI>();
            if (cardUI != null)
            {
                cardUI.SetCardData(entry.DisplayNumber, entry.Word);
            }
            else { Debug.LogError($"生成した札 ({newCardObject.name}) にWordCardUIなし！"); }
        }

        cardsSelectedByGenji = 0;
        cardsSelectedByHeishi = 0;
        currentPlayerTurnTeamIndex = 0;
        currentGenjiPlayerSelectIndex = 0;
        currentHeishiPlayerSelectIndex = 0;
        currentlyHighlightedCard = null;

        UpdateOverallTurnDisplay();
        UpdateCurrentSelectingPlayerNameDisplay();
        Debug.Log("WordSelectionManager: 札の生成と初期設定完了。");
    }

    bool ValidateReferences()
    {
        bool isValid = true;
        if (wordCardPrefab == null) { Debug.LogError("WordSelectionManager: wordCardPrefabが未設定！"); isValid = false; }
        if (cardGridParent == null) { Debug.LogError("WordSelectionManager: cardGridParentが未設定！"); isValid = false; }
        if (WordDataManager.Instance == null) { Debug.LogError("WordSelectionManager: WordDataManagerが見つかりません！"); isValid = false; }
        if (overallTurnTextDisplay == null) { Debug.LogWarning("WordSelectionManager: overallTurnTextDisplayが未設定。"); }
        if (currentSelectingPlayerNameDisplayText == null) { Debug.LogWarning("WordSelectionManager: currentSelectingPlayerNameDisplayTextが未設定。"); }
        if (enlargedCardHolder == null) { Debug.LogError("WordSelectionManager: enlargedCardHolderが未設定！"); isValid = false; }
        return isValid;
    }

public void OnCardClicked(WordCardUI clickedCard)
    {
        if (clickedCard == null || clickedCard.IsSelectedByTeam()) return;
        Debug.Log($"WordSelectionManager: 札「{clickedCard.GetCardNumber()}」クリック。選択中: {(currentlyHighlightedCard != null ? currentlyHighlightedCard.GetCardNumber() : "なし")}");

        // public Transform enlargedCardHolder; // ← この変数はもう使いません (宣言から削除してもOK)

        if (currentlyHighlightedCard == null) // Case 1: まだ何も拡大されていない
        {
            currentlyHighlightedCard = clickedCard;
            currentlyHighlightedCard.SetHighlighted(true, selectedCardScaleFactor); // ★引数を2つに★
        }
        else if (currentlyHighlightedCard != clickedCard) // Case 2: 違う札をクリックした
        {
            currentlyHighlightedCard.SetHighlighted(false, 1f); // 前の札を元のサイズに ★引数を2つに★
            currentlyHighlightedCard = clickedCard;
            currentlyHighlightedCard.SetHighlighted(true, selectedCardScaleFactor); // 新しい札を拡大 ★引数を2つに★
        }
        else if (currentlyHighlightedCard == clickedCard) // Case 3: 同じ拡大中の札を再度クリック（確定）
        {
            Debug.Log($"札「{clickedCard.GetCardNumber()}」を確定します。");
            Color teamColor = (currentPlayerTurnTeamIndex == 0) ? genjiTeamColor : heishiTeamColor;
            
            // clickedCard.SetHighlighted(false, 1f); // SetSelectedByTeam の中でスケールも戻るはず
            clickedCard.SetSelectedByTeam(teamColor); // 色を変えて選択不可に (この中で元のスケールに戻る)
            
            if (currentPlayerTurnTeamIndex == 0) cardsSelectedByGenji++;
            else cardsSelectedByHeishi++;
            Debug.Log($"{(currentPlayerTurnTeamIndex == 0 ? "源氏" : "平氏")}が札「{clickedCard.GetCardNumber()}」獲得。");
            currentlyHighlightedCard = null;

            // ... (終了条件チェックとターン交代はそのまま) ...
            if ((currentPlayerTurnTeamIndex == 0 && cardsSelectedByGenji >= CARDS_TO_SELECT_PER_TEAM) &&
                (currentPlayerTurnTeamIndex == 1 && cardsSelectedByHeishi >= CARDS_TO_SELECT_PER_TEAM) ||
                (cardsSelectedByGenji + cardsSelectedByHeishi >= WordDataManager.Instance.SelectableWordsList.Count - 10) )
            {
                // ... (終了処理) ...
                return;
            }
            SwitchToNextPlayerAndTurn();
        }
    }

    void UpdateOverallTurnDisplay()
    {
        if (overallTurnTextDisplay != null)
        {
            overallTurnTextDisplay.text = (currentPlayerTurnTeamIndex == 0) ? "源氏軍選択ターン" : "平氏軍選択ターン";
            overallTurnTextDisplay.color = (currentPlayerTurnTeamIndex == 0) ? genjiTeamColor : heishiTeamColor;
        }
    }

// 現在選択中のプレイヤー名表示を更新する
    void UpdateCurrentSelectingPlayerNameDisplay()
    {
        if (currentSelectingPlayerNameDisplayText != null)
        {
            string playerName = GetCurrentPlayerName(); // 現在のプレイヤー名を取得

            // ★↓表示するテキストを、プレイヤー名だけにします↓★
            currentSelectingPlayerNameDisplayText.text = playerName;
            // ★↑ここまで↑★

            // 文字色をチームカラーに設定 (これはそのまま)
            currentSelectingPlayerNameDisplayText.color = (currentPlayerTurnTeamIndex == 0) ? genjiTeamColor : heishiTeamColor;
            
            Debug.Log($"選択中プレイヤー表示を更新: 「{currentSelectingPlayerNameDisplayText.text}」 (チーム色適用)");
        }
        else
        {
            Debug.LogWarning("WordSelectionManager: currentSelectingPlayerNameDisplayText がInspectorで設定されていません。");
        }
    }

    string GetCurrentTeamNameForDisplay() // ログや表示用のチーム名取得ヘルパー
    {
        return (currentPlayerTurnTeamIndex == 0) ? "源氏軍" : "平氏軍";
    }

    string GetCurrentPlayerName()
    {
        if (WordDataManager.Instance == null) return "エラー";
        List<string> names;
        int index;
        int count;

        if (currentPlayerTurnTeamIndex == 0)
        {
            names = WordDataManager.Instance.GenjiPlayerNames;
            index = currentGenjiPlayerSelectIndex;
            count = WordDataManager.Instance.GenjiPlayerCount;
        }
        else
        {
            names = WordDataManager.Instance.HeishiPlayerNames;
            index = currentHeishiPlayerSelectIndex;
            count = WordDataManager.Instance.HeishiPlayerCount;
        }

        if (names != null && index < names.Count && index < count)
        {
            return names[index];
        }
        return $"{(currentPlayerTurnTeamIndex == 0 ? "源氏" : "平氏")}{(index + 1)}"; // 仮の名前
    }

    string GetCurrentPlayerRole()
    {
        int playerIndexInTeam = (currentPlayerTurnTeamIndex == 0) ? currentGenjiPlayerSelectIndex : currentHeishiPlayerSelectIndex;
        return (playerIndexInTeam == 0) ? leaderRoleName : memberRoleName;
    }

    void SwitchToNextPlayerAndTurn()
    {
        if (WordDataManager.Instance == null) return;
        int genjiCount = WordDataManager.Instance.GenjiPlayerCount;
        int heishiCount = WordDataManager.Instance.HeishiPlayerCount;

        string previousPlayerName = GetCurrentPlayerName(); // ログ用

        if (currentPlayerTurnTeamIndex == 0) // 現在が源氏のターンだった
        {
            // 次が平氏のターンで、平氏のプレイヤーがまだ残っていれば
            if (currentHeishiPlayerSelectIndex < heishiCount)
            {
                currentPlayerTurnTeamIndex = 1;
            }
            else
            { // 平氏が全員選び終わっていたら、源氏の次の人（これは源平交互なのでありえないはずだが念のため）
                currentGenjiPlayerSelectIndex++;
                if (currentGenjiPlayerSelectIndex >= genjiCount) currentGenjiPlayerSelectIndex = 0; // 源氏も一周
                // currentPlayerTurnTeamIndex は 0 のまま
            }
        }
        else // 現在が平氏のターンだった
        {
            // 次は源氏のターンなので、両チームのインデックスを進める
            currentGenjiPlayerSelectIndex++;
            currentHeishiPlayerSelectIndex++; // 次の平氏のターンのための準備
            currentPlayerTurnTeamIndex = 0;

            if (currentGenjiPlayerSelectIndex >= genjiCount) currentGenjiPlayerSelectIndex = 0;
            if (currentHeishiPlayerSelectIndex >= heishiCount && heishiCount > 0)
            { // 平氏が0人の場合を考慮
              // 平氏が全員選び終わっていても、源氏の番なら源氏のインデックスは進む
              // もし、源氏も平氏も一周したら、というロジックはここではない
            }
        }



        // ---- 新しいターンの決定ロジック (源氏→平氏→源氏の次の人→平氏の次の人...) ----
        if (currentPlayerTurnTeamIndex == 0)
        { // 前のターンが平氏で、今源氏の番になった
            // currentGenjiPlayerSelectIndex は既にインクリメントされているか、0に戻っている
        }
        else
        { 
        }


        UpdateOverallTurnDisplay();
        UpdateCurrentSelectingPlayerNameDisplay();
        Debug.Log($"ターン交代。前: {previousPlayerName} さん。 次は {GetCurrentTeamNameForDisplay()} の {GetCurrentPlayerRole()}「{GetCurrentPlayerName()}」さんの番です。");
    }

    void SetAllCardsInteractable(bool interactable)
    {
        if (cardGridParent == null) return;
        foreach (Transform cardTransform in cardGridParent)
        {
            WordCardUI card = cardTransform.GetComponent<WordCardUI>();
            if (card != null && !card.IsSelectedByTeam())
            {
                Button btn = card.GetComponent<Button>();
                if (btn != null) btn.interactable = interactable;
            }
        }
    }
}