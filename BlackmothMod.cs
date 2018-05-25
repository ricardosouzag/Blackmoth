using GlobalEnums;
using Modding;
using System.Reflection;
using UnityEngine;
using HutongGames.PlayMaker;
using UnityEngine.SceneManagement;
using System.Collections;

namespace BlackmothMod
{
    public class Blackmoth : Mod, ITogglableMod
    {
        private static Blackmoth Instance;

        public override string GetVersion() => "1.6.8";

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
            On.PlayMakerFSM.Awake += PlayMakerFSM_Awake;

            Instance.Log("Blackmoth initialized!");
        }

        private void PlayMakerFSM_Awake(On.PlayMakerFSM.orig_Awake orig, PlayMakerFSM self)
        {
            if (self.name.Contains("Sharp Shadow Impact")) sharpShadowControl = self;
            orig(self);
        }

        public void Unload()
        {
            ModHooks.Instance.HeroUpdateHook -= Update;
            ModHooks.Instance.HeroUpdateHook -= GetReferences;
            ModHooks.Instance.DashVectorHook -= CalculateDashVelocity;
            ModHooks.Instance.DashPressedHook -= CheckForDash;
            ModHooks.Instance.OnGetEventSenderHook -= DashSoul;
            ModHooks.Instance.LanguageGetHook -= Descriptions;
            EventInfo hi = ModHooks.Instance.GetType().GetEvent("HitInstanceHook", BindingFlags.Instance | BindingFlags.Public);
            ModHooks.Instance.HitInstanceHook -= SetDamages;

            Instance.Log("Blackmoth unloaded!");
        }

        private void Update()
        {
            DashCooldownUpdate();
            if (PlayerData.instance.equippedCharm_35 && GameManager.instance.inputHandler.inputActions.dash.IsPressed)
            {
                CheckForDash();
            }

            if (dashInvulTimer > 0)
            {
                HeroController.instance.cState.invulnerable = true;
            }
            else if (dashInvulTimer == 0 && oldDashInvulTimer > 0)
            {
                oldDashInvulTimer = 0;
                HeroController.instance.cState.invulnerable = false;
            }

            if (PlayerData.instance.hasSuperDash && PlayerData.instance.defeatedNightmareGrimm) AirSuperDash();

            if (HeroController.instance.cState.onGround && dashCount > 1) dashCount = 0;

            GetSuperdashDirection();

            //NewSuperDash();

            HeroController.instance.gameObject.transform.position = GrubbersHandling();

            if (sharpShadowControl != null && sharpShadowControl.FsmStates[0].Active)
                sharpShadowControl.SendEvent("FINISHED");
        }

        void NewSuperDash()
        {
            InControl.PlayerAction cdash = GameManager.instance.inputHandler.inputActions.superDash;
            if (superDash.ActiveStateName != "Init")
            {
                superDash.SetState("Init");
            }
            if (cdash.WasPressed)
            {
                foreach (NamedVariable var in superDash.FsmVariables.GetAllNamedVariables())
                {
                    Log($@"Superdash has variable {var.Name} ({var.GetType()}) = {var}");
                }
            }
        }

        void ResetPosition(Scene scene, LoadSceneMode mode)
        {
            grubberOn = false;
            Log("Resetting Grubber");
        }

