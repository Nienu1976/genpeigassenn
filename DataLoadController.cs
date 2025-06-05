using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;


public class DataLoadController : MonoBehaviour


{
    [Header("UI参照（インスペクターで設定）")]
    public TMP_InputField selectableWordsPathInput;
    public TMP_InputField masterQuizDataPathInput;
    public Button loadDataButton;
    public TextMeshProUGUI loadStatusText;
    public Button proceedToNextScreenButton;

    [Header("次に表示するCanvas（インスペクターで設定）")]
    public GameObject dataLoadingCanvasObject;
    public GameObject nextScreenCanvasObject; // PlayerSetupCanvasへの参照

    private bool selectableWordsFileSuccessfullyLoaded = false;
    private bool masterQuizDataFileSuccessfullyLoaded = false;

    // ★↓ここからドロップダウンの参照を追加↓★
    public TMP_Dropdown genjiPlayerCountDropdown_DataLoad; // データ読み込み画面の源氏軍人数ドロップダウン
    public TMP_Dropdown heishiPlayerCountDropdown_DataLoad; // データ読み込み画面の平氏軍人数ドロップダウン

    [Header("デバッグ用固定パス設定")]
    public bool useFixedPathsForData = true; // ★trueにしておくと、常に固定パスを使用する★
    public string fixedSelectCsvPath = "/Users/nienu/Desktop/genpei/select.csv";
    public string fixedQuestionCsvPath = "/Users/nienu/Desktop/genpei/Question.csv";

    // おそらく、パス入力用のInputFieldの参照も既にお持ちかと思います
    // public TMP_InputField selectCsvPathInputField; // 仮の名前
    // public TMP_InputField questionCsvPathInputField; // 仮の名前

    [Header("データロード後動画演出（インスペクターで設定）")]
    public GameObject dataLoadTransitionVideoRoot; // 動画の親GameObject
    public UnityEngine.Video.VideoPlayer dataLoadTransitionVideoPlayer; // 動画再生用のVideoPlayer
    public UnityEngine.Video.VideoClip dataLoadTransitionVideoClip; // 再生する動画クリップ


    void Start()
    {
        Debug.Log("DataLoadController: Start() が呼び出されました。");
        if (WordDataManager.Instance == null)
        {
            Debug.LogError("DataLoadController: WordDataManagerのインスタンスが見つかりません！");
            SetUIInteractable(false);
            if (loadStatusText != null) loadStatusText.text = "エラー: データ管理システム準備不足。";
            return;
        }
        ValidateInspectorReferences_DLC();
        if (loadDataButton != null) loadDataButton.onClick.AddListener(OnLoadDataButtonClicked);
        if (proceedToNextScreenButton != null)
        {
            proceedToNextScreenButton.onClick.AddListener(OnProceedToNextScreenButtonClicked);
            proceedToNextScreenButton.interactable = false;
        }
        if (loadStatusText != null) loadStatusText.text = "各CSVファイルのフルパスを入力し、「データ読み込み」を押してください。";

        if (genjiPlayerCountDropdown_DataLoad != null)
        {
            // genjiPlayerCountDropdown_DataLoad.onValueChanged.AddListener(delegate { OnPlayerCountDropdownChanged(); });
        }
        if (heishiPlayerCountDropdown_DataLoad != null)
        {
            // heishiPlayerCountDropdown_DataLoad.onValueChanged.AddListener(delegate { OnPlayerCountDropdownChanged(); });
        }
        // ...
    }

    void ValidateInspectorReferences_DLC()
    {
        if (selectableWordsPathInput == null) Debug.LogError("DataLoadController: selectableWordsPathInputが未設定！");
        if (masterQuizDataPathInput == null) Debug.LogError("DataLoadController: masterQuizDataPathInputが未設定！");
        if (loadDataButton == null) Debug.LogError("DataLoadController: loadDataButtonが未設定！");
        if (proceedToNextScreenButton == null) Debug.LogError("DataLoadController: proceedToNextScreenButtonが未設定！");
        if (dataLoadingCanvasObject == null) Debug.LogError("DataLoadController: dataLoadingCanvasObjectが未設定！");
        if (nextScreenCanvasObject == null) Debug.LogError("DataLoadController: nextScreenCanvasObjectが未設定！");
    }

    void SetUIInteractable(bool interactable)
    {
        if (selectableWordsPathInput != null) selectableWordsPathInput.interactable = interactable;
        if (masterQuizDataPathInput != null) masterQuizDataPathInput.interactable = interactable;
        if (loadDataButton != null) loadDataButton.interactable = interactable;
    }

