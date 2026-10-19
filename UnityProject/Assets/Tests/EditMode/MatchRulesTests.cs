using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UltimaJugada.Tests
{
    public sealed class MatchRulesTests
    {
        [Test]
        public void NewMatchStartsClean()
        {
            var s = new MatchEngine(new MatchSettings(), 7).State;
            Assert.That(s.Minute, Is.Zero);
            Assert.That(s.HomeGoals + s.AwayGoals, Is.Zero);
            Assert.That(s.Stats.Interventions, Is.Zero);
            Assert.That(s.Stamina.Value, Is.EqualTo(100));
            Assert.That(s.Affinity.Value, Is.EqualTo(50));
        }

        [TestCase(1)] [TestCase(42)] [TestCase(2026)]
        public void WholeMatchHasTwelveBaseActionsHalftimeAndMonotonicClock(int seed)
        {
            var engine = new MatchEngine(new MatchSettings(), seed);
            var actions = new List<InterventionKind>();
            float last = 0f;
            int halftimes = 0;
            for (int step = 0; step < 20 && engine.State.Phase != MatchPhase.FullTime; step++)
            {
                engine.Tick(10000f);
                Assert.That(engine.State.Minute, Is.GreaterThanOrEqualTo(last));
                last = engine.State.Minute;
                if (engine.State.Phase == MatchPhase.HalfTime) { halftimes++; engine.Continue(); }
                else if (engine.State.Phase == MatchPhase.Intervention)
                {
                    actions.Add(engine.State.CurrentIntervention);
                    float paused = engine.State.Minute;
                    engine.Tick(10000f);
                    Assert.That(engine.State.Minute, Is.EqualTo(paused));
                    var result = new ActionResult(engine.State.CurrentIntervention, ActionOutcome.Timeout);
                    Assert.That(engine.Resolve(result), Is.True);
                    Assert.That(engine.Resolve(result), Is.False, "A result cannot be counted twice");
                    engine.Tick(10f);
                    Assert.That(engine.State.Minute, Is.EqualTo(paused));
                    engine.Continue();
                    Assert.That(engine.State.Minute, Is.EqualTo(paused));
                }
            }
            Assert.That(engine.State.Minute, Is.EqualTo(90f));
            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.FullTime));
            Assert.That(halftimes, Is.EqualTo(1));
            Assert.That(actions.Count, Is.EqualTo(12));
            Assert.That(actions.Distinct().Count(), Is.EqualTo(3));
            Assert.That(engine.State.Stats.PlayerGoals, Is.Zero);
            int count = engine.State.Events.Count;
            engine.Tick(10000f);
            engine.Continue();
            Assert.That(engine.State.Events.Count, Is.EqualTo(count));
        }

        [Test]
        public void InvalidKindCannotResolveOrChangePossession()
        {
            var engine = new MatchEngine(new MatchSettings(), 3);
            engine.Tick(10000f);
            Assert.That(engine.Resolve(new ActionResult(InterventionKind.Shot, ActionOutcome.Success)), Is.False);
            Assert.That(engine.State.Stats.Shots, Is.Zero);
            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.Intervention));
        }

        [Test]
        public void AimingTimeoutDoesNotCountAsAnExecutedPassOrShot()
        {
            var e = new MatchEngine(new MatchSettings(), 3);
            for (int i = 0; i < 3; i++)
            {
                e.Tick(10000f);
                e.Resolve(new ActionResult(e.State.CurrentIntervention, ActionOutcome.Timeout));
                e.Continue();
            }
            Assert.That(e.State.Stats.PassAttempts, Is.Zero);
            Assert.That(e.State.Stats.Shots, Is.Zero);
            Assert.That(e.State.Stats.PrecisionAttempts, Is.Zero);
            Assert.That(e.State.Stats.Interventions, Is.EqualTo(3));
        }

        [Test]
        public void MaximumShotCanStillReachGoalUnderExtremeFatigue()
        {
            var stamina = new StaminaSystem(); stamina.Spend(100f);
            var settings = new MatchSettings();
            Ballistics.ApplyPrecision(Vector3.forward, settings.maxShotSpeed, 100, 1, stamina, settings, out _, out float speed);
            Assert.That(speed * speed / (2f * settings.ballDeceleration), Is.GreaterThan(FieldView.GoalLine + 2.8f));
        }

        [Test]
        public void FailedActionsChangeStatsAffinityAndPossession()
        {
            var e = new MatchEngine(new MatchSettings(), 3);
            e.Tick(10000);
            e.Resolve(new ActionResult(InterventionKind.Dribble, ActionOutcome.Intercepted));
            Assert.That(e.State.Stats.Interceptions, Is.EqualTo(1));
            Assert.That(e.State.Possession, Is.EqualTo("Deportivo Cordillera"));
            e.Continue(); e.Tick(10000);
            e.Resolve(new ActionResult(InterventionKind.Pass, ActionOutcome.Short, 10));
            Assert.That(e.State.Affinity.Value, Is.EqualTo(46));
            Assert.That(e.State.Stats.CompletedPasses, Is.Zero);
            e.Continue(); e.Tick(10000);
            int before = e.State.HomeGoals;
            e.Resolve(new ActionResult(InterventionKind.Shot, ActionOutcome.Missed, 30));
            Assert.That(e.State.HomeGoals, Is.EqualTo(before));
            Assert.That(e.State.Stats.Shots, Is.EqualTo(1));
            Assert.That(e.State.Stats.AveragePrecision, Is.EqualTo(20));
        }

        [Test]
        public void ExtremeFatigueIsGuaranteedByLateDribbleEvenWithNoRunning()
        {
            var e = new MatchEngine(new MatchSettings(), 6);
            for (int i = 0; i < 24; i++)
            {
                e.Tick(10000);
                if (e.State.Minute == 80f)
                {
                    Assert.That(e.State.Stamina.Extreme, Is.True);
                    return;
                }
                if (e.State.Phase == MatchPhase.HalfTime) e.Continue();
                else if (e.State.Phase == MatchPhase.Intervention)
                { e.Resolve(new ActionResult(e.State.CurrentIntervention, ActionOutcome.Timeout)); e.Continue(); }
            }
            Assert.Fail("The late dribble was not scheduled");
        }

        [TestCase(0, 1f)] [TestCase(1, 1.5f)] [TestCase(2, 2f)] [TestCase(3, 1f)]
        public void SimulationSpeedCyclesAndScalesClockWithoutSkippingAnIntervention(int clicks, float multiplier)
        {
            var e = new MatchEngine(new MatchSettings(), 7);
            for (int i = 0; i < clicks; i++) e.CycleSimulationSpeed();
            Assert.That(e.SimulationSpeed, Is.EqualTo(multiplier));
            e.Tick(1f);
            Assert.That(e.State.Minute, Is.EqualTo(e.Settings.simulatedMinutesPerSecond * multiplier).Within(0.0001f));
            e.Tick(10000f);
            Assert.That(e.State.Minute, Is.EqualTo(6f));
            e.Tick(10000f);
            Assert.That(e.State.Minute, Is.EqualTo(6f), "Speed must never advance the intervention clock");
        }

        [TestCase(InterventionKind.Pass)] [TestCase(InterventionKind.Shot)]
        public void SuccessfulDribbleImmediatelyOffersEitherFollowUpAtTheSameMinute(InterventionKind choice)
        {
            var e = new MatchEngine(new MatchSettings(), 3);
            e.Tick(10000f);
            e.Resolve(new ActionResult(InterventionKind.Dribble, ActionOutcome.Success));
            Assert.That(e.PendingFollowUp, Is.EqualTo(FollowUpKind.ChoosePassOrShot));
            e.Continue(); e.Tick(10000f);
            Assert.That(e.State.Minute, Is.EqualTo(6f));
            Assert.That(e.ChooseFollowUp(InterventionKind.Dribble), Is.False);
            Assert.That(e.ChooseFollowUp(choice), Is.True);
            Assert.That(e.State.CurrentIntervention, Is.EqualTo(choice));
            Assert.That(e.State.Phase, Is.EqualTo(MatchPhase.Intervention));
            Assert.That(e.State.IsChainedIntervention, Is.True);
            Assert.That(e.State.Minute, Is.EqualTo(6f));
            Assert.That(e.ChooseFollowUp(choice), Is.False);
        }

        private static MatchEngine ResolveFirstPass(int seed, ActionOutcome outcome, float chance = 0.9f)
        {
            var e = new MatchEngine(new MatchSettings { passFollowUpChance = chance }, seed);
            e.Tick(10000f);
            e.Resolve(new ActionResult(InterventionKind.Dribble, ActionOutcome.Timeout));
            e.Continue(); e.Tick(10000f);
            e.Resolve(new ActionResult(InterventionKind.Pass, outcome, 100));
            return e;
        }

        [Test]
        public void SuccessfulPassStronglyFavorsImmediateShotsAndCanChooseBothFinishers()
        {
            int player = 0, teammate = 0;
            for (int seed = 0; seed < 1000; seed++)
            {
                var e = ResolveFirstPass(seed, ActionOutcome.Success);
                if (e.PendingFollowUp == FollowUpKind.PlayerShot) player++;
                else if (e.PendingFollowUp == FollowUpKind.TeammateShot) teammate++;
                else continue;
                e.Continue();
                Assert.That(e.State.Phase, Is.EqualTo(MatchPhase.Intervention));
                Assert.That(e.State.Minute, Is.EqualTo(13f));
                Assert.That(e.State.IsChainedIntervention, Is.True);
            }
            Assert.That(player + teammate, Is.InRange(840, 960));
            Assert.That(player, Is.GreaterThan(300));
            Assert.That(teammate, Is.GreaterThan(250));
            Assert.That(ResolveFirstPass(3, ActionOutcome.Intercepted, 1f).PendingFollowUp, Is.EqualTo(FollowUpKind.None));
            Assert.That(ResolveFirstPass(3, ActionOutcome.Success, 0f).PendingFollowUp, Is.EqualTo(FollowUpKind.None));
        }

        [TestCase(ActionOutcome.Success)] [TestCase(ActionOutcome.Saved)]
        public void TeammateShotCountsTeamGoalAndAssistWithoutPlayerShotOrPrecision(ActionOutcome outcome)
        {
            MatchEngine e = null;
            for (int seed = 0; seed < 50; seed++)
            {
                e = ResolveFirstPass(seed, ActionOutcome.Success, 1f);
                if (e.PendingFollowUp == FollowUpKind.TeammateShot) break;
            }
            Assert.That(e.PendingFollowUp, Is.EqualTo(FollowUpKind.TeammateShot));
            e.Continue();
            int goals = e.State.HomeGoals;
            var result = new ActionResult(InterventionKind.TeammateShot, outcome);
            Assert.That(e.Resolve(result), Is.True);
            Assert.That(e.Resolve(result), Is.False);
            Assert.That(e.State.Stats.TeammateShots, Is.EqualTo(1));
            Assert.That(e.State.Stats.Shots, Is.Zero);
            Assert.That(e.State.Stats.PlayerGoals, Is.Zero);
            Assert.That(e.State.Stats.PrecisionAttempts, Is.EqualTo(1));
            Assert.That(e.State.Stats.Interventions, Is.EqualTo(2));
            Assert.That(e.State.HomeGoals, Is.EqualTo(goals + (outcome == ActionOutcome.Success ? 1 : 0)));
            Assert.That(e.State.Stats.Assists, Is.EqualTo(outcome == ActionOutcome.Success ? 1 : 0));
        }

        [TestCase(1)] [TestCase(42)] [TestCase(2026)]
        public void WholeSuccessfulMatchWithChainsStillReachesHalftimeAndNinetyWithoutLosingScheduledActions(int seed)
        {
            var e = new MatchEngine(new MatchSettings { passFollowUpChance = 1f }, seed);
            int scheduled = 0, halftime = 0;
            for (int step = 0; step < 80 && e.State.Phase != MatchPhase.FullTime; step++)
            {
                e.Tick(10000f);
                if (e.State.Phase == MatchPhase.HalfTime) { halftime++; e.Continue(); }
                else if (e.State.Phase == MatchPhase.Intervention)
                {
                    if (!e.State.IsChainedIntervention) scheduled++;
                    e.Resolve(new ActionResult(e.State.CurrentIntervention, ActionOutcome.Success, e.State.CurrentIntervention == InterventionKind.TeammateShot ? -1 : 100));
                    if (e.PendingFollowUp == FollowUpKind.ChoosePassOrShot) e.ChooseFollowUp(InterventionKind.Pass);
                    else e.Continue();
                }
            }
            Assert.That(e.State.Phase, Is.EqualTo(MatchPhase.FullTime));
            Assert.That(e.State.Minute, Is.EqualTo(90f));
            Assert.That(scheduled, Is.EqualTo(12));
            Assert.That(halftime, Is.EqualTo(1));
            Assert.That(e.State.Stats.Interventions, Is.GreaterThan(12));
            Assert.That(e.State.HomeGoals, Is.GreaterThanOrEqualTo(e.State.Stats.PlayerGoals + e.State.Stats.TeammateGoals));
        }

        [Test]
        public void ExtremeFatigueReducesSpeedPrecisionAndPowerAndClamps()
        {
            var stamina = new StaminaSystem();
            float freshSpeed = stamina.SpeedMultiplier;
            stamina.Spend(80);
            Assert.That(stamina.Extreme, Is.True);
            Assert.That(stamina.SpeedMultiplier, Is.LessThan(freshSpeed));
            Assert.That(stamina.EffectivePrecision(100), Is.EqualTo(78));
            Assert.That(stamina.PowerMultiplier, Is.LessThan(1));
            stamina.Spend(1000); Assert.That(stamina.Value, Is.Zero);
            stamina.Recover(1000); Assert.That(stamina.Value, Is.EqualTo(100));
        }

        [Test]
        public void AffinityChangesActualReceptionRadius()
        {
            var affinity = new AffinitySystem();
            affinity.Change(-1000);
            Assert.That(affinity.ReceptionRadius, Is.EqualTo(0.55f).Within(0.0001f));
            Vector3 from = new Vector3(-2f, 0, 0.95f), to = new Vector3(2f, 0, 0.95f);
            Assert.That(Ballistics.SegmentCircle(from, to, Vector3.zero, affinity.ReceptionRadius, out _), Is.False);
            affinity.Change(1000);
            Assert.That(Ballistics.SegmentCircle(from, to, Vector3.zero, affinity.ReceptionRadius, out _), Is.True);
            Assert.That(affinity.Value, Is.EqualTo(100));
        }

        [TestCase(0f, 100)] [TestCase(50f, 50)] [TestCase(100f, 0)] [TestCase(150f, 0)]
        public void PrecisionDependsOnDistanceToCenter(float distance, int expected)
            => Assert.That(Ballistics.PrecisionScore(new Vector2(distance, 0), 100f), Is.EqualTo(expected));

        [TestCase(100f, 0f, 12f)] [TestCase(50f, 6f, 10.5f)] [TestCase(0f, 12f, 9f)]
        public void PrecisionChangesBothDirectionAndSpeed(float score, float angle, float expectedSpeed)
        {
            Ballistics.ApplyPrecision(Vector3.forward, 12, score, 1, new StaminaSystem(), new MatchSettings(), out var dir, out float speed);
            Assert.That(Vector3.Angle(Vector3.forward, dir), Is.EqualTo(angle).Within(0.001f));
            Assert.That(speed, Is.EqualTo(expectedSpeed).Within(0.001f));
        }

        [Test]
        public void PoorPrecisionCanDeviateToEitherSide()
        {
            Ballistics.ApplyPrecision(Vector3.forward, 12, 0, 1, new StaminaSystem(), new MatchSettings(), out var right, out _);
            Ballistics.ApplyPrecision(Vector3.forward, 12, 0, -1, new StaminaSystem(), new MatchSettings(), out var left, out _);
            Assert.That(right.x, Is.GreaterThan(0));
            Assert.That(left.x, Is.LessThan(0));
        }

        [Test]
        public void BallTravelIsFrameRateIndependentAndNeverReverses()
        {
            var fine = new BallController(Vector3.zero, Vector3.forward, 12, 4);
            var coarse = new BallController(Vector3.zero, Vector3.forward, 12, 4);
            for (int i = 0; i < 600; i++) fine.Step(0.01f);
            coarse.Step(6f);
            Assert.That(fine.Position.z, Is.EqualTo(18f).Within(0.001f));
            Assert.That(coarse.Position.z, Is.EqualTo(fine.Position.z).Within(0.001f));
            fine.Step(100f);
            Assert.That(fine.Position.z, Is.EqualTo(18f).Within(0.001f));
        }

        [Test]
        public void SweptContactFindsObjectsBetweenFrames()
        {
            Assert.That(Ballistics.SegmentCircle(new Vector3(0, 0, -5), new Vector3(0, 0, 5), Vector3.zero, 0.5f, out float t), Is.True);
            Assert.That(t, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(Ballistics.SegmentCircle(Vector3.zero, Vector3.zero, Vector3.zero, 0.5f, out t), Is.True);
            Assert.That(t, Is.Zero);
        }

        [Test]
        public void GoalRequiresCrossingBetweenThePosts()
        {
            Assert.That(Ballistics.GoalCrossing(new Vector3(1, 0, 7), new Vector3(1, 0, 8.5f), out _), Is.True);
            Assert.That(Ballistics.GoalCrossing(new Vector3(3, 0, 7), new Vector3(3, 0, 8.5f), out _), Is.False);
            Assert.That(Ballistics.GoalCrossing(new Vector3(0, 0, 8.5f), new Vector3(0, 0, 7), out _), Is.False);
        }

        [Test]
        public void DiagonalMovementIsNormalizedAndFatigueChangesDistance()
        {
            var straight = new PlayerController(Vector3.zero);
            var diagonal = new PlayerController(Vector3.zero);
            var tired = new PlayerController(Vector3.zero);
            straight.Tick(Vector2.up, 0.1f, new StaminaSystem());
            diagonal.Tick(Vector2.one, 0.1f, new StaminaSystem());
            var stamina = new StaminaSystem(); stamina.Spend(90);
            tired.Tick(Vector2.up, 0.1f, stamina);
            Assert.That(diagonal.Position.magnitude, Is.EqualTo(straight.Position.magnitude).Within(0.001f));
            Assert.That(tired.Position.magnitude, Is.LessThan(straight.Position.magnitude));
        }

        [Test]
        public void FixedRolesProduceDifferentDefensiveTargets()
        {
            var pressure = new DefenderAI(Vector3.zero, DefenderRole.Pressure);
            var anticipation = new DefenderAI(Vector3.zero, DefenderRole.Anticipation);
            var cover = new DefenderAI(Vector3.zero, DefenderRole.Cover);
            var player = new Vector3(0, 0, -3);
            pressure.Tick(player, Vector3.right, 0.1f);
            anticipation.Tick(player, Vector3.right, 0.1f);
            cover.Tick(player, Vector3.right, 0.1f);
            Assert.That(pressure.Position.x, Is.Zero);
            Assert.That(anticipation.Position.x, Is.GreaterThan(0));
            Assert.That(pressure.Role, Is.EqualTo(DefenderRole.Pressure));
            Assert.That(cover.Role, Is.EqualTo(DefenderRole.Cover));
        }

        [TestCase(2, 1, "VICTORIA")] [TestCase(1, 1, "EMPATE")] [TestCase(0, 2, "DERROTA")]
        public void VerdictCoversAllScores(int home, int away, string expected)
        {
            // Score setters stay internal to the runtime assembly; construct real scoring via events.
            var s = new GameState();
            typeof(GameState).GetProperty(nameof(GameState.HomeGoals)).SetValue(s, home);
            typeof(GameState).GetProperty(nameof(GameState.AwayGoals)).SetValue(s, away);
            Assert.That(s.Verdict, Is.EqualTo(expected));
        }
    }
}
