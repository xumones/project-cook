using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using Unity.Cinemachine;
using ProjectCook.Core;
using ProjectCook.Dialogue;
using ProjectCook.Player;

namespace ProjectCook.Cinematics
{
    /// <summary>ประตูเดียวเข้าออก cutscene: โหมดเกม ภาพเข้าออก หยุดโลก ปุ่มข้าม และผลตอนจบ ไม่สนว่าใครสั่งเล่น</summary>
    // ดูจบกับกดข้ามผ่าน Finish ทางเดียวกัน ผลถาวรจึงเหมือนกันเสมอ ห้ามฝากผลถาวรไว้ใน Signal กลาง Timeline
    public class CutscenePlayer : SceneSingleton<CutscenePlayer>
    {
        // ภาพเลื่อนเข้าออกต้องทับกล้องที่ Timeline วางไว้ จึงใช้ลำดับสูงสุด
        private const int BlendOverridePriority = int.MaxValue;

        [Tooltip("Cutscene/Skip (ใช้ Hold interaction)")]
        [SerializeField] private InputActionReference skipAction;

        [Tooltip("Cutscene/Advance ไปต่อบทพูดหรือภาพถัดไป")]
        [SerializeField] private InputActionReference advanceAction;

        [SerializeField] private CinemachineBrain brain;
        [SerializeField] private ScreenFader fader;
        [SerializeField] private CutsceneSkipPrompt skipPrompt;
        [SerializeField] private MediaCutsceneView mediaView;
        [SerializeField] private DevSettingsSO devSettings;

        private CutsceneSO current;
        // null = cutscene แบบสื่อ (Video / Slides)
        private CutsceneStage currentStage;
        private int overrideId = -1;

        // บทพูด: บรรทัดบนจอ (และยังรอให้กดไปต่อไหม) / บรรทัดถัดไปที่ต่อคิวรอ / Timeline ค้างอยู่ / กำลังจบ
        private DialogueLineBehaviour shownLine;
        private bool shownLineWaiting;
        private DialogueLineBehaviour queuedLine;
        private bool holding;
        private bool finishing;

        private bool worldPaused;
        private float savedTimeScale = 1f;
        private bool savedIgnoreTimeScale;
        private bool audioPaused;

        public bool IsPlaying => current != null;

        /// <summary>cutscene จบแล้ว (ดูจบหรือข้าม) และใส่ผลตอนจบแล้ว เกิดก่อนภาพออกจึงยังอยู่ในโหมด Cutscene</summary>
        public event Action<CutsceneSO> Completed;

        private bool LineWaiting => shownLine != null && shownLineWaiting;

        private static CinemachineCamera PlayerCamera => CameraManager.Instance.PlayerCamera;

        /// <summary>stage ว่างได้เฉพาะ cutscene แบบสื่อ</summary>
        public void Play(CutsceneSO cutscene, CutsceneStage stage)
        {
            if (IsPlaying || cutscene == null || (!cutscene.IsMedia && stage == null)) return;
            StartCoroutine(Run(cutscene, stage));
        }

        /// <summary>DialogueLineClip เริ่ม: แสดงบรรทัด หรือต่อคิวถ้าบรรทัดก่อนหน้ายังรอผู้เล่นอ่านอยู่</summary>
        // clip ที่ต่อกันพอดี Timeline อาจเรียก "เริ่มอันใหม่" ก่อน "จบอันเก่า" ในเฟรมเดียวกัน จึงต้องต่อคิวแทนการแสดงทับ
        public void ShowLine(DialogueLineBehaviour line)
        {
            if (!IsPlaying || finishing) return;

            if (LineWaiting) queuedLine = line;
            else Present(line);
        }

        /// <summary>DialogueLineClip จบ: ถ้าบรรทัดยังรอกดอยู่ ค้าง Timeline ไว้ ไม่งั้นไปบรรทัดที่ต่อคิวหรือซ่อน</summary>
        public void EndLine(DialogueLineBehaviour line)
        {
            if (!IsPlaying) return;

            if (line == queuedLine)
            {
                queuedLine = null;
                return;
            }
            if (line != shownLine) return;

            if (LineWaiting && !finishing)
            {
                holding = true;
                SetTimelineSpeed(0f);
                return;
            }
            ShowQueuedOrHide();
        }

        // หยุดเกมกลาง cutscene เต็ม ความเร็วเวลาและเสียงต้องไม่ค้างข้ามการกด Play
        protected override void OnDestroy()
        {
            ResumeWorld();
            base.OnDestroy();
        }