    void OnLoadDataButtonClicked()
    {
        if (WordDataManager.Instance == null)
        {
            UpdateLoadStatus("エラー: データ管理システム準備不足。", false);
            return;
        }

        string selectablePathToUse; // 実際に使う選択ワードCSVのパス
        string masterPathToUse;     // 実際に使う問題データCSVのパス

        if (useFixedPathsForData) // 固定パスを使用するかどうかのフラグ (public bool useFixedPathsForData = true; と宣言済みのはずです)
        {
            selectablePathToUse = fixedSelectCsvPath;   // 固定パスを使用
            masterPathToUse = fixedQuestionCsvPath;     // 固定パスを使用
            Debug.LogWarning($"[データロード] 固定パスを使用します。\nSelectableWords: {selectablePathToUse}\nMasterQuizData: {masterPathToUse}");

            // InputFieldを操作不可にする（任意ですが、固定パス使用中であることを分かりやすくするため）
            if (selectableWordsPathInput != null) selectableWordsPathInput.interactable = false;
            if (masterQuizDataPathInput != null) masterQuizDataPathInput.interactable = false;
        }
        else // InputFieldからのパスを使用する場合
        {
            if (selectableWordsPathInput == null || masterQuizDataPathInput == null)
            {
                UpdateLoadStatus("エラー: パス入力フィールドがInspectorで設定されていません。", false);
                return;
            }
            selectablePathToUse = selectableWordsPathInput.text.Trim();
            masterPathToUse = masterQuizDataPathInput.text.Trim();

            if (string.IsNullOrWhiteSpace(selectablePathToUse) || string.IsNullOrWhiteSpace(masterPathToUse))
            {
                UpdateLoadStatus("エラー: 両方のCSVパスを指定してください。", false);
                return;
            }
            Debug.Log($"[データロード] InputFieldのパスを使用します。\nSelectableWords: {selectablePathToUse}\nMasterQuizData: {masterPathToUse}");
        }

        UpdateLoadStatus("データを読み込み中...", false);
        SetUIInteractable(false); // データロード中はUI操作を一時的に不可に

        // selectablePathToUse と masterPathToUse を使ってデータをロード
        WordDataManager.LoadResult resultSelectable = WordDataManager.Instance.LoadSelectableWordsFromCSV(selectablePathToUse);
        selectableWordsFileSuccessfullyLoaded = (resultSelectable == WordDataManager.LoadResult.Success);
        string statusMsg = selectableWordsFileSuccessfullyLoaded ? $"ワードリスト成功 ({WordDataManager.Instance.SelectableWordsList.Count}件)" : $"ワードリスト失敗: {resultSelectable} ({System.IO.Path.GetFileName(selectablePathToUse)})";

        if (selectableWordsFileSuccessfullyLoaded)
        {
            WordDataManager.LoadResult resultMaster = WordDataManager.Instance.LoadMasterQuizDataFromCSV(masterPathToUse, true);
            masterQuizDataFileSuccessfullyLoaded = (resultMaster == WordDataManager.LoadResult.Success);
            statusMsg += masterQuizDataFileSuccessfullyLoaded ? $"\n問題データ成功 ({WordDataManager.Instance.MasterQuizDataList.Count}件)" : $"\n問題データ失敗: {resultMaster} ({System.IO.Path.GetFileName(masterPathToUse)})";
        }
        else
        {
            masterQuizDataFileSuccessfullyLoaded = false;
            statusMsg += "\n問題データは読み込みませんでした。";
        }

        bool bothLoaded = selectableWordsFileSuccessfullyLoaded && masterQuizDataFileSuccessfullyLoaded;
        statusMsg += bothLoaded ? "\n\n読み込み完了！「次へ進む」を押してください。" : "\nファイルパスやCSV内容を確認し再試行してください。";
        UpdateLoadStatus(statusMsg, bothLoaded);
        SetUIInteractable(true); // UI操作を再度有効に
    }

