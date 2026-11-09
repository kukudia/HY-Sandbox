// 职责：显示新版基地战斗状态，将输入转发给场景入口；控件由Editor保存。
// 入口：Bind()；不创建运行时Canvas或持有战斗规则。
using UnityEngine;
using UnityEngine.UI;

public sealed class SiegeConstructionPanel : MonoBehaviour
{
    [SerializeField]
    private Button wave;
    [SerializeField]
    private Button repair;
    [SerializeField]
    private Text details;
    [SerializeField]
    private Text summary;
    [SerializeField]
    private Text feedback;
    [SerializeField]
    private Text returnLabel;
    private SiegeGameplayStage stage;
    private float nextRefresh;
    public void Bind(SiegeGameplayStage stage)
    {
        this.stage = stage;
        wave.onClick.AddListener(() => stage.BeginWave());
        repair.onClick.AddListener(() => stage.Repair());
    }

    private void Update()
    {
        if (stage == null || Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + .2f;
        wave.interactable = stage.Base != null && !stage.Settled && stage.Match.CanRefit;
        repair.interactable = wave.interactable;
        long quote = stage.Base != null ? stage.Base.Quote() : 0;
        repair.GetComponentInChildren<Text>().text = $"REPAIR + REBUILD  {quote}";
        details.text = stage.DesignStatus;
        summary.text = $"{stage.Match.Phase}  |  WAVE {stage.Match.Wave}\nCREDITS {stage.Match.Coins:N0}  |  +{stage.Match.PendingCoins:N0} PENDING\nRAIDERS {stage.Match.ActiveAttackers}+{stage.Match.PendingAttackers}  |  KILLS {stage.Match.DefeatedAttackers}";
        feedback.text = stage.Status;
        returnLabel.text = stage.Base != null && !stage.Settled ? "RETREAT / SETTLEMENT" : "BACK TO BUILD";
    }
}
