using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace EverythingGrows
{
    public sealed partial class GameSession
    {
        // Different scenes cannot be visible together in the current single-scene travel model.
        public static void CleanupUnloadedRegions(FarmGame farm)
        {
            if(data==null || farm==null || farm.Cycle==null || !farm.Cycle.IsDaytime || blocked)return;
            var types=Resources.LoadAll<MonsterType>("");
            foreach(var map in data.maps)
            {
                if(map.scene==farm.gameObject.scene.path || !map.regionalCombatSaved || map.regionalEnemies==null)continue;
                map.regionalEnemies.RemoveAll(enemy=>types.Any(type=>type.SaveKey==enemy.type && !type.isBoss));
            }
        }
        static void ValidateRegionalEnemies(GameSaveData candidate)
        {
            var types=Resources.LoadAll<MonsterType>("");var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var map in candidate.maps)
            {
                if(map==null)continue;map.regionalEnemies??=new();
                foreach(var enemy in map.regionalEnemies)
                {
                    if(enemy==null || double.IsNaN(enemy.bossRespawnAt) || double.IsInfinity(enemy.bossRespawnAt) || enemy.bossRespawnAt<0 || string.IsNullOrWhiteSpace(enemy.id) || !ids.Add(enemy.id) || !types.Any(t=>t.SaveKey==enemy.type) || !SetupDataValidator.Finite(enemy.health) || enemy.health<0 || !SetupDataValidator.Finite(enemy.position.x) || !SetupDataValidator.Finite(enemy.position.y) || !SetupDataValidator.Finite(enemy.home.x) || !SetupDataValidator.Finite(enemy.home.y) || !SetupDataValidator.Finite(enemy.size.x) || !SetupDataValidator.Finite(enemy.size.y) || enemy.size.x<=0 || enemy.size.y<=0)
                        throw new InvalidDataException("저장된 지역 몬스터 설정이 유효하지 않습니다: "+map.scene);
                }
            }
        }
    }
}
