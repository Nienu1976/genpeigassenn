using UnityEngine;

public class QuizPlayer // MonoBehaviourはまだ付けません（データを持つだけのクラスなので）
{
    public string PlayerName { get; private set; }
    public string TeamInitial { get; private set; } // "A" (源氏) または "B" (平氏)
    public bool IsLeader { get; private set; }
    public int CorrectAnswers { get; private set; } // このプレイヤーの正解数
    public int IncorrectAnswers { get; private set; } // このプレイヤーの誤答数
    public int MaxAnswersAllowed { get; private set; } // このプレイヤーの上限解答数
    public bool CanAnswer { get; set; } // 現在、このプレイヤーが解答権を持っているか

    // コンストラクタ（QuizPlayerを作るときに呼ばれるお仕事）
    public QuizPlayer(string name, string team, bool isLeader, int maxAnswers)
    {
        PlayerName = name;
        TeamInitial = team;
        IsLeader = isLeader;
        CorrectAnswers = 0;
        IncorrectAnswers = 0;
        MaxAnswersAllowed = maxAnswers;
        CanAnswer = true; // 最初は解答権あり
        HasReachedMaxAnswers = false; // ★最初は上限に達していないので false に設定★

    }
    public bool HasReachedMaxAnswers { get; private set; } // ★上限に達したかどうかの印（フラグ）を追加★


    // 正解した時のお仕事
    public void IncrementCorrectAnswers()
    {
        CorrectAnswers++;
        //Debug.Log($"{TeamInitial}チームの{PlayerName}さんが正解！ 現在の正解数: {CorrectAnswers}");
        // ここで、もし CorrectAnswers が MaxAnswersAllowed に達したら CanAnswer = false; にするロジックも将来追加
        // ★↓上限に達したかチェックし、達していたら印をつけ、解答権もなくす処理を追加↓★
        if (CorrectAnswers >= MaxAnswersAllowed)
        {
            HasReachedMaxAnswers = true; // 上限に達した印を true にする
            CanAnswer = false;          // これ以上解答できないようにする
            //Debug.LogWarning($"{TeamInitial}チームの{PlayerName}さんは、上限解答数 {MaxAnswersAllowed} 問に到達しました！「完走」です！");
        }
        // ★↑ここまで追加↑★
    }


    // 誤答した時のお仕事 (今回はまだ使いませんが、将来のために)
    public void IncrementIncorrectAnswers()
    {
        IncorrectAnswers++;
        //Debug.Log($"{TeamInitial}チームの{PlayerName}さんが誤答。 現在の誤答数: {IncorrectAnswers}");
        // ここで、お手つきのペナルティ処理などを将来追加
    }
        // ★↓覚えておいた状態（スナップショット）からプレイヤーの状態を復元する関数を追加します↓★
    public void RestoreStateFromSnapshot(PlayerStateSnapshot snapshot)
    {
        this.CorrectAnswers = snapshot.CorrectAnswers;
        this.IncorrectAnswers = snapshot.IncorrectAnswers;
        this.CanAnswer = snapshot.CanAnswer;
        this.HasReachedMaxAnswers = snapshot.HasReachedMaxAnswers;

        //Debug.LogWarning($"{this.PlayerName}の状態がスナップショットから復元されました。" +
        //$"正解数: {this.CorrectAnswers}, 誤答数: {this.IncorrectAnswers}, 解答権: {this.CanAnswer}, 完走: {this.HasReachedMaxAnswers}");
    }
    // ★↑ここまで追加↑★
}
