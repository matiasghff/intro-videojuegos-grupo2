using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UltimaJugada.Tests
{
    public sealed class VisualSmokeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator CaptureSuccessfulDribblePassAndTeammateFinish()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var session = GameManager.Instance;
            int chosenSeed = -1;
            for (int seed = 0; seed < 200; seed++)
            {
                var probe = new MatchEngine(session.Settings, seed);
                probe.Tick(10000f);
                probe.Resolve(new ActionResult(InterventionKind.Dribble, ActionOutcome.Success));
                probe.ChooseFollowUp(InterventionKind.Pass);
                probe.Resolve(new ActionResult(InterventionKind.Pass, ActionOutcome.Success, 100));
                if (probe.PendingFollowUp == FollowUpKind.TeammateShot && probe.TeammateAimX > 0f) { chosenSeed = seed; break; }
            }
            Assert.That(chosenSeed, Is.GreaterThanOrEqualTo(0));
            session.NewMatch(chosenSeed);
            yield return SceneManager.LoadSceneAsync("Match");
            yield return null;
            var manager = Object.FindAnyObjectByType<MatchSimulationManager>();
            manager.enabled = false;
            var engine = manager.Engine;
            engine.Tick(10000f);
            var dribble = (DribbleController)manager.CurrentAction;
            var route = new[] { new Vector2(3.29f, -3.81f), new Vector2(3.09f, -0.59f), new Vector2(0.38f, 4f), new Vector2(-0.35f, 7.5f) };
            int waypoint = 0;
            for (int i = 0; i < 960 && !dribble.Finished; i++)
            {
                var position = new Vector2(dribble.Player.Position.x, dribble.Player.Position.z);
                if (Vector2.Distance(position, route[waypoint]) < 0.15f && waypoint < route.Length - 1) waypoint++;
                dribble.TickInput((route[waypoint] - position).normalized, 1f / 60f);
            }
            Assert.That(engine.PendingFollowUp, Is.EqualTo(FollowUpKind.ChoosePassOrShot));
            yield return Capture("06-dribble-choice");
            GameObject.Find("DAR UN PASE").GetComponent<Button>().onClick.Invoke();
            var pass = (PassShotController)manager.CurrentAction;
            var field = manager.GetComponentInChildren<FieldView>();
            Physics.SyncTransforms();
            Vector3 start = pass.BallView.position;
            Set(mouse.position, (Vector2)field.FieldCamera.WorldToScreenPoint(start));
            Press(mouse.leftButton);
            yield return null;
            pass.Tick(0.01f);
            Set(mouse.position, (Vector2)field.FieldCamera.WorldToScreenPoint(start - (pass.ReceiverView.position - start).normalized * 3.4f));
            yield return null;
            pass.Tick(0.01f);
            Release(mouse.leftButton);
            yield return null;
            pass.Tick(0.01f);
            Assert.That(pass.Phase, Is.EqualTo(BallActionPhase.Precision));
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, pass.Precision.Target.position));
            Press(mouse.leftButton);
            yield return null;
            pass.Tick(0.01f);
            Release(mouse.leftButton);
            for (int i = 0; i < 400 && !pass.Finished; i++) pass.Tick(1f / 60f);
            Assert.That(engine.PendingFollowUp, Is.EqualTo(FollowUpKind.TeammateShot));
            manager.Continue();
            for (int i = 0; i < 4; i++) manager.CurrentAction.Tick(0.25f);
            manager.CurrentAction.Tick(0.1f);
            yield return Capture("07-teammate-shot");
            for (int i = 0; i < 200 && !manager.CurrentAction.Finished; i++) manager.CurrentAction.Tick(1f / 60f);
            Assert.That(engine.State.Stats.Assists, Is.EqualTo(1));
            Assert.That(engine.State.Minute, Is.EqualTo(6f));
            yield return Capture("08-teammate-goal");
            yield return SceneManager.LoadSceneAsync("MainMenu");
        }

        [UnityTest]
        public IEnumerator CaptureRenderedMenuFieldPrecisionAndResult()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            yield return Capture("01-main-menu");
            GameObject.Find("JUGAR PARTIDO").GetComponent<Button>().onClick.Invoke();
            yield return null; yield return null;
            var manager = Object.FindAnyObjectByType<MatchSimulationManager>();
            manager.enabled = false;
            var engine = manager.Engine;
            engine.Tick(10000f);
            yield return Capture("02-dribble");
            for (int i = 0; i < 80 && !manager.CurrentAction.Finished; i++) manager.CurrentAction.Tick(0.25f);
            manager.Continue();
            engine.Tick(10000f);
            var pass = (PassShotController)manager.CurrentAction;
            var field = manager.GetComponentInChildren<FieldView>();
            Physics.SyncTransforms();
            Vector3 start = pass.BallView.position;
            Set(mouse.position, (Vector2)field.FieldCamera.WorldToScreenPoint(start));
            Press(mouse.leftButton);
            yield return null; // UnityTest input events are processed by the next player-loop update.
            pass.Tick(0.01f);
            Assert.That(pass.Phase, Is.EqualTo(BallActionPhase.Dragging), $"Selection: clicked={GameInput.Clicked}, mouse={GameInput.MousePosition}, viewport={field.FieldCamera.pixelRect}, ball={field.FieldCamera.WorldToScreenPoint(start)}");
            Vector3 drag = start - (pass.ReceiverView.position - start).normalized * 3.8f;
            Set(mouse.position, (Vector2)field.FieldCamera.WorldToScreenPoint(drag));
            yield return null;
            pass.Tick(0.01f);
            yield return Capture("03-pass-aim");
            Release(mouse.leftButton);
            yield return null;
            pass.Tick(0.01f);
            Assert.That(pass.Phase, Is.EqualTo(BallActionPhase.Precision));
            yield return Capture("04-precision");
            // Finish through normal controller timeouts, then let the manager load the result scene.
            for (int i = 0; i < 100 && !manager.CurrentAction.Finished; i++) manager.CurrentAction.Tick(0.25f);
            manager.Continue();
            for (int step = 0; step < 20 && engine.State.Phase != MatchPhase.FullTime; step++)
            {
                engine.Tick(10000f);
                if (engine.State.Phase == MatchPhase.Intervention)
                    for (int i = 0; i < 120 && !manager.CurrentAction.Finished; i++) manager.CurrentAction.Tick(0.25f);
                if (engine.State.Phase == MatchPhase.HalfTime || engine.State.Phase == MatchPhase.Resolution) manager.Continue();
            }
            manager.enabled = true;
            yield return null; yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MatchResult"));
            yield return Capture("05-result");
        }

        private static IEnumerator Capture(string name)
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/TestScreenshots"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, name + ".png");
            // Batch has no presented Game view. Render the actual field and Canvas offscreen.
            var canvas = Object.FindAnyObjectByType<Canvas>();
            var uiObject = new GameObject("Offscreen test camera", typeof(Camera));
            var uiCamera = uiObject.GetComponent<Camera>();
            uiCamera.enabled = false;
            uiCamera.orthographic = true;
            uiCamera.clearFlags = CameraClearFlags.SolidColor;
            uiCamera.backgroundColor = UIFactory.Background;
            uiCamera.cullingMask = 1 << 5;
            var output = new RenderTexture(1920, 1080, 24);
            uiCamera.targetTexture = output;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 2f;
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
            var field = Object.FindAnyObjectByType<FieldView>();
            RenderTexture fieldOutput = null;
            GameObject imageObject = null;
            if (field != null)
            {
                var camera = field.FieldCamera;
                Rect viewport = camera.rect;
                fieldOutput = new RenderTexture(1344, 745, 24);
                camera.targetTexture = fieldOutput;
                camera.rect = new Rect(0, 0, 1, 1);
                int mask = camera.cullingMask;
                camera.cullingMask = ~(1 << 5);
                camera.Render();
                camera.cullingMask = mask;
                camera.targetTexture = null;
                camera.rect = viewport;
                var rt = UIFactory.Rect("Offscreen field", canvas.transform, viewport.min, viewport.max);
                rt.SetAsFirstSibling();
                rt.gameObject.layer = 5;
                rt.gameObject.AddComponent<RawImage>().texture = fieldOutput;
                imageObject = rt.gameObject;
            }
            Canvas.ForceUpdateCanvases();
            uiCamera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = output;
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            RenderTexture.active = previous;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            uiCamera.targetTexture = null;
            if (imageObject != null) Object.DestroyImmediate(imageObject);
            Object.DestroyImmediate(uiObject);
            Object.DestroyImmediate(pixels);
            output.Release(); Object.DestroyImmediate(output);
            if (fieldOutput != null) { fieldOutput.Release(); Object.DestroyImmediate(fieldOutput); }
            Canvas.ForceUpdateCanvases();
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(10000), "Rendered snapshot should contain geometry and text");
            yield return null;
        }
    }
}
