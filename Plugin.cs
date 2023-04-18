#pragma warning disable 0612
#pragma warning disable 0618
#pragma warning disable 0619

using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;
using Comfort.Common;
using EFT;
using System.Collections.Generic;
using System;
using System.Linq;
using TMPro;

using Object = UnityEngine.Object;

namespace FloatingHealthbars
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class FHPlugin : BaseUnityPlugin
    {
        private static GameObject _hookObject;
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            _hookObject = new GameObject("HealthbarCore");
            DontDestroyOnLoad(_hookObject);

            new ExecuteOnRaidStartPatch().Enable();
        }

        public static void Execute(Player _player)
        {
            _hookObject.AddComponent<HealthbarCore>();
        }
    }

    public class HealthbarCore : MonoBehaviour
    {
        public Object healthbarUIPrefab;
        public Object dmgNumberUIPrefab;

        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("HealthbarCore");

        public void Start()
        {
            logger.LogInfo("HealthbarCore began execution.");

            // Handle bot spawn Action.
            var botGame = Singleton<AbstractGame>.Instance as IBotGame;
            botGame.BotsController.BotSpawner.OnBotCreated += HandleNewSpawn;

            // Load healthbar assets.
            string filepath = BepInEx.Paths.PluginPath + "/fhbars/" + "floatinghealthbars";
            var bundle = AssetBundle.LoadFromFile(filepath);
            healthbarUIPrefab = bundle.LoadAsset("Assets/FloatingHealthbars/HealthbarCanvas.prefab");
            dmgNumberUIPrefab = bundle.LoadAsset("Assets/FloatingHealthbars/Dmg.prefab");
        }

        private void HandleNewSpawn(BotOwner owner)
        {
            logger.LogInfo("New spawn.");

            var healthbar = Instantiate(healthbarUIPrefab) as GameObject;
            var tracker = healthbar.AddComponent<HealthbarTracker>();
            var dmgInfo = Instantiate(dmgNumberUIPrefab) as GameObject;

            tracker.owner = owner;
            tracker.healthbar = healthbar;
            tracker.dmgInfo = dmgInfo;
        }
    }

    public class HealthbarTracker : MonoBehaviour
    {
        private static Player LocalPlayerSingleton() => Singleton<GameWorld>.Instance.RegisteredPlayers.Find(p => p.IsYourPlayer);
        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("HealthbarTracker");

        public float offset = 2.0f;
        private float maxHealth = 0.0f;

        public BotOwner owner;
        public GameObject healthbar;
        public Canvas healthbarCanvas;

        private Image healthbarImage;
        private TextMeshPro textComponent;

        public GameObject dmgInfo;

        private float _previousDamageCache = -1;

        private static float NormalizeHealth(float min, float max, float curr) => (curr - min) / (max - min) * 1;

        public void Start()
        {
            logger.LogInfo("New HealthbarTracker instantiated, a bot should've been spawned.");
            maxHealth = MaxHealth();

            healthbarCanvas = healthbar.GetComponent<Canvas>();
            healthbarImage = healthbarCanvas.GetComponentsInChildren<Image>().Single(c => c.name == "Healthbar");
            textComponent = healthbarCanvas.GetComponentInChildren<TextMeshPro>();
        }

        public void Update()
        {
            // Not in world (impossible... probably).
            if (LocalPlayerSingleton() == null)
            {
                logger.LogInfo("Not in world.");
                return;
            }

            // Object destroyed (this is the parent object so I don't think this can be called).
            if (healthbar == null)
            {
                logger.LogInfo("Healthbar was either destroyed or is null.");
                return;
            }

            if (_previousDamageCache < 0)
            {
                _previousDamageCache = CurrentHealth();
            }

            // Update healthbar info.
            healthbar.transform.LookAt(LocalPlayerSingleton().Transform.position);
            healthbar.transform.position = new Vector3(this.owner.Transform.position.x, this.owner.Transform.position.y + offset, this.owner.Transform.position.z);
            healthbarImage.fillAmount = NormalizeHealth(0, maxHealth, CurrentHealth());
            // textComponent.text = Convert.ToInt32(CurrentHealth()).ToString();

            // Play damage anim if bot was damaged.
            // if (CurrentHealth() < _previousDamageCache && CurrentHealth() > 1 && !owner.IsDead)
            // {
            //     try { dmgInfo.GetComponentInChildren<TextMeshPro>().text = (_previousDamageCache - CurrentHealth()).ToString(); } catch { logger.LogError("Can't get TMP.");  }
            //     try { dmgInfo.GetComponentInChildren<Animation>().Play(); } catch { logger.LogError("Can't play anim.");  }
            //     _previousDamageCache = CurrentHealth();
            // }
            
            // Bot died.
            if (CurrentHealth() < 1 || owner.IsDead)
            {
                logger.LogInfo("Destroying healthbar for dead bot: " + owner.name);
                healthbar.SetActive(false);
                Destroy(healthbar);
            }
        }

        private float CurrentHealth()
        {
            float health = 0;

            foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
            {
                health += owner.HealthController.GetBodyPartHealth(bodyPart).Current;
            }

            return health;
        }

        private float MaxHealth()
        {
            float health = 0;

            foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
            {
                health += owner.HealthController.GetBodyPartHealth(bodyPart).Maximum;
            }

            return health;
        }
    }
}
