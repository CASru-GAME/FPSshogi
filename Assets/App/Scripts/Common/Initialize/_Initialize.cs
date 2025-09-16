namespace App.Common.Initialize
{
    public interface IInitializable
    {
        int InitializationPriority { get; } // 数値が小さいほど先に実行
        System.Type[] Dependencies { get; } // 依存するIInitializableの型
        void Initialize(ReferenceHolder referenceHolder);
    }
}
