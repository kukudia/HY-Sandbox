// 职责：在显式Player烟测中验证输入推进、炮弹击毁与机器人回收入仓。
// 入口：Run()；只修改本进程临时夹具，迭代器释放时恢复输入设备与镜头。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

internal static class WorldCombatPlayerChecks
{
    internal static IEnumerator Run(ControlUnit player, EnemySpawner spawner, Action<bool, string> check)
    {
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        Camera camera = Camera.main;
        CameraController controller = camera.GetComponent<CameraController>();
        bool cameraEnabled = controller.enabled;
        Quaternion cameraRotation = camera.transform.rotation;
        ControlUnit enemy = null;
        var fixtures = new List<GameObject>();
        try
        {
            controller.enabled = false;
            camera.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            Rigidbody body = player.GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            MainThruster thruster = Spawn("MainThruster", player, new Vector3(0f, 0f, -3f), fixtures).GetComponent<MainThruster>();
            Spawn("PowerGeneratingUnit", player, new Vector3(2f, 0f, -2f), fixtures);
            Spawn("PowerTransmissionDevice", player, new Vector3(-2f, 0f, -2f), fixtures);
            CargoHold hold = Spawn("CargoHold", player, new Vector3(3f, 0f, 0f), fixtures).GetComponent<CargoHold>();
            CollectionBot bot = Spawn("CollectionBotContainer", player, new Vector3(-3f, 0f, 0f), fixtures).GetComponentInChildren<CollectionBot>();
            ProjectileTurret turret = Spawn("ProjectileTurretSingle", player, new Vector3(0f, 0f, 3f), fixtures).GetComponent<ProjectileTurret>();
            turret.SetStarted(false);
            hold.RestoreContents(new List<CargoItem>());
            player.RefreshChildren();
            player.AssignRuntimeOwnershipToBlocks(true);
            body.mass = player.GetComponentsInChildren<Block>().Sum(block => block.mass);
            Vector3 start = body.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            bool sawInput = false;
            bool sawThrust = false;
            float until = Time.time + 2f;
            while (Time.time < until)
            {
                sawInput |= player.MovementInput.z > .9f;
                sawThrust |= thruster.thrust > 1f && thruster.GetComponent<Power>().isWorking;
                yield return null;
            }

            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            check(sawInput, "Input System W reaches the enabled player ControlUnit");
            check(
                sawThrust && body.position.z > start.z + .1f,
                "real powered thruster advances dynamic Player through PhysX");
            yield return null;
            yield return null;
            check(player.MovementInput.sqrMagnitude < .001f, "released keyboard clears player movement intent");
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.constraints = RigidbodyConstraints.FreezeAll;
            // Stable combat platform isolates weapon/recovery from flight tuning; movement above was dynamic.
            BlockDataList blueprint = EnemyCargoFixture();
            enemy = spawner.SpawnBlockData(
                blueprint,
                "Explicit cargo target",
                player.transform.position + Vector3.forward * 15f,
                Quaternion.identity);
            check(enemy != null, "production spawner restores a cargo-bearing hostile unit");
            enemy.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            enemy.GetComponent<EnemyController>().enabled = false;
            CargoHold hostileHold = enemy.GetComponentInChildren<CargoHold>();
            check(hostileHold.Used == 1, "hostile cargo originates in blueprint contents");
            Durability cockpitHealth = enemy.cockpit.GetComponent<Durability>();
            float initialHealth = cockpitHealth.currentDurability;
            turret.SetStarted(true);
            bool sawShell = false;
            bool sawDamage = false;
            until = Time.time + 45f;
            while (cockpitHealth != null && cockpitHealth.enabled && Time.time < until)
            {
                sawShell |= Object.FindObjectsByType<TurretProjectile>(FindObjectsSortMode.None).Length > 0;
                sawDamage |= cockpitHealth.currentDurability < initialHealth;
                yield return null;
            }

            check(
                sawShell && sawDamage,
                "automatic powered turret launches shells and damages hostile cockpit across frames");
            check(
                cockpitHealth == null || !cockpitHealth.enabled,
                "projectile damage destroys hostile cockpit through production destruction");
            turret.SetStarted(false);
            bool sawDrop = false;
            bool sawCarry = false;
            until = Time.time + 85f;
            while (hold.Used == 0 && Time.time < until)
            {
                sawDrop |= LootDrop.Active.Any(drop => drop != null && drop.Item.kind == CargoKind.SpecialPart && drop.Item.resourcePath == "Blocks/1x1x1");
                sawCarry |= bot.HasCargo;
                yield return null;
            }

            check(sawDrop, "hostile destruction releases authored special cargo into real world");
            check(sawCarry, "collection robot physically picks up wreck cargo");
            check(
                hold.Contents.Any(item => item.kind == CargoKind.SpecialPart && item.resourcePath == "Blocks/1x1x1"),
                "robot returns and deposits wreck cargo in powered player hold");
            check(bot.transform.parent == bot.home, "delivery completes only after actual home docking");
            check(!turret.GetComponent<Power>().IsWorkRequested, "stopped combat turret releases power demand");
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            controller.enabled = cameraEnabled;
            camera.transform.rotation = cameraRotation;
            if (enemy != null)
            {
                Object.Destroy(enemy.gameObject);
            }

            foreach (GameObject fixture in fixtures)
            {
                if (fixture != null)
                {
                    Object.Destroy(fixture);
                }
            }
        }
    }

    private static GameObject Spawn(string resource, ControlUnit owner, Vector3 local, List<GameObject> fixtures)
    {
        GameObject obj = Object.Instantiate(Resources.Load<GameObject>("Blocks/" + resource), owner.transform);
        obj.transform.localPosition = local;
        obj.transform.localRotation = Quaternion.identity;
        fixtures.Add(obj);
        return obj;
    }

    private static BlockDataList EnemyCargoFixture()
    {
        var result = new BlockDataList();
        foreach (string resource in new[]
        {
            "Cockpit",
            "CargoHold"
        }

        )
        {
            Block block = Resources.Load<GameObject>("Blocks/" + resource).GetComponent<Block>();
            var data = new BlockData(block)
            {
                resourcePath = "Blocks/" + resource,
                posX = 0f,
                posY = 0f,
                posZ = resource == "Cockpit" ? 0f : 3f,
                rotX = 0f,
                rotY = 0f,
                rotZ = 0f,
                rotW = 1f
            };
            if (resource == "CargoHold")
            {
                data.cargo = new List<CargoItem>
                {
                    new CargoItem
                    {
                        kind = CargoKind.SpecialPart,
                        resourcePath = "Blocks/1x1x1",
                        amount = 1
                    }
                };
            }

            result.blocks.Add(data);
        }

        return result;
    }
}
