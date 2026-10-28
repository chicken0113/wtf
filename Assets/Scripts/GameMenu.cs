using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EverythingGrows
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameMenu : MonoBehaviour
    {
        public const string TitleScene="Assets/Scenes/System/MainMenu.unity";
        public const string FirstScene="Assets/Scenes/OpenWorld/GeneratedWorld.unity";
        private enum Page { Playing, Title, Paused, Settings, ConfirmNew }
        [Serializable] private sealed class Options { public float volume=1;public bool fullscreen;public int width=1280,height=720; }
        public static GameMenu Instance { get; private set; }
        private static int consumedFrame=-1;
        public static bool ConsumedInputThisFrame=>consumedFrame==Time.frameCount;
        public static bool IsBlocking=>Instance!=null && (Instance.page!=Page.Playing || Instance.loading);
        private Page page,settingsReturn;
        private Options options=new();
        private bool loading;
        private string message;
        private Vector2 scroll;
        private float previousTimeScale=1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance=null;consumedFrame=-1; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if(Instance!=null)return;
            var root=new GameObject("Game menus");root.AddComponent<GameMenu>();
        }
        private void Awake()
        {
            if(Instance!=null && Instance!=this) { Destroy(gameObject);return; }
            Instance=this;DontDestroyOnLoad(gameObject);previousTimeScale=Time.timeScale;
            SceneManager.sceneLoaded+=OnSceneLoaded;EnsureListener();LoadOptions();SetPage(SceneManager.GetActiveScene().path==TitleScene?Page.Title:Page.Playing);
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded-=OnSceneLoaded;
            if(Instance==this) { Time.timeScale=previousTimeScale;Instance=null; }
        }
        private void OnSceneLoaded(Scene scene,LoadSceneMode mode)
        { if(mode!=LoadSceneMode.Single)return;loading=false;message=null;EnsureListener();SetPage(scene.path==TitleScene?Page.Title:Page.Playing); }
        private static void EnsureListener() { if(Camera.main!=null && UnityEngine.Object.FindFirstObjectByType<AudioListener>()==null)Camera.main.gameObject.AddComponent<AudioListener>(); }
        private void SetPage(Page next)
        {
            if(page==Page.Playing && next!=Page.Playing)previousTimeScale=Time.timeScale;
            page=next;Time.timeScale=page==Page.Playing?previousTimeScale:0;
            consumedFrame=Time.frameCount;
        }
        public void TogglePause()
        { if(loading)return;SetPage(page==Page.Playing?Page.Paused:Page.Playing); }
        private void Update()
        {
            if(!Input.GetKeyDown(KeyCode.Escape) || loading)return;
            if(page==Page.Settings) { SetPage(settingsReturn);return; }
            if(page==Page.ConfirmNew) { SetPage(Page.Title);return; }
            if(page==Page.Title)return;
            var farm=GameSession.CurrentFarm;
            if(page==Page.Playing && farm!=null && (farm.InventoryOpen || farm.MapOpen))return;
            if(!string.IsNullOrEmpty(GameSession.LastError))return;
            TogglePause();
        }
        private bool LoadScene(string path)
        {
            if(loading || SceneUtility.GetBuildIndexByScenePath(path)<0) { message="이동할 씬이 빌드에 없습니다.";return false; }
            try { loading=true;SceneManager.LoadSceneAsync(path,LoadSceneMode.Single);return true; }
            catch(Exception failure) { loading=false;message=failure.Message;return false; }
        }
        public bool ContinueGame()
        {
            if(!GameSession.TryContinue(out var scene,out var reason)) { message=reason;return false; }
            return LoadScene(scene);
        }
        public bool StartNewGame()
        {
            if(SceneUtility.GetBuildIndexByScenePath(FirstScene)<0) { message="시작 농장 씬이 빌드에 없습니다.";return false; }
            if(!GameSession.PrepareNewGame(out var reason)) { message=reason;return false; }
            return LoadScene(FirstScene);
        }
        public bool ReturnToTitle(bool save=true)
        {
            if(SceneUtility.GetBuildIndexByScenePath(TitleScene)<0) { message="타이틀 씬이 빌드에 없습니다.";return false; }
            if(save && GameSession.CurrentFarm!=null && !GameSession.Save(GameSession.CurrentFarm)) { message=GameSession.LastError;return false; }
            GameSession.ReleaseToTitle();return LoadScene(TitleScene);
        }
        private void Quit()
        {
            if(page!=Page.Title && GameSession.CurrentFarm!=null && !GameSession.Save(GameSession.CurrentFarm)) { message=GameSession.LastError;return; }
            Time.timeScale=previousTimeScale;
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit();
            #endif
        }
        private string OptionsPath=>GameSession.Enabled?Path.Combine(GameSession.SaveDirectory,"options.json"):null;
        private void LoadOptions()
        {
            try
            {
                if(OptionsPath==null || !File.Exists(OptionsPath))return;
                var loaded=JsonUtility.FromJson<Options>(File.ReadAllText(OptionsPath));
                if(loaded==null || !SetupDataValidator.Finite(loaded.volume) || loaded.width<640 || loaded.height<360)throw new InvalidDataException("설정 파일이 유효하지 않습니다.");
                options=loaded;AudioListener.volume=Mathf.Clamp01(options.volume);Screen.SetResolution(options.width,options.height,options.fullscreen);
            }
            catch(Exception failure) { message="설정 불러오기 실패: "+failure.Message; }
        }
        private void SaveOptions()
        {
            try
            {
                if(OptionsPath==null)throw new IOException("설정 저장 경로가 없습니다.");
                Directory.CreateDirectory(Path.GetDirectoryName(OptionsPath));string temp=OptionsPath+".tmp";
                File.WriteAllText(temp,JsonUtility.ToJson(options,true));
                if(File.Exists(OptionsPath))File.Replace(temp,OptionsPath,null);else File.Move(temp,OptionsPath);
                AudioListener.volume=options.volume;Screen.SetResolution(options.width,options.height,options.fullscreen);message="설정을 저장했습니다.";
            }
            catch(Exception failure) { message="설정 저장 실패: "+failure.Message; }
        }
        private void OnGUI()
        {
            if(!IsBlocking)return;
            int depth=GUI.depth;GUI.depth=-100;
            var tint=GUI.color;GUI.color=new Color(.04f,.07f,.07f,.98f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=tint;
            float width=Mathf.Min(440,Screen.width-24),height=Mathf.Min(540,Screen.height-24);
            GUILayout.BeginArea(new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height),GUI.skin.box);
            scroll=GUILayout.BeginScrollView(scroll);
            GUILayout.Label(page==Page.Title?"WHAT THE FARM":page==Page.Settings?"설정":page==Page.ConfirmNew?"새 게임":"일시정지",new GUIStyle(GUI.skin.label){fontSize=26,alignment=TextAnchor.MiddleCenter});GUILayout.Space(18);
            using(new GUIEnabledScope(!loading))
            {
                if(page==Page.Title)
                {
                    using(new GUIEnabledScope(GameSession.HasSave))if(GUILayout.Button("이어하기",GUILayout.Height(38)))ContinueGame();
                    if(GUILayout.Button("새 게임",GUILayout.Height(38))) { if(GameSession.HasSave)SetPage(Page.ConfirmNew);else StartNewGame(); }
                    if(GUILayout.Button("설정",GUILayout.Height(38))) { settingsReturn=Page.Title;SetPage(Page.Settings); }
                    if(GUILayout.Button("종료",GUILayout.Height(38)))Quit();
                }
                else if(page==Page.Paused)
                {
                    if(GUILayout.Button("계속하기",GUILayout.Height(38)))SetPage(Page.Playing);
                    if(GUILayout.Button("설정",GUILayout.Height(38))) { settingsReturn=Page.Paused;SetPage(Page.Settings); }
                    if(GUILayout.Button("저장 후 타이틀",GUILayout.Height(38)))ReturnToTitle();
                    if(GUILayout.Button("저장 후 종료",GUILayout.Height(38)))Quit();
                }
                else if(page==Page.ConfirmNew)
                {
                    GUILayout.Label("현재 진행을 초기화하고 새로 시작할까요?\n이전 저장은 별도로 보관됩니다.");
                    if(GUILayout.Button("새로 시작"))StartNewGame();if(GUILayout.Button("취소"))SetPage(Page.Title);
                }
                else if(page==Page.Settings)
                {
                    GUILayout.Label("전체 음량: "+Mathf.RoundToInt(options.volume*100)+"%");options.volume=GUILayout.HorizontalSlider(options.volume,0,1);
                    options.fullscreen=GUILayout.Toggle(options.fullscreen,"전체 화면");
                    string[] resolutions={"1280 × 720","1600 × 900","1920 × 1080"};int index=options.width==1920?2:options.width==1600?1:0;
                    int next=GUILayout.SelectionGrid(index,resolutions,1);if(next!=index) { options.width=next==2?1920:next==1?1600:1280;options.height=next==2?1080:next==1?900:720; }
                    if(GUILayout.Button("적용 및 저장"))SaveOptions();if(GUILayout.Button("뒤로"))SetPage(settingsReturn);
                }
            }
            if(loading)GUILayout.Label("불러오는 중...");if(!string.IsNullOrEmpty(message))GUILayout.Label(message,new GUIStyle(GUI.skin.label){wordWrap=true});
            GUILayout.EndScrollView();GUILayout.EndArea();GUI.depth=depth;
        }
        private readonly struct GUIEnabledScope : IDisposable
        {
            private readonly bool previous;
            public GUIEnabledScope(bool enabled) { previous=GUI.enabled;GUI.enabled=previous&&enabled; }
            public void Dispose()=>GUI.enabled=previous;
        }
    }
}
