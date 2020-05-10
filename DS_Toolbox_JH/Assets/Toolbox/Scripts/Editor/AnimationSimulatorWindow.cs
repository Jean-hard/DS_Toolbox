using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using ToolboxEngine;

namespace ToolboxEditor
{
    public class AnimationSimulatorWindow : EditorWindow
    {
        private int _tabIndex = 0;

        private float _clipSampleValue = 0;
        private float _speed = 0;
        private float animTime = 0;
        private float _currentAnimTime = 0;
        private float _repeatOffset = 0;

        public static string[] tabs = new string[]
        {
            "Creator",
            "Animator Selector",
            "Clip Handler"
        };

        private Animator[] _animatorComponentsArr = null;

        private Animator _currentAnimator = null;
        private AnimationClip[] _animClipsArr = null;
        private string[] _animNamesArr = null;
        private bool _isInitialized = false;

        private int _currentAnimIndex = 0;

        private bool _isPlaying = false;

        private float _editorLastTime = 0f;

        private Vector3 _currentAnimatorPosition = new Vector3();

        private AnimationSimulator _animSimulator;

        //Create settings
        private Vector3 _instantiatePosition = new Vector3();
        private Animator _selectedAnimatorToCreate;

        [MenuItem("Toolbox/Animation Simulator")]
        static void InitWindow()
        {
            EditorWindow window = GetWindow<AnimationSimulatorWindow>();
            window.autoRepaintOnSceneChange = true;
            window.Show();
            window.titleContent = new GUIContent("Animation Simulator");
        }

        private void OnGUI()
        {
            if (!_isInitialized)
            {
                _currentAnimator = _FindAnimatorComponentsInScene()[0];
                _animClipsArr = FindAnimClips(_currentAnimator);
                _animNamesArr = FindAnimNames(_animClipsArr);
                _isInitialized = true;
            }

            _tabIndex = GUILayout.Toolbar(_tabIndex, tabs);

            switch (_tabIndex)
            {
                case 0:
                    _GUITabsCreator(); break;
                case 1:
                    _GUITabsAnimatorSelector(); break;
                case 2:
                    _GUITabsClipHandler(); break;
            }
        }

        private void _GUITabsCreator()
        {
            GUILayout.Space(10f);
            EditorGUILayout.LabelField("Select an animator to instantiate :", EditorStyles.boldLabel);

            GUILayout.Space(10f);
            AnimPrefabsData animPrefabsData = _FindAnimPrefabsDataInProject();
            Animator[] animPrefabs = animPrefabsData.animPrefabsArr;
            if (null != animPrefabs && animPrefabs.Length > 0)
            {
                foreach (Animator animatorPrefab in animPrefabs)
                {
                    if (GUILayout.Button(animatorPrefab.name))
                    {
                        _selectedAnimatorToCreate = animatorPrefab;
                    }
                }
            }

            if(_selectedAnimatorToCreate != null)
            {
                GUILayout.Space(30f);
                _instantiatePosition = EditorGUILayout.Vector3Field("To position : ", _instantiatePosition);

                GUILayout.Space(10f);
                if (GUILayout.Button("Create " + _selectedAnimatorToCreate))
                {
                    Instantiate(_selectedAnimatorToCreate, _instantiatePosition, new Quaternion(0f, 180f, 0f, 0f));
                }
            }
        }

        private void _GUITabsAnimatorSelector()
        {
            _clipSampleValue = 0;
            _selectedAnimatorToCreate = null;

            EditorApplication.update -= _OnEditorPositionUpdate;

            GUILayout.Space(10f);
            EditorGUILayout.LabelField("Select an animator :", EditorStyles.boldLabel);

            GUILayout.Space(10f);

            if (null == _animatorComponentsArr)
            {
                _animatorComponentsArr = _FindAnimatorComponentsInScene();
            }


            foreach (Animator animator in _animatorComponentsArr)
            {
                if (GUILayout.Button(animator.name))
                {
                    if (_isPlaying)
                    {
                        StopAnim();
                        _currentAnimator = null;
                    }
                    Selection.activeGameObject = animator.gameObject;
                    SceneView.lastActiveSceneView.FrameSelected();
                    EditorGUIUtility.PingObject(animator.gameObject);
                    _currentAnimator = animator;
                    _currentAnimatorPosition = _currentAnimator.transform.position;
                    _currentAnimIndex = 0;

                    _animClipsArr = FindAnimClips(_currentAnimator);
                    _animNamesArr = FindAnimNames(_animClipsArr);
                }
            }
        }