        private IEnumerator Run(CutsceneSO cutscene, CutsceneStage stage)
        {
            current = cutscene;
            currentStage = stage;
            finishing = false;
            bool media = cutscene.IsMedia;
            bool instant = devSettings != null && devSettings.SkipCutscenes;

            GameModeManager.Instance.Enter(this, GameMode.Cutscene);
            // สื่อบังภาพทั้งจอ หันกล้องไปก็ไม่เห็น จึงล็อกไว้กันผู้เล่นหันจนหลงทิศ
            bool lockLook = media || !cutscene.AllowLook;
            if (lockLook) CameraManager.Instance.RequestLookLock(this);

            CutsceneTransition enter = instant ? CutsceneTransition.Cut
                : media ? CutsceneTransition.Fade
                : cutscene.TransitionIn;
            CinemachineVirtualCameraBase firstShot = media ? null : stage.FirstShot;
            if (enter == CutsceneTransition.Fade) yield return fader.FadeOut();
            if (enter == CutsceneTransition.Blend && firstShot != null) yield return BlendOverride(PlayerCamera, firstShot);

            if (media || cutscene.Kind == CutsceneKind.Full) PauseWorld(pauseAudio: media);
            if (media) mediaView.Show();
            else StartTimeline(stage);

            // รอให้ Timeline วางกล้องของมันหนึ่งเฟรมก่อน แล้วค่อยปล่อยภาพที่เลื่อนเข้ามา
            yield return null;
            ReleaseOverride();
            if (enter == CutsceneTransition.Fade) yield return fader.FadeIn();

            if (!instant)
            {
                skipPrompt.Show();
                yield return media ? PlayMedia(cutscene) : WaitForTimeline(stage.Director);
                skipPrompt.Hide();
            }
            Finish(cutscene, stage);

            CutsceneTransition exit = instant ? CutsceneTransition.Cut
                : media || stage.PlayerEndPoint != null ? CutsceneTransition.Fade
                : cutscene.TransitionOut;
            CinemachineVirtualCameraBase lastShot = exit == CutsceneTransition.Blend ? stage.LastShot : null;
            if (exit == CutsceneTransition.Fade) yield return fader.FadeOut();

            // ทับภาพไว้ก่อนปล่อย Timeline กล้องจะไม่กระตุกกลับสายตาผู้เล่นหนึ่งเฟรม
            if (lastShot != null) SetOverride(lastShot, PlayerCamera, 0f);
            if (media) mediaView.Hide();
            else stage.Director.Stop();
            if (!media && stage.PlayerEndPoint != null) LocationLoader.Instance.PlacePlayerAt(stage.PlayerEndPoint.position);
            if (lastShot != null) yield return BlendOverride(lastShot, PlayerCamera);
            ReleaseOverride();
            if (exit == CutsceneTransition.Fade) yield return fader.FadeIn();

            if (lockLook) CameraManager.Instance.ReleaseLookLock(this);
            current = null;
            currentStage = null;
            // ออกจากโหมดเป็นขั้นสุดท้าย ผู้จัดคิวจะเริ่มอันถัดไปได้ทันทีที่กลับมาโหมดเดินสำรวจ
            GameModeManager.Instance.Exit(this);
        }

        private void StartTimeline(CutsceneStage stage)
        {
            PlayableDirector director = stage.Director;
            stage.BindBrain(brain);
            director.extrapolationMode = DirectorWrapMode.Hold;
            director.timeUpdateMode = worldPaused ? DirectorUpdateMode.UnscaledGameTime : DirectorUpdateMode.GameTime;
            director.Play();
        }

        private IEnumerator WaitForTimeline(PlayableDirector director)
        {
            // บรรทัดสุดท้ายที่จบพอดีท้าย Timeline ยังต้องรอผู้เล่นกดก่อนจบ
            while ((director.time < director.duration || LineWaiting) && !SkipRequested())
            {
                if (advanceAction.WasPressedThisFrame()) DialogueRunner.Instance.AdvanceLine();
                yield return null;
            }
        }

        private IEnumerator PlayMedia(CutsceneSO cutscene)
        {
            if (cutscene.Content == CutsceneContent.Video)
            {
                mediaView.PlayVideo(cutscene.Video);
                while (!mediaView.VideoFinished && !SkipRequested()) yield return null;
                yield break;
            }

            foreach (MediaSlide slide in cutscene.Slides)
            {
                mediaView.ShowSlide(slide.Image);
                bool advanced = false;
                if (slide.HasText) DialogueRunner.Instance.PlayLine(slide.Speaker, slide.Text, () => advanced = true);
                else DialogueRunner.Instance.HideLine();

                while (!advanced)
                {
                    if (SkipRequested()) yield break;

                    if (advanceAction.WasPressedThisFrame())
                    {
                        if (slide.HasText) DialogueRunner.Instance.AdvanceLine();
                        else advanced = true;
                    }
                    yield return null;
                }
            }
        }

