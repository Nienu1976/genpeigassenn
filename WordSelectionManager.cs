using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

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

    //private const int CARDS_TO_SELECT_PER_TEAM = 45;
    private const int TOTAL_CARDS_TO_TRIGGER_LOCK = 90; // この枚数に達したら残りをロック


    [Header("取得枚数表示UI")]
    public TextMeshProUGUI genjiCardsSelectedDisplayText; // 源氏軍の取得枚数表示用
    public TextMeshProUGUI heishiCardsSelectedDisplayText; // 平氏軍の取得枚数表示用

    [Header("Undo機能関連")]
    public Button undoButton; // Inspectorで「一手戻す」ボタンを設定

    // ★↓ここからUndo機能のための変数を追加または確認↓★
    [Header("Undo機能関連（Inspector設定不要）")] // UndoボタンはPlayerSetupManager側にある想定でしたね
    private WordCardUI lastConfirmedCard = null;    // 直前に「確定」された札を覚えておく
    private int lastConfirmedCardTeamIndex = -1; // 直前に札を「確定」したチームのインデックス
    private int previousGenjiPlayerSelectIndex = 0;  // 札が確定される「前」の源氏インデックス
    private int previousHeishiPlayerSelectIndex = 0; // 札が確定される「前」の平氏インデックス
    // ★↑ここまで↑★


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

        // ★↓ここから合計選択枚数をチェックし、必要なら残りの札をロックする処理を追加↓★
        int totalSelectedCards = cardsSelectedByGenji + cardsSelectedByHeishi;
        Debug.Log($"現在の合計選択枚数: {totalSelectedCards}枚");

        if (totalSelectedCards >= TOTAL_CARDS_TO_TRIGGER_LOCK)
        {
            Debug.Log($"合計選択枚数が{TOTAL_CARDS_TO_TRIGGER_LOCK}枚に達しました。残りの未選択の札を全てロックします。");
            SetAllUnselectedCardsInteractable(false); // ★新しいヘルパー関数を呼び出す★

            // ターン表示なども「札選択終了」などに変更しても良い
            if (overallTurnTextDisplay != null) overallTurnTextDisplay.text = "札選択終了";
            if (currentSelectingPlayerNameDisplayText != null) currentSelectingPlayerNameDisplayText.text = "";

            // ここで次のフェイズ（クイズ解答フェイズ）へ移行する準備をしても良い
            // GoToQuizPhase(); 
            return; // ターン交代は行わない
        }
        // ★↑ここまで↑★

        UpdateOverallTurnDisplay();
        UpdateCurrentSelectingPlayerNameDisplay();
        UpdateCardsSelectedDisplay(); // ★追加：初期表示のため★

        // ★↓Undoボタンを初期状態（押せない）にする↓★
        PlayerSetupManager psm = FindObjectOfType<PlayerSetupManager>(); // PlayerSetupManagerを探す
        if (psm != null && psm.undoCardSelectionButton != null)
        {
            psm.undoCardSelectionButton.interactable = false;
        }
        lastConfirmedCard = null;
        lastConfirmedCardTeamIndex = -1;
        // ★↑ここまで↑★
        Debug.Log("WordSelectionManager: 札の生成と初期設定完了。");

        Debug.Log("WordSelectionManager: 全ての札の生成と初期設定が完了しました。");
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
        if (genjiCardsSelectedDisplayText == null) { Debug.LogWarning("WordSelectionManager: genjiCardsSelectedDisplayText が未設定。"); }
        if (heishiCardsSelectedDisplayText == null) { Debug.LogWarning("WordSelectionManager: heishiCardsSelectedDisplayText が未設定。"); }

        return isValid;
    }

    public void OnCardClicked(WordCardUI clickedCard)
    {
        if (clickedCard == null || clickedCard.IsSelectedByTeam()) return;
        Debug.Log($"WordSelectionManager: 札「{clickedCard.GetCardNumber()}」クリック。選択中: {(currentlyHighlightedCard != null ? currentlyHighlightedCard.GetCardNumber() : "なし")}");

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

            // ★↓Undoのための情報を記録する (ターン交代の直前)↓★
            lastConfirmedCard = clickedCard;              // どの札が確定されたか
            lastConfirmedCardTeamIndex = currentPlayerTurnTeamIndex; // どのチームが確定したか
            previousGenjiPlayerSelectIndex = currentGenjiPlayerSelectIndex; // 確定した時の源氏の順番
            previousHeishiPlayerSelectIndex = currentHeishiPlayerSelectIndex; // 確定した時の平氏の順番

            // ★↓Undoのための情報を記録し、Undoボタンを押せるようにする↓★
            lastConfirmedCard = clickedCard;
            lastConfirmedCardTeamIndex = currentPlayerTurnTeamIndex;
            previousGenjiPlayerSelectIndex = currentGenjiPlayerSelectIndex;
            previousHeishiPlayerSelectIndex = currentHeishiPlayerSelectIndex;

            PlayerSetupManager psm = FindObjectOfType<PlayerSetupManager>();
            if (psm != null && psm.undoCardSelectionButton != null)
            {
                psm.undoCardSelectionButton.interactable = true; // ★Undoボタンを押せるようにする★
            }

            if (currentPlayerTurnTeamIndex == 0) cardsSelectedByGenji++;
            else cardsSelectedByHeishi++;
            Debug.Log($"{(currentPlayerTurnTeamIndex == 0 ? "源氏" : "平氏")}が札「{clickedCard.GetCardNumber()}」獲得。");
            UpdateCardsSelectedDisplay(); // ★追加：札獲得後に表示を更新★
            currentlyHighlightedCard = null;

            // ★↓ここから終了条件の判定を「合計90枚選択」のみにします↓★
            int totalSelectedCardsNow = cardsSelectedByGenji + cardsSelectedByHeishi;
            Debug.Log($"現在の合計選択枚数: {totalSelectedCardsNow}枚");

            // TOTAL_CARDS_TO_TRIGGER_LOCK はクラスの変数宣言部分で 
            // private const int TOTAL_CARDS_TO_TRIGGER_LOCK = 90; と定義されているはずです。
            if (totalSelectedCardsNow >= TOTAL_CARDS_TO_TRIGGER_LOCK) 
            {
                Debug.Log($"合計選択枚数が{TOTAL_CARDS_TO_TRIGGER_LOCK}枚に達しました。札選択フェイズ終了。残りの札をロックします。");
                SetAllUnselectedCardsInteractable(false); // 残りの札（空札のはず）をロック
                
                if (overallTurnTextDisplay != null) overallTurnTextDisplay.text = "札選択終了";
                if (currentSelectingPlayerNameDisplayText != null) currentSelectingPlayerNameDisplayText.text = "";
                
                // ここで次のフェイズ（クイズ解答フェイズ）へ移行する処理を呼び出す
                // GoToQuizPhase(); 
                return; // ターン交代は行わない
            }
            // ★↑ここまで終了条件の判定を修正↑★
            
            SwitchToNextPlayerAndTurn(); // まだ終了していなければターンを交代する
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
        if (WordDataManager.Instance == null)
        {
            Debug.LogError("SwitchToNextPlayerAndTurn: WordDataManagerが見つかりません！");
            return;
        }

        int genjiCount = WordDataManager.Instance.GenjiPlayerCount;
        int heishiCount = WordDataManager.Instance.HeishiPlayerCount;

        if (genjiCount <= 0 || heishiCount <= 0) // どちらかのチームの人数が0以下なら、ターン進行はしない
        {
            Debug.LogError("SwitchToNextPlayerAndTurn: チームの参加人数が不正です。源氏: " + genjiCount + ", 平氏: " + heishiCount);
            // 実際には、プレイヤー設定画面で最低1人を強制するべき
            return;
        }

        string previousPlayerName = GetCurrentPlayerName(); // ログ用に、今のプレイヤー名を覚えておく

        // 現在のターンが源氏軍（0）だった場合
        if (currentPlayerTurnTeamIndex == 0)
        {
            // 次は平氏軍のターンにする
            currentPlayerTurnTeamIndex = 1;
            // 平氏軍のプレイヤーインデックスは、現在のものを使う
            // （currentHeishiPlayerSelectIndex は、前回平氏軍が選んだ時の次の人を指しているはず、
            //  または、ゲーム開始時なら0になっているはず）
            // もし、平氏軍の現在のインデックスが参加人数以上になっていたら、0に戻す（一周したということ）
            if (currentHeishiPlayerSelectIndex >= heishiCount)
            {
                currentHeishiPlayerSelectIndex = 0;
            }
        }
        // 現在のターンが平氏軍（1）だった場合
        else
        {
            // 次は源氏軍のターンにする
            currentPlayerTurnTeamIndex = 0;
            // 源氏軍のプレイヤーインデックスを1つ進める
            currentGenjiPlayerSelectIndex++;
            // もし、源氏軍の現在のインデックスが参加人数以上になったら、0に戻す（一周したということ）
            if (currentGenjiPlayerSelectIndex >= genjiCount)
            {
                currentGenjiPlayerSelectIndex = 0;
            }

            // ★重要：平氏軍のターンが終わって、次に源氏軍のターンに移るので、
            // 　　　　 平氏軍のプレイヤーインデックスも、次の平氏軍のターンのために1つ進めておく
            currentHeishiPlayerSelectIndex++;
            if (currentHeishiPlayerSelectIndex >= heishiCount)
            {
                currentHeishiPlayerSelectIndex = 0; // 平氏軍も一周したら0に戻る
            }
        }

        // 画面の表示を更新する
        UpdateOverallTurnDisplay();
        UpdateCurrentSelectingPlayerNameDisplay();

        Debug.Log($"ターン交代。前: {previousPlayerName} さん。 次は {GetCurrentTeamNameForDisplay()} の {GetCurrentPlayerRole()}「{GetCurrentPlayerName()}」さんの番です。 GenjiIdx:{currentGenjiPlayerSelectIndex}, HeishiIdx:{currentHeishiPlayerSelectIndex}");
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
    void UpdateCardsSelectedDisplay()
    {
        if (genjiCardsSelectedDisplayText != null)
        {
            genjiCardsSelectedDisplayText.text = $"源氏軍：{cardsSelectedByGenji}枚";
            // genjiCardsSelectedDisplayText.color = genjiTeamColor; // 必要なら色もチームカラーに
        }
        if (heishiCardsSelectedDisplayText != null)
        {
            heishiCardsSelectedDisplayText.text = $"平氏軍：{cardsSelectedByHeishi}枚";
            // heishiCardsSelectedDisplayText.color = heishiTeamColor; // 必要なら色もチームカラーに
        }
        Debug.Log($"取得枚数表示を更新。源氏:{cardsSelectedByGenji}枚, 平氏:{cardsSelectedByHeishi}枚");
    }
    // 「一手戻す」ボタンが押された時のお仕事
    // 「一手戻す」ボタンが押された時に、PlayerSetupManagerなどから呼び出されるお仕事
    public void OnUndoButtonClicked()
    {
        if (lastConfirmedCard == null || lastConfirmedCardTeamIndex == -1)
        {
            Debug.LogWarning("WordSelectionManager: Undoできる操作がありません。");
            // ★↓ここでUndoボタンを無効化するのは、Undoできるものが無くなった時なので適切↓★
            PlayerSetupManager psm = FindObjectOfType<PlayerSetupManager>();
            if (psm != null && psm.undoCardSelectionButton != null)
            {
                psm.undoCardSelectionButton.interactable = false;
            }
            // ★↑ここまで↑★
            return;
        }

        Debug.Log($"★★ WordSelectionManager: Undo処理開始 - 札「{lastConfirmedCard.GetCardNumber()}」の選択を取り消します。★★");

        // 1. 直前に確定された札の WordCardUI を取得し、選択状態をリセットする
        lastConfirmedCard.ResetToUnselectedState(); // WordCardUIにリセットをお任せ (元の色、スケール、interactable=true になるはず)


        // 2. その札を獲得したチームの獲得枚数を1つ減らす
        if (lastConfirmedCardTeamIndex == 0) // 源氏軍が取った札だったら
        {
            if (cardsSelectedByGenji > 0) cardsSelectedByGenji--;
        }
        else // 平氏軍が取った札だったら
        {
            if (cardsSelectedByHeishi > 0) cardsSelectedByHeishi--;
        }
        UpdateCardsSelectedDisplay(); // 取得枚数表示を更新 (これも忘れずに！)

        // 3. ターンとプレイヤーのインデックスを、その札を選択した「前」の状態に戻す
        currentPlayerTurnTeamIndex = lastConfirmedCardTeamIndex; // 札を取ったチームのターンに戻す
        currentGenjiPlayerSelectIndex = previousGenjiPlayerSelectIndex; // 覚えておいたインデックスに戻す
        currentHeishiPlayerSelectIndex = previousHeishiPlayerSelectIndex; // 覚えておいたインデックスに戻す

        // 4. 画面上のターン表示と、現在選択中のプレイヤー名表示を更新する
        UpdateOverallTurnDisplay();
        UpdateCurrentSelectingPlayerNameDisplay();

        // 5. Undo情報をクリアし、次のUndoに備える (連続Undoを許容しないならここでUndoボタンを無効化)
        Debug.Log($"Undo処理: lastConfirmedCard ({lastConfirmedCard.GetCardNumber()}) の情報をクリアします。");
        lastConfirmedCard = null;
        lastConfirmedCardTeamIndex = -1;
        // ★↓Undo情報をクリアしたら、Undoボタンは押せないようにする↓★
        PlayerSetupManager psmAfterUndo = FindObjectOfType<PlayerSetupManager>(); // 再度取得
        if (psmAfterUndo != null && psmAfterUndo.undoCardSelectionButton != null)
        {
            psmAfterUndo.undoCardSelectionButton.interactable = false;
        }
        // ★↑ここまで↑★

        Debug.Log("★★ WordSelectionManager: Undo処理完了 ★★");
    }
    // まだチームに選択されていない全ての札の操作可否を設定するお仕事

    void SetAllUnselectedCardsInteractable(bool interactable)
    {
        if (cardGridParent == null)
        {
            Debug.LogError("SetAllUnselectedCardsInteractable: cardGridParentが未設定です！");
            return;
        }
        Debug.Log($"全ての未選択の札を {(interactable ? "選択可能" : "選択不可")} にします。");
        foreach (Transform cardTransform in cardGridParent)
        {
            WordCardUI card = cardTransform.GetComponent<WordCardUI>();
            if (card != null && !card.IsSelectedByTeam()) 
            {
                Button btn = card.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = interactable;
                }
            }
        }
    }
}
