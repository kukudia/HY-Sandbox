// 职责：显式Player隔离文件仓，验证正常返航写盘、重载、死亡清货与再次出发。
// 入口：Run()；固定两模块夹具，仅写唯一缓存目录，finally恢复原文件仓及名称。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

internal static class WorldReturnPlayerChecks
{
    internal static IEnumerator Run(Action<bool, string> check)
    {
        SaveManager saves = SaveManager.instance;
        BuildManager build = BuildManager.instance;
        WorldSessionManager world = PlayManager.instance.WorldSession;
        FieldInfo files = typeof(SaveManager).GetField("_playerFiles", BindingFlags.Instance | BindingFlags.NonPublic);
        object originalFiles = files.GetValue(saves);
        string originalName = saves.currentSaveName;
        string originalBuildName = build.currentSaveName;
        BlockDataList originalData = saves.cachedData;
        List<string> originalNames = saves.saves;
        string directory = Path.Combine(Application.temporaryCachePath, "CodexReturn", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            // Reflection is limited to this opt-in fixture; production has no writable storage override API.
            files.SetValue(saves, new BlueprintFileStore(directory));
            saves.saves = new List<string>();
            const string name = "return-route";
            string path = saves.GetSavePath(name);
            var blueprint = new BlockDataList();
            foreach (string module in new[]
            {
                "Cockpit",
                "CargoHold"
            }

            )
            {
                var block = new BlockData(Resources.Load<GameObject>("Blocks/" + module).GetComponent<Block>())
                {
                    id = "return-route-" + module,
                    resourcePath = "Blocks/" + module,
                    posX = module == "Cockpit" ? 0f : 2f,
                    posY = 0f,
                    posZ = 0f,
                    rotX = 0f,
                    rotY = 0f,
                    rotZ = 0f,
                    rotW = 1f,
                    cargo = new List<CargoItem>()
                };
                blueprint.blocks.Add(block);
            }

            File.WriteAllText(path, JsonUtility.ToJson(blueprint, true));
            saves.LoadSave(name);
            while (build.IsLoadingBlocks || world.State == WorldSessionState.Preparing)
            {
                yield return null;
            }

            MainUIPanels.instance.PlayStart();
            while (!PlayManager.instance.playMode || world.IsEntering)
            {
                yield return null;
            }

            ControlUnit player = GameManager.instance.blocksParent.GetComponent<ControlUnit>();
            player.GetComponent<Rigidbody>().isKinematic = true;
            CargoHold hold = player.GetComponentInChildren<CargoHold>();
            check(
                hold != null && hold.Owner == player && player.HasValidCockpit,
                "return fixture loads connected cockpit and cargo hold through production loader");
            hold.RestoreContents(new List<CargoItem> { new CargoItem { kind = CargoKind.SpecialPart, resourcePath = "Blocks/UniversalThruster", amount = 1 } });
            MainUIPanels.instance.PlayEnd();
            yield return null;
            check(
                !PlayManager.instance.playMode && world.State == WorldSessionState.Preparing,
                "normal UI return saves before ending actual world session");
            while (build.IsLoadingBlocks || world.State == WorldSessionState.Preparing)
            {
                yield return null;
            }

            BlockDataList returned = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
            BlockData returnedHold = returned.blocks.Single(block => block.id == "return-route-CargoHold");
            check(
                returnedHold.cargo.Count == 1 && returnedHold.cargo[0].resourcePath == "Blocks/UniversalThruster",
                "normal return persists cargo into isolated player blueprint");
            check(
                File.Exists(path + ".cargo.bak") && !File.Exists(path + ".cargo.tmp"),
                "normal return completes atomic replace and backup in Player");
            hold = GameManager.instance.blocksParent.GetComponentInChildren<CargoHold>();
            check(
                world.State == WorldSessionState.Ready && hold != null && hold.Used == 1,
                "build reload restores returned cargo before next departure");
            MainUIPanels.instance.PlayStart();
            while (!PlayManager.instance.playMode || world.IsEntering)
            {
                yield return null;
            }

            player = GameManager.instance.blocksParent.GetComponent<ControlUnit>();
            player.GetComponent<Rigidbody>().isKinematic = true;
            check(
                player.GetComponentInChildren<CargoHold>().Used == 1,
                "second departure retains exactly one returned part");
            DestroyManager.instance.DestroyGameObject(player.cockpit.gameObject);
            yield return null;
            check(
                !PlayManager.instance.playMode && MainUIButtons.instance.respawnButton.interactable,
                "real cockpit destruction opens usable death respawn action");
            MainUIButtons.instance.respawnButton.onClick.Invoke();
            yield return null;
            while (build.IsLoadingBlocks || world.State == WorldSessionState.Preparing)
            {
                yield return null;
            }

            BlockDataList dead = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
            check(
                dead.blocks.Count == 2 && dead.blocks.All(block => block.cargo == null || block.cargo.Count == 0),
                "death respawn clears saved cargo while preserving original modules");
            hold = GameManager.instance.blocksParent.GetComponentInChildren<CargoHold>();
            check(
                world.State == WorldSessionState.Ready && hold != null && hold.Used == 0,
                "death reload restores empty cargo in ready build session");
            MainUIPanels.instance.PlayStart();
            while (!PlayManager.instance.playMode || world.IsEntering)
            {
                yield return null;
            }

            check(world.State == WorldSessionState.Playing, "third departure starts after death and respawn");
            GameManager.instance.blocksParent.GetComponent<Rigidbody>().isKinematic = true;
            MainUIPanels.instance.PlayEnd();
            yield return null;
            while (build.IsLoadingBlocks || world.State == WorldSessionState.Preparing)
            {
                yield return null;
            }

            check(
                world.State == WorldSessionState.Ready,
                "post-death departure returns safely to ready build session");
        }
        finally
        {
            files.SetValue(saves, originalFiles);
            saves.saves = originalNames;
            saves.currentSaveName = originalName;
            saves.cachedData = originalData;
            build.SetCurrentSaveName(originalBuildName);
        }

        saves.LoadSave(originalName);
        while (build.IsLoadingBlocks || world.State == WorldSessionState.Preparing)
        {
            yield return null;
        }

        check(
            world.State == WorldSessionState.Ready,
            "isolated return route restores original read-only save and ready world");
    }
}
