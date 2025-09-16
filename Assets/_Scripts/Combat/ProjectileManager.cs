using UnityEngine;
using System.Collections.Generic;
using MonsterLegion.Data;

namespace MonsterLegion.Combat
{
    public class ProjectileManager : MonoBehaviour
    {
        public static ProjectileManager Instance { get; private set; }

        [SerializeField] private GameObject defaultProjectilePrefab; // Prefab viên đạn cơ bản

        private Dictionary<string, List<GameObject>> projectilePools = new Dictionary<string, List<GameObject>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SpawnProjectile(MonsterController attacker, MonsterController target, ProjectileSO projectileData, Vector3 startPosition)
        {
            GameObject projectileGO = GetPooledProjectile(projectileData);

            projectileGO.transform.position = startPosition;

            ProjectileController controller = projectileGO.GetComponent<ProjectileController>();
            controller.Launch(attacker, target, projectileData);
        }

        private GameObject GetPooledProjectile(ProjectileSO data)
        {
            string poolKey = data.ProjectileID;
            if (!projectilePools.ContainsKey(poolKey))
            {
                projectilePools[poolKey] = new List<GameObject>();
            }

            // Tìm một viên đạn không hoạt động trong pool
            foreach (var proj in projectilePools[poolKey])
            {
                if (!proj.activeInHierarchy)
                {
                    return proj;
                }
            }

            // Nếu không có, tạo một viên đạn mới và thêm vào pool
            GameObject prefabToSpawn = data.ProjectilePrefab ?? defaultProjectilePrefab;
            GameObject newProj = Instantiate(prefabToSpawn, transform); // Đặt làm con của Manager
            projectilePools[poolKey].Add(newProj);
            return newProj;
        }
    }
}