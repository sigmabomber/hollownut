public interface IUnlockCondition
{
    bool IsMet { get; }
    void Initialize(System.Action onConditionMet);
}