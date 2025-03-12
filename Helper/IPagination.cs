namespace ChatAISystem.Helper
{
    public interface IPagination
    {
        int InitialPage { get; }
        int TotalPages { get; }
    }
}
