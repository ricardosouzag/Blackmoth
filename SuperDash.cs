using System.Collections;
using UnityEngine;
using nv;

namespace BlackmothMod
{
    public class SuperDashHandler : GameStateMachine
    {
        public SpriteRenderer spriteRenderer;
        public Animator unityAnimator;
        public tk2dSpriteAnimator tk2dAnimator;
        public MeshRenderer meshRenderer;
        public GameObject SDCharge;

        public override bool Running
        {
            get
            {
                return gameObject.activeInHierarchy;
            }

            set
            {
                gameObject.SetActive(value);
            }
        }

        protected override void SetupRequiredReferences()
        {
            base.SetupRequiredReferences();
            spriteRenderer = GetComponent<SpriteRenderer>();
            unityAnimator = GetComponent<Animator>();
            tk2dAnimator = GetComponent<tk2dSpriteAnimator>();
            meshRenderer = GetComponent<MeshRenderer>();
            foreach (GameObject go in FindObjectsOfType<GameObject>())
                if (go.name.Contains("SD Charge"))
                    SDCharge = go;
            gameObject.PrintSceneHierarchyTree(gameObject.name);
            game = GameManager.instance;
            inputHandler = game.GetComponent<InputHandler>();
            hero = HeroController.instance;
        }

        protected override void Update()
        {
            if (!game.isPaused && hero.acceptingInput && !PlayerData.instance.GetBool("atBench"))
            {
                wasPressed = inputHandler.inputActions.superDash.WasPressed;
                wasReleased = inputHandler.inputActions.superDash.WasReleased;
                isPressed = inputHandler.inputActions.superDash.IsPressed;
                isNotPressed = !inputHandler.inputActions.superDash.IsPressed;
            }
            if (chargeTimer > 0f)
                chargeTimer -= Time.deltaTime;
            else
                chargeTimer = 0f;
        }

        protected override void RemoveDeprecatedComponents()
        {
            Destroy(HeroController.instance.superDash);
        }

        protected override IEnumerator ExtractReferencesFromExternalSources()
        {
            yield return base.ExtractReferencesFromExternalSources();
            
        }

        protected override IEnumerator Init()
        {
            Dev.Where();
            
            hero.RegainControl();
            hero.SetCState("freezeCharge", false);
            yield return base.Init();

            nextState = Inactive;

            yield break;
        }

        protected virtual IEnumerator Inactive()
        {
            Dev.Where();
            //dunno, play with these here
            while (!wasPressed && !isPressed)
            {
                yield return new WaitForEndOfFrame();
            }

            nextState = CheckCanSuperDash;

            yield break;
        }

        protected virtual IEnumerator CheckCanSuperDash()
        {
            Dev.Where();

            if (hero.CanSuperDash())
                nextState = RelinquishControl;
            else
                nextState = Inactive;

            yield break;
        }

        protected virtual IEnumerator RelinquishControl()
        {
            Dev.Where();

            hero.RelinquishControl();

            nextState = CheckOnGround;

            yield break;
        }

        protected virtual IEnumerator CheckOnGround()
        {
            Dev.Where();

            if (hero.cState.onGround)
                nextState = GroundCharge;
            else
                nextState = WallCharge;
            yield break;
        }

        protected virtual IEnumerator GroundCharge()
        {
            Dev.Where();

            chargeTimer = 0.8f;
            PlayAnimation("SD Charge Ground");
            PlayMakerFSM.BroadcastEvent("SUPERDASH CHARGING G");
            hero.SetCState("freezeCharge", true);
            DoCameraEffect("RumblingFocus");
            PlayMakerFSM.BroadcastEvent("FocusRumble");

            if (chargeTimer <= 0)
            {
                PlayAnimation(SDCharge.GetComponent<tk2dSpriteAnimator>(), "Charge Effect");
                PlayAnimation("SD Fx Charge");

                if (isNotPressed)
                    nextState = Init;
            }

            yield break;
        }

        protected virtual IEnumerator WallCharge()
        {
            

            nextState = CheckOnGround;

            yield break;
        }

        protected virtual void PlayAnimation(string animation)
        {
            PlayAnimation(tk2dAnimator, animation);
        }

        protected virtual void PlayAnimation(tk2dSpriteAnimationClip animation)
        {
            tk2dAnimator.Play(animation);
        }

        public HeroController hero;
        public GameManager game;
        public InputHandler inputHandler;
        public tk2dSpriteAnimationClip groundChargeClip;
        public MeshRenderer chargeEffectMesh;
        public bool wasPressed;
        public bool wasReleased;        
        public bool isPressed;        
        public bool isNotPressed;
        public float chargeTimer;
    }
}