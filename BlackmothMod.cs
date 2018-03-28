using GlobalEnums;
using Modding;
using System.Reflection;
using UnityEngine;
using HutongGames.PlayMaker;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


namespace BlackmothMod
{
    public class Blackmoth : Mod, ITogglableMod, IMod, Modding.ILogger
    {
        public static Blackmoth Instance;

        public override string GetVersion() => "1.6.0";

        public override void Initialize()
        {

            Instance = this;
            Instance.Log("Blackmoth initializing!");

            ModHooks.Instance.HeroUpdateHook += Update;
            ModHooks.Instance.HeroUpdateHook += GetReferences;
            ModHooks.Instance.DashVectorHook += CalculateDashVelocity;
            ModHooks.Instance.DashPressedHook += CheckForDash;
            ModHooks.Instance.OnGetEventSenderHook += DashSoul;
            ModHooks.Instance.LanguageGetHook += Descriptions;
            //ModHooks.Instance.SceneChanged += DashSoul;


            Instance.Log("Blackmoth initialized!");
        }

        public void Unload()
        {
            ModHooks.Instance.HeroUpdateHook -= Update;
            ModHooks.Instance.HeroUpdateHook -= GetReferences;
            ModHooks.Instance.DashVectorHook -= CalculateDashVelocity;
            ModHooks.Instance.DashPressedHook -= CheckForDash;
            ModHooks.Instance.OnGetEventSenderHook -= DashSoul;
            PlayerData.instance.nailDamage = 5 + 4 * PlayerData.instance.nailSmithUpgrades;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");

            Instance.Log("Blackmoth unloaded!");
        }

        public void Update()
        {
            DashCooldownUpdate();
            if (PlayerData.instance.equippedCharm_35 && GameManager.instance.inputHandler.inputActions.dash.IsPressed)
            {
                CheckForDash();
            }
            SetDamages();

            if (PlayerData.instance.hasSuperDash && PlayerData.instance.defeatedNightmareGrimm) AirSuperDash();

            if (HeroController.instance.cState.onGround && dashCount > 1) dashCount = 0;

            GetSuperdashDirection();
        }

