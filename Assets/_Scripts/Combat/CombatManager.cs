using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MonsterLegion.Core;
using MonsterLegion.Data;
using MonsterLegion.Gameplay;

namespace MonsterLegion.Combat
{
    public class CombatManager : MonoBehaviour
    {
        public enum CombatState { Preparing, InProgress, Victory, Defeat }

        [SerializeField] private Transform[] _playerSpawnPoints = new Transform[6];
        [SerializeField] private Transform[] _enemySpawnPoints = new Transform[6];

        private StageSO _currentStage;
        private List<MonsterController> _playerUnits = new List<MonsterController>();
        private List<MonsterController> _enemyUnits = new List<MonsterController>();

        public CombatState CurrentState { get; private set; }

        private void Start()
        {
            _currentStage = StaticStageHolder.StageToLoad;

            if (_currentStage != null)
            {
                InitializeCombat(_currentStage);
            }
        }

        private void Update()
        {
            if (CurrentState != CombatState.InProgress) return;
            CheckForCombatEnd();
        }

        private void InitializeCombat(StageSO stageData)
        {
            CurrentState = CombatState.Preparing;
            SpawnPlayerTeam();
            SpawnEnemyTeam(stageData);
            CurrentState = CombatState.InProgress;
        }

        private void SpawnPlayerTeam()
        {
            var eliteSquad = GameDataManager.Instance.Data.OwnedMonsters.Where(m => m.IsInEliteSquad).ToList();

            for (int i = 0; i < eliteSquad.Count && i < _playerSpawnPoints.Length; i++)
            {
                MonsterInstance monsterInstance = eliteSquad[i];
                MonsterSpeciesSO speciesData = GameDatabase.Instance.GetMonsterSpeciesByID(monsterInstance.SpeciesID);

                if (speciesData != null && speciesData.ModelPrefab != null)
                {
                    SpawnMonster(monsterInstance, speciesData, _playerSpawnPoints[i], _playerUnits, _enemyUnits);
                }
            }
        }

        private void SpawnEnemyTeam(StageSO stageData)
        {
            foreach (var enemy in stageData.EnemyFormations)
            {
                var enemyInstance = new MonsterInstance(enemy.Species.SpeciesID, Personality.Neutral)
                {
                    CurrentLevel = enemy.Level
                };

                int spawnIndex = enemy.GridPosition.x * 3 + enemy.GridPosition.y;
                if (spawnIndex < _enemySpawnPoints.Length && enemy.Species.ModelPrefab != null)
                {
                    SpawnMonster(enemyInstance, enemy.Species, _enemySpawnPoints[spawnIndex], _enemyUnits, _playerUnits);
                }
            }
        }

        private void SpawnMonster(MonsterInstance instance, MonsterSpeciesSO species, Transform spawnPoint, List<MonsterController> allies, List<MonsterController> opponents)
        {
            GameObject monsterGO = Instantiate(species.ModelPrefab, spawnPoint.position, spawnPoint.rotation);
            MonsterController controller = monsterGO.GetComponent<MonsterController>();

            controller.Initialize(instance, species, allies, opponents);
            controller.OnDeath += HandleMonsterDeath;

            allies.Add(controller);
        }

        private void HandleMonsterDeath(MonsterController deadMonster)
        {
            deadMonster.OnDeath -= HandleMonsterDeath;
            _playerUnits.Remove(deadMonster);
            _enemyUnits.Remove(deadMonster);
        }

        private void CheckForCombatEnd()
        {
            if (_playerUnits.All(m => !m.IsAlive()))
            {
                EndCombat(false);
            }
            else if (_enemyUnits.All(m => !m.IsAlive()))
            {
                EndCombat(true);
            }
        }

        private void EndCombat(bool playerWon)
        {
            CurrentState = playerWon ? CombatState.Victory : CombatState.Defeat;
            // Additional logic for victory/defeat screen and rewards
        }
    }
}