// 职责：定义预研与新版基地战共享的舰队部署界面契约，避免复制部署和修缮规则。
// 入口：SiegeFleet / SiegeDeploymentView；会话持有经济、目录和生命周期。
using System;

public interface ISiegeDeploymentHost
{
    SiegeMatch Match
    {
        get;
    }

    SiegeSettings Settings
    {
        get;
    }

    SiegeFleet Fleet
    {
        get;
    }

    SiegeReturnFlight FlightGuidance
    {
        get;
    }

    SiegeShipOption[] DefenderOptions
    {
        get;
    }

    SiegeShipOption[] AttackerOptions
    {
        get;
    }

    bool IsEditingBlueprint
    {
        get;
    }

    bool IsRunning
    {
        get;
    }

    int SelectedDefender
    {
        get;
    }

    int SelectedAttacker
    {
        get;
    }

    event Action RefitChanged;
    void SelectDefender(int index);
    void SelectAttacker(int index);
    bool RefreshBlueprints();
    bool TryDeployAt(int optionIndex, int dock, out string reason);
    bool TryTakeControl(int slot);
    bool TryTakeControl(string id);
    bool TryRepairShip(string id, bool repairBase = false);
}
