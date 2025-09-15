using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using App.Common.Initialize;

namespace App.Main.Initialize
{
    /// <summary>
    /// ゲーム開始時の初期化を行うクラス（依存関係解決対応）
    /// 
    /// 初期化順序の決定ルール：
    /// 1. 依存関係が最優先（依存先が先に初期化される）
    /// 2. 依存関係が同じレベルなら InitializationPriority の小さい順
    /// 3. 優先度も同じなら型名のアルファベット順（安定ソート）
    /// </summary>
    public class GameInitiater : MonoBehaviour
    {
        [Header("初期化設定")]
        [SerializeField] private GameObject[] InitializeObjects = null;
        [SerializeField] private bool useDependencyResolution = true;
        [SerializeField] private bool enableDetailedLogging = true;
        
        private void Awake()
        {
            if (useDependencyResolution)
            {
                InitializeWithDependencies();
            }
            else
            {
                InitializeSimple();
            }
        }
        
        /// <summary>
        /// 依存関係を考慮した初期化
        /// </summary>
        private void InitializeWithDependencies()
        {
            if (enableDetailedLogging)
                Debug.Log("=== GameInitiater: 依存関係解決付き初期化開始 ===");
            
            // 全てのIInitializableコンポーネントを収集
            var components = CollectAllInitializables();
            
            if (enableDetailedLogging)
                Debug.Log($"初期化対象コンポーネント数: {components.Count}");
            
            // 依存関係と優先度を考慮して順序を決定
            var sortedComponents = ResolveDependencyOrderWithPriority(components);
            
            // 順序通りに初期化実行
            foreach (var component in sortedComponents)
            {
                try
                {
                    component.Initialize();
                    
                    if (enableDetailedLogging)
                    {
                        Debug.Log($"✅ 初期化完了: {component.GetType().Name} " +
                                 $"(Priority: {component.InitializationPriority})");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ 初期化エラー: {component.GetType().Name} - {ex.Message}");
                }
            }
            
            if (enableDetailedLogging)
                Debug.Log("=== GameInitiater: 初期化完了 ===");
        }
        
        /// <summary>
        /// シンプルな初期化（従来方式）
        /// </summary>
        private void InitializeSimple()
        {
            Debug.Log("=== GameInitiater: シンプル初期化開始 ===");
            
            foreach (var obj in InitializeObjects)
            {
                if (obj == null) continue;
                
                var initializers = obj.GetComponents<IInitializable>();
                foreach (var initializer in initializers)
                {
                    try
                    {
                        initializer.Initialize();
                        Debug.Log($"✅ 初期化完了: {initializer.GetType().Name}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"❌ 初期化エラー: {initializer.GetType().Name} - {ex.Message}");
                    }
                }
            }
            
            Debug.Log("=== GameInitiater: シンプル初期化完了 ===");
        }
        
        /// <summary>
        /// 全てのIInitializableコンポーネントを収集
        /// </summary>
        private List<IInitializable> CollectAllInitializables()
        {
            var components = new List<IInitializable>();
            
            if (InitializeObjects != null && InitializeObjects.Length > 0)
            {
                // 指定されたオブジェクトから収集
                foreach (var obj in InitializeObjects)
                {
                    if (obj == null) continue;
                    
                    var objComponents = obj.GetComponents<IInitializable>();
                    components.AddRange(objComponents);
                }
            }
            else
            {
                // シーン全体から収集
                var allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .OfType<IInitializable>()
                    .ToList();
                components.AddRange(allComponents);
            }
            
            return components;
        }
        
        /// <summary>
        /// 依存関係と優先度を考慮した初期化順序を決定（統合版）
        /// </summary>
        private List<IInitializable> ResolveDependencyOrderWithPriority(List<IInitializable> components)
        {
            var result = new List<IInitializable>();
            var available = new List<IInitializable>(components);
            var processed = new HashSet<System.Type>();
            
            while (available.Count > 0)
            {
                // 現在初期化可能なコンポーネント（依存関係が満たされている）
                var readyComponents = available.Where(component => 
                    component.Dependencies == null || 
                    component.Dependencies.All(depType => processed.Contains(depType))
                ).ToList();
                
                if (readyComponents.Count == 0)
                {
                    // デッドロック状態（循環依存など）
                    Debug.LogError("🚨 依存関係のデッドロックを検出しました");
                    Debug.LogError($"残りコンポーネント: {string.Join(", ", available.Select(c => c.GetType().Name))}");
                    break;
                }
                
                // 同じ優先度が複数ある場合の警告
                if (readyComponents.Count > 1)
                {
                    var priorityGroups = readyComponents.GroupBy(c => c.InitializationPriority);
                    foreach (var group in priorityGroups.Where(g => g.Count() > 1))
                    {
                        Debug.LogWarning($"⚠️ 同じ優先度({group.Key})のコンポーネントが複数あります: " +
                                       $"{string.Join(", ", group.Select(c => c.GetType().Name))} " +
                                       $"→ 型名順で実行されます");
                    }
                }
                
                // 初期化可能なコンポーネントを優先度順でソート、同じ優先度なら型名順
                var nextComponent = readyComponents
                    .OrderBy(c => c.InitializationPriority)
                    .ThenBy(c => c.GetType().Name)
                    .First();
                
                result.Add(nextComponent);
                processed.Add(nextComponent.GetType());
                available.Remove(nextComponent);
                
                if (enableDetailedLogging)
                {
                    var dependencyNames = nextComponent.Dependencies?.Select(t => t.Name).ToArray() ?? new string[0];
                    Debug.Log($"🔧 初期化順序決定: {nextComponent.GetType().Name} " +
                             $"(Priority: {nextComponent.InitializationPriority}, " +
                             $"Dependencies: [{string.Join(", ", dependencyNames)}])");
                }
            }
            
            return result;
        }
    }
}