        private void GetSuperdashDirection()
        {
            if (HeroController.instance.cState.superDashing && PlayerData.instance.defeatedNightmareGrimm)
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

        private void SetDamages()
        {
            dashDamage = 5 + PlayerData.instance.nailSmithUpgrades * 4;
            if (PlayerData.instance.equippedCharm_16)
            {
                dashDamage *= 2;
            }
            if (PlayerData.instance.equippedCharm_25)
            {
                dashDamage = (int)((double)dashDamage * 1.5);
            }
            if (oldDashDamage != dashDamage)
            {
                Log($@"[Blackmoth] Sharp Shadow Damage set to {dashDamage}");
                oldDashDamage = dashDamage;
            }
            try
            {
                sharpShadowFSM.FsmVariables.GetFsmInt("damageDealt").Value = dashDamage;
                //sharpShadowFSM.FsmVariables.GetFsmInt("attackType").Value = 0;
                if (PlayerData.instance.defeatedNightmareGrimm) superDash.FsmVariables.GetFsmInt("DamageDealt").Value = dashDamage;
            }
            catch
            {
                Blackmoth.Instance.LogWarn("[Blackmoth] Sharp Shadow object not set!");
            }
            PlayerData.instance.nailDamage = 1;
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }

        private void DashCooldownUpdate()
        {
            if (dashCooldown != 0)
            {
                dashCooldown -= Time.deltaTime;
            }
        }

        public void GetReferences()
        {
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

            if (superDash == null)
            {
                superDash = HeroController.instance.superDash;
            }
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
                        if (PlayerData.instance.killedNightmareGrimm)
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


        public Vector2 CalculateDashVelocity()
        {
            float num;
            if (!PlayerData.instance.hasDash)
            {
                if (PlayerData.instance.equippedCharm_16 && HeroController.instance.cState.shadowDashing)
                {
                    num = HeroController.instance.DASH_SPEED_SHARP;
                }
                else
                {
                    num = HeroController.instance.DASH_SPEED;
                }
            }
            else if (PlayerData.instance.equippedCharm_16 && HeroController.instance.cState.shadowDashing)
            {
                num = HeroController.instance.DASH_SPEED_SHARP * 1.2f;
            }
            else
            {
                num = HeroController.instance.DASH_SPEED * 1.2f;
            }
            if (PlayerData.instance.equippedCharm_18)
            {
                num *= 1.2f;
            }
            if (PlayerData.instance.equippedCharm_13)
            {
                num *= 1.5f;
            }
            if (DashDirection.y == 0)
            {
                if (HeroController.instance.cState.facingRight)
                {
                    if (HeroController.instance.CheckForBump(CollisionSide.right))
                    {
                        return new Vector2(num, (float)GetPrivateField("BUMP_VELOCITY_DASH").GetValue(HeroController.instance));
                    }
                    else
                    {
                        return new Vector2(num, 0f);
                    }
                }
                else if (HeroController.instance.CheckForBump(CollisionSide.left))
                {
                    return new Vector2(-num, (float)GetPrivateField("BUMP_VELOCITY_DASH").GetValue(HeroController.instance));
                }
                else
                {
                    return new Vector2(-num, 0f);
                }
            }
            else if (DashDirection.x == 0)
            {
                return num * DashDirection;
            }
            else
            {
                return (num / Mathf.Sqrt(2)) * DashDirection;
            }
        }


        public void CheckForDash()
        {
            dashCooldownStart = dashCooldownStart == HeroController.instance.DASH_COOLDOWN_CH * 0.4f ? dashCooldownStart : HeroController.instance.DASH_COOLDOWN_CH * 0.4f;
            dashCooldownHasDash = dashCooldownHasDash == HeroController.instance.DASH_COOLDOWN_CH * 0.1f ? dashCooldownHasDash : HeroController.instance.DASH_COOLDOWN_CH * 0.1f;
            GetPrivateField("dashQueueSteps").SetValue(HeroController.instance, 0);
            GetPrivateField("dashQueuing").SetValue(HeroController.instance, false);
            HeroActions direction = GameManager.instance.inputHandler.inputActions;
            if (PlayerData.instance.equippedCharm_35)
            {
                GetPrivateField("dashCooldownTimer").SetValue(HeroController.instance, 0f);
                GetPrivateField("shadowDashTimer").SetValue(HeroController.instance, 0);
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
            if (!PlayerData.instance.hasDash)
            {
                DashDirection.y = 0;
                DashDirection.x = HeroController.instance.cState.facingRight ? 1 : -1;
            }
            if (PlayerData.instance.equippedCharm_35) HeroController.instance.cState.invulnerable = true;
            DoDash();
            if (PlayerData.instance.equippedCharm_35) HeroController.instance.cState.invulnerable = false;
        }

        bool AirDashed()
        {
            if (PlayerData.instance.equippedCharm_31)
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
                || PlayerData.instance.equippedCharm_35)
            {
                if ((!HeroController.instance.cState.onGround || DashDirection.y != 0) && !HeroController.instance.inAcid && !HeroController.instance.playerData.equippedCharm_32)
                {
                    GetPrivateField("airDashed").SetValue(HeroController.instance, true);
                }
                if (PlayerData.instance.hasShadowDash)
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

                if (HeroController.instance.playerData.hasDash)
                    dashCooldown = dashCooldownHasDash;
                else
                    dashCooldown = dashCooldownStart;
                if (HeroController.instance.playerData.equippedCharm_32)
                    dashCooldown = 0;
                GetPrivateField("shadowDashTimer").SetValue(HeroController.instance, GetPrivateField("dashCooldownTimer").GetValue(HeroController.instance));
                HeroController.instance.proxyFSM.SendEvent("HeroCtrl-ShadowDash");
                HeroController.instance.cState.shadowDashing = true;
                ((AudioSource)GetPrivateField("audioSource").GetValue(HeroController.instance)).PlayOneShot(HeroController.instance.sharpShadowClip, 1f); HeroController.instance.sharpShadowPrefab.SetActive(true);

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
            }
        }


        private GameObject DashSoul(GameObject go, Fsm fsm)
        {
            if (fsm.GameObject.GetComponent<HealthManager>() != null)
            {
                HeroController.instance.AddMPChargeSpa(11);
            }
            return go;
        }

        string Descriptions(string key, string sheet)
        {
            string ret = Language.Language.GetInternal(key, sheet);
            if (sheet == "UI")
            {
                switch(key)
                {
                    case "CHARM_DESC_13":
                        ret = $@"Freely given by the Mantis Tribe to those they respect.

Greatly increases the range of the bearer's dash, allowing them to strike foes from further away.";
                        break;

                    case "CHARM_DESC_16":
                        ret = $@"Contains a forbidden spell that transforms shadows into deadly weapons.

When dashing, the bearer's body will sharpen and deal extra damage to enemies.";
                        break;

                    case "CHARM_DESC_18":
                        ret = $@"Increases the range of the bearer's dash, allowing them to strike foes from further away.";
                        break;

                    case "CHARM_DESC_25":
                        ret = $@"Strengthens the bearer, increasing the damage they deal to enemies with their dash.

This charm is fragile, and will break if its bearer is killed.";
                        break;

                    case "CHARM_DESC_25_G":
                        ret = $@"Strengthens the bearer, increasing the damage they deal to enemies with their dash.

This charm is ubreakable.";
                        break;

                    case "CHARM_DESC_31":
                        ret = $@"Bears the likeness of an eccentric bug known only as {"The Dashmaster"}.

The bearer will be able to dash more often as well as dash in midair. Perfect for those who want to move around as quickly as possible.";
                        break;

                    case "CHARM_DESC_32":
                        ret = $@"Born from imperfect, discarded cloaks that have fused together. The cloaks still long to be worn.

Allows the bearer to slash much more rapidly with their dash.";
                        break;

                    case "CHARM_DESC_35":
                        ret = $@"Contains the gratitude of grubs who will move to the next stage of their lives. Imbues weapons with a holy strength.

Allows bearer to ascend and become the legendary Grubbermoth and fly away to the heavens.";
                        break;

                    case "CHARM_NAME_18":
                        ret = $@"Longdash";
                        break;

                    case "CHARM_NAME_32":
                        ret = $@"Quick Dash";
                        break;

                    case "CHARM_NAME_35":
                        ret = $@"Grubbermoth's Elegy";
                        break;

                    case "INV_DESC_DASH":
                        ret = $@"Cloak threaded with mothwing strands. Allows the wearer to dash in every direction.";
                        break;

                    case "INV_DESC_SHADOWDASH":
                        ret = $@"Cloak formed from the substance of the Abyss, the progenitor of the Blackmoth. Allows the wearer to gain soul while dashing.";
                        break;

                    case "INV_DESC_SUPERDASH":
                        if (PlayerData.instance.defeatedNightmareGrimm)
                        {
                            ret = $@"The energy core of an old mining golem, fashioned around a potent crystal. The crystal's energy can be channeled to launch the bearer forward at dangerous speeds.

With the Nightmare defeated, the crystal has revealed its true potential, granting its wielder more flexibility.";
                        }
                        else
                        {
                            ret = $@"The energy core of an old mining golem, fashioned around a potent crystal. The crystal's energy can be channeled to launch the bearer forward at dangerous speeds.

Even though it's quite powerful, it seems as if a Nightmare is preventing it from unleashing its true potential...";
                        }
                        break;

                    case "INV_NAME_SHADOWDASH":
                        ret = $@"Blackmoth Cloak";
                        break;
                }
            }
            else if (sheet == "Prompts")
            {
                switch(key)
                {
                    case "NAILSMITH_UPGRADE_1":
                        ret = "Pay Geo to strengthen dash?";
                        break;

                    case "NAILSMITH_UPGRADE_2":
                        ret = "Give Pale Ore and Geo to strengthen dash?";
                        break;

                    case "NAILSMITH_UPGRADE_3":
                        ret = "Give two Pale Ore and Geo to strengthen dash?";
                        break;

                    case "NAILSMITH_UPGRADE_4":
                        ret = "Give three Pale Ore and Geo to strengthen dash?";
                        break;

                    case "GET_SHADOWDASH_2":
                        ret = "Use the cloak to dash and collect Soul from the Void in your surroundings.";
                        break;

                    case "GET_SHADOWDASH_1":
                        ret = "to dash forwards and gain Soul.";
                        break;

                    case "GET_DASH_2":
                        ret = "Use the cloak to dash quickly through the air in all directions.";
                        break;

                    case "GET_DASH_1":
                        ret = "while holding any direction to dash in that direction.";
                        break;
                }
            }
            return ret;
        }

        private FieldInfo GetPrivateField(string fieldName)
        {
            return HeroController.instance.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }

        private void InvokePrivateMethod(string methodName)
        {
            HeroController.instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(HeroController.instance, new object[] { });
        }

        private MethodInfo GetPrivateMethod(string methodName)
        {
            return typeof(HeroController).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        }


        public Vector2 DashDirection;
        public GameObject sharpShadow;
        public PlayMakerFSM superDash;
        public PlayMakerFSM sharpShadowFSM;
        int Multiplier { get; set; } = 1;
        int oldDashDamage { get; set; } = 0;
        int dashDamage { get; set; }
        float timer { get; set; } = 0;
        float dashCooldown { get; set; }
        float dashCooldownStart { get; set; }
        float dashCooldownHasDash { get; set; }
        int dashCount { get; set; } = 0;
    }
}
