using System;
using GlobalEnums;
using Modding;
using System.Reflection;
using UnityEngine;
using HutongGames.PlayMaker;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BlackmothMod
{
    public class Blackmoth : Mod
    {
        public static Blackmoth Instance;

        public override string GetVersion() => "1.7.2";

        public override void Initialize()
        {

            Instance = this;
            Instance.Log("Blackmoth initializing!");

            ModHooks.Instance.HeroUpdateHook += Update;
            ModHooks.Instance.NewGameHook += GetReferences;
            ModHooks.Instance.AfterSavegameLoadHook += GetReferences;
            ModHooks.Instance.DashVectorHook += CalculateDashVelocity;
            ModHooks.Instance.DashPressedHook += CheckForDash;
            ModHooks.Instance.LanguageGetHook += Descriptions;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += ResetPosition;
            ModHooks.Instance.HitInstanceHook += SetDamages;

            // Init dictionaries to stop nullRef.
            InitializeDictionaries();
            Instance.Log("Blackmoth initialized!");
        }

        private void InitializeDictionaries()
        {
            LogDebug("Initializing dictionaries.");
            privateFields = new Dictionary<string, FieldInfo>();
            privateMethods = new Dictionary<string, MethodInfo>();
            FlavorDictionary = new Dictionary<KeyValuePair<int, string>, Dictionary<string, string>>();
            Dictionary<string, string> ptbrUIDictionary = new Dictionary<string, string>
            {
                ["CHARM_DESC_13"] = @"Dado livremente pela Tribo dos Louva-deuses àqueles dignos de respeito.

Aumenta consideravelmente o alcance da esquiva do portador, permitindo-o atacar inimigos mais distantes.",

                ["CHARM_DESC_15"] = @"Formado de mantos de guerreiros caídos.
                            
Aumenta a força da esquiva do portador, fazendo inimigos recuarem mais quando atacados.",

                ["CHARM_DESC_16"] = @"Contém um feitiço proibido que transforma matéria em vazio.

Enquanto usa a esquiva, o corpo do portador poderá ir através de objetos sólidos.",

                ["CHARM_DESC_18"] = @"Aumenta o alcance da esquiva do portador, permitindo-o atacar inimigos mais de longe.",

                ["CHARM_DESC_25"] = @"Fortalece o portador, aumentando o dano que ele causa aos inimigos com sua esquiva.

Esse amuleto é frágil e quebrará se o portador for morto",

                ["CHARM_DESC_25_G"] = @"Fortalece o portador, aumentando o dano que ele causa nos inimigos com sua esquiva.

Esse amuleto é inquebrável.",

                ["CHARM_DESC_31"] = $@"Tem a semelhança de um inseto excêntrico conhecido apenas como {"O Mestre da Esquiva"}.

O portador será capaz de se esquivar mais, bem como esquivar-se em pleno ar. Perfeito para aqueles que querem se mover o mais rápido possível.",

                ["CHARM_DESC_32"] = @"Nascido de mantos descartados e imperfeitos que se uniram. Esses mantos ainda anseiam ser usados.

Permite o portador a golpear muito mais rapidamente com sua esquiva.",

                ["CHARM_DESC_35"] = @"Contém a gratidão de larvas que vão se mover para o próximo estágio de suas vidas. Confere uma força sagrada.

Permite ao portador ascender e se tornar o lendário Grubbermoth e voar pelos céus.",

                ["CHARM_NAME_16"] = @"Sombra do Vazio",

                ["CHARM_NAME_18"] = @"Esquiva Longa",

                ["CHARM_NAME_32"] = @"Esquiva Veloz",

                ["CHARM_NAME_35"] = @"Elegia do Grubbermoth.",

                ["INV_DESC_DASH"] = @"Manto feito com asas de mariposa. Permite ao usuário a esquivar-se em todas as direções.",

                ["INV_DESC_SHADOWDASH"] = @"Capa formada da substância do Abismo, o progenitor do Blackmoth. Permite ao usuário a ganhar ALMA e dar o dobro do dano enquanto se esquiva.",

                ["INV_DESC_SUPERDASH"] = PlayerData.instance.GetBool("defeatedNightmareGrimm") ? @"O núcleo de energia de um velho golem minerador enfeitado de um potente cristal. A energia pode ser usada para lançar o portador pra frente em velocidades perigosas.

Com o Pesadelo derrotado, o cristal revelou seu verdadeiro potencial, conferindo ao portador mais flexibilidade." : @"O núcleo de energia de um velho golem minerador enfeitado de um potente cristal. A energia pode ser usada para lançar o portador pra frente em velocidades perigosas.

Mesmo que seja bem poderoso, parece que um Pesadelo o impede de revelar seu verdadeiro potencial...",

                ["INV_NAME_SHADOWDASH"] = @"Manto de Blackmoth",
            };
            Dictionary<string, string> enUIDictionary = new Dictionary<string, string>
            {
                ["CHARM_DESC_13"] = @"Freely given by the Mantis Tribe to those they respect.

Greatly increases the range of the bearer's dash, allowing them to strike foes from further away.",

                ["CHARM_DESC_15"] = @"Formed from the cloaks of fallen warriors.
                            
Increases the force of the bearer's dash, causing enemies to recoil further when hit.",

                ["CHARM_DESC_16"] = @"Contains a forbidden spell that transforms matter into void.

When dashing, the bearer's body will be able to go through solid objects.",

                ["CHARM_DESC_18"] =
                    @"Strengthens the bearer, increasing the damage they deal to enemies with their dash.

This charm is fragile, and will break if its bearer is killed.",

                ["CHARM_DESC_25_G"] =
                    @"Strengthens the bearer, increasing the damage they deal to enemies with their dash.

This charm is ubreakable.",

                ["CHARM_DESC_31"] = $@"Bears the likeness of an eccentric bug known only as {"The Dashmaster"}.

The bearer will be able to dash more often as well as dash in midair. Perfect for those who want to move around as quickly as possible.",

                ["CHARM_DESC_32"] =
                    @"Born from imperfect, discarded cloaks that have fused together. The cloaks still long to be worn.

Allows the bearer to slash much more rapidly with their dash.",

                ["CHARM_DESC_35"] =
                    @"Contains the gratitude of grubs who will move to the next stage of their lives. Imbues weapons with a holy strength.

Allows bearer to ascend and become the legendary Grubbermoth and fly away to the heavens.",

                ["CHARM_NAME_16"] = @"Void Shade",

                ["CHARM_NAME_18"] = @"Longdash",

                ["CHARM_NAME_32"] = @"Quick Dash",

                ["CHARM_NAME_35"] = @"Grubbermoth's Elegy",

                ["INV_DESC_DASH"] =
                    @"Cloak threaded with mothwing strands. Allows the wearer to dash in every direction.",

                ["INV_DESC_SHADOWDASH"] =
                    @"Cloak formed from the substance of the Abyss, the progenitor of the Blackmoth. Allows the wearer to gain SOUL and deal double damage while dashing.",

                ["INV_DESC_SUPERDASH"] = PlayerData.instance.GetBool("defeatedNightmareGrimm")
                    ? @"The energy core of an old mining golem, fashioned around a potent crystal. The crystal's energy can be channeled to launch the bearer forward at dangerous speeds.

With the Nightmare defeated, the crystal has revealed its true potential, granting its wielder more flexibility."
                    : @"The energy core of an old mining golem, fashioned around a potent crystal. The crystal's energy can be channeled to launch the bearer forward at dangerous speeds.

Even though it's quite powerful, it seems as if a Nightmare is preventing it from unleashing its true potential...",

                ["INV_NAME_SHADOWDASH"] = @"Blackmoth Cloak"
            };
            Dictionary<string, string> rusUIDictionary = new Dictionary<string, string>
            {
                ["CHARM_DESC_13"] = @"Племя богомолов награждает подобными амулетами тех, кого они уважают.

Весьма ощутимо увеличивает дальность рывка, позволяя достать противников с большого расстояния.",

                ["CHARM_DESC_15"] = @"Создан из плащей поверженных воинов.
            
Увеличивает мощь рывка носителя, позволяя отбрасывать пораженных врагов назад с каждым ударом.",

                ["CHARM_DESC_16"] = @"Заключает в себе запретное заклятье, превращающее носителя в пустоту.

Во время рывка носитель может проходить сквозь твердые объекты.",

                ["CHARM_DESC_18"] =
    @"УДает носителю больше силы, увеличивая урон, наносимый рывком.

Этот амулет очень хрупок и сломается при смерти владельца.",

                ["CHARM_DESC_25_G"] =
    @"Дает носителю больше силы, увеличивая урон, наносимый рывком.

Этот амулет нельзя сломать.",

                ["CHARM_DESC_31"] = $@"Очень похож на чудаковатого жука, известного лишь как  {"Трюкач"}.

Носитель может чаще передвигаться рывками и даже делать рывок в воздухе. Отлично подходит для тех, кто хочет передвигаться быстрее.
",

                ["CHARM_DESC_32"] =
    @"Создан из неблаговидных и забракованных плащей, сотканных воедино. Они все еще жаждут обрести хозяев.

Позволяет чаще наносить удары рывком.",

                ["CHARM_DESC_35"] =
    @"Содержит в себе благодарность всех гусеничек, которым вы помогли перейти на следующую стадию жизни. Наполняет ваше оружие священной мощью.

Позволяет носителю вознестись и стать легендарным Гусельком, и улететь в рай.",

                ["CHARM_NAME_16"] = @"Тень Пустоты",

                ["CHARM_NAME_18"] = @"Длинный рывок",

                ["CHARM_NAME_32"] = @"Быстрый рывок",

                ["CHARM_NAME_35"] = @"Элегия Гуселька",

                ["INV_DESC_DASH"] =
    @"Плащ с мотыльковой прострочкой. Позволяет носителю делать рывок в любую сторону.",

                ["INV_DESC_SHADOWDASH"] =
    @"Плащ, сделанный из субстанции Бездны, прародителя Черного Мотылька. Позволяет носителю получать ДУШУ и наносить двойной урон при рывке.",

                ["INV_DESC_SUPERDASH"] = PlayerData.instance.GetBool("defeatedNightmareGrimm")
    ? @"Силовое ядро старого шахтерского голема, в которое вделан крепкий кристалл. Сила кристалла позволяет носящему нестись вперед с умопомрачительной скоростью.

После уничтожения Кошмара кристалл раскрыл свой полный потенциал и дает носителю больше ловкости."
    : @"Силовое ядро старого шахтерского голема, в которое вделан крепкий кристалл. Сила кристалла позволяет носящему нестись вперед с умопомрачительной скоростью.

Несмотря на то, что кристалл довольно силен, Кошмар до сих пор не дает раскрыть его полный потенциал.",

                ["INV_NAME_SHADOWDASH"] = @"Мантия Черного Мотылька"
            };
            Dictionary<string, string> ptbrPromptDictionary = new Dictionary<string, string>
            {
                ["NAILSMITH_UPGRADE_1"] = "Pagar Geo para fortalecer a esquiva?",

                ["NAILSMITH_UPGRADE_2"] = "Dar Minério Pálido e Geo para fortalecer a esquiva?",

                ["NAILSMITH_UPGRADE_3"] = "Dar doi Minérios Pálido e Geo para fortalecer a esquiva?",

                ["NAILSMITH_UPGRADE_4"] = "Dar trê Minérios Pálido e Geo para fortalecer a esquiva?",

                ["GET_SHADOWDASH_2"] = "Use o manto para atravessar as costuras na realidade e pegar ALMA do espaço intermediário.",

                ["GET_SHADOWDASH_1"] = "para se esquivar enquanto coleta ALMA do ambiente.",

                ["GET_DASH_2"] = "Use o manto para esquivar-se rapidamente através do ar em todas as direções.",

                ["GET_DASH_1"] = "e qualquer direção para esquivar-se naquela direção."
            };
            Dictionary<string, string> enPromptDictionary = new Dictionary<string, string>
            {
                ["NAILSMITH_UPGRADE_1"] = "Pay Geo to strengthen dash?",

                ["NAILSMITH_UPGRADE_2"] = "Give Pale Ore and Geo to strengthen dash?",

                ["NAILSMITH_UPGRADE_3"] = "Give two Pale Ore and Geo to strengthen dash?",

                ["NAILSMITH_UPGRADE_4"] = "Give three Pale Ore and Geo to strengthen dash?",

                ["GET_SHADOWDASH_2"] = "Use the cloak to dash through the seams in reality and gather SOUL from the space in-between.",

                ["GET_SHADOWDASH_1"] = "to dash while gathering SOUL from the environment.",

                ["GET_DASH_2"] = "Use the cloak to dash quickly through the air in all directions.",

                ["GET_DASH_1"] = "while holding any direction to dash in that direction."
            };
            Dictionary<string, string> rusPromptDictionary = new Dictionary<string, string>
            {
                ["NAILSMITH_UPGRADE_1"] = "Заплатить Гео, чтобы усилить рывок?",

                ["NAILSMITH_UPGRADE_2"] = "Отдать одну Бледную руду и Гео для усиления рывка?",

                ["NAILSMITH_UPGRADE_3"] = "Отдать две Бледные руды и Гео для усиления рывка?",

                ["NAILSMITH_UPGRADE_4"] = "Отдать три Бледные руды и Гео для усиления рывка?",

                ["GET_SHADOWDASH_2"] = "Используйте плащ, чтобы проходить через щели в реальности и получать ДУШУ с пространства внутри.",

                ["GET_SHADOWDASH_1"] = "для рывка, пока получаете ДУШУ от окружения.",

                ["GET_DASH_2"] = "Используйте плащ, чтобы делать рывок в любом направлении.",

                ["GET_DASH_1"] = "пока держите кнопку любого направления для рывка в этом направлении."
            };
            FlavorDictionary.Add(new KeyValuePair<int, string>(147, "UI"), ptbrUIDictionary);
            FlavorDictionary.Add(new KeyValuePair<int, string>(44, "UI"), enUIDictionary);
            FlavorDictionary.Add(new KeyValuePair<int, string>(154, "UI"), rusUIDictionary);
            FlavorDictionary.Add(new KeyValuePair<int, string>(147, "Prompts"), ptbrPromptDictionary);
            FlavorDictionary.Add(new KeyValuePair<int, string>(44, "Prompts"), enPromptDictionary);
            FlavorDictionary.Add(new KeyValuePair<int, string>(154, "Prompts"), rusPromptDictionary);
            LogDebug("Finished initializing dictionaries.");
        }

        private void Update()
        {
            DashCooldownUpdate();
            if (PlayerData.instance.GetBool("equippedCharm_35") && GameManager.instance.inputHandler.inputActions.dash.IsPressed)
            {
                CheckForDash();
            }

            if (antiTurboDashFrames > 0 && dashInvulTimer <= 0)
            {
                antiTurboDashFrames--;
            }
            if (dashInvulTimer > 0)
            {
                HeroController.instance.cState.invulnerable = true;
            }
            else if (dashInvulTimer == 0 && oldDashInvulTimer > 0)
            {
                oldDashInvulTimer = 0;
                HeroController.instance.cState.invulnerable = false;
                // Set to the minimum number of frames between dashes. By default. 1 + however long one tick is rounded
                // down. I think this is the minimum possible # of frames that will still prevent turbo abuse for
                // invulnerability. You can still use the turbo button to go really fast though.
                antiTurboDashFrames = 1 + (int)(Time.fixedDeltaTime / Time.deltaTime);
                LogDebug("Anti-turbo set to " + antiTurboDashFrames);
            }

            if (PlayerData.instance.GetBool("hasSuperDash") && PlayerData.instance.GetBool("defeatedNightmareGrimm")) AirSuperDash();

            if (HeroController.instance.cState.onGround && dashCount > 1) dashCount = 0;

            GetSuperdashDirection();

            //NewSuperDash();

            HeroController.instance.gameObject.transform.position = GrubbersHandling();
        }

        void ResetPosition(Scene scene, LoadSceneMode mode)
        {
            grubberOn = false;
            Log("Resetting Grubber");
        }

        Vector3 GrubbersHandling()
        {
            Vector3 ret = HeroController.instance.gameObject.transform.position;
            if (PlayerData.instance.GetBool("equippedCharm_35") && GameManager.instance.inputHandler.inputActions.dash.WasPressed)
            {
                grubberOn = !grubberOn;
                heroPos = HeroController.instance.gameObject.transform.position;
            }
            if (PlayerData.instance.GetBool("equippedCharm_35"))
            {
                if (grubberOn)
                {
                    float num;
                    Vector3 vector3 = Vector3.zero;
                    if (!PlayerData.instance.GetBool("hasDash"))
                    {
                        if (PlayerData.instance.GetBool("equippedCharm_16") && HeroController.instance.cState.shadowDashing)
                        {
                            num = HeroController.instance.DASH_SPEED_SHARP;
                        }
                        else
                        {
                            num = HeroController.instance.DASH_SPEED;
                        }
                    }
                    else if (PlayerData.instance.GetBool("equippedCharm_16") && HeroController.instance.cState.shadowDashing)
                    {
                        num = HeroController.instance.DASH_SPEED_SHARP * 1.2f;
                    }
                    else
                    {
                        num = HeroController.instance.DASH_SPEED * 1.2f;
                    }
                    if (PlayerData.instance.GetBool("equippedCharm_18"))
                    {
                        num *= 1.2f;
                    }
                    if (PlayerData.instance.GetBool("equippedCharm_13"))
                    {
                        num *= 1.5f;
                    }
                    if (GameManager.instance.inputHandler.inputActions.right.IsPressed)
                    {
                        vector3 += Vector3.right;
                        HeroController.instance.proxyFSM.SendEvent("HeroCtrl-ShadowDash");
                        ((AudioSource)GetPrivateField("audioSource").GetValue(HeroController.instance)).PlayOneShot(HeroController.instance.sharpShadowClip, 0.2f);
                        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x - 5.21f, HeroController.instance.transform.position.y - 0.58f, HeroController.instance.transform.position.z + 0.00101f)));
                        ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(-1.919591f, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.y, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.z);
                    }
                    if (GameManager.instance.inputHandler.inputActions.left.IsPressed)
                    {
                        vector3 += Vector3.left;
                        HeroController.instance.proxyFSM.SendEvent("HeroCtrl-ShadowDash");
                        ((AudioSource)GetPrivateField("audioSource").GetValue(HeroController.instance)).PlayOneShot(HeroController.instance.sharpShadowClip, 0.2f);
                        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x + 5.21f, HeroController.instance.transform.position.y - 0.58f, HeroController.instance.transform.position.z + 0.00101f)));
                        ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(1.919591f, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.y, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.z);
                    }
                    if (GameManager.instance.inputHandler.inputActions.up.IsPressed)
                    {
                        vector3 += Vector3.up;
                        HeroController.instance.proxyFSM.SendEvent("HeroCtrl-ShadowDash");
                        ((AudioSource)GetPrivateField("audioSource").GetValue(HeroController.instance)).PlayOneShot(HeroController.instance.sharpShadowClip, 0.2f);
                        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x - 0.58f, HeroController.instance.transform.position.y - 5.21f, HeroController.instance.transform.position.z + 0.00101f), new Quaternion(0f, 0f, -0.7071f, 0.7071f)));
                        ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(1.919591f, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.y, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.z);
                    }
                    if (GameManager.instance.inputHandler.inputActions.down.IsPressed)
                    {
                        vector3 += Vector3.down;
                        HeroController.instance.proxyFSM.SendEvent("HeroCtrl-ShadowDash");
                        ((AudioSource)GetPrivateField("audioSource").GetValue(HeroController.instance)).PlayOneShot(HeroController.instance.sharpShadowClip, 0.2f);
                        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x - 0.58f, HeroController.instance.transform.position.y + 5.21f, HeroController.instance.transform.position.z + 0.00101f), new Quaternion(0f, 0f, +0.7071f, 0.7071f)));
                        ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(1.919591f, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.y, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.z);
                    }
                    if (GameManager.instance.inputHandler.inputActions.right.IsPressed)
                    {
                        HeroController.instance.FaceRight();
                    }
                    else if (GameManager.instance.inputHandler.inputActions.left.IsPressed)
                    {
                        HeroController.instance.FaceLeft();
                    }
                    HeroController.instance.cState.dashing = true;
                    GetPrivateField("dashQueueSteps").SetValue(HeroController.instance, 0);
                    HeroController.instance.dashBurst.transform.localPosition = new Vector3(4.11f, -0.55f, 0.001f);
                    HeroController.instance.dashBurst.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                    HeroController.instance.dashingDown = false;
                    GetPrivateField("shadowDashTimer").SetValue(HeroController.instance, GetPrivateField("dashCooldownTimer").GetValue(HeroController.instance));
                    HeroController.instance.proxyFSM.SendEvent("HeroCtrl-ShadowDash");
                    HeroController.instance.cState.shadowDashing = true;
                    ((AudioSource)GetPrivateField("audioSource").GetValue(HeroController.instance)).PlayOneShot(HeroController.instance.sharpShadowClip, sharpShadowVolume);
                    HeroController.instance.sharpShadowPrefab.SetActive(true);
                    heroPos += vector3 * num * Time.deltaTime;
                    ret = heroPos;
                }
            }
            return ret;
        }

        private void GetSuperdashDirection()
        {
            if (HeroController.instance.cState.superDashing && PlayerData.instance.GetBool("defeatedNightmareGrimm"))
            {
                if (GameManager.instance.inputHandler.inputActions.right.IsPressed && !HeroController.instance.cState.facingRight)
                {
                    HeroController.instance.gameObject.transform.Rotate(new Vector3(0, 1, 0), 180f);
                    HeroController.instance.cState.facingRight = true;
                    superDash.SetState("G Right");
                }
                else if (GameManager.instance.inputHandler.inputActions.left.IsPressed && HeroController.instance.cState.facingRight)
                {
                    HeroController.instance.gameObject.transform.Rotate(new Vector3(0, 1, 0), 180f);
                    HeroController.instance.cState.facingRight = false;
                    superDash.SetState("G Left");
                }
            }
        }

        private HitInstance SetDamages(Fsm hitter, HitInstance hit)
        {
            LogDebug($@"Creating HitInstance for {hitter.Owner}");
            Fsm fsm = hitter;
            dashDamage = 5 + PlayerData.instance.GetInt("nailSmithUpgrades") * 4;
            float multiplier = 1;
            if (PlayerData.instance.GetBool("hasShadowDash"))
            {
                multiplier *= 2;
            }
            if (PlayerData.instance.GetBool("equippedCharm_25"))
            {
                multiplier *= 1.5f;
            }
            if (PlayerData.instance.GetBool("equippedCharm_6") && PlayerData.instance.GetInt("health") == 1)
            {
                multiplier *= 1.75f;
            }
            if (oldDashDamage != dashDamage)
            {
                Log($@"[Blackmoth] Sharp Shadow Damage set to {dashDamage}");
                oldDashDamage = dashDamage;
            }
            if (sharpShadow != null && hitter.GameObject == sharpShadow)
            {
                LogDebug($@"Setting damage for {hitter.GameObject.name}");
                hit.DamageDealt = dashDamage;
                hit.AttackType = 0;
                hit.Multiplier = multiplier;
                hit.Direction = HeroController.instance.cState.facingRight ? 0 : 180;
                hit.MagnitudeMultiplier = PlayerData.instance.GetBool("equippedCharm_15") ? 2f : 0f;
            }
            else if (hitter.GameObject.name.Contains("Slash"))
            {
                LogDebug($@"Setting damage for {hitter.GameObject.name}");
                hit.DamageDealt = 1;
            }
            else if (hitter.GameObject.name == superDash.gameObject.name && PlayerData.instance.GetBool("defeatedNightmareGrimm"))
            {
                LogDebug($@"Setting damage for {hitter.GameObject.name}");
                hit.DamageDealt = dashDamage;
            }
            return hit;
        }

        private void DashCooldownUpdate()
        {
            if (dashCooldown > 0)
            {
                dashCooldown -= Time.deltaTime;
            }
            else dashCooldown = 0;
            if (dashInvulTimer > 0)
            {
                oldDashInvulTimer = dashInvulTimer;
                dashInvulTimer -= Time.deltaTime;
            }
            else dashInvulTimer = 0;
        }

        private void GetReferences()
        {
            GameManager.instance.StartCoroutine(ConfigureHero());
        }

        private void GetReferences(SaveGameData data)
        {
            GameManager.instance.StartCoroutine(ConfigureHero());
        }

        IEnumerator ConfigureHero()
        {
            while (HeroController.instance == null)
                yield return new WaitForEndOfFrame();

            if (sharpShadow == null || sharpShadow.tag != "Sharp Shadow")
                foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (gameObject != null && gameObject.tag == "Sharp Shadow")
                    {
                        sharpShadow = gameObject;
                        sharpShadowFSM = FSMUtility.LocateFSM(sharpShadow, "damages_enemy");
                        LogDebug($@"Found Sharp Shadow: {sharpShadow} - {sharpShadowFSM}.");
                    }
                }

            //HeroController.instance.gameObject.AddComponent<SuperDashHandler>();
        }

        void AirSuperDash()
        {
            if (GameManager.instance.inputHandler.inputActions.superDash.IsPressed)
            {
                foreach (FsmState state in superDash.FsmStates)
                {
                    if (state.Name == "Inactive")
                    {
                        state.Transitions[0].ToState = "Relinquish Control";
                    }
                    if (state.Name == "Relinquish Control")
                    {
                        if (PlayerData.instance.GetBool("killedNightmareGrimm"))
                        {
                            if (HeroController.instance.cState.wallSliding)
                            {
                                state.Transitions[0].ToState = "Wall Charged";
                                break;
                            }
                            else
                            {
                                state.Transitions[0].ToState = "Ground Charged";
                                break;
                            }
                        }
                        else if (HeroController.instance.cState.wallSliding)
                        {
                            state.Transitions[0].ToState = "Wall Charge";
                            break;
                        }
                        else if (HeroController.instance.cState.onGround)
                        {
                            state.Transitions[0].ToState = "Ground Charge";
                            break;
                        }
                    }
                }
            }
        }


        private Vector2 CalculateDashVelocity(Vector2 change)
        {
            float num;
            Vector2 ret;
            if (heroRigidbody2D == null) heroRigidbody2D = HeroController.instance.GetComponent<Rigidbody2D>();
            if (PlayerData.instance.GetBool("equippedCharm_16"))
            {
                if (!PlayerData.instance.GetBool("hasDash"))
                {
                    if (PlayerData.instance.GetBool("equippedCharm_16") && HeroController.instance.cState.shadowDashing)
                    {
                        num = HeroController.instance.DASH_SPEED_SHARP;
                    }
                    else
                    {
                        num = HeroController.instance.DASH_SPEED;
                    }
                }
                else if (PlayerData.instance.GetBool("equippedCharm_16") && HeroController.instance.cState.shadowDashing)
                {
                    num = HeroController.instance.DASH_SPEED_SHARP * 1.2f;
                }
                else
                {
                    num = HeroController.instance.DASH_SPEED * 1.2f;
                }
                if (PlayerData.instance.GetBool("equippedCharm_18"))
                {
                    num *= 1.2f;
                }
                if (PlayerData.instance.GetBool("equippedCharm_13"))
                {
                    num *= 1.5f;
                }
                if (DashDirection.y == 0)
                {
                    if (HeroController.instance.cState.facingRight)
                    {
                        if (HeroController.instance.CheckForBump(CollisionSide.right))
                        {
                            ret = Time.deltaTime * new Vector2(num, (float)GetPrivateField("BUMP_VELOCITY_DASH").GetValue(HeroController.instance));
                        }
                        else
                        {
                            ret = Time.deltaTime * new Vector2(num, 0f);
                        }
                    }
                    else if (HeroController.instance.CheckForBump(CollisionSide.left))
                    {
                        ret = Time.deltaTime * new Vector2(-num, (float)GetPrivateField("BUMP_VELOCITY_DASH").GetValue(HeroController.instance));
                    }
                    else
                    {
                        ret = Time.deltaTime * new Vector2(-num, 0f);
                    }
                }
                else if (DashDirection.x == 0)
                {
                    ret = Time.deltaTime * num * DashDirection;
                }
                else
                {
                    ret = Time.deltaTime * (num / Mathf.Sqrt(2)) * DashDirection;
                }
                heroRigidbody2D.position = heroRigidbody2D.position + ret;
                return (ret * 0.001f);
            }

            if (!PlayerData.instance.GetBool("hasDash"))
            {
                num = PlayerData.instance.GetBool("equippedCharm_16") && HeroController.instance.cState.shadowDashing
                    ? HeroController.instance.DASH_SPEED_SHARP
                    : HeroController.instance.DASH_SPEED;
            }
            else if (PlayerData.instance.GetBool("equippedCharm_16") && HeroController.instance.cState.shadowDashing)
            {
                num = HeroController.instance.DASH_SPEED_SHARP * 1.2f;
            }
            else
            {
                num = HeroController.instance.DASH_SPEED * 1.2f;
            }
            if (PlayerData.instance.GetBool("equippedCharm_18"))
            {
                num *= 1.2f;
            }
            if (PlayerData.instance.GetBool("equippedCharm_13"))
            {
                num *= 1.5f;
            }
            if (DashDirection.y == 0)
            {
                if (HeroController.instance.cState.facingRight)
                {
                    ret = HeroController.instance.CheckForBump(CollisionSide.right) ? new Vector2(num, (float)GetPrivateField("BUMP_VELOCITY_DASH").GetValue(HeroController.instance)) : new Vector2(num, 0f);
                }
                else
                {
                    ret = HeroController.instance.CheckForBump(CollisionSide.left)
                        ? new Vector2(-num,
                            (float)GetPrivateField("BUMP_VELOCITY_DASH").GetValue(HeroController.instance))
                        : new Vector2(-num, 0f);
                }
            }
            else
                ret = DashDirection.x == 0 ? num * DashDirection : (num / Mathf.Sqrt(2)) * DashDirection;

            return ret;
        }


        public bool CheckForDash()
        {
            if (antiTurboDashFrames > 0)
                return true;
            dashCooldownStart = dashCooldownStart == HeroController.instance.DASH_COOLDOWN_CH * 0.4f ? dashCooldownStart : HeroController.instance.DASH_COOLDOWN_CH * 0.4f;
            dashCooldownHasDash = dashCooldownHasDash == HeroController.instance.DASH_COOLDOWN_CH * 0.1f ? dashCooldownHasDash : HeroController.instance.DASH_COOLDOWN_CH * 0.1f;
            GetPrivateField("dashQueueSteps").SetValue(HeroController.instance, 0);
            GetPrivateField("dashQueuing").SetValue(HeroController.instance, false);
            HeroActions direction = GameManager.instance.inputHandler.inputActions;
            if (PlayerData.instance.GetBool("equippedCharm_35"))
            {
                GetPrivateField("dashCooldownTimer").SetValue(HeroController.instance, 0f);
                GetPrivateField("shadowDashTimer").SetValue(HeroController.instance, 0f);
            }

            if (direction.up.IsPressed)
            {
                DashDirection.y = 1;
            }
            else if (direction.down.IsPressed && !HeroController.instance.cState.onGround)
            {
                DashDirection.y = -1;
            }
            else
            {
                DashDirection.y = 0;
            }

            if (direction.right.IsPressed)
            {
                DashDirection.x = 1;
            }
            else if (direction.left.IsPressed)
            {
                DashDirection.x = -1;
            }
            else if (DashDirection.y == 0)
            {
                DashDirection.x = HeroController.instance.cState.facingRight ? 1 : -1;
            }
            else
            {
                DashDirection.x = 0;
            }
            LogDebug($@"Dash direction: {DashDirection}");
            if (!PlayerData.instance.GetBool("hasDash"))
            {
                DashDirection.y = 0;
                DashDirection.x = HeroController.instance.cState.facingRight ? 1 : -1;
            }

            if (!PlayerData.instance.GetBool("equippedCharm_35"))
            {
                DoDash();

                // Fixes TC problem where after tink sharp shadow is broken
                sharpShadowFSM.SetState("Idle");
            }

            return true;
        }

        bool AirDashed()
        {
            if (PlayerData.instance.GetBool("equippedCharm_31"))
            {
                if ((bool)GetPrivateField("airDashed").GetValue(HeroController.instance))
                {
                    if (dashCount < 2)
                    {
                        dashCount++;
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            else return (bool)GetPrivateField("airDashed").GetValue(HeroController.instance);
        }

        void DoDash()
        {
            if ((HeroController.instance.hero_state != ActorStates.no_input
                && HeroController.instance.hero_state != ActorStates.hard_landing
                && HeroController.instance.hero_state != ActorStates.dash_landing
                && dashCooldown <= 0f
                && !HeroController.instance.cState.dashing && !HeroController.instance.cState.backDashing
                && (!HeroController.instance.cState.attacking
                || (float)GetPrivateField("attack_time").GetValue(HeroController.instance) >= HeroController.instance.ATTACK_RECOVERY_TIME)
                && !HeroController.instance.cState.preventDash
                && (HeroController.instance.cState.onGround
                || !AirDashed()
                || HeroController.instance.cState.wallSliding))
                || PlayerData.instance.GetBool("equippedCharm_35"))
            {
                sharpShadowVolume = PlayerData.instance.GetBool("equippedCharm_35") ? 0.1f : 1f;
                if ((!HeroController.instance.cState.onGround || DashDirection.y != 0) && !HeroController.instance.inAcid && !HeroController.instance.playerData.equippedCharm_32)
                {
                    GetPrivateField("airDashed").SetValue(HeroController.instance, true);
                }
                if (PlayerData.instance.GetBool("hasShadowDash"))
                {
                    HeroController.instance.AddMPChargeSpa(3);
                }


                InvokePrivateMethod("ResetAttacksDash");
                InvokePrivateMethod("CancelBounce");
                ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.FOOTSTEPS_RUN);
                ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.FOOTSTEPS_WALK);
                ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.DASH);
                InvokePrivateMethod("ResetLook");

                HeroController.instance.cState.recoiling = false;
                if (HeroController.instance.cState.wallSliding)
                {
                    HeroController.instance.FlipSprite();
                }
                else if (GameManager.instance.inputHandler.inputActions.right.IsPressed)
                {
                    HeroController.instance.FaceRight();
                }
                else if (GameManager.instance.inputHandler.inputActions.left.IsPressed)
                {
                    HeroController.instance.FaceLeft();
                }
                HeroController.instance.cState.dashing = true;
                GetPrivateField("dashQueueSteps").SetValue(HeroController.instance, 0);
                HeroController.instance.dashBurst.transform.localPosition = new Vector3(4.11f, -0.55f, 0.001f);
                HeroController.instance.dashBurst.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                HeroController.instance.dashingDown = false;

                if (HeroController.instance.playerData.equippedCharm_32)
                    dashCooldown = 0;
                else
                {
                    dashCooldown = HeroController.instance.playerData.hasDash ? dashCooldownHasDash : dashCooldownStart;
                }

                GetPrivateField("shadowDashTimer").SetValue(HeroController.instance, GetPrivateField("dashCooldownTimer").GetValue(HeroController.instance));
                HeroController.instance.proxyFSM.SendEvent("HeroCtrl-ShadowDash");
                HeroController.instance.cState.shadowDashing = true;
                ((AudioSource)GetPrivateField("audioSource").GetValue(HeroController.instance)).PlayOneShot(HeroController.instance.sharpShadowClip, sharpShadowVolume);
                HeroController.instance.sharpShadowPrefab.SetActive(true);

                if (HeroController.instance.cState.shadowDashing)
                {
                    if (HeroController.instance.transform.localScale.x > 0f)
                    {
                        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x + 5.21f, HeroController.instance.transform.position.y - 0.58f, HeroController.instance.transform.position.z + 0.00101f)));
                        ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(1.919591f, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.y, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.z);
                    }
                    else
                    {
                        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x - 5.21f, HeroController.instance.transform.position.y - 0.58f, HeroController.instance.transform.position.z + 0.00101f)));
                        ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(-1.919591f, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.y, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.z);
                    }
                    HeroController.instance.shadowRechargePrefab.SetActive(true);
                    FSMUtility.LocateFSM(HeroController.instance.shadowRechargePrefab, "Recharge Effect").SendEvent("RESET");
                    ParticleSystem ps = HeroController.instance.shadowdashParticlesPrefab.GetComponent<ParticleSystem>();
                    var em = ps.emission;
                    em.enabled = true;
                    HeroController.instance.shadowRingPrefab.Spawn(HeroController.instance.transform.position);
                }
                else
                {
                    HeroController.instance.dashBurst.SendEvent("PLAY");
                    ParticleSystem ps = HeroController.instance.dashParticlesPrefab.GetComponent<ParticleSystem>();
                    var em = ps.emission;
                    em.enabled = true;
                }
                if (HeroController.instance.cState.onGround && !HeroController.instance.cState.shadowDashing)
                {
                    GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.backDashPrefab.Spawn(HeroController.instance.transform.position));
                    ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(HeroController.instance.transform.localScale.x * -1f, HeroController.instance.transform.localScale.y, HeroController.instance.transform.localScale.z);
                }
                dashInvulTimer = 0.3f;
            }
        }


        private GameObject DashSoul(GameObject go, Fsm fsm)
        {
            if (go == sharpShadow)
            {
                PlayMakerFSM hm = FSMUtility.LocateFSM(fsm.GameObject, "health_manager") ?? FSMUtility.LocateFSM(fsm.GameObject, "health_manager_enemy");
                if (!Equals(hm, null))
                {
                    LogDebug($"Hitting enemy {fsm.GameObject}");
                    HeroController.instance.AddMPChargeSpa(11);
                }
            }
            return go;
        }

        string Descriptions(string key, string sheet)
        {
            string ret = Language.Language.GetInternal(key, sheet);
            int lang = (int)Language.Language.CurrentLanguage();
            KeyValuePair<int, string> langSheet = new KeyValuePair<int, string>(lang, sheet);
            if (!FlavorDictionary.ContainsKey(langSheet)) return ret;
            return FlavorDictionary[langSheet].ContainsKey(key) ? FlavorDictionary[langSheet][key] : ret;
        }

        private FieldInfo GetPrivateField(string fieldName)
        {
            if (!privateFields.ContainsKey(fieldName))
            {
                privateFields.Add(fieldName,
                    HeroController.instance.GetType()
                        .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance));
            }
            return privateFields[fieldName];
        }

        private void InvokePrivateMethod(string methodName)
        {
            if (!privateMethods.ContainsKey(methodName))
            {
                privateMethods.Add(methodName,
                    HeroController.instance.GetType()
                        .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance));
            }
            privateMethods[methodName]?.Invoke(HeroController.instance, new object[] { });
        }

        public Vector2 DashDirection;
        public GameObject sharpShadow;
        public PlayMakerFSM superDash;
        public PlayMakerFSM sharpShadowFSM;
        int oldDashDamage { get; set; }
        int dashDamage { get; set; }
        float dashCooldown { get; set; }
        float dashCooldownStart { get; set; }
        float dashCooldownHasDash { get; set; }
        float oldDashInvulTimer { get; set; }
        float dashInvulTimer { get; set; }
        float sharpShadowVolume { get; set; }
        int dashCount { get; set; }
        private int antiTurboDashFrames = 0;
        public bool grubberOn;
        private Rigidbody2D heroRigidbody2D;
        Vector3 heroPos { get; set; } = Vector3.zero;
        private Dictionary<string, FieldInfo> privateFields;
        private Dictionary<string, MethodInfo> privateMethods;
        public Dictionary<KeyValuePair<int, string>, Dictionary<string, string>> FlavorDictionary;
    }
}