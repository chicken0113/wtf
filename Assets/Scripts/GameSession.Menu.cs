using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EverythingGrows
{
    public sealed partial class GameSession
    {
        public static bool HasSave => Enabled && (File.Exists(SavePath) || File.Exists(SavePath+".bak"));
        public static bool RestoreBlocked => blocked;
        public static FarmGame CurrentFarm => current;
        public static bool TryContinue(out string scene,out string reason)
        {
            scene=null;reason=null;
            try
            {
                if(!HasSave) { reason="이어할 저장이 없습니다.";return false; }
                Load();scene=data?.currentScene;
                if(string.IsNullOrEmpty(scene) || SceneUtility.GetBuildIndexByScenePath(scene)<0)throw new InvalidDataException("저장된 씬을 빌드에서 찾을 수 없습니다.");
                return true;
            }
            catch(Exception failure) { reason=failure.Message;ReleaseToTitle();return false; }
        }
        public static void ReleaseToTitle()
        {
            if(runner!=null)Destroy(runner.gameObject);
            ResetRuntime();
        }
        // Called only after the title screen's explicit new-game confirmation.
        // Archive the previous run instead of irreversibly deleting player data.
        public static bool PrepareNewGame(out string reason)
        {
            reason=null;var moved=new List<(string source,string destination)>();
            try
            {
                if(!Enabled)throw new InvalidOperationException("저장 경로를 사용할 수 없습니다.");
                string archive=Path.Combine(SaveDirectory,"PreviousRuns",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N"));
                foreach(string suffix in new[]{"",".bak",".tmp"})
                {
                    string source=SavePath+suffix;if(!File.Exists(source))continue;
                    Directory.CreateDirectory(archive);string destination=Path.Combine(archive,Path.GetFileName(source));
                    File.Move(source,destination);moved.Add((source,destination));
                }
                ReleaseToTitle();return true;
            }
            catch(Exception failure)
            {
                reason=failure.Message;
                for(int i=moved.Count-1;i>=0;i--)try { File.Move(moved[i].destination,moved[i].source); }
                catch(Exception rollback) { reason+=" 복구 확인 필요: "+rollback.Message; }
                return false;
            }
        }
    }
}
