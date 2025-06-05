using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class PlayerSetupManager : MonoBehaviour
{
    [Header("この画面のCanvas")]
    public GameObject playerSetupCanvasObject;

    [Header("遷移先のCanvas")]
    public GameObject dataLoadingCanvasObject;
    public GameObject cardSelectionCanvasObject;

    [Header("UIボタン")]
    public Button goToCardSelectButton;
    public Button returnToDataLoadButton;
    public Button undoCardSelectionButton; // ★追加★ 札選択画面で使う「一手戻す」ボタン


    [Header("参加人数選択UI")]
    public TMP_Dropdown genjiPlayerCountDropdown;
    public TMP_Dropdown heishiPlayerCountDropdown;

    [Header("上限解答数入力UI")]
    public TMP_InputField genjiMaxAnswersInput;
    public TMP_InputField heishiMaxAnswersInput;

    [Header("源氏軍プレイヤーUIリスト")]
    public List<TMP_InputField> genjiPlayerNameInputs;
    public List<TextMeshProUGUI> genjiPlayerRoleTexts;

    [Header("平氏軍プレイヤーUIリスト")]
    public List<TMP_InputField> heishiPlayerNameInputs;
    public List<TextMeshProUGUI> heishiPlayerRoleTexts;

    [Header("役職名")]
    public string leaderRoleName = "総大将";
    public string memberRoleName = "武将";

    private int maxGenjiPlayersUI;
    private int maxHeishiPlayersUI;

    [Header("札選択画面への移行動画演出（インスペクターで設定）")]
    public GameObject playerSetupTransitionVideoRoot; // 動画の親GameObject
    public UnityEngine.Video.VideoPlayer playerSetupTransitionVideoPlayer; // 動画再生用のVideoPlayer
    public UnityEngine.Video.VideoClip playerSetupTransitionVideoClip; // 再生する動画クリップ

    void Start()
    {
        Debug.Log("PlayerSetupManager: Start() が呼び出されました。");
        ValidateInspectorReferences_PSM();

        if (goToCardSelectButton != null) goToCardSelectButton.onClick.AddListener(OnGoToCardSelectButtonClicked);
        if (returnToDataLoadButton != null) returnToDataLoadButton.onClick.AddListener(OnReturnToDataLoadButtonClicked);

        maxGenjiPlayersUI = (genjiPlayerNameInputs != null) ? genjiPlayerNameInputs.Count : 0;
        maxHeishiPlayersUI = (heishiPlayerNameInputs != null) ? heishiPlayerNameInputs.Count : 0;

        if (genjiPlayerCountDropdown != null)
        {
            SetupPlayerCountDropdown(genjiPlayerCountDropdown, maxGenjiPlayersUI);
            genjiPlayerCountDropdown.onValueChanged.AddListener(delegate { UpdatePlayerUIBasedOnDropdown(); });
        }
        if (heishiPlayerCountDropdown != null)
        {
            SetupPlayerCountDropdown(heishiPlayerCountDropdown, maxHeishiPlayersUI);
            heishiPlayerCountDropdown.onValueChanged.AddListener(delegate { UpdatePlayerUIBasedOnDropdown(); });
        }
        UpdatePlayerUIBasedOnDropdown();

        if (undoCardSelectionButton != null)
        {
            undoCardSelectionButton.onClick.AddListener(OnUndoCardSelectionButtonClicked);
            //undoCardSelectionButton.gameObject.SetActive(false); // 最初は非表示、または押せないように
            undoCardSelectionButton.interactable = false;      // ← interactableの初期設定はここで行う

        }
    }

    void ValidateInspectorReferences_PSM()
    {
        if (playerSetupCanvasObject == null) Debug.LogError("PSM: playerSetupCanvasObjectが未設定！");
        if (dataLoadingCanvasObject == null) Debug.LogError("PSM: dataLoadingCanvasObjectが未設定！");
        if (cardSelectionCanvasObject == null) Debug.LogError("PSM: cardSelectionCanvasObjectが未設定！");
        if (goToCardSelectButton == null) Debug.LogError("PSM: goToCardSelectButtonが未設定！");
        if (returnToDataLoadButton == null) Debug.LogError("PSM: returnToDataLoadButtonが未設定！");
        if (genjiPlayerCountDropdown == null) Debug.LogError("PSM: genjiPlayerCountDropdownが未設定！");
        if (heishiPlayerCountDropdown == null) Debug.LogError("PSM: heishiPlayerCountDropdownが未設定！");
        if (genjiMaxAnswersInput == null) Debug.LogError("PSM: genjiMaxAnswersInputが未設定！");
        if (heishiMaxAnswersInput == null) Debug.LogError("PSM: heishiMaxAnswersInputが未設定！");
        if (genjiPlayerNameInputs == null || genjiPlayerNameInputs.Count == 0) Debug.LogError("PSM: genjiPlayerNameInputsが未設定または空！");
        if (heishiPlayerNameInputs == null || heishiPlayerNameInputs.Count == 0) Debug.LogError("PSM: heishiPlayerNameInputsが未設定または空！");
        if (genjiPlayerRoleTexts == null || genjiPlayerRoleTexts.Count == 0) Debug.LogWarning("PSM: genjiPlayerRoleTextsが未設定または空（役職表示なし）。");
        if (heishiPlayerRoleTexts == null || heishiPlayerRoleTexts.Count == 0) Debug.LogWarning("PSM: heishiPlayerRoleTextsが未設定または空（役職表示なし）。");
    }

    void SetupPlayerCountDropdown(TMP_Dropdown dropdown, int maxCountUI)
    {
        if (dropdown == null || maxCountUI <= 0) return;
        dropdown.ClearOptions();
        List<string> options = new List<string>();
        for (int i = 1; i <= maxCountUI; i++) { options.Add(i.ToString() + "人"); }
        dropdown.AddOptions(options);
        if (options.Count > 0) { dropdown.value = options.Count - 1; dropdown.RefreshShownValue(); }
    }

    // ★★★↓この関数が、ドロップダウンの値が変わった時に呼ばれ、UIを更新します↓★★★
    public void UpdatePlayerUIBasedOnDropdown() // public に変更して DataLoadController からも呼べるように
    {
        int genjiTargetCount = maxGenjiPlayersUI;
        if (genjiPlayerCountDropdown != null && genjiPlayerCountDropdown.options.Count > 0)
        {
            string selectedText = genjiPlayerCountDropdown.options[genjiPlayerCountDropdown.value].text;
            int.TryParse(selectedText.Replace("人", ""), out genjiTargetCount);
            genjiTargetCount = Mathf.Clamp(genjiTargetCount, 1, maxGenjiPlayersUI);
        }

        int heishiTargetCount = maxHeishiPlayersUI;
        if (heishiPlayerCountDropdown != null && heishiPlayerCountDropdown.options.Count > 0)
        {
            string selectedText = heishiPlayerCountDropdown.options[heishiPlayerCountDropdown.value].text;
            int.TryParse(selectedText.Replace("人", ""), out heishiTargetCount);
            heishiTargetCount = Mathf.Clamp(heishiTargetCount, 1, maxHeishiPlayersUI);
        }

        Debug.Log($"UI更新指示。源氏ターゲット: {genjiTargetCount}人, 平氏ターゲット: {heishiTargetCount}人");
        UpdateTeamUIVisibility("源氏軍", genjiTargetCount, genjiPlayerNameInputs, genjiPlayerRoleTexts);
        UpdateTeamUIVisibility("平氏軍", heishiTargetCount, heishiPlayerNameInputs, heishiPlayerRoleTexts);
    }
    // ★★★↑ここまで↑★★★

    // ★★★↓この関数が、実際のUIの表示/非表示と役職テキストの設定を行います↓★★★
    void UpdateTeamUIVisibility(string teamNameForLog, int targetCount, List<TMP_InputField> nameInputs, List<TextMeshProUGUI> roleTexts)
    {
        if (nameInputs != null)
        {
            for (int i = 0; i < nameInputs.Count; i++)
            {
                if (nameInputs[i] != null) nameInputs[i].gameObject.SetActive(i < targetCount);
            }
        }
        if (roleTexts != null)
        {
            for (int i = 0; i < roleTexts.Count; i++)
            {
                if (roleTexts[i] != null)
                {
                    bool shouldBeActive = (i < targetCount);
                    roleTexts[i].gameObject.SetActive(shouldBeActive);
                    if (shouldBeActive)
                    {
                        roleTexts[i].text = (i == 0) ? leaderRoleName : memberRoleName;
                        if (i != 0 && string.IsNullOrEmpty(memberRoleName)) roleTexts[i].gameObject.SetActive(false);
                    }
                }
            }
        }
        Debug.Log($"{teamNameForLog} の名前入力欄と役職表示を {targetCount}人分更新。");
    }
    // ★★★↑ここまで↑★★★

    public void OnGoToCardSelectButtonClicked()
    {
        Debug.Log("「札選択へ進む」ボタンが押されました。");
        int genjiCount = (genjiPlayerCountDropdown != null && genjiPlayerCountDropdown.options.Count > 0) ? (genjiPlayerCountDropdown.value + 1) : 1;
        int heishiCount = (heishiPlayerCountDropdown != null && heishiPlayerCountDropdown.options.Count > 0) ? (heishiPlayerCountDropdown.value + 1) : 1;
        List<string> genjiNames = GetPlayerNamesFromInputs(genjiPlayerNameInputs, genjiCount);
        List<string> heishiNames = GetPlayerNamesFromInputs(heishiPlayerNameInputs, heishiCount);
        int genjiMaxAns = 4; if (genjiMaxAnswersInput != null && !string.IsNullOrEmpty(genjiMaxAnswersInput.text) && int.TryParse(genjiMaxAnswersInput.text, out int pG)) genjiMaxAns = Mathf.Max(1, pG);
        int heishiMaxAns = 4; if (heishiMaxAnswersInput != null && !string.IsNullOrEmpty(heishiMaxAnswersInput.text) && int.TryParse(heishiMaxAnswersInput.text, out int pH)) heishiMaxAns = Mathf.Max(1, pH);

        if (WordDataManager.Instance != null) { WordDataManager.Instance.SetTeamSettings(genjiCount, genjiNames, genjiMaxAns, heishiCount, heishiNames, heishiMaxAns); }
        else { Debug.LogError("PSM: WordDataManagerが見つかりません！"); return; }

        // ボタンを非活性化 (任意)
        if (goToCardSelectButton != null)
        {
            goToCardSelectButton.interactable = false;
        }

        // 動画再生処理を開始
        if (playerSetupTransitionVideoRoot != null && playerSetupTransitionVideoPlayer != null && playerSetupTransitionVideoClip != null)
        {
            Debug.Log("[PlayerSetupManager] プレイヤー設定後動画の再生準備を開始します。");
            StartCoroutine(PlayPlayerSetupTransitionVideoAndProceed());
        }
        else
        {
            Debug.LogError("[PlayerSetupManager] プレイヤー設定後動画の再生に必要な参照が設定されていません。動画をスキップして札選択画面へ進みます。");
            ProceedToCardSelectionScreen();
        }
    }

    List<string> GetPlayerNamesFromInputs(List<TMP_InputField> inputFields, int countToGet) { List<string> names = new List<string>(); if (inputFields == null) return names; for (int i = 0; i < Mathf.Min(countToGet, inputFields.Count); i++) { if (inputFields[i] != null && !string.IsNullOrEmpty(inputFields[i].text)) { names.Add(inputFields[i].text); } else { names.Add($"プレイヤー{i + 1}"); } } return names; }
    public void OnReturnToDataLoadButtonClicked()
    {
        if (playerSetupCanvasObject != null) playerSetupCanvasObject.SetActive(false);
        if (dataLoadingCanvasObject != null) dataLoadingCanvasObject.SetActive(true);
        if (undoCardSelectionButton != null) // ★追加★
        {
            undoCardSelectionButton.gameObject.SetActive(false);
        }
    }
    // 「一手戻す」ボタンが押された時のお仕事
    public void OnUndoCardSelectionButtonClicked()
    {
        WordSelectionManager wordSelector = FindObjectOfType<WordSelectionManager>();
        if (wordSelector != null)
        {
            Debug.Log("PlayerSetupManager: WordSelectionManagerのUndo処理を呼び出します。");
            wordSelector.OnUndoButtonClicked(); // WordSelectionManagerのUndo関数を呼び出す

        }
        else
        {
            Debug.LogError("PlayerSetupManager: WordSelectionManagerが見つかりません！Undo処理を実行できません。");
        }
    }

    private IEnumerator PlayPlayerSetupTransitionVideoAndProceed()
    {
        Debug.Log("[PlayerSetupManager] プレイヤー設定後動画の再生を開始します。(コルーチン開始)");

        // 0. まず、動画が表示されるべきCanvas（playerSetupCanvasObject）がアクティブであることを確認
        //    (このCanvas自体は、動画再生中も表示したままで良いかもしれません。動画がその上に重なるので。)
        //    今回は、playerSetupCanvasObjectはアクティブのまま、その上に動画UIが表示されると仮定します。

        // 1. 次に、動画表示用の親GameObject (playerSetupTransitionVideoRoot) の有効化
        if (playerSetupTransitionVideoRoot == null)
        {
            Debug.LogError("[PlayerSetupManager] playerSetupTransitionVideoRoot が null です！動画再生できません。");
            ProceedToCardSelectionScreen(); // 動画なしで直接次へ
            yield break;
        }
        if (!playerSetupTransitionVideoRoot.activeSelf)
        {
            playerSetupTransitionVideoRoot.SetActive(true);
            Debug.Log("[PlayerSetupManager] playerSetupTransitionVideoRoot をアクティブにしました。");
        }
        yield return null; // 親のアクティブ化を1フレーム待つ

        // 2. VideoPlayerとそのGameObjectがアクティブか確認し、必要ならアクティブにする
        if (playerSetupTransitionVideoPlayer == null)
        {
            Debug.LogError("[PlayerSetupManager] playerSetupTransitionVideoPlayer参照がnullです！動画再生できません。");
            if (playerSetupTransitionVideoRoot != null) playerSetupTransitionVideoRoot.SetActive(false);
            ProceedToCardSelectionScreen();
            yield break;
        }
        // (VideoPlayerのGameObjectやコンポーネントの有効化チェックは、前回同様にここに入れてもOKです)
        if (!playerSetupTransitionVideoPlayer.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[動画準備] {playerSetupTransitionVideoPlayer.gameObject.name} が階層的に非アクティブだったので強制的にアクティブ化試行");
            playerSetupTransitionVideoPlayer.gameObject.SetActive(true); yield return null;
        }
        if (!playerSetupTransitionVideoPlayer.enabled)
        {
            playerSetupTransitionVideoPlayer.enabled = true; yield return null;
        }

        // 3. VideoPlayerの設定と再生準備
        if (playerSetupTransitionVideoClip == null)
        {
            Debug.LogError("[PlayerSetupManager] playerSetupTransitionVideoClip が null です！動画再生できません。");
            if (playerSetupTransitionVideoRoot != null) playerSetupTransitionVideoRoot.SetActive(false);
            ProceedToCardSelectionScreen();
            yield break;
        }

        if (playerSetupTransitionVideoPlayer.gameObject.activeInHierarchy && playerSetupTransitionVideoPlayer.enabled)
        {
            playerSetupTransitionVideoPlayer.clip = playerSetupTransitionVideoClip;
            playerSetupTransitionVideoPlayer.isLooping = false;

            playerSetupTransitionVideoPlayer.loopPointReached -= OnPlayerSetupTransitionVideoEnd;
            playerSetupTransitionVideoPlayer.loopPointReached += OnPlayerSetupTransitionVideoEnd;

            playerSetupTransitionVideoPlayer.Prepare();
            Debug.Log("[PlayerSetupManager] VideoPlayer.Prepare() を呼び出しました。");

            float prepareWaitStartTime = Time.time;
            while (!playerSetupTransitionVideoPlayer.isPrepared)
            {
                if (Time.time - prepareWaitStartTime > 7f)
                {
                    Debug.LogError("[PlayerSetupManager] VideoPlayer.Prepare() が7秒以上完了しませんでした。動画再生を中止します。");
                    if (playerSetupTransitionVideoRoot != null) playerSetupTransitionVideoRoot.SetActive(false);
                    ProceedToCardSelectionScreen();
                    yield break;
                }
                yield return null;
            }
            Debug.Log("[PlayerSetupManager] プレイヤー設定後動画の準備完了。再生開始。");
            playerSetupTransitionVideoPlayer.Play();
        }
        else
        {
            Debug.LogError($"[PlayerSetupManager] Prepare() を呼び出す直前でVideoPlayerが無効な状態です。動画再生を中止します。");
            if (playerSetupTransitionVideoRoot != null) playerSetupTransitionVideoRoot.SetActive(false);
            ProceedToCardSelectionScreen();
        }
    }

    private void OnPlayerSetupTransitionVideoEnd(UnityEngine.Video.VideoPlayer vp)
    {
        Debug.Log("[PlayerSetupManager] プレイヤー設定後動画の再生が終了しました。(イベント)");

        if (vp != null)
        {
            vp.loopPointReached -= OnPlayerSetupTransitionVideoEnd;
        }

        if (playerSetupTransitionVideoRoot != null)
        {
            playerSetupTransitionVideoRoot.SetActive(false);
            Debug.Log("[PlayerSetupManager] playerSetupTransitionVideoRoot を非アクティブにしました。(動画終了時)");
        }

        ProceedToCardSelectionScreen();
    }

    private void ProceedToCardSelectionScreen()
    {
        Debug.Log("[PlayerSetupManager] 札選択画面へ移行します。");

        // このプレイヤー設定画面を非表示にする
        if (playerSetupCanvasObject != null)
        {
            playerSetupCanvasObject.SetActive(false);
            Debug.Log($"[PlayerSetupManager] {playerSetupCanvasObject.name} を非表示にしました。");
        }
        // (else節はエラーログなのでそのまま)

        // 次に表示する札選択画面のCanvasを表示する
        if (cardSelectionCanvasObject != null)
        {
            cardSelectionCanvasObject.SetActive(true);
            Debug.Log($"[PlayerSetupManager] 次の画面として {cardSelectionCanvasObject.name} を表示しました。");

            // WordSelectionManager の初期化処理を呼び出す
            WordSelectionManager wordSelector = FindObjectOfType<WordSelectionManager>();
            if (wordSelector != null)
            {
                wordSelector.InitializeAndDisplayCards();
                Debug.Log("[PlayerSetupManager] WordSelectionManagerのInitializeAndDisplayCardsを呼び出しました。");
            }
            // (else節はエラーログなのでそのまま)
        }
        // (else節はエラーログなのでそのまま)

        // Undoボタンの表示制御 (これはWordSelectionManagerに任せるので、ここでは不要かも)
        // if (undoCardSelectionButton != null) 
        // {
        //     undoCardSelectionButton.gameObject.SetActive(true); 
        // }
    }
}
