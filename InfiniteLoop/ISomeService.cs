namespace InfiniteLoop
{
    internal interface ISomeService
    {
        void Notify();
        T TryGetItem<T>();
    }
}