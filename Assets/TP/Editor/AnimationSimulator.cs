using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class AnimationSimulator : EditorWindow
{
    private static Animator[] animators;
    private static bool[] dropdownStatus;

    private static PlayModeStateChange currentState = PlayModeStateChange.EnteredEditMode;
    private static float lastEditorTime = 0f;
    private static int[] idCurrentAnim = new int[2] { -1, -1 };
    //private static bool onScene = false;

    private static Vector3[] iniPos;
    private static float currentposAnim = 0f;
    private static bool isInPause = true;
    private static float speed = 1f;
    private static float loopDelay = 0f;
    private static float countLoopDelay = 0f;

    static AnimationSimulator()
    {
        EditorApplication.playModeStateChanged += StopOnPlay;
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += SceneClosing;
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpening += SceneOpening;
    }

    [MenuItem("Window/AnimationSimulator")]
    public static void DrawAnimatorListWindow()
    {
        var window = GetWindow(typeof(AnimationSimulator));
        window.minSize = new Vector2(400, 250);
        window.position = new Rect(420, 120, 200, 200);
    }

    private void OnSelectionChange()
    {
        if (currentState != PlayModeStateChange.EnteredEditMode) return;

        animators = FindObjectsOfType<Animator>();
        if (animators.Length != dropdownStatus.Length)
        {
            dropdownStatus = new bool[animators.Length];
            iniPos = new Vector3[animators.Length];
        }
    }

    private void OnEnable()
    {

        //VisualTreeAsset original = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/TP/Editor/AnimationSimulatorWindow.uxml");
        //TemplateContainer treeAsset = original.CloneTree();
        //rootVisualElement.Add(treeAsset);

        //StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/TP/Editor/AnimationSimulatorStyles.uss");
        //rootVisualElement.styleSheets.Add(styleSheet);

        animators = FindObjectsOfType<Animator>();
        dropdownStatus = new bool[animators.Length];
        iniPos = new Vector3[animators.Length];

        //CreateAnimationSimulatorView();
    }

    private void CreateAnimationSimulatorView()
    {
        FindAllAnimators(out Animator[] animators);

        ListView animatorList = rootVisualElement.Query<ListView>("Animator").First();
        animatorList.makeItem = () => new Label();
        animatorList.bindItem = (element, i) => (element as Label).text = animators[i].runtimeAnimatorController.name;

        animatorList.itemsSource = animators;
        animatorList.itemHeight = 16;
        animatorList.selectionType = SelectionType.Single;

        animatorList.onSelectionChanged += (enumerable) =>
         {
             foreach (Object item in enumerable)
             {

             }
         };

        animatorList.Refresh();
    }

    private void FindAllAnimators(out Animator[] animators)
    {
        animators = FindObjectsOfType<Animator>();
    }

    private void OnDisable()
    {
        //AnimationMode.StopAnimationMode();
        //EditorApplication.update -= OnEditorUpdate;
    }


    private void OnGUI()
    {
        if (currentState != PlayModeStateChange.EnteredEditMode) return;

        if (animators.Length == 0)
        {
            if (GUILayout.Button("Update"))
            {
                if (currentState != PlayModeStateChange.EnteredEditMode) return;

                animators = FindObjectsOfType<Animator>();
                if (animators.Length != dropdownStatus.Length)
                {
                    dropdownStatus = new bool[animators.Length];
                    iniPos = new Vector3[animators.Length];
                }
            }
            return;
        }

        for (int i = animators.Length;i-->0;)
        {
            if(EditorGUILayout.DropdownButton(new GUIContent(animators[i].runtimeAnimatorController.name), FocusType.Keyboard))
            {
                dropdownStatus[i] = !dropdownStatus[i];
                if (dropdownStatus[i])
                {
                    Selection.activeObject = animators[i].gameObject;
                    iniPos[i] = animators[i].transform.position;
                }
            }

            if (dropdownStatus[i])
            {

                for (int j = 0; j < animators[i].runtimeAnimatorController.animationClips.Length; j++)
                {
                    if (GUILayout.Button(animators[i].runtimeAnimatorController.animationClips[j].name))
                    {
                        if (idCurrentAnim[0] != i || idCurrentAnim[1] != j)
                        {
                            iniPos[i] = animators[i].transform.position;

                            if (idCurrentAnim[0] != -1)
                            {
                                AnimationMode.StopAnimationMode();
                                EditorApplication.update -= OnEditorUpdate;
                            }

                            idCurrentAnim[0] = i;
                            idCurrentAnim[1] = j;
                            loopDelay = 0f;

                            AnimationMode.StartAnimationMode();
                            EditorApplication.update += OnEditorUpdate;
                            lastEditorTime = Time.realtimeSinceStartup;

                            for (int k = dropdownStatus.Length; k-->0;)
                            {
                                if (k != i)
                                    dropdownStatus[k] = false;
                            }
                        }
                    }

                }
            }
        }

        if (idCurrentAnim[0] >= 0 && idCurrentAnim[1] >= 0)
        {
            GUILayout.Space(10);
            GUILayout.Label(animators[idCurrentAnim[0]].runtimeAnimatorController.name, EditorStyles.boldLabel);

            if (GUILayout.Button("STOP"))
            {
                AnimationMode.StopAnimationMode();
                EditorApplication.update -= OnEditorUpdate;
                idCurrentAnim[0] = -1;
                idCurrentAnim[1] = -1;
            }
            else
            {
                if (!isInPause)
                {
                    if (GUILayout.Button("Pause"))
                    {
                        isInPause = !isInPause;
                    }
                    GUILayout.Label("Speed :");
                    speed = EditorGUILayout.Slider(speed, 0, 10);
                    GUILayout.Label("Animation Delay betwin loop:");
                    loopDelay = EditorGUILayout.Slider(loopDelay, 0, 120);
                }
                else
                {
                    if (GUILayout.Button("Play"))
                    {
                        isInPause = !isInPause;
                    }
                    currentposAnim = EditorGUILayout.Slider(currentposAnim/ animators[idCurrentAnim[0]].runtimeAnimatorController.animationClips[idCurrentAnim[1]].length, 0f, 1)* animators[idCurrentAnim[0]].runtimeAnimatorController.animationClips[idCurrentAnim[1]].length;
                    GUILayout.Label((currentposAnim.ToString("f2") + "s / " + animators[idCurrentAnim[0]].runtimeAnimatorController.animationClips[idCurrentAnim[1]].length.ToString("f2") + "s"));
                    GUILayout.Toggle(animators[idCurrentAnim[0]].runtimeAnimatorController.animationClips[idCurrentAnim[1]].isLooping, "Is anim loop");
                }
            }
        }

        //Debug.Log("GUI");
    }

    private static void OnEditorUpdate()
    {
        if (currentState != PlayModeStateChange.EnteredEditMode) return;

        float deltaTime = Time.realtimeSinceStartup - lastEditorTime;
        lastEditorTime = Time.realtimeSinceStartup;

        AnimationClip clip = animators[idCurrentAnim[0]].runtimeAnimatorController.animationClips[idCurrentAnim[1]];

        if (!isInPause)
        {
            if (currentposAnim >= clip.length)
            {
                if (clip.isLooping)
                {
                    countLoopDelay += deltaTime;
                    if (countLoopDelay >= loopDelay)
                    {
                        currentposAnim -= clip.averageDuration;
                        countLoopDelay = 0f;
                    }
                }
                else
                    isInPause = true;
            }
            else
            {
                currentposAnim += deltaTime * speed;
            }
        }

        AnimationMode.SampleAnimationClip(animators[idCurrentAnim[0]].gameObject, clip, currentposAnim);

        animators[idCurrentAnim[0]].transform.position = iniPos[idCurrentAnim[0]];
    }

    private static void StopOnPlay(PlayModeStateChange state)
    {
        currentState = state;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            AnimationMode.StopAnimationMode();
            EditorApplication.update -= OnEditorUpdate;
            idCurrentAnim[0] = -1;
            idCurrentAnim[1] = -1;


            Debug.Log("Exit edit mode");
        }
    }
    private static void SceneClosing(Scene scene, bool removingScene)
    {
        //onScene = false;

        AnimationMode.StopAnimationMode();
        EditorApplication.update -= OnEditorUpdate;
        idCurrentAnim[0] = -1;
        idCurrentAnim[1] = -1;

        animators = new Animator[0];
        dropdownStatus = new bool[animators.Length];
        iniPos = new Vector3[animators.Length];

        Debug.Log("Exit Scene");
    }

    private static void SceneOpening(string path, OpenSceneMode mode)
    {
        animators = FindObjectsOfType<Animator>();
        //dropdownStatus = new bool[animators.Length];

        //onScene = true;
        Debug.Log("Open Scene");
    }
}