        private void _GUITabsClipHandler()
        {
            _selectedAnimatorToCreate = null;

            if (null == _currentAnimator)
            {
                EditorGUILayout.LabelField("No animator selected", EditorStyles.boldLabel);
                return;
            }

            _currentAnimatorPosition = _currentAnimator.transform.position;
            EditorApplication.update += _OnEditorPositionUpdate;

            GUILayout.Space(10f);
            if (!EditorApplication.isPlaying)
            {
                //CLIP
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("Animation Clip", EditorStyles.boldLabel);
                //_currentAnimIndex = EditorGUILayout.Popup("Current Anim", _currentAnimIndex, _animNamesArr);
                for(int i = 0; i < _animClipsArr.Length; i++)
                {
                    if (GUILayout.Button(_animClipsArr[i].name))
                    {
                        _currentAnimIndex = i;
                        StopAnim();
                    }
                }
                EditorGUILayout.EndVertical();

                //SPEED
                GUILayout.Space(10f);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("Speed", EditorStyles.boldLabel);
                _speed = EditorGUILayout.Slider(_speed, 0f, 2f);
                EditorGUILayout.EndVertical();

                //LOOP OFFSET
                if (_isPlaying)
                {
                    GUILayout.Space(10f);
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.LabelField("Repeating Offset", EditorStyles.boldLabel);
                    _repeatOffset = EditorGUILayout.FloatField(_repeatOffset);

                    EditorGUILayout.EndVertical();
                }

                //SAMPLE
                GUILayout.Space(10f);
                if (!_isPlaying)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.LabelField("Sampler", EditorStyles.boldLabel);

                    AnimationClip animClip = _animClipsArr[_currentAnimIndex];
                    _clipSampleValue = EditorGUILayout.Slider(_clipSampleValue, 0f, animClip.length);
                    animClip.SampleAnimation(_currentAnimator.gameObject, _clipSampleValue);
                    EditorGUILayout.EndVertical();
                }
                _currentAnimator.transform.position = _currentAnimatorPosition;
                _currentAnimator.transform.rotation = new Quaternion(0f, 180f, 0f, 0f);

                //INFORMATIONS
                if (_isPlaying)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.LabelField("Infos", EditorStyles.boldLabel);
                    
                    AnimationClip animClip = _animClipsArr[_currentAnimIndex];
                    EditorGUILayout.LabelField("Anim Time : " + _currentAnimTime + "/" + animClip.length);
                    EditorGUILayout.LabelField("Looping : " + animClip.isLooping.ToString()); 

                    EditorGUILayout.EndVertical();
                }

                //PLAY AND STOP BUTTON
                GUILayout.Space(20f);
                if (_isPlaying)
                {
                    if (GUILayout.Button("Stop"))
                    {
                        StopAnim();
                    }
                }
                else
                {
                    if (GUILayout.Button("Play"))
                    {
                        PlayAnim();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("In Play Mode ! Quit Play Mode to play anim.", EditorStyles.boldLabel);
                StopAnim();
            }
        }

        private void OnHierarchyChange()
        {
            _animatorComponentsArr = _FindAnimatorComponentsInScene();
        }

        private void PlayAnim()
        {
            if (_isPlaying) return;
            
            
            _editorLastTime = Time.realtimeSinceStartup;
            EditorApplication.update += _OnEditorUpdate;
            AnimationMode.StartAnimationMode();
            _isPlaying = true;
        }

        private void StopAnim()
        {
            if (!_isPlaying) return;
            EditorApplication.update -= _OnEditorUpdate;
            AnimationMode.StopAnimationMode();
            _isPlaying = false;
        }

        private void _OnEditorUpdate()
        {
            if (!_isPlaying) return;
            animTime = Time.realtimeSinceStartup - _editorLastTime;
            AnimationClip animClip = _animClipsArr[_currentAnimIndex];
            animTime *= _speed;
            _currentAnimTime = animTime;
            _currentAnimTime %= animClip.length;
            animTime %= animClip.length + _repeatOffset/3;
            AnimationMode.SampleAnimationClip(_currentAnimator.gameObject, animClip, animTime);

            _currentAnimator.transform.position = _currentAnimatorPosition;
            _currentAnimator.transform.rotation = new Quaternion(0f, 180f, 0f, 0f);
        }

        private void _OnEditorPositionUpdate()
        {
            if (null == _currentAnimator) return;
            _currentAnimator.transform.position = _currentAnimatorPosition;
            _currentAnimator.transform.rotation = new Quaternion(0f, 180f, 0f, 0f);
        }

        private Animator[] _FindAnimatorComponentsInScene()
        {
            List<Animator> animatorComponentsList = new List<Animator>();
            foreach (GameObject rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                animatorComponentsList.AddRange(rootGameObject.GetComponentsInChildren<Animator>());
            }
            
            return animatorComponentsList.ToArray();
        }

        private string[] FindAnimNames(AnimationClip[] animClipsArr)
        {
            List<string> resultList = new List<string>();
            foreach (AnimationClip clip in animClipsArr)
            {
                resultList.Add(clip.name);
            }

            return resultList.ToArray();
        }

        private AnimationClip[] FindAnimClips(Animator animator)
        {
            List<AnimationClip> resultList = new List<AnimationClip>();

            AnimatorController editorController = animator.runtimeAnimatorController as AnimatorController;

            AnimatorControllerLayer controllerLayer = editorController.layers[0];
            foreach (ChildAnimatorState childState in controllerLayer.stateMachine.states)
            {
                AnimationClip animClip = childState.state.motion as AnimationClip;
                if (animClip != null)
                {
                    resultList.Add(animClip);
                }
            }

            return resultList.ToArray();
        }

        private AnimPrefabsData _FindAnimPrefabsDataInProject()
        {
            string[] fileGuidsArr = AssetDatabase.FindAssets("t: " + typeof(AnimPrefabsData)); //Trouve les assets par leur guid
            if (fileGuidsArr.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(fileGuidsArr[0]);
                return AssetDatabase.LoadAssetAtPath<AnimPrefabsData>(assetPath);
            }
            else
            {
                AnimPrefabsData animPrefabsData = ScriptableObject.CreateInstance<AnimPrefabsData>();
                AssetDatabase.CreateAsset(animPrefabsData, "Assets/AnimPrefabsData.asset");
                AssetDatabase.SaveAssets();
                return animPrefabsData;
            }
        }
    }
}