    void UpdateLoadStatus(string message, bool allowProceed)
    {
        if (loadStatusText != null) loadStatusText.text = message;
        if (proceedToNextScreenButton != null) proceedToNextScreenButton.interactable = allowProceed;
    }
public void OnProceedToNextScreenButtonClicked()
{
    // まず、データが正しく読み込まれているかを確認
    if (selectableWordsFileSuccessfullyLoaded && masterQuizDataFileSuccessfullyLoaded)
    {
        Debug.Log("[DataLoadController] 「次へ進む」ボタンが押されました。データ読み込み成功済み。");

        // WordDataManagerに人数を保存 (これは既存の処理でOKです)
        if (WordDataManager.Instance != null)
        {
            if (genjiPlayerCountDropdown_DataLoad != null && genjiPlayerCountDropdown_DataLoad.options.Count > 0)
            {
                WordDataManager.Instance.GenjiPlayerCount = genjiPlayerCountDropdown_DataLoad.value + 1;
            }
            if (heishiPlayerCountDropdown_DataLoad != null && heishiPlayerCountDropdown_DataLoad.options.Count > 0)
            {
                WordDataManager.Instance.HeishiPlayerCount = heishiPlayerCountDropdown_DataLoad.value + 1;
            }
            Debug.Log($"[DataLoadController] 参加人数をWordDataManagerに保存: 源氏={WordDataManager.Instance.GenjiPlayerCount}人, 平氏={WordDataManager.Instance.HeishiPlayerCount}人");
        }
        else
        {
            Debug.LogError("[DataLoadController] WordDataManagerのインスタンスが見つかりません。参加人数を保存できません。");
            // エラーなので、ここで処理を中断することも検討
        }

        // ボタンを非活性化 (任意ですが、二度押し防止などに有効)
        if (proceedToNextScreenButton != null)
        {
            proceedToNextScreenButton.interactable = false;
        }

        // 動画再生処理を開始 (もし動画関連の参照が全て設定されていれば)
        if (dataLoadTransitionVideoRoot != null && dataLoadTransitionVideoPlayer != null && dataLoadTransitionVideoClip != null)
        {
            Debug.Log("[DataLoadController] データロード後動画の再生準備を開始します。");
            StartCoroutine(PlayDataLoadTransitionVideoAndProceed()); // ★動画再生コルーチンを開始★
        }
        else // 動画関連の参照が一つでも欠けていたら、動画をスキップして直接次の処理へ
        {
            Debug.LogError("[DataLoadController] データロード後動画の再生に必要な参照が設定されていません。動画をスキップしてプレイヤー設定画面へ進みます。");
            ProceedToPlayerSetupScreen(); // ★動画なしで直接プレイヤー設定画面へ★
        }
    }
    else // データが読み込まれていなければ
    {
        UpdateLoadStatus("エラー: データが読み込まれていません。「データ読み込み」を再試行してください。", false);
        Debug.LogError("[DataLoadController] データ未読み込みのため画面遷移できません。");
    }
}
    private IEnumerator PlayDataLoadTransitionVideoAndProceed()
    {
        Debug.Log("[DataLoadController] データロード後動画の再生を開始します。(コルーチン開始)");

        // 0. まず、動画が表示されるべきCanvas（dataLoadingCanvasObject）がアクティブであることを確認
        //    (このCanvas自体は、動画再生中も表示したままで良いかもしれません。動画がその上に重なるので。)
        //    あるいは、動画再生中はDataLoadCanvasの主要UIを隠し、動画だけを見せる形も考えられます。
        //    今回は、DataLoadCanvasはアクティブのまま、その上に動画UIが表示されると仮定します。

        // 1. 次に、動画表示用の親GameObject (dataLoadTransitionVideoRoot) の有効化
        if (dataLoadTransitionVideoRoot == null)
        {
            Debug.LogError("[DataLoadController] dataLoadTransitionVideoRoot が null です！動画再生できません。");
            ProceedToPlayerSetupScreen(); // 動画なしで直接次へ
            yield break;
        }
        if (!dataLoadTransitionVideoRoot.activeSelf)
        {
            dataLoadTransitionVideoRoot.SetActive(true);
            Debug.Log("[DataLoadController] dataLoadTransitionVideoRoot をアクティブにしました。");
        }
        yield return null; // 親のアクティブ化を1フレーム待つ

        // 2. VideoPlayerとそのGameObjectがアクティブか確認し、必要ならアクティブにする
        if (dataLoadTransitionVideoPlayer == null)
        {
            Debug.LogError("[DataLoadController] dataLoadTransitionVideoPlayer参照がnullです！動画再生できません。");
            if (dataLoadTransitionVideoRoot != null) dataLoadTransitionVideoRoot.SetActive(false);
            ProceedToPlayerSetupScreen();
            yield break;
        }
        // (VideoPlayerのGameObjectやコンポーネントの有効化チェックは、前回同様にここに入れてもOKです)
        // if (!dataLoadTransitionVideoPlayer.gameObject.activeInHierarchy) { ... dataLoadTransitionVideoPlayer.gameObject.SetActive(true); yield return null; ... }
        // if (!dataLoadTransitionVideoPlayer.enabled) { ... dataLoadTransitionVideoPlayer.enabled = true; yield return null; ... }

        // 3. VideoPlayerの設定と再生準備
        if (dataLoadTransitionVideoClip == null)
        {
            Debug.LogError("[DataLoadController] dataLoadTransitionVideoClip が null です！動画再生できません。");
            if (dataLoadTransitionVideoRoot != null) dataLoadTransitionVideoRoot.SetActive(false);
            ProceedToPlayerSetupScreen();
            yield break;
        }

        // Prepare()前に VideoPlayer が有効か最終チェック
        if (dataLoadTransitionVideoPlayer.gameObject.activeInHierarchy && dataLoadTransitionVideoPlayer.enabled)
        {
            dataLoadTransitionVideoPlayer.clip = dataLoadTransitionVideoClip;
            dataLoadTransitionVideoPlayer.isLooping = false;

            dataLoadTransitionVideoPlayer.loopPointReached -= OnDataLoadTransitionVideoEnd;
            dataLoadTransitionVideoPlayer.loopPointReached += OnDataLoadTransitionVideoEnd;

            dataLoadTransitionVideoPlayer.Prepare();
            Debug.Log("[DataLoadController] VideoPlayer.Prepare() を呼び出しました。");

            float prepareWaitStartTime = Time.time;
            while (!dataLoadTransitionVideoPlayer.isPrepared)
            {
                if (Time.time - prepareWaitStartTime > 7f)
                {
                    Debug.LogError("[DataLoadController] VideoPlayer.Prepare() が7秒以上完了しませんでした。動画再生を中止します。");
                    if (dataLoadTransitionVideoRoot != null) dataLoadTransitionVideoRoot.SetActive(false);
                    ProceedToPlayerSetupScreen();
                    yield break;
                }
                yield return null;
            }
            Debug.Log("[DataLoadController] データロード後動画の準備完了。再生開始。");
            dataLoadTransitionVideoPlayer.Play();
        }
        else
        {
            Debug.LogError($"[DataLoadController] Prepare() を呼び出す直前でVideoPlayerが無効な状態です。動画再生を中止します。");
            if (dataLoadTransitionVideoRoot != null) dataLoadTransitionVideoRoot.SetActive(false);
            ProceedToPlayerSetupScreen();
        }
    }

