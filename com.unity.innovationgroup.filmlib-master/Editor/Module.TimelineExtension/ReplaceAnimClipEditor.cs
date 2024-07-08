using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;

namespace MWU.Timeline
{
    public class ReplaceAnimClipEditor : EditorWindow
    {
        List<TimelineClip> selectedClips;
        AnimationClip newClip = null;

        public static void ShowWindow()
        {
            var win = EditorWindow.GetWindow(typeof(ReplaceAnimClipEditor)) as ReplaceAnimClipEditor;
            win.titleContent = new GUIContent(" ReplaceAnim");
            win.minSize = new Vector2(500, 120);
            win.Show();
        }

        public void OnEnable()
        {
            InitAnimList();
        }

        void GetSelectionClip()
        {
            var selections = Selection.objects;
            if (selectedClips.Count > 1)
            {
                selectedClips = new List<TimelineClip>();
            }

            for (int i = 0; i < selections.Length; i++)
            {
                var selectedClip = TimelineUtils.GetClip(selections[i]);
                selectedClips.Add(selectedClip);
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Replacing [ " + selectedClips.Count + " ] anim clip(s) with...");

            if (GUILayout.Button("Get Selection", GUILayout.MaxWidth(100)))
            {
                GetSelectionClip();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (selectedClips.Count == 0) { return; }

            newClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", newClip, typeof(AnimationClip), false);
            GUILayout.Space(10);

            if (GUILayout.Button("REPLACE ANIMATION CLIP", GUILayout.Height(30)))
            {
                if (newClip == null)
                {
                    UnityEngine.Debug.LogError("No Animation Clip. Make sure you put in one");
                    return;
                }

                var selectedTrack = TimelineUtils.GetTrackBasedOnClip(selectedClips[0]) as AnimationTrack;
                var timeline_asset = selectedTrack.timelineAsset;

                var director = TimelineUtils.GetDirectorFromTimeline(timeline_asset);

                Undo.RecordObject(director.playableAsset, "Replace Animation Clip");

                for (int i = 0; i < selectedClips.Count; i++)
                {
                    Replace(selectedClips[i], newClip, timeline_asset);
                }

                InitAnimList();

                TimelineUtils.ToggleLockWindow();
                TimelineUtils.SetTimeline(timeline_asset, director);
                Selection.activeObject = director.gameObject;
                TimelineUtils.ToggleLockWindow();
            }
        }

        void Replace(TimelineClip _oldclip, AnimationClip _clip, TimelineAsset _timelineAsset)
        {
            // 获取原来剪辑所在的轨道
            var originalTrack = _oldclip.parentTrack as AnimationTrack;

            // 获取原来剪辑的 clipIn 值
            var clipInField = _oldclip.GetType().GetField("m_ClipIn", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var clipInValue = (double)clipInField.GetValue(_oldclip);

            // 删除原来剪辑
            _timelineAsset.DeleteClip(_oldclip);

            // 在原来剪辑的轨道上创建新的动画剪辑
            var newAnimClip = originalTrack.CreateClip(newClip);
            newAnimClip.start = _oldclip.start;
            newAnimClip.duration = _oldclip.duration;
            newAnimClip.displayName = _clip.name;
            newAnimClip.timeScale = _oldclip.timeScale;
            newAnimClip.clipIn = clipInValue;
        }

        void InitAnimList()
        {
            selectedClips = new List<TimelineClip>();
        }
    }
}
