using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

[InitializeOnLoad]
public static class MenuFlowCheck
{
    private const string Key="MenuFlowCheckActive";
    private static int step,frames;
    private static double deadline,pauseStarted;
    private static double worldBeforePause;
    private static string titleImage,pauseImage,error;
    static MenuFlowCheck() { if(Application.isBatchMode && SessionState.GetBool(Key,false)) { deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick;Application.logMessageReceived+=OnLog; } }
    private static void Check(bool value,string text) { if(!value)throw new Exception(text); }
    public static void Run()
    {
        Check(Application.isBatchMode && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) && Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Isolated project and saves only.");
        EditorSceneManager.OpenScene("Assets/Scenes/LegacyVillage/FirstGarden.unity");MenuFoundationSetup.Ensure();EditorSceneManager.OpenScene(GameMenu.TitleScene);
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Show();view.position=new Rect(100,100,1280,800);view.Focus();
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    private static void OnLog(string message,string trace,LogType type)
    { if(type==LogType.Exception && (trace.Contains("EverythingGrows.") || trace.Contains("GUI")))error=message+trace; }
    private static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"Menu flow timeout at step "+step);Check(string.IsNullOrEmpty(error),error);
            if(!EditorApplication.isPlaying || GameMenu.Instance==null)return;
            var menu=GameMenu.Instance;var farm=GameSession.CurrentFarm;
            bool title=SceneManager.GetActiveScene().path==GameMenu.TitleScene;
            if(step==0)
            {
                if(!title || ++frames<8)return;
                Check(GameMenu.IsBlocking && Time.timeScale==0 && !GameSession.HasSave,"Initial title state invalid");
                titleImage=Path.GetFullPath("TitleMenu-"+Guid.NewGuid().ToString("N")+".png");ScreenCapture.CaptureScreenshot(titleImage);step++;return;
            }
            if(step==1)
            {
                if(!File.Exists(titleImage))return;Check(menu.StartNewGame(),"New game failed");step++;return;
            }
            if(step==2)
            {
                if(farm==null || !farm.IsInitialized || title || GameMenu.IsBlocking || GameSession.Busy)return;
                Check(farm.gameObject.scene.path==GameMenu.FirstScene && farm.Gold==100 && farm.FarmingLevel==1,"New game loadout/state wrong");
                farm.SetInventoryOpen(true);Check(farm.ProcessMenuInput(false,true,false) && !farm.InventoryOpen && !GameMenu.IsBlocking,"Inventory Esc became pause");
                menu.TogglePause();worldBeforePause=GameSession.WorldSeconds;pauseStarted=EditorApplication.timeSinceStartup;
                pauseImage=Path.GetFullPath("PauseMenu-"+Guid.NewGuid().ToString("N")+".png");ScreenCapture.CaptureScreenshot(pauseImage);step++;return;
            }
            if(step==3)
            {
                if(EditorApplication.timeSinceStartup-pauseStarted<.3 || !File.Exists(pauseImage))return;
                Check(GameMenu.IsBlocking && Time.timeScale==0 && GameSession.WorldSeconds==worldBeforePause,"Pause advanced simulation");
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var options=typeof(GameMenu).GetField("options",flags).GetValue(menu);options.GetType().GetField("volume").SetValue(options,.42f);
                typeof(GameMenu).GetMethod("SaveOptions",flags).Invoke(menu,null);
                Check(File.Exists(Path.Combine(GameSession.SaveDirectory,"options.json")) && Mathf.Abs(AudioListener.volume-.42f)<.001f,"Settings save/audio apply failed");
                options.GetType().GetField("volume").SetValue(options,.9f);typeof(GameMenu).GetMethod("LoadOptions",flags).Invoke(menu,null);
                Check(Mathf.Abs(AudioListener.volume-.42f)<.001f,"Settings reload failed");
                menu.TogglePause();Check(!GameMenu.IsBlocking && Time.timeScale>0,"Resume failed");
                farm.Shop.RestoreGold(317);Check(menu.ReturnToTitle(),"Save and title failed");step++;return;
            }
            if(step==4)
            {
                if(!title)return;Check(GameSession.HasSave && menu.ContinueGame(),"Continue failed");step++;return;
            }
            if(step==5)
            {
                if(farm==null || title || GameMenu.IsBlocking || GameSession.Busy)return;
                Check(farm.Gold==317 && farm.gameObject.scene.path==GameMenu.FirstScene,"Continue lost saved state");
                Directory.CreateDirectory(GameSession.SavePath+".tmp");
                Check(!menu.ReturnToTitle() && GameSession.CurrentFarm==farm && !string.IsNullOrEmpty(GameSession.LastError),"Failed save still left gameplay");
                Directory.Delete(GameSession.SavePath+".tmp");Check(menu.ReturnToTitle(),"Save retry/title failed");step++;return;
            }
            if(step==6)
            {
                if(!title)return;Check(menu.StartNewGame(),"Reset/new game failed");step++;return;
            }
            if(step==7)
            {
                if(farm==null || title || GameMenu.IsBlocking || GameSession.Busy)return;
                Check(farm.Gold==100 && farm.FarmingLevel==1 && Directory.Exists(Path.Combine(GameSession.SaveDirectory,"PreviousRuns")),"New game did not reset/archive old run");
                SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;
                Debug.Log("MENU_FLOW_SUCCESS: title/new game, inventory Esc priority, paused simulation/resume, audio/display settings persistence, save-title-continue, failed save stay/retry and archived reset. Images: "+titleImage+" | "+pauseImage);EditorApplication.Exit(0);
            }
        }
        catch(Exception failure) { SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.LogException(failure);EditorApplication.Exit(1); }
    }
}