        // อ่านปุ่มข้ามและอัปเดตแถบกดค้างในจังหวะเดียวกัน
        private bool SkipRequested()
        {
            skipPrompt.SetProgress(skipAction.HoldProgress());
            return skipAction.WasPerformedThisFrame();
        }

        // ดูจบและกดข้ามมาถึงที่นี่ทางเดียว ทุกแถวจึงได้ท่าสุดท้ายและผลตอนจบเหมือนกันเสมอ
        private void Finish(CutsceneSO cutscene, CutsceneStage stage)
        {
            finishing = true;
            if (stage != null)
            {
                stage.Director.time = stage.Director.duration;
                stage.Director.Evaluate();
            }
            shownLine = null;
            queuedLine = null;
            holding = false;
            DialogueRunner.Instance.HideLine();
            ResumeWorld();

            Completed?.Invoke(cutscene);
            if (!string.IsNullOrEmpty(cutscene.FinishEvent))
            {
                MessageBus<WorldEventTriggered>.Publish(new WorldEventTriggered(cutscene.FinishEvent));
            }
        }

        // กดก่อน clip จบ = รอซ่อนตอน clip จบ (เว้นแต่มีบรรทัดถัดไปรออยู่), กดตอนค้าง = ไปต่อแล้วเล่นต่อ
        private void HandleLineAdvanced()
        {
            shownLineWaiting = false;
            if (!holding)
            {
                if (queuedLine != null) ShowQueuedOrHide();
                return;
            }

            holding = false;
            ShowQueuedOrHide();
            SetTimelineSpeed(1f);
        }

        private void Present(DialogueLineBehaviour line)
        {
            shownLine = line;
            shownLineWaiting = line.WaitForAdvance;
            DialogueRunner.Instance.PlayLine(line.Speaker, line.Line, HandleLineAdvanced);
        }

        private void ShowQueuedOrHide()
        {
            DialogueLineBehaviour next = queuedLine;
            queuedLine = null;
            shownLine = null;

            if (next != null) Present(next);
            else DialogueRunner.Instance.HideLine();
        }

        // ค้างด้วยความเร็ว 0 ไม่ใช่ Pause() เพราะ Pause() ทำให้ Timeline ปล่อยกล้องกับท่าทาง
        private void SetTimelineSpeed(float speed)
        {
            PlayableGraph graph = currentStage.Director.playableGraph;
            if (graph.IsValid()) graph.GetRootPlayable(0).SetSpeed(speed);
        }

        // เลื่อนกล้องด้วยกองกล้องของ Brain ใช้เวลาและเส้นโค้งจากไฟล์ blends ของกล้องหลัก ปรับที่เดียวได้ทั้งเกม
        private IEnumerator BlendOverride(ICinemachineCamera from, ICinemachineCamera to)
        {
            CinemachineBlendDefinition blend = CinemachineBlenderSettings.LookupBlend(
                from, to, brain.DefaultBlend, brain.CustomBlends, brain);

            float duration = blend.BlendTime;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                SetOverride(from, to, blend.BlendCurve.Evaluate(elapsed / duration));
                yield return null;
            }
            SetOverride(from, to, 1f);
        }

        private void SetOverride(ICinemachineCamera from, ICinemachineCamera to, float weight) =>
            overrideId = brain.SetCameraOverride(overrideId, BlendOverridePriority, from, to, weight, Time.unscaledDeltaTime);

        private void ReleaseOverride()
        {
            if (overrideId < 0) return;

            brain.ReleaseCameraOverride(overrideId);
            overrideId = -1;
        }

        // cutscene เต็มและสื่อหยุดเวลาทั้งเกม ของที่ต้องเดินต่อ (Timeline, กล้อง, ตัวพิมพ์) ใช้เวลาจริง สื่อหยุดเสียงเกมด้วย
        // ตัวนี้เป็นที่เดียวที่แตะ Time.timeScale และ AudioListener.pause และต้องคืนค่าทุกทางออก
        private void PauseWorld(bool pauseAudio)
        {
            savedTimeScale = Time.timeScale;
            savedIgnoreTimeScale = brain.IgnoreTimeScale;
            Time.timeScale = 0f;
            brain.IgnoreTimeScale = true;
            worldPaused = true;

            if (!pauseAudio) return;
            AudioListener.pause = true;
            audioPaused = true;
        }

        private void ResumeWorld()
        {
            if (audioPaused)
            {
                AudioListener.pause = false;
                audioPaused = false;
            }
            if (!worldPaused) return;

            Time.timeScale = savedTimeScale;
            if (brain != null) brain.IgnoreTimeScale = savedIgnoreTimeScale;
            worldPaused = false;
        }
    }
}
