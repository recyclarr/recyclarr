namespace Common;

public class AppContextProxy : IAppContext
{
    public string BaseDirectory => AppContext.BaseDirectory;
}