    private void ProceedToPlayerSetupScreen()
    {
        Debug.Log("[DataLoadController] プレイヤー設定画面へ移行します。");

        // データ読み込み画面(dataLoadingCanvasObject)を非表示にする
        if (dataLoadingCanvasObject != null)
        {
            dataLoadingCanvasObject.SetActive(false);
            Debug.Log($"[DataLoadController] {dataLoadingCanvasObject.name} を非表示にしました。");
        }
        // (else節はエラーログなのでそのまま)

        // 次に表示するプレイヤー設定画面のCanvas(nextScreenCanvasObject)を表示する
        if (nextScreenCanvasObject != null) // nextScreenCanvasObject は PlayerSetupCanvas への参照
        {
            nextScreenCanvasObject.SetActive(true);
            Debug.Log($"[DataLoadController] 次の画面として {nextScreenCanvasObject.name} を表示しました。");

            // PlayerSetupManager の UI 更新をここで明示的に呼ぶ
            PlayerSetupManager playerSetupMgr = nextScreenCanvasObject.GetComponent<PlayerSetupManager>();
            if (playerSetupMgr != null)
            {
                playerSetupMgr.UpdatePlayerUIBasedOnDropdown();
                Debug.Log("[DataLoadController] PlayerSetupManagerのUI更新を呼び出しました。");
            }
            // (else節はエラーログなのでそのまま)
        }
        // (else節はエラーログなのでそのまま)
    }

    private void OnDataLoadTransitionVideoEnd(UnityEngine.Video.VideoPlayer vp)
    {
        Debug.Log("[DataLoadController] データロード後動画の再生が終了しました。(イベント)");

        if (vp != null)
        {
            vp.loopPointReached -= OnDataLoadTransitionVideoEnd;
        }

        if (dataLoadTransitionVideoRoot != null)
        {
            dataLoadTransitionVideoRoot.SetActive(false);
            Debug.Log("[DataLoadController] dataLoadTransitionVideoRoot を非アクティブにしました。(動画終了時)");
        }

        ProceedToPlayerSetupScreen();
    }

}
