using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UltimaJugada.Tests
{
    public sealed class GameplayIntegrationTests : InputTestFixture
    {
        private GameObject root;
        private FieldView field;
        private MatchHUD hud;
        private MatchEngine engine;
        private Mouse mouse;
        private IInterventionController action;
        private ActionResult? result;

        public override void Setup()
        {
            base.Setup();
            mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.AddDevice<Keyboard>();
        }
        public override void TearDown()
        {
            action?.Dispose();
            if (root != null) Object.DestroyImmediate(root);
            base.TearDown();
        }

        private void Rig(InterventionKind kind)
        {
            root = new GameObject("Test · real gameplay rig");
            field = FieldView.Build(root.transform);
            field.Initialize();
            engine = new MatchEngine(new MatchSettings(), 3);
            while (true)
            {
                engine.Tick(10000f);
                if (engine.State.Phase == MatchPhase.Intervention && engine.State.CurrentIntervention == kind) break;
                if (engine.State.Phase == MatchPhase.Intervention) engine.Resolve(new ActionResult(engine.State.CurrentIntervention, ActionOutcome.Timeout));
                engine.Continue();
            }
            engine.State.Stamina.Recover(100);
            hud = root.AddComponent<MatchHUD>();
            hud.Initialize(engine, () => engine.Continue());
        }
        private PassShotController BallAction(InterventionKind kind)
        {
            Rig(kind);
            var controller = new PassShotController(field, engine, hud, kind, r => { result = r; engine.Resolve(r); });
            action = controller;
            Physics.SyncTransforms();
            return controller;
        }

        private void Aim(PassShotController controller, Vector3 target, float power)
        {
            Vector3 start = controller.BallView.position;
            Set(mouse.position, (Vector2)field.FieldCamera.WorldToScreenPoint(start));
            Press(mouse.leftButton);
            controller.Tick(0.01f);
            Assert.That(controller.Phase, Is.EqualTo(BallActionPhase.Dragging));
            Vector3 dragEnd = start - (target - start).normalized * (engine.Settings.maxDragDistance * power);
            Set(mouse.position, (Vector2)field.FieldCamera.WorldToScreenPoint(dragEnd));
            controller.Tick(0.01f);
            Release(mouse.leftButton);
            controller.Tick(0.01f);
            Assert.That(controller.Phase, Is.EqualTo(BallActionPhase.Precision), "Release must wait for precision before flight");
            Assert.That(controller.BallView.position, Is.EqualTo(start));
        }
        private void CenterAndFly(PassShotController controller)
        {
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, controller.Precision.Target.position));
            Press(mouse.leftButton);
            controller.Tick(0.01f);
            Release(mouse.leftButton);
            Assert.That(controller.RawPrecision, Is.EqualTo(100));
            for (int i = 0; i < 600 && !controller.Finished; i++) controller.Tick(1f / 60f);
            Assert.That(controller.Finished, Is.True);
        }

        [Test]
        public void ActualMouseDragPrecisionAndFlightCompleteAPass()
        {
            var controller = BallAction(InterventionKind.Pass);
            Aim(controller, controller.ReceiverView.position, 0.85f);
            CenterAndFly(controller);
            Assert.That(result.Value.Outcome, Is.EqualTo(ActionOutcome.Success));
            Assert.That(engine.State.Stats.CompletedPasses, Is.EqualTo(1));
            Assert.That(engine.State.Affinity.Value, Is.GreaterThan(50));
        }

        [Test]
        public void ActualPassCanBeIntercepted()
        {
            var controller = BallAction(InterventionKind.Pass);
            Aim(controller, new Vector3(-0.35f, 0.25f, -0.8f), 1f);
            CenterAndFly(controller);
            Assert.That(result.Value.Outcome, Is.EqualTo(ActionOutcome.Intercepted));
        }

        [Test]
        public void LowPowerPassStopsShort()
        {
            var controller = BallAction(InterventionKind.Pass);
            Aim(controller, controller.ReceiverView.position, 0.10f);
            CenterAndFly(controller);
            Assert.That(result.Value.Outcome, Is.EqualTo(ActionOutcome.Short));
        }

        [Test]
        public void VeryLongMouseDragSaturatesPowerAndKeepsBothIndicatorsCompact()
        {
            var controller = BallAction(InterventionKind.Pass);
            Vector3 start = controller.BallView.position;
            Set(mouse.position, (Vector2)field.FieldCamera.WorldToScreenPoint(start));
            Press(mouse.leftButton);
            controller.Tick(0.01f);
            Set(mouse.position, (Vector2)field.FieldCamera.WorldToScreenPoint(start + Vector3.right * 100f));
            controller.Tick(0.01f);
            Assert.That(controller.Power, Is.EqualTo(1f));
            foreach (var line in new[] { controller.AimArrow, controller.DragIndicator })
                for (int i = 0; i < line.positionCount; i++)
                    Assert.That(Vector3.Distance(start, line.GetPosition(i)), Is.LessThanOrEqualTo(engine.Settings.maxAimArrowLength + 0.001f));
            Release(mouse.leftButton);
        }

        [Test]
        public void PrecisionMovesContinuouslyWithinItsPanelAndSpeedDoesNotShortenItsTimer()
        {
            var controller = BallAction(InterventionKind.Pass);
            Aim(controller, controller.ReceiverView.position, 0.85f);
            engine.CycleSimulationSpeed(); engine.CycleSimulationSpeed();
            Vector2 initial = controller.Precision.Target.anchoredPosition;
            bool moved = false;
            for (int i = 0; i < 15; i++)
            {
                controller.Tick(0.08f);
                var target = controller.Precision.Target;
                moved |= Vector2.Distance(target.anchoredPosition, initial) > 2f;
                Rect bounds = ((RectTransform)target.parent).rect;
                Assert.That(Mathf.Abs(target.anchoredPosition.x) + target.rect.width * 0.5f, Is.LessThanOrEqualTo(bounds.width * 0.5f));
                Assert.That(Mathf.Abs(target.anchoredPosition.y) + target.rect.height * 0.5f, Is.LessThanOrEqualTo(bounds.height * 0.5f));
            }
            Assert.That(moved, Is.True);
            Assert.That(controller.Precision.Remaining, Is.EqualTo(0.8f).Within(0.001f));
            CenterAndFly(controller);
            Assert.That(result.Value.Outcome, Is.EqualTo(ActionOutcome.Success));
        }

        [TestCase(true)] [TestCase(false)]
        public void AutomaticTeammateShotCanScoreOrBeSavedThroughRealFlight(bool goal)
        {
            Rig(InterventionKind.Pass);
            for (int seed = 0; seed < 100; seed++)
            {
                engine = new MatchEngine(new MatchSettings { passFollowUpChance = 1f }, seed);
                engine.Tick(10000f);
                engine.Resolve(new ActionResult(InterventionKind.Dribble, ActionOutcome.Timeout));
                engine.Continue(); engine.Tick(10000f);
                engine.Resolve(new ActionResult(InterventionKind.Pass, ActionOutcome.Success, 100));
                if (engine.PendingFollowUp == FollowUpKind.TeammateShot && (engine.TeammateAimX > 0f) == goal) break;
            }
            Assert.That(engine.PendingFollowUp, Is.EqualTo(FollowUpKind.TeammateShot));
            Assert.That(engine.TeammateAimX > 0f, Is.EqualTo(goal));
            engine.Continue();
            int before = engine.State.HomeGoals;
            var controller = new PassShotController(field, engine, hud, InterventionKind.TeammateShot, r => { result = r; engine.Resolve(r); });
            action = controller;
            for (int frame = 0; frame < 400 && !controller.Finished; frame++) controller.Tick(1f / 60f);
            Assert.That(controller.Finished, Is.True);
            Assert.That(result.Value.Outcome, Is.EqualTo(goal ? ActionOutcome.Success : ActionOutcome.Saved));
            Assert.That(engine.State.HomeGoals, Is.EqualTo(before + (goal ? 1 : 0)));
            Assert.That(engine.State.Stats.PlayerGoals, Is.Zero);
            Assert.That(engine.State.Stats.TeammateShots, Is.EqualTo(1));
            Assert.That(engine.State.Stats.Assists, Is.EqualTo(goal ? 1 : 0));
        }

        [UnityTest]
        public IEnumerator RealDribbleChoiceStartsNextControllerAndButtonChangesOnlySimulationSpeed()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            GameObject.Find("JUGAR PARTIDO").GetComponent<Button>().onClick.Invoke();
            yield return null; yield return null;
            var manager = Object.FindAnyObjectByType<MatchSimulationManager>();
            manager.enabled = false;
            engine = manager.Engine;
            GameObject.Find("x1").GetComponent<Button>().onClick.Invoke();
            Assert.That(engine.SimulationSpeed, Is.EqualTo(1.5f));
            GameObject.Find("x1").GetComponent<Button>().onClick.Invoke();
            Assert.That(engine.SimulationSpeed, Is.EqualTo(2f));
            engine.Tick(10000f);
            var controller = (DribbleController)manager.CurrentAction;
            var route = new[] { new Vector2(3.29f, -3.81f), new Vector2(3.09f, -0.59f), new Vector2(0.38f, 4f), new Vector2(-0.35f, 7.5f) };
            int waypoint = 0;
            for (int i = 0; i < 960 && !controller.Finished; i++)
            {
                var position = new Vector2(controller.Player.Position.x, controller.Player.Position.z);
                if (Vector2.Distance(position, route[waypoint]) < 0.15f && waypoint < route.Length - 1) waypoint++;
                controller.TickInput((route[waypoint] - position).normalized, 1f / 60f);
            }
            Assert.That(engine.PendingFollowUp, Is.EqualTo(FollowUpKind.ChoosePassOrShot));
            Assert.That(GameObject.Find("DAR UN PASE"), Is.Not.Null);
            GameObject.Find("TIRO AL ARCO").GetComponent<Button>().onClick.Invoke();
            Assert.That(manager.CurrentAction, Is.TypeOf<PassShotController>());
            Assert.That(engine.State.CurrentIntervention, Is.EqualTo(InterventionKind.Shot));
            Assert.That(engine.State.Minute, Is.EqualTo(6f));
            Assert.That(GameObject.Find("TIRO AL ARCO"), Is.Null);
            yield return SceneManager.LoadSceneAsync("MainMenu");
        }

        [Test]
        public void ActualShotCrossesGoalLineAndUpdatesMatchScore()
        {
            var controller = BallAction(InterventionKind.Shot);
            int before = engine.State.HomeGoals;
            Aim(controller, new Vector3(1.6f, 0.25f, FieldView.GoalLine), 1f);
            CenterAndFly(controller);
            Assert.That(result.Value.Outcome, Is.EqualTo(ActionOutcome.Success));
            Assert.That(engine.State.HomeGoals, Is.EqualTo(before + 1));
            Assert.That(engine.State.Stats.PlayerGoals, Is.EqualTo(1));
        }

        [Test]
        public void ActualShotOutsidePostsMisses()
        {
            var controller = BallAction(InterventionKind.Shot);
            Aim(controller, new Vector3(4f, 0.25f, FieldView.GoalLine), 1f);
            CenterAndFly(controller);
            Assert.That(result.Value.Outcome, Is.EqualTo(ActionOutcome.Missed));
        }

        [Test]
        public void CenterShotIsSavedByKeeper()
        {
            var controller = BallAction(InterventionKind.Shot);
            Aim(controller, new Vector3(0f, 0.25f, FieldView.GoalLine), 1f);
            CenterAndFly(controller);
            Assert.That(result.Value.Outcome, Is.EqualTo(ActionOutcome.Saved));
        }

        [Test]
        public void PrecisionTimeoutLaunchesWithZeroAndOutsideCircleClickIsIgnored()
        {
            var controller = BallAction(InterventionKind.Pass);
            Aim(controller, controller.ReceiverView.position, 1f);
            Vector3 corner = controller.Precision.Target.TransformPoint(new Vector3(100, 100));
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, corner));
            Press(mouse.leftButton);
            controller.Tick(0.1f);
            Assert.That(controller.Phase, Is.EqualTo(BallActionPhase.Precision));
            Release(mouse.leftButton);
            controller.Tick(3f);
            Assert.That(controller.Phase, Is.EqualTo(BallActionPhase.Flight));
            Assert.That(controller.RawPrecision, Is.Zero);
        }

        [Test]
        public void WASDAndArrowsUseTheNewInputSystemAndNormalizeDiagonal()
        {
            var keyboard = Keyboard.current;
            Press(keyboard.wKey); Press(keyboard.dKey);
            Assert.That(GameInput.Move.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(GameInput.Move.x, Is.GreaterThan(0));
            Assert.That(GameInput.Move.y, Is.GreaterThan(0));
            Release(keyboard.wKey); Release(keyboard.dKey);
            Press(keyboard.leftArrowKey); Press(keyboard.downArrowKey);
            Assert.That(GameInput.Move.x, Is.LessThan(0));
            Assert.That(GameInput.Move.y, Is.LessThan(0));
        }

        [Test]
        public void DribbleCanBeatAllThreeFixedDefenders()
        {
            Rig(InterventionKind.Dribble);
            engine.State.Stamina.Spend(13f);
            var controller = new DribbleController(field, engine, hud, r => { result = r; engine.Resolve(r); });
            action = controller;
            var route = new[] { new Vector2(3.29f, -3.81f), new Vector2(3.09f, -0.59f), new Vector2(0.38f, 4f), new Vector2(-0.35f, 7.5f) };
            int waypoint = 0;
            for (int i = 0; i < 960 && !controller.Finished; i++)
            {
                var position = new Vector2(controller.Player.Position.x, controller.Player.Position.z);
                if (Vector2.Distance(position, route[waypoint]) < 0.15f && waypoint < route.Length - 1) waypoint++;
                controller.TickInput((route[waypoint] - position).normalized, 1f / 60f);
            }
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value.Outcome, Is.EqualTo(ActionOutcome.Success));
            Assert.That(engine.State.Stats.SuccessfulDribbles, Is.EqualTo(1));
        }

        [Test]
        public void DribbleCanBeInterceptedWithoutEndingMatch()
        {
            Rig(InterventionKind.Dribble);
            var controller = new DribbleController(field, engine, hud, r => { result = r; engine.Resolve(r); });
            action = controller;
            for (int i = 0; i < 600 && !controller.Finished; i++) controller.TickInput(Vector2.zero, 1f / 60f);
            Assert.That(result.Value.Outcome, Is.EqualTo(ActionOutcome.Intercepted));
            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.Resolution));
            engine.Continue(); engine.Tick(1f);
            Assert.That(engine.State.Minute, Is.GreaterThan(6f));
        }

        [UnityTest]
        public IEnumerator SceneFlowPlaysTwoCompleteMatchesAndResetsEverything()
        {
            // Scene navigation + real timeout paths; precise control outcomes have separate tests above.
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            Assert.That(GameObject.Find("JUGAR PARTIDO"), Is.Not.Null);
            GameObject.Find("JUGAR PARTIDO").GetComponent<Button>().onClick.Invoke();
            yield return null; yield return null;
            for (int matchNumber = 0; matchNumber < 2; matchNumber++)
            {
                var session = GameManager.Instance;
                var engine = session.Match;
                Assert.That(engine.State.HomeGoals + engine.State.AwayGoals, Is.Zero);
                Assert.That(engine.State.Stats.Interventions, Is.Zero);
                Assert.That(engine.State.Affinity.Value, Is.EqualTo(50));
                var manager = Object.FindAnyObjectByType<MatchSimulationManager>();
                // Stop automatic frame updates so the test can advance the same production engine deterministically.
                manager.enabled = false;
                for (int step = 0; step < 20 && engine.State.Phase != MatchPhase.FullTime; step++)
                {
                    engine.Tick(10000f);
                    if (engine.State.Phase == MatchPhase.Intervention)
                    {
                        Assert.That(manager.CurrentAction, Is.Not.Null);
                        // Timeout through production controllers, without assigning a result in the test.
                        for (int frame = 0; frame < 200 && !manager.CurrentAction.Finished; frame++) manager.CurrentAction.Tick(0.25f);
                        Assert.That(manager.CurrentAction.Finished, Is.True);
                    }
                    if (engine.State.Phase == MatchPhase.Resolution || engine.State.Phase == MatchPhase.HalfTime) manager.Continue();
                }
                Assert.That(engine.State.Minute, Is.EqualTo(90));
                Assert.That(engine.State.Stats.Interventions, Is.EqualTo(12));
                manager.enabled = true;
                yield return null; yield return null; yield return null;
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MatchResult"));
                Assert.That(GameObject.Find("JUGAR OTRA VEZ"), Is.Not.Null);
                if (matchNumber == 0)
                {
                    GameObject.Find("JUGAR OTRA VEZ").GetComponent<Button>().onClick.Invoke();
                    yield return null; yield return null;
                    Assert.That(GameManager.Instance.Match, Is.Not.SameAs(engine));
                }
            }
            yield return SceneManager.LoadSceneAsync("MainMenu");
        }
    }
}
