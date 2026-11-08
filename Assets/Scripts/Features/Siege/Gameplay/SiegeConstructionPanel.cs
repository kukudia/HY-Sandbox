// 职责：显示新版基地部署与战斗状态，将输入转发给场景入口；控件由Editor保存。
// 入口：Bind()；不创建运行时Canvas或持有战斗规则。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class SiegeConstructionPanel : MonoBehaviour
{
    [SerializeField]
    private Dropdown designs;
    [SerializeField]
    private InputField x;
    [SerializeField]
    private InputField z;
    [SerializeField]
    private Button rotate;
    [SerializeField]
    private Button deploy;
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
    private int quarterTurns;
    private float nextRefresh;
    public void Bind(SiegeGameplayStage stage)
    {
        this.stage = stage;
        designs.ClearOptions();
        var names = new List<string>();
        foreach (string name in stage.DesignNames)
        {
            names.Add(name);
        }

        designs.AddOptions(names);
        designs.onValueChanged.AddListener(stage.SelectDesign);
        x.onEndEdit.AddListener(_ => ApplyPose());
        z.onEndEdit.AddListener(_ => ApplyPose());
        rotate.onClick.AddListener(() =>
        {
            quarterTurns = (quarterTurns + 1) % 4;
            ApplyPose();
        });
        deploy.onClick.AddListener(() =>
        {
            ApplyPose();
            stage.Deploy();
        });
        wave.onClick.AddListener(() => stage.BeginWave());
        repair.onClick.AddListener(() => stage.Repair());
    }

    private void ApplyPose()
    {
        if (!int.TryParse(x.text, out int px) || !int.TryParse(z.text, out int pz))
        {
            x.SetTextWithoutNotify(Mathf.RoundToInt(stage.Placement.x).ToString());
            z.SetTextWithoutNotify(Mathf.RoundToInt(stage.Placement.z).ToString());
            return;
        }

        stage.SetPose(px, pz, quarterTurns);
    }

    private void Update()
    {
        if (stage == null || Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + .2f;
        bool choosing = stage.Base == null && !stage.Settled;
        designs.interactable = choosing;
        x.interactable = z.interactable = rotate.interactable = choosing;
        rotate.GetComponentInChildren<Text>().text = $"ROTATE {quarterTurns * 90}°";
        deploy.interactable = choosing && stage.CanDeploy;
        wave.interactable = stage.Base != null && !stage.Settled && stage.Match.CanRefit;
        repair.interactable = wave.interactable;
        long quote = stage.Base != null ? stage.Base.Quote() : 0;
        repair.GetComponentInChildren<Text>().text = $"REPAIR + REBUILD  {quote}";
        details.text = stage.DesignStatus;
        summary.text = $"{stage.Match.Phase}  |  WAVE {stage.Match.Wave}\nCoins {stage.Match.Coins}  |  Raiders {stage.Match.ActiveAttackers} + {stage.Match.PendingAttackers}\nDestroyed {stage.Match.DefeatedAttackers}";
        feedback.text = stage.Status;
        returnLabel.text = stage.Base != null && !stage.Settled ? "RETREAT / SETTLEMENT" : "BACK TO BUILD";
    }
}