        Vector3 GrubbersHandling()
        {
            Vector3 ret = HeroController.instance.gameObject.transform.position;
            if (PlayerData.instance.equippedCharm_35 && GameManager.instance.inputHandler.inputActions.dash.WasPressed)
            {
                grubberOn = !grubberOn;
                heroPos = HeroController.instance.gameObject.transform.position;
            }
            if (PlayerData.instance.equippedCharm_35)
            {
                if (grubberOn)
                {
                    float num;
                    Vector3 vector3 = Vector3.zero;
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
                        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x - 0.58f, HeroController.instance.transform.position.y - 5.21f, HeroController.instance.transform.position.z + 0.00101f), new Quaternion(0f, 0f, - 0.7071f, 0.7071f)));
                        ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(1.919591f, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.y, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.z);
                    }
                    if (GameManager.instance.inputHandler.inputActions.down.IsPressed)
                    {
                        vector3 += Vector3.down;
                        HeroController.instance.proxyFSM.SendEvent("HeroCtrl-ShadowDash");
                        ((AudioSource)GetPrivateField("audioSource").GetValue(HeroController.instance)).PlayOneShot(HeroController.instance.sharpShadowClip, 0.2f);
                        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x - 0.58f, HeroController.instance.transform.position.y + 5.21f, HeroController.instance.transform.position.z + 0.00101f), new Quaternion(0f, 0f, + 0.7071f, 0.7071f)));
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

        //private void OldSetDamages()
        //{
        //    dashDamage = 5 + PlayerData.instance.nailSmithUpgrades * 4;
        //    if (PlayerData.instance.equippedCharm_16)
        //    {
        //        dashDamage *= 2;
        //    }
        //    if (PlayerData.instance.equippedCharm_25)
        //    {
        //        dashDamage = (int)((double)dashDamage * 1.5);
        //    }
        //    if (oldDashDamage != dashDamage)
        //    {
        //        Log($@"[Blackmoth] Sharp Shadow Damage set to {dashDamage}");
        //        oldDashDamage = dashDamage;
        //    }
        //    try
        //    {
        //        sharpShadowFSM.FsmVariables.GetFsmInt("damageDealt").Value = dashDamage;
        //        //sharpShadowFSM.FsmVariables.GetFsmInt("attackType").Value = 0;
        //        if (PlayerData.instance.defeatedNightmareGrimm) superDash.FsmVariables.GetFsmInt("DamageDealt").Value = dashDamage;
        //    }
        //    catch
        //    {
        //        Blackmoth.Instance.LogWarn("[Blackmoth] Sharp Shadow object not set!");
        //    }
        //    PlayerData.instance.nailDamage = 1;
        //    PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        //}

        private HitInstance SetDamages(Fsm hitter, HitInstance hit)
        {
            LogDebug($@"Creating HitInstance for {hitter.Owner}");
            Fsm fsm = hitter;
            dashDamage = 5 + PlayerData.instance.GetInt("nailSmithUpgrades") * 4;
            float multiplier = 1;
            if (PlayerData.instance.hasShadowDash)
            {
                multiplier *= 2;
            }
            if (PlayerData.instance.equippedCharm_25)
            {
                multiplier *= 1.5f;
            }
            if (PlayerData.instance.equippedCharm_6 && PlayerData.instance.health == 1)
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
                hit.MagnitudeMultiplier = PlayerData.instance.equippedCharm_15 ? 2f : 0f;
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
            FieldInfo[] pinfo = hit.GetType().GetFields();
            LogDebug($"{hitter.GameObject.name} HitInstance:");
            foreach (FieldInfo stat in pinfo)
            {
                LogDebug($"{stat.Name} = {stat.GetValue(hit)}");
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


        private Vector2 CalculateDashVelocity()
        {
            float num;
            Vector2 ret;
            if (PlayerData.instance.GetBool("equippedCharm_16"))
            {
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
                HeroController.instance.GetComponent<Rigidbody2D>().position = HeroController.instance.GetComponent<Rigidbody2D>().position + ret;
                return Vector2.zero;
            }

            if (!PlayerData.instance.hasDash)
            {
                num = PlayerData.instance.equippedCharm_16 && HeroController.instance.cState.shadowDashing
                    ? HeroController.instance.DASH_SPEED_SHARP
                    : HeroController.instance.DASH_SPEED;
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
                    ret = HeroController.instance.CheckForBump(CollisionSide.right) ? new Vector2(num, (float)GetPrivateField("BUMP_VELOCITY_DASH").GetValue(HeroController.instance)) : new Vector2(num, 0f);
                }
                else
                {
                    ret = HeroController.instance.CheckForBump(CollisionSide.left)
                        ? new Vector2(-num,
                            (float) GetPrivateField("BUMP_VELOCITY_DASH").GetValue(HeroController.instance))
                        : new Vector2(-num, 0f);
                }
            }
            else
                ret = DashDirection.x == 0 ? num * DashDirection : (num / Mathf.Sqrt(2)) * DashDirection;

            return ret;
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
            if (!PlayerData.instance.equippedCharm_35) DoDash();
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
                sharpShadowVolume = PlayerData.instance.equippedCharm_35 ? 0.1f : 1f;
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
            if (sheet == "UI")
            {
                switch(key)
                {
                    case "CHARM_DESC_13":
                        ret = @"Freely given by the Mantis Tribe to those they respect.

Greatly increases the range of the bearer's dash, allowing them to strike foes from further away.";
                        break;

                    case "CHARM_DESC_15":
                        ret = @"Formed from the cloaks of fallen warriors.
                            
Increases the force of the bearer's dash, causing enemies to recoil further when hit.";
                        break;

                    case "CHARM_DESC_16":
                        ret = @"Contains a forbidden spell that transforms matter into void.

When dashing, the bearer's body will be able to go through solid objects.";
                        break;

                    case "CHARM_DESC_18":
                        ret =
                            @"Increases the range of the bearer's dash, allowing them to strike foes from further away.";
                        break;

                    case "CHARM_DESC_25":
                        ret = @"Strengthens the bearer, increasing the damage they deal to enemies with their dash.

This charm is fragile, and will break if its bearer is killed.";
                        break;

                    case "CHARM_DESC_25_G":
                        ret = @"Strengthens the bearer, increasing the damage they deal to enemies with their dash.

This charm is ubreakable.";
                        break;

                    case "CHARM_DESC_31":
                        ret = $@"Bears the likeness of an eccentric bug known only as {"The Dashmaster"}.

The bearer will be able to dash more often as well as dash in midair. Perfect for those who want to move around as quickly as possible.";
                        break;

                    case "CHARM_DESC_32":
                        ret = @"Born from imperfect, discarded cloaks that have fused together. The cloaks still long to be worn.

Allows the bearer to slash much more rapidly with their dash.";
                        break;

                    case "CHARM_DESC_35":
                        ret = @"Contains the gratitude of grubs who will move to the next stage of their lives. Imbues weapons with a holy strength.

Allows bearer to ascend and become the legendary Grubbermoth and fly away to the heavens.";
                        break;

                    case "CHARM_NAME_16":
                        ret = @"Void Shade";
                        break;

                    case "CHARM_NAME_18":
                        ret = @"Longdash";
                        break;

                    case "CHARM_NAME_32":
                        ret = @"Quick Dash";
                        break;

                    case "CHARM_NAME_35":
                        ret = @"Grubbermoth's Elegy";
                        break;

                    case "INV_DESC_DASH":
                        ret = @"Cloak threaded with mothwing strands. Allows the wearer to dash in every direction.";
                        break;

                    case "INV_DESC_SHADOWDASH":
                        ret = @"Cloak formed from the substance of the Abyss, the progenitor of the Blackmoth. Allows the wearer to gain soul and deal double damage while dashing.";
                        break;

                    case "INV_DESC_SUPERDASH":
                        ret = PlayerData.instance.defeatedNightmareGrimm ? @"The energy core of an old mining golem, fashioned around a potent crystal. The crystal's energy can be channeled to launch the bearer forward at dangerous speeds.

With the Nightmare defeated, the crystal has revealed its true potential, granting its wielder more flexibility." : @"The energy core of an old mining golem, fashioned around a potent crystal. The crystal's energy can be channeled to launch the bearer forward at dangerous speeds.

Even though it's quite powerful, it seems as if a Nightmare is preventing it from unleashing its true potential...";
                        break;

                    case "INV_NAME_SHADOWDASH":
                        ret = @"Blackmoth Cloak";
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
                        ret = "Use the cloak to dash through the seams in reality and gather soul from the space in-between.";
                        break;

                    case "GET_SHADOWDASH_1":
                        ret = "to dash while gathering Soul from the environment.";
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

        private FieldInfo GetPrivateField(string fieldName) => HeroController.instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

        private void InvokePrivateMethod(string methodName) => HeroController.instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(HeroController.instance, new object[] { });


        public Vector2 DashDirection;
        public GameObject sharpShadow;
        public PlayMakerFSM superDash;
        public PlayMakerFSM sharpShadowFSM;
        public PlayMakerFSM sharpShadowControl;
        int Multiplier { get; set; } = 1;
        int oldDashDamage { get; set; } = 0;
        int dashDamage { get; set; }
        float dashCooldown { get; set; }
        float dashCooldownStart { get; set; }
        float dashCooldownHasDash { get; set; }
        float oldDashInvulTimer { get; set; } = 0f;
        float dashInvulTimer { get; set; } = 0f;
        float sharpShadowVolume { get; set; }
        int dashCount { get; set; } = 0;
        bool grubberOn = false;
        Vector3 heroPos { get; set; } = Vector3.zero;
    }
}
